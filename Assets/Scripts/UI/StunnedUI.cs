using UnityEngine;

// Shows above the enemy's health bar while the enemy is stunned
public class StunnedUI : WorldUI
{
    [SerializeField] GameObject graphic;

    EnemyController enemy;

    public void Init(Transform target, EnemyController stunnedEnemy)
    {
        Follow(target);
        enemy = stunnedEnemy;
        graphic.SetActive(false);
    }

    protected override void LateUpdate()
    {
        base.LateUpdate();

        if (enemy != null)
            graphic.SetActive(enemy.IsStunned);
    }
}
