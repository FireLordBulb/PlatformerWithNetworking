using Unity.Netcode;

public static class Util {
	// Neat global math constants used for removing "magic numbers".
	public const int Up = +1, Down = -1, Right = +1, Left = -1;
	public const float WholeRotation = 360, HalfRotation = WholeRotation/2;
	public const float FacingRightAngle = 0, FacingLeftAngle = HalfRotation;
	
	// Global boilerplate because Unity doesn't have sender validation in Rpc:s for some reason.
	// Any hacked client can use any Rpc method to send bogus data to the server/clients which can receive that type of Rpc.
	public static bool SenderIsServer(RpcParams rpcParams){
		return rpcParams.Receive.SenderClientId == NetworkManager.ServerClientId;
	}
}
