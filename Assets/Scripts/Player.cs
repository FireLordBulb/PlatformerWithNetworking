using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour {
    [SerializeField] private SpriteRenderer body;
    private Vector2 currentDirection;
    public void Awake(){
        
    }
    public void Move(Vector2 direction){
        currentDirection = direction;
        if (0 < currentDirection.x){
            body.flipX = false;
        } else if (currentDirection.x < 0){
            body.flipX = true;
        }
        
    }
    public void Jump(){
        
    }
    public void Attack(){
        
    }
    public void FixedUpdate(){
        
    }
}
