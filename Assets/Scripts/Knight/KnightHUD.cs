using UnityEngine;

//temp prototype box has shield state, spin cooldown, ult meter / Unkillable timer
//the base UI doesnt show abilities yet
[RequireComponent(typeof(KnightAbilities))]
public class KnightHUD : MonoBehaviour
{
    [SerializeField] bool show = true;

    KnightShield shield;
    PlayerStats stats;

    void Awake()
    {
        shield = GetComponent<KnightShield>();
        stats = GetComponent<PlayerStats>();
    }
    void OnGUI()
    {
        if (!show || shield == null) return;

        float width = 250f;
        float x = Screen.width - width - 15f;
        float y = 10f;

        GUI.Box(new Rect(x - 5f, y - 5f, width + 10f, 118f), GUIContent.none);
        GUIStyle rich = DebugHud.RichText();

        GUI.Label(new Rect(x, y, width, 20f), "<b>KNIGHT</b>  RMB block/parry · F spin · R ult", rich);
        y += 20f;
        //shield state
        string guard = "Shield down (hold right click)";
        if (shield.GuardBroken) guard = "<color=red><b>GUARD BROKEN</b></color>";
        else if (shield.IsBlocking) guard = "<color=#66b3ff><b>BLOCKING</b></color>";
        if (shield.RiposteArmed) guard += "  <color=yellow>Riposte ready!</color>";
        GUI.Label(new Rect(x, y, width, 20f), guard, rich);
        y += 20f;
        //spin cooldown
        string spin = "No basic ability assigned";
        if (stats.BasicAbility != null)
        {
            if (stats.BasicAbility.IsReady) spin = "Spin Attack: ready (F)";
            else spin = "Spin Attack: " + stats.BasicAbility.CooldownRemaining.ToString("F1") + "s";
        }
        GUI.Label(new Rect(x, y, width, 20f), spin);
        y += 22f;

        //ult meter
        string activeName = null;
        if (shield.UnkillableActive) activeName = "UNKILLABLE";
        DebugHud.DrawUltBar(x, y, width, stats, activeName, shield.UnkillableRemaining, "R");
    }
}
