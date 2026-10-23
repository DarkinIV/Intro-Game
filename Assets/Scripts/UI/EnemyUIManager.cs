using UnityEngine;

// Creates the UI that floats above each enemy (health bar and stunned icon).
// Any new enemy UI can be added here as another prefab.
public class EnemyUIManager : MonoBehaviour
{
    public static EnemyUIManager instance;

    public HealthUI healthUIPrefab;
    public StunnedUI stunnedUIPrefab;

    void Awake()
    {
        instance = this;
    }

    // They all follow the same point, the prefabs themselves decide how high each one sits
    public void Create(EnemyController enemy, Transform anchor)
    {
        Instantiate(healthUIPrefab, transform).Init(anchor, enemy.GetComponent<CharacterStats>());
        Instantiate(stunnedUIPrefab, transform).Init(anchor, enemy);
    }
}
