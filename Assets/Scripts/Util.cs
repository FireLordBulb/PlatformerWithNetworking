using Unity.Netcode;
using UnityEngine;

public static class Util {
	public const int Up = +1, Down = -1, Right = +1, Left = -1;
	
	public static Vector2 ToDirection(bool isLeft){
		return new Vector2(ToDirectionSign(isLeft), 0);
	}
	// true is -1, (leftwards), false is +1, (rightwards)
	public static int ToDirectionSign(bool isLeft){
		return isLeft ? Left : Right;
	}
	// Global boilerplate because Unity doesn't have sender validation in Rpc:s for some reason.
	// Any hacked client can use any Rpc method to send bogus data to the server/clients which can receive that type of Rpc.
	public static bool SenderIsServer(RpcParams rpcParams){
		return rpcParams.Receive.SenderClientId == NetworkManager.ServerClientId;
	}
}
