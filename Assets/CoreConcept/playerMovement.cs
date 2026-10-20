using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playerMovement : MonoBehaviour
{
    public Rigidbody rb;

    public float forwardForce = 2000f;
    public float horizontalForce = 500f;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Debug.Log("Hello world");
        //rb.useGravity = false;
        //rb.AddForce(0, 200, 500);
    }

    // Update is called once per frame
    //the frame rate will affect the force based on how good the computer is 
    void FixedUpdate()
    {
        //rb.AddForce(0, 0, 200 * Time.deltaTime);
        //the better the computer performance the lower the deltaTime so that it can make it fair 
        //if after doing this the obj doesn't move then try and make the values bigger
        rb.AddForce(-forwardForce * Time.deltaTime, 0, 0);

        //because we are using the physics components we need to use the FixedUpdate instead of Update as itar makes it smoother and better


        //we can have the player input aswell

        //float hor = Input.GetAxis("Horizontal");

        //rb.AddForce(hor * horizontalForce * Time.fixedDeltaTime, 0, 0, ForceMode.VelocityChange);


    }
}