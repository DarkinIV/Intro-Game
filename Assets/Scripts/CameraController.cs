// AI use: Restructured by Claude, mainly the obstacle fading.

using System.Collections.Generic;
using UnityEngine;

// Third person camera that orbits the player with the mouse.
// When locked on it turns to keep the enemy in view instead.
// It also pulls in closer when a wall is in the way, and fades out objects that get right up close to it.
public class CameraController : MonoBehaviour
{
    [Header("Camera Movement")]
    public Transform target;
    public Transform lockOnTarget;

    [SerializeField] Vector3 offset = new Vector3(0f, 2.5f, 0f);   // from the player's feet to the point we orbit around
    [SerializeField] float cameraDistance = 5f;
    [SerializeField] float mouseSensitivity = 3f;

    [Header("Orbit")]
    [SerializeField] float minPitch = -35f;
    [SerializeField] float maxPitch = 65f;
    [SerializeField] float rotationSmoothness = 15f;
    [SerializeField] float followSmoothness = 10f;

    [Header("Lock Target")]
    [SerializeField] Vector3 lockOffset = new Vector3(0f, 3f, 0f);
    [SerializeField] float lockMinPitch = -15f;
    [SerializeField] float lockMaxPitch = 25f;
    [SerializeField] float lockRotationSmoothness = 7f;
    [SerializeField] float lockFollowSmoothness = 4f;

    [Header("Camera Collision")]
    [SerializeField] LayerMask obstacleMask;
    [SerializeField] float collisionRadius = 0.25f;
    [SerializeField] float collisionPadding = 0.15f;
    [SerializeField] float distanceReturnSpeed = 5f;

    [Header("Nearby Obstacle Fade")]
    [SerializeField] LayerMask fadeMask;
    [SerializeField] float fadeDetectionRadius = 0.65f;
    [SerializeField] float fadeSpeed = 5f;
    [Range(0f, 1f)]
    [SerializeField] float fadedAlpha = 0.2f;

    float targetYaw;
    float targetPitch;
    float currentYaw;
    float currentPitch;
    float currentDistance;
    Vector3 currentPivot;

    // An object we've faded, with its original colours so we can put them back
    class FadedObject
    {
        public Material[] materials;
        public Color[] originalColours;
        public float alpha = 1f;
        public float targetAlpha = 1f;
    }

    Dictionary<Renderer, FadedObject> fadedObjects = new Dictionary<Renderer, FadedObject>();
    List<Renderer> finishedFading = new List<Renderer>();

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        targetYaw = currentYaw = angles.y;
        targetPitch = currentPitch = Mathf.Clamp(Mathf.DeltaAngle(0f, angles.x), minPitch, maxPitch);
        currentDistance = cameraDistance;

        if (target != null)
            currentPivot = target.position + offset;

