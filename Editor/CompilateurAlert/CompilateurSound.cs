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
    private const string KeyPending = "CompilateurSound.Pending";
    private const string KeyHadErrors = "CompilateurSound.HadErrors";

    private static bool capturingErrors;
    private static bool hasErrors;

    private static double finishedAt;

    static CompilateurSound()
    {
        CompilationPipeline.compilationStarted += OnCompilationStarted;
        CompilationPipeline.compilationFinished += OnCompilationFinished;

        // * Après domain reload, on rejoue l'action en attente.
        AssemblyReloadEvents.afterAssemblyReload += TryPlayPendingSoundAfterReload;

        Application.logMessageReceived += OnLogMessageReceived;

        // * Sécurité: si pending est déjà posé au chargement, tente immédiatement.
        TryPlayPendingSoundAfterReload();
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

    private static void TryPlayPendingSoundAfterReload()
    {
        if (!SessionState.GetBool(KeyPending, false))
            return;

        bool hadErrors = SessionState.GetBool(KeyHadErrors, false);

        // * Nettoyage avant lecture pour éviter les doublons si exception.
        SessionState.EraseBool(KeyPending);
        SessionState.EraseBool(KeyHadErrors);

        string fileName = hadErrors ? "error.wav" : "succes.wav";
        string soundsFolder = GetSoundsFolderFullPath();
        if (string.IsNullOrEmpty(soundsFolder))
            return;

        string fullPath = Path.Combine(soundsFolder, fileName);
        PlaySoundWavFile(fullPath);
    }

    /// <summary>
    /// Résout le dossier Sounds à côté de ce script (Assets ou package UPM).
    /// </summary>
    private static string GetSoundsFolderFullPath()
    {
        string[] guids = AssetDatabase.FindAssets("CompilateurSound t:MonoScript");
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(assetPath) || !assetPath.EndsWith("CompilateurSound.cs", StringComparison.OrdinalIgnoreCase))
                continue;

            string scriptDir = Path.GetDirectoryName(assetPath);
            if (string.IsNullOrEmpty(scriptDir))
                continue;

            string soundsRelative = (scriptDir + "/Sounds").Replace('\\', '/');
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string fullPath = Path.GetFullPath(
                Path.Combine(projectRoot, soundsRelative.Replace('/', Path.DirectorySeparatorChar)));

            if (Directory.Exists(fullPath))
                return fullPath;
        }

        Debug.LogWarning("CompileErrorSound: impossible de localiser le dossier Sounds (CompilateurSound.cs introuvable).");
        return null;
    }

    private static void PlaySoundWavFile(string fullPath)
    {
        try
        {
            if (!File.Exists(fullPath))
            {
                Debug.LogWarning($"CompileErrorSound: fichier audio introuvable: {fullPath}");
                return;
            }

            var player = new SoundPlayer(fullPath);
            player.Load();
            player.PlaySync(); // * garantit la lecture complète
        }
        catch (Exception ex)
        {
            Debug.LogWarning("CompileErrorSound: erreur lecture son: " + ex.Message);
        }
    }
}
#endif
