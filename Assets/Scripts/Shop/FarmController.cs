using System;
using System.Collections.Generic;
using UnityEngine;

public class FarmController : MonoBehaviour
{
    private const string PlotCropIdKey = "CropId";
    private const string PlotPlantedTimeKey = "PlantedTime";
    private const string PlotGrowthSecondsKey = "GrowthSeconds";

    [Header("Farm Settings")]
    [SerializeField] private int plotCount = 9;
    [SerializeField] private bool persistFarmState = true;
    [SerializeField] private string farmSaveKeyPrefix = "FarmController.Plot.";

    [Header("Prototype Crop Data")]
    [SerializeField] private List<FarmCropDefinition> cropDefinitions = new List<FarmCropDefinition>();
    [SerializeField] private List<FarmPlotState> plots = new List<FarmPlotState>();

    private readonly Dictionary<string, FarmCropDefinition> cropLookup = new Dictionary<string, FarmCropDefinition>();
    private ResourceInventory resourceInventory;

    public IReadOnlyList<FarmPlotState> Plots => plots;
    public IReadOnlyList<FarmCropDefinition> CropDefinitions => cropDefinitions;

    public event Action<int, FarmPlotState> PlotChanged;
    public event Action<int, FarmCropDefinition, int> CropHarvested;

    private void Awake()
    {
        EnsureDefaultCropDefinitions();
        RebuildCropLookup();
        EnsurePlotList();
        LoadFarmState();
        RefreshPlotGrowthStates();
    }

    private void Update()
    {
        RefreshPlotGrowthStates();
    }

    public bool TryPlantCrop(int plotIndex, string cropId)
    {
        if (!IsValidPlotIndex(plotIndex) || string.IsNullOrEmpty(cropId))
        {
            return false;
        }

        FarmPlotState plot = plots[plotIndex];

        if (plot.Phase != FarmPlotPhase.Empty)
        {
            return false;
        }

        if (!cropLookup.TryGetValue(cropId, out FarmCropDefinition crop))
        {
            Debug.LogWarning($"Farm crop id is not registered: {cropId}");
            return false;
        }

        plot.SetPlanted(crop.CropId, GetCurrentUnixTime(), crop.GrowthSeconds);
        SavePlotState(plotIndex);
        RaisePlotChanged(plotIndex);
        return true;
    }

    public bool TryPlantCrop(int plotIndex, FarmCropKind cropKind)
    {
        return TryPlantCrop(plotIndex, GetCropId(cropKind));
    }

    public bool TryHarvestCrop(int plotIndex)
    {
        if (!IsValidPlotIndex(plotIndex))
        {
            return false;
        }

        RefreshPlotGrowthState(plotIndex);
        FarmPlotState plot = plots[plotIndex];

        if (plot.Phase != FarmPlotPhase.Ready)
        {
            return false;
        }

        if (!cropLookup.TryGetValue(plot.CropId, out FarmCropDefinition crop))
        {
            Debug.LogWarning($"Farm plot has an unknown crop id: {plot.CropId}");
            return false;
        }

        ResolveResourceInventory().AddIngredient(crop.OutputIngredientId, crop.OutputAmount);
        int harvestedAmount = crop.OutputAmount;
        plot.Clear();
        SavePlotState(plotIndex);
        RaisePlotChanged(plotIndex);
        CropHarvested?.Invoke(plotIndex, crop, harvestedAmount);
        return true;
    }

    public void ClearPlot(int plotIndex)
    {
        if (!IsValidPlotIndex(plotIndex))
        {
            return;
        }

        plots[plotIndex].Clear();
        SavePlotState(plotIndex);
        RaisePlotChanged(plotIndex);
    }

    public void ForcePlotReady(int plotIndex)
    {
        if (!IsValidPlotIndex(plotIndex) || plots[plotIndex].Phase == FarmPlotPhase.Empty)
        {
            return;
        }

        FarmPlotState plot = plots[plotIndex];
        plot.SetPlanted(plot.CropId, GetCurrentUnixTime() - Mathf.Max(1, plot.GrowthSeconds), plot.GrowthSeconds);
        RefreshPlotGrowthState(plotIndex);
        SavePlotState(plotIndex);
        RaisePlotChanged(plotIndex);
    }

    public FarmCropDefinition GetCropDefinition(string cropId)
    {
        return !string.IsNullOrEmpty(cropId) && cropLookup.TryGetValue(cropId, out FarmCropDefinition crop)
            ? crop
            : null;
    }

