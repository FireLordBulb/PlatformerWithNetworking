using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;

public class GameManagerNetworkBehaviour : NetworkBehaviour {

	public bool canDie;
	
	private GameManager gameManager;
	
	private void Awake(){
		gameManager = GameManager.Instance;
	}

	// It's immortal until the gameManager says it isn't. You kill it, and its parent spawns another.
	private new void OnDestroy(){
		if (!canDie){
			gameManager.SpawnNetworkBehavior();
		}
	}
	
	[Rpc(SendTo.Everyone)]
	public void StartGameRpc(RpcParams rpcParams = default){
		if (!Util.SenderIsServer(rpcParams)){
			return;
		}
		gameManager.StartGame();
	}

	public void SendCantJoinRpc(bool gameHasStarted, ulong clientId){
		CantJoinRpc(gameHasStarted, RpcTarget.Single(clientId, RpcTargetUse.Temp));
	}
	[Rpc(SendTo.SpecifiedInParams)]
	private void CantJoinRpc(bool gameHasStarted, RpcParams rpcParams){
		if (!Util.SenderIsServer(rpcParams)){
			return;
		}
		gameManager.CantJoin(gameHasStarted);
	}
	[Rpc(SendTo.Everyone)]
	public void GameIsOverRpc(ulong winnerClientId, RpcParams rpcParams = default){
		if (!Util.SenderIsServer(rpcParams)){
			return;
		}
		gameManager.EndGame(winnerClientId);
	}
}
