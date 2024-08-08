using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour {
    [SerializeField] private Rigidbody2D rigidBody;
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private Blade blade;
    [SerializeField] private float runningSpeed;
    [SerializeField] private float jumpForce;
    private const int Up = +1, Down = -1, Right = +1, Left = -1;
    private Vector2 currentDirection;
    public void Awake(){
        
    }
    public void Move(Vector2 direction){
        currentDirection = direction;
        int xMovement = (int)currentDirection.x;
        switch(xMovement){
            case Right:
                SetBodySprite(false);
                break;
            case Left:
                SetBodySprite(true);
                break;
            case 0:
                break;
            default:
                // Invalid value for xMovement (only Right, Left & 0 are valid), client which sent "direction" must have been hacked in an attempt to cheat.
                // Return early to ignore input to prevent the hacked values from affecting the server.
                return;
        }
        rigidBody.AddForce(new Vector2(xMovement*runningSpeed, 0), ForceMode2D.Force);
    }
    private void SetBodySprite(bool flipX){
        if (body.flipX == flipX){
            return;
        }
        body.flipX = flipX;
        if (IsServer){
            SetBodySpriteRpc(flipX);
        }
    }
    // TODO: Add holdable jump.
    public void Jump(){
        rigidBody.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
    }
    public void Attack(){
        
    }
    public void FixedUpdate(){
        
    }
  
    // Should only be sent by server, the if-statement checks that this is the case on all unmodified clients,
    // so a hacked client can't use this to alter the state of other clients.
    [Rpc(SendTo.NotServer)]
    private void SetBodySpriteRpc(bool flipX, RpcParams rpcParams = default){
        if (rpcParams.Receive.SenderClientId == NetworkManager.ServerClientId){
            body.flipX = flipX;
        }
    }
}
