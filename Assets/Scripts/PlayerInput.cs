using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : NetworkBehaviour {
	[SerializeField] private Player player;
	private Controls.MouseAndKeyboardActions actions;
#if UNITY_EDITOR
	private bool isHacked;
#endif
	private void Awake(){
		Controls controls = new();
		controls.Enable();
		actions = controls.MouseAndKeyboard;
		actions.Jump.performed += JumpPerformed;
		actions.Jump.canceled += StopJumpingPerformed;
		actions.Attack.performed += AttackPerformed;
		actions.Spell.performed += SpellPerformed;
	}
	private void OnDisable(){
		actions.Jump.performed -= JumpPerformed;
		actions.Jump.canceled -= StopJumpingPerformed;
		actions.Attack.performed -= AttackPerformed;
		actions.Spell.performed -= SpellPerformed;
	}
	private void JumpPerformed(InputAction.CallbackContext _){
		if (!CanControlPlayer()){
			return;
		}
		player.Jump();
		if (!IsServer){
			JumpRpc();
		}
	}
	private void StopJumpingPerformed(InputAction.CallbackContext _){
		if (!CanControlPlayer()){
			return;
		}
		player.StopJumping();
		if (!IsServer){
			StopJumpingRpc();
		}
	}
	private void AttackPerformed(InputAction.CallbackContext _){
		if (!CanControlPlayer()){
			return;
		}
		player.Attack();
		if (!IsServer){
			AttackRpc();
		}
	}
	private void SpellPerformed(InputAction.CallbackContext _){
		if (!CanControlPlayer()){
			return;
		}
		player.CastSpell();
		if (!IsServer){
			SpellRpc();
		}
	}
	
	public void FixedUpdate(){
	     Vector2 movementDirection = actions.Direction.ReadValue<Vector2>();
#if UNITY_EDITOR
	     if (isHacked){
		     //movementDirection *= 5;
	     }
#endif
	     if (CanControlPlayer()){
		     player.Move(movementDirection);
		     if (!IsServer){
			     MoveRpc(movementDirection);
		     }
	     }
#if UNITY_EDITOR
	     if (Input.GetKey(KeyCode.Y)){
		     print("Debug hacking on!");
		     isHacked = true;
	     } else if (Input.GetKey(KeyCode.U)){
		     print("Debug hacking off!");
		     isHacked = false;
	     }
#endif
     }
     private bool CanControlPlayer(){
#if UNITY_EDITOR
	     return player.IsOwner || isHacked;
#else	  
		return player.IsOwner;
#endif
     }
     
     [Rpc(SendTo.Server)]
     private void JumpRpc(RpcParams rpcParams = default){
	     if (IsPlayerOwnedBySender(rpcParams)){
		     player.Jump();
	     }
     }
     [Rpc(SendTo.Server)]
     private void StopJumpingRpc(RpcParams rpcParams = default){
	     if (IsPlayerOwnedBySender(rpcParams)){
		     player.StopJumping();
	     }
     }
     [Rpc(SendTo.Server)]
     private void AttackRpc(RpcParams rpcParams = default){
	     if (IsPlayerOwnedBySender(rpcParams)){
		     player.Attack();
	     }
     }
     [Rpc(SendTo.Server)]
     private void SpellRpc(RpcParams rpcParams = default){
	     if (IsPlayerOwnedBySender(rpcParams)){
		     player.CastSpell();
	     }
     }
     [Rpc(SendTo.Server)]
     private void MoveRpc(Vector2 direction, RpcParams rpcParams = default){
		 if (IsPlayerOwnedBySender(rpcParams)){ 
			 player.Move(direction);
	     }
     }
     private bool IsPlayerOwnedBySender(RpcParams rpcParams){
	     return rpcParams.Receive.SenderClientId == player.OwnerClientId;
     }
}
