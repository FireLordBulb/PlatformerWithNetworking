using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BladeHitbox : AttackHitbox {
    public Blade Blade {get; set;}
    
    private void OnTriggerEnter2D(Collider2D other){
        if (other.isTrigger){
            // TODO sword on sword clank
            return;
        }
        if (TryGetPlayer(other.attachedRigidbody, out Player player)){
            player.GetHit(this, Blade.SwingDirection, true);
        }
    }
}
