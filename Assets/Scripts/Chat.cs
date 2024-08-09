using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;

public class Chat : NetworkBehaviour {
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private TMP_InputField inputField;
    
    private const int MaxChars128Bytes = 64;
    private readonly NetworkVariable<bool> lastMessageIsComplete = new(true);

    private void Awake(){
        inputField.onSubmit.AddListener(message => {
            inputField.text = message.Trim();
            SendChatMessage(inputField.text);
            inputField.ActivateInputField();
            inputField.text = "";
        });
        inputField.onValueChanged.AddListener(message => inputField.text = message.TrimEnd('\n').TrimEnd('\v'));
    }
    private void SendChatMessage(string message){
        int unsentCharsInMessage = message.Length;
        if (unsentCharsInMessage == 0){
            return;
        }
        if (unsentCharsInMessage <= MaxChars128Bytes){
            SubmitMessageRPC(new FixedString128Bytes(message), NetworkManager.LocalClientId);
        } else {
            while (MaxChars128Bytes < unsentCharsInMessage){
                FixedString128Bytes messageChunk = new(message.Substring(message.Length - unsentCharsInMessage, MaxChars128Bytes));
                SubmitMessageRPC(messageChunk, NetworkManager.LocalClientId);
                unsentCharsInMessage -= MaxChars128Bytes;
            }
            SubmitMessageRPC(new FixedString128Bytes(message[^unsentCharsInMessage..]), NetworkManager.LocalClientId);
        }
        EndMessageRPC();
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
