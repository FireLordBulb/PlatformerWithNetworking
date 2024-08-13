using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Fireball : NetworkBehaviour {
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private AttackHitbox hitbox;
    [SerializeField] private float speed;
    [SerializeField] private float despawnTime;

    private Vector3 positionDelta;
    private bool isDespawning;
    
    public bool IsMovingLeft {get; private set;}
    
    private void OnTriggerEnter2D(Collider2D other){
        if (!other.isTrigger && AttackHitbox.TryGetPlayer(other.attachedRigidbody, out Player player)){
            if (player == hitbox.Player){
                return;
            }
            player.GetHit(hitbox, Util.ToDirection(IsMovingLeft));
        }
        // Only the server handles despawning networkObjects.
        if (!IsServer){
            return;
        }
        // Fireballs pass through each other.
        if (other.TryGetComponent<Fireball>(out _)){
            return;
        }
        isDespawning = true;
    }
    
    private void FixedUpdate(){
        transform.position += positionDelta;
        if (!isDespawning){
            return;
        }
        despawnTime -= Time.fixedDeltaTime;
        if (despawnTime < 0){
            // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
            GetComponent<NetworkObject>().Despawn();
        }
    }
    
    public void SetCaster(Player player){
        hitbox.Player = player;
    }
    
    [Rpc(SendTo.Everyone)]
    public void StartMovingRpc(bool isMovingLeft, RpcParams rpcParams = default){
        if (!Util.SenderIsServer(rpcParams)){
            return;
        }
        IsMovingLeft = isMovingLeft;
        sprite.flipX = isMovingLeft;
        positionDelta = speed*Time.fixedDeltaTime * Util.ToDirection(isMovingLeft);
    }
}
