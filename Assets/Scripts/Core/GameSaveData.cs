using System;

[Serializable]
public sealed class GameSaveData
{
    public int saveVersion = 2;
    public bool hasStarted;
    public string lastSafeSceneName = "Stage_1_2";
    public string storyStageId = "Prologue";
    public string createdUtc;
    public string updatedUtc;
    public float totalPlaySeconds;
    public bool usesLegacyPlayerPrefsBridge = true;
}
