using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ChestItemEntry
{
    public Item item;
    [Min(1)] public int quantity = 1;
}

public class TreasureChest : Interactable {

	Animator animator;

	bool isOpen;
	public ChestItemEntry[] loot = Array.Empty<ChestItemEntry>();

	void Start() {
		animator = GetComponent<Animator> ();
	}

	public override void Interact ()
	{
		base.Interact ();
		if (!isOpen) {
			animator.SetTrigger ("Open");
			StartCoroutine (CollectTreasure ());
		}
	}

	IEnumerator CollectTreasure() {

		isOpen = true;

		yield return new WaitForSeconds (1f);
		print ("Chest opened");
		foreach (ChestItemEntry entry in loot) {
			if (entry == null || entry.item == null)
				continue;

			Inventory.instance.Add(entry.item, Mathf.Max(1, entry.quantity));
		}
	}
}
