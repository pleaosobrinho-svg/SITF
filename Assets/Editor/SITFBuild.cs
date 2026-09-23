using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.IO;

#if UNITY_EDITOR
public static class SITFBuild
{
    [MenuItem("SITF/Build Android APK")]
    public static void BuildAndroid()
    {
        Directory.CreateDirectory("Build");
        BuildTargetGroup group=BuildTargetGroup.Android;
        EditorUserBuildSettings.SwitchActiveBuildTarget(group,BuildTarget.Android);
        PlayerSettings.productName="SITF — Silence in the Fire";
        PlayerSettings.companyName="SITF Studio";
        PlayerSettings.bundleVersion="0.2.0";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,"com.sitf.silenceinthefire");
        PlayerSettings.defaultScreenOrientation=UIOrientation.LandscapeLeft;
        PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel24;
        PlayerSettings.Android.targetSdkVersion=AndroidSdkVersions.AndroidApiLevel35;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android,ManagedStrippingLevel.Medium);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3});
        EditorUserBuildSettings.buildAppBundle=false;
        PlayerSettings.Android.useCustomKeystore=false;

        BuildReport report=BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes=new[]{"Assets/Scenes/Main.unity"},
            locationPathName="Build/SITF.apk",
            target=BuildTarget.Android,
            options=BuildOptions.None
        });
        if(report.summary.result!=BuildResult.Succeeded)
            throw new System.Exception("SITF Android build failed: "+report.summary.result);
    }
}
#endif
