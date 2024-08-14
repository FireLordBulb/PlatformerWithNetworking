using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour {
    // Start Page
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private Toggle localhostToggle;
    [SerializeField] private TMP_InputField ipInputField;
    // Host Page
    [SerializeField] private Button startButton;
    [SerializeField] private TextMeshProUGUI currentPlayersText;
    [SerializeField] private TextMeshProUGUI ipAddressText;
    [SerializeField] private Button copyIpButton;
    // Join Page
    [SerializeField] private Button disconnectButton;
    
    private void Awake(){
        hostButton.onClick.AddListener(() => {
            hostButton.gameObject.SetActive(false);
            joinButton.gameObject.SetActive(false);
            GameManager.Instance.StartHost();
        });
        joinButton.onClick.AddListener(() => {
            NetworkManager.Singleton.StartClient();
            hostButton.gameObject.SetActive(false);
            joinButton.gameObject.SetActive(false);
            Debug.Log("Started a Client");
        });
    }
}
