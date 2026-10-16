
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Camera Movement")]
    public Transform target;
    public Transform lockOnTarget;

    // Offset from player to camera pivot.
    public Vector3 offset = new Vector3(0f, 2.5f, 0f);
    public Vector3 lockOffset = new Vector3(0f, 4f, 0f);

    [SerializeField] float cameraDistance = 4f;
    public float mouseSensitivity = 3f;

    [Header("Orbit")]
    [SerializeField] float minPitch = -35f;
    [SerializeField] float maxPitch = 65f;
    [SerializeField] float rotationSmoothness = 15f;
    public float followSmoothness = 10f;

    [Header("Camera Collision")]
    public LayerMask obstructionMask;
    [SerializeField] float collisionRadius = 0.25f;
    [SerializeField] float collisionPadding = 0.15f;
    [SerializeField] float distanceReturnSpeed = 5f;

    [Header("Nearby Obstacle Fade")]
    [SerializeField] float fadeDetectionRadius = 0.65f;
    public float fadeSpeed = 5f;

    [Range(0f, 1f)]
    public float fadedAlpha = 0.2f;

    float targetYaw;
    float targetPitch;

    float currentYaw;
    float currentPitch;
    float currentDistance;

    Vector3 currentPivot;

    // Each renderer stores its original colours.
    class FadeInfo
    {
        public MeshRenderer renderer;
        public Color[] originalColors;
        public float alpha = 1f;
        public bool shouldFade;
    }

    readonly Dictionary<MeshRenderer, FadeInfo> fadedRenderers = new Dictionary<MeshRenderer, FadeInfo>();

    readonly List<MeshRenderer> cleanup = new List<MeshRenderer>();

    void Start()
    {
        Vector3 angles = transform.eulerAngles;

        targetYaw = currentYaw = angles.y;

        targetPitch = currentPitch =
            Mathf.Clamp(Mathf.DeltaAngle(0f, angles.x), minPitch, maxPitch);

        currentDistance = cameraDistance;

        if (target != null)
            currentPivot = target.position + offset;

        LockCursor();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Input.GetMouseButtonDown(0) &&
            Cursor.lockState != CursorLockMode.Locked)
        {
            LockCursor();
            return;
        }

        if (Cursor.lockState == CursorLockMode.Locked && lockOnTarget == null)
        {
            HandleMouseInput();
        }
    }

    void LateUpdate()
    {
        if (target == null)
        {
            RestoreAllMaterials();
            return;
        }
        if (lockOnTarget != null)
        {
            Vector3 direction = lockOnTarget.position + Vector3.up - (target.position + lockOffset);

            float horizontalDistance = new Vector2(direction.x, direction.z).magnitude;

            if (horizontalDistance > 0.01f)
            {
                targetYaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

                targetPitch = Mathf.Clamp(Mathf.Atan2(-direction.y, horizontalDistance) * Mathf.Rad2Deg, minPitch, maxPitch);
            }
        }

        float dt = Time.deltaTime;

        // Smoothly follow the player with a pivot.
        Vector3 desiredPivot = target.position + offset;

        float followFactor = 1f - Mathf.Exp(-followSmoothness * dt);

        currentPivot = Vector3.Lerp(currentPivot, desiredPivot, followFactor);

        // Smooth yaw and pitch separately.
        float rotationFactor =1f - Mathf.Exp(-rotationSmoothness * dt);

        currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, rotationFactor);

        currentPitch = Mathf.Lerp(currentPitch, targetPitch, rotationFactor);

        Quaternion orbitRotation = Quaternion.Euler(currentPitch, currentYaw, 0f);

        // Direction pointing backwards from the pivot.
        Vector3 cameraDirection = orbitRotation * Vector3.back;

        // Determine the safe camera distance.
        float safeDistance = GetSafeCameraDistance(currentPivot, cameraDirection);

        // Move inward immediately when obstructed,
        // but return outward smoothly.
        if (safeDistance < currentDistance)
        {
            currentDistance = safeDistance;
        }
        else
        {
            float returnFactor = 1f - Mathf.Exp(-distanceReturnSpeed * dt);

            currentDistance = Mathf.Lerp(currentDistance, safeDistance, returnFactor);
        }

        // Apply orbital position and rotation.
        transform.position = currentPivot + cameraDirection * currentDistance;

        transform.rotation = orbitRotation;

        // Fade only obstacles close to the camera.
        UpdateNearbyObstacleFade(dt);
    }

    void HandleMouseInput()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        targetYaw += mouseX * mouseSensitivity;
        targetPitch -= mouseY * mouseSensitivity;

        targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);
    }

    float GetSafeCameraDistance(Vector3 pivot,Vector3 direction)
    {
        float safeDistance = cameraDistance;

        // SphereCast checks the camera's path
        // instead of checking only a thin line.
        RaycastHit[] hits = Physics.SphereCastAll(pivot, collisionRadius, direction, cameraDistance, obstructionMask, QueryTriggerInteraction.Ignore);

        foreach (RaycastHit hit in hits)
        {
            if (IsPlayerCollider(hit.collider))
                continue;

            float hitDistance = Mathf.Max(0.05f, hit.distance - collisionPadding);

            safeDistance = Mathf.Min(safeDistance, hitDistance);
        }

        return safeDistance;
    }

    void UpdateNearbyObstacleFade(float dt)
    {
        // Mark tracked obstacles as no longer near.
        foreach (FadeInfo info in fadedRenderers.Values)
            info.shouldFade = false;

        // ONLY detect objects in a small radius
        // surrounding the actual camera position.
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, fadeDetectionRadius, obstructionMask, QueryTriggerInteraction.Ignore);

        foreach (Collider col in nearbyColliders)
        {
            if (IsPlayerCollider(col))
                continue;

            MeshRenderer renderer = col.GetComponent<MeshRenderer>();

            if (renderer == null)
                renderer = col.GetComponentInParent<MeshRenderer>();

            if (renderer == null)
                continue;

            // Register newly encountered renderer.
            if (!fadedRenderers.TryGetValue(renderer, out FadeInfo info))
            {
                info = new FadeInfo();
                info.renderer = renderer;

                Material[] materials = renderer.materials;

                info.originalColors = new Color[materials.Length];

                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i].HasProperty("_Color"))
                    {
                        info.originalColors[i] = materials[i].color;
                    }
                    else
                    {
                        info.originalColors[i] = Color.white;
                    }
                }

                fadedRenderers.Add(renderer, info);
            }

            info.shouldFade = true;
        }

        cleanup.Clear();

        foreach (var pair in fadedRenderers)
        {
            FadeInfo info = pair.Value;

            if (info.renderer == null)
            {
                cleanup.Add(pair.Key);
                continue;
            }

            float desiredAlpha = info.shouldFade ? fadedAlpha : 1f;

            info.alpha = Mathf.MoveTowards( info.alpha, desiredAlpha, fadeSpeed * dt);

            Material[] materials = info.renderer.materials;

            for (int i = 0; i < materials.Length && i < info.originalColors.Length; i++)
            {
                if (!materials[i].HasProperty("_Color"))
                    continue;

                Color color = info.originalColors[i];

                color.a *= info.alpha;

                materials[i].color = color;
            }

            if (!info.shouldFade && Mathf.Approximately(info.alpha, 1f))
            {
                RestoreRenderer(info);
                cleanup.Add(pair.Key);
            }
        }

        foreach (MeshRenderer renderer in cleanup)
            fadedRenderers.Remove(renderer);
    }

    bool IsPlayerCollider(Collider col)
    {
        if (target == null || col == null)
            return false;

        return col.transform.root == target.root;
    }

    void RestoreRenderer(FadeInfo info)
    {
        if (info.renderer == null)
            return;

        Material[] materials = info.renderer.materials;

        for (int i = 0; i < materials.Length && i < info.originalColors.Length; i++)
        {
            if (materials[i].HasProperty("_Color"))
            {
                materials[i].color = info.originalColors[i];
            }
        }
    }

    void RestoreAllMaterials()
    {
        foreach (FadeInfo info in fadedRenderers.Values)
            RestoreRenderer(info);

        fadedRenderers.Clear();
    }

    void OnDisable()
    {
        RestoreAllMaterials();
    }

    void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        Gizmos.DrawWireSphere(transform.position, fadeDetectionRadius);

        if (target != null)
        {
            Gizmos.color = Color.yellow;

            Gizmos.DrawWireSphere(target.position + offset, 0.1f);
        }
    }
}
