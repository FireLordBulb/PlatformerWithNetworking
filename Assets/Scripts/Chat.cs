using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class Chat : MonoBehaviour {
    public static Chat Instance;
    
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private TextMeshProUGUI placeholder;
    [SerializeField] private ChatManager chatManagerPrefab;
    [SerializeField] private InputAction toggleKey;

    public TextMeshProUGUI Text => text;
    public ChatManager ChatManager {get; set;}
    
    private void Awake(){
        if (Instance != null){
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        gameObject.SetActive(false);
        toggleKey.performed += _ => {
            if (!GameManager.Instance.GameIsOngoing){
                return;
            }
            
            gameObject.SetActive(!gameObject.activeSelf);
            if (gameObject.activeSelf){
                inputField.ActivateInputField();
            } else {
                EventSystem.current.SetSelectedGameObject(null);
            }
        };
        toggleKey.Enable();
        
        inputField.onSubmit.AddListener(message => {
            message = message.Trim();
            if (message.Length != 0){
                ChatManager.SubmitMessageRPC(message);
            }
            inputField.ActivateInputField();
            inputField.text = "";
        });
        inputField.onValueChanged.AddListener(message => inputField.text = message.TrimEnd('\n').TrimEnd('\v'));
        inputField.onDeselect.AddListener(_ => {
            gameObject.SetActive(false);
        });
    }

    public void Activate(){
        if (NetworkManager.Singleton.IsServer){
            Instantiate(chatManagerPrefab).GetComponent<NetworkObject>().Spawn();
        }
    }
    
    public void SetPlaceholderText(string placeholderText){
        placeholder.text = placeholderText;
    }

    public void RemoveUsername(ulong clientId){
        if (ChatManager){
            ChatManager.RemoveUsername(clientId);
        }
    }
}
