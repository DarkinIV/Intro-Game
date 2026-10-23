using UnityEngine;

// Goes on the Lash part of the whip prefab. It does two things:
// keeps the hitbox pointing straight ahead (a whip shouldn't swing round like a sword),
// and moves the points of the LineRenderer so the whip hangs, cracks and stretches.
// The look of the line (width, colour, material, number of points) is set on the LineRenderer.
[RequireComponent(typeof(Collider), typeof(LineRenderer))]
public class WhipLash : MonoBehaviour
{
    [SerializeField] Transform lineOrigin;        // where the whip comes out of the handle

    [Header("Reach")]
    [SerializeField] float length = 7f;           // should be the same as the hitbox length

    [Header("Crack timing (seconds)")]
    [SerializeField] float lashOutTime = 0.12f;
    [SerializeField] float snapHoldTime = 0.06f;
    [SerializeField] float retractTime = 0.18f;

    [Header("Shape")]
    [SerializeField] float waveAmplitude = 0.6f;
    [SerializeField] float waveCount = 1.5f;
    [SerializeField] float idleLength = 0.9f;

    CharacterCombat combat;
    PlayerController player;
    LineRenderer line;
    int segments;
    float crackStartTime = -999f;
    Transform pullTarget;
    float pullUntil;

    float CrackTotal => lashOutTime + snapHoldTime + retractTime;

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = true;
        segments = line.positionCount - 1;
    }

    // The whip is also shown on its own on the selection screen, so there might not be a character holding it
    void OnEnable()
    {
        combat = GetComponentInParent<CharacterCombat>();
        player = GetComponentInParent<PlayerController>();

        // Crack when the hitbox opens, not when the button is pressed
        if (combat != null)
            combat.OnStrike += PlayCrack;
    }

    void OnDisable()
    {
        if (combat != null)
            combat.OnStrike -= PlayCrack;
    }

    void PlayCrack()
    {
        crackStartTime = Time.time;
    }

    public void ShowPull(Transform target, float duration)
    {
        pullTarget = target;
        pullUntil = Time.time + duration;
    }

    void LateUpdate()
    {
        Transform owner = combat != null ? combat.transform : transform.root;
        Vector3 origin = lineOrigin.position;

        // Aim at the locked on enemy, or straight ahead
        Vector3 aim = owner.forward;
        aim.y = 0f;
        if (player != null && player.LockTarget != null)
            aim = player.LockTarget.position + Vector3.up * 0.8f - origin;
        aim.Normalize();

        // Keep the hitbox lined up with where we're aiming
        transform.SetPositionAndRotation(origin + aim * (length * 0.5f), Quaternion.LookRotation(aim));

        float timeSinceCrack = Time.time - crackStartTime;

        if (pullTarget != null && Time.time < pullUntil)
            DrawPull(origin);
        else if (timeSinceCrack >= 0f && timeSinceCrack < CrackTotal)
            DrawCrack(origin, aim, timeSinceCrack);
        else
            DrawIdle(origin, owner);
    }

    // A straight line out to whatever we're pulling
    void DrawPull(Vector3 origin)
    {
        Vector3 end = pullTarget.position + Vector3.up * 0.8f;

        for (int i = 0; i <= segments; i++)
            line.SetPosition(i, Vector3.Lerp(origin, end, i / (float)segments));
    }

    void DrawCrack(Vector3 origin, Vector3 aim, float t)
    {
        // reach is how far out the tip is (0 = at the hand, 1 = full length)
        // wave is how wavy the whip is (1 = very wavy, 0 = straight)
        float reach;
        float wave;

        if (t < lashOutTime)
        {
            // Flying out. Fast at the start and slowing down at the end.
            float p = t / lashOutTime;
            reach = 1f - (1f - p) * (1f - p);
            wave = 1f - p;
        }
        else if (t < lashOutTime + snapHoldTime)
        {
            // The crack itself, completely straight
            reach = 1f;
            wave = 0f;
        }
        else
        {
            // Coming back in, a bit floppy
            float p = (t - lashOutTime - snapHoldTime) / retractTime;
            reach = Mathf.Lerp(1f, idleLength / length, p * p);
            wave = p * 0.5f;
        }

        Vector3 side = Vector3.Cross(Vector3.up, aim).normalized;
        Vector3 up = Vector3.Cross(aim, side);

        // Moving the sine wave along over time makes it look like it travels down the whip
        float phase = t * 40f;

        for (int i = 0; i <= segments; i++)
        {
            float u = i / (float)segments;   // 0 at the hand, 1 at the tip
            float along = u * reach * length;

            float bend = Mathf.Sin(u * waveCount * Mathf.PI * 2f - phase) * waveAmplitude * wave * u;
            float sag = -0.4f * u * u * (1f - reach) * reach;   // droops a little while flying out

            line.SetPosition(i, origin + aim * along + side * bend + up * (bend * 0.4f + sag));
        }
    }

    // Hangs down from the hand and sways a little
    void DrawIdle(Vector3 origin, Transform owner)
    {
        Vector3 back = -owner.forward;
        float sway = Mathf.Sin(Time.time * 2f) * 0.05f;

        for (int i = 0; i <= segments; i++)
        {
            float u = i / (float)segments;
            line.SetPosition(i, origin + Vector3.down * (u * idleLength) + back * (u * u * idleLength * 0.35f) + owner.right * (sway * u));
        }
    }
}
