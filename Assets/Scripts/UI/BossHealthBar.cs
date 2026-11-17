using TMPro;
using UnityEngine;

// The big health bar at the bottom of the screen during a boss fight. Goes on the HUD canvas
// (on an object that stays switched on). BossPhases shows and hides it.
public class BossHealthBar : MonoBehaviour
{
    public static BossHealthBar instance;

    [SerializeField] GameObject panel;     // everything below sits under this, it's switched on and off
    [SerializeField] TMP_Text nameText;
    [SerializeField] ResourceBar bar;

    CharacterStats boss;

    void Awake()
    {
        instance = this;
        panel.SetActive(false);
    }

    public void Show(string bossName, CharacterStats bossStats)
    {
        Hide();

        boss = bossStats;
        boss.OnHealthChanged += UpdateBar;
        boss.OnDamaged += ShowLoss;

        nameText.text = bossName;
        bar.Set(boss.currentHealth, boss.maxHealth.Value);
        panel.SetActive(true);
    }

    public void Hide()
    {
        if (boss != null)
        {
            boss.OnHealthChanged -= UpdateBar;
            boss.OnDamaged -= ShowLoss;
            boss = null;
        }

        panel.SetActive(false);
    }

    void UpdateBar(int current, int max)
    {
        bar.Set(current, max);
    }

    // Health has already gone down when this is called, so add the damage back on for the old value
    void ShowLoss(int damage)
    {
        bar.ShowLoss(boss.currentHealth + damage, boss.maxHealth.Value);
    }
}
