using UnityEngine;
//purely visual again but hey thats what makes it JUICY(saw this on insta) gives a projectile a fading glowing trail. put it on the fate orb prefab
public class OrbTrail : MonoBehaviour
{
    [SerializeField] Color color = new Color(0.7f, 0.35f, 1f, 1f);
    [SerializeField] float time = 0.25f;//how long the trail lasts behind the orb
    [SerializeField] float width = 0.12f;
    void Awake()
    {
        //a TrailRenderer draws a ribbon behind a moving object
        TrailRenderer trail = gameObject.AddComponent<TrailRenderer>();
        trail.time = time;
        trail.startWidth = width;
        trail.endWidth = 0f;
        trail.material = new Material(Shader.Find("Sprites/Default")); //simple shader that just shows a colour
        trail.startColor = color;
        trail.endColor = new Color(color.r, color.g, color.b, 0f); //same colour but see through and fades out
        trail.numCapVertices = 2;
    }
}
