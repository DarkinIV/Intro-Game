using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float sprintMultiplier = 1.5f;
    [SerializeField] float sprintStaminaPerSecond = 15f;
    [SerializeField] bool isSprinting;

    [Header("Dash")]
    [SerializeField] float dashDistance = 4f;
    [SerializeField] float dashDuration = 0.15f;
    [SerializeField] float dashCooldown = 0.4f;
    [SerializeField] float dashStaminaCost = 20f;
    [Range(0, 1)][SerializeField] float iframeStartPercent = 0.0f; // When i-frames begin (0 = start)
    [Range(0, 1)][SerializeField] float iframeEndPercent = 0.8f;   // When i-frames end (1 = end)

    [Header("Combat")]
    [SerializeField] float lockOnRange = 12f;
    Transform lockedTarget;

    CharacterController controller;
    PlayerStats stats;
    PlayerCombat combat;
    PlayerInteract interactor;
    Camera cam;
    CameraController cameraController;

    float verticalVelocity;
    float nextDashTime;
    Vector3 moveDirection;
    Vector3 lastMoveDirection = Vector3.forward;

    public bool IsDashing { get; private set; }
    public bool IsInvulnerable { get; private set; }
    public float DashCooldownRemaining => Mathf.Max(0, nextDashTime - Time.time);
    public float DashCooldownDuration => dashCooldown;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        stats = GetComponent<PlayerStats>();
        combat = GetComponent<PlayerCombat>();
        interactor = GetComponent<PlayerInteract>();
        cam = Camera.main;
        cameraController = cam.GetComponent<CameraController>();
    }

    void Update()
    {
        if (!IsDashing)
            Move();

        if (Input.GetKeyDown(KeyCode.Space))
            Dash();

        if (Input.GetKeyDown(KeyCode.Q))
            ToggleLockOn();
        if (lockedTarget != null && (lockedTarget.GetComponentInParent<CharacterStats>().IsDead || Vector3.Distance(transform.position, lockedTarget.position) > lockOnRange))
        {
            lockedTarget = null;
        }
        if (cameraController != null)
            cameraController.lockOnTarget = lockedTarget;

        if (Input.GetMouseButtonDown(0) && !IsDashing)
            combat.TryAttack();

        if (Input.GetKeyDown(KeyCode.E) && !IsDashing)
            interactor.TryInteract();
    }

    void Move()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 forward = cam.transform.forward;
        Vector3 right = cam.transform.right;
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        moveDirection = forward * vertical + right * horizontal;
        if (moveDirection.magnitude > 1)
            moveDirection.Normalize();

        float speed = stats.GetMoveSpeed();

        // --- SPRINTING LOGIC ---
        // Start sprinting only on fresh key down
        if (Input.GetKeyDown(KeyCode.LeftShift) && moveDirection.magnitude > 0.1f)
        {
            isSprinting = true;
        }

        // Stop sprinting if key released or player stops moving
        if (Input.GetKeyUp(KeyCode.LeftShift) || moveDirection.magnitude <= 0.1f)
        {
            isSprinting = false;
        }

        if (isSprinting)
        {
            float staminaCost = sprintStaminaPerSecond * Time.deltaTime;

            // If stamina hits 0, break out of sprinting completely
            if (stats.TrySpendStamina(staminaCost, false))
            {
                speed *= sprintMultiplier;
            }
            else
            {
                isSprinting = false;
            }
        }

        // Face locked enemy unless sprinting
        if (lockedTarget != null && !isSprinting)
        {
            Vector3 direction = lockedTarget.position - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion rotation = Quaternion.LookRotation(direction);

                transform.rotation = Quaternion.Slerp(transform.rotation, rotation, stats.turnSpeed.Value * Time.deltaTime);
            }
        }
        else if (moveDirection.magnitude > 0.1f)
        {
            lastMoveDirection = moveDirection;

            Quaternion rotation = Quaternion.LookRotation(moveDirection);

            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, stats.turnSpeed.Value * Time.deltaTime);
        }

        // Gravity
        if (controller.isGrounded)
            verticalVelocity = -2f;
        else
            verticalVelocity += -20f * Time.deltaTime;

        Vector3 movement = moveDirection * speed;
        movement.y = verticalVelocity;
        controller.Move(movement * Time.deltaTime);
    }

    void Dash()
    {
        if (IsDashing || Time.time < nextDashTime) return;
        if (!stats.TrySpendStamina(dashStaminaCost)) return;

        // Force dash to move either in current raw movement direction or last recorded direction
        Vector3 direction = moveDirection.magnitude > 0.1f ? moveDirection : lastMoveDirection;
        direction.y = 0;
        direction.Normalize();

        // Ensure character immediately snaps to face dash target direction
        transform.rotation = Quaternion.LookRotation(direction);

        nextDashTime = Time.time + dashCooldown;
        StartCoroutine(DashMovement(direction));
    }

    IEnumerator DashMovement(Vector3 direction)
    {
        IsDashing = true;
        float speed = dashDistance / dashDuration;
        float elapsed = 0f;

        // GetComponent<Animator>().SetTrigger("Dash");

        while (elapsed < dashDuration)
        {
            float currentPercent = elapsed / dashDuration;

            // Process dynamic frame boundaries for invincibility flags
            IsInvulnerable = (currentPercent >= iframeStartPercent && currentPercent <= iframeEndPercent);

            // Re-apply standard downward gravity modifications mid-dash sequence 
            if (controller.isGrounded) verticalVelocity = -2f;
            else verticalVelocity += -20f * Time.deltaTime;

            Vector3 movement = direction * speed;
            movement.y = verticalVelocity;

            controller.Move(movement * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        IsInvulnerable = false;
        IsDashing = false;
    }

    void ToggleLockOn()
    {
        if (lockedTarget != null)
        {
            lockedTarget = null;
            return;
        }

        float bestDistance = Mathf.Infinity;

        foreach (EnemyCombat enemy in FindObjectsByType<EnemyCombat>(FindObjectsSortMode.None))
        {
            if (enemy.GetComponent<CharacterStats>().IsDead)
                continue;

            Vector3 screenPos = cam.WorldToViewportPoint(enemy.transform.position + Vector3.up);

            // Enemy must be in front of the camera
            if (screenPos.z <= 0)
                continue;

            // Distance from centre of screen
            float distance = Vector2.Distance(new Vector2(screenPos.x, screenPos.y), new Vector2(0.5f, 0.5f));

            if (distance < bestDistance && Vector3.Distance(transform.position, enemy.transform.position) <= lockOnRange)
            {
                bestDistance = distance;
                lockedTarget = enemy.lockOnTransform;
            }
        }
    }
}
