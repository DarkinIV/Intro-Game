using System.Collections.Generic;
using UnityEngine;

// The Fortune Teller's combat. Her orbs do magic damage.
// The Tower and Death cards power up her next hit, and Fate Sealed makes her hits on marked enemies crit.
[RequireComponent(typeof(Mana))]
public class FortuneTellerCombat : PlayerCombat
{
    [SerializeField] float fatedKillMana = 30f;

    [Header("Effects")]
    [SerializeField] GameObject fateMarkParticles;
    [SerializeField] GameObject towerParticles;

    Mana mana;

    float towerUntil;
    float towerRadius;
    float towerMultiplier;

    float deathUntil;
    float deathThreshold;

    float fateUntil;
    float critMultiplier;
    List<CharacterStats> markedEnemies = new List<CharacterStats>();
    List<GameObject> markEffects = new List<GameObject>();

    public bool TowerReady => Time.time < towerUntil;
    public bool DeathReady => Time.time < deathUntil;
    public bool FateActive => Time.time < fateUntil;
    public float FateRemaining => Mathf.Max(0f, fateUntil - Time.time);

    protected override DamageType AttackDamageType => DamageType.Magic;

    protected override void Awake()
    {
        base.Awake();
        mana = GetComponent<Mana>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        OnHit += ApplyHitEffects;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        OnHit -= ApplyHitEffects;
        ClearMarks();
    }

    protected override void Update()
    {
        base.Update();

        if (markedEnemies.Count > 0 && !FateActive)
            ClearMarks();
    }

    protected override int GetAttackDamage()
    {
        return playerStats.magic.Value;
    }

    // Killing a marked enemy gives some mana back
    protected override void OnSuccessfulHit(CharacterStats target, int damageDealt)
    {
        base.OnSuccessfulHit(target, damageDealt);

        if (FateActive && target.IsDead && markedEnemies.Remove(target))
            mana.Add(fatedKillMana);
    }

    // Tarot cards

    public void ArmTower(float radius, float magicMultiplier, float duration)
    {
        towerRadius = radius;
        towerMultiplier = magicMultiplier;
        towerUntil = Time.time + duration;
    }

    public void ArmDeath(float healthThreshold, float duration)
    {
        deathThreshold = healthThreshold;
        deathUntil = Time.time + duration;
    }

    // Fate Sealed: marks every enemy that's close enough and on screen
    public void SealFate(FateSealedData data)
    {
        ClearMarks();
        fateUntil = Time.time + data.duration;
        critMultiplier = data.critMultiplier;

        foreach (EnemyCombat enemy in FindObjectsByType<EnemyCombat>(FindObjectsSortMode.None))
        {
            CharacterStats enemyStats = enemy.GetComponent<CharacterStats>();

            if (enemyStats.IsDead || Vector3.Distance(transform.position, enemy.transform.position) > data.range)
                continue;

            if (!IsOnScreen(enemy.transform.position + Vector3.up))
                continue;

            markedEnemies.Add(enemyStats);

            if (fateMarkParticles != null)
                markEffects.Add(Instantiate(fateMarkParticles, enemy.transform));
        }
    }

    bool IsOnScreen(Vector3 worldPoint)
    {
        Vector3 viewport = Camera.main.WorldToViewportPoint(worldPoint);
        return viewport.z > 0f && viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f;
    }

    void ClearMarks()
    {
        foreach (GameObject effect in markEffects)
        {
            if (effect != null)
                Destroy(effect);
        }

        markedEnemies.Clear();
        markEffects.Clear();
    }

    // Runs after a normal orb hit has done its damage
    void ApplyHitEffects(CharacterStats victim)
    {
        // Fate Sealed: marked enemies take extra damage on top of the hit
        if (FateActive && !victim.IsDead && markedEnemies.Contains(victim))
        {
            int bonus = weapons.CurrentWeapon != null ? weapons.CurrentWeapon.bonusDamage : 0;
            int extraDamage = Mathf.RoundToInt((GetAttackDamage() + bonus) * (critMultiplier - 1f));
            DealAbilityDamage(victim, extraDamage, DamageType.Magic);
        }

        // Death card: finishes off the enemy if it's low enough
        if (DeathReady && !victim.IsDead && victim.currentHealth <= victim.maxHealth.Value * deathThreshold)
        {
            deathUntil = 0f;
            DealAbilityDamage(victim, victim.currentHealth, DamageType.Magic);
        }

        // Tower card: the hit explodes and damages everyone around the enemy
        if (TowerReady)
        {
            towerUntil = 0f;
            int towerDamage = Mathf.RoundToInt(playerStats.magic.Value * towerMultiplier);
            DealAreaDamage(victim.transform.position, towerRadius, towerDamage, DamageType.Magic);

            if (towerParticles != null)
                Destroy(Instantiate(towerParticles, victim.transform.position, Quaternion.identity), 2f);
        }
    }
}
