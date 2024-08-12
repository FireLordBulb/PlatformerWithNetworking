using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Fireball : NetworkBehaviour {
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private AttackHitbox hitbox;

    public bool IsMovingLeft {get; private set;}
    
    public AttackHitbox GetAttackHitbox(){
        return hitbox;
    }
    
    [Rpc(SendTo.Everyone)]
    public void StartMovingRpc(bool isMovingLeft, RpcParams rpcParams = default){
        if (rpcParams.Receive.SenderClientId != NetworkManager.ServerClientId){
            return;
        }
        IsMovingLeft = isMovingLeft;
        sprite.flipX = isMovingLeft;
    }
}
