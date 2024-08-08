using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Blade : NetworkBehaviour {
    [SerializeField] private float halfSwingAngle;
    [SerializeField] private float swingTime;

    private const float WholeRotation = 360, HalfRotation = WholeRotation/2;
    private const float FacingRightAngle = 0, FacingLeftAngle = HalfRotation;
    private float swingSpeed;
    private float endAngle = 0;
    
    
    public Player Wielder {get; set;}
    public bool IsSwinging {get; private set;}

    private void Awake(){
        swingSpeed = halfSwingAngle*2/swingTime;
    }
    
    public void StartSwinging(float yDirection, bool isFacingLeft){
        if (IsSwinging){
            return;
        }
        gameObject.SetActive(true);
        IsSwinging = true;
        // Sideways swings are always to the "Right" z-angle-wise, since the y-angle flip handles left/right.
        float directionAngle = Mathf.Rad2Deg*Mathf.Atan2(yDirection, yDirection == 0 ? Player.Right : 0);
        transform.eulerAngles = new Vector3(
            transform.eulerAngles.x, 
            isFacingLeft ? FacingLeftAngle : FacingRightAngle,
            directionAngle + halfSwingAngle
        );
        endAngle = directionAngle - halfSwingAngle;
    }
    private void Update(){
        if (!IsSwinging){
            return;
        }
        float newAngle = transform.eulerAngles.z - swingSpeed*Time.deltaTime;
        // Convert newAngle from "0 to 360"-format to "-180 to +180"-format so it can be correctly compared with endAngle.
        newAngle = (newAngle+HalfRotation)%WholeRotation - HalfRotation;
        if (newAngle < endAngle){
            gameObject.SetActive(false);
            IsSwinging = false;
            return;
        }
        Vector3 eulerAngles = transform.eulerAngles;
        eulerAngles.z = newAngle;
        transform.eulerAngles = eulerAngles;
    }
    
    private void OnTriggerEnter2D(Collider2D other){
        if (!IsServer || other.gameObject == Wielder.gameObject || !other.TryGetComponent(out Player hitPlayer)){
            return;
        }
        print($"Client {hitPlayer.OwnerClientId}'s player got hit!");
    }
}
