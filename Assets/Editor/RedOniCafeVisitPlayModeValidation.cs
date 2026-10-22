using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Integration check for the demo v0.1 Red Oni -> cafe beat. It sets the save story stage
// directly instead of playing the Boss, so it is not a manual playtest. The autosave file
// is backed up and restored; no ingredients are produced or served.
// Run: Unity -batchmode -projectPath <project> -executeMethod RedOniCafeVisitPlayModeValidation.Run -logFile <log>
[InitializeOnLoad]
public static class RedOniCafeVisitPlayModeValidation
{
    private const string Key = "RedOniCafeVisit.Validation.";
    private const string LogDirectory = "Logs/RedOniCafeVisitValidation";
    private const string BossScenePath = "Assets/Scenes/Stage_1_Boss_RedOni.unity";
    private const string CafeScenePath = "Assets/Scenes/CafeInterior_Temporary.unity";
    private const string CafeSceneName = "CafeInterior_Temporary";
    private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static int step;
    private static double stepAt;

    static RedOniCafeVisitPlayModeValidation()
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
        File.WriteAllText(LogDirectory + "/playmode.txt", "Red Oni cafe visit Play Mode integration checks\n");
        SessionState.SetInt(Key + "ExitCode", 0);

        try
        {
            CheckBossSceneStoryStage();
        }
        catch (Exception exception)
        {
            File.AppendAllText(LogDirectory + "/playmode.txt", "FAIL: " + exception.Message + "\n");
            RestoreSaveAndExit(1);
            return;
        }

        SessionState.SetBool(Key + "Requested", true);
        SessionState.SetInt(Key + "Phase", 0);
    }

    private static void CheckBossSceneStoryStage()
    {
        EditorSceneManager.OpenScene(BossScenePath, OpenSceneMode.Single);
        GameManager manager = UnityEngine.Object.FindAnyObjectByType<GameManager>();
        Require(manager != null, "Boss scene GameManager missing");
        string storyStage = new SerializedObject(manager).FindProperty("clearStoryStageId")?.stringValue;
        Require(storyStage == SaveManager.RedOniDefeatedStoryStage, "Boss clearStoryStageId is '" + storyStage + "'");
        Record("Boss scene GameManager records RedOniDefeated on Stage Clear.");
    }

    private static void Tick()
    {
        if (!SessionState.GetBool(Key + "Requested", false)) return;
        try
        {
            int phase = SessionState.GetInt(Key + "Phase", 0);
            if (phase == 0 && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorSceneManager.OpenScene(CafeScenePath, OpenSceneMode.Single);
                SessionState.SetInt(Key + "Phase", 1);
                EditorApplication.EnterPlaymode();
                return;
            }
            if (phase == 1 && EditorApplication.isPlaying)
            {
                SessionState.SetInt(Key + "Phase", 2);
                Advance(0);
                return;
            }
            if (phase == 2 && EditorApplication.isPlaying) RunStep();
            if (SessionState.GetInt(Key + "Phase", 0) == 3 && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.SetBool(Key + "Requested", false);
                RestoreSaveAndExit(SessionState.GetInt(Key + "ExitCode", 1));
            }
        }
        catch (Exception exception) { Fail(exception.ToString()); }
    }

    private static void RunStep()
    {
        if (EditorApplication.timeSinceStartup - stepAt < 1.0) return;

        switch (step)
        {
            case 0:
                SetStoryStage(SaveManager.HubArrivalStoryStage);
                SceneManager.LoadScene(CafeSceneName);
                Advance(1);
                break;
            case 1:
            {
                CafeOperationController operation = Find<CafeOperationController>();
                Require(FindDishIndex(operation) < 0, "Red Oni dish visible before victory");
                Require(GameObject.Find(RedOniCafeVisitor.ObjectName) == null, "Red Oni visitor visible before victory");
                Record("Before victory: 3-item menu, no Red Oni visitor.");

                SetStoryStage(SaveManager.RedOniDefeatedStoryStage);
                SaveManager.RecordCheckpoint(null, SaveManager.HubArrivalStoryStage);
                Require(SaveManager.HasReachedStoryStage(SaveManager.RedOniDefeatedStoryStage), "Replaying Stage_1_2 moved the story stage backwards");
                Record("Replaying an earlier stage keeps RedOniDefeated.");
                SceneManager.LoadScene(CafeSceneName);
                Advance(2);
                break;
            }
            case 2:
                CheckAfterVictory();
                Advance(3);
                break;
            case 3:
                Require(SceneManager.GetActiveScene().name == RedOniCafeVisitConfig.Load().TitleSceneName, "Demo end did not return to the title scene");
                Require(SaveManager.HasReachedStoryStage(SaveManager.DemoCompleteStoryStage), "DemoComplete was not saved");
                Record("Demo end button returned to the title and saved DemoComplete.");
                SessionState.SetInt(Key + "Phase", 3);
                EditorApplication.ExitPlaymode();
                break;
        }
    }

    private static void CheckAfterVictory()
    {
        RedOniCafeVisitConfig config = RedOniCafeVisitConfig.Load();
        CafeOperationController operation = Find<CafeOperationController>();
        CafeSceneController cafe = Find<CafeSceneController>();

        int dishIndex = FindDishIndex(operation);
        Require(dishIndex >= 0, "Red Oni dish missing after victory");
        CafeMenuItem dish = operation.MenuItems[dishIndex];
        int expectedReward = CafeEconomyFormula.CalculateMenuFaithReward(dish.Ingredients);
        Require(dish.FaithPointReward == expectedReward && expectedReward > 0, "Red Oni dish reward does not follow CafeEconomyFormula");
        Require(operation.GetFinishedItemIdForMenu(dish) == ResourceInventory.RedOniSpicyDishId, "Red Oni dish has no finished-item slot");
        Record("After victory: dish '" + dish.DisplayName + "' added, reward " + dish.FaithPointReward + " Faith.");

        GameObject visitorObject = GameObject.Find(RedOniCafeVisitor.ObjectName);
        Require(visitorObject != null, "Red Oni visitor missing after victory");
        SpriteRenderer visitorRenderer = visitorObject.GetComponent<SpriteRenderer>();
        BoxCollider2D visitorCollider = visitorObject.GetComponent<BoxCollider2D>();
        Require(visitorRenderer != null && visitorRenderer.sprite != null, "Red Oni visitor has no sprite");
        Require(visitorCollider != null && visitorCollider.isTrigger, "Red Oni visitor click area must be a trigger");
        Record("Red Oni visitor at " + (Vector2)visitorObject.transform.position + " with sprite " + visitorRenderer.sprite.name + ".");

        cafe.ShowVisitorLine(config.VisitorDisplayName, config.VisitorLine);
        Require(Field<GameObject>(cafe, "infoPanel").activeSelf, "Visitor line panel did not open");
        Require(Field<Text>(cafe, "infoBody").text == config.VisitorLine, "Visitor line text mismatch");
        cafe.HideInfoPanel();
        Record("Visitor line panel opens with the configured line.");

        cafe.ShowReceptionPanel();
        CafeProductionPopupController popup = Field<CafeProductionPopupController>(cafe, "cafeProductionPopupController");
        Require(popup != null && Field<Button>(popup, "redOniDishRecipeButton") != null, "Oven has no Red Oni dish recipe button");
        popup.SelectMachine(CafeProductionMachineType.BakerMachine);
        Require(Field<Button>(popup, "redOniDishRecipeButton").gameObject.activeInHierarchy, "Red Oni dish button hidden on the oven");
        popup.ClosePopup();
        Record("Oven popup shows the Red Oni dish recipe.");

        operation.TryOpenForBusiness();
        cafe.ReturnToHub();
        Require(Field<Text>(cafe, "infoCloseButtonText").text == config.ResultNextButtonLabel, "Day result does not lead to the demo end panel");
        Field<Button>(cafe, "infoCloseButton").onClick.Invoke();
        Require(Field<Text>(cafe, "infoTitle").text == config.DemoEndTitle, "Demo end panel title mismatch");
        Record("Day result -> demo end panel shown.");
        Field<Button>(cafe, "infoCloseButton").onClick.Invoke();
    }

    private static int FindDishIndex(CafeOperationController operation)
    {
        Require(operation != null, "CafeOperationController missing");
        for (int i = 0; i < operation.MenuItems.Count; i++)
        {
            if (operation.MenuItems[i].MenuId == RedOniCafeVisitConfig.DishMenuId) return i;
        }
        return -1;
    }

    private static void SetStoryStage(string storyStageId)
    {
        SaveManager manager = SaveManager.Instance;
        Require(manager != null, "SaveManager missing");
        string now = DateTime.UtcNow.ToString("O");
        typeof(SaveManager).GetField("currentData", Private).SetValue(manager, new GameSaveData
        {
            hasStarted = true,
            lastSafeSceneName = CafeSceneName,
            storyStageId = storyStageId,
            createdUtc = now,
            updatedUtc = now
        });
    }

    private static T Find<T>() where T : UnityEngine.Object => UnityEngine.Object.FindAnyObjectByType<T>();
    private static T Field<T>(object value, string name) => (T)value.GetType().GetField(name, Private).GetValue(value);
    private static void Advance(int next)
    {
        step = next;
        stepAt = EditorApplication.timeSinceStartup;
    }
    private static void Require(bool ok, string message)
    {
        if (!ok) throw new InvalidOperationException(message);
    }
    private static void Record(string message)
    {
        File.AppendAllText(LogDirectory + "/playmode.txt", message + "\n");
        Debug.Log("Red Oni cafe validation: " + message);
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
    private static void RestoreSaveAndExit(int exitCode)
    {
        string save = SessionState.GetString(Key + "SavePath", "");
        if (SessionState.GetBool(Key + "HadSave", false))
            File.Copy(LogDirectory + "/autosave.before.json", save, true);
        else if (!string.IsNullOrEmpty(save) && File.Exists(save)) File.Delete(save);
        Record(exitCode == 0
            ? "PASS: integration checks only; autosave restored. Boss victory by hand, visuals and text remain unverified."
            : "FAILED; autosave restored.");
        EditorApplication.Exit(exitCode);
    }
}
