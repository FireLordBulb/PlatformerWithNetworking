using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HUD : MonoBehaviour {
    public static HUD Instance;

    [SerializeField] private Image[] hearts;
    [SerializeField] private Sprite fullHeart;
    [SerializeField] private Sprite brokenHeart;
    
    private void Awake(){
        if (Instance != null){
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        gameObject.SetActive(false);
    }
    
    public void SetHeartsLeft(int heartsLeft){
        for (int i = 0; i < hearts.Length; i++){
            hearts[i].sprite = i < heartsLeft ? fullHeart : brokenHeart;
        }
    }
}
