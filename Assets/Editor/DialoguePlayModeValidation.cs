using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Runs against saved scenes but authors only transient runtime test text. Direct
// progression, teleportation and enemy defeat are integration checks, not playtests.
[InitializeOnLoad]
public static class DialoguePlayModeValidation
{
    private const string Key = "Dialogue.Validation.";
    private const string LogDirectory = "Logs/DialogueValidation";
    private const string FirstText = "Validation line one: reveal, advance and restore controls.";
    private const string SecondText = "Validation line two: transient test content only.";
    private static readonly string[] Scenes = { "Stage_1_2_DesignLab", "Stage_1_Route_DesignLab", "Stage_1_2" };
    private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static PlayerController player;
    private static Rigidbody2D body;
    private static DialoguePresenter presenter;
    private static DialogueSequence sequence;
    private static DesignLabDialogueCue cue;
    private static DesignLabEncounter encounter;
    private static CombatPauseController pause;
    private static int step;
    private static double stepAt;

    static DialoguePlayModeValidation()
    {
        EditorApplication.update += Tick;
        Application.logMessageReceived += OnLog;
    }

    public static void Run()
    {
        Directory.CreateDirectory(LogDirectory);
        string save = SaveManager.SaveFilePath;
        SessionState.SetString(Key + "SavePath", save);
        SessionState.SetBool(Key + "HadSave", File.Exists(save));
        if (File.Exists(save)) File.Copy(save, LogDirectory + "/autosave.before.json", true);
        File.WriteAllText(LogDirectory + "/playmode.txt", "Real Unity dialogue Play Mode integration checks\n");
        SessionState.SetBool(Key + "Requested", true);
        SessionState.SetInt(Key + "Scene", 0);
        SessionState.SetInt(Key + "Phase", 0);
        SessionState.SetInt(Key + "ExitCode", 0);
    }

    private static T Field<T>(object value, string name) => (T)value.GetType().GetField(name, Private).GetValue(value);
    private static void Set(object value, string name, object fieldValue)
    {
        FieldInfo field = value.GetType().GetField(name, Private);
        Require(field != null, "Missing validation field: " + value.GetType().Name + "." + name);
        field.SetValue(value, fieldValue);
    }
    private static void Require(bool ok, string message)
    {
        if (!ok) throw new InvalidOperationException(message);
    }
    private static void Record(string message)
    {
        File.AppendAllText(LogDirectory + "/playmode.txt", message + "\n");
        Debug.Log("Dialogue validation: " + message);
    }
    private static void Advance(int next)
    {
        step = next;
        stepAt = EditorApplication.timeSinceStartup;
    }
    private static void OnLog(string condition, string trace, LogType type)
    {
        if (SessionState.GetBool(Key + "Requested", false)
            && SessionState.GetInt(Key + "Phase", 0) == 2
            && (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)) Fail(condition);
    }
    private static void Fail(string message)
    {
        if (SessionState.GetInt(Key + "Phase", 0) == 3) return;
        SessionState.SetInt(Key + "ExitCode", 1);
        SessionState.SetInt(Key + "Phase", 3);
        File.AppendAllText(LogDirectory + "/playmode.txt", "FAIL: " + message + "\n");
        if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
    }

