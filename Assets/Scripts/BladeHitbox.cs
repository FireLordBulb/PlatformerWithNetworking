using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BladeHitbox : AttackHitbox {
    public Blade Blade {get; set;}
    
    private void OnTriggerEnter2D(Collider2D other){
        bool otherIsPlayer = TryGetPlayer(other.attachedRigidbody, out Player otherPlayer);
        if (other.isTrigger){
            if (other.TryGetComponent<BladeHitbox>(out _)){
                Player.Parry();
                otherPlayer.Parry();
            }
            return;
        }
        if (otherIsPlayer){
            otherPlayer.GetHit(this, Blade.SwingDirection, true);
        }
    }
}
