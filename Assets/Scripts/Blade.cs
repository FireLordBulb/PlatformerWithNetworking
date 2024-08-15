using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Blade : MonoBehaviour {
    [SerializeField] private float halfSwingAngle;
    [SerializeField] private float swingTime;
    [SerializeField] private BladeHitbox[] hitboxes;

    private const float WholeRotation = 360, HalfRotation = WholeRotation/2;
    private const float FacingRightAngle = 0, FacingLeftAngle = HalfRotation;

    private Player player;
    private float swingSpeed;
    private float endAngle;
    
    public Vector2 SwingDirection {get; private set;}
    public bool IsSwinging {get; private set;}
    public bool HasPogoed {get; set;}

    private void Awake(){
        swingSpeed = halfSwingAngle*2/swingTime;
    }

    public void SetWielder(Player wielder){
        player = wielder;
        foreach (BladeHitbox hitbox in hitboxes){
            hitbox.Player = wielder;
            hitbox.Blade = this;
        }
    }
    
    public void StartSwinging(float yDirection, bool isFacingLeft, bool isOnSolidGround){
        if (IsSwinging){
            return;
        }
        SetSwinging(true); 
        HasPogoed = false;
        // Can't swing downwards when on solid ground.
        if (isOnSolidGround){
            yDirection = Mathf.Max(yDirection, 0);
        }
        // Sideways swings are always to the "Right" z-angle-wise, since the y-angle flip handles left/right.
        float xDirection = yDirection == 0 ? Util.Right : 0;
        float directionAngle = Mathf.Rad2Deg*Mathf.Atan2(yDirection, xDirection);
        SwingDirection = new Vector2(xDirection*Util.ToDirectionSign(isFacingLeft), yDirection);
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
            SetSwinging(false);
            return;
        }
        Vector3 eulerAngles = transform.eulerAngles;
        eulerAngles.z = newAngle;
        transform.eulerAngles = eulerAngles;
    }

    public void SetSwinging(bool isSwinging){
        gameObject.SetActive(isSwinging);
        IsSwinging = isSwinging;
        // If you're holding a move direction when swinging ends, you should start to face that direction.
        if (!isSwinging){
            player.ReapplyMoveDirection();
        }
    }
}
