using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour {
    [SerializeField] private Rigidbody2D rigidBody;
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private Collider2D legCollider;
    [SerializeField] private Blade blade;
    [SerializeField] private float runningSpeed;
    [SerializeField] private float jumpStartForce;
    [SerializeField] private float jumpHoldForce;
    [SerializeField] private float jumpHoldTime;
    public const int Up = +1, Down = -1, Right = +1, Left = -1;
    private const float MaxGroundDistance = 0.02f;
    private Vector2 currentDirection;
    private float jumpHoldTimeLeft;
    private bool isOnGround;
    private bool canDoubleJump;
    public void Awake(){
        blade.SetWielder(this);
    }
    public void Move(Vector2 direction){
        int xMovement = (int)direction.x;
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
        currentDirection = direction;
        rigidBody.AddForce(new Vector2(xMovement*runningSpeed, 0), ForceMode2D.Force);
    }
    private void SetBodySprite(bool flipX){
        if (body.flipX == flipX || blade.IsSwinging){
            return;
        }
        body.flipX = flipX;
        if (IsServer){
            SetBodySpriteRpc(flipX);
        }
    }
    // TODO: Add double jump.
    public void Jump(){
        if (!isOnGround){
            if (!canDoubleJump){
                return;
            }
            canDoubleJump = false;
        } else {
            isOnGround = false;
            canDoubleJump = true;
        }
        rigidBody.AddForce(new Vector2(0, jumpStartForce), ForceMode2D.Force);
        jumpHoldTimeLeft = jumpHoldTime;
    }
    public void StopJumping(){
        jumpHoldTimeLeft = 0;
    }
    public void Attack(){
        blade.StartSwinging(currentDirection.y, body.flipX);
        if (IsServer){
            StartSwingingRpc();
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other){
        if (!other.gameObject.TryGetComponent(out BladeHitbox hitbox) || hitbox.Wielder == this){
            return;
        }
        print($"Client {OwnerClientId}'s player got hit!");
        if (!IsServer){
            return;
        }
    }
    
    public void FixedUpdate(){
        if (jumpHoldTimeLeft == 0){
            Bounds legBounds = legCollider.bounds;
            CheckForGroundBelow(legBounds.min.x, legBounds.max.x, (legBounds.min.x+legBounds.max.x)/2);
        }
        JumpHoldUpdate();
    }

    private void CheckForGroundBelow(params float[] xValues){
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (float x in xValues){
            if (Physics2D.Raycast(new Vector2(x, transform.position.y), -Vector2.up, MaxGroundDistance)){
                isOnGround = true;
                return;
            }
        }
    }
    private void JumpHoldUpdate(){
        if (jumpHoldTimeLeft == 0){
            return;
        }
        jumpHoldTimeLeft -= Time.fixedDeltaTime;
        if (jumpHoldTimeLeft < 0){
            jumpHoldTimeLeft = 0;
            return;
        }
        rigidBody.AddForce(new Vector2(0, jumpHoldForce), ForceMode2D.Force);
    }
    
    [Rpc(SendTo.Everyone)]
    private void StartSwingingRpc(RpcParams rpcParams = default){
        if (rpcParams.Receive.SenderClientId == NetworkManager.ServerClientId){
            blade.StartSwinging(currentDirection.y, body.flipX);
        }
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
