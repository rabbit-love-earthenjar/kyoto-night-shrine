using UnityEngine;

// Position/completion cues live in the scene copies; story text lives in assets.
public sealed class DesignLabDialogueCue : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private DialogueSequence sequence;
    [SerializeField] private Vector2 triggerMinimum;
    [SerializeField] private Vector2 triggerMaximum;
    [SerializeField] private DesignLabEncounter encounter;
    [SerializeField] private bool triggerAfterEncounter;
    [SerializeField] private Transform focusTarget;
    [SerializeField] private Vector2 focusSize = new Vector2(3f, 3f);
    [SerializeField, Min(0.1f)] private float focusDuration = 2f;
    [SerializeField] private bool requireGrounded = true;
    [SerializeField] private bool oncePerScene = true;

    public bool Triggered { get; private set; }
    public DialogueSequence Sequence => sequence;
    private bool consumedInArea;

    private void Awake()
    {
        enabled = gameObject.scene.name.EndsWith("_DesignLab");
        if (player == null) player = FindAnyObjectByType<PlayerController>();
    }

    private void Update()
    {
        if (player != null && !Contains(player.transform.position)) consumedInArea = false;
        TryTrigger();
    }

    private bool Contains(Vector2 position) => position.x >= triggerMinimum.x
        && position.x <= triggerMaximum.x && position.y >= triggerMinimum.y
        && position.y <= triggerMaximum.y;

    public bool TryTrigger()
    {
        if (!isActiveAndEnabled || (oncePerScene ? Triggered : consumedInArea) || sequence == null
            || !sequence.HasContent || player == null || !player.ControlsEnabled
            || (requireGrounded && (!player.IsGrounded || Mathf.Abs(player.Velocity.y) > 0.1f))
            || (triggerAfterEncounter && (encounter == null || !encounter.Completed))) return false;

        if (!Contains(player.transform.position)) return false;
        if (!DialoguePresenter.ForScene(gameObject.scene).Show(sequence, player)) return false;
        Triggered = true;
        consumedInArea = true;
        if (focusTarget != null)
            DialogueFocusEffect.Play(focusTarget, focusSize, focusDuration);
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.4f, 0.8f);
        Vector2 center = (triggerMinimum + triggerMaximum) * 0.5f;
        Gizmos.DrawWireCube(center, triggerMaximum - triggerMinimum);
    }
}
