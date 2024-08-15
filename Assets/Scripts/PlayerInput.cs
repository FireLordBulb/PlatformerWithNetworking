using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : NetworkBehaviour {
	[SerializeField] private Player player;

	private readonly Dictionary<BinaryInputAction, Action> playerActions = new();
	private Controls.MouseAndKeyboardActions actions;
	private Vector2 previousDirection;
#if UNITY_EDITOR
	private bool isHacked;
#endif
	
    private void Awake(){
	    playerActions.Add(BinaryInputAction.Jump, player.Jump);
		playerActions.Add(BinaryInputAction.StopJumping, player.StopJumping);
		playerActions.Add(BinaryInputAction.Attack, player.Attack);
		playerActions.Add(BinaryInputAction.Spell, player.CastSpell);
		
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
		InputActionPerformed(BinaryInputAction.Jump);
	}
	private void StopJumpingPerformed(InputAction.CallbackContext _){
		InputActionPerformed(BinaryInputAction.StopJumping);
	}
	private void AttackPerformed(InputAction.CallbackContext _){
		InputActionPerformed(BinaryInputAction.Attack);
	}
	private void SpellPerformed(InputAction.CallbackContext _){
		InputActionPerformed(BinaryInputAction.Spell);
	}
	private void InputActionPerformed(BinaryInputAction playerAction){
		if (!CanControlPlayer()){
			return;
		}
		playerActions[playerAction]();
		if (!IsServer){
			BinaryInputActionRpc(playerAction);
		}
	}
	public void FixedUpdate(){
	     Vector2 movementDirection = actions.Direction.ReadValue<Vector2>();
	     if (movementDirection != previousDirection && CanControlPlayer()){
		     player.SetMoveDirection(movementDirection);
		     if (!IsServer){
			     MoveRpc(movementDirection);
		     }
	     }
	     previousDirection = movementDirection;
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
		return player.IsOwner && GameManager.Instance.GameIsOngoing && !Chat.Instance.gameObject.activeSelf || isHacked;
#else
		return player.IsOwner && GameManager.Instance.GameIsOngoing && !Chat.Instance.gameObject.activeSelf;
#endif
	}
     
	[Rpc(SendTo.Server)] 
	private void BinaryInputActionRpc(BinaryInputAction inputAction, RpcParams rpcParams = default){
		if (SenderCanControlPlayer(rpcParams)){
			playerActions[inputAction]();
		}
	}
	[Rpc(SendTo.Server)]
	private void MoveRpc(Vector2 direction, RpcParams rpcParams = default){
		if (SenderCanControlPlayer(rpcParams)){ 
			player.SetMoveDirection(direction);
		}
	}
	private bool SenderCanControlPlayer(RpcParams rpcParams){
		return rpcParams.Receive.SenderClientId == player.OwnerClientId && GameManager.Instance.GameIsOngoing;
	}
	 
	private enum BinaryInputAction {
		Jump,
		StopJumping,
		Attack, 
		Spell
	}
}
