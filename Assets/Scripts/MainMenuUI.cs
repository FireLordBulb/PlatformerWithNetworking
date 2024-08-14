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
    [SerializeField] private TextMeshProUGUI joinErrorText;
    // Host Page
    [SerializeField] private Button startButton;
    [SerializeField] private TextMeshProUGUI currentPlayersText;
    [SerializeField] private TextMeshProUGUI ipAddressText;
    [SerializeField] private Button copyIpButton;
    // Join Page
    [SerializeField] private Button disconnectButton;

    private const string CantFindHostMessage = "Can't find host at IP address!", HostMaxPlayersMessage = "Host already has max players!";
    
    private void Awake(){
        GameManager gameManager = GameManager.Instance;
        
        hostButton.onClick.AddListener(() => {
            hostButton.gameObject.SetActive(false);
            joinButton.gameObject.SetActive(false);
            gameManager.StartHost(localhostToggle.isOn);
        });
        joinButton.onClick.AddListener(() => {
            hostButton.gameObject.SetActive(false);
            joinButton.gameObject.SetActive(false);
            gameManager.StartClient(ipInputField.text);
        });
        // Clear any error message when the IP address is edited.
        ipInputField.onValueChanged.AddListener(value => {
            joinErrorText.text = "";
        });
        //
        startButton.onClick.AddListener(() => {
            
        });
        copyIpButton.onClick.AddListener(() => {
            
        });
        disconnectButton.onClick.AddListener(() => {
            
        });
    }
}
