using UnityEngine;

[DefaultExecutionOrder(100)]
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [SerializeField] private float maxAllowedStrength = 0.12f;

    private Vector3 lastOffset;
    private float shakeTimer;
    private float shakeDuration = 0.1f;
    private float shakeStrength;

    public static void Shake(float strength, float duration)
    {
        CameraShake cameraShake = ResolveInstance();

        if (cameraShake != null)
        {
            cameraShake.AddShake(strength, duration);
        }
    }

    private static CameraShake ResolveInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        Camera mainCamera = Camera.main;

        if (mainCamera == null)
        {
            mainCamera = FindAnyObjectByType<Camera>();
        }

        if (mainCamera == null)
        {
            return null;
        }

        CameraShake cameraShake = mainCamera.GetComponent<CameraShake>();

        if (cameraShake == null)
        {
            cameraShake = mainCamera.gameObject.AddComponent<CameraShake>();
        }

        return cameraShake;
    }

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        ClearLastOffset();
    }

    private void LateUpdate()
    {
        if (shakeTimer <= 0f)
        {
            shakeStrength = 0f;
            shakeDuration = 0.1f;
            return;
        }

        shakeTimer = Mathf.Max(0f, shakeTimer - Time.unscaledDeltaTime);
        float progress = shakeDuration > 0f ? Mathf.Clamp01(shakeTimer / shakeDuration) : 0f;
        float currentStrength = shakeStrength * progress;
        Vector2 randomOffset = Random.insideUnitCircle * currentStrength;
        lastOffset = new Vector3(randomOffset.x, randomOffset.y, 0f);
        transform.position += lastOffset;
    }

    private void OnDisable()
    {
        ClearLastOffset();
    }

    private void AddShake(float strength, float duration)
    {
        float safeDuration = Mathf.Max(0.01f, duration);
        float safeStrength = Mathf.Min(Mathf.Abs(strength), Mathf.Max(0f, maxAllowedStrength));

        if (shakeTimer <= 0f)
        {
            shakeDuration = safeDuration;
            shakeTimer = safeDuration;
            shakeStrength = safeStrength;
            return;
        }

        shakeDuration = Mathf.Max(shakeDuration, safeDuration);
        shakeTimer = Mathf.Max(shakeTimer, safeDuration);
        shakeStrength = Mathf.Max(shakeStrength, safeStrength);
    }

    private void ClearLastOffset()
    {
        if (lastOffset.sqrMagnitude <= 0.000001f)
        {
            return;
        }

        transform.position -= lastOffset;
        lastOffset = Vector3.zero;
    }
}
