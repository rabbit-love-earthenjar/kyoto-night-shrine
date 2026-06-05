using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteFrameAnimator : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField, Min(0.1f)] private float framesPerSecond = 6f;
    [SerializeField] private bool playOnAwake = true;
    [SerializeField] private bool loop = true;

    private SpriteRenderer spriteRenderer;
    private int frameIndex;
    private float timer;
    private bool isPlaying;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        ResetAnimation();
        isPlaying = playOnAwake && HasFrames;
    }

    private void Update()
    {
        if (!isPlaying || frames.Length <= 1)
        {
            return;
        }

        timer += Time.deltaTime;
        float frameDuration = 1f / Mathf.Max(0.1f, framesPerSecond);

        while (timer >= frameDuration)
        {
            timer -= frameDuration;
            AdvanceFrame();
        }
    }

    public void Play()
    {
        isPlaying = HasFrames;
    }

    public void Stop()
    {
        isPlaying = false;
    }

    public void ResetAnimation()
    {
        frameIndex = 0;
        timer = 0f;

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (HasFrames && spriteRenderer != null)
        {
            spriteRenderer.sprite = frames[0];
        }
    }

    private void AdvanceFrame()
    {
        frameIndex++;

        if (frameIndex >= frames.Length)
        {
            if (!loop)
            {
                frameIndex = frames.Length - 1;
                isPlaying = false;
            }
            else
            {
                frameIndex = 0;
            }
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = frames[frameIndex];
        }
    }

    private bool HasFrames => frames != null && frames.Length > 0;

    private void OnValidate()
    {
        framesPerSecond = Mathf.Max(0.1f, framesPerSecond);

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (HasFrames && spriteRenderer != null && spriteRenderer.sprite == null)
        {
            spriteRenderer.sprite = frames[0];
        }
    }
}
