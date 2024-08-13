using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;

public class Player : NetworkBehaviour {
    [SerializeField] private NetworkObject networkObject;
    [SerializeField] private Rigidbody2D rigidBody;
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private Collider2D legCollider;
    [SerializeField] private Transform fireballSpawn;
    [SerializeField] private Blade blade;
    [SerializeField] private Fireball fireballPrefab;
    [SerializeField] private float invisTime;
    [SerializeField] private float runningSpeed;
    [SerializeField] private float jumpStartImpulse;
    [SerializeField] private float jumpHoldForce;
    [SerializeField] private float jumpHoldTime;
    [SerializeField] private float knockbackImpulse;
    [SerializeField] private float sideUpwardsKnockbackImpulse;
    [SerializeField] private float pogoImpulse;
    [SerializeField] private int maxHealth;
    
    public const int Up = +1, Down = -1, Right = +1, Left = -1;
    private const float MaxGroundDistance = 0.02f;
    
    private readonly NetworkVariable<Vector2> networkCurrentDirection = new();
    private Vector2 localCurrentDirection;
    private float invisTimeLeft;
    private float jumpHoldTimeLeft;
    private int healthPoints;
    private bool isOnGround;
    private bool isOnSolidGround;
    private bool canDoubleJump;

    private Vector2 CurrentDirection => !IsServer && IsOwner ? localCurrentDirection : networkCurrentDirection.Value;
    public void Awake(){
        blade.SetWielder(this);
        healthPoints = maxHealth;
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
        AddYCancelingImpulse(new Vector2(0, jumpStartImpulse));
        jumpHoldTimeLeft = jumpHoldTime;
    }
    public void StopJumping(){
        jumpHoldTimeLeft = 0;
    }
    public void Attack(){
        blade.StartSwinging(CurrentDirection.y, body.flipX, isOnSolidGround);
        if (IsServer){
            StartSwingingRpc();
        }
    }
    public void CastSpell(){
        if (!IsServer || blade.IsSwinging){
            return;
        }
        Fireball newFireball = Instantiate(fireballPrefab, fireballSpawn.position, Quaternion.identity);
        newFireball.GetAttackHitbox().Player = this;
        newFireball.GetComponent<NetworkObject>().Spawn();
        newFireball.StartMovingRpc(body.flipX);
    }
    private void OnTriggerEnter2D(Collider2D other){
        if (0 < invisTimeLeft || !other.gameObject.TryGetComponent(out AttackHitbox hitbox) || hitbox.Player == this){
            return;
        }
        Vector2 knockback = new();
        if (hitbox is BladeHitbox bladeHitbox){
            knockback = bladeHitbox.Blade.SwingDirection*knockbackImpulse;
            hitbox.Player.Pogo();
        } else if (hitbox.TryGetComponent(out Fireball fireball)){
            if (fireball.GetAttackHitbox().Player == this){
                return;
            }
            knockback = new Vector2(fireball.IsMovingLeft ? -knockbackImpulse : knockbackImpulse, 0);
        }
        if (knockback.y == 0){
            knockback.y += sideUpwardsKnockbackImpulse;
        }
        AddYCancelingImpulse(knockback);
        invisTimeLeft = invisTime;
        if (!IsServer){
            return;
        }
        healthPoints--;
        UpdateHealthPointsRpc(healthPoints);
        if (0 < healthPoints){
            return;
        }
        print($"Client {OwnerClientId}'s player died!");
        networkObject.Despawn();
    }
    private void Pogo(){
        if (blade.HasPogoed || blade.SwingDirection != new Vector2(0, Down)){
            return;
        }
        blade.HasPogoed = true;
        jumpHoldTimeLeft = 0;
        canDoubleJump = true;
        AddYCancelingImpulse(new Vector2(0, pogoImpulse));
    }
    private void AddYCancelingImpulse(Vector2 impulse){
        rigidBody.velocity = new Vector2(rigidBody.velocity.x, 0);
        rigidBody.AddForce(impulse, ForceMode2D.Impulse);
    }
    
    public void FixedUpdate(){
        rigidBody.AddForce(new Vector2(CurrentDirection.x*runningSpeed, 0), ForceMode2D.Force);
        if (0 < invisTimeLeft){
            invisTimeLeft -= Time.fixedDeltaTime;
            if (invisTimeLeft < 0){
                invisTimeLeft = 0;
            }
        }
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
            // Dynamic Rigidbodies (like other players) are ground, but not solid ground.
            isOnSolidGround = hit.collider.attachedRigidbody == null || hit.collider.attachedRigidbody.bodyType != RigidbodyType2D.Dynamic;
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
            blade.StartSwinging(CurrentDirection.y, body.flipX, isOnSolidGround);
        }
    }
    [Rpc(SendTo.Owner)]
    private void UpdateHealthPointsRpc(int serverHealthPoints, RpcParams rpcParams = default){
        if (rpcParams.Receive.SenderClientId != NetworkManager.ServerClientId){
            return;
        }
        healthPoints = serverHealthPoints;
        HealthUI.Instance.SetHeartsLeft(healthPoints);
        print($"Client {OwnerClientId}'s player got hit!\n{healthPoints} health points left.");
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
