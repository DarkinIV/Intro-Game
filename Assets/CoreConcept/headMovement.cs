using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class headMovement : MonoBehaviour
{
    public Rigidbody rb;
    public float forwardForce;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        rb.AddForce(-forwardForce * Time.deltaTime, 0, 0);
    }
}
