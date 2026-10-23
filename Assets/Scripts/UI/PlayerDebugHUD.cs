// AI use: Whole script written by Claude (Anthropic).
// Owner: (add name)

using UnityEngine;

// Debug text in the top right for testing movement and abilities. Untick "show" to hide it.
public class PlayerDebugHUD : MonoBehaviour
{
    [SerializeField] bool show = true;

    PlayerController player;
    PlayerStats stats;

    void Awake()
    {
        player = GetComponent<PlayerController>();
        stats = GetComponent<PlayerStats>();
    }

    void OnGUI()
    {
        if (!show)
            return;

        string text = stats.Character.characterName;

        if (stats.Ultimate != null)
            text += "\nUlt " + Mathf.RoundToInt(stats.Ultimate.ChargePercent * 100f) + "%";

        if (player.IsInvulnerable) text += "\nI-FRAMES";
        if (player.IsDashing) text += "\nDashing";
        if (player.IsSliding) text += "\nSliding";
        if (player.IsWallRunning) text += "\nWall run";
        if (player.IsWallClimbing) text += "\nWall climb";
        if (!player.IsGrounded) text += "\nIn air (air jumps: " + player.AirJumpsLeft + ")";

        if (player.DebugStatus != null)
            text += "\n" + player.DebugStatus;

        GUI.Label(new Rect(Screen.width - 260f, 10f, 250f, 300f), text);
    }
}
