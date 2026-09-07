using System;
using System.IO;
using DodgerMover.Presentation;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace DodgerMover.Editor
{
    public static class ImpactProject
    {
        public const string ScenePath = "Assets/DodgerMover/Scenes/ImpactProof.unity";
        private const string Settings = "Assets/DodgerMover/Settings";

        [MenuItem("Dodger Mover/Configure Impact Proof")]
        public static void Configure()
        {
            Directory.CreateDirectory("Assets/DodgerMover/Scenes");
            Directory.CreateDirectory(Settings);
            AssetDatabase.Refresh();
            var renderer = AssetDatabase.LoadAssetAtPath<Renderer2DData>(Settings + "/ImpactRenderer.asset");
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<Renderer2DData>();
                AssetDatabase.CreateAsset(renderer, Settings + "/ImpactRenderer.asset");
            }
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Settings + "/ImpactPipeline.asset");
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                pipeline.msaaSampleCount = 1;
                pipeline.supportsHDR = false;
                AssetDatabase.CreateAsset(pipeline, Settings + "/ImpactPipeline.asset");
            }
            GraphicsSettings.defaultRenderPipeline = pipeline;
            for (int level = 0; level < QualitySettings.names.Length; level++)
            {
                QualitySettings.SetQualityLevel(level);
                QualitySettings.renderPipeline = pipeline;
            }
            var material = AssetDatabase.LoadAssetAtPath<Material>(Settings + "/Silhouette.mat");
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (shader == null) throw new InvalidOperationException("URP sprite shader unavailable.");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, Settings + "/Silhouette.mat");
            }
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Impact Proof").AddComponent<ImpactBootstrap>();
            var serialized = new SerializedObject(root);
            serialized.FindProperty("spriteMaterial").objectReferenceValue = material;
            serialized.FindProperty("interfaceFont").objectReferenceValue = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            PlayerSettings.companyName = "YeerooXY";
            PlayerSettings.productName = "Dodger Mover - Impact Proof";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.defaultScreenWidth = 1920; PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.resizableWindow = true; PlayerSettings.runInBackground = false;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.Standalone, ApiCompatibilityLevel.NET_Standard);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 });
            // Set through serialized settings because Unity has no public setter for active input handling.
            var playerSettings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            playerSettings.FindProperty("activeInputHandler").intValue = 1;
            playerSettings.ApplyModifiedPropertiesWithoutUndo();
            Time.fixedDeltaTime = 1f / 60;
            EditorSettings.serializationMode = SerializationMode.ForceText;
            AssetDatabase.SaveAssets();
            Debug.Log("IMPACT_SETUP_OK: scene, URP, Input System, and Windows settings serialized.");
        }

        [MenuItem("Dodger Mover/Build Windows Playtest")]
        public static void BuildWindows()
        {
            string output = Environment.GetEnvironmentVariable("DODGER_BUILD_PATH");
            if (string.IsNullOrWhiteSpace(output)) output = "Builds/S001-ImpactProof/DodgerMover.exe";
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath }, locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Windows build failed: " + report.summary.result);
            Debug.Log("IMPACT_BUILD_OK: " + report.summary.totalSize + " bytes, " + report.summary.totalTime);
        }
    }
}
