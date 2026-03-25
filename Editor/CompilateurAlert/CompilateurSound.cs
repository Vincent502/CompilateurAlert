#if UNITY_EDITOR
using System;
using System.IO;
using System.Media;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

[InitializeOnLoad]
internal static class CompilateurSound
{
    //* Key to capture the pending
    private const string KeyPending = "CompilateurSound.Pending";
    private const string KeyHadErrors = "CompilateurSound.HadErrors";

    //* Key to capture the preferences
    private const string PrefErrorSoundName = "CompilateurSound.ErrorSoundName";
    private const string PrefSuccesSoundName = "CompilateurSound.SuccessSoundName";
    private const string PrefErrorSoundVolume = "CompilateurSound.ErrorSoundVolume";
    private const string PrefSuccesSoundVolume = "CompilateurSound.SuccessSoundVolume";
    //* Key to capture the errors
    private static bool capturingErrors;
    private static bool hasErrors;
    //* Time of the compilation
    private static double finishedAt;
    //* Name of the sound
    public static string errorSoundName = "cheh_Maskey.wav";
    public static string succesSoundName = "Shoobidoba.wav";
    //* Volume of the sound
    public static float errorSoundVolume = 0.5f;
    public static float succesSoundVolume = 0.5f;

    static CompilateurSound()
    {
        //* Charge les réglages persistés
        LoadSettingsFromPrefs();
        CompilationPipeline.compilationStarted += OnCompilationStarted;
        CompilationPipeline.compilationFinished += OnCompilationFinished;
        AssemblyReloadEvents.afterAssemblyReload += TryPlayPendingSoundAfterReload;
        Application.logMessageReceived += OnLogMessageReceived;
        //* Sécurité: si pending est déjà posé au chargement
        TryPlayPendingSoundAfterReload();
    }
    //* Charge les réglages persistés
    private static void LoadSettingsFromPrefs()
    {
        errorSoundName = EditorPrefs.GetString(PrefErrorSoundName, "error.wav");
        succesSoundName = EditorPrefs.GetString(PrefSuccesSoundName, "succes.wav");
        errorSoundVolume = EditorPrefs.GetFloat(PrefErrorSoundVolume, 0.5f);
        succesSoundVolume = EditorPrefs.GetFloat(PrefSuccesSoundVolume, 0.5f);
        //* Clamp au cas où
        errorSoundVolume = Mathf.Clamp01(errorSoundVolume);
        succesSoundVolume = Mathf.Clamp01(succesSoundVolume);
    }

    public static void SaveSettingsToPrefs()
    {
        EditorPrefs.SetString(PrefErrorSoundName, errorSoundName);
        EditorPrefs.SetString(PrefSuccesSoundName, succesSoundName);
        EditorPrefs.SetFloat(PrefErrorSoundVolume, Mathf.Clamp01(errorSoundVolume));
        EditorPrefs.SetFloat(PrefSuccesSoundVolume, Mathf.Clamp01(succesSoundVolume));
    }


    private static void OnCompilationStarted(object _)
    {
        capturingErrors = true;
        hasErrors = false;
    }

    

    private static void OnCompilationFinished(object _)
    {
         SessionState.SetBool(KeyPending, true);
        SessionState.SetBool(KeyHadErrors, hasErrors); // * état initial
        finishedAt = EditorApplication.timeSinceStartup;

        EditorApplication.update -= TryFinalizeAndPlay;
        EditorApplication.update += TryFinalizeAndPlay;
    }

    private static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
    {
        if (!capturingErrors) return;
        if (type != LogType.Error) return;
        if (string.IsNullOrEmpty(condition)) return;

        if (condition.IndexOf("error CS", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            hasErrors = true;
            SessionState.SetBool(KeyHadErrors, true); // * MAJ même après compilationFinished
        }
    }

    private static void TryFinalizeAndPlay()
    {
        if (EditorApplication.timeSinceStartup < finishedAt + 0.5) return;
        TryPlayPendingSoundAfterReload();
        EditorApplication.update -= TryFinalizeAndPlay;
    }

    /// <summary>
    /// Résout le dossier Sounds à côté de ce script (Assets ou package UPM).
    /// </summary>
    private static string GetSoundsFolderAssetPath()
    {
        string[] guids = AssetDatabase.FindAssets("CompilateurSound t:MonoScript");
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(assetPath) || !assetPath.EndsWith("CompilateurSound.cs", StringComparison.OrdinalIgnoreCase))
                continue;

            string scriptDir = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(scriptDir))
                continue;

            // * Important: AssetDatabase.IsValidFolder attend un chemin "Assets/..."
            string soundsFolderAssetPath = (scriptDir + "/Sounds").Replace('\\', '/');
            // * Vérifie si le dossier existe dans l'Asset Database
            if (AssetDatabase.IsValidFolder(soundsFolderAssetPath))
            return soundsFolderAssetPath;
        }

        Debug.LogWarning("CompileErrorSound: impossible de localiser le dossier Sounds (CompilateurSound.cs introuvable).");
        return null;
    }

    private static void TryPlayPendingSoundAfterReload()
    {
        if (!SessionState.GetBool(KeyPending, false))
            return;

        bool hadErrors = SessionState.GetBool(KeyHadErrors, false);

        // * Nettoyage avant lecture pour éviter les doublons si exception.
        SessionState.EraseBool(KeyPending);
        SessionState.EraseBool(KeyHadErrors);

        string fileName = hadErrors ? errorSoundName : succesSoundName;
        float volume = hadErrors ? errorSoundVolume : succesSoundVolume;

        string soundsFolderAssetPath = GetSoundsFolderAssetPath();
        if (string.IsNullOrEmpty(soundsFolderAssetPath))
            return;

        string clipAssetPath = (soundsFolderAssetPath + "/" + fileName).Replace('\\', '/');
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipAssetPath);

        if (clip == null){
            Debug.LogWarning($"CompileErrorSound: fichier audio introuvable: {clipAssetPath}");
            return;
        }
        PlayClipWithVolume(clip, volume);
    }


    private static void PlayClipWithVolume(AudioClip clip, float volume)
    {
        //* Crée un GameObject temporaire pour jouer le son
        var go = new GameObject("CompilateurSound_AudioSource");
        go.hideFlags = HideFlags.HideAndDontSave;

        var source = go.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;

        source.Play();

        double destroyAt = EditorApplication.timeSinceStartup + (clip.length > 0.0f ? clip.length : 1.0f);

        void OnUpdate()
        {
            if(EditorApplication.timeSinceStartup >= destroyAt)
            {
                EditorApplication.update -= OnUpdate;
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
        EditorApplication.update += OnUpdate;
    }
}
#endif
