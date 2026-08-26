using UnityEngine;

public static class FaithBeanVfx
{
    private static Material particleMaterial;
    private static Texture2D particleTexture;

    public static void SpawnLaunch(Vector2 position, Vector2 direction)
    {
        ParticleSystem effect = CreateEffect("FaithBeanLaunchVfx", position, 18);
        effect.transform.rotation = Quaternion.Euler(
            0f,
            0f,
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

        ParticleSystem.MainModule main = effect.main;
        main.duration = 0.18f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 3.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.24f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 1f, 0.72f, 1f),
            new Color(1f, 0.48f, 0.05f, 0.9f));

        ParticleSystem.ShapeModule shape = effect.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 18f;
        shape.radius = 0.08f;

        ConfigureBurst(effect, 14);
        effect.Play();
    }

    public static void SpawnImpact(Vector2 position, float scale)
    {
        ParticleSystem effect = CreateEffect("FaithBeanImpactVfx", position, 30);
        effect.transform.localScale = Vector3.one * Mathf.Max(0.1f, scale);

        ParticleSystem.MainModule main = effect.main;
        main.duration = 0.28f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.48f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.8f, 4.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.38f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 1f, 0.82f, 1f),
            new Color(1f, 0.42f, 0.02f, 0.95f));

        ParticleSystem.ShapeModule shape = effect.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.18f;
        shape.radiusThickness = 1f;

        ConfigureBurst(effect, 24);
        effect.Play();
    }

    private static ParticleSystem CreateEffect(string objectName, Vector2 position, int maxParticles)
    {
        GameObject effectObject = new GameObject(objectName);
        effectObject.transform.position = position;
        ParticleSystem effect = effectObject.AddComponent<ParticleSystem>();
        effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = effect.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = maxParticles;
        main.stopAction = ParticleSystemStopAction.Destroy;

        ParticleSystem.EmissionModule emission = effect.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = effect.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(new Color(1f, 0.55f, 0.08f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.8f, 0.45f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;

        ParticleSystemRenderer renderer = effect.GetComponent<ParticleSystemRenderer>();
        renderer.material = GetParticleMaterial();
        renderer.sortingOrder = 26;
        return effect;
    }

    private static void ConfigureBurst(ParticleSystem effect, short count)
    {
        ParticleSystem.EmissionModule emission = effect.emission;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, count)
        });
    }

    private static Material GetParticleMaterial()
    {
        if (particleMaterial != null)
        {
            return particleMaterial;
        }

        Shader shader = Shader.Find("Sprites/Default");

        if (shader == null)
        {
            return null;
        }

        particleMaterial = new Material(shader)
        {
            name = "RuntimeFaithBeanParticleMaterial",
            hideFlags = HideFlags.HideAndDontSave,
            mainTexture = GetParticleTexture()
        };
        return particleMaterial;
    }

    private static Texture2D GetParticleTexture()
    {
        if (particleTexture != null)
        {
            return particleTexture;
        }

        const int size = 16;
        particleTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "RuntimeFaithBeanParticle",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / (size * 0.5f);
                float alpha = Mathf.Clamp01(1f - distance);
                particleTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
            }
        }

        particleTexture.Apply(false, true);
        return particleTexture;
    }
}
