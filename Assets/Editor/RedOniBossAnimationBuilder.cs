using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class RedOniBossAnimationBuilder
{
    private const string BossArtFolder = "Assets/Art/boss";
    private const string HighSourcePath = BossArtFolder + "/flip_up.png";
    private const string MiddleSourcePath = BossArtFolder + "/flip_middle.png";
    private const string LowSourcePath = BossArtFolder + "/flip_down.png";

    private const string IdleClipPath = BossArtFolder + "/RedOni_Idle.anim";
    private const string HighClipPath = BossArtFolder + "/RedOni_Attack_High.anim";
    private const string MiddleClipPath = BossArtFolder + "/RedOni_Attack_Middle.anim";
    private const string LowClipPath = BossArtFolder + "/RedOni_Attack_Low.anim";
    private const string ControllerPath = BossArtFolder + "/RedOni_Phase1.controller";
    private const string PrefabPath = BossArtFolder + "/RedOni_Phase1_Visual.prefab";

    private const float TargetFrameWorldHeight = 2.56f;
    private const float AttackFrameRate = 12f;

    [MenuItem("Tools/Kyoto Night Shrine/Boss/Build Red Oni Phase 1 Animations")]
    public static void BuildPhaseOneAnimations()
    {
        ConfigureSpriteSheet(HighSourcePath, 5, 3, "RedOni_Attack_High");
        ConfigureSpriteSheet(MiddleSourcePath, 5, 4, "RedOni_Attack_Middle");
        ConfigureSpriteSheet(LowSourcePath, 6, 3, "RedOni_Attack_Low");

        Sprite[] highFrames = LoadFrames(HighSourcePath, 15);
        Sprite[] middleFrames = LoadFrames(MiddleSourcePath, 20);
        Sprite[] lowFrames = LoadFrames(LowSourcePath, 18);
        Sprite idleSprite = middleFrames[0];

        AnimationClip idleClip = CreateOrUpdateClip(IdleClipPath, new[] { idleSprite }, 1f, true);
        AnimationClip highClip = CreateOrUpdateClip(HighClipPath, highFrames, AttackFrameRate, false);
        AnimationClip middleClip = CreateOrUpdateClip(MiddleClipPath, middleFrames, AttackFrameRate, false);
        AnimationClip lowClip = CreateOrUpdateClip(LowClipPath, lowFrames, AttackFrameRate, false);
        AnimatorController controller = CreateController(idleClip, highClip, middleClip, lowClip);

        CreateVisualPrefab(idleSprite, controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ValidatePhaseOneAnimations();
        Debug.Log("Red Oni Phase 1 animation package built in Assets/Art/boss.");
    }

    public static void ValidatePhaseOneAnimations()
    {
        List<string> failures = new List<string>();

        ValidateClip(IdleClipPath, 1, true, failures);
        ValidateClip(HighClipPath, 15, false, failures);
        ValidateClip(MiddleClipPath, 20, false, failures);
        ValidateClip(LowClipPath, 18, false, failures);

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);

        if (controller == null)
        {
            failures.Add("RedOni_Phase1.controller is missing.");
        }
        else
        {
            string[] requiredTriggers = { "AttackHigh", "AttackMiddle", "AttackLow" };

            foreach (string trigger in requiredTriggers)
            {
                bool exists = controller.parameters.Any(
                    parameter => parameter.name == trigger && parameter.type == AnimatorControllerParameterType.Trigger);

                if (!exists)
                {
                    failures.Add($"Animator trigger {trigger} is missing.");
                }
            }

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            string[] requiredStates = { "Idle", "AttackHigh", "AttackMiddle", "AttackLow" };

            foreach (string stateName in requiredStates)
            {
                if (!stateMachine.states.Any(child => child.state.name == stateName))
                {
                    failures.Add($"Animator state {stateName} is missing.");
                }
            }
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

        if (prefab == null
            || prefab.GetComponent<SpriteRenderer>() == null
            || prefab.GetComponent<Animator>() == null)
        {
            failures.Add("RedOni_Phase1_Visual.prefab is missing its visual components.");
        }

        if (failures.Count > 0)
        {
            throw new InvalidOperationException(
                "Red Oni Phase 1 animation validation failed:\n- " + string.Join("\n- ", failures));
        }

        Debug.Log(
            "Red Oni Phase 1 animation validation passed: Idle=1 frame, High=15 frames, "
            + "Middle=20 frames, Low=18 frames, with three attack triggers and a visual prefab.");
    }

    private static void ConfigureSpriteSheet(string assetPath, int columns, int rows, string framePrefix)
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);

        if (texture == null)
        {
            throw new InvalidOperationException($"Red Oni source texture is missing: {assetPath}");
        }

        TextureImporter importer = GetTextureImporter(assetPath);
        ApplySharedTextureSettings(importer);
        importer.spriteImportMode = SpriteImportMode.Multiple;
        // The source sheets use different cell heights (256 px versus about
        // 341 px). Normalize their world height so attacks do not make the Oni
        // jump in size or appear to overlap the previous pose.
        float frameHeightPixels = texture.height / (float)rows;
        importer.spritePixelsPerUnit = frameHeightPixels / TargetFrameWorldHeight;

        List<SpriteMetaData> sprites = new List<SpriteMetaData>(columns * rows);
        int frameIndex = 0;

        for (int row = 0; row < rows; row++)
        {
            int top = Mathf.RoundToInt(texture.height * (1f - row / (float)rows));
            int bottom = Mathf.RoundToInt(texture.height * (1f - (row + 1) / (float)rows));

            for (int column = 0; column < columns; column++)
            {
                int left = Mathf.RoundToInt(texture.width * (column / (float)columns));
                int right = Mathf.RoundToInt(texture.width * ((column + 1) / (float)columns));

                sprites.Add(new SpriteMetaData
                {
                    name = $"{framePrefix}_{frameIndex:00}",
                    rect = new Rect(left, bottom, right - left, top - bottom),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f)
                });

                frameIndex++;
            }
        }

