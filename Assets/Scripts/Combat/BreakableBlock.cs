using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BreakableBlock : MonoBehaviour
{
    private enum RewardDropType
    {
        None,
        FaithPoint,
        Heart
    }

    [SerializeField] private int maxHP = 1;
    [SerializeField] private int faithPointReward = 1;
    [SerializeField] private int healReward;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private Color hitFlashColor = new Color(1f, 0.9f, 0.55f, 1f);
    [SerializeField] private float hitFlashDuration = 0.08f;
    [SerializeField] private float hitShakeDuration = 0.12f;
    [SerializeField] private float hitShakeDistance = 0.075f;
    [SerializeField] private float hitScalePunch = 0.08f;
    [SerializeField] private float breakDestroyDelay = 0.02f;
    [SerializeField] private RewardDropType dropRewardType;
    [SerializeField] private int dropRewardAmount = 1;
    [SerializeField] private GameObject dropPrefab;
    [SerializeField] private Sprite dropSprite;
    [SerializeField] private Vector2 dropOffset = new Vector2(0f, 0.32f);
    [SerializeField] private float runtimeDropScale = 0.28f;

    private SpriteRenderer spriteRenderer;
    private Collider2D blockCollider;
    private Color originalColor = Color.white;
    private Vector3 originalLocalPosition;
    private Vector3 originalLocalScale;
    private int currentHP;
    private bool broken;
    private Coroutine hitFlashRoutine;
    private Coroutine hitShakeRoutine;
    private static Sprite runtimeFaithPointSprite;
    private static Sprite runtimeHeartSprite;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        blockCollider = GetComponent<Collider2D>();
        originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        originalLocalPosition = transform.localPosition;
        originalLocalScale = transform.localScale;
        currentHP = Mathf.Max(1, maxHP);

        if (gameManager == null)
        {
            gameManager = FindAnyObjectByType<GameManager>();
        }
    }

    public void Configure(int hitPoints, int faithReward, int healRewardAmount, GameManager targetGameManager)
    {
        maxHP = Mathf.Max(1, hitPoints);
        currentHP = maxHP;
        faithPointReward = Mathf.Max(0, faithReward);
        healReward = Mathf.Max(0, healRewardAmount);
        gameManager = targetGameManager;
        dropRewardType = RewardDropType.None;
        dropRewardAmount = 1;
        broken = false;
    }

    public void ConfigureFaithPointDrop(int hitPoints, int amount, GameManager targetGameManager)
    {
        Configure(hitPoints, 0, 0, targetGameManager);
        dropRewardType = RewardDropType.FaithPoint;
        dropRewardAmount = Mathf.Max(1, amount);
    }

    public void ConfigureHeartDrop(int hitPoints, int amount, GameManager targetGameManager)
    {
        Configure(hitPoints, 0, 0, targetGameManager);
        dropRewardType = RewardDropType.Heart;
        dropRewardAmount = Mathf.Max(1, amount);
    }

    public void TakeDamage(int damage)
    {
        TakeDamage(damage, transform.position);
    }

    public void TakeDamage(int damage, Vector2 attackerPosition)
    {
        if (broken)
        {
            return;
        }

        currentHP -= Mathf.Max(1, damage);
        Vector2 hitDirection = ((Vector2)transform.position - attackerPosition).normalized;

        if (hitDirection.sqrMagnitude < 0.01f)
        {
            hitDirection = Vector2.up;
        }

        ApplyHitFeedback(hitDirection);

        if (currentHP <= 0)
        {
            Break(hitDirection);
        }
    }

    private void ApplyHitFeedback(Vector2 hitDirection)
    {
        CombatFeedbackEffects.SpawnBreakableHit(transform.position, hitDirection);

        if (spriteRenderer == null || hitFlashDuration <= 0f)
        {
            return;
        }

        if (hitFlashRoutine != null)
        {
            StopCoroutine(hitFlashRoutine);
        }

        hitFlashRoutine = StartCoroutine(HitFlashRoutine());

        if (hitShakeDuration > 0f && hitShakeDistance > 0f)
        {
            if (hitShakeRoutine != null)
            {
                StopCoroutine(hitShakeRoutine);
            }

            hitShakeRoutine = StartCoroutine(HitShakeRoutine(hitDirection));
        }
    }

    private IEnumerator HitFlashRoutine()
    {
        spriteRenderer.color = hitFlashColor;
        yield return new WaitForSecondsRealtime(hitFlashDuration);

        if (!broken && spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        hitFlashRoutine = null;
    }

    private IEnumerator HitShakeRoutine(Vector2 hitDirection)
    {
        Vector3 shakeDirection = new Vector3(hitDirection.x, hitDirection.y, 0f);

        if (shakeDirection.sqrMagnitude < 0.01f)
        {
            shakeDirection = Vector3.up;
        }

        float elapsed = 0f;

        while (elapsed < hitShakeDuration && !broken)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / hitShakeDuration);
            float wave = Mathf.Sin(progress * Mathf.PI * 4f) * (1f - progress);
            transform.localPosition = originalLocalPosition + shakeDirection.normalized * hitShakeDistance * wave;
            transform.localScale = originalLocalScale * (1f + Mathf.Max(0f, hitScalePunch) * (1f - progress));
            yield return null;
        }

        if (!broken)
        {
            transform.localPosition = originalLocalPosition;
            transform.localScale = originalLocalScale;
        }

        hitShakeRoutine = null;
    }

    private void Break(Vector2 hitDirection)
    {
        broken = true;
        CombatFeedbackEffects.SpawnBreakableBreak(transform.position, hitDirection);

        bool spawnedDrop = TrySpawnConfiguredDrop();

        if (!spawnedDrop && gameManager != null && faithPointReward > 0)
        {
            gameManager.AddFaithPoints(faithPointReward);
            CombatFeedbackEffects.SpawnPickupDrop(transform.position + Vector3.up * 0.25f);
        }

        if (!spawnedDrop && healReward > 0)
        {
            PlayerHealth playerHealth = FindAnyObjectByType<PlayerHealth>();

            if (playerHealth != null)
            {
                playerHealth.Heal(healReward);
            }

            CombatFeedbackEffects.SpawnPickupDrop(transform.position + Vector3.up * 0.3f);
        }

        if (blockCollider != null)
        {
            blockCollider.enabled = false;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        Destroy(gameObject, Mathf.Max(0f, breakDestroyDelay));
    }

    private bool TrySpawnConfiguredDrop()
    {
        if (dropRewardType == RewardDropType.None || dropRewardAmount <= 0)
        {
            return false;
        }

        Vector3 dropPosition = transform.position + (Vector3)dropOffset;
        GameObject dropObject = dropPrefab != null
            ? Instantiate(dropPrefab, dropPosition, Quaternion.identity)
            : CreateRuntimeDropObject(dropPosition);

        if (dropObject == null)
        {
            return false;
        }

        PickupItem pickupItem = dropObject.GetComponent<PickupItem>();

        if (pickupItem == null)
        {
            pickupItem = dropObject.AddComponent<PickupItem>();
        }

        if (dropRewardType == RewardDropType.FaithPoint)
        {
            pickupItem.ConfigureFaithPoint(gameManager != null ? gameManager : FindAnyObjectByType<GameManager>(), dropRewardAmount);
        }
        else if (dropRewardType == RewardDropType.Heart)
        {
            pickupItem.ConfigureHeart(dropRewardAmount);
        }

        CombatFeedbackEffects.SpawnPickupDrop(dropPosition);
        return true;
    }

    private GameObject CreateRuntimeDropObject(Vector3 dropPosition)
    {
        GameObject dropObject = new GameObject(dropRewardType == RewardDropType.Heart ? "HeartDrop_Runtime" : "FaithPointDrop_Runtime");
        dropObject.transform.position = dropPosition;
        dropObject.transform.localScale = Vector3.one * Mathf.Max(0.05f, runtimeDropScale);

        SpriteRenderer renderer = dropObject.AddComponent<SpriteRenderer>();
        renderer.sprite = dropSprite != null ? dropSprite : GetRuntimeDropSprite(dropRewardType);
        renderer.color = Color.white;
        renderer.sortingOrder = 7;

        BoxCollider2D collider = dropObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = Vector2.one;

        return dropObject;
    }

    private static Sprite GetRuntimeDropSprite(RewardDropType rewardType)
    {
        if (rewardType == RewardDropType.Heart)
        {
            if (runtimeHeartSprite == null)
            {
                runtimeHeartSprite = CreateRuntimeShapeSprite("RuntimeHeartDropSprite", new Color(1f, 0.22f, 0.38f, 1f), true);
            }

            return runtimeHeartSprite;
        }

        if (runtimeFaithPointSprite == null)
        {
            runtimeFaithPointSprite = CreateRuntimeShapeSprite("RuntimeFaithPointDropSprite", new Color(0.34f, 0.88f, 1f, 1f), false);
        }

        return runtimeFaithPointSprite;
    }

    private static Sprite CreateRuntimeShapeSprite(string spriteName, Color color, bool heartShape)
    {
        const int size = 16;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = spriteName;
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool filled = heartShape ? IsHeartPixel(x, y, size) : IsDiamondPixel(x, y, size);
                texture.SetPixel(x, y, filled ? color : Color.clear);
            }
        }

        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        sprite.name = spriteName;
        return sprite;
    }

    private static bool IsDiamondPixel(int x, int y, int size)
    {
        float center = (size - 1) * 0.5f;
        return Mathf.Abs(x - center) + Mathf.Abs(y - center) <= center * 0.72f;
    }

    private static bool IsHeartPixel(int x, int y, int size)
    {
        float nx = (x - (size - 1) * 0.5f) / ((size - 1) * 0.5f);
        float ny = (y - (size - 1) * 0.5f) / ((size - 1) * 0.5f);
        ny = -ny + 0.12f;
        float value = nx * nx + ny * ny - 0.32f;
        return value * value * value - nx * nx * ny * ny * ny <= 0f;
    }
}
