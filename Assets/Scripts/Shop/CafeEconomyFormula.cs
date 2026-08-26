using System;
using System.Collections.Generic;

public static class CafeEconomyFormula
{
    public const float TargetMarkupRate = 0.25f;
    public const int MinimumProfit = 1;

    public static int GetIngredientUnitPrice(string ingredientId)
    {
        switch (ingredientId)
        {
            case ResourceInventory.CoffeeBeanId:
            case ResourceInventory.MilkId:
            case ResourceInventory.SugarId:
                return 1;
            case ResourceInventory.FlourId:
                return 2;
            default:
                return 1;
        }
    }

    public static int CalculateRecipeReferenceCost(IReadOnlyList<CafeIngredientRequirement> ingredients)
    {
        if (ingredients == null)
        {
            return 0;
        }

        int totalCost = 0;

        for (int i = 0; i < ingredients.Count; i++)
        {
            CafeIngredientRequirement ingredient = ingredients[i];

            if (ingredient == null || ingredient.Amount <= 0)
            {
                continue;
            }

            totalCost += GetIngredientUnitPrice(ingredient.IngredientId) * ingredient.Amount;
        }

        return Math.Max(0, totalCost);
    }

    public static int CalculateMenuFaithReward(IReadOnlyList<CafeIngredientRequirement> ingredients)
    {
        int referenceCost = CalculateRecipeReferenceCost(ingredients);
        int percentageReward = (int)Math.Ceiling(referenceCost * (1f + TargetMarkupRate));
        return Math.Max(referenceCost + MinimumProfit, percentageReward);
    }

    public static int CalculateNetFaithProfit(int faithReward, int actualIngredientExpense)
    {
        return Math.Max(0, faithReward) - Math.Max(0, actualIngredientExpense);
    }
}
