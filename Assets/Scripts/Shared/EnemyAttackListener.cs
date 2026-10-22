using System.Collections.Generic;
using UnityEngine;

//goes on the player, every enemy announces each swing through the base event CharacterCombat.OnAttack, but that event doesnt tell us which enemy attacked or swung
//this script listens to all of them and fires 1 event that includes the enemy, so other scripts (acrobat ultimate, fortune teller foresight) can just sub to OnEnemyAttack
//enemies can spawn at any time, so twice a second it looks for ones it isnt listening to yet
public class EnemyAttackListener : MonoBehaviour
{
    //an enemy started a swing, with the enemy
    public event System.Action<EnemyCombat> OnEnemyAttack;
    List<EnemyCombat> enemiesWeListenTo = new List<EnemyCombat>();
    float nextScan;
    void Update()
    {
        if (Time.time < nextScan) return;
        nextScan = Time.time + 0.5f;

        //forget enemies that were destroyed
        for (int i = enemiesWeListenTo.Count - 1; i >= 0; i--)
        {
            if (enemiesWeListenTo[i] == null) enemiesWeListenTo.RemoveAt(i);
        }

        EnemyCombat[] enemies = FindObjectsByType<EnemyCombat>(FindObjectsSortMode.None);
        foreach (EnemyCombat enemy in enemies)
        {
            if (enemiesWeListenTo.Contains(enemy)) continue;
            enemiesWeListenTo.Add(enemy);
            //when this enemy attacks, tell our listeners which enemy it was
            EnemyCombat thisEnemy = enemy;
            enemy.OnAttack += () => Announce(thisEnemy);
        }
    }

    void Announce(EnemyCombat enemy)
    {
        if (enemy == null) return;
        if (OnEnemyAttack != null) OnEnemyAttack(enemy);
    }
}
