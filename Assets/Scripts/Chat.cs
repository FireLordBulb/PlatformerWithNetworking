using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class Chat : NetworkBehaviour {
    private const int MaxChars128Bytes = 64;
    private NetworkVariable<bool> lastMessageIsComplete = new (true);
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private TMP_InputField inputField;
    private void Awake(){
        inputField.onSubmit.AddListener(SendChatMessage);
    }
    private void SendChatMessage(string message){
        int unsentCharsInMessage = message.Length;
        if (unsentCharsInMessage <= MaxChars128Bytes){
            SubmitMessageRPC(new FixedString128Bytes(message), NetworkManager.LocalClientId);
        } else {
            while (MaxChars128Bytes < unsentCharsInMessage){
                SubmitMessageRPC(new FixedString128Bytes(message.Substring(message.Length-unsentCharsInMessage, MaxChars128Bytes)), NetworkManager.LocalClientId);
                unsentCharsInMessage -= MaxChars128Bytes;
            }
            SubmitMessageRPC(new FixedString128Bytes(message.Substring(message.Length-unsentCharsInMessage)), NetworkManager.LocalClientId);
        }
        EndMessageRPC();
        inputField.text = "";
    }
    [Rpc(SendTo.Server)]
    private void EndMessageRPC(){
        lastMessageIsComplete.Value = true;
    }
    [Rpc(SendTo.Server)]
    private void SubmitMessageRPC(FixedString128Bytes message, ulong sender){
        UpdateMessageRPC(message, sender);
        lastMessageIsComplete.Value = false;
    }
    [Rpc(SendTo.Everyone)]
    private void UpdateMessageRPC(FixedString128Bytes message, ulong sender){
        if (lastMessageIsComplete.Value){
            text.text += $"\n{sender}: ";
        }
        text.text += message.ToString();
    }
}
