using UnityEngine;

//glowing circle drawn with a LineRenderer. Used by foresight for fortune teller (red warning rings where an enemy hit will land)
//and fate sealed (purple rings under marked enemies). Can follow a target, grow over time and remove itself test it 
public class RingMarker : MonoBehaviour
{
    const int Points = 40; //how many points make up the circle
    LineRenderer line;
    Transform follow;
    bool hadFollow;  //was it following something
    Vector3 offset; //where the ring sits compared to the thing it follows
    float startRadius;
    float endRadius;
    float growTime;
    float lifetime;
    float born;  //the time it was created
    //makes a new ring. follow : null means it stays where it was put, lifetime 0 = lives forever
    public static RingMarker Spawn(Vector3 position, float startRadius, float endRadius, float growTime,float lifetime, Color color, Transform follow = null, float width = 0.06f)
    {
        GameObject go = new GameObject("RingMarker");
        RingMarker ring = go.AddComponent<RingMarker>();
        ring.Setup(position, startRadius, endRadius, growTime, lifetime, color, follow, width);
        return ring;
    }

    void Setup(Vector3 position, float r0, float r1, float grow, float life, Color color, Transform target, float width)
    {
        follow = target;
        hadFollow = target != null;
        offset = Vector3.zero;
        if (target != null) offset = position - target.position;
        transform.position = position;
        startRadius = r0;
        endRadius = r1;
        growTime = grow;
        lifetime = life;
        born = Time.time;

        line = gameObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.loop = true; //joins the last point back to the first
        line.positionCount = Points;
        line.startWidth = width;
        line.endWidth = width;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = color;
        line.endColor = color;
        Redraw(r0);
    }
    void LateUpdate()
    {
        float age = Time.time - born;
        if (lifetime > 0f && age >= lifetime)
        {
            Destroy(gameObject); //time is up
            return;
        }
        if (hadFollow && follow == null)
        {
            Destroy(gameObject); //the enemy it was under died or disappeared
            return;
        }
        if (follow != null) transform.position = follow.position + offset;
        //t goes from 0 to 1 over growTime, so the ring grows from startRadius to endRadius
        float t = 1f;
        if (growTime > 0f) t = Mathf.Clamp01(age / growTime);
        Redraw(Mathf.Lerp(startRadius, endRadius, t));
    }

    void Redraw(float radius)
    {
        Vector3 c = transform.position + Vector3.up * 0.05f; //just above the floor so it isn6 hidden inside it
        for (int i = 0; i < Points; i++)
        {
            float angle = i / (float)Points * Mathf.PI * 2f; //spread the points around the full circle
            Vector3 point = c + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            line.SetPosition(i, point);
        }
    }
}
