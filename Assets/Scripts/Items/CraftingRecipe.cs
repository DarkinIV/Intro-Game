using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CraftingIngredient
{
    public Item item;
    [Min(1)] public int quantity = 1;

    public bool IsValid()
    {
        return item != null && quantity > 0;
    }
}

[Serializable]
public class CraftingOutput
{
    public Item item;
    [Min(1)] public int quantity = 1;

    public bool IsValid()
    {
        return item != null && quantity > 0;
    }
}

[CreateAssetMenu(fileName = "New Crafting Recipe", menuName = "Crafting/Recipe")]
public class CraftingRecipe : ScriptableObject
{
    new public string name = "New Recipe";
    public Sprite icon;

    [SerializeField] List<CraftingIngredient> ingredients = new List<CraftingIngredient>();
    [SerializeField] List<CraftingOutput> outputs = new List<CraftingOutput>();

    public IReadOnlyList<CraftingIngredient> Ingredients => ingredients;
    public IReadOnlyList<CraftingOutput> Outputs => outputs;

    public bool HasAnyValidIngredient()
    {
        for (int i = 0; i < ingredients.Count; i++)
        {
            if (ingredients[i].IsValid())
                return true;
        }

        return false;
    }

    public bool HasAnyValidOutput()
    {
        for (int i = 0; i < outputs.Count; i++)
        {
            if (outputs[i].IsValid())
                return true;
        }

        return false;
    }
}
