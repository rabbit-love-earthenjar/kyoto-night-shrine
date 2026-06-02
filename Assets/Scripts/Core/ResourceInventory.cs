using System.Collections.Generic;
using UnityEngine;

public class ResourceInventory : MonoBehaviour
{
    public const string BasicYokaiMaterialId = "BasicYokaiMaterial";
    public const string CoffeeBeanId = "CoffeeBean";
    public const string MilkId = "Milk";
    public const string SugarId = "Sugar";
    public const string FlourId = "Flour";

    public static ResourceInventory Instance { get; private set; }

    [SerializeField] private int faithPoints;
    [SerializeField] private bool persistAcrossScenes = true;
    [SerializeField] private bool cafeStarterIngredientsInitialized;
    [SerializeField] private List<MaterialStack> materials = new List<MaterialStack>();

    private readonly Dictionary<string, int> materialCounts = new Dictionary<string, int>();

    public int FaithPoints => faithPoints;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        RebuildMaterialCache();

        if (persistAcrossScenes)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void AddFaithPoints(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        faithPoints += amount;
    }

    public bool SpendFaithPoints(int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (faithPoints < amount)
        {
            return false;
        }

        faithPoints -= amount;
        return true;
    }

    public void AddMaterial(string materialId, int amount)
    {
        if (string.IsNullOrEmpty(materialId) || amount <= 0)
        {
            return;
        }

        int newAmount = GetMaterialCount(materialId) + amount;
        materialCounts[materialId] = newAmount;
        SyncMaterialList(materialId, newAmount);
    }

    public int GetMaterialCount(string materialId)
    {
        if (string.IsNullOrEmpty(materialId))
        {
            return 0;
        }

        return materialCounts.TryGetValue(materialId, out int amount) ? amount : 0;
    }

    public bool SpendMaterial(string materialId, int amount)
    {
        if (string.IsNullOrEmpty(materialId) || amount <= 0)
        {
            return true;
        }

        int currentAmount = GetMaterialCount(materialId);

        if (currentAmount < amount)
        {
            return false;
        }

        int newAmount = currentAmount - amount;
        materialCounts[materialId] = newAmount;
        SyncMaterialList(materialId, newAmount);
        return true;
    }

    public void AddIngredient(string ingredientId, int amount)
    {
        AddMaterial(ingredientId, amount);
    }

    public bool SpendIngredient(string ingredientId, int amount)
    {
        return SpendMaterial(ingredientId, amount);
    }

    public int GetIngredientCount(string ingredientId)
    {
        return GetMaterialCount(ingredientId);
    }

    public bool HasIngredient(string ingredientId, int amount)
    {
        return amount <= 0 || GetIngredientCount(ingredientId) >= amount;
    }

    public void EnsureCafeStarterIngredients(int amountPerIngredient = 2)
    {
        if (cafeStarterIngredientsInitialized)
        {
            return;
        }

        cafeStarterIngredientsInitialized = true;
        int starterAmount = Mathf.Max(0, amountPerIngredient);
        AddIngredient(CoffeeBeanId, starterAmount);
        AddIngredient(MilkId, starterAmount);
        AddIngredient(SugarId, starterAmount);
        AddIngredient(FlourId, starterAmount);
    }

    private void RebuildMaterialCache()
    {
        materialCounts.Clear();

        foreach (MaterialStack material in materials)
        {
            if (material == null || string.IsNullOrEmpty(material.materialId) || material.amount <= 0)
            {
                continue;
            }

            materialCounts[material.materialId] = material.amount;
        }
    }

    private void SyncMaterialList(string materialId, int amount)
    {
        for (int i = 0; i < materials.Count; i++)
        {
            if (materials[i] != null && materials[i].materialId == materialId)
            {
                materials[i].amount = amount;
                return;
            }
        }

        materials.Add(new MaterialStack
        {
            materialId = materialId,
            amount = amount
        });
    }

    [System.Serializable]
    private class MaterialStack
    {
        public string materialId = BasicYokaiMaterialId;
        public int amount;
    }
}
