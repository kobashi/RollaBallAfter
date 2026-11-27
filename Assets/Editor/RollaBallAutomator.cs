using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.Build.Reporting;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

public static class RollaBallAutomator
{
    private static AudioMixerGroup masterMixerGroup;
    private static AudioClip pickupClip;
    private static AudioClip wallBumpClip;

    public static void Run()
    {
        Debug.Log("==================================================");
        Debug.Log(">>> Starting RollaBall GP08-10 Setup & Build Pipeline <<<");
        Debug.Log("==================================================");

        try
        {
            EnsureFolders();
            AddTag("PickUp");
            AddTag("Player");

            AssetDatabase.Refresh();
            LoadAudioClips();
            CreateAudioMixer();

            CreateMaterials();
            CreatePickUpPrefab();
            CreateWallsPrefab();
            CreatePlayerSetPrefab();
            CreateAnimations();

            CreateOpeningScene();
            CreateMiniGameScene("Assets/Scenes/MiniGame.unity", 12, 10f, "Assets/Materials/Background.mat", null, 0);
            CreateMiniGameScene("Assets/Scenes/MiniGame1.unity", 8, 12f, "Assets/Materials/Background1.mat", "Assets/Materials/Wall1.mat", 1);
            CreateMiniGameScene("Assets/Scenes/MiniGame2.unity", 6, 14f, "Assets/Materials/Background2.mat", "Assets/Materials/Wall2.mat", 2);
            CreateMiniGame3PistonScene();
            CreateEndingScene();

            BuildGame();
            Debug.Log(">>> RollaBall GP08-10 Pipeline Completed Successfully! <<<");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error in RollaBall Pipeline: " + ex.ToString());
            EditorApplication.Exit(1);
        }
    }

    private static void EnsureFolders()
    {
        string[] folders = { "Materials", "Prefabs", "Scenes", "Sounds", "Animations" };
        foreach (string f in folders)
        {
            if (!AssetDatabase.IsValidFolder("Assets/" + f))
            {
                AssetDatabase.CreateFolder("Assets", f);
            }
        }
    }

    private static void AddTag(string tag)
    {
        Object[] asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (asset != null && asset.Length > 0)
        {
            SerializedObject so = new SerializedObject(asset[0]);
            SerializedProperty tags = so.FindProperty("tags");
            for (int i = 0; i < tags.arraySize; i++)
            {
                if (tags.GetArrayElementAtIndex(i).stringValue.Equals(tag))
                {
                    return;
                }
            }
            // Built-in tags like "Player" do not need to be added to custom tags list
            if (tag == "Player") return;

            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
            so.ApplyModifiedProperties();
            so.Update();
            Debug.Log($"Tag '{tag}' added successfully.");
        }
    }

    private static void LoadAudioClips()
    {
        pickupClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/pickup_se.wav");
        wallBumpClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/wall_bump_se.wav");
        if (pickupClip == null) Debug.LogWarning("pickup_se.wav not found.");
        if (wallBumpClip == null) Debug.LogWarning("wall_bump_se.wav not found.");
    }

    private static void CreateAudioMixer()
    {
        string mixerPath = "Assets/Sounds/MainAudioMixer.mixer";
        AudioMixer existing = AssetDatabase.LoadAssetAtPath<AudioMixer>(mixerPath);
        if (existing != null)
        {
            var groups = existing.FindMatchingGroups("Master");
            if (groups != null && groups.Length > 0) masterMixerGroup = groups[0];
            return;
        }

        System.Type mixerControllerType = System.Type.GetType("UnityEditor.Audio.AudioMixerController, UnityEditor");
        if (mixerControllerType != null)
        {
            MethodInfo createMethod = mixerControllerType.GetMethod("CreateMixerControllerAtPath", BindingFlags.Public | BindingFlags.Static);
            if (createMethod != null)
            {
                AudioMixer mixer = createMethod.Invoke(null, new object[] { mixerPath }) as AudioMixer;
                if (mixer != null)
                {
                    AssetDatabase.SaveAssets();
                    var groups = mixer.FindMatchingGroups("Master");
                    if (groups != null && groups.Length > 0) masterMixerGroup = groups[0];
                    Debug.Log("Created AudioMixer at: " + mixerPath);
                }
            }
        }
    }

