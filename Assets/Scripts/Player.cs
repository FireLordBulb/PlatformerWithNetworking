using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

public class Player : NetworkBehaviour {
    [Header("Components on the prefab")]
    [SerializeField] private NetworkObject networkObject;
    [SerializeField] private Rigidbody2D rigidBody;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Collider2D legCollider;
    [SerializeField] private Transform fireballSpawn;
    [SerializeField] private GameObject fireballCooldownBackground;
    [SerializeField] private Transform fireballCooldownBar;
    [SerializeField] private Blade blade;
    [Header("Config values")]
    [SerializeField] private Fireball fireballPrefab;
    [SerializeField] private Sprite localSprite;
    [SerializeField] private float fireballCooldownTime;
    [SerializeField] private float invisTime;
    [SerializeField] private float runningSpeed;
    [SerializeField] private float jumpStartImpulse;
    [SerializeField] private float jumpHoldForce;
    [SerializeField] private float jumpHoldTime;
    [SerializeField] private float hitKnockbackImpulse;
    [SerializeField] private float parryKnockbackImpulse;
    [SerializeField] private float sideUpwardsKnockbackImpulse;
    [SerializeField] private float pogoImpulse;
    [SerializeField] private int maxHealth;
    
    // Constants.
    private const float MaxGroundDistance = 0.02f;
    
    // Mutable state.
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
    private void Awake(){
        blade.SetWielder(this);
        healthPoints = maxHealth;
    }
    private void Start(){
        if (IsOwner){
            spriteRenderer.sprite = localSprite;
        }
    } 
    
    public void ReapplyMoveDirection(){
        SetMoveDirection(CurrentDirection);
    }
    public void SetMoveDirection(Vector2 direction){
        int xDirection = (int)direction.x;
        int yDirection = (int)direction.y;
        switch(xDirection){
            case Util.Right:
                SetSpriteRotation(Util.FacingRightAngle);
                break;
            case Util.Left:
                SetSpriteRotation(Util.FacingLeftAngle);
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
    private void SetSpriteRotation(float rotation){
        if (blade.IsSwinging){
            return;
        }
        transform.eulerAngles = new Vector3(0, rotation, 0);
        // The cooldown bar doesn't flip when the player does. It always stays the same.
        fireballCooldownBackground.transform.rotation = Quaternion.identity;
        
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
        blade.StartSwinging(CurrentDirection.y, isOnSolidGround);
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
        newFireball.StartMovingRpc(transform.eulerAngles.y);
    }
    private void StartFireballCooldown(){
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
        knockback *= hitKnockbackImpulse;
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
        GameManager.Instance.RemovePlayer(OwnerClientId);
        networkObject.Despawn();
    }
    public void Parry(){
        AddYCancelingImpulse(-parryKnockbackImpulse*blade.SwingDirection);
        Pogo();
        blade.SetSwinging(false);
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
    
    [Rpc(SendTo.NotOwner)]
    private void StartSwingingRpc(RpcParams rpcParams = default){
        if (Util.SenderIsServer(rpcParams)){
            blade.StartSwinging(CurrentDirection.y, isOnSolidGround);
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
}
