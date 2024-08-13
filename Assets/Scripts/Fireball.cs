using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Fireball : NetworkBehaviour {
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private AttackHitbox hitbox;
    [SerializeField] private float speed;

    private Vector3 positionDelta;
    public bool IsMovingLeft {get; private set;}
    
    public AttackHitbox GetAttackHitbox(){
        return hitbox;
    }
    private void FixedUpdate(){
        transform.position += positionDelta;
    }
    
    [Rpc(SendTo.Everyone)]
    public void StartMovingRpc(bool isMovingLeft, RpcParams rpcParams = default){
        if (rpcParams.Receive.SenderClientId != NetworkManager.ServerClientId){
            return;
        }
        IsMovingLeft = isMovingLeft;
        sprite.flipX = isMovingLeft;
        positionDelta = new Vector3((isMovingLeft ? -1 : +1)*speed*Time.fixedDeltaTime, 0);
    }
}