    public float GetPlotGrowth01(int plotIndex)
    {
        if (!IsValidPlotIndex(plotIndex))
        {
            return 0f;
        }

        FarmPlotState plot = plots[plotIndex];

        if (plot.Phase == FarmPlotPhase.Empty)
        {
            return 0f;
        }

        if (plot.Phase == FarmPlotPhase.Ready)
        {
            return 1f;
        }

        int growthSeconds = Mathf.Max(1, plot.GrowthSeconds);
        long elapsed = Math.Max(0L, GetCurrentUnixTime() - plot.PlantedUnixTime);
        return Mathf.Clamp01((float)elapsed / growthSeconds);
    }

    public FarmPlotPhase GetPlotPhase(int plotIndex)
    {
        if (!IsValidPlotIndex(plotIndex))
        {
            return FarmPlotPhase.Empty;
        }

        RefreshPlotGrowthState(plotIndex);
        return plots[plotIndex].Phase;
    }

    public FarmCropDefinition GetPlotCropDefinition(int plotIndex)
    {
        if (!IsValidPlotIndex(plotIndex))
        {
            return null;
        }

        return GetCropDefinition(plots[plotIndex].CropId);
    }

    private void RefreshPlotGrowthStates()
    {
        for (int i = 0; i < plots.Count; i++)
        {
            RefreshPlotGrowthState(i);
        }
    }

    private void RefreshPlotGrowthState(int plotIndex)
    {
        if (!IsValidPlotIndex(plotIndex))
        {
            return;
        }

        FarmPlotState plot = plots[plotIndex];

        if (plot.Phase == FarmPlotPhase.Empty || plot.Phase == FarmPlotPhase.Ready)
        {
            return;
        }

        long elapsed = Math.Max(0L, GetCurrentUnixTime() - plot.PlantedUnixTime);

        if (elapsed >= Mathf.Max(1, plot.GrowthSeconds))
        {
            plot.SetPhase(FarmPlotPhase.Ready);
            SavePlotState(plotIndex);
            RaisePlotChanged(plotIndex);
            return;
        }

        FarmPlotPhase targetPhase = elapsed > 0
            ? FarmPlotPhase.Growing
            : FarmPlotPhase.Seed;

        if (plot.Phase != targetPhase)
        {
            plot.SetPhase(targetPhase);
            SavePlotState(plotIndex);
            RaisePlotChanged(plotIndex);
        }
    }

    private void EnsureDefaultCropDefinitions()
    {
        if (cropDefinitions.Count > 0)
        {
            return;
        }

        cropDefinitions.Add(new FarmCropDefinition(
            "wheat",
            "麦",
            ResourceInventory.FlourId,
            1,
            30));
        cropDefinitions.Add(new FarmCropDefinition(
            "coffee_bean",
            "コーヒー",
            ResourceInventory.CoffeeBeanId,
            1,
            40));
        cropDefinitions.Add(new FarmCropDefinition(
            "sugarcane",
            "砂糖きび",
            ResourceInventory.SugarId,
            1,
            35));
    }

    private void RebuildCropLookup()
    {
        cropLookup.Clear();

        for (int i = 0; i < cropDefinitions.Count; i++)
        {
            FarmCropDefinition crop = cropDefinitions[i];

            if (crop == null || string.IsNullOrEmpty(crop.CropId))
            {
                continue;
            }

            cropLookup[crop.CropId] = crop;
        }
    }

    private void EnsurePlotList()
    {
        int targetCount = Mathf.Max(1, plotCount);

        while (plots.Count < targetCount)
        {
            plots.Add(new FarmPlotState());
        }

        while (plots.Count > targetCount)
        {
            plots.RemoveAt(plots.Count - 1);
        }
    }

    private void LoadFarmState()
    {
        if (!persistFarmState)
        {
            return;
        }

        for (int i = 0; i < plots.Count; i++)
        {
            string cropKey = GetPlotSaveKey(i, PlotCropIdKey);

            if (!PlayerPrefs.HasKey(cropKey))
            {
                continue;
            }

            string cropId = PlayerPrefs.GetString(cropKey, string.Empty);

            if (string.IsNullOrEmpty(cropId))
            {
                plots[i].Clear();
                continue;
            }

            long plantedTime = ParseLong(PlayerPrefs.GetString(GetPlotSaveKey(i, PlotPlantedTimeKey), "0"));
            int growthSeconds = Mathf.Max(1, PlayerPrefs.GetInt(GetPlotSaveKey(i, PlotGrowthSecondsKey), 30));
            plots[i].SetPlanted(cropId, plantedTime, growthSeconds);
        }
    }

