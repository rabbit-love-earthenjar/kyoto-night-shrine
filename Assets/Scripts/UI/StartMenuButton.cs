using UnityEngine;
using UnityEngine.EventSystems;

public class StartMenuButton : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField] private StartScreenController controller;
    [SerializeField, Min(0)] private int menuIndex;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (controller != null)
        {
            controller.SelectMenuItem(menuIndex);
        }
    }
}
