using UnityEngine;

//temporary prototype for debugging and seeing whats up (top right)
//ult charge, untouchable timer, i-frames, current move, air jumps. untick show to hide it, dont think u need to do that tho
//the bar drawing lives in the shared DebugHud helper (all three class HUDs use it)
[RequireComponent(typeof(AcrobatController))]
public class AcrobatDebugHUD : MonoBehaviour
{
    [SerializeField] bool show = true;
    AcrobatController acro;
    AcrobatDodge dodge;
    AcrobatWallMoves wall;
    AcrobatUltimate ult;
    PlayerStats stats;
    AcrobatInvulnerability invuln;
    AcrobatWhip whip;
    void Awake()
    {
        acro = GetComponent<AcrobatController>();
        dodge = GetComponent<AcrobatDodge>();
        wall = GetComponent<AcrobatWallMoves>();
        ult = GetComponent<AcrobatUltimate>();
        stats = GetComponent<PlayerStats>();
        invuln = GetComponent<AcrobatInvulnerability>();
        whip = GetComponent<AcrobatWhip>();
    }
    //OnGUI draws simple 2d boxes and text on top of the game, it runs every frame
    void OnGUI()
    {
        if (!show || acro == null) return;

        float width = 240f;
        float x = Screen.width - width - 15f;
        float y = 10f;

        GUI.Box(new Rect(x - 5f, y - 5f, width + 10f, 118f), GUIContent.none); 
        GUIStyle rich = DebugHud.RichText();

        GUI.Label(new Rect(x, y, width, 20f), "<b>ACROBAT</b>  (Q = Untouchable)", rich);
        y += 20f;

        //ult meter
        string activeName = null;
        float remaining = 0f;
        if (ult != null && ult.UltimateActive)
        {
            activeName = "UNTOUCHABLE";
            remaining = ult.UltimateRemaining;
        }
        DebugHud.DrawUltBar(x, y, width, stats, activeName, remaining, "Q");
        y += 22f;

        //what the player is doing right now
        string state = "";
        if (invuln != null && invuln.IsInvulnerable) state += "<color=cyan><b>I-FRAMES</b></color> ";
        if (dodge != null && dodge.IsDodging) state += "Dodging ";
        if (dodge != null && dodge.IsSliding) state += "Sliding ";
        if (wall != null && wall.IsWallRunning) state += "Wall run ";
        if (wall != null && wall.IsWallClimbing) state += "Wall climb ";
        if (acro.IsSprintingNow) state += "Sprinting ";
        if (whip != null && whip.IsBusy) state += "Whip ";
        if (!acro.IsGroundedNow) state += "In air";
        GUI.Label(new Rect(x, y, width, 20f), state, rich);
        y += 20f;

        GUI.Label(new Rect(x, y, width, 20f), "Air jumps left: " + acro.AirJumpsLeft);
        y += 20f;

        if (acro.LockTarget != null) GUI.Label(new Rect(x, y, width, 20f), "Locked: " + acro.LockTarget.root.name);
        else GUI.Label(new Rect(x, y, width, 20f), "Lock-on: middle click");
    }
}
