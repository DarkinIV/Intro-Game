// AI use: Whole script written by Claude (Anthropic).
// Owner: (add name)

using UnityEngine;

// Base for UI that floats above a character (health bar, stunned icon).
// It follows the target around, turns to face the camera and deletes itself when the target is gone.
public class WorldUI : MonoBehaviour
{
    Transform target;
    Transform cam;

    protected void Follow(Transform followTarget)
    {
        target = followTarget;
        cam = Camera.main.transform;
    }

    protected virtual void LateUpdate()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        transform.position = target.position;
        transform.LookAt(new Vector3(cam.position.x, transform.position.y, cam.position.z), Vector3.down);
    }
}