    private void SavePlotState(int plotIndex)
    {
        if (!persistFarmState || !IsValidPlotIndex(plotIndex))
        {
            return;
        }

        FarmPlotState plot = plots[plotIndex];

        if (plot.Phase == FarmPlotPhase.Empty)
        {
            PlayerPrefs.SetString(GetPlotSaveKey(plotIndex, PlotCropIdKey), string.Empty);
            PlayerPrefs.SetString(GetPlotSaveKey(plotIndex, PlotPlantedTimeKey), "0");
            PlayerPrefs.SetInt(GetPlotSaveKey(plotIndex, PlotGrowthSecondsKey), 0);
        }
        else
        {
            PlayerPrefs.SetString(GetPlotSaveKey(plotIndex, PlotCropIdKey), plot.CropId);
            PlayerPrefs.SetString(GetPlotSaveKey(plotIndex, PlotPlantedTimeKey), plot.PlantedUnixTime.ToString());
            PlayerPrefs.SetInt(GetPlotSaveKey(plotIndex, PlotGrowthSecondsKey), plot.GrowthSeconds);
        }

        PlayerPrefs.Save();
    }

    private string GetPlotSaveKey(int plotIndex, string suffix)
    {
        return $"{farmSaveKeyPrefix}{plotIndex}.{suffix}";
    }

    private bool IsValidPlotIndex(int plotIndex)
    {
        return plotIndex >= 0 && plotIndex < plots.Count;
    }

    private void RaisePlotChanged(int plotIndex)
    {
        PlotChanged?.Invoke(plotIndex, plots[plotIndex]);
    }

    private ResourceInventory ResolveResourceInventory()
    {
        if (resourceInventory != null)
        {
            return resourceInventory;
        }

        resourceInventory = ResourceInventory.Instance;

        if (resourceInventory == null)
        {
            resourceInventory = FindAnyObjectByType<ResourceInventory>();
        }

        if (resourceInventory == null)
        {
            GameObject inventoryObject = new GameObject("ResourceInventory");
            resourceInventory = inventoryObject.AddComponent<ResourceInventory>();
        }

        return resourceInventory;
    }

    private static long GetCurrentUnixTime()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    private static long ParseLong(string value)
    {
        return long.TryParse(value, out long result) ? result : 0;
    }

    private static string GetCropId(FarmCropKind cropKind)
    {
        switch (cropKind)
        {
            case FarmCropKind.Wheat:
                return "wheat";
            case FarmCropKind.CoffeeBean:
                return "coffee_bean";
            case FarmCropKind.Sugarcane:
                return "sugarcane";
            default:
                return string.Empty;
        }
    }
}

public enum FarmCropKind
{
    Wheat,
    CoffeeBean,
    Sugarcane
}

public enum FarmPlotPhase
{
    Empty,
    Seed,
    Growing,
    Ready
}

[Serializable]
public class FarmCropDefinition
{
    [SerializeField] private string cropId;
    [SerializeField] private string displayName;
    [SerializeField] private string outputIngredientId;
    [SerializeField] private int outputAmount = 1;
    [SerializeField] private int growthSeconds = 30;
    [SerializeField] private Sprite seedSprite;
    [SerializeField] private Sprite growingSprite;
    [SerializeField] private Sprite readySprite;

    public string CropId => cropId;
    public string DisplayName => displayName;
    public string OutputIngredientId => outputIngredientId;
    public int OutputAmount => Mathf.Max(1, outputAmount);
    public int GrowthSeconds => Mathf.Max(1, growthSeconds);
    public Sprite SeedSprite => seedSprite;
    public Sprite GrowingSprite => growingSprite;
    public Sprite ReadySprite => readySprite != null ? readySprite : growingSprite;

    public FarmCropDefinition()
    {
    }

    public FarmCropDefinition(
        string cropId,
        string displayName,
        string outputIngredientId,
        int outputAmount,
        int growthSeconds)
    {
        this.cropId = cropId;
        this.displayName = displayName;
        this.outputIngredientId = outputIngredientId;
        this.outputAmount = Mathf.Max(1, outputAmount);
        this.growthSeconds = Mathf.Max(1, growthSeconds);
    }
}

[Serializable]
public class FarmPlotState
{
    [SerializeField] private FarmPlotPhase phase = FarmPlotPhase.Empty;
    [SerializeField] private string cropId;
    [SerializeField] private long plantedUnixTime;
    [SerializeField] private int growthSeconds;

    public FarmPlotPhase Phase => phase;
    public string CropId => cropId;
    public long PlantedUnixTime => plantedUnixTime;
    public int GrowthSeconds => growthSeconds;

    public void SetPlanted(string cropId, long plantedUnixTime, int growthSeconds)
    {
        this.cropId = cropId;
        this.plantedUnixTime = plantedUnixTime;
        this.growthSeconds = Mathf.Max(1, growthSeconds);
        phase = FarmPlotPhase.Seed;
    }

    public void SetPhase(FarmPlotPhase phase)
    {
        this.phase = phase;
    }

    public void Clear()
    {
        phase = FarmPlotPhase.Empty;
        cropId = string.Empty;
        plantedUnixTime = 0;
        growthSeconds = 0;
    }
}
