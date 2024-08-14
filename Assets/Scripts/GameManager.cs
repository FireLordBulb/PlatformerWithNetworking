using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class GameManager : MonoBehaviour {
    public static GameManager Instance;

    [SerializeField] private Player playerPrefab;
    [SerializeField] private Transform[] playerSpawns;

    private UnityTransport transport;
    private int nextPlayerIndex;
    
    private void Awake(){
        if (Instance != null){
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    private void Start(){
        transport = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
    }
    
    public static string GetLocalIPAddress()
    {
        IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (IPAddress ip in host.AddressList){
            if (ip.AddressFamily == AddressFamily.InterNetwork){
                return ip.ToString();
            }
        }
        return null;
    }
    
    public void StartHost(){
        NetworkManager.Singleton.StartHost();
        print($"Started a Host: {transport.ConnectionData.Address}");
        print($"Local IP: {GetLocalIPAddress()}");
        SpawnPlayer(NetworkManager.Singleton, NetworkManager.Singleton.LocalClientId);
        NetworkManager.Singleton.OnConnectionEvent += (manager, eventData) => {
            if (eventData.EventType == ConnectionEvent.ClientConnected){
                SpawnPlayer(manager, eventData.ClientId);
            }
        };
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
        } else {
            // TODO send connect failed Rpc
            manager.DisconnectClient(clientId);
        }
    }
}
