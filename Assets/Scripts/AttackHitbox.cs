using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackHitbox : MonoBehaviour {
    public Player Player {get; set;}
    
    public static bool TryGetPlayer(Rigidbody2D rigidbody2D, out Player player){
        player = null;
        return rigidbody2D != null && rigidbody2D.TryGetComponent(out player);
    }
}
