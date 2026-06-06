using UnityEngine;
using UnityEngine.EventSystems;

public class HubMapInteractable : MonoBehaviour
{
    [SerializeField] private HubInteractionType interactionType;
    [SerializeField] private HubMapController hubMapController;

    public void Configure(HubMapController controller, HubInteractionType type)
    {
        hubMapController = controller;
        interactionType = type;
    }

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

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (hubMapController.BlocksHubInteraction)
        {
            return;
        }

        if (interactionType == HubInteractionType.Warehouse)
        {
            hubMapController.ShowWarehousePanel();
            return;
        }

        if (interactionType == HubInteractionType.NightPatrol)
        {
            hubMapController.EnterNight();
            return;
        }

        if (interactionType == HubInteractionType.IngredientShop)
        {
            hubMapController.ShowIngredientShopPanel();
            return;
        }

        hubMapController.ShowShrinePanel();
    }
}

public enum HubInteractionType
{
    RuinedShrine,
    Warehouse,
    NightPatrol,
    IngredientShop
}
