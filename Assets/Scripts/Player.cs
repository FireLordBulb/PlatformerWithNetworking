using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour {
    [SerializeField] private Rigidbody2D rigidBody;
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private float runningSpeed;
    [SerializeField] private float jumpForce;
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
        rigidBody.AddForce(new Vector2(currentDirection.x*runningSpeed, 0), ForceMode2D.Force);
    }
    // TODO: Add holdable jump.
    public void Jump(){
        rigidBody.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
    }
    public void Attack(){
        
    }
    public void FixedUpdate(){
        
    }
}
