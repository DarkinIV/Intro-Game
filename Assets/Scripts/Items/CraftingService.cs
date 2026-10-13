using System.Collections.Generic;
using UnityEngine;

public class CraftingService : MonoBehaviour
{
    public static CraftingService instance;

    Inventory inventory;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        inventory = Inventory.instance;
    }

    public bool CanCraft(CraftingRecipe recipe, int batchCount = 1)
    {
        if (recipe == null || inventory == null || batchCount <= 0)
            return false;

        if (!recipe.HasAnyValidIngredient() || !recipe.HasAnyValidOutput())
            return false;

        for (int i = 0; i < recipe.Ingredients.Count; i++)
        {
            CraftingIngredient ingredient = recipe.Ingredients[i];
            if (!ingredient.IsValid())
                continue;

            int required = ingredient.quantity * batchCount;
            if (!inventory.HasItem(ingredient.item, required))
                return false;
        }

        for (int i = 0; i < recipe.Outputs.Count; i++)
        {
            CraftingOutput output = recipe.Outputs[i];
            if (!output.IsValid())
                continue;

            int produced = output.quantity * batchCount;
            if (!inventory.CanAdd(output.item, produced))
                return false;
        }

        return true;
    }

    public bool Craft(CraftingRecipe recipe, int batchCount = 1)
    {
        if (!CanCraft(recipe, batchCount))
            return false;

        ConsumeIngredients(recipe, batchCount);
        ProduceOutputs(recipe, batchCount);

        Debug.Log("Crafted " + recipe.name + " x" + batchCount);

        return true;
    }

    void ConsumeIngredients(CraftingRecipe recipe, int batchCount)
    {
        for (int i = 0; i < recipe.Ingredients.Count; i++)
        {
            CraftingIngredient ingredient = recipe.Ingredients[i];
            if (!ingredient.IsValid())
                continue;

            int amount = ingredient.quantity * batchCount;
            inventory.Remove(ingredient.item, amount);
        }
    }

    void ProduceOutputs(CraftingRecipe recipe, int batchCount)
    {
        for (int i = 0; i < recipe.Outputs.Count; i++)
        {
            CraftingOutput output = recipe.Outputs[i];
            if (!output.IsValid())
                continue;

            int amount = output.quantity * batchCount;
            inventory.Add(output.item, amount);
        }
    }
}
