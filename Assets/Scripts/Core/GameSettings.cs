using System;
using UnityEngine;

public static class GameSettings
{
    public enum TitleBgmPreference
    {
        Random = 0,
        Cheerful = 1,
        Melancholy = 2
    }

    private const string BgmVolumeKey = "Settings.BgmVolume";
    private const string SfxVolumeKey = "Settings.SfxVolume";
    private const string FullscreenKey = "Settings.Fullscreen";
    private const string TitleBgmPreferenceKey = "Settings.TitleBgmPreference";
    private const string ResolutionIndexKey = "Settings.ResolutionIndex";

    private static readonly Vector2Int[] ResolutionOptions =
    {
        new Vector2Int(1280, 720),
        new Vector2Int(1920, 1080),
        new Vector2Int(2560, 1440)
    };

    public static event Action Changed;

    public static float BgmVolume => PlayerPrefs.GetFloat(BgmVolumeKey, 1f);
    public static float SfxVolume => PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
    public static bool IsFullscreen => PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
    public static TitleBgmPreference PreferredTitleBgm => (TitleBgmPreference)Mathf.Clamp(
        PlayerPrefs.GetInt(TitleBgmPreferenceKey, (int)TitleBgmPreference.Random),
        0,
        2);
    public static int ResolutionIndex => Mathf.Clamp(
        PlayerPrefs.GetInt(ResolutionIndexKey, FindClosestResolutionIndex(Screen.width, Screen.height)),
        0,
        ResolutionOptions.Length - 1);
    public static Vector2Int SelectedResolution => ResolutionOptions[ResolutionIndex];

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplySavedDisplayMode()
    {
        if (PlayerPrefs.HasKey(FullscreenKey) || PlayerPrefs.HasKey(ResolutionIndexKey))
        {
            ApplyDisplayMode();
        }
    }

    public static void SetBgmVolume(float value)
    {
        PlayerPrefs.SetFloat(BgmVolumeKey, Mathf.Clamp01(value));
        SaveAndNotify();
    }

    public static void SetSfxVolume(float value)
    {
        PlayerPrefs.SetFloat(SfxVolumeKey, Mathf.Clamp01(value));
        SaveAndNotify();
    }

    public static void SetFullscreen(bool fullscreen)
    {
        PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
        ApplyDisplayMode();
        SaveAndNotify();
    }

    public static void CycleResolution()
    {
        SetResolutionIndex((ResolutionIndex + 1) % ResolutionOptions.Length);
    }

    public static void SetResolutionIndex(int index)
    {
        PlayerPrefs.SetInt(ResolutionIndexKey, Mathf.Clamp(index, 0, ResolutionOptions.Length - 1));
        ApplyDisplayMode();
        SaveAndNotify();
    }

    public static void CycleTitleBgmPreference()
    {
        int next = ((int)PreferredTitleBgm + 1) % 3;
        SetTitleBgmPreference((TitleBgmPreference)next);
    }

    public static void SetTitleBgmPreference(TitleBgmPreference preference)
    {
        PlayerPrefs.SetInt(TitleBgmPreferenceKey, Mathf.Clamp((int)preference, 0, 2));
        SaveAndNotify();
    }

    public static string GetResolutionLabel()
    {
        Vector2Int resolution = SelectedResolution;
        return $"{resolution.x} × {resolution.y}";
    }

    public static string GetTitleBgmLabelJapanese()
    {
        switch (PreferredTitleBgm)
        {
            case TitleBgmPreference.Cheerful:
                return "楽しい";
            case TitleBgmPreference.Melancholy:
                return "悲しい";
            default:
                return "ランダム";
        }
    }

    private static void ApplyDisplayMode()
    {
        Vector2Int resolution = SelectedResolution;
        FullScreenMode mode = IsFullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        Screen.SetResolution(resolution.x, resolution.y, mode);
    }

    private static int FindClosestResolutionIndex(int width, int height)
    {
        int bestIndex = 0;
        long bestDistance = long.MaxValue;
        for (int index = 0; index < ResolutionOptions.Length; index++)
        {
            Vector2Int option = ResolutionOptions[index];
            long widthDelta = option.x - width;
            long heightDelta = option.y - height;
            long distance = widthDelta * widthDelta + heightDelta * heightDelta;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = index;
            }
        }

        return bestIndex;
    }

    private static void SaveAndNotify()
    {
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