    private static void Tick()
    {
        if (!SessionState.GetBool(Key + "Requested", false)) return;
        try
        {
            int phase = SessionState.GetInt(Key + "Phase", 0);
            int scene = SessionState.GetInt(Key + "Scene", 0);
            if (phase == 0 && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                var loaded = EditorSceneManager.OpenScene("Assets/Scenes/" + Scenes[scene] + ".unity", OpenSceneMode.Single);
                foreach (var root in loaded.GetRootGameObjects())
                    foreach (var t in root.GetComponentsInChildren<Transform>(true))
                        Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) == 0,
                            "Missing script: " + t.name);
                SessionState.SetInt(Key + "Phase", 1);
                EditorApplication.EnterPlaymode();
                return;
            }
            if (phase == 1 && EditorApplication.isPlaying)
            {
                SessionState.SetInt(Key + "Phase", 2);
                player = null;
                Advance(-1);
                return;
            }
            if (phase == 2 && EditorApplication.isPlaying) RunStep(scene);
            if (SessionState.GetInt(Key + "Phase", 0) == 3 && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                int exitCode = SessionState.GetInt(Key + "ExitCode", 1);
                if (exitCode == 0 && scene < Scenes.Length - 1)
                {
                    SessionState.SetInt(Key + "Scene", scene + 1);
                    SessionState.SetInt(Key + "Phase", 0);
                    return;
                }
                SessionState.SetBool(Key + "Requested", false);
                string save = SessionState.GetString(Key + "SavePath", "");
                if (SessionState.GetBool(Key + "HadSave", false))
                    File.Copy(LogDirectory + "/autosave.before.json", save, true);
                else if (File.Exists(save)) File.Delete(save);
                Record(exitCode == 0
                    ? "PASS: both DesignLab scenes and formal-scene isolation; autosave restored. Human playtesting and user text remain untested."
                    : "FAILED; autosave restored.");
                EditorApplication.Exit(exitCode);
            }
        }
        catch (Exception exception) { Fail(exception.ToString()); }
    }

    private static void Initialize(int scene)
    {
        player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
        Require(player != null && GameManager.Instance != null, "Player or GameManager missing");
        Require(player.ControlsEnabled && Mathf.Approximately(Time.timeScale, 1f) && !DialoguePresenter.IsOpen,
            "Blank dialogue setup interfered with initial gameplay");
        // Avoid scene traversal and suppress session playtime, while preserving the
        // original autosave even if scene bootstrap or a formal checkpoint writes it.
        player.enabled = false;
        body = player.GetComponent<Rigidbody2D>();
        body.simulated = false;
        foreach (var collider in player.GetComponents<Collider2D>()) collider.enabled = false;
        if (SaveManager.Instance != null) Set(SaveManager.Instance, "currentData", null);
        pause = UnityEngine.Object.FindAnyObjectByType<CombatPauseController>();
        Require(pause != null, "Global pause controller missing");
        DesignLabDialogueCue[] cues = UnityEngine.Object.FindObjectsByType<DesignLabDialogueCue>(FindObjectsInactive.Include);
        if (scene == 2)
        {
            Require(cues.Length == 0 && DialoguePresenter.Instance == null,
                "Dialogue framework auto-injected into formal scene");
            cue = new GameObject("TransientFormalCueValidation").AddComponent<DesignLabDialogueCue>();
            Require(!cue.enabled && !cue.TryTrigger() && !cue.Triggered,
                "Dialogue cue did not reject formal scene");
            Record(Scenes[scene] + ": PASS no injected dialogue; explicit transient cue rejects formal scene");
            FinishScene();
            return;
        }

        Require(cues.Length > 0 && cues.All(c => c.enabled), "Expected enabled DesignLab authoring slots");
        Set(player, "isGrounded", true);
        foreach (DesignLabDialogueCue slot in cues)
        {
            DialogueSequence empty = Field<DialogueSequence>(slot, "sequence");
            Require(empty != null && !empty.HasContent, "Authoring slot is not blank: " + slot.name);
            player.transform.position = (Field<Vector2>(slot, "triggerMinimum") + Field<Vector2>(slot, "triggerMaximum")) * .5f;
            Require(!slot.TryTrigger() && !slot.Triggered && !DialoguePresenter.IsOpen && player.ControlsEnabled,
                "Blank slot consumed itself or blocked player: " + slot.name);
            slot.enabled = false;
        }
        // Blank cues create no panel themselves. Explicitly create the transient
        // presenter only after verifying their inert default authoring state.
        presenter = DialoguePresenter.ForScene(player.gameObject.scene);
        DialogueSequence blank = ScriptableObject.CreateInstance<DialogueSequence>();
        Require(!presenter.Show(blank, player), "Presenter accepted blank sequence");
        UnityEngine.Object.Destroy(blank);
        foreach (var room in UnityEngine.Object.FindObjectsByType<DesignLabEncounter>()) room.enabled = false;
        player.transform.position = new Vector3(0, -2.7f, 0);
        sequence = ScriptableObject.CreateInstance<DialogueSequence>();
        Set(sequence, "sequenceId", "TransientValidation");
        Sprite existingPortrait = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/cafe_icon/fox_god_transparent.png");
        Require(existingPortrait != null, "Existing portrait reference unavailable; do not change import settings");
        var lines = new[]
        {
            new DialogueLine { speaker = "Validation", text = FirstText, portrait = existingPortrait },
            new DialogueLine { speaker = "Validation", text = SecondText }
        };
        FieldInfo lineField = typeof(DialogueSequence).GetField("lines", Private);
        lineField.SetValue(sequence, lineField.FieldType == typeof(DialogueLine[]) ? (object)lines : new List<DialogueLine>(lines));
        Set(presenter, "charactersPerSecond", 20f);
        cue = cues[0];
        Set(cue, "sequence", sequence);
        Set(cue, "triggerMinimum", new Vector2(-1, -4));
        Set(cue, "triggerMaximum", new Vector2(1, 0));
        Set(cue, "encounter", null);
        Set(cue, "triggerAfterEncounter", false);
        Set(cue, "requireGrounded", true);
        Set(cue, "oncePerScene", true);
        Set(cue, "focusTarget", player.transform);
        Set(cue, "focusSize", new Vector2(2f, 3f));
        Set(cue, "focusDuration", .45f);
        cue.enabled = true;
        Set(player, "isGrounded", false);
        Require(!cue.TryTrigger() && !cue.Triggered, "Airborne cue consumed itself");
        Set(player, "isGrounded", true);
        body.linearVelocity = new Vector2(0f, 12f);
        Require(!cue.TryTrigger() && !cue.Triggered, "Cue interrupted upward motion with stale grounded flag");
        body.linearVelocity = new Vector2(0f, -12f);
        Require(!cue.TryTrigger() && !cue.Triggered, "Cue interrupted falling motion with stale grounded flag");
        body.linearVelocity = Vector2.zero;
        player.SetControlEnabled(false);
        Require(!presenter.Show(sequence, player) && !cue.TryTrigger() && !cue.Triggered,
            "Dialogue accepted disabled controls");
        player.SetControlEnabled(true);
        SetPause(true);
        Require(!presenter.Show(sequence, player) && !cue.TryTrigger() && !cue.Triggered,
            "Dialogue opened over pause menu");
        pause.Resume();
        Require(cue.TryTrigger() && cue.Triggered && DialoguePresenter.IsOpen, "Populated grounded cue failed");
        Require(!player.ControlsEnabled && Mathf.Approximately(Time.timeScale, 0f), "Dialogue did not own input/time");
        SetPause(true);
        Require(!Field<bool>(pause, "isPaused") && Mathf.Approximately(Time.timeScale, 0f),
            "Pause menu took ownership while dialogue open");
        Record(Scenes[scene] + ": PASS blank slots, ground/control/pause guards and populated trigger");
        Advance(0);
    }

    private static void SetPause(bool value) => typeof(CombatPauseController).GetMethod("SetPaused", Private).Invoke(pause, new object[] { value });
    private static void RunStep(int scene)
    {
        double elapsed = EditorApplication.timeSinceStartup - stepAt;
        Require(elapsed < 20, "Timed out in " + Scenes[scene] + " step " + step);
        if (step == -1)
        {
            if (elapsed > 1) Initialize(scene);
        }
        else if (step == 0 && elapsed > .25)
        {
            Require(presenter.LineIndex == 0 && presenter.IsTyping && presenter.DisplayedText.Length > 0
                && presenter.DisplayedText.Length < FirstText.Length && presenter.CurrentSequenceId == "TransientValidation",
                "Unscaled typewriter/progression state failed");
            presenter.Advance();
            Require(presenter.LineIndex == 0 && !presenter.IsTyping && presenter.DisplayedText == FirstText,
                "Advance did not reveal current line before progression");
            DialogueFocusEffect focus = UnityEngine.Object.FindAnyObjectByType<DialogueFocusEffect>();
            Require(focus != null && focus.GetComponent<LineRenderer>().positionCount == 4
                && Vector3.Distance(focus.transform.position, player.transform.position) < .01f,
                "Focus outline did not track target during paused dialogue");
            VerifyButtonLabel("DialogueNext", "继续  [Enter / E]");
            VerifyButtonLabel("DialogueSkip", "跳过  [Tab]");
            Image portrait = GameObject.Find("DialoguePortrait").GetComponent<Image>();
            Require(portrait.enabled && portrait.sprite == sequence.Lines[0].portrait && portrait.preserveAspect,
                "Optional existing portrait was not presented with preserved aspect ratio");
            CaptureDialogue(Scenes[scene] + "_dialogue.png");
            Advance(10);
        }
        else if (step == 10 && elapsed > .05)
        {
            presenter.Advance();
            Require(presenter.LineIndex == 1 && presenter.IsTyping, "Advance did not enter second line");
            Require(!GameObject.Find("DialoguePortrait").GetComponent<Image>().enabled,
                "Portrait leaked into a line with no portrait");
            Advance(1);
        }
        else if (step == 1 && elapsed > .15)
        {
            presenter.Skip();
            Advance(2);
        }
        else if (step == 2 && elapsed > .25)
        {
            Require(!DialoguePresenter.IsOpen && player.ControlsEnabled && Mathf.Approximately(Time.timeScale, 1f),
                "Skip did not restore controls/time after fade");
            Require(UnityEngine.Object.FindObjectsByType<DialogueFocusEffect>().Length == 0,
                "Focus outline did not expire using unscaled time");
            GameManager.Instance.Retry();
            Require(cue.Triggered && !cue.TryTrigger(), "Once-per-scene cue repeated after retry");
            Require(player.ControlsEnabled && body.simulated && body.linearVelocity == Vector2.zero,
                "Retry left invalid player state");
            body.simulated = false;
            Require(presenter.Show(sequence, player), "Presenter could not reopen after skip");
            GameManager.Instance.Retry();
            Require(!DialoguePresenter.IsOpen && player.ControlsEnabled && body.simulated
                && Mathf.Approximately(Time.timeScale, 1f) && body.linearVelocity == Vector2.zero,
                "Retry during dialogue failed to close/restore player");
            // Hold the fixture again after validating Retry's immediate state.
            // Delayed dialogue cleanup must not overwrite this new owner state.
            body.simulated = false;
            Advance(3);
        }
        else if (step == 3 && elapsed > .25)
        {
            Require(!DialoguePresenter.IsOpen && player.ControlsEnabled && !body.simulated
                && Mathf.Approximately(Time.timeScale, 1f) && body.linearVelocity == Vector2.zero,
                "Delayed dialogue cleanup overwrote post-retry player state");
            body.simulated = false;
            Require(presenter.Show(sequence, player), "Presenter could not reopen after retry");
            presenter.enabled = false;
            Require(!DialoguePresenter.IsOpen && player.ControlsEnabled && Mathf.Approximately(Time.timeScale, 1f),
                "Disabling open presenter did not restore controls/time");
            presenter.enabled = true;
            Require(presenter.Show(sequence, player), "Presenter could not reopen after disable cleanup");
            GameManager.Instance.PlayerFell();
            Advance(4);
        }
        else if (step == 4 && elapsed > .25)
        {
            Require(!DialoguePresenter.IsOpen && GameManager.Instance.IsBlockingUiVisible
                && !player.ControlsEnabled && !body.simulated && Mathf.Approximately(Time.timeScale, 1f),
                "Dialogue close unlocked retry-owned controls or physics");
            Require(!presenter.Show(sequence, player), "Dialogue opened over retry UI");
            GameManager.Instance.Retry();
            body.simulated = false;
            Record(Scenes[scene] + ": PASS typewriter/reveal/advance/skip, once-per-scene retry and UI ownership");
            BeginCompletionTest();
            Advance(5);
        }
        else if (step == 5 && encounter.Running)
        {
            foreach (var health in encounter.GetComponentsInChildren<GhostHealth>())
                if (!health.IsDead) health.Die();
            Advance(6);
        }
        else if (step == 6 && encounter.CurrentWave == 2)
        {
            foreach (var health in encounter.GetComponentsInChildren<GhostHealth>())
                if (!health.IsDead) health.Die();
            Advance(7);
        }
        else if (step == 7 && encounter.Completed)
        {
            Require(!cue.Triggered && !DialoguePresenter.IsOpen && !cue.TryTrigger(),
                "Completion cue interrupted airborne player");
            Set(player, "isGrounded", true);
            Require(cue.TryTrigger() && cue.Triggered && DialoguePresenter.IsOpen,
                "Completed grounded encounter cue did not fire");
            presenter.Advance();
            Advance(8);
        }
        else if (step == 8 && elapsed > .25)
        {
            CaptureDialogue(Scenes[scene] + "_completion.png");
            presenter.Advance();
            Require(presenter.LineIndex == 1, "Natural closure fixture did not enter final line");
            Advance(81);
        }
        else if (step == 81 && elapsed > .05)
        {
            presenter.Advance();
            Require(!presenter.IsTyping && presenter.DisplayedText == SecondText, "Final line did not reveal");
            Advance(82);
        }
        else if (step == 82 && elapsed > .05)
        {
            presenter.Advance();
            Advance(83);
        }
        else if (step == 83 && elapsed > .1)
        {
            Require(!DialoguePresenter.IsOpen && player.ControlsEnabled && Mathf.Approximately(Time.timeScale, 1f),
                "Advancing past final line did not restore controls/time");
            Require(presenter.Show(sequence, player), "Presenter could not reopen after natural closure");
            UnityEngine.Object.Destroy(presenter.gameObject);
            Advance(9);
        }
        else if (step == 9 && elapsed > .1)
        {
            Require(!DialoguePresenter.IsOpen && player.ControlsEnabled && Mathf.Approximately(Time.timeScale, 1f),
                "Presenter destruction did not restore controls/time");
            Record(Scenes[scene] + ": PASS real two-wave completion gate, safe ground trigger and destruction cleanup");
            FinishScene();
        }
    }

    private static void BeginCompletionTest()
    {
        cue.enabled = false;
        encounter = UnityEngine.Object.FindObjectsByType<DesignLabEncounter>().OrderBy(r => r.name).First();
        encounter.enabled = true;
        var owner = new GameObject("TransientCompletionCueValidation");
        owner.SetActive(false);
        cue = owner.AddComponent<DesignLabDialogueCue>();
        Set(cue, "player", player);
        Set(cue, "sequence", sequence);
        Set(cue, "encounter", encounter);
        Set(cue, "triggerAfterEncounter", true);
        Set(cue, "requireGrounded", true);
        Set(cue, "triggerMinimum", new Vector2(-999, -999));
        Set(cue, "triggerMaximum", new Vector2(999, 999));
        Set(player, "isGrounded", false);
        owner.SetActive(true);
        Require(!cue.TryTrigger() && !cue.Triggered, "Incomplete encounter cue triggered");
        player.transform.position = (Field<Vector2>(encounter, "triggerMinimum") + Field<Vector2>(encounter, "triggerMaximum")) * .5f;
    }

    private static void CaptureDialogue(string filename)
    {
        Camera camera = Camera.main;
        Require(camera != null, "Capture camera missing");
        Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>();
        RenderMode[] modes = canvases.Select(c => c.renderMode).ToArray();
        Camera[] cameras = canvases.Select(c => c.worldCamera).ToArray();
        float[] distances = canvases.Select(c => c.planeDistance).ToArray();
        RenderTexture previousTarget = camera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        var target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
        var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        try
        {
            for (int i = 0; i < canvases.Length; i++)
            {
                if (modes[i] != RenderMode.ScreenSpaceOverlay) continue;
                canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                canvases[i].worldCamera = camera;
                canvases[i].planeDistance = 1f;
            }
            Canvas.ForceUpdateCanvases();
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            texture.Apply();
            File.WriteAllBytes(LogDirectory + "/" + filename, texture.EncodeToPNG());
            Record("Captured " + filename + " (transient validation text; visual review required)");
        }
        finally
        {
            for (int i = 0; i < canvases.Length; i++)
            {
                canvases[i].renderMode = modes[i];
                canvases[i].worldCamera = cameras[i];
                canvases[i].planeDistance = distances[i];
            }
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(target);
        }
    }

    private static void VerifyButtonLabel(string name, string expected)
    {
        Canvas.ForceUpdateCanvases();
        Button button = GameObject.Find(name).GetComponent<Button>();
        Text label = button.GetComponentInChildren<Text>();
        Require(button.IsInteractable() && label != null && label.text == expected
            && label.cachedTextGenerator.vertexCount > 0,
            "Dialogue button label missing or not generated for rendering: " + name);
    }

    private static void FinishScene()
    {
        if (sequence != null) UnityEngine.Object.Destroy(sequence);
        SessionState.SetInt(Key + "Phase", 3);
        EditorApplication.ExitPlaymode();
    }
}
