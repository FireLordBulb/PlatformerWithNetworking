using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour {
    [Header("Start Page")]
    [SerializeField] private GameObject startPage;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private Toggle localhostToggle;
    [SerializeField] private TMP_InputField ipInputField;
    [SerializeField] private TextMeshProUGUI joinErrorText;
    [Header("Host Page")]
    [SerializeField] private GameObject hostPage;
    [SerializeField] private Button startButton;
    [SerializeField] private TextMeshProUGUI currentPlayersText;
    [SerializeField] private TextMeshProUGUI ipAddressText;
    [SerializeField] private Button copyIpButton;
    [Header("Join Page")]
    [SerializeField] private GameObject joinPage;
    [SerializeField] private Button disconnectButton;
    [Header("Game Over Page")]
    [SerializeField] private GameObject gameOverPage;
    [SerializeField] private TextMeshProUGUI gameResultText;
    [SerializeField] private Button restartButton;

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
        restartButton.onClick.AddListener(() => {
            // Destroy the NetworkManager manually since it's set to "Don't destroy on load."
            Destroy(NetworkManager.Singleton.GameObject());
            // Reload the scene, resetting the entire game.
            SceneManager.LoadScene(gameObject.scene.name);
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
        gameOverPage.SetActive(true);
        gameResultText.text = isWinner ? YouWonMessage : YouLostMessage;
        gameObject.SetActive(true);
    }
}
