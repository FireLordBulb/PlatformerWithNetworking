using Unity.Netcode;

public static class Util {
	// Global boilerplate because Unity doesn't have sender validation in Rpc:s for some reason.
	// Any hacked client can use any Rpc method to send bogus data to the server/clients which can receive that type of Rpc.
	public static bool SenderIsServer(RpcParams rpcParams){
		return rpcParams.Receive.SenderClientId == NetworkManager.ServerClientId;
	}
}
