using UnityEngine;
using UnityEngine.EventSystems;

// Special visitor shown at one fixed spot in the cafe after the Red Oni is defeated.
// It never walks or takes a seat; clicking it shows one line in the cafe info panel.
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class RedOniCafeVisitor : MonoBehaviour
{
    public const string ObjectName = "RedOniCafeVisitor";

    private CafeSceneController cafeSceneController;
    private RedOniCafeVisitConfig config;

    public static RedOniCafeVisitor SpawnIfUnlocked(CafeSceneController controller)
    {
        if (!RedOniCafeVisitConfig.IsRedOniDefeated)
        {
            return null;
        }

        GameObject visitorObject = GameObject.Find(ObjectName);

        if (visitorObject == null)
        {
            visitorObject = new GameObject(ObjectName);
        }

        RedOniCafeVisitor visitor = visitorObject.GetComponent<RedOniCafeVisitor>();

        if (visitor == null)
        {
            visitor = visitorObject.AddComponent<RedOniCafeVisitor>();
        }

        visitor.Configure(controller, RedOniCafeVisitConfig.Load());
        return visitor;
    }

    public void Configure(CafeSceneController controller, RedOniCafeVisitConfig visitConfig)
    {
        cafeSceneController = controller;
        config = visitConfig;

        Vector2 position = config.VisitorPosition;
        transform.position = new Vector3(position.x, position.y, 0f);
        transform.localScale = new Vector3(config.VisitorScale, config.VisitorScale, 1f);

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = config.VisitorSprite;
        spriteRenderer.sortingOrder = config.VisitorSortingOrder;

        // Trigger-only click area matching the sprite, so the visitor never blocks the player.
        BoxCollider2D clickArea = GetComponent<BoxCollider2D>();
        clickArea.isTrigger = true;

        if (config.VisitorSprite != null)
        {
            clickArea.offset = config.VisitorSprite.bounds.center;
            clickArea.size = config.VisitorSprite.bounds.size;
        }
        else
        {
            Debug.LogWarning("RedOniCafeVisitConfig has no visitor sprite; the Red Oni is clickable but invisible.", this);
            clickArea.offset = new Vector2(0f, 1.5f);
            clickArea.size = new Vector2(2.5f, 3f);
        }
    }

    private void OnMouseDown()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (cafeSceneController == null)
        {
            cafeSceneController = FindAnyObjectByType<CafeSceneController>();
        }

        if (cafeSceneController != null && config != null)
        {
            cafeSceneController.ShowVisitorLine(config.VisitorDisplayName, config.VisitorLine);
        }
    }
}
