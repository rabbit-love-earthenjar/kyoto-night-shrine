using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class GhostEnemy : MonoBehaviour
{
    [SerializeField] private float hoverDistance = 0.45f;
    [SerializeField] private float hoverSpeed = 2f;
    [SerializeField] private float bobDistance = 0.18f;
    [SerializeField] private float bobSpeed = 3f;
    [SerializeField] private float destroyDelay = 0.05f;

    private SpriteRenderer spriteRenderer;
    private Collider2D ghostCollider;
    private Vector3 startPosition;
    private bool defeated;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        ghostCollider = GetComponent<Collider2D>();
        startPosition = transform.position;
    }

    private void Update()
    {
        if (defeated)
        {
            return;
        }

        float hoverX = Mathf.Sin(Time.time * hoverSpeed) * hoverDistance;
        float bobY = Mathf.Sin(Time.time * bobSpeed) * bobDistance;
        transform.position = startPosition + new Vector3(hoverX, bobY, 0f);
    }

    public void TakeHit()
    {
        if (defeated)
        {
            return;
        }

        defeated = true;

        if (ghostCollider != null)
        {
            ghostCollider.enabled = false;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }

        Destroy(gameObject, destroyDelay);
    }
}
