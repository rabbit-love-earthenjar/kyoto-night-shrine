using System;

public static class FarmEconomyFormula
{
    public const float SeedCostRate = 0.5f;
    public const int GrowthSecondsPerNetValue = 45;
    public const int StarterOutputAmount = 2;

    public static string GetSeedId(string cropId)
    {
        switch (cropId)
        {
            case "wheat":
                return ResourceInventory.WheatSeedId;
            case "coffee_bean":
                return ResourceInventory.CoffeeSeedId;
            case "sugarcane":
                return ResourceInventory.SugarcaneSeedId;
            default:
                return string.Empty;
        }
    }

    public static string GetOutputIngredientId(string cropId)
    {
        switch (cropId)
        {
            case "wheat":
                return ResourceInventory.FlourId;
            case "coffee_bean":
                return ResourceInventory.CoffeeBeanId;
            case "sugarcane":
                return ResourceInventory.SugarId;
            default:
                return string.Empty;
        }
    }

    public static int CalculateSeedPrice(string cropId, int outputAmount = StarterOutputAmount)
    {
        string outputIngredientId = GetOutputIngredientId(cropId);
        int grossValue = CafeEconomyFormula.GetIngredientUnitPrice(outputIngredientId) * Math.Max(1, outputAmount);
        return Math.Max(1, (int)Math.Ceiling(grossValue * SeedCostRate));
    }

    public static int CalculateGrowthSeconds(string cropId, int outputAmount = StarterOutputAmount)
    {
        string outputIngredientId = GetOutputIngredientId(cropId);
        int grossValue = CafeEconomyFormula.GetIngredientUnitPrice(outputIngredientId) * Math.Max(1, outputAmount);
        int netValue = Math.Max(1, grossValue - CalculateSeedPrice(cropId, outputAmount));
        return GrowthSecondsPerNetValue * netValue;
    }
}
