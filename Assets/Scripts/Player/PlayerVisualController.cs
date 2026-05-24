using UnityEngine;

public class PlayerVisualController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite standSprite;
    [SerializeField] private Sprite runSprite;
    [SerializeField] private Sprite jumpSprite;
    [SerializeField] private Sprite attackSprite;
    [SerializeField] private bool spriteFacesRight;
    [SerializeField] private float runVelocityThreshold = 0.15f;
    [SerializeField] private float jumpVelocityThreshold = 0.12f;

    private PlayerController playerController;
    private PlayerAttack playerAttack;
    private float facingDirection = 1f;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        playerAttack = GetComponent<PlayerAttack>();

        if (spriteRenderer == null || !spriteRenderer.enabled)
        {
            spriteRenderer = FindActiveSpriteRenderer();
        }
    }

    private void Update()
    {
        if (spriteRenderer == null || playerController == null)
        {
            return;
        }

        Vector2 velocity = playerController.Velocity;
        UpdateFacingDirection(velocity.x);
        spriteRenderer.flipX = spriteFacesRight ? facingDirection < 0f : facingDirection > 0f;
        spriteRenderer.sprite = PickSprite(velocity);
    }

    private void UpdateFacingDirection(float velocityX)
    {
        if (playerAttack != null)
        {
            facingDirection = playerAttack.FacingDirection;
            return;
        }

        if (velocityX > runVelocityThreshold)
        {
            facingDirection = 1f;
        }
        else if (velocityX < -runVelocityThreshold)
        {
            facingDirection = -1f;
        }
    }

    private Sprite PickSprite(Vector2 velocity)
    {
        if (playerAttack != null && playerAttack.IsAttacking && attackSprite != null)
        {
            return attackSprite;
        }

        if (!playerController.IsGrounded && Mathf.Abs(velocity.y) > jumpVelocityThreshold && jumpSprite != null)
        {
            return jumpSprite;
        }

        if (Mathf.Abs(velocity.x) > runVelocityThreshold && runSprite != null)
        {
            return runSprite;
        }

        return standSprite != null ? standSprite : spriteRenderer.sprite;
    }

    private SpriteRenderer FindActiveSpriteRenderer()
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();

        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer != null && renderer.enabled)
            {
                return renderer;
            }
        }

        return GetComponent<SpriteRenderer>();
    }
}
