using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;

#if UNITY_EDITOR_OSX
using UnityEditor.iOS.Xcode;
using UnityEditor.iOS.Xcode.Extensions;
#endif

public class Builder
{
    [PostProcessBuild(100)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.iOS)
        {
            return;
        }

#if UNITY_EDITOR_OSX
        string projPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
        var proj = new PBXProject();
        proj.ReadFromString(File.ReadAllText(projPath));

        string targetGuid = proj.GetUnityMainTargetGuid();

        string[] sdkPaths = new string[]{
            "Frameworks/com.netease.game.rtc/Runtime/Plugins/iOS/nertc-c-sdk.framework"
        };

        foreach (var sdkPath in sdkPaths)
        {
            string sdkGuid = proj.FindFileGuidByRealPath(sdkPath, PBXSourceTree.Source);
            Debug.Log($"sdkGuid:{sdkGuid},sdkPath:{sdkPath}");
            if (!string.IsNullOrEmpty(sdkGuid))
            {
                proj.AddFileToEmbedFrameworks(targetGuid, sdkGuid);
            }
        }

        //save to projecy
        File.WriteAllText(projPath, proj.WriteToString());
#endif
    }
}

[InitializeOnLoad]
public class PreloadKeystoreSetting
{
#if UNITY_ANDROID
    static PreloadKeystoreSetting()
    {
        Debug.Log("PreloadKeystoreSetting");
        PlayerSettings.Android.keystoreName = "user.keystore";
        PlayerSettings.Android.keyaliasName = "nertc.examples";
        PlayerSettings.Android.keystorePass = "111111";
        PlayerSettings.Android.keyaliasPass = "111111";
    }
#endif
}

