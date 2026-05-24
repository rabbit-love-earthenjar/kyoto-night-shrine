using UnityEngine;

public class PlayerJumpFlip : MonoBehaviour
{
    [SerializeField] private bool enableJumpFlip = true;
    [SerializeField] private Transform playerVisual;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerAttack playerAttack;
    [SerializeField] private float flipDuration = 0.45f;
    [SerializeField] private float jumpStartVelocityThreshold = 0.1f;

    private bool wasGrounded;
    private bool isFlipping;
    private float flipTimer;
    private float flipDirection = -1f;

    private void Awake()
    {
        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
        }

        if (playerAttack == null)
        {
            playerAttack = GetComponent<PlayerAttack>();
        }

        if (playerVisual == null)
        {
            playerVisual = transform.Find("PlayerVisual");
        }
    }

    private void Start()
    {
        wasGrounded = playerController != null && playerController.IsGrounded;
        ResetFlip();
    }

    private void Update()
    {
        if (playerVisual == null || playerController == null)
        {
            return;
        }

        if (!enableJumpFlip)
        {
            ResetFlip();
            wasGrounded = playerController.IsGrounded;
            return;
        }

        bool isGrounded = playerController.IsGrounded;

        if (isGrounded)
        {
            ResetFlip();
            wasGrounded = true;
            return;
        }

        if (wasGrounded && playerController.Velocity.y > jumpStartVelocityThreshold)
        {
            StartFlip();
        }

        if (isFlipping)
        {
            UpdateFlip();
        }

        wasGrounded = false;
    }

    private void StartFlip()
    {
        isFlipping = true;
        flipTimer = 0f;
        flipDirection = ResolveFlipDirection();
    }

    private void UpdateFlip()
    {
        flipTimer += Time.deltaTime;

        float duration = Mathf.Max(0.01f, flipDuration);
        float progress = Mathf.Clamp01(flipTimer / duration);
        float angle = 360f * progress * flipDirection;

        playerVisual.localRotation = Quaternion.Euler(0f, 0f, angle);

        if (progress >= 1f)
        {
            isFlipping = false;
            playerVisual.localRotation = Quaternion.identity;
        }
    }

    private float ResolveFlipDirection()
    {
        if (playerAttack != null)
        {
            return playerAttack.FacingDirection >= 0f ? -1f : 1f;
        }

        float velocityX = playerController.Velocity.x;

        if (Mathf.Abs(velocityX) > 0.01f)
        {
            return velocityX >= 0f ? -1f : 1f;
        }

        return -1f;
    }

    private void ResetFlip()
    {
        isFlipping = false;
        flipTimer = 0f;

        if (playerVisual != null)
        {
            playerVisual.localRotation = Quaternion.identity;
        }
    }
}
