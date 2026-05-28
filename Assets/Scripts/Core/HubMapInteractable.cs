using UnityEngine;

public class HubMapInteractable : MonoBehaviour
{
    [SerializeField] private HubInteractionType interactionType;
    [SerializeField] private HubMapController hubMapController;

    private void Awake()
    {
        if (hubMapController == null)
        {
            hubMapController = FindAnyObjectByType<HubMapController>();
        }
    }

    private void OnMouseDown()
    {
        if (hubMapController == null)
        {
            return;
        }

        if (interactionType == HubInteractionType.Warehouse)
        {
            hubMapController.ShowWarehousePanel();
            return;
        }

        hubMapController.ShowShrinePanel();
    }
}

public enum HubInteractionType
{
    RuinedShrine,
    Warehouse
}
