using System.Collections.Generic;
using UnityEngine;

public class ResourceInventory : MonoBehaviour
{
    public const string BasicYokaiMaterialId = "BasicYokaiMaterial";
    public const string CoffeeBeanId = "CoffeeBean";
    public const string MilkId = "Milk";
    public const string SugarId = "Sugar";
    public const string FlourId = "Flour";
    public const string HeartFoxId = "HeartFox";
    public const string InariCoffeeId = "InariCoffee";
    public const string KitsunebiLatteId = "KitsunebiLatte";
    public const string YozakuraCakeId = "YozakuraCake";
    public const string WheatSeedId = "WheatSeed";
    public const string CoffeeSeedId = "CoffeeSeed";
    public const string SugarcaneSeedId = "SugarcaneSeed";

    private const string FaithPointsSaveKey = "ResourceInventory.FaithPoints";
    private const string CafeStarterIngredientsSaveKey = "ResourceInventory.CafeStarterIngredientsInitialized";
    private const string FarmStarterSeedsSaveKey = "ResourceInventory.FarmStarterSeedsInitialized";
    private const string MaterialSaveKeyPrefix = "ResourceInventory.Material.";
    private static readonly string[] PersistedMaterialIds =
    {
        BasicYokaiMaterialId,
        CoffeeBeanId,
        MilkId,
        SugarId,
        FlourId,
        HeartFoxId,
        InariCoffeeId,
        KitsunebiLatteId,
        YozakuraCakeId,
        WheatSeedId,
        CoffeeSeedId,
        SugarcaneSeedId
    };

    public static ResourceInventory Instance { get; private set; }

    [SerializeField] private int faithPoints;
    [SerializeField] private bool persistAcrossScenes = true;
    [SerializeField] private bool persistResourceState = true;
    [SerializeField] private bool cafeStarterIngredientsInitialized;
    [SerializeField] private bool farmStarterSeedsInitialized;
    [SerializeField] private List<MaterialStack> materials = new List<MaterialStack>();

    private readonly Dictionary<string, int> materialCounts = new Dictionary<string, int>();

    public int FaithPoints => faithPoints;
    public int HeartFoxCount => GetMaterialCount(HeartFoxId);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        RebuildMaterialCache();
        LoadPersistedState();

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
        PersistState();
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
        PersistState();
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
        PersistState();
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
        PersistState();
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

    public void AddSeed(string seedId, int amount)
    {
        AddMaterial(seedId, amount);
    }

    public bool SpendSeed(string seedId, int amount)
    {
        return SpendMaterial(seedId, amount);
    }

    public int GetSeedCount(string seedId)
    {
        return GetMaterialCount(seedId);
    }

    public bool HasSeed(string seedId, int amount)
    {
        return amount <= 0 || GetSeedCount(seedId) >= amount;
    }

    public void AddFinishedItem(string finishedItemId, int amount)
    {
        AddMaterial(finishedItemId, amount);
    }

    public bool SpendFinishedItem(string finishedItemId, int amount)
    {
        return SpendMaterial(finishedItemId, amount);
    }

    public int GetFinishedItemCount(string finishedItemId)
    {
        return GetMaterialCount(finishedItemId);
    }

    public bool HasFinishedItem(string finishedItemId, int amount)
    {
        return amount <= 0 || GetFinishedItemCount(finishedItemId) >= amount;
    }

    public void AddHeartFox(int amount)
    {
        AddMaterial(HeartFoxId, amount);
    }

    public void ResetForNewGame()
    {
        faithPoints = 0;
        cafeStarterIngredientsInitialized = false;
        farmStarterSeedsInitialized = false;
        materialCounts.Clear();
        materials.Clear();
        PersistState();
    }

    public bool SpendHeartFox(int amount)
    {
        return SpendMaterial(HeartFoxId, amount);
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
        PersistState();
    }

    public void EnsureFarmStarterSeeds(int amountPerSeed = 1)
    {
        if (farmStarterSeedsInitialized)
        {
            return;
        }

        farmStarterSeedsInitialized = true;
        int starterAmount = Mathf.Max(0, amountPerSeed);
        AddSeed(WheatSeedId, starterAmount);
        AddSeed(CoffeeSeedId, starterAmount);
        AddSeed(SugarcaneSeedId, starterAmount);
        PersistState();
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

    private void LoadPersistedState()
    {
        if (!persistResourceState)
        {
            return;
        }

        if (PlayerPrefs.HasKey(FaithPointsSaveKey))
        {
            faithPoints = Mathf.Max(0, PlayerPrefs.GetInt(FaithPointsSaveKey, faithPoints));
        }

        cafeStarterIngredientsInitialized = PlayerPrefs.GetInt(
            CafeStarterIngredientsSaveKey,
            cafeStarterIngredientsInitialized ? 1 : 0) == 1;
        farmStarterSeedsInitialized = PlayerPrefs.GetInt(
            FarmStarterSeedsSaveKey,
            farmStarterSeedsInitialized ? 1 : 0) == 1;

        for (int i = 0; i < PersistedMaterialIds.Length; i++)
        {
            string materialId = PersistedMaterialIds[i];
            string saveKey = GetMaterialSaveKey(materialId);

            if (!PlayerPrefs.HasKey(saveKey))
            {
                continue;
            }

            int amount = Mathf.Max(0, PlayerPrefs.GetInt(saveKey, GetMaterialCount(materialId)));
            materialCounts[materialId] = amount;
            SyncMaterialList(materialId, amount);
        }
    }

    private void PersistState()
    {
        if (!persistResourceState)
        {
            return;
        }

        PlayerPrefs.SetInt(FaithPointsSaveKey, Mathf.Max(0, faithPoints));
        PlayerPrefs.SetInt(CafeStarterIngredientsSaveKey, cafeStarterIngredientsInitialized ? 1 : 0);
        PlayerPrefs.SetInt(FarmStarterSeedsSaveKey, farmStarterSeedsInitialized ? 1 : 0);

        for (int i = 0; i < PersistedMaterialIds.Length; i++)
        {
            string materialId = PersistedMaterialIds[i];
            PlayerPrefs.SetInt(GetMaterialSaveKey(materialId), Mathf.Max(0, GetMaterialCount(materialId)));
        }

        PlayerPrefs.Save();
        SaveManager.NotifyLegacyProgressChanged();
    }

    private static string GetMaterialSaveKey(string materialId)
    {
        return MaterialSaveKeyPrefix + materialId;
    }

    [System.Serializable]
    private class MaterialStack
    {
        public string materialId = BasicYokaiMaterialId;
        public int amount;
    }
}
