using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SecondCrossingPlatform : MonoBehaviour
{
    private Collider2D platformCollider;
    private SpriteRenderer spriteRenderer;
    private Vector3 startPosition;
    private Color normalColor = Color.white;
    private int separateEntries;
    private bool playerInside;
    private bool collapsed;

    private void Awake()
    {
        platformCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        startPosition = transform.position;

        if (spriteRenderer != null)
        {
            normalColor = spriteRenderer.color;
        }
    }

    private void OnEnable()
    {
        GameManager.RetryCompleted += ResetPlatform;
    }

    private void OnDisable()
    {
        GameManager.RetryCompleted -= ResetPlatform;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collapsed || playerInside || collision.collider.GetComponentInParent<PlayerController>() == null)
        {
            return;
        }

        playerInside = true;
        separateEntries++;

        if (separateEntries >= 2)
        {
            CollapseImmediately();
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider.GetComponentInParent<PlayerController>() != null)
        {
            playerInside = false;
        }
    }

    private void CollapseImmediately()
    {
        collapsed = true;

        if (platformCollider != null)
        {
            platformCollider.enabled = false;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
            spriteRenderer.color = normalColor;
        }
    }

    private void ResetPlatform()
    {
        separateEntries = 0;
        playerInside = false;
        collapsed = false;
        transform.position = startPosition;

        if (platformCollider != null)
        {
            platformCollider.enabled = true;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.color = normalColor;
        }
    }
}
