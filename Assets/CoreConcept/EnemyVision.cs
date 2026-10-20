using UnityEngine;

/// Attach to an empty "Enemy" parent object (scale 1,1,1).
/// - The head smoothly turns to follow the Sphere.
/// - If the Player is inside the vision cone AND nothing (the cubes) blocks
///   the line of sight, the enemy spots them and the head locks onto them instead.
public class EnemyVision : MonoBehaviour
{
    [Header("References")]
    public Transform head;          // child object that rotates
    public Transform lookTarget;    // the Sphere in the background
    public Transform player;        // the small capsule

    [Header("Head Movement")]
    public float turnSpeed = 3f;    // higher = snappier head
    public float maxHeadYaw = 90f;  // how far the head can turn left/right from forward
    public bool flatten = true;     // only rotate left/right (no looking up/down)

    [Header("Vision")]
    public float viewDistance = 40f;
    [Range(1, 180)] public float viewAngle = 70f;   // full cone angle
    public LayerMask obstacleMask;                  // put the big cubes on an "Obstacle" layer
    public LayerMask playerMask;                    // put the Player on a "Player" layer
    public Transform eyes;                          // optional; falls back to head



    public bool PlayerSpotted { get; private set; }

    public float forwardForce;

    Quaternion restRotation;

    void Start()
    {
        if (head == null) head = transform;
        if (eyes == null) eyes = head;
        restRotation = head.localRotation;
    }

    void Update()
    {
        PlayerSpotted = CanSeePlayer();

        Transform target = PlayerSpotted ? player : lookTarget;
        if (target != null) TurnHeadToward(target.position);

        if (PlayerSpotted)
            Debug.Log("Player spotted!");   // swap for game-over / alert logic

    }

    void TurnHeadToward(Vector3 worldPoint)
    {
        Vector3 dir = worldPoint - head.position;
        if (flatten) dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion targetWorld = Quaternion.LookRotation(dir.normalized, Vector3.up);

        // Clamp relative to the enemy body so the head can't spin 360 degrees
        Quaternion targetLocal = Quaternion.Inverse(transform.rotation) * targetWorld;
        targetLocal = Quaternion.RotateTowards(restRotation, targetLocal, maxHeadYaw);

        head.localRotation = Quaternion.Slerp(head.localRotation, targetLocal, turnSpeed * Time.deltaTime);
    }

    bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector3 origin = eyes.position;
        Vector3 toPlayer = player.position - origin;

        // 1. Close enough?
        if (toPlayer.magnitude > viewDistance) return false;

        // 2. Inside the vision cone? (uses the direction the head is facing)
        if (Vector3.Angle(head.forward, toPlayer) > viewAngle * 0.5f) return false;

        // 3. Anything in the way? Cast against obstacles + player only.
        //    If the first thing hit is an obstacle, the player is hiding.
        if (Physics.Raycast(origin, toPlayer.normalized, out RaycastHit hit,
                            viewDistance, obstacleMask | playerMask))
        {
            return hit.transform == player || hit.transform.IsChildOf(player);
        }
        return false;
    }

    // Draws the vision cone in the Scene view so you can tune it
    void OnDrawGizmosSelected()
    {
        Transform h = head != null ? head : transform;
        Vector3 o = (eyes != null ? eyes : h).position;
        Gizmos.color = PlayerSpotted ? Color.red : Color.yellow;

        Vector3 left = Quaternion.AngleAxis(-viewAngle * 0.5f, Vector3.up) * h.forward;
        Vector3 right = Quaternion.AngleAxis(viewAngle * 0.5f, Vector3.up) * h.forward;
        Gizmos.DrawRay(o, left * viewDistance);
        Gizmos.DrawRay(o, right * viewDistance);
        Gizmos.DrawRay(o, h.forward * viewDistance);
    }
}
