using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuitButton : MonoBehaviour
{
    [SerializeField] private Button quitButton;
    
    private void Awake(){
        quitButton.onClick.AddListener(Application.Quit);
    }
}
