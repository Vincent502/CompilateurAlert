using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.IO;
using System;


public class WindowEditor : EditorWindow
{
    private static string cachedSounsdFolder;
    private static string[] cachedWavNames;
    private List<string> wavNamesSucces = new List<string>();
    private List<string> wavNamesError = new List<string>();
    public void OnEnable()
    {
         wavNamesError = GetWavFileNames();
         wavNamesSucces = GetWavFileNames();
    }
    // getComponent<CompilateurSound> compilateurSound;
    //* MenuItem to display the window
    [MenuItem("CompilateurAlert/Settings Window")]
    //* Method to display the window
    public static void ShowExample()
    {
        WindowEditor wnd = GetWindow<WindowEditor>();
        wnd.titleContent = new GUIContent("Compilateur Alert Settings Window");
    }

    private List<string> GetWavFileNames()
    {
        if (cachedWavNames != null)
            return new List<string>(cachedWavNames);
        string soundsFolder = GetSoundsFolderFullPath();
        cachedSounsdFolder = soundsFolder; //* same name as the declaration
        if (string.IsNullOrEmpty(soundsFolder) || !Directory.Exists(soundsFolder))
        {
            cachedWavNames = Array.Empty<string>();
            return new List<string>(cachedWavNames);
        }
        string[] fullPaths = Directory.GetFiles(soundsFolder, "*.wav");
        string[] names = new string[fullPaths.Length];
        for (int i = 0; i < fullPaths.Length; i++)
            names[i] = Path.GetFileName(fullPaths[i]);
        Array.Sort(names, StringComparer.OrdinalIgnoreCase);
        cachedWavNames = names;
        return new List<string>(cachedWavNames);
    }

    private string GetSoundsFolderFullPath()
    {
        //* Resolve the Sounds folder next to this script (works in Assets or in UPM package)
        string[] guids = AssetDatabase.FindAssets("CompilateurSound t:MonoScript");
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(assetPath) ||
                !assetPath.EndsWith("CompilateurSound.cs", StringComparison.OrdinalIgnoreCase))
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
        Debug.LogWarning("CompilateurAlert: impossible de localiser le dossier Sounds (CompilateurSound.cs introuvable).");
        return null;
    }



    //* Method to create the window
    public void CreateGUI()
    {
        //* Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        //* Create a dropdown to select the sound file error
        DropdownField dropdownError = new DropdownField();
        dropdownError.name = "dropdownError";
        dropdownError.label = "Error Sound File";
        dropdownError.choices = new List<string> (wavNamesError);
        dropdownError.value = CompilateurSound.errorSoundName;
        dropdownError.style.marginTop = 15;
        dropdownError.RegisterValueChangedCallback((evt) => {
            Debug.Log(evt.newValue);
            CompilateurSound.errorSoundName = evt.newValue;
            });
        root.Add(dropdownError);

        //* VisualElements objects can contain other VisualElement following a tree hierarchy
        Label labelError = new Label("Volume of the error sound");
        labelError.style.marginTop = 25;
        root.Add(labelError);

        //* Create slider volume error sound
        Slider sliderErrorVolume = new Slider();
        sliderErrorVolume.name = "sliderErrorVolume";
        sliderErrorVolume.label = " Volume";
        sliderErrorVolume.lowValue = 0.0f;
        sliderErrorVolume.highValue = 1.0f;
        sliderErrorVolume.value = CompilateurSound.errorSoundVolume;
        sliderErrorVolume.RegisterValueChangedCallback((evt) =>{
            Debug.Log(evt.newValue);
            CompilateurSound.errorSoundVolume = evt.newValue;
            });
        root.Add(sliderErrorVolume);

        //* Create a dropdown to select the sound file succes
        DropdownField dropdownSucces    = new DropdownField();
        dropdownSucces.name = "dropdownSucces";
        dropdownSucces.label = "Succes Sound File";
        dropdownSucces.choices = new List<string> (wavNamesSucces);
        dropdownSucces.value = CompilateurSound.succesSoundName;
        dropdownSucces.style.marginTop = 15;
        dropdownSucces.RegisterValueChangedCallback((evt) => {
            Debug.Log(evt.newValue);
            CompilateurSound.succesSoundName = evt.newValue;
            });
        root.Add(dropdownSucces);


        //* VisualElements objects can contain other VisualElement following a tree hierarchy
        Label labelSuccess = new Label("Volume of the success sound");
        labelSuccess.style.marginTop = 25;
        root.Add(labelSuccess);

        //* Create slider volume success sound
        Slider sliderSuccessVolume = new Slider();
        sliderSuccessVolume.name = "sliderSuccessVolume";
        sliderSuccessVolume.label = " Volume";
        sliderSuccessVolume.lowValue = 0.0f;
        sliderSuccessVolume.highValue = 1.0f;
        sliderSuccessVolume.value = CompilateurSound.succesSoundVolume;
        sliderSuccessVolume.RegisterValueChangedCallback((evt) => {
            Debug.Log(evt.newValue);
            CompilateurSound.succesSoundVolume = evt.newValue;
            });
        root.Add(sliderSuccessVolume);

        //* Create Button comfirm settings
        Button SoundConfirm = new Button();
        SoundConfirm.name = "Confirm settings";
        SoundConfirm.text = "Confirm settings";
        SoundConfirm.style.marginTop = 25;
        SoundConfirm.clicked += () => SoundConfirmSettings();
        root.Add(SoundConfirm);  
          
        //* Create button close window
        Button buttonClose = new Button();
        buttonClose.name = "Quit";
        buttonClose.text = "Quit";
        buttonClose.style.marginTop = 25;
        buttonClose.clicked += () => Quit();
        root.Add(buttonClose);
    }
    //* Method to quit the application
    public void Quit()
    {
        GetWindow<WindowEditor>().Close();

    }
    public void SoundConfirmSettings()
    {
        CompilateurSound.SaveSettingsToPrefs();
        Debug.Log("Settings saved");
    }

}