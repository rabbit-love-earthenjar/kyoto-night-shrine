using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Component integration checks in the real scenes. Teleportation and direct enemy
// defeat are intentional: this does not measure player skill, route time or fun.
[InitializeOnLoad]
public static class DesignLabPlayModeValidation
{
    private const string Key = "DesignLab.Validation.";
    private static readonly string[] Scenes = { "Stage_1_2_DesignLab", "Stage_1_Route_DesignLab" };
    private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static DesignLabEncounter[] rooms;
    private static PlayerController player;
    private static Rigidbody2D body;
    private static int roomIndex, step, baselineFaith;
    private static double stepAt;
    private static int[] oldActors;
    private static TemporaryCloudPlatform cloud;
    private static Collider2D cloudCollider;

    static DesignLabPlayModeValidation()
    {
        EditorApplication.update += Tick;
        Application.logMessageReceived += OnLog;
    }

    public static void Run()
    {
        Directory.CreateDirectory("Logs/DesignLabValidation");
        // Preserve the user's existing autosave across both independent Play sessions.
        string save = SaveManager.SaveFilePath;
        SessionState.SetString(Key + "SavePath", save);
        SessionState.SetBool(Key + "HadSave", File.Exists(save));
        if (File.Exists(save)) File.Copy(save, "Logs/DesignLabValidation/autosave.before.json", true);
        File.WriteAllText("Logs/DesignLabValidation/playmode.txt", "Real Unity Play Mode integration checks\n");
        SessionState.SetBool(Key + "Requested", true);
        SessionState.SetInt(Key + "Scene", 0);
        SessionState.SetInt(Key + "Phase", 0);
        SessionState.SetInt(Key + "ExitCode", 0);
    }

