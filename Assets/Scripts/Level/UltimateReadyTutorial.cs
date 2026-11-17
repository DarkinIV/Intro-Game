using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UltimateReadyTutorial : MonoBehaviour
{
    [SerializeField] List<TutorialData> ultimateTutorials;
    [SerializeField] UltimateAbility playerUlt;

    bool shown;
    // Start is called before the first frame update
    void Start()
    {
         playerUlt = Player.instance.playerStats.Ultimate;
    }

    // Update is called once per frame
    void Update()
    {
        if (shown || playerUlt is null)
            return;

        if (playerUlt.IsReady && playerUlt.ChargePercent >= 1f)
        {
            shown = true;
            foreach (TutorialData tutorial in ultimateTutorials)
            {
                UIManager.instance.ShowTutorial(tutorial);
            }
                
        }
    }
}