#pragma warning disable CS0618
        importer.spritesheet = sprites.ToArray();
#pragma warning restore CS0618
        importer.SaveAndReimport();
    }

    private static void ApplySharedTextureSettings(TextureImporter importer)
    {
        importer.textureType = TextureImporterType.Sprite;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;
    }

    private static TextureImporter GetTextureImporter(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;

        if (importer == null)
        {
            throw new InvalidOperationException($"TextureImporter is unavailable for {assetPath}.");
        }

        return importer;
    }

    private static Sprite[] LoadFrames(string assetPath, int expectedCount)
    {
        Sprite[] frames = AssetDatabase.LoadAllAssetsAtPath(assetPath)
            .OfType<Sprite>()
            .OrderBy(sprite => sprite.name, StringComparer.Ordinal)
            .ToArray();

        if (frames.Length != expectedCount)
        {
            throw new InvalidOperationException(
                $"Expected {expectedCount} frames in {assetPath}, but Unity imported {frames.Length}.");
        }

        return frames;
    }

    private static AnimationClip CreateOrUpdateClip(
        string assetPath,
        IReadOnlyList<Sprite> frames,
        float frameRate,
        bool loop)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);

        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, assetPath);
        }

        clip.name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        clip.frameRate = frameRate;

        EditorCurveBinding binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = string.Empty,
            propertyName = "m_Sprite"
        };

        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[frames.Count];

        for (int index = 0; index < frames.Count; index++)
        {
            keyframes[index] = new ObjectReferenceKeyframe
            {
                time = index / frameRate,
                value = frames[index]
            };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static AnimatorController CreateController(
        AnimationClip idleClip,
        AnimationClip highClip,
        AnimationClip middleClip,
        AnimationClip lowClip)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);

        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        }
        else
        {
            while (controller.parameters.Length > 0)
            {
                controller.RemoveParameter(0);
            }

            AnimatorStateMachine existingStateMachine = controller.layers[0].stateMachine;

            foreach (AnimatorStateTransition transition in existingStateMachine.anyStateTransitions.ToArray())
            {
                existingStateMachine.RemoveAnyStateTransition(transition);
            }

            foreach (ChildAnimatorState childState in existingStateMachine.states.ToArray())
            {
                existingStateMachine.RemoveState(childState.state);
            }
        }

        controller.AddParameter("AttackHigh", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("AttackMiddle", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("AttackLow", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState idle = stateMachine.AddState("Idle", new Vector3(200f, 80f));
        AnimatorState high = stateMachine.AddState("AttackHigh", new Vector3(470f, 10f));
        AnimatorState middle = stateMachine.AddState("AttackMiddle", new Vector3(470f, 100f));
        AnimatorState low = stateMachine.AddState("AttackLow", new Vector3(470f, 190f));

        idle.motion = idleClip;
        high.motion = highClip;
        middle.motion = middleClip;
        low.motion = lowClip;
        stateMachine.defaultState = idle;

        AddTriggeredAttack(idle, high, "AttackHigh");
        AddTriggeredAttack(idle, middle, "AttackMiddle");
        AddTriggeredAttack(idle, low, "AttackLow");
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static void AddTriggeredAttack(
        AnimatorState idle,
        AnimatorState attack,
        string triggerName)
    {
        AnimatorStateTransition enter = idle.AddTransition(attack);
        enter.hasExitTime = false;
        enter.duration = 0f;
        enter.AddCondition(AnimatorConditionMode.If, 0f, triggerName);

        AnimatorStateTransition exit = attack.AddTransition(idle);
        exit.hasExitTime = true;
        exit.exitTime = 1f;
        exit.duration = 0f;
    }

    private static void CreateVisualPrefab(Sprite idleSprite, RuntimeAnimatorController controller)
    {
        GameObject root = new GameObject("RedOni_Phase1_Visual");
        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = idleSprite;
        renderer.sortingOrder = 5;

        Animator animator = root.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void ValidateClip(
        string clipPath,
        int expectedFrames,
        bool expectedLoop,
        ICollection<string> failures)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);

        if (clip == null)
        {
            failures.Add($"{clipPath} is missing.");
            return;
        }

        EditorCurveBinding binding = AnimationUtility.GetObjectReferenceCurveBindings(clip)
            .FirstOrDefault(candidate => candidate.propertyName == "m_Sprite");
        ObjectReferenceKeyframe[] frames = AnimationUtility.GetObjectReferenceCurve(clip, binding);

        if (frames == null || frames.Length != expectedFrames)
        {
            failures.Add($"{clip.name} has {frames?.Length ?? 0} frames; expected {expectedFrames}.");
        }

        bool loops = AnimationUtility.GetAnimationClipSettings(clip).loopTime;

        if (loops != expectedLoop)
        {
            failures.Add($"{clip.name} loop setting is {loops}; expected {expectedLoop}.");
        }
    }
}
