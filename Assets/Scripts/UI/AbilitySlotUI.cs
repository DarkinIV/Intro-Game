using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One slot on the ability bar. UIManager makes one for each ability.
// The fill covers the slot while it's not ready: the cooldown time left, how much
// the ultimate still needs to charge, or how much of a held ability's meter is used up.
public class AbilitySlotUI : MonoBehaviour
{
    [SerializeField] Image icon;
    [SerializeField] Image fill;
    [SerializeField] TMP_Text fillText;
    [SerializeField] TMP_Text keyBinding;

    Ability ability;

    public void Setup(Ability slotAbility)
    {
        ability = slotAbility;
        keyBinding.text = ability.Key.ToString();

        if (ability.Data.icon != null)
            icon.sprite = ability.Data.icon;
    }

    void Update()
    {
        float covered = 0f;
        string text = "";

        if (ability.CooldownRemaining > 0f)
        {
            covered = ability.CooldownRemaining / ability.Data.cooldown;
            text = ability.CooldownRemaining.ToString("0.0") + "s";
        }
        else if (ability is UltimateAbility ultimate && ultimate.ChargePercent < 1f)
        {
            covered = 1f - ultimate.ChargePercent;
            text = "%" + Mathf.FloorToInt(ultimate.ChargePercent * 100f);
        }
        else if (ability is ChanneledAbility channel && channel.Meter < 1f)
        {
            covered = 1f - channel.Meter;
            text = "%" + Mathf.CeilToInt(channel.Meter * 100f);
        }

        fill.fillAmount = covered;
        fillText.text = text;
    }
}
