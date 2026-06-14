using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CafeProductionMachineButton : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private CafeProductionPopupController popupController;
    [SerializeField] private CafeProductionMachineType machineType = CafeProductionMachineType.CoffeeMachine;

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

    public void Configure(CafeProductionPopupController controller, CafeProductionMachineType targetMachineType)
    {
        popupController = controller;
        machineType = targetMachineType;
    }

    private void NotifyController()
    {
        if (popupController != null)
        {
            popupController.SelectMachine(machineType);
        }
    }
}
