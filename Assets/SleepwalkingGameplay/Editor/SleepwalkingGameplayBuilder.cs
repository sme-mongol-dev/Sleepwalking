using System.IO;
using Sleepwalking;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SleepwalkingGameplayBuilder
{
    const string ScenePath = "Assets/Scenes/Sleepwalking.unity";
    const string ProfilesPath = "Assets/SleepwalkingGameplay/Profiles";

    [MenuItem("Sleepwalking/Gameplay/Create Movement Profiles")]
    public static void BuildMovementProfiles()
    {
        Directory.CreateDirectory(ProfilesPath);
        AssetDatabase.Refresh();
        CreateProfile("01_Controlled", "Candidate: deliberate takeoff and narrow dash window.",
            7.6f, 60f, 30f, 11.8f, 3.3f, 17.5f, .17f, 2.2f, .10f, .12f, .36f);
        CreateProfile("02_Responsive", "Default movement after L01-L07 PlayMode calibration.",
            8.5f, 65f, 38f, 12f, 3.2f, 18f, .18f, 2f, .11f, .13f, .35f);
        CreateProfile("03_Aggressive", "Candidate: faster run and stronger dash, with firm gravity.",
            9.4f, 80f, 48f, 12.15f, 3.35f, 20f, .19f, 1.8f, .12f, .14f, .30f);
        AssetDatabase.SaveAssets();
    }

    static void CreateProfile(string fileName, string description, float run, float groundAccel,
        float airAccel, float jump, float gravity, float dash, float dashSeconds, float wallSlide,
        float coyote, float jumpBuffer, float dashCooldown)
    {
        string path = ProfilesPath + "/" + fileName + ".asset";
        if (AssetDatabase.LoadAssetAtPath<SleepwalkingMovementProfile>(path) != null) return;
        SleepwalkingMovementProfile profile = ScriptableObject.CreateInstance<SleepwalkingMovementProfile>();
        profile.Description = description;
        profile.RunSpeed = run;
        profile.GroundAcceleration = groundAccel;
        profile.AirAcceleration = airAccel;
        profile.JumpSpeed = jump;
        profile.GravityScale = gravity;
        profile.DashSpeed = dash;
        profile.DashSeconds = dashSeconds;
        profile.WallSlideSpeed = wallSlide;
        profile.CoyoteSeconds = coyote;
        profile.JumpBufferSeconds = jumpBuffer;
        profile.DashCooldownSeconds = dashCooldown;
        AssetDatabase.CreateAsset(profile, path);
    }

    [MenuItem("Sleepwalking/Gameplay/Build Integration Scene")]
    public static void BuildScene()
    {
        TryBuildScene();
    }

    static bool TryBuildScene()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
        BuildMovementProfiles();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = new GameObject("Sleepwalking Game");
        SleepwalkingGame game = root.AddComponent<SleepwalkingGame>();
        game.MovementProfiles[0] = AssetDatabase.LoadAssetAtPath<SleepwalkingMovementProfile>(ProfilesPath + "/01_Controlled.asset");
        game.MovementProfiles[1] = AssetDatabase.LoadAssetAtPath<SleepwalkingMovementProfile>(ProfilesPath + "/02_Responsive.asset");
        game.MovementProfiles[2] = AssetDatabase.LoadAssetAtPath<SleepwalkingMovementProfile>(ProfilesPath + "/03_Aggressive.asset");
        game.MovementProfile = AssetDatabase.LoadAssetAtPath<SleepwalkingMovementProfile>(ProfilesPath + "/02_Responsive.asset");
        for (int q = 0; q < 7; q++)
            game.LevelPrefabs[q] = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/SleepwalkingLevels/Prefabs/L" + (q + 1).ToString("00") + "_DreamGeometry.prefab");
        game.RoomPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SleepwalkingVisual/Prefabs/SleepwalkingRoom.prefab");
        string[] themeNames = { "Meat", "PostApocalypse", "DeepWater", "Heaven" };
        for (int q = 0; q < themeNames.Length; q++)
            game.ThemePrefabs[q] = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/SleepwalkingVisual/Prefabs/Themes/" + themeNames[q] + ".prefab");

        GameObject uiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SleepwalkingUI/Prefabs/SleepwalkingUI.prefab");
        if (uiPrefab != null)
        {
            GameObject uiInstance = (GameObject)PrefabUtility.InstantiatePrefab(uiPrefab);
            game.UI = uiInstance.GetComponent<SleepwalkingUI>();
        }
        else
        {
            GameObject ui = new GameObject("Sleepwalking UI");
            game.UI = ui.AddComponent<SleepwalkingUI>();
        }

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        Debug.Log("Built shared scene: " + ScenePath);
        return true;
    }

    [MenuItem("Sleepwalking/Gameplay/Build Windows")]
    public static void BuildWindows()
    {
        AssetDatabase.Refresh();
        for (int q = 0; q < 7; q++)
            if (AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/SleepwalkingLevels/Prefabs/L" + (q + 1).ToString("00") + "_DreamGeometry.prefab") == null)
                throw new UnityException("Missing authored dream geometry L" + (q + 1).ToString("00"));
        if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SleepwalkingVisual/Prefabs/SleepwalkingRoom.prefab") == null)
            throw new UnityException("Missing editable room prefab.");
        string[] themes = { "Meat", "PostApocalypse", "DeepWater", "Heaven" };
        foreach (string theme in themes)
            if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SleepwalkingVisual/Prefabs/Themes/" + theme + ".prefab") == null)
                throw new UnityException("Missing dream style prefab: " + theme);
        if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SleepwalkingAudio/Resources/SleepwalkingAudioDirector.prefab") == null)
            throw new UnityException("Missing audio director prefab.");
        if (!TryBuildScene()) return;
        Directory.CreateDirectory("Builds/Windows");
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = "Builds/Windows/Sleepwalking.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };
        UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(options);
        Debug.Log("Windows build: " + report.summary.result + " " + report.summary.totalErrors + " errors");
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new UnityException("Windows build failed with " + report.summary.totalErrors + " errors.");
    }
}
