using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    //adjustable in inspector 
    [Header("Target")]
    [SerializeField] private Transform target; //player to follow
    //transform bcz we need players pos and rotation
    [SerializeField] private float focusHeight = 1f; // lloook at point this far

    [Header("Orbit")]
    [SerializeField] private float distance = 8f; //length of invisible stick
    [SerializeField] private float mouseSensitivity = 3f; //how fast mouse reacts to turns 
    [SerializeField] private float minPitch = -20f; // how far the cam can tilt up
    [SerializeField] private float maxPitch = 60f; //how far can the cam tilt down 

    [Header("Smoothing")]
    [SerializeField] private float followSmoothing = 15f; //follow stricter

    [Header("Lock On")]
    [SerializeField] private float lockOnTurnSpeed = 8f; //how quickly the cam swings round to the locked target
    [SerializeField] private float lockOnPitchOffset = 10f; //extra downward tilt while locked so we see over the player

    //the player lock on script sets this, null = normal mouse control
    public LockOnTarget LockTarget { get; set; }

    //cams memory
    private float yaw; //left and right angle around player
    private float pitch = 15f; //up and down angle 
    private Vector3 focusPoint; //the point cam looks at 

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked; //mouse kept in game window
        Cursor.visible = false; //hide mouse pointer 
        yaw = target.eulerAngles.y; // start behind player 
        //eulerAngles gives the rotation as normal angles, copies players left/right facing into yaw
        focusPoint = target.position + Vector3.up * focusHeight; 
    }

    // Update is called once per frame
    void LateUpdate()
    {
        Vector3 desiredFocus = target.position + Vector3.up * focusHeight; //where the focus point should be rn
        focusPoint = Vector3.Lerp(focusPoint, desiredFocus, followSmoothing * Time.deltaTime);
        //found this online ask teacher as far as i know its closes about a quarter of the gap from a to b so that the focus point catches up quickly

        if (LockTarget != null)
        {
            LookAtLockTarget(); //locked on, the target decides where the cam points
        }
        else
        {
            yaw += Input.GetAxis("Mouse X") * mouseSensitivity; //how far left or right the mpuse has moved
            pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;//up and down, negative bcz positive pitch in unity means going down
        }
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f); //make rotation out of thw angles 
        transform.position = focusPoint - rotation * Vector3.forward * distance;
        //rotation times vec3 forward = whcih way is forward after turnning by this rotation, look there
        //dist make that 8 long follow arrow length
        //start at focus point and go back the arrows length 
        transform.rotation = rotation;
    }

    private void LookAtLockTarget()
    {
        Vector3 toTarget = LockTarget.LockPoint - focusPoint; //arrow from the player's focus point to the target
        float flatDistance = new Vector2(toTarget.x, toTarget.z).magnitude; //distance along the floor only

        //if the target is basically on top of us the direction is meaningless and the cam would spin, so leave it
        if (flatDistance < 0.5f) return;

        //Atan2 turns an arrow into an angle, Rad2Deg turns radians into normal degrees
        float targetYaw = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg; //left/right angle to face the target
        //minus bcz positive pitch looks DOWN in unity, target below us = look down
        float targetPitch = -Mathf.Atan2(toTarget.y, flatDistance) * Mathf.Rad2Deg + lockOnPitchOffset;

        //LerpAngle is Lerp for angles, it knows 350 and 10 are only 20 apart and goes the short way round
        yaw = Mathf.LerpAngle(yaw, targetYaw, lockOnTurnSpeed * Time.deltaTime);
        pitch = Mathf.Lerp(pitch, targetPitch, lockOnTurnSpeed * Time.deltaTime);
    }

}
