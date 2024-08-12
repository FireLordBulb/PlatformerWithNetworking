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
    [SerializeField] private float jumpStartImpulse;
    [SerializeField] private float jumpHoldForce;
    [SerializeField] private float jumpHoldTime;
    
    public const int Up = +1, Down = -1, Right = +1, Left = -1;
    private const float MaxGroundDistance = 0.02f;
    
    private readonly NetworkVariable<Vector2> networkCurrentDirection = new();
    private Vector2 localCurrentDirection;
    private float jumpHoldTimeLeft;
    private bool isOnGround;
    private bool isOnSolidGround;
    private bool canDoubleJump;
    public void Awake(){
        blade.SetWielder(this);
    }
    
    public void Move(Vector2 direction){
        int xDirection = (int)direction.x;
        int yDirection = (int)direction.y;
        switch(xDirection){
            case Right:
                SetBodySprite(false);
                break;
            case Left:
                SetBodySprite(true);
                break;
            case 0:
                break;
            default:
                // Invalid value for xDirection (only Right, Left & 0 are valid), client which sent "direction" must have been hacked in an attempt to cheat.
                // Return early to ignore input to prevent the hacked values from affecting the server.
                return;
        }
        switch(yDirection){
            case Up:
            case Down:
            case 0:
                break;
            default:
                // Invalid value for yDirection (only Up, Down & 0 are valid), client which sent "direction" must have been hacked in an attempt to cheat.
                // Return early to ignore input to prevent the hacked values from affecting the server.
                return;
        }
        if (IsServer){
            networkCurrentDirection.Value = direction;
        } else if (IsOwner){
            localCurrentDirection = direction;
        }
        rigidBody.AddForce(new Vector2(xDirection*runningSpeed, 0), ForceMode2D.Force);
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
    public void Jump(){
        if (!isOnGround){
            if (!canDoubleJump){
                return;
            }
            canDoubleJump = false;
        } else {
            isOnGround = false;
            isOnSolidGround = false;
            canDoubleJump = true;
        }
        rigidBody.velocity = new Vector2(rigidBody.velocity.x, 0);
        rigidBody.AddForce(new Vector2(0, jumpStartImpulse), ForceMode2D.Impulse);
        jumpHoldTimeLeft = jumpHoldTime;
    }
    public void StopJumping(){
        jumpHoldTimeLeft = 0;
    }
    public void Attack(){
        blade.StartSwinging((!IsServer && IsOwner ? localCurrentDirection : networkCurrentDirection.Value).y, body.flipX, isOnSolidGround);
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
        if (isOnGround || jumpHoldTimeLeft == 0){
            Bounds legBounds = legCollider.bounds;
            CheckForGroundBelow(legBounds.min.x, legBounds.max.x, (legBounds.min.x+legBounds.max.x)/2);
        }
        JumpHoldUpdate();
    }
    
    private void CheckForGroundBelow(params float[] xValues){
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (float x in xValues){
            RaycastHit2D hit = Physics2D.Raycast(new Vector2(x, transform.position.y), -Vector2.up, MaxGroundDistance);
            if (!hit){
                continue;
            }
            isOnGround = true;
            isOnSolidGround = hit.collider.attachedRigidbody == null;
            return;
        }
        if (isOnGround){
            canDoubleJump = true;
        }
        isOnGround = false;
        isOnSolidGround = false;
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
            blade.StartSwinging(networkCurrentDirection.Value.y, body.flipX, isOnSolidGround);
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
