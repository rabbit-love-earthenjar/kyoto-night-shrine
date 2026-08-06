using UnityEngine;

[DisallowMultipleComponent]
public class RedOniVisualHeightNormalizer : MonoBehaviour
{
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField, Min(0.1f)] private float targetWorldHeight = 6.65f;

    private Sprite lastSprite;

    public void Configure(SpriteRenderer renderer, float worldHeight)
    {
        targetRenderer = renderer;
        targetWorldHeight = Mathf.Max(0.1f, worldHeight);
        RefreshScale();
    }

    private void Awake()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<SpriteRenderer>();
        }

        RefreshScale();
    }

    private void LateUpdate()
    {
        if (targetRenderer != null && targetRenderer.sprite != lastSprite)
        {
            RefreshScale();
        }
    }

    private void RefreshScale()
    {
        if (targetRenderer == null || targetRenderer.sprite == null)
        {
            return;
        }

        float spriteHeight = targetRenderer.sprite.bounds.size.y;

        if (spriteHeight <= 0.001f)
        {
            return;
        }

        float scale = targetWorldHeight / spriteHeight;
        transform.localScale = new Vector3(scale, scale, 1f);
        lastSprite = targetRenderer.sprite;
    }
}
