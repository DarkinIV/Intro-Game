using UnityEngine;

public class Checkpoint : Interactable
{
    public override void Interact()
    {
        Player.instance.Rest();
    }
}
