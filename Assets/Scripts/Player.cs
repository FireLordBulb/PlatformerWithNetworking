using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour {
    // Components on the prefab.
    [SerializeField] private NetworkObject networkObject;
    [SerializeField] private Rigidbody2D rigidBody;
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private Collider2D legCollider;
    [SerializeField] private Transform fireballSpawn;
    [SerializeField] private GameObject fireballCooldownBackground;
    [SerializeField] private Transform fireballCooldownBar;
    [SerializeField] private Blade blade;
    // Config values.
    [SerializeField] private Fireball fireballPrefab;
    [SerializeField] private float fireballCooldownTime;
    [SerializeField] private float invisTime;
    [SerializeField] private float runningSpeed;
    [SerializeField] private float jumpStartImpulse;
    [SerializeField] private float jumpHoldForce;
    [SerializeField] private float jumpHoldTime;
    [SerializeField] private float knockbackImpulse;
    [SerializeField] private float sideUpwardsKnockbackImpulse;
    [SerializeField] private float pogoImpulse;
    [SerializeField] private int maxHealth;
    
    // Constants
    private const float MaxGroundDistance = 0.02f;
    
    // Mutable state
    private readonly NetworkVariable<Vector2> networkCurrentDirection = new();
    private Vector2 localCurrentDirection;
    private float fireballCooldownTimeLeft;
    private float invisTimeLeft;
    private float jumpHoldTimeLeft;
    private int healthPoints;
    private bool isOnGround;
    private bool isOnSolidGround;
    private bool canDoubleJump;

    public int SpawnIndex {get; set;}
    public new NetworkObject NetworkObject => networkObject;
    private Vector2 CurrentDirection => !IsServer && IsOwner ? localCurrentDirection : networkCurrentDirection.Value;
    public void Awake(){
        blade.SetWielder(this);
        healthPoints = maxHealth;
    }
    
    public void Move(Vector2 direction){
        int xDirection = (int)direction.x;
        int yDirection = (int)direction.y;
        switch(xDirection){
            case Util.Right:
                SetBodySprite(false);
                break;
            case Util.Left:
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
            case Util.Up:
            case Util.Down:
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
    public void SetBodySprite(bool flipX){
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
        if (0 < fireballCooldownTimeLeft || blade.IsSwinging){
            return;
        }
        if (IsOwner){
            StartFireballCooldown();
        }
        if (!IsServer){
            return;
        }
        StartFireballCooldownRpc();
        Fireball newFireball = Instantiate(fireballPrefab, fireballSpawn.position, Quaternion.identity);
        newFireball.SetCaster(this);
        newFireball.GetComponent<NetworkObject>().Spawn();
        newFireball.StartMovingRpc(body.flipX);
    }
    public void StartFireballCooldown(){
        fireballCooldownBackground.SetActive(true);
        fireballCooldownBar.localScale = Vector3.one;
        fireballCooldownTimeLeft = fireballCooldownTime;
    }
    public void GetHit(AttackHitbox hitbox, Vector2 knockback, bool canCausePogo = false){
        if (0 < invisTimeLeft || hitbox.Player == this){
            return;
        }
        if (canCausePogo){
            hitbox.Player.Pogo();
        }
        knockback *= knockbackImpulse;
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
        GameManager.Instance.RemovePlayer(OwnerClientId);
        networkObject.Despawn();
    }
    private void Pogo(){
        if (blade.HasPogoed || blade.SwingDirection != -Vector2.up){
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
        if (0 < fireballCooldownTimeLeft){
            fireballCooldownTimeLeft -= Time.fixedDeltaTime;
            if (fireballCooldownTimeLeft < 0){
                fireballCooldownTimeLeft = 0;
                fireballCooldownBackground.SetActive(false);
            } else {
                fireballCooldownBar.localScale = new Vector3(fireballCooldownTimeLeft/fireballCooldownTime, 1, 1);
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
        if (Util.SenderIsServer(rpcParams)){
            blade.StartSwinging(CurrentDirection.y, body.flipX, isOnSolidGround);
        }
    }
    [Rpc(SendTo.NotOwner)]
    private void StartFireballCooldownRpc(RpcParams rpcParams = default){
        if (Util.SenderIsServer(rpcParams)){
            StartFireballCooldown();
        }
    }
    [Rpc(SendTo.Owner)]
    private void UpdateHealthPointsRpc(int serverHealthPoints, RpcParams rpcParams = default){
        if (!Util.SenderIsServer(rpcParams)){
            return;
        }
        healthPoints = serverHealthPoints;
        HUD.Instance.SetHeartsLeft(healthPoints);
    }
    [Rpc(SendTo.NotServer)]
    private void SetBodySpriteRpc(bool flipX, RpcParams rpcParams = default){
        if (Util.SenderIsServer(rpcParams)){
            body.flipX = flipX;
        }
    }
}
