using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

public static class GP11SampleBuilder
{
    public static void BuildSampleScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Directional Light
        GameObject lightGo = new GameObject("Directional Light");
        Light light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = Color.white;
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // Camera overlooking all 3 groups
        GameObject camGo = new GameObject("Main Camera");
        Camera cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.14f, 0.16f, 0.22f, 1f);
        camGo.tag = "MainCamera";
        camGo.AddComponent<AudioListener>();
        camGo.transform.position = new Vector3(0f, 12f, -18f);
        camGo.transform.rotation = Quaternion.Euler(30f, 0f, 0f);
        var urpCamType = System.Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
        if (urpCamType != null) camGo.AddComponent(urpCamType);

        Material matBg = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Background1.mat");
        Material matWall1 = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Wall1.mat");
        Material matWall2 = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Wall2.mat");
        Material matPickUp = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/PickUp.mat");

        // 1. Wave Floor (Grid of pillars)
        GameObject floorGo = new GameObject("Sample_WaveFloor");
        floorGo.transform.position = new Vector3(-11f, 0f, 0f);
        WaveGroupAnimator floorAnim = floorGo.AddComponent<WaveGroupAnimator>();
        floorAnim.layoutMode = WaveGroupAnimator.LayoutMode.Grid;
        floorAnim.countX = 8;
        floorAnim.countZ = 8;
        floorAnim.spacing = new Vector3(1.05f, 0f, 1.05f);
        floorAnim.primitiveType = PrimitiveType.Cube;
        floorAnim.childScale = new Vector3(1f, 2f, 1f);
        floorAnim.childMaterial = matWall2;
        floorAnim.speed = 0.5f;
        floorAnim.phaseStepXDeg = 25f;
        floorAnim.phaseStepZDeg = 25f;
        floorAnim.applyPosition = true;
        floorAnim.posAmplitude = new Vector3(0f, 0.8f, 0f);
        floorAnim.RebuildChildren();

        // 2. Moving Stairs
        GameObject stairsGo = new GameObject("Sample_MovingStairs");
        stairsGo.transform.position = new Vector3(0f, 0f, 0f);
        WaveGroupAnimator stairsAnim = stairsGo.AddComponent<WaveGroupAnimator>();
        stairsAnim.layoutMode = WaveGroupAnimator.LayoutMode.Line;
        stairsAnim.countX = 10;
        stairsAnim.countZ = 1;
        stairsAnim.spacing = new Vector3(0f, 0.35f, 1.1f);
        stairsAnim.primitiveType = PrimitiveType.Cube;
        stairsAnim.childScale = new Vector3(2.5f, 0.4f, 1f);
        stairsAnim.childMaterial = matWall1;
        stairsAnim.speed = 0.5f;
        stairsAnim.phaseStepXDeg = 36f;
        stairsAnim.phaseStepZDeg = 0f;
        stairsAnim.applyPosition = true;
        stairsAnim.posAmplitude = new Vector3(0f, 0.5f, 0f);
        stairsAnim.RebuildChildren();

        // 3. Chained Spinning Blades
        GameObject bladesGo = new GameObject("Sample_SpinningBlades");
        bladesGo.transform.position = new Vector3(10f, 1.5f, 0f);
        WaveGroupAnimator bladesAnim = bladesGo.AddComponent<WaveGroupAnimator>();
        bladesAnim.layoutMode = WaveGroupAnimator.LayoutMode.Line;
        bladesAnim.countX = 8;
        bladesAnim.countZ = 1;
        bladesAnim.spacing = new Vector3(0f, 0f, 1.5f);
        bladesAnim.primitiveType = PrimitiveType.Cube;
        bladesAnim.childScale = new Vector3(3.2f, 0.25f, 0.4f);
        bladesAnim.childMaterial = matPickUp;
        bladesAnim.speed = 1.0f;
        bladesAnim.phaseStepXDeg = 22.5f;
        bladesAnim.phaseStepZDeg = 0f;
        bladesAnim.applyPosition = false;
        bladesAnim.applyRotation = true;
        bladesAnim.continuousSpin = true;
        bladesAnim.spinSpeedDegPerSec = new Vector3(0f, 90f, 0f);
        bladesAnim.RebuildChildren();

        // Ground reference
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = new Vector3(0f, -1.5f, 0f);
        ground.transform.localScale = new Vector3(4f, 1f, 3f);
        if (matBg != null) ground.GetComponent<MeshRenderer>().sharedMaterial = matBg;

        string scenePath = "Assets/Scenes/GP11_WaveSample.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("GP11 Wave Sample Scene created at: " + scenePath);
    }

    public static void CaptureManualScreenshots()
    {
        BuildSampleScene();
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/GP11_WaveSample.unity", OpenSceneMode.Single);

        // Animate all groups to t = 1.25s so waves/stairs/blades show clear phase offset
        WaveGroupAnimator[] anims = Object.FindObjectsByType<WaveGroupAnimator>(FindObjectsSortMode.None);
        foreach (var a in anims)
        {
            a.ApplyAnimationAtTime(1.25f);
        }

        string outDir = Path.Combine(Directory.GetCurrentDirectory(), "docs", "images");
        if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camGo = new GameObject("CaptureCam");
            cam = camGo.AddComponent<Camera>();
        }

        // 1. Overview
        RenderCameraToPng(cam, new Vector3(0f, 12f, -18f), Quaternion.Euler(28f, 0f, 0f),
            Path.Combine(outDir, "overview_sample_scene.png"), 1280, 720);

        // 2. Wave Floor close-up
        RenderCameraToPng(cam, new Vector3(-11f, 7f, -10.5f), Quaternion.Euler(32f, 0f, 0f),
            Path.Combine(outDir, "example_wave_floor.png"), 1280, 720);

        // 3. Moving Stairs close-up
        RenderCameraToPng(cam, new Vector3(-6.5f, 5.5f, -3.5f), Quaternion.Euler(22f, 45f, 0f),
            Path.Combine(outDir, "example_moving_stairs.png"), 1280, 720);

        // 4. Spinning Blades close-up
        RenderCameraToPng(cam, new Vector3(10f, 7.5f, -9.5f), Quaternion.Euler(35f, 0f, 0f),
            Path.Combine(outDir, "example_spinning_blades.png"), 1280, 720);

        // 5. Helix Wave temporary showcase
        GameObject helixGo = new GameObject("Temp_Helix");
        helixGo.transform.position = new Vector3(0f, 3f, -4f);
        WaveGroupAnimator helixAnim = helixGo.AddComponent<WaveGroupAnimator>();
        helixAnim.layoutMode = WaveGroupAnimator.LayoutMode.Line;
        helixAnim.countX = 14;
        helixAnim.countZ = 1;
        helixAnim.spacing = new Vector3(0f, 0f, 0.8f);
        helixAnim.primitiveType = PrimitiveType.Sphere;
        helixAnim.childScale = new Vector3(0.6f, 0.6f, 0.6f);
        helixAnim.childMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Player.mat");
        helixAnim.speed = 0.6f;
        helixAnim.phaseStepXDeg = 25f;
        helixAnim.applyPosition = true;
        helixAnim.posAmplitude = new Vector3(1.5f, 1.5f, 0f);
        helixAnim.posAxisPhaseDeg = new Vector3(0f, 90f, 0f);
        helixAnim.RebuildChildren();
        helixAnim.ApplyAnimationAtTime(1.25f);

        RenderCameraToPng(cam, new Vector3(6.5f, 5.5f, -11f), Quaternion.Euler(20f, -32f, 0f),
            Path.Combine(outDir, "example_helix_wave.png"), 1280, 720);

        Object.DestroyImmediate(helixGo);
        Debug.Log("Captured all manual screenshots to: " + outDir);
    }

    private static void RenderCameraToPng(Camera cam, Vector3 pos, Quaternion rot, string filePath, int width, int height)
    {
        cam.transform.position = pos;
        cam.transform.rotation = rot;

        RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        RenderTexture prevActive = RenderTexture.active;
        RenderTexture prevTarget = cam.targetTexture;

        cam.targetTexture = rt;
        RenderTexture.active = rt;
        cam.Render();

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        cam.targetTexture = prevTarget;
        RenderTexture.active = prevActive;
        Object.DestroyImmediate(rt);

        byte[] bytes = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);
        File.WriteAllBytes(filePath, bytes);
        Debug.Log("Saved screenshot: " + filePath);
    }
}
