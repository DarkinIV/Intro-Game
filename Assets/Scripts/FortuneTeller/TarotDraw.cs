using UnityEngine;
//runtime side of the card draw, the base BasicAbility handles the cooldown, this says what happens when its used
public class TarotDraw : BasicAbility
{
    TarotDrawData settings;
    public TarotDraw(TarotDrawData data) : base(data)
    {
        settings = data;
    }
    public TarotDrawData Settings
    {
        get { return settings; }
    }
    protected override void ExecuteInternal(PlayerStats stats, PlayerController controller, PlayerCombat combat)
    {
        FortuneTellerAbilities ft = stats.GetComponent<FortuneTellerAbilities>();
        if (ft != null)
        {
            ft.DrawCard(settings);
        }
        else
        {
            Debug.LogWarning("Tarot Draw needs the FortuneTellerAbilities component on the player.");
        }
    }
}
