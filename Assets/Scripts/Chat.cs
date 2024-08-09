using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class Chat : NetworkBehaviour {
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private TMP_InputField inputField;

    private Dictionary<ulong, string> usernames = new();
    private void Awake(){
        inputField.onSubmit.AddListener(message => {
            SendChatMessage(message);
            inputField.ActivateInputField();
            inputField.text = "";
        });
        inputField.onValueChanged.AddListener(message => inputField.text = message.TrimEnd('\n').TrimEnd('\v'));
    }
    private void SendChatMessage(string message){
        message = message.Trim();
        if (message.Length == 0){
            return;
        }
        SubmitMessageRPC(message);
    }
    [Rpc(SendTo.Server)]
    private void SubmitMessageRPC(string message, RpcParams rpcParams = default){
        ulong senderClientId = rpcParams.Receive.SenderClientId;
        if (!usernames.TryGetValue(senderClientId, out string username)){
            username = $"{senderClientId}";
        }
        AddChatLineRPC($"{username}: {message}");
    }
    [Rpc(SendTo.Everyone)]
    private void AddChatLineRPC(string chatLine, RpcParams rpcParams = default){
        if (rpcParams.Receive.SenderClientId == NetworkManager.ServerClientId){
            text.text += $"\n{chatLine}";
        }
    }
}
