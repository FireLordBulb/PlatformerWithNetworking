using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.VisualScripting;
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
    private int nextPlayerIndex;
    private bool isConnecting;

    private bool IsConnected => NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient || isConnecting;
    
    private void Awake(){
        if (Instance != null){
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        SpawnNetworkBehavior();
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
            if (eventData.EventType != ConnectionEvent.ClientConnected){
                return;
            }
            SpawnPlayer(manager, eventData.ClientId);
            mainMenu.SetCurrentPlayersText(NetworkManager.Singleton.ConnectedClients.Count);
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
        if (!manager.IsServer){
            return;
        }
        if (nextPlayerIndex < playerSpawns.Length){
            Player newPlayer = Instantiate(playerPrefab, playerSpawns[nextPlayerIndex].position, Quaternion.identity);
            newPlayer.NetworkObject.SpawnWithOwnership(clientId);
            if (playerSpawns[nextPlayerIndex].rotation.y != 0){
                newPlayer.SetBodySprite(true);
            }
            nextPlayerIndex++;
        } else{
            networkBehaviour.SendRoomIsFullRpc(clientId);
            manager.DisconnectClient(clientId);
        }
    }
    
    public void StartClient(string ipAddress){
        if (IsConnected){
            return;
        }
        transport.ConnectionData.Address = ipAddress;
        Debug.Log($"Starting and client and connecting to: {transport.ConnectionData.Address}");
        isConnecting = true;
        Invoke(nameof(CheckIfConnectSuccessful), connectTimeout);
        NetworkManager.Singleton.StartClient();
    }

    private void CheckIfConnectSuccessful(){
        if (!isConnecting){
            return;
        }
        isConnecting = false;
        print("invoke called");
        if (NetworkManager.Singleton.IsConnectedClient){
            mainMenu.SwapToJoinPage();
            print("is connected");
        } else {
            print("is not connected");
            mainMenu.CantFindHost();
            try{
                NetworkManager.Singleton.Shutdown();
            } catch(Exception e){
                Console.WriteLine(e);
            }
        }
    }
    
    public void SendStartGameRpc(){
        networkBehaviour.StartGameRpc();
    }
    public void StartGame(){
        // TODO enable input.
        mainMenu.SwapToHud();
    }

    public void RoomIsFull(){
        mainMenu.HostHasMaxPlayers();
        mainMenu.SwapToStartPage();
        isConnecting = false;
    }
}
