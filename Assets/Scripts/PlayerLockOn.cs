using UnityEngine;

//elden ring style lock on, goes on the player
//middle click (press the scroll wheel) = lock on / let go, scroll up/down = switch target right/left
public class PlayerLockOn : MonoBehaviour
{
    [Header("Lock On")]
    [SerializeField] private float maxLockDistance = 20f; //cant lock onto things further than this
    [SerializeField] private float breakDistance = 25f; //lock lets go if the target gets this far away
    [SerializeField] private LayerMask sightBlockers = ~0; //what can block the view of a target (walls etc)

    [Header("Reference")]
    [SerializeField] private ThirdPersonCamera cameraRig; //left empty = finds the main camera by itself

    [Header("Lock Icon")]
    [SerializeField] private float iconSize = 12f; //size of the red dot in pixels
    [SerializeField] private Color iconColor = Color.red;

    //other scripts (movement, camera) read this to know what we are locked onto, null = not locked
    public LockOnTarget CurrentTarget { get; private set; }

    private Camera cam;

    private void Awake()
    {
        if (cameraRig == null && Camera.main != null)
        {
            cameraRig = Camera.main.GetComponent<ThirdPersonCamera>(); //Camera.main = the camera tagged MainCamera
        }
        if (cameraRig != null)
        {
            cam = cameraRig.GetComponent<Camera>();
        }
    }

    void Update()
    {
        if (cam == null) return; //no camera, cant work out whats on screen

        //2 = middle mouse button (pressing the scroll wheel in)
        if (Input.GetMouseButtonDown(2))
        {
            if (CurrentTarget != null) SetTarget(null); //already locked = let go
            else SetTarget(FindBestTarget()); //not locked = lock onto the best one (or nothing if none found)
        }

        if (CurrentTarget == null) return;

        //let go if the target died, got switched off or ran too far away
        float distance = Vector3.Distance(transform.position, CurrentTarget.LockPoint);
        if (!CurrentTarget.CanBeTargeted || distance > breakDistance)
        {
            SetTarget(null);
            return;
        }

        //scroll wheel: up = positive, down = negative, 0 when not scrolling
        float scroll = Input.mouseScrollDelta.y;
        if (scroll > 0f) SwitchTarget(1); //up = next target to the right
        else if (scroll < 0f) SwitchTarget(-1); //down = next target to the left
    }

    private void SetTarget(LockOnTarget target)
    {
        CurrentTarget = target;
        cameraRig.LockTarget = target; //tell the camera what to look at (null = back to mouse control)
    }

    //picks the target closest to the middle of the screen, like elden ring
    private LockOnTarget FindBestTarget()
    {
        LockOnTarget best = null;
        float bestScore = float.MaxValue; //start huge so the first valid target always wins

        foreach (LockOnTarget target in LockOnTarget.All)
        {
            if (!IsValidTarget(target, out Vector3 viewportPos)) continue;

            //viewport = the screen as 0 to 1, so (0.5, 0.5) is the exact middle
            float fromMiddle = Vector2.Distance(new Vector2(viewportPos.x, viewportPos.y), new Vector2(0.5f, 0.5f));
            if (fromMiddle < bestScore)
            {
                bestScore = fromMiddle;
                best = target;
            }
        }
        return best;
    }

    //direction 1 = right, -1 = left. finds the nearest target on that side of the current one on screen
    private void SwitchTarget(int direction)
    {
        float currentX = cam.WorldToViewportPoint(CurrentTarget.LockPoint).x;
        LockOnTarget best = null;
        float bestGap = float.MaxValue;

        foreach (LockOnTarget target in LockOnTarget.All)
        {
            if (target == CurrentTarget || !IsValidTarget(target, out Vector3 viewportPos)) continue;

            float gap = (viewportPos.x - currentX) * direction; //positive = on the side we want
            if (gap > 0f && gap < bestGap)
            {
                bestGap = gap;
                best = target;
            }
        }

        if (best != null) SetTarget(best); //nothing on that side = keep the current one
    }

    //can we lock onto this target rn? also hands back where it is on screen
    private bool IsValidTarget(LockOnTarget target, out Vector3 viewportPos)
    {
        viewportPos = Vector3.zero;
        if (target == null || !target.CanBeTargeted) return false;

        Vector3 point = target.LockPoint;
        if (Vector3.Distance(transform.position, point) > maxLockDistance) return false; //too far

        //on screen check: z <= 0 means its behind the camera, x and y outside 0-1 means off the edge
        viewportPos = cam.WorldToViewportPoint(point);
        if (viewportPos.z <= 0f || viewportPos.x < 0f || viewportPos.x > 1f || viewportPos.y < 0f || viewportPos.y > 1f)
            return false;

        //line of sight: shoot a line from the player to the target, if anything else is in the way its blocked
        Vector3 toTarget = point - transform.position;
        RaycastHit[] hits = Physics.RaycastAll(transform.position, toTarget.normalized, toTarget.magnitude,
            sightBlockers, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit hit in hits)
        {
            if (hit.transform.IsChildOf(transform)) continue; //our own body/nose doesnt count
            if (hit.transform.IsChildOf(target.transform)) continue; //the target itself doesnt count
            return false; //something else is in the way
        }
        return true;
    }

    //OnGUI draws simple 2d stuff on top of the game, here a red dot on the locked target
    private void OnGUI()
    {
        if (CurrentTarget == null || cam == null) return;

        Vector3 screenPos = cam.WorldToScreenPoint(CurrentTarget.LockPoint);
        if (screenPos.z <= 0f) return; //behind the camera

        //screen positions start at the BOTTOM left, but GUI starts at the TOP left, so flip y
        Rect rect = new Rect(screenPos.x - iconSize * 0.5f, Screen.height - screenPos.y - iconSize * 0.5f, iconSize, iconSize);
        GUI.color = iconColor;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = Color.white; //put it back so other GUI isnt tinted
    }
}
