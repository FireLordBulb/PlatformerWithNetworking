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
    
    public AttackHitbox GetAttackHitbox(){
        return hitbox;
    }

    private void OnTriggerEnter2D(Collider2D other){
        // If the other is the player then player is responsible for despawning the fireball.
        if (!other.isTrigger && AttackHitbox.TryGetPlayer(other.attachedRigidbody, out Player player)){
            player.GetHit(hitbox.Collider);
            return;
        }
        // Only the server handles despawning networkObjects.
        if (!IsServer){
            return;
        }
        // Fireballs pass through each other.
        if (other.TryGetComponent<Fireball>(out _)){
            return;
        }
        StartDespawning();
    }

    public void StartDespawning(){
        isDespawning = true;
    }
    
    private void FixedUpdate(){
        transform.position += positionDelta;
        if (!isDespawning){
            return;
        }
        despawnTime -= Time.fixedDeltaTime;
        if (despawnTime < 0){
            GetComponent<NetworkObject>().Despawn();
        }
    }
    
    [Rpc(SendTo.Everyone)]
    public void StartMovingRpc(bool isMovingLeft, RpcParams rpcParams = default){
        if (!Util.SenderIsServer(rpcParams)){
            return;
        }
        IsMovingLeft = isMovingLeft;
        sprite.flipX = isMovingLeft;
        positionDelta = new Vector3((isMovingLeft ? -1 : +1)*speed*Time.fixedDeltaTime, 0);
    }
}