        LockCursor();
    }

    void Update()
    {
        // Escape frees the mouse, clicking locks it again
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
        {
            LockCursor();
            return;
        }

        // The mouse only moves the camera when we're not locked on
        if (Cursor.lockState == CursorLockMode.Locked && lockOnTarget == null)
        {
            targetYaw += Input.GetAxis("Mouse X") * mouseSensitivity;
            targetPitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);
        }
    }

    void LateUpdate()
    {
        if (target == null)
        {
            RestoreAll();
            return;
        }

        bool locked = lockOnTarget != null;

        Vector3 activeOffset = locked ? lockOffset : offset;
        float followSpeed = locked ? lockFollowSmoothness : followSmoothness;
        float rotationSpeed = locked ? lockRotationSmoothness : rotationSmoothness;

        Vector3 desiredPivot = target.position + activeOffset;

        // When locked on, point the camera at the enemy
        if (locked)
        {
            Vector3 toEnemy = lockOnTarget.position + Vector3.up - desiredPivot;
            float flatDistance = new Vector2(toEnemy.x, toEnemy.z).magnitude;

            if (flatDistance > 0.01f)
            {
                targetYaw = Mathf.Atan2(toEnemy.x, toEnemy.z) * Mathf.Rad2Deg;
                targetPitch = Mathf.Clamp(Mathf.Atan2(-toEnemy.y, flatDistance) * Mathf.Rad2Deg, lockMinPitch, lockMaxPitch);
            }
        }

        // Smooth follow and rotation (using Exp so it's the same at any frame rate)
        currentPivot = Vector3.Lerp(currentPivot, desiredPivot, 1f - Mathf.Exp(-followSpeed * Time.deltaTime));

        float rotationT = 1f - Mathf.Exp(-rotationSpeed * Time.deltaTime);
        currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, rotationT);
        currentPitch = Mathf.Lerp(currentPitch, targetPitch, rotationT);

        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
        Vector3 backwards = rotation * Vector3.back;

        // Jump straight in when something is in the way, but move back out slowly
        float safeDistance = GetSafeDistance(currentPivot, backwards);
        if (safeDistance < currentDistance)
            currentDistance = safeDistance;
        else
            currentDistance = Mathf.Lerp(currentDistance, safeDistance, 1f - Mathf.Exp(-distanceReturnSpeed * Time.deltaTime));

        transform.position = currentPivot + backwards * currentDistance;
        transform.rotation = rotation;

        UpdateFading();
    }

    // How far back the camera can go before hitting something
    float GetSafeDistance(Vector3 pivot, Vector3 direction)
    {
        float safeDistance = cameraDistance;

        // A sphere cast so the camera doesn't clip through the edges of walls
        foreach (RaycastHit hit in Physics.SphereCastAll(pivot, collisionRadius, direction, cameraDistance, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            if (IsPlayer(hit.collider))
                continue;

            safeDistance = Mathf.Min(safeDistance, Mathf.Max(0.05f, hit.distance - collisionPadding));
        }

        return safeDistance;
    }

    // Fades out anything right next to the camera so it doesn't block the view
    void UpdateFading()
    {
        foreach (FadedObject faded in fadedObjects.Values)
            faded.targetAlpha = 1f;

        foreach (Collider col in Physics.OverlapSphere(transform.position, fadeDetectionRadius, fadeMask, QueryTriggerInteraction.Ignore))
        {
            if (IsPlayer(col))
                continue;

            MeshRenderer rend = col.GetComponentInParent<MeshRenderer>();
            if (rend == null)
                continue;

            if (!fadedObjects.TryGetValue(rend, out FadedObject faded))
            {
                faded = StartFading(rend);
                fadedObjects.Add(rend, faded);
            }

            // The closer it is, the more see-through it gets
            float distance = Vector3.Distance(transform.position, col.ClosestPoint(transform.position));
            float closeness = Mathf.InverseLerp(fadeDetectionRadius * 0.4f, fadeDetectionRadius, distance);
            faded.targetAlpha = Mathf.Lerp(fadedAlpha, 1f, closeness);
        }

        finishedFading.Clear();

        foreach (var pair in fadedObjects)
        {
            FadedObject faded = pair.Value;

            if (pair.Key == null)
            {
                finishedFading.Add(pair.Key);
                continue;
            }

            faded.alpha = Mathf.MoveTowards(faded.alpha, faded.targetAlpha, fadeSpeed * Time.deltaTime);
            SetAlpha(faded, faded.alpha);

            // Back to fully visible and not near the camera any more
            if (faded.targetAlpha >= 1f && Mathf.Approximately(faded.alpha, 1f))
                finishedFading.Add(pair.Key);
        }

        foreach (Renderer rend in finishedFading)
            fadedObjects.Remove(rend);
    }

    FadedObject StartFading(Renderer rend)
    {
        FadedObject faded = new FadedObject();
        faded.materials = rend.materials;
        faded.originalColours = new Color[faded.materials.Length];

        for (int i = 0; i < faded.materials.Length; i++)
        {
            if (faded.materials[i].HasProperty("_Color"))
                faded.originalColours[i] = faded.materials[i].color;
        }

        return faded;
    }

    void SetAlpha(FadedObject faded, float alpha)
    {
        for (int i = 0; i < faded.materials.Length; i++)
        {
            if (!faded.materials[i].HasProperty("_Color"))
                continue;

            Color colour = faded.originalColours[i];
            colour.a *= alpha;
            faded.materials[i].color = colour;
        }
    }

    void RestoreAll()
    {
        foreach (var pair in fadedObjects)
        {
            if (pair.Key != null)
                SetAlpha(pair.Value, 1f);
        }

        fadedObjects.Clear();
    }

    bool IsPlayer(Collider col)
    {
        return target != null && col.transform.root == target.root;
    }

    void OnDisable()
    {
        RestoreAll();
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
