using UnityEngine;

public class CafeInteractable : MonoBehaviour
{
    [SerializeField] private CafeInteractionType interactionType;
    [SerializeField] private CafeSceneController cafeSceneController;

    public void Configure(CafeSceneController controller, CafeInteractionType type)
    {
        cafeSceneController = controller;
        interactionType = type;
    }

    private void OnMouseDown()
    {
        if (cafeSceneController == null)
        {
            cafeSceneController = FindAnyObjectByType<CafeSceneController>();
        }

        if (cafeSceneController == null)
        {
            return;
        }

        switch (interactionType)
        {
            case CafeInteractionType.FoxAltar:
                cafeSceneController.ShowFoxAltarPanel();
                break;
            case CafeInteractionType.FrontCounter:
                cafeSceneController.ShowReceptionPanel();
                break;
            case CafeInteractionType.MenuBoard:
                cafeSceneController.ShowMenuBoardPanel();
                break;
        }
    }
}

public enum CafeInteractionType
{
    FoxAltar,
    FrontCounter,
    MenuBoard
}
