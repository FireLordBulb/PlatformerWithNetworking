using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ChatManager : NetworkBehaviour {

    private const string
        UsernameTakenPlaceholder = "Username taken!",
        RegularPlaceholder = "Type message...";
    
    private readonly Dictionary<ulong, string> usernames = new();
    private Chat chat;
    
    private void Awake(){
        chat = Chat.Instance;
        chat.ChatManager = this;
    }
    
    public void RemoveUsername(ulong clientId){
        usernames.Remove(clientId);
    }
    
    [Rpc(SendTo.Server)]
    public void SubmitMessageRPC(string message, RpcParams rpcParams = default){
        message = message.Trim();
        if (message.Length == 0){
            return;
        }
        ulong senderClientId = rpcParams.Receive.SenderClientId;
        if (!usernames.TryGetValue(senderClientId, out string username)){
            RpcParams rpcSendParams = RpcTarget.Single(senderClientId, RpcTargetUse.Temp);
            if (usernames.ContainsValue(message)){
                ChangePlaceholderTextRPC(UsernameTakenPlaceholder, rpcSendParams);
            } else {
                usernames.Add(senderClientId, message);
                ChangePlaceholderTextRPC(RegularPlaceholder, rpcSendParams);
            }
            return;
        }
        AddChatLineRPC($"{username}: {message}");
    }
    [Rpc(SendTo.Everyone)]
    private void AddChatLineRPC(string chatLine, RpcParams rpcParams = default){
        if (Util.SenderIsServer(rpcParams)){
            chat.Text.text += $"\n{chatLine}";
        }
    }
    
    [Rpc(SendTo.SpecifiedInParams)]
    private void ChangePlaceholderTextRPC(string placeholderText, RpcParams rpcParams = default){
        if (Util.SenderIsServer(rpcParams)){
            chat.SetPlaceholderText(placeholderText);
        }
    }
}
