using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkUI : MonoBehaviour {
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    // Start is called before the first frame update
    void Awake()
    {
        hostButton.onClick.AddListener(() => {
            NetworkManager.Singleton.StartHost();
            Debug.Log("Started a Host");
        });
        joinButton.onClick.AddListener(() => {
            NetworkManager.Singleton.StartClient();
            Debug.Log("Started a Client");
        });
    }
}
