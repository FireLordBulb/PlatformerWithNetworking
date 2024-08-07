using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour {
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button quitButton;
    // Start is called before the first frame update
    private void Awake(){
        hostButton.onClick.AddListener(() => {
            NetworkManager.Singleton.StartHost();
            Debug.Log("Started a Host");
        });
        joinButton.onClick.AddListener(() => {
            NetworkManager.Singleton.StartClient();
            Debug.Log("Started a Client");
        });
        quitButton.onClick.AddListener(Application.Quit);
    }
}
