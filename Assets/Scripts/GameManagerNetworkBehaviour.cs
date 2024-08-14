using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class GameManagerNetworkBehaviour : NetworkBehaviour {

	private GameManager gameManager;
	
	private void Awake(){
		gameManager = GameManager.Instance;
	}

	// It's immortal. You kill it, and its parent spawns another.
	private void OnDisable(){
		gameManager.SpawnNetworkBehavior();
	}
	
	[Rpc(SendTo.Everyone)]
	public void StartGameRpc(RpcParams rpcParams = default){
		if (!Util.SenderIsServer(rpcParams)){
			return;
		}
		gameManager.StartGame();
	}

	public void SendRoomIsFullRpc(ulong clientId){
		RoomIsFullRpc(RpcTarget.Single(clientId, RpcTargetUse.Temp));
	}
	[Rpc(SendTo.SpecifiedInParams)]
	public void RoomIsFullRpc(RpcParams rpcParams){
		if (!Util.SenderIsServer(rpcParams)){
			return;
		}
		gameManager.RoomIsFull();
	}
}
