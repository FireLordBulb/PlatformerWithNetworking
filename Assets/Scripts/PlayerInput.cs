using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : MonoBehaviour {
    [SerializeField] private Player player;
    private Controls.MouseAndKeyboardActions actions;
    // Start is called before the first frame update
     public void Awake(){
	     Controls controls = new();
	     controls.Enable();
	     actions = controls.MouseAndKeyboard;
	     actions.Jump.performed += context => {
		     player.Jump();
	     };
	     actions.Attack.performed += context => {
	         player.Attack();
         };
     }
     public void FixedUpdate(){
         player.Move(actions.Direction.ReadValue<Vector2>());
     }
}
