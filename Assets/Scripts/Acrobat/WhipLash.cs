using UnityEngine;

//lives on the Lash child of the whip weapon prefab
//concerned with hitbox so the base combat opens and closes the WeaponHitbox on this object through animation events but a whip shouldnt sweep around like a 7 metre sword, so every frame we keep the hitbox pointing straigh ahead from the hand (or at the lock on target) and not following the hand swing
//concerned with look so the box mesh is hidden, a LineRenderer draws a bendy whip instead, it hangs loose when idle and every time the base combat starts an attack it lashes out, snaps straight and then comes back
[RequireComponent(typeof(Collider))]
public class WhipLash : MonoBehaviour
{
    [Header("Reach")]
    [SerializeField] float length = 7f;//should match the hitbox length
    [Header("Crack timing (seconds)")]
    [SerializeField] float lashOutTime = 0.12f;//hand to full length
    [SerializeField] float snapHoldTime = 0.06f;//stays straight for a moment which is the crack
    [SerializeField] float retractTime = 0.18f;  //back to the hand
    [Header("Shape")]
    [SerializeField] int segments = 20;  //how many points the line is made of
    [SerializeField] float waveAmplitude = 0.6f;//how wavy it is while flying out
    [SerializeField] float waveCount = 1.5f; //how many waves along its length
    [SerializeField] float idleLength = 0.9f; //hanging length when not attacking
    [SerializeField] float thickness = 0.05f;
    [SerializeField] Color color = new Color(0.45f, 0.25f, 0.1f);

    CharacterCombat combat;
    AcrobatController acro;
    Transform hand;
    LineRenderer line;
    Renderer boxRenderer;
    float crackStartTime = -999f;

    float CrackTotal
    {
        get { return lashOutTime + snapHoldTime + retractTime; }
    }
    void Awake()
    {
        hand = transform.parent;
        boxRenderer = GetComponent<Renderer>();
        if (boxRenderer != null) boxRenderer.enabled = false; //the stiff box is only a hitbox now
        //own child object, an object can only have one renderer and the lash already has the hidden box renderer
        GameObject lineObject = new GameObject("WhipLine");
        lineObject.transform.SetParent(transform, false);
        line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = segments + 1;
        line.startWidth = thickness;
        line.endWidth = thickness * 0.3f;//thinner at the tip
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = color;
        line.endColor = color;
        line.numCapVertices = 2;
    }
    void OnEnable()
    {
        //the weapon is spawned under the player
        combat = GetComponentInParent<CharacterCombat>();
        acro = GetComponentInParent<AcrobatController>();
        if (combat != null) combat.OnAttack += PlayCrack;
    }
    void OnDisable()
    {
        if (combat != null) combat.OnAttack -= PlayCrack;
    }
    void PlayCrack()
    {
        crackStartTime = Time.time;
    }
    void LateUpdate()
    {
        if (hand == null) return;
        Transform owner = hand.root;
        if (combat != null) owner = combat.transform;
        Vector3 origin = hand.position;

        //aimworks like so lock on target if there is one, otherwise straight ahead so its flat
        Vector3 aim = owner.forward;
        aim.y = 0f;
        if (acro != null && acro.LockTarget != null)
        {
            aim = acro.LockTarget.position + Vector3.up * 0.8f - origin;
        }
        if (aim.sqrMagnitude < 0.0001f) aim = owner.forward;
        aim.Normalize();
        //1 is keep the hitbox on the aim line (its middle is half the length in front of the hand)
        transform.SetPositionAndRotation(origin + aim * (length * 0.5f), Quaternion.LookRotation(aim));

        //2 is draw the whip
        float t = Time.time - crackStartTime; //how long ago the attack started
        if (t >= 0f && t < CrackTotal)
        {
            DrawCrack(origin, aim, owner, t);
        }
        else
        {
            DrawIdle(origin, owner);
        }
    }
    void DrawCrack(Vector3 origin, Vector3 aim, Transform owner, float t)
    {
        //reach is how far out the tip is (0 at the hand, 1 at full length)
        //wave = how wavy the whip is (1 is very, 0 is bery straight )
        float reach;
        float wave;
        if (t < lashOutTime)
        {
            //flying out is fast at first thenslowing down near the end
            float p = t / lashOutTime;
            reach = 1f - (1f - p) * (1f - p);
            wave = 1f - p;
        }
        else if (t < lashOutTime + snapHoldTime)
        {
            //the crack must be fully straight
            reach = 1f;
            wave = 0f;
        }
        else
        {
            //coming back must be a bit limp
            float p = (t - lashOutTime - snapHoldTime) / retractTime;
            reach = Mathf.Lerp(1f, idleLength / length, p * p);
            wave = p * 0.5f;
        }
        //side = the direction 90 degrees from the aim used for the wave
        Vector3 side = Vector3.Cross(Vector3.up, aim).normalized;
        if (side.sqrMagnitude < 0.01f) side = owner.right;
        Vector3 up = Vector3.Cross(aim, side);
        float phase = t * 40f; //makes the wave travel down the whip
        for (int i = 0; i <= segments; i++)
        {
            float u = i / (float)segments;//0 at the hand, 1 at the tip
            float along = u * reach * length; //how far along the aim this point is
            //sin makes the wave, * u makes the tip wave more than the handle, * wave straightens it out
            float bend = Mathf.Sin(u * waveCount * Mathf.PI * 2f - phase) * waveAmplitude * wave * u;
            float sag = -0.4f * u * u * (1f - reach) * reach;   //slight droop mid flight
            Vector3 point = origin + aim * along + side * bend + up * (bend * 0.4f + sag);
            line.SetPosition(i, point);
        }
    }
    void DrawIdle(Vector3 origin, Transform owner)
    {
        //hangs down from the hand and trails slightly behind, swaying a little
        Vector3 back = -owner.forward;
        float sway = Mathf.Sin(Time.time * 2f) * 0.05f;
        for (int i = 0; i <= segments; i++)
        {
            float u = i / (float)segments;
            Vector3 point = origin + Vector3.down * (u * idleLength)  + back * (u * u * idleLength * 0.35f) + owner.right * (sway * u);
            line.SetPosition(i, point);
        }
    }
}