    private static T Field<T>(object value, string name) => (T)value.GetType().GetField(name, Private).GetValue(value);
    private static void Require(bool ok, string message)
    {
        if (!ok) throw new InvalidOperationException(message);
    }
    private static void Record(string message)
    {
        File.AppendAllText("Logs/DesignLabValidation/playmode.txt", message + "\n");
        Debug.Log("DesignLab validation: " + message);
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
            && (type == LogType.Exception || type == LogType.Error || type == LogType.Assert))
            Fail(condition);
    }
    private static void Fail(string message)
    {
        SessionState.SetInt(Key + "ExitCode", 1);
        SessionState.SetInt(Key + "Phase", 3);
        File.AppendAllText("Logs/DesignLabValidation/playmode.txt", "FAIL: " + message + "\n");
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
                rooms = null;
                roomIndex = 0;
                Advance(-1);
                return;
            }
            if (phase == 2 && EditorApplication.isPlaying) RunStep(scene);
            if (phase == 3 && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                int exitCode = SessionState.GetInt(Key + "ExitCode", 1);
                if (exitCode == 0 && scene == 0)
                {
                    SessionState.SetInt(Key + "Scene", 1);
                    SessionState.SetInt(Key + "Phase", 0);
                    return;
                }
                SessionState.SetBool(Key + "Requested", false);
                string save = SessionState.GetString(Key + "SavePath", "");
                if (SessionState.GetBool(Key + "HadSave", false))
                    File.Copy("Logs/DesignLabValidation/autosave.before.json", save, true);
                else if (File.Exists(save)) File.Delete(save);
                Record(exitCode == 0 ? "PASS: both scenes; autosave restored. Human traversal remains untested."
                    : "FAILED; autosave restored.");
                EditorApplication.Exit(exitCode);
            }
        }
        catch (Exception e) { Fail(e.ToString()); }
    }

    private static GhostHealth[] Actors(DesignLabEncounter room) =>
        room.GetComponentsInChildren<GhostHealth>().Where(h => !h.IsDead).ToArray();
    private static void Enter(DesignLabEncounter room)
    {
        Vector2 min = Field<Vector2>(room, "triggerMinimum"), max = Field<Vector2>(room, "triggerMaximum");
        player.transform.position = (min + max) * .5f;
        // GameManager.Retry correctly re-enables physics; hold the test fixture
        // stationary again while its colliders are disabled for lifecycle checks.
        body.simulated = false;
        player.SetControlEnabled(true);
        Physics2D.SyncTransforms();
    }
    private static void Defeat(DesignLabEncounter room)
    {
        foreach (GhostHealth h in Actors(room)) h.Die();
    }
    private static void Retry(DesignLabEncounter room)
    {
        oldActors = Actors(room).Select(h => h.GetInstanceID()).ToArray();
        GameManager.Instance.Retry();
        Require(!room.Running && !room.Completed && room.CurrentWave == 0, "Retry did not reset encounter");
        Require(Actors(room).Length == 0, "Retry left live actors");
        Require(Vector3.Distance(player.transform.position, Field<Vector3>(room, "checkpointPosition")) < .01f,
            "Retry did not use encounter checkpoint");
        Require(GameManager.Instance.GetFaithPointCount() == baselineFaith, "Incomplete waves awarded faith");
        Enter(room);
    }

    private static void RunStep(int scene)
    {
        double elapsed = EditorApplication.timeSinceStartup - stepAt;
        Require(elapsed < 25, "Timed out in scene " + Scenes[scene] + " step " + step
            + "; player=" + (player == null ? "missing" : player.transform.position.ToString())
            + "; controls=" + (player != null && player.ControlsEnabled) + "; timescale=" + Time.timeScale);
        if (step == -1)
        {
            if (elapsed < 1) return;
            player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            Require(player != null && GameManager.Instance != null, "Player or GameManager missing");
            player.enabled = false;
            body = player.GetComponent<Rigidbody2D>();
            body.simulated = false;
            foreach (var c in player.GetComponents<Collider2D>()) c.enabled = false;
            // Prevent the integration test from accumulating saved play time on exit.
            if (SaveManager.Instance != null)
                typeof(SaveManager).GetField("currentData", Private).SetValue(SaveManager.Instance, null);
            rooms = UnityEngine.Object.FindObjectsByType<DesignLabEncounter>().OrderBy(r => r.name).ToArray();
            Require(rooms.Length == 2 && rooms.All(r => r.enabled), "Expected two enabled encounters");
            Require(!Field<bool>(ResourceInventory.Instance, "persistResourceState"), "Inventory persists rewards");
            Record(Scenes[scene] + ": imported, no missing scripts; Play Mode initialized");
            Advance(0);
        }
        else if (roomIndex < rooms.Length) TestRoom(rooms[roomIndex]);
        else if (scene == 1) TestCloud(elapsed);
        else FinishScene();
    }

    private static void TestRoom(DesignLabEncounter room)
    {
        var gate = Field<GameObject>(room, "exitBarrier");
        if (step == 0)
        {
            baselineFaith = GameManager.Instance.GetFaithPointCount();
            Require(gate.activeInHierarchy, "Gate not initially closed");
            Enter(room);
            Advance(1);
        }
        else if (step == 1 && room.Running)
        {
            Require(room.CurrentWave == 1 && Actors(room).Length == 2, "First wave count");
            Actors(room)[0].TakeDamage(1);
            Retry(room);
            Advance(2);
        }
        else if ((step == 2 || step == 4) && room.Running)
        {
            var actors = Actors(room);
            Require(room.CurrentWave == 1 && actors.Length == 2, "Restarted first wave count");
            Require(actors.All(h => h.CurrentHP == h.MaxHP && !oldActors.Contains(h.GetInstanceID())), "Retry did not create fresh healthy actors");
            Defeat(room);
            Advance(step == 2 ? 3 : 5);
        }
        else if ((step == 3 || step == 5) && room.CurrentWave == 2)
        {
            Require(Actors(room).Length == 3, "Second wave count");
            Require(gate.activeInHierarchy, "Gate opened before second wave defeat");
            if (step == 3) { Retry(room); Advance(4); }
            else { Defeat(room); Advance(6); }
        }
        else if (step == 6 && room.Completed)
        {
            int expected = baselineFaith + Field<int>(room, "completionFaith");
            Require(!gate.activeInHierarchy && GameManager.Instance.GetFaithPointCount() == expected, "Completion gate or faith reward");
            GameManager.Instance.Retry();
            body.simulated = false;
            Require(room.Completed && !gate.activeInHierarchy, "Completed gate relocked on retry");
            Require(GameManager.Instance.GetFaithPointCount() == expected, "Duplicate completion reward");
            Record(room.name + ": PASS two waves, both retry phases, fresh HP, checkpoint, gate and single reward");
            roomIndex++;
            Advance(roomIndex == rooms.Length ? 10 : 0);
        }
    }

    private static void TestCloud(double elapsed)
    {
        if (step == 10)
        {
            player.transform.position = new Vector3(30, -2.7f, 0);
            Advance(11);
        }
        else if (step == 11 && elapsed > .1)
        {
            var checkpoint = UnityEngine.Object.FindAnyObjectByType<DesignLabCheckpoint>();
            Require(checkpoint != null && !checkpoint.enabled, "Cloud approach checkpoint not consumed");
            Require(Vector3.Distance(Field<Transform>(checkpoint, "retryPoint").position, new Vector3(30,-2.7f,0)) < .01f, "Cloud retry position");
            cloud = GameObject.Find("Lab_Cloud_03").GetComponent<TemporaryCloudPlatform>();
            cloudCollider = cloud.GetComponent<Collider2D>();
            player.transform.position = new Vector3(cloudCollider.bounds.center.x, cloudCollider.bounds.max.y + 1.1f, 0);
            foreach (var c in player.GetComponents<Collider2D>()) c.enabled = true;
            body.simulated = true;
            body.linearVelocity = Vector2.zero;
            Advance(12);
        }
        else if (step == 12 && Field<Coroutine>(cloud, "cycleRoutine") != null)
        {
            Require(cloudCollider.enabled, "Cloud disappeared before warning");
            body.simulated = false;
            Advance(13);
        }
        else if (step == 13 && !cloudCollider.enabled)
        {
            Require(elapsed >= .65 && elapsed < 1.5, "Cloud warning timing");
            Advance(14);
        }
        else if (step == 14 && cloudCollider.enabled)
        {
            Require(elapsed > 2.5 && elapsed < 3.5, "Cloud recovery timing");
            GameManager.Instance.Retry();
            Require(cloudCollider.enabled, "Cloud unavailable after retry");
            Record("Cloud approach checkpoint and physical landing/disappear/recover cycle: PASS");
            FinishScene();
        }
    }

    private static void FinishScene()
    {
        SessionState.SetInt(Key + "Phase", 3);
        EditorApplication.ExitPlaymode();
    }
}
