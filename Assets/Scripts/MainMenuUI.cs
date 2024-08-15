using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour {
    // Start Page
    [SerializeField] private GameObject startPage;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private Toggle localhostToggle;
    [SerializeField] private TMP_InputField ipInputField;
    [SerializeField] private TextMeshProUGUI joinErrorText;
    // Host Page
    [SerializeField] private GameObject hostPage;
    [SerializeField] private Button startButton;
    [SerializeField] private TextMeshProUGUI currentPlayersText;
    [SerializeField] private TextMeshProUGUI ipAddressText;
    [SerializeField] private Button copyIpButton;
    // Join Page
    [SerializeField] private GameObject joinPage;
    [SerializeField] private Button disconnectButton;

    private const string
        CantFindHostMessage = "Can't find host at IP address!",
        HostHasMaxPlayersMessage = "Host already has max players!",
        GameHasStartedMessage = "Game has already started!",
        YouLostMessage = "You lost!",
        YouWonMessage = "You won!";

    private string ipAddress;
    
    private void Start(){
        GameManager gameManager = GameManager.Instance;
        gameManager.SetMainMenu(this);
        
        hostButton.onClick.AddListener(() => {
            ipAddress = gameManager.StartHost(localhostToggle.isOn);
            if (ipAddress == null){
                return;
            }
            startPage.SetActive(false);
            hostPage.SetActive(true);
            SetCurrentPlayersText(1);
            ipAddressText.text = $"IP address:\n{ipAddress}";
        });
        joinButton.onClick.AddListener(() => {
            gameManager.StartClient(ipInputField.text);
        });
        // Clear any error message when the IP address is edited.
        ipInputField.onValueChanged.AddListener(value => {
            joinErrorText.text = "";
        });
        startButton.onClick.AddListener(() => {
            gameManager.SendStartGameRpc();
        });
        copyIpButton.onClick.AddListener(() => {
            GUIUtility.systemCopyBuffer = ipAddress;
        });
        disconnectButton.onClick.AddListener(() => {
            SwapToStartPage();
            gameManager.Disconnect();
        });
    }

    public void SetCurrentPlayersText(int currentPlayers){
        currentPlayersText.text = $"Current players: {currentPlayers}";
        startButton.interactable = 1 < currentPlayers;
    }

    public void SwapToJoinPage(){
        startPage.SetActive(false);
        joinPage.SetActive(true);
    }
    public void SwapToStartPage(){
        joinPage.SetActive(false);
        startPage.SetActive(true);
    }
    
    public void CantFindHost(){
        joinErrorText.text = CantFindHostMessage;
    }
    public void HostHasMaxPlayers(){
        joinErrorText.text = HostHasMaxPlayersMessage;
    }
    public void GameHasStarted(){
        joinErrorText.text = GameHasStartedMessage;
    }
    
    public void SwapToHud(){
        gameObject.SetActive(false);
        HUD.Instance.gameObject.SetActive(true);
    }

    public void SwapToGameOverPage(bool isWinner){
        hostPage.SetActive(false);
        joinPage.SetActive(false);
        //gameOverPage.SetActive(false);
        //gameResultText.text = isWinner ? YouWonMessage : LostWonMessage;
        print(isWinner ? YouWonMessage : YouLostMessage);
        gameObject.SetActive(true);
    }
}
