using UnityEngine;

public static class CombatFeedbackEffects
{
    private static Sprite particleSprite;

    public static void SpawnAttackStart(Vector3 position, Vector2 attackDirection)
    {
        Color color = new Color(0.9f, 0.98f, 1f, 0.8f);
        SpawnBurst("AttackStartMotes", position, attackDirection, color, 3, 0.16f, 0.045f, 0.006f, 0.9f);
    }

    public static void SpawnGhostHit(Vector3 position, Vector2 hitDirection)
    {
        Color color = new Color(0.8f, 0.96f, 1f, 0.95f);
        SpawnBurst("GhostHitMotes", position, hitDirection, color, 4, 0.22f, 0.075f, 0.015f, 1.4f);
        CameraShake.Shake(0.025f, 0.07f);
    }

    public static void SpawnGhostVanish(Vector3 position)
    {
        Color color = new Color(0.58f, 0.88f, 1f, 0.88f);
        SpawnBurst("GhostVanishMotes", position, Vector2.up, color, 7, 0.42f, 0.09f, 0.01f, 1.15f);
        CameraShake.Shake(0.04f, 0.12f);
    }

    public static void SpawnPlayerHurt(Vector3 position, Vector2 damageDirection)
    {
        Color color = new Color(1f, 0.38f, 0.42f, 0.9f);
        SpawnBurst("PlayerHurtMotes", position, damageDirection, color, 5, 0.26f, 0.07f, 0.012f, 1.05f);
        CameraShake.Shake(0.055f, 0.14f);
    }

    private static void SpawnBurst(
        string rootName,
        Vector3 position,
        Vector2 mainDirection,
        Color color,
        int count,
        float lifetime,
        float startScale,
        float endScale,
        float speed)
    {
        Sprite sprite = GetParticleSprite();

        if (sprite == null || count <= 0)
        {
            return;
        }

        Vector2 safeDirection = mainDirection.sqrMagnitude > 0.01f ? mainDirection.normalized : Vector2.up;
        GameObject root = new GameObject(rootName);
        root.transform.position = position;
        Object.Destroy(root, Mathf.Max(0.05f, lifetime + 0.08f));

        for (int i = 0; i < count; i++)
        {
            float ratio = count > 1 ? i / (float)(count - 1) : 0f;
            float angle = Mathf.Lerp(-55f, 55f, ratio);
            Vector2 direction = Rotate(safeDirection, angle);
            float speedScale = Random.Range(0.7f, 1.18f);
            Vector3 offset = new Vector3(Random.Range(-0.05f, 0.05f), Random.Range(-0.04f, 0.06f), 0f);

            GameObject particleObject = new GameObject($"{rootName}_{i + 1:00}");
            particleObject.transform.SetParent(root.transform, false);
            particleObject.transform.position = position + offset;

            SpriteRenderer renderer = particleObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = 8;

            CombatFeedbackParticle particle = particleObject.AddComponent<CombatFeedbackParticle>();
            particle.Initialize(direction * speed * speedScale, color, lifetime * Random.Range(0.85f, 1.18f), startScale, endScale);
        }
    }

    private static Sprite GetParticleSprite()
    {
        if (particleSprite != null)
        {
            return particleSprite;
        }

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.name = "RuntimeCombatMote";
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        particleSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 16f);
        particleSprite.name = "RuntimeCombatMoteSprite";
        return particleSprite;
    }

    private static Vector2 Rotate(Vector2 vector, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);
        return new Vector2(vector.x * cos - vector.y * sin, vector.x * sin + vector.y * cos).normalized;
    }
}
