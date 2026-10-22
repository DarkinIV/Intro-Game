using UnityEngine;

//same thing as knight and acro but this one has mana, next card, active card effects, the ult, sight
[RequireComponent(typeof(FortuneTellerAbilities))]
public class FortuneTellerHUD : MonoBehaviour
{
    [SerializeField] bool show = true;

    FortuneTellerAbilities ft;
    PlayerStats stats;
    Mana mana;
    void Awake()
    {
        ft = GetComponent<FortuneTellerAbilities>();
        stats = GetComponent<PlayerStats>();
        mana = GetComponent<Mana>();
    }
    void OnGUI()
    {
        if (!show || ft == null) return;

        float width = 250f;
        float x = Screen.width - width - 15f;
        float y = 10f;

        GUI.Box(new Rect(x - 5f, y - 5f, width + 10f, 182f), GUIContent.none);
        GUIStyle rich = DebugHud.RichText();

        GUI.Label(new Rect(x, y, width, 20f), "<b>FORTUNE TELLER</b>  F card · R ult · C sight · V meditate", rich);
        y += 20f;

        //mana
        if (mana != null)
        {
            string manaLabel = "Mana " + Mathf.FloorToInt(mana.Current) + " / " + Mathf.RoundToInt(mana.Max);
            if (ft.IsMeditating) manaLabel += "  (meditating)";
            float fill = 0f;
            if (mana.Max > 0f) fill = mana.Current / mana.Max;
            DebugHud.DrawBar(x, y, width, fill, new Color(0.35f, 0.45f, 1f), manaLabel);
        }
        else
        {
            GUI.Label(new Rect(x, y, width, 18f), "<color=red>No Mana component</color>", rich);
        }
        y += 22f;

        //next card + cooldown
        string cooldown = " (ready)";
        if (stats != null && stats.BasicAbility != null && !stats.BasicAbility.IsReady)
        {
            cooldown = " (ready in " + stats.BasicAbility.CooldownRemaining.ToString("F1") + "s)";
        }
        GUI.Label(new Rect(x, y, width, 20f), "Next card: <b>" + Name(ft.NextCard) + "</b>" + cooldown, rich);
        y += 20f;

        //armed cards or curse
        string active = "";
        if (ft.TowerReady) active += "<color=orange>Tower armed</color>  ";
        if (ft.DeathReady) active += "<color=silver>Death armed</color>  ";
        if (ft.Cursed) active += "<color=red>Cursed (slow)</color>  ";
        if (active == "") active = "Last card: " + Name(ft.LastCard);
        GUI.Label(new Rect(x, y, width, 20f), active, rich);
        y += 22f;

        //ult meter
        string activeName = null;
        if (ft.FateActive) activeName = "FATE SEALED (" + ft.MarkedCount + " marked)";
        DebugHud.DrawUltBar(x, y, width, stats, activeName, ft.FateRemaining, "R");
        y += 22f;

        if (ft.SightActive) GUI.Label(new Rect(x, y, width, 20f), "<color=cyan><b>CRYSTAL SIGHT</b></color>", rich);
        else GUI.Label(new Rect(x, y, width, 20f), "Sight: hold C", rich);
    }
    static string Name(FortuneTellerAbilities.Card c)
    {
        if (c == FortuneTellerAbilities.Card.Sun) return "The Sun";
        if (c == FortuneTellerAbilities.Card.Tower) return "The Tower";
        if (c == FortuneTellerAbilities.Card.Death) return "Death";
        if (c == FortuneTellerAbilities.Card.Star) return "The Star";
        if (c == FortuneTellerAbilities.Card.Fool) return "The Fool";
        return "-";
    }
}
