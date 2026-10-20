using UnityEngine;

/// Moves an object back and forth between its start position
/// and start position + direction * distance.
public class TargetMovement : MonoBehaviour
{
    public float force; // how far it travels
    public Rigidbody rb; 

    void Start()
    {
        rb.AddForce(-force * Time.deltaTime, 0, 0);
    }

    void Update()
    {
        if (rb.position.x < 33.83967)
        {
            rb.AddForce(force * Time.deltaTime, 0, 0);

        }
        else
        {
            rb.AddForce(-force * Time.deltaTime, 0, 0);

        }
    }
}
