using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CafeRecipeButton : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private CafeProductionPopupController popupController;
    [SerializeField] private string recipeId = "InariCoffee";

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.AddListener(NotifyController);
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(NotifyController);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (button == null)
        {
            NotifyController();
        }
    }

    public void Configure(CafeProductionPopupController controller, string targetRecipeId)
    {
        popupController = controller;
        recipeId = targetRecipeId;
    }

    private void NotifyController()
    {
        if (popupController != null)
        {
            popupController.StartRecipe(recipeId);
        }
    }
}
