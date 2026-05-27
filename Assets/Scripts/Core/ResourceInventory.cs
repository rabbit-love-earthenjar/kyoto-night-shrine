using System.Collections.Generic;
using UnityEngine;

public class ResourceInventory : MonoBehaviour
{
    public const string BasicYokaiMaterialId = "BasicYokaiMaterial";

    public static ResourceInventory Instance { get; private set; }

    [SerializeField] private int faithPoints;
    [SerializeField] private bool persistAcrossScenes = true;
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