    private static Material CreateLitMaterial(string path, Color color, float smoothness)
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) urpLit = Shader.Find("Standard");

        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            existing.SetColor("_BaseColor", color);
            existing.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        Material mat = new Material(urpLit);
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Smoothness", smoothness);
        mat.SetFloat("_Metallic", 0f);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static void CreateMaterials()
    {
        CreateLitMaterial("Assets/Materials/Background.mat", new Color(130f / 255f, 130f / 255f, 130f / 255f, 1f), 0.25f);
        CreateLitMaterial("Assets/Materials/Background1.mat", new Color(90f / 255f, 130f / 255f, 110f / 255f, 1f), 0.25f);
        CreateLitMaterial("Assets/Materials/Background2.mat", new Color(110f / 255f, 100f / 255f, 145f / 255f, 1f), 0.25f);
        CreateLitMaterial("Assets/Materials/Wall1.mat", new Color(200f / 255f, 120f / 255f, 80f / 255f, 1f), 0.3f);
        CreateLitMaterial("Assets/Materials/Wall2.mat", new Color(80f / 255f, 150f / 255f, 200f / 255f, 1f), 0.3f);
        CreateLitMaterial("Assets/Materials/Player.mat", new Color(0f / 255f, 220f / 255f, 255f / 255f, 1f), 0.75f);
        CreateLitMaterial("Assets/Materials/PickUp.mat", new Color(255f / 255f, 200f / 255f, 0f / 255f, 1f), 0.25f);
        AssetDatabase.SaveAssets();
    }

    private static void CreatePickUpPrefab()
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "PickUp";
        go.tag = "PickUp";

        Material pickUpMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/PickUp.mat");
        if (pickUpMat != null) go.GetComponent<MeshRenderer>().sharedMaterial = pickUpMat;

        BoxCollider bc = go.GetComponent<BoxCollider>();
        if (bc != null) bc.isTrigger = true;

        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;

        go.AddComponent<Rotator>();

        AudioSource audio = go.AddComponent<AudioSource>();
        audio.clip = pickupClip;
        audio.playOnAwake = false;
        if (masterMixerGroup != null) audio.outputAudioMixerGroup = masterMixerGroup;

        go.transform.position = new Vector3(0f, 0.5f, 0f);
        go.transform.rotation = Quaternion.Euler(45f, 45f, 45f);
        go.transform.localScale = Vector3.one * 0.5f;

        PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/PickUp.prefab");
        Object.DestroyImmediate(go);
        AssetDatabase.SaveAssets();
    }

    private static GameObject CreateSingleWallPrefab(string name, Vector3 localPos, Vector3 localScale)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.localPosition = localPos;
        wall.transform.localRotation = Quaternion.identity;
        wall.transform.localScale = localScale;

        BoxCollider col = wall.GetComponent<BoxCollider>();
        if (col != null) col.isTrigger = false;

        AudioSource audio = wall.AddComponent<AudioSource>();
        audio.clip = wallBumpClip;
        audio.playOnAwake = false;
        if (masterMixerGroup != null) audio.outputAudioMixerGroup = masterMixerGroup;

        wall.AddComponent<WallBump>();
        return wall;
    }

    private static void CreateWallsPrefab()
    {
        GameObject northWall = CreateSingleWallPrefab("North Wall", new Vector3(0f, 0f, 10f), new Vector3(20.5f, 2f, 0.5f));
        GameObject northPrefab = PrefabUtility.SaveAsPrefabAsset(northWall, "Assets/Prefabs/NorthWall.prefab");
        Object.DestroyImmediate(northWall);

        GameObject walls = new GameObject("Walls");
        walls.transform.position = Vector3.zero;

        InstantiateWallFromPrefab(northPrefab, "West Wall", walls.transform, new Vector3(-10f, 0f, 0f), new Vector3(0.5f, 2f, 20.5f));
        InstantiateWallFromPrefab(northPrefab, "East Wall", walls.transform, new Vector3(10f, 0f, 0f), new Vector3(0.5f, 2f, 20.5f));
        InstantiateWallFromPrefab(northPrefab, "North Wall", walls.transform, new Vector3(0f, 0f, 10f), new Vector3(20.5f, 2f, 0.5f));
        InstantiateWallFromPrefab(northPrefab, "South Wall", walls.transform, new Vector3(0f, 0f, -10f), new Vector3(20.5f, 2f, 0.5f));

        PrefabUtility.SaveAsPrefabAsset(walls, "Assets/Prefabs/Walls.prefab");
        Object.DestroyImmediate(walls);
        AssetDatabase.SaveAssets();
    }

    private static void InstantiateWallFromPrefab(GameObject prefab, string name, Transform parent, Vector3 pos, Vector3 scale)
    {
        GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        inst.name = name;
        inst.transform.localPosition = pos;
        inst.transform.localRotation = Quaternion.identity;
        inst.transform.localScale = scale;
    }

    private static TMP_FontAsset GetDefaultFont()
    {
        TMP_FontAsset fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (fontAsset == null)
        {
            fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        }
        return fontAsset;
    }

    private static void CreatePlayerSetPrefab()
    {
        GameObject playerSet = new GameObject("PlayerSet");
        playerSet.transform.position = Vector3.zero;
        playerSet.transform.rotation = Quaternion.identity;
        playerSet.transform.localScale = Vector3.one;

        // 1. Main Camera
        GameObject camGo = new GameObject("Main Camera");
        camGo.transform.SetParent(playerSet.transform, false);
        camGo.AddComponent<Camera>();
        camGo.tag = "MainCamera";
        camGo.AddComponent<AudioListener>();
        camGo.transform.localPosition = new Vector3(0f, 10f, -10f);
        camGo.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
        var urpCamType = System.Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
        if (urpCamType != null) camGo.AddComponent(urpCamType);
        CameraController camController = camGo.AddComponent<CameraController>();

        // 2. Canvas
        GameObject canvasGo = CreateCanvas("Canvas", playerSet.transform);
        TMP_FontAsset fontAsset = GetDefaultFont();

        GameObject countTextGo = CreateTMPText("CountText", canvasGo.transform, fontAsset, "Count: 0", 28f, Color.white, FontStyles.Normal, TextAlignmentOptions.TopLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(10f, -10f), new Vector2(300f, 60f));
        GameObject winTextGo = CreateTMPText("WinText", canvasGo.transform, fontAsset, "You Win!", 32f, Color.black, FontStyles.Bold, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 130f), new Vector2(350f, 80f));

        // 3. EventSystem
        GameObject eventSystemGo = new GameObject("EventSystem");
        eventSystemGo.transform.SetParent(playerSet.transform, false);
        eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystemGo.AddComponent<InputSystemUIInputModule>();

        // 4. Player
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        player.name = "Player";
        player.tag = "Player";
        player.transform.SetParent(playerSet.transform, false);
        player.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        player.transform.localScale = Vector3.one;
        Material playerMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Player.mat");
        if (playerMat != null) player.GetComponent<MeshRenderer>().sharedMaterial = playerMat;

        Rigidbody playerRb = player.AddComponent<Rigidbody>();
        playerRb.isKinematic = false;
        playerRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        PlayerInput playerInput = player.AddComponent<PlayerInput>();
        InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
        if (inputActions != null)
        {
            playerInput.actions = inputActions;
            playerInput.defaultActionMap = "Player";
            playerInput.defaultControlScheme = "Keyboard&Mouse";
        }
        playerInput.notificationBehavior = PlayerNotifications.SendMessages;

        PlayerController playerController = player.AddComponent<PlayerController>();
        playerController.speed = 10f;
        playerController.item_count = 12;
        playerController.countText = countTextGo.GetComponent<TextMeshProUGUI>();
        playerController.winTextObject = winTextGo;

        camController.player = player;

        PrefabUtility.SaveAsPrefabAsset(playerSet, "Assets/Prefabs/PlayerSet.prefab");
        Object.DestroyImmediate(playerSet);
        AssetDatabase.SaveAssets();
    }

    private static GameObject CreateCanvas(string name, Transform parent)
    {
        GameObject canvasGo = new GameObject(name);
        if (parent != null) canvasGo.transform.SetParent(parent, false);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();
        return canvasGo;
    }

    private static GameObject CreateTMPText(string name, Transform parent, TMP_FontAsset fontAsset, string text, float size, Color color, FontStyles style, TextAlignmentOptions align,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) tmp.font = fontAsset;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = align;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        return go;
    }

    private static void CreateAnimations()
    {
        // 1. p_anim.anim (0 -> 2 -> 0 over 1.0s)
        AnimationClip pAnim = new AnimationClip();
        AnimationCurve pCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.5f, 2.0f),
            new Keyframe(1.0f, 0f)
        );
        pAnim.SetCurve("", typeof(Transform), "localPosition.y", pCurve);
        AnimationClipSettings pSettings = AnimationUtility.GetAnimationClipSettings(pAnim);
        pSettings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(pAnim, pSettings);
        AssetDatabase.CreateAsset(pAnim, "Assets/Animations/p_anim.anim");

        // 2. w_anim.anim (idle at 0 over 1.0s)
        AnimationClip wAnim = new AnimationClip();
        AnimationCurve wCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(1.0f, 0f)
        );
        wAnim.SetCurve("", typeof(Transform), "localPosition.y", wCurve);
        AnimationClipSettings wSettings = AnimationUtility.GetAnimationClipSettings(wAnim);
        wSettings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(wAnim, wSettings);
        AssetDatabase.CreateAsset(wAnim, "Assets/Animations/w_anim.anim");

        // 3. piston.controller
        AnimatorController pistonCtrl = AnimatorController.CreateAnimatorControllerAtPath("Assets/Animations/piston.controller");
        pistonCtrl.AddMotion(pAnim);

        // 4. TriggerAnim.controller
        AnimatorController triggerCtrl = AnimatorController.CreateAnimatorControllerAtPath("Assets/Animations/TriggerAnim.controller");
        triggerCtrl.AddParameter("keyP", AnimatorControllerParameterType.Trigger);
        AnimatorStateMachine sm = triggerCtrl.layers[0].stateMachine;
        AnimatorState wState = sm.AddState("w_anim");
        wState.motion = wAnim;
        AnimatorState pState = sm.AddState("p_anim");
        pState.motion = pAnim;
        sm.defaultState = wState;

        AnimatorStateTransition t1 = wState.AddTransition(pState);
        t1.hasExitTime = false;
        t1.AddCondition(AnimatorConditionMode.If, 0, "keyP");

        AnimatorStateTransition t2 = pState.AddExitTransition();
        t2.hasExitTime = true;
        t2.exitTime = 1.0f;

        AssetDatabase.SaveAssets();
    }

    private static void CreateMiniGameScene(string scenePath, int itemCount, float speed, string groundMatPath, string wallMatPath, int stageVariant)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Light
        GameObject lightGo = new GameObject("Directional Light");
        Light light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = Color.white;
        lightGo.transform.rotation = Quaternion.Euler(50f, 50f, 0f);

        // Ground
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(2f, 1f, 2f);
        Material bgMat = AssetDatabase.LoadAssetAtPath<Material>(groundMatPath);
        if (bgMat != null) ground.GetComponent<MeshRenderer>().sharedMaterial = bgMat;

        // Walls Prefab
        GameObject wallsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Walls.prefab");
        GameObject walls = (GameObject)PrefabUtility.InstantiatePrefab(wallsPrefab);
        if (!string.IsNullOrEmpty(wallMatPath))
        {
            Material wMat = AssetDatabase.LoadAssetAtPath<Material>(wallMatPath);
            if (wMat != null)
            {
                foreach (MeshRenderer mr in walls.GetComponentsInChildren<MeshRenderer>())
                {
                    mr.sharedMaterial = wMat;
                }
            }
        }

        // Extra obstacles for stages 1 & 2
        if (stageVariant == 1)
        {
            GameObject northPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/NorthWall.prefab");
            InstantiateWallFromPrefab(northPrefab, "Center Wall", walls.transform, new Vector3(0f, 0f, 3.5f), new Vector3(8f, 2f, 0.5f));
        }
        else if (stageVariant == 2)
        {
            GameObject northPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/NorthWall.prefab");
            InstantiateWallFromPrefab(northPrefab, "Obstacle Wall 1", walls.transform, new Vector3(-3.5f, 0f, 2f), new Vector3(0.5f, 2f, 8f));
            InstantiateWallFromPrefab(northPrefab, "Obstacle Wall 2", walls.transform, new Vector3(3.5f, 0f, -2f), new Vector3(0.5f, 2f, 8f));
        }

        // PlayerSet Prefab
        GameObject playerSetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PlayerSet.prefab");
        GameObject playerSet = (GameObject)PrefabUtility.InstantiatePrefab(playerSetPrefab);
        PlayerController pc = playerSet.GetComponentInChildren<PlayerController>();
        if (pc != null)
        {
            pc.item_count = itemCount;
            pc.speed = speed;
        }

        // PickUps
        GameObject pickupParent = new GameObject("PickUp Parent");
        GameObject pickupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PickUp.prefab");
        float radius = stageVariant == 0 ? 7f : (stageVariant == 1 ? 6f : 7.5f);
        for (int i = 0; i < itemCount; i++)
        {
            float angle = (Mathf.PI * 2f / itemCount) * i;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            GameObject item = (GameObject)PrefabUtility.InstantiatePrefab(pickupPrefab, pickupParent.transform);
            item.transform.localPosition = new Vector3(x, 0.5f, z);
            item.name = "PickUp";
        }

        EditorSceneManager.SaveScene(scene, scenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("Saved scene: " + scenePath);
    }

    private static void CreateMiniGame3PistonScene()
    {
        CreateMiniGameScene("Assets/Scenes/MiniGame3.unity", 8, 12f, "Assets/Materials/Background1.mat", "Assets/Materials/Wall1.mat", 1);
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/MiniGame3.unity", OpenSceneMode.Single);

        // piston (looping)
        GameObject piston = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        piston.name = "piston";
        piston.transform.position = new Vector3(1f, 0f, 5f);
        Animator anim1 = piston.AddComponent<Animator>();
        anim1.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/piston.controller");

        // piston(1) (TriggerKeyP)
        GameObject piston1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        piston1.name = "piston(1)";
        piston1.transform.position = new Vector3(-2f, 0f, 5f);
        Animator anim2 = piston1.AddComponent<Animator>();
        anim2.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/TriggerAnim.controller");
        anim2.applyRootMotion = true;
        piston1.AddComponent<TriggerKeyP>();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MiniGame3.unity");
        AssetDatabase.SaveAssets();
        Debug.Log("Saved piston practice scene: Assets/Scenes/MiniGame3.unity");
    }

    private static void CreateOpeningScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateSimpleUISceneCamera();

        GameObject canvasGo = CreateCanvas("Canvas", null);
        TMP_FontAsset fontAsset = GetDefaultFont();

        CreateTMPText("Title", canvasGo.transform, fontAsset, "Roll a Ball", 64f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 100f), new Vector2(600f, 120f));

        // Button
        GameObject btnGo = new GameObject("Button");
        btnGo.transform.SetParent(canvasGo.transform, false);
        Image btnImg = btnGo.AddComponent<Image>();
        btnImg.color = Color.white;
        Button btn = btnGo.AddComponent<Button>();
        RectTransform btnRt = btnGo.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.5f, 0.5f);
        btnRt.anchorMax = new Vector2(0.5f, 0.5f);
        btnRt.pivot = new Vector2(0.5f, 0.5f);
        btnRt.anchoredPosition = new Vector2(0f, -60f);
        btnRt.sizeDelta = new Vector2(260f, 70f);

        CreateTMPText("Text (TMP)", btnGo.transform, fontAsset, "START", 32f, Color.black, FontStyles.Bold, TextAlignmentOptions.Center,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        GameObject esGo = new GameObject("EventSystem");
        esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esGo.AddComponent<InputSystemUIInputModule>();

        GameObject gameSys = new GameObject("GameSystem");
        OpeningButton openingBtn = gameSys.AddComponent<OpeningButton>();
        UnityEventTools.AddPersistentListener(btn.onClick, openingBtn.StartGame);

        // Practice Button (MiniGame3)
        GameObject pracBtnGo = new GameObject("PistonPracticeButton");
        pracBtnGo.transform.SetParent(canvasGo.transform, false);
        Image pracBtnImg = pracBtnGo.AddComponent<Image>();
        pracBtnImg.color = new Color(0.85f, 0.92f, 1f, 1f);
        Button pracBtn = pracBtnGo.AddComponent<Button>();
        RectTransform pracBtnRt = pracBtnGo.GetComponent<RectTransform>();
        pracBtnRt.anchorMin = new Vector2(0.5f, 0.5f);
        pracBtnRt.anchorMax = new Vector2(0.5f, 0.5f);
        pracBtnRt.pivot = new Vector2(0.5f, 0.5f);
        pracBtnRt.anchoredPosition = new Vector2(0f, -150f);
        pracBtnRt.sizeDelta = new Vector2(260f, 60f);

        CreateTMPText("Text (TMP)", pracBtnGo.transform, fontAsset, "PISTON", 28f, Color.black, FontStyles.Bold, TextAlignmentOptions.Center,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        UnityEventTools.AddPersistentListener(pracBtn.onClick, openingBtn.StartPistonPractice);

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Opening.unity");
        AssetDatabase.SaveAssets();
    }

    private static void CreateEndingScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateSimpleUISceneCamera();

        GameObject canvasGo = CreateCanvas("Canvas", null);
        TMP_FontAsset fontAsset = GetDefaultFont();

        GameObject scoreGo = CreateTMPText("TotalScore", canvasGo.transform, fontAsset, "Total Score: 0", 54f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700f, 120f));
        scoreGo.AddComponent<Ending>();

        GameObject esGo = new GameObject("EventSystem");
        esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esGo.AddComponent<InputSystemUIInputModule>();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Ending.unity");
        AssetDatabase.SaveAssets();
    }

    private static void CreateSimpleUISceneCamera()
    {
        GameObject camGo = new GameObject("Main Camera");
        Camera cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(30f / 255f, 35f / 255f, 50f / 255f, 1f);
        camGo.tag = "MainCamera";
        camGo.AddComponent<AudioListener>();
        var urpCamType = System.Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
        if (urpCamType != null) camGo.AddComponent(urpCamType);
    }

    private static void BuildGame()
    {
        string[] buildScenes = new string[]
        {
            "Assets/Scenes/Opening.unity",
            "Assets/Scenes/MiniGame.unity",
            "Assets/Scenes/MiniGame1.unity",
            "Assets/Scenes/MiniGame2.unity",
            "Assets/Scenes/Ending.unity",
            "Assets/Scenes/MiniGame3.unity"
        };

        List<EditorBuildSettingsScene> ebScenes = new List<EditorBuildSettingsScene>();
        foreach (string s in buildScenes)
        {
            ebScenes.Add(new EditorBuildSettingsScene(s, true));
        }
        EditorBuildSettings.scenes = ebScenes.ToArray();

        string buildDir = "build";
        if (!Directory.Exists(buildDir)) Directory.CreateDirectory(buildDir);
        string exePath = Path.Combine(buildDir, "RollaBall.exe");

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = buildScenes;
        buildPlayerOptions.locationPathName = exePath;
        buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
        buildPlayerOptions.options = BuildOptions.None;

        Debug.Log($"Starting Build to: {exePath}...");
        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"Build SUCCEEDED! Total time: {summary.totalTime.TotalSeconds:F1}s, Size: {summary.totalSize} bytes");
        }
        else
        {
            Debug.LogError($"Build FAILED! Result: {summary.result}, Errors: {summary.totalErrors}");
            throw new System.Exception($"Build failed with result: {summary.result}");
        }
    }
}
