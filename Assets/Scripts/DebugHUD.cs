using UnityEngine;

//TEMPORARY debug readout in the top left corner of the game view: class, health, stamina, i-frames
//not the real UI, just so we can see the numbers while testing. goes on the player
public class DebugHUD : MonoBehaviour
{
    [SerializeField] private bool show = true;

    private Health health;
    private Stamina stamina;
    private PlayerClass playerClass;
    private PlayerMovement movement;
    private PlayerDodge dodge;
    private PlayerTraversal traversal;

    private void Awake()
    {
        traversal = GetComponent<PlayerTraversal>();
        health = GetComponent<Health>();
        stamina = GetComponent<Stamina>();
        playerClass = GetComponent<PlayerClass>();
        movement = GetComponent<PlayerMovement>();
        dodge = GetComponent<PlayerDodge>();
    }

    //OnGUI draws simple 2d boxes and text on top of the game, it runs every frame
    private void OnGUI()
    {
        if (!show) return;

        float x = 10f, y = 10f, width = 220f;
        GUI.Box(new Rect(x - 5f, y - 5f, width + 10f, 142f), GUIContent.none); //dark background

        string className = playerClass != null && playerClass.Current != null ? playerClass.Current.displayName : "No class";
        GUI.Label(new Rect(x, y, width, 20f), $"Class: {className}");
        y += 22f;

        if (health != null)
        {
            DrawBar(x, y, width, health.CurrentHealth, health.MaxHealth, new Color(0.85f, 0.2f, 0.2f), "HP");
            y += 22f;
        }

        if (stamina != null)
        {
            //turns orange when exhausted (ran out, cant sprint until it refills a bit)
            Color c = stamina.IsExhausted ? new Color(1f, 0.5f, 0f) : new Color(0.3f, 0.8f, 0.3f);
            DrawBar(x, y, width, stamina.Current, stamina.Max, c, "Stamina");
            y += 22f;
        }

        //what the player is doing rn
        string state = "";
        if (health != null && health.IsInvulnerable) state += "<color=cyan><b>I-FRAMES</b></color>  ";
        if (dodge != null && dodge.IsDodging) state += "Dodging  ";
        if (movement != null && movement.IsSprinting) state += "Sprinting  ";
        if (traversal != null && traversal.IsSliding) state += "Sliding  ";
        if (traversal != null && traversal.IsWallRunning) state += "Wall running  ";
        if (movement != null && !movement.IsGrounded) state += "In air";
        GUIStyle rich = new GUIStyle(GUI.skin.label) { richText = true }; //richText lets us use <color> in the text
        GUI.Label(new Rect(x, y, width, 20f), state, rich);
        y += 22f;

        if (movement != null) GUI.Label(new Rect(x, y, width, 20f), $"Air jumps left: {movement.AirJumpsLeft}");
    }

    private void DrawBar(float x, float y, float width, float value, float max, Color color, string label)
    {
        float fill = max > 0f ? Mathf.Clamp01(value / max) : 0f; //0 to 1, how full the bar is

        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(new Rect(x, y, width, 18f), Texture2D.whiteTexture); //empty part
        GUI.color = color;
        GUI.DrawTexture(new Rect(x, y, width * fill, 18f), Texture2D.whiteTexture); //full part
        GUI.color = Color.white;
        //Ceil rounds up so 0.4 stamina shows as 1, not 0
        GUI.Label(new Rect(x + 5f, y, width, 18f), $"{label}: {Mathf.Ceil(value)} / {max}");
    }
}
