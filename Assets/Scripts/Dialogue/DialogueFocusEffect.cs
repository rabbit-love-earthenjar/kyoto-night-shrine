using UnityEngine;
using UnityEngine.SceneManagement;

// Temporary target outline drawn in code; no textures, sprites or imports change.
public sealed class DialogueFocusEffect : MonoBehaviour
{
    private Transform target;
    private Vector2 size;
    private float lifetime;
    private float age;
    private LineRenderer outline;
    private Material material;

    public static void Play(Transform target, Vector2 size, float seconds)
    {
        if (target == null) return;
        GameObject owner = new GameObject("DialogueFocusEffect");
        SceneManager.MoveGameObjectToScene(owner, target.gameObject.scene);
        DialogueFocusEffect effect = owner.AddComponent<DialogueFocusEffect>();
        effect.target = target;
        effect.size = new Vector2(Mathf.Max(0.2f, size.x), Mathf.Max(0.2f, size.y));
        effect.lifetime = Mathf.Max(0.1f, seconds);
        effect.outline = owner.AddComponent<LineRenderer>();
        effect.outline.useWorldSpace = false;
        effect.outline.loop = true;
        effect.outline.positionCount = 4;
        effect.outline.widthMultiplier = 0.045f;
        effect.material = new Material(Shader.Find("Sprites/Default"));
        effect.outline.sharedMaterial = effect.material;
        effect.outline.sortingOrder = 120;
    }

    private void Update()
    {
        if (target == null || age >= lifetime)
        {
            Destroy(gameObject);
            return;
        }
        age += Time.unscaledDeltaTime;
        transform.position = target.position;
        float pulse = 1f + 0.05f * Mathf.Sin(age * 8f);
        Vector2 half = size * (0.5f * pulse);
        outline.SetPosition(0, new Vector3(-half.x, -half.y));
        outline.SetPosition(1, new Vector3(-half.x, half.y));
        outline.SetPosition(2, new Vector3(half.x, half.y));
        outline.SetPosition(3, new Vector3(half.x, -half.y));
        Color color = new Color(1f, 0.85f, 0.4f, Mathf.Clamp01((lifetime - age) * 3f));
        outline.startColor = outline.endColor = color;
    }

    private void OnDestroy()
    {
        if (material != null) Destroy(material);
    }
}
