using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class GameManager : MonoBehaviour {
    public static GameManager Instance;
    
    [SerializeField] private Transform[] playerSpawns;
    [SerializeField] private Player playerPrefab;
    [SerializeField] private float connectTimeout;
    
    private const string Localhost = "127.0.0.1";
    
    private GameManagerNetworkBehaviour networkBehaviour;
    private UnityTransport transport;
    private MainMenuUI mainMenu;
    private readonly List<int> unusedSpawnIndexes = new();
    private readonly Dictionary<ulong, Player> players = new();
    private bool isConnecting;
    private bool gameIsOngoing;

    private bool IsConnected => NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient || isConnecting;
    
    private void Awake(){
        if (Instance != null){
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        SpawnNetworkBehavior();
        for (int i = 0; i < playerSpawns.Length; i++){
            unusedSpawnIndexes.Add(i);
        }
    }
    private void OnDestroy(){
        networkBehaviour.canDie = true;
    }

    // This has to be a separate class because all NetworkObjects even ones that were always in the
    // scene, get destroyed when the client disconnects or fails to connect.
    public void SpawnNetworkBehavior(){
        GameObject emptyChild = Instantiate(new GameObject(), transform);
        emptyChild.AddComponent<NetworkObject>();
        networkBehaviour = emptyChild.AddComponent<GameManagerNetworkBehaviour>();
    }
    
    private void Start(){
        transport = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
    }
    public void SetMainMenu(MainMenuUI mainMenuUI){
        
        mainMenu = mainMenuUI;
    }
    
    public string StartHost(bool doUseLocalhost){
        if (IsConnected){
            return null;
        }
        NetworkManager.Singleton.StartHost();
        transport.ConnectionData.Address = doUseLocalhost ? Localhost : GetLocalIPAddress();
        print($"Started a Host: {transport.ConnectionData.Address}");
        SpawnPlayer(NetworkManager.Singleton, NetworkManager.Singleton.LocalClientId);
        NetworkManager.Singleton.OnConnectionEvent += (manager, eventData) => {
            switch(eventData.EventType){
                case ConnectionEvent.ClientConnected:
                    SpawnPlayer(manager, eventData.ClientId);
                    mainMenu.SetCurrentPlayersText(NetworkManager.Singleton.ConnectedClients.Count);
                    break;
                case ConnectionEvent.ClientDisconnected:
                    // Insert at the start to prevent joining then disconnecting then joining again from letting a player cycle through different spawns.
                    unusedSpawnIndexes.Insert(0, players[eventData.ClientId].SpawnIndex);
                    RemovePlayer(eventData.ClientId);
                    break;
                case ConnectionEvent.PeerConnected:
                case ConnectionEvent.PeerDisconnected:
                default:
                    break;
            }
        };
        return transport.ConnectionData.Address;
    }
    
    private static string GetLocalIPAddress()
    {
        IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (IPAddress ip in host.AddressList){
            if (ip.AddressFamily == AddressFamily.InterNetwork){
                return ip.ToString();
            }
        }
        return Localhost;
    }
    
    private void SpawnPlayer(NetworkManager manager, ulong clientId){
        if (gameIsOngoing){
            networkBehaviour.SendCantJoinRpc(true, clientId);
            manager.DisconnectClient(clientId);
            return;
        }
        if (unusedSpawnIndexes.Count < 1){
            networkBehaviour.SendCantJoinRpc(false, clientId);
            manager.DisconnectClient(clientId);
            return;
        }
        Transform playerSpawn = playerSpawns[unusedSpawnIndexes[0]];
        Player newPlayer = Instantiate(playerPrefab, playerSpawn.position, Quaternion.identity);
        newPlayer.NetworkObject.SpawnWithOwnership(clientId);
        if (playerSpawn.rotation.y != 0){
            newPlayer.SetBodySprite(true);
        }
        players.Add(clientId, newPlayer);
        newPlayer.SpawnIndex = unusedSpawnIndexes[0];
        unusedSpawnIndexes.RemoveAt(0);
    }
    
    public void StartClient(string ipAddress){
        if (IsConnected){
            return;
        }
        transport.ConnectionData.Address = ipAddress;
        Debug.Log($"Starting and client and connecting to: {transport.ConnectionData.Address}");
        isConnecting = true;
        Invoke(nameof(CheckIfConnectSuccessful), connectTimeout);
        NetworkManager.Singleton.OnConnectionEvent += OnClientConnect;
        NetworkManager.Singleton.StartClient();
    }

    private void CheckIfConnectSuccessful(){
        NetworkManager.Singleton.OnConnectionEvent -= OnClientConnect;
        if (!isConnecting){
            return;
        }
        isConnecting = false;
        if (NetworkManager.Singleton.IsConnectedClient){
            mainMenu.SwapToJoinPage();
        } else {
            mainMenu.CantFindHost();
            NetworkManager.Singleton.Shutdown();
        }
    }
    private void OnClientConnect(NetworkManager manager, ConnectionEventData eventData){
        if (eventData.EventType != ConnectionEvent.ClientConnected){
            return;
        }
        isConnecting = false;
        mainMenu.SwapToJoinPage();
    }

    public void Disconnect(){
        isConnecting = false;
        NetworkManager.Singleton.Shutdown();
    }
    
    public void SendStartGameRpc(){
        networkBehaviour.StartGameRpc();
    }
    public void StartGame(){
        if (gameIsOngoing){
            return;
        }
        // TODO enable input.
        gameIsOngoing = true;
        mainMenu.SwapToHud();
    }

    public void CantJoin(bool gameHasStartedOnServer){
        if (gameHasStartedOnServer){
            mainMenu.GameHasStarted();
        } else{
            mainMenu.HostHasMaxPlayers();
        }
        mainMenu.SwapToStartPage();
        isConnecting = false;
    }

    public void RemovePlayer(ulong playerClientId){
        players.Remove(playerClientId);
        if (!gameIsOngoing || players.Count != 1){
            return;
        }
        networkBehaviour.GameIsOverRpc(players.ToList()[0].Value.OwnerClientId);
    }

    public void EndGame(ulong winnerClientId){
        gameIsOngoing = false;
        mainMenu.SwapToGameOverPage(winnerClientId == NetworkManager.Singleton.LocalClientId);
    }
}
