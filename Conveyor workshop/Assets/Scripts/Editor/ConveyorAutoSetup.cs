using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ConveyorAutoSetup
{
    [MenuItem("Tools/Conveyor/Generate Full Demo Setup")]
    public static void GenerateFullDemoSetup()
    {
        EnsureFolders();
        Scene scene = EnsureOpenScene();

        CleanupScene(scene);

        GameManager gameManager = CreateGameManager();
        Transform conveyorRoot = new GameObject("ConveyorRoot").transform;
        conveyorRoot.position = Vector3.zero;

        SetupEnvironment();
        SetupCamera();
        SetupPostProcessing();
        SetupDecor();

        List<CargoBox> cargoPrefabs = CreateCargoPrefabs();
        CargoSpawner spawner = CreateSpawner(cargoPrefabs);

        BuildConveyorLayout(conveyorRoot, out DiverterSwitch diverter, out LaneRouter laneRouter);
        BuildReceivers(out ReceiverZone redReceiver, out ReceiverZone blueReceiver, out ReceiverZone greenReceiver);
        CreateOutOfBoundsZone();
        HUDController hud = CreateHud();

        WireSpawner(spawner, cargoPrefabs);
        WireHud(hud, gameManager);
        ConfigureDiverter(dverter: diverter);
        ConfigureRouter(laneRouter, diverter);
        ConfigureReceiver(redReceiver, CargoType.Red);
        ConfigureReceiver(blueReceiver, CargoType.Blue);
        ConfigureReceiver(greenReceiver, CargoType.Green);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Conveyor demo generated. Open SampleScene and press Play.");
    }

    private static Scene EnsureOpenScene()
    {
        const string scenePath = "Assets/Scenes/SampleScene.unity";
        Scene scene = EditorSceneManager.GetActiveScene();
        if (scene.path != scenePath)
        {
            scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }
        return scene;
    }

    private static void CleanupScene(Scene scene)
    {
        var roots = scene.GetRootGameObjects();
        for (int i = roots.Length - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(roots[i]);
        }
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets", "Prefabs");
        EnsureFolder("Assets/Prefabs", "Cargo");
        EnsureFolder("Assets", "Materials");
        EnsureFolder("Assets", "Art");
        EnsureFolder("Assets", "Settings");
    }

    private static void EnsureFolder(string parent, string name)
    {
        string path = $"{parent}/{name}";
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, name);
        }
    }

    private static GameManager CreateGameManager()
    {
        GameObject go = new GameObject("GameManager");
        GameManager gm = go.AddComponent<GameManager>();
        go.AddComponent<ConveyorGameSfx>();
        SerializedObject so = new SerializedObject(gm);
        so.FindProperty("waveDurationSeconds").floatValue = 300f;
        so.FindProperty("waveCount").intValue = 10;
        so.FindProperty("waveStepSpeedBonus").floatValue = 0.26f;
        so.FindProperty("waveStepSpawnBonus").floatValue = 0.36f;
        so.FindProperty("baseConveyorSpeedMultiplier").floatValue = 1.06f;
        so.FindProperty("baseSpawnRateMultiplier").floatValue = 1.1f;
        so.FindProperty("conveyorSpeedRampPerMinute").floatValue = 0.1f;
        so.FindProperty("spawnRateRampPerMinute").floatValue = 0.12f;
        so.FindProperty("finalStretchSeconds").floatValue = 90f;
        so.FindProperty("finalStretchDifficultyFactor").floatValue = 0.88f;
        so.FindProperty("finalSprintSeconds").floatValue = 30f;
        so.FindProperty("finalSprintDifficultyFactor").floatValue = 0.74f;
        so.ApplyModifiedPropertiesWithoutUndo();
        return gm;
    }

    private static void SetupEnvironment()
    {
        GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
        platform.name = "WorkshopPlatform";
        // Single base object only (no extra floor plane). Width/length swapped per scene framing.
        platform.transform.position = new Vector3(0f, -0.12f, 0.2f);
        platform.transform.localScale = new Vector3(58f, 0.2f, 37.6f);
        Material platformMat = CreateMaterialAsset("Platform_Mat", new Color(0.15f, 0.13f, 0.11f));
        platformMat.SetFloat("_Smoothness", 0.42f);
        platformMat.SetFloat("_Metallic", 0.1f);
        ApplySharedNormalMap(platformMat, 0.22f);
        platform.GetComponent<Renderer>().sharedMaterial = platformMat;

        GameObject lightGO = new GameObject("Directional Light");
        Light light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.97f, 0.93f);
        light.intensity = 0.92f;
        light.shadows = LightShadows.Soft;
        light.shadowStrength = 0.48f;
        light.shadowBias = 0.05f;
        lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.08f, 0.09f, 0.12f);
        RenderSettings.fogDensity = 0.0042f;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.17f, 0.18f, 0.21f);
    }

    private static void SetupCamera()
    {
        GameObject camGO = new GameObject("Main Camera");
        Camera cam = camGO.AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.clearFlags = CameraClearFlags.SolidColor;
        // Меньший FOV и ближе к цеху: меньше пустоты за задней стеной; сдвиг по Z к спавну — больше решётки в кадре.
        cam.fieldOfView = 53f;
        cam.allowHDR = true;
        camGO.transform.position = new Vector3(0f, 15.35f, -10.65f);
        camGO.transform.rotation = Quaternion.Euler(55.5f, 0f, 0f);
        cam.backgroundColor = new Color(0.07f, 0.08f, 0.1f);
        camGO.AddComponent<AudioListener>();
        UniversalAdditionalCameraData urpCam = cam.GetUniversalAdditionalCameraData();
        urpCam.renderPostProcessing = true;
    }

    private static void SetupPostProcessing()
    {
        VolumeProfile profile = CreateOrReplaceDemoVolumeProfile();
        GameObject volumeGo = new GameObject("Global Volume");
        Volume volume = volumeGo.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 0f;
        volume.weight = 1f;
        volume.profile = profile;
    }

    private static void SetupDecor()
    {
        // Плоскость стены сразу за приёмниками; боковые стены и рельсы обрезаются по задней грани стены (без «рогов» в пустоту).
        const float backWallZ = 8.22f;
        const float backWallHalfDepth = 0.125f;
        const float sideRunZBack = -10.15f;
        float sideRunZEnd = backWallZ + backWallHalfDepth;
        float sideRunLen = sideRunZEnd - sideRunZBack;
        float sideRunCenterZ = (sideRunZBack + sideRunZEnd) * 0.5f;

        // Thinner but taller sides to feel like a cardboard box tray.
        GameObject railLeft = CreateDecorBlock("RailLeft", new Vector3(-5.01f, 0.3f, sideRunCenterZ), new Vector3(0.08f, 0.36f, sideRunLen), new Color(0.24f, 0.2f, 0.15f));
        GameObject railRight = CreateDecorBlock("RailRight", new Vector3(5.01f, 0.3f, sideRunCenterZ), new Vector3(0.08f, 0.36f, sideRunLen), new Color(0.24f, 0.2f, 0.15f));
        ApplySharedNormalMap(railLeft.GetComponent<Renderer>().sharedMaterial, 0.35f);
        ApplySharedNormalMap(railRight.GetComponent<Renderer>().sharedMaterial, 0.35f);
        // Keep side geometry clean for stylized cardboard look.

        GameObject backWall = CreateDecorBlock("BackWall", new Vector3(0f, 2.7f, backWallZ), new Vector3(10.2f, 5.4f, 0.1f), new Color(0.24f, 0.2f, 0.15f));
        GameObject leftWall = CreateDecorBlock("LeftWall", new Vector3(-5.12f, 2.7f, sideRunCenterZ), new Vector3(0.07f, 5.4f, sideRunLen), new Color(0.23f, 0.19f, 0.14f));
        GameObject rightWall = CreateDecorBlock("RightWall", new Vector3(5.12f, 2.7f, sideRunCenterZ), new Vector3(0.07f, 5.4f, sideRunLen), new Color(0.23f, 0.19f, 0.14f));
        MakeMaterialMatte(backWall.GetComponent<Renderer>()?.sharedMaterial, 0.08f, 0f);
        MakeMaterialMatte(leftWall.GetComponent<Renderer>()?.sharedMaterial, 0.08f, 0f);
        MakeMaterialMatte(rightWall.GetComponent<Renderer>()?.sharedMaterial, 0.08f, 0f);
        CreateDecorBlock("TopEdge_Back", new Vector3(0f, 5.43f, backWallZ - 0.02f), new Vector3(10.2f, 0.04f, 0.08f), new Color(0.66f, 0.56f, 0.43f));
        CreateDecorBlock("TopEdge_Left", new Vector3(-5.12f, 5.43f, sideRunCenterZ), new Vector3(0.08f, 0.04f, sideRunLen), new Color(0.66f, 0.56f, 0.43f));
        CreateDecorBlock("TopEdge_Right", new Vector3(5.12f, 5.43f, sideRunCenterZ), new Vector3(0.08f, 0.04f, sideRunLen), new Color(0.66f, 0.56f, 0.43f));

        CreatePointLight("FillLight_Left", new Vector3(-2.8f, 2.75f, 6.4f), new Color(1f, 0.62f, 0.56f), 1.25f, 8.5f);
        CreatePointLight("FillLight_Mid", new Vector3(0f, 2.85f, 6.5f), new Color(0.68f, 0.75f, 0.98f), 1.12f, 8.5f);
        CreatePointLight("FillLight_Right", new Vector3(2.8f, 2.75f, 6.4f), new Color(0.62f, 0.94f, 0.65f), 1.25f, 8.5f);
        CreatePointLight("TopFill", new Vector3(0f, 2.62f, 5.2f), new Color(1f, 0.95f, 0.86f), 1.02f, 9f);
        CreateConveyorAccent("ApproachAccent", new Vector3(0f, 0.305f, -4.97f), new Vector3(9.85f, 0.02f, 10.06f), new Color(0.41f, 0.36f, 0.3f));
    }

    private static List<CargoBox> CreateCargoPrefabs()
    {
        List<CargoBox> prefabs = new List<CargoBox>();
        prefabs.Add(CreateCargoPrefab("CargoBox_Red", CargoType.Red));
        prefabs.Add(CreateCargoPrefab("CargoBox_Blue", CargoType.Blue));
        prefabs.Add(CreateCargoPrefab("CargoBox_Green", CargoType.Green));
        return prefabs;
    }

    private static CargoBox CreateCargoPrefab(string prefabName, CargoType type)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = prefabName;
        go.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.mass = 1.2f;
        rb.angularDamping = 1f;
        rb.linearDamping = 0.1f;

        CargoBox cargo = go.AddComponent<CargoBox>();
        SerializedObject so = new SerializedObject(cargo);
        so.FindProperty("cargoType").enumValueIndex = (int)type;
        so.FindProperty("scoreValue").intValue = 10;
        so.FindProperty("colorRenderer").objectReferenceValue = go.GetComponent<Renderer>();
        so.ApplyModifiedPropertiesWithoutUndo();

        Material cargoMat = CreateMaterialAsset($"CargoPlastic_{prefabName}_Mat", new Color(0.72f, 0.61f, 0.45f));
        cargoMat.SetFloat("_Smoothness", 0.06f);
        cargoMat.SetFloat("_Metallic", 0f);
        ApplySharedNormalMap(cargoMat, 0.1f);
        go.GetComponent<Renderer>().sharedMaterial = cargoMat;

        string prefabPath = $"Assets/Prefabs/Cargo/{prefabName}.prefab";
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);
        return saved.GetComponent<CargoBox>();
    }

    private static CargoSpawner CreateSpawner(List<CargoBox> prefabs)
    {
        GameObject spawnerGO = new GameObject("CargoSpawner");
        spawnerGO.transform.position = new Vector3(0f, 0.64f, -9f);
        CargoSpawner spawner = spawnerGO.AddComponent<CargoSpawner>();
        return spawner;
    }

    private static void BuildConveyorLayout(Transform root, out DiverterSwitch diverter, out LaneRouter router)
    {
        // Серая подложка заходит на цветные ленты по Z; чуть больше перекрытие + визуальный мостик.
        const float seamOverlap = 0.03f;
        const float approachLen = 10f + seamOverlap;
        const float approachZBack = -10f;
        const float approachZCenter = approachZBack + approachLen * 0.5f;
        const float approachWidthTotal = 10f;
        Color approachTint = new Color(0.35f, 0.35f, 0.35f);
        Material approachMat = CreateMaterialAsset("Approach_Mat", approachTint);
        ApplyConveyorSurfaceFinish(approachMat);
        CreateConveyorSegment(
            root,
            "Approach_Slab",
            new Vector3(0f, 0.15f, approachZCenter),
            new Vector3(approachWidthTotal, 0.3f, approachLen),
            Vector3.forward,
            approachMat);

        CreateApproachLaneTintBands(root, approachZCenter, approachLen);
        CreateApproachLaneDividers(root, approachZCenter, approachLen);
        CreateApproachEntranceLaneHints(root);

        const float branchZEnd = 8.2f;
        const float branchZStart = -seamOverlap;
        const float branchLen = branchZEnd - branchZStart;
        const float branchZCenter = (branchZStart + branchZEnd) * 0.5f;
        const float branchWidth = 3.52f;
        CreateConveyorSegment(root, "BranchLeft", new Vector3(-3.5f, 0.15f, branchZCenter), new Vector3(branchWidth, 0.3f, branchLen), Vector3.forward, new Color(0.55f, 0.24f, 0.24f));
        CreateConveyorSegment(root, "BranchMid", new Vector3(0f, 0.15f, branchZCenter), new Vector3(branchWidth, 0.3f, branchLen), Vector3.forward, new Color(0.25f, 0.44f, 0.7f));
        CreateConveyorSegment(root, "BranchRight", new Vector3(3.5f, 0.15f, branchZCenter), new Vector3(branchWidth, 0.3f, branchLen), Vector3.forward, new Color(0.24f, 0.58f, 0.33f));
        CreateBranchLaneDividers(root, branchZCenter, branchLen);

        GameObject diverterGO = new GameObject("Diverter_1");
        diverterGO.transform.parent = root;
        diverterGO.transform.position = new Vector3(0f, 0.2f, 0.5f);
        diverter = diverterGO.AddComponent<DiverterSwitch>();

        SerializedObject so = new SerializedObject(diverter);
        so.FindProperty("lane1Key").intValue = (int)KeyCode.Alpha1;
        so.FindProperty("lane2Key").intValue = (int)KeyCode.Alpha2;
        so.FindProperty("lane3Key").intValue = (int)KeyCode.Alpha3;
        so.FindProperty("previousLaneKey").intValue = (int)KeyCode.LeftArrow;
        so.FindProperty("nextLaneKey").intValue = (int)KeyCode.RightArrow;
        so.FindProperty("startLaneIndex").intValue = 1;

        SerializedProperty laneBlockers = so.FindProperty("laneBlockers");
        laneBlockers.arraySize = 0;
        so.ApplyModifiedPropertiesWithoutUndo();

        CreateLaneIndicators(root, diverter);

        GameObject routerGO = new GameObject("LaneRouter");
        routerGO.transform.parent = root;
        routerGO.transform.position = new Vector3(0f, 0.4f, 1.5f);
        BoxCollider routerTrigger = routerGO.AddComponent<BoxCollider>();
        routerTrigger.isTrigger = true;
        routerTrigger.size = new Vector3(7.2f, 1.6f, 0.6f);
        router = routerGO.AddComponent<LaneRouter>();
    }

    private static void CreateApproachLaneDividers(Transform parent, float zCenter, float zLength)
    {
        const float yOnSlab = 0.312f;
        Material grooveMat = CreateMaterialAsset("ApproachGroove_Mat", new Color(0.72f, 0.62f, 0.49f));
        grooveMat.SetFloat("_Smoothness", 0.18f);
        grooveMat.SetFloat("_Metallic", 0.02f);
        foreach (float x in new[] { -1.75f, 1.75f })
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = x < 0f ? "ApproachDivider_L" : "ApproachDivider_R";
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(x, yOnSlab, zCenter);
            go.transform.localScale = new Vector3(0.11f, 0.028f, zLength - 0.06f);
            go.GetComponent<Renderer>().sharedMaterial = grooveMat;
            Object.DestroyImmediate(go.GetComponent<Collider>());
        }
    }

    private static void CreateApproachEntranceLaneHints(Transform parent)
    {
        float zEntrance = -9.55f;
        float y = 0.34f;
        (float x, Color c, string id)[] hints =
        {
            (-3.5f, new Color(0.75f, 0.22f, 0.22f), "Hint_L"),
            (0f, new Color(0.22f, 0.42f, 0.85f), "Hint_M"),
            (3.5f, new Color(0.22f, 0.72f, 0.34f), "Hint_R")
        };

        for (int i = 0; i < hints.Length; i++)
        {
            GameObject puck = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            puck.name = $"EntranceLaneHint_{hints[i].id}";
            puck.transform.SetParent(parent);
            puck.transform.position = new Vector3(hints[i].x, y, zEntrance);
            puck.transform.localScale = new Vector3(0.26f, 0.035f, 0.26f);
            Material m = CreateMaterialAsset($"EntranceHint_{hints[i].id}_Mat", hints[i].c);
            m.SetFloat("_Smoothness", 0.55f);
            m.SetFloat("_Metallic", 0.18f);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", hints[i].c * 0.18f);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            puck.GetComponent<Renderer>().sharedMaterial = m;
            Object.DestroyImmediate(puck.GetComponent<Collider>());
        }
    }

    private static void CreateApproachLaneTintBands(Transform parent, float zCenter, float zLength)
    {
        const float yOnSlab = 0.305f;
        float zVis = zLength - 0.2f;
        (float x, Color tint, string id)[] bands =
        {
            (-3.5f, new Color(0.48f, 0.36f, 0.36f), "L"),
            (0f, new Color(0.34f, 0.4f, 0.52f), "M"),
            (3.5f, new Color(0.36f, 0.5f, 0.4f), "R")
        };

        for (int i = 0; i < bands.Length; i++)
        {
            Material m = CreateMaterialAsset($"ApproachLaneTint_{bands[i].id}_Mat", bands[i].tint);
            m.SetFloat("_Smoothness", 0.38f);
            m.SetFloat("_Metallic", 0.05f);
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"ApproachLaneTint_{bands[i].id}";
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(bands[i].x, yOnSlab, zCenter);
            go.transform.localScale = new Vector3(2.78f, 0.012f, zVis);
            go.GetComponent<Renderer>().sharedMaterial = m;
            Object.DestroyImmediate(go.GetComponent<Collider>());
        }
    }

    private static void CreateBranchLaneDividers(Transform parent, float branchZCenter, float branchLen)
    {
        Material divMat = CreateMaterialAsset("BranchLaneDivider_Mat", new Color(0.78f, 0.69f, 0.55f));
        divMat.SetFloat("_Metallic", 0f);
        divMat.SetFloat("_Smoothness", 0.14f);
        float zVis = branchLen - 0.4f;
        foreach (float x in new[] { -1.75f, 1.75f })
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = x < 0f ? "BranchDivider_L" : "BranchDivider_R";
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(x, 0.305f, branchZCenter);
            go.transform.localScale = new Vector3(0.086f, 0.03f, zVis);
            go.GetComponent<Renderer>().sharedMaterial = divMat;
            Object.DestroyImmediate(go.GetComponent<Collider>());
        }
    }

    private static void CreateSortingTransitionStrip(Transform parent, float seamOverlap)
    {
        float zBridge = seamOverlap * 0.45f;

        Material fillMat = CreateMaterialAsset("SortingTransitionFill_Mat", new Color(0.1f, 0.11f, 0.14f));
        fillMat.SetFloat("_Metallic", 0.22f);
        fillMat.SetFloat("_Smoothness", 0.4f);
        GameObject fill = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fill.name = "SortingTransitionFill";
        fill.transform.SetParent(parent);
        fill.transform.position = new Vector3(0f, 0.275f, zBridge);
        fill.transform.localScale = new Vector3(10.25f, 0.05f, 0.32f);
        fill.GetComponent<Renderer>().sharedMaterial = fillMat;
        Object.DestroyImmediate(fill.GetComponent<Collider>());

        Material beamMat = CreateMaterialAsset("SortingTransitionBeam_Mat", new Color(0.42f, 0.43f, 0.48f));
        beamMat.SetFloat("_Metallic", 0.82f);
        beamMat.SetFloat("_Smoothness", 0.72f);
        GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
        beam.name = "SortingTransitionBeam";
        beam.transform.SetParent(parent);
        beam.transform.position = new Vector3(0f, 0.336f, zBridge);
        beam.transform.localScale = new Vector3(10.12f, 0.075f, 0.26f);
        beam.GetComponent<Renderer>().sharedMaterial = beamMat;
        Object.DestroyImmediate(beam.GetComponent<Collider>());

        Material warnMat = CreateMaterialAsset("SortingTransitionWarn_Mat", new Color(0.95f, 0.62f, 0.12f));
        warnMat.SetFloat("_Metallic", 0.15f);
        warnMat.SetFloat("_Smoothness", 0.45f);
        warnMat.EnableKeyword("_EMISSION");
        warnMat.SetColor("_EmissionColor", new Color(0.95f, 0.55f, 0.08f) * 0.45f);
        warnMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        GameObject warn = GameObject.CreatePrimitive(PrimitiveType.Cube);
        warn.name = "SortingTransitionWarnStripe";
        warn.transform.SetParent(parent);
        warn.transform.position = new Vector3(0f, 0.352f, zBridge + 0.06f);
        warn.transform.localScale = new Vector3(9.95f, 0.022f, 0.07f);
        warn.GetComponent<Renderer>().sharedMaterial = warnMat;
        Object.DestroyImmediate(warn.GetComponent<Collider>());

        Material postMat = CreateMaterialAsset("TransitionPost_Mat", new Color(0.18f, 0.19f, 0.22f));
        postMat.SetFloat("_Metallic", 0.55f);
        postMat.SetFloat("_Smoothness", 0.52f);
        foreach (float x in new[] { -5.05f, 5.05f })
        {
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post.name = x < 0f ? "TransitionPost_L" : "TransitionPost_R";
            post.transform.SetParent(parent);
            post.transform.position = new Vector3(x, 0.382f, zBridge);
            post.transform.localScale = new Vector3(0.14f, 0.14f, 0.24f);
            post.GetComponent<Renderer>().sharedMaterial = postMat;
            Object.DestroyImmediate(post.GetComponent<Collider>());
        }

        Material rivetMat = CreateMaterialAsset("TransitionRivet_Mat", new Color(0.48f, 0.49f, 0.52f));
        rivetMat.SetFloat("_Metallic", 0.88f);
        rivetMat.SetFloat("_Smoothness", 0.72f);
        for (int i = 0; i < 15; i++)
        {
            float t = i / 14f;
            float x = Mathf.Lerp(-4.75f, 4.75f, t);
            GameObject rivet = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rivet.name = $"TransitionRivet_{i}";
            rivet.transform.SetParent(parent);
            rivet.transform.position = new Vector3(x, 0.378f, zBridge - 0.02f);
            rivet.transform.localScale = new Vector3(0.1f, 0.02f, 0.1f);
            rivet.GetComponent<Renderer>().sharedMaterial = rivetMat;
            Object.DestroyImmediate(rivet.GetComponent<Collider>());
        }
    }

    private static void CreateConveyorSegment(Transform parent, string name, Vector3 position, Vector3 scale, Vector3 direction, Color color)
    {
        Material beltMat = CreateMaterialAsset($"{name}_Mat", color);
        ApplyConveyorSurfaceFinish(beltMat);
        CreateConveyorSegment(parent, name, position, scale, direction, beltMat);
    }

    private static void CreateConveyorSegment(Transform parent, string name, Vector3 position, Vector3 scale, Vector3 direction, Material sharedMaterial)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.parent = parent;
        go.transform.position = position;
        go.transform.localScale = scale;
        go.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        go.GetComponent<Renderer>().sharedMaterial = sharedMaterial;

        Collider col = go.GetComponent<Collider>();
        col.isTrigger = true;
        if (col is BoxCollider box)
        {
            box.size = new Vector3(1f, 2.2f, 1f);
            box.center = new Vector3(0f, 0.45f, 0f);
        }

        ConveyorSegment segment = go.AddComponent<ConveyorSegment>();
        SerializedObject so = new SerializedObject(segment);
        so.FindProperty("directionReference").objectReferenceValue = null;
        so.FindProperty("baseSpeed").floatValue = 1.25f;
        so.FindProperty("lateralDamping").floatValue = 0.9f;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateLaneIndicators(Transform parent, DiverterSwitch diverter)
    {
        GameObject root = new GameObject("LaneIndicators");
        root.transform.SetParent(parent);
        root.transform.localPosition = Vector3.zero;

        Material discMat = CreateMaterialAsset("LaneIndicator_Disc_Mat", new Color(0.53f, 0.46f, 0.37f));
        discMat.SetFloat("_Smoothness", 0.58f);
        discMat.EnableKeyword("_EMISSION");
        discMat.SetColor("_EmissionColor", Color.black);
        discMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

        Material haloMat = CreateMaterialAsset("LaneIndicator_Halo_Mat", new Color(0.64f, 0.56f, 0.44f));
        haloMat.SetFloat("_Smoothness", 0.42f);
        haloMat.EnableKeyword("_EMISSION");
        haloMat.SetColor("_EmissionColor", Color.black);
        haloMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

        Transform[] laneRoots = new Transform[3];
        Renderer[] discs = new Renderer[3];
        Renderer[] halos = new Renderer[3];
        float[] laneX = { -3.5f, 0f, 3.5f };
        for (int i = 0; i < 3; i++)
        {
            GameObject laneRoot = new GameObject($"LaneMarker_{i + 1}");
            laneRoot.transform.SetParent(root.transform);
            laneRoot.transform.position = new Vector3(laneX[i], 0.95f, 1.5f);
            laneRoots[i] = laneRoot.transform;

            GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "Disc";
            disc.transform.SetParent(laneRoot.transform);
            disc.transform.localPosition = Vector3.zero;
            disc.transform.localRotation = Quaternion.identity;
            disc.transform.localScale = new Vector3(0.35f, 0.03f, 0.35f);
            Renderer discR = disc.GetComponent<Renderer>();
            discR.sharedMaterial = discMat;
            Object.DestroyImmediate(disc.GetComponent<Collider>());
            discs[i] = discR;

            GameObject halo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            halo.name = "Halo";
            halo.transform.SetParent(laneRoot.transform);
            halo.transform.localPosition = new Vector3(0f, -0.012f, 0f);
            halo.transform.localRotation = Quaternion.identity;
            halo.transform.localScale = new Vector3(0.52f, 0.012f, 0.52f);
            Renderer haloR = halo.GetComponent<Renderer>();
            haloR.sharedMaterial = haloMat;
            Object.DestroyImmediate(halo.GetComponent<Collider>());
            halos[i] = haloR;
        }

        LaneVisualIndicator indicator = root.AddComponent<LaneVisualIndicator>();
        SerializedObject so = new SerializedObject(indicator);
        so.FindProperty("diverter").objectReferenceValue = diverter;
        WriteTransformArray(so, "laneRoots", laneRoots);
        WriteRendererArray(so, "laneDiscs", discs);
        WriteRendererArray(so, "laneHalos", halos);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BuildReceivers(out ReceiverZone red, out ReceiverZone blue, out ReceiverZone green)
    {
        // Центр цеха сдвинут вперёд (меньше Z): иначе подставка уходит за край цветной ленты (z=8.2).
        const float receiverZ = 7.05f;
        red = CreateReceiver("Receiver_Red", new Vector3(-3.5f, 0.6f, receiverZ), new Color(0.93f, 0.28f, 0.28f));
        blue = CreateReceiver("Receiver_Blue", new Vector3(0f, 0.6f, receiverZ), new Color(0.26f, 0.52f, 0.93f));
        green = CreateReceiver("Receiver_Green", new Vector3(3.5f, 0.6f, receiverZ), new Color(0.28f, 0.78f, 0.4f));
    }

    private static ReceiverZone CreateReceiver(string name, Vector3 position, Color visualColor)
    {
        GameObject receiver = new GameObject(name);
        receiver.name = name;
        receiver.transform.position = position;
        BuildReceiverDockVisual(receiver.transform, visualColor, name);

        BoxCollider trigger = receiver.AddComponent<BoxCollider>();
        trigger.size = new Vector3(1.9f, 2f, 1.9f);
        trigger.isTrigger = true;

        ReceiverZone zone = receiver.AddComponent<ReceiverZone>();
        ReceiverFeedback feedback = receiver.AddComponent<ReceiverFeedback>();

        // simple one-shot effects
        ParticleSystem success = CreateReceiverParticle("SuccessEffect", receiver.transform, visualColor, 24, 0.56f, 0.34f);
        ParticleSystem fail = CreateReceiverParticle("FailEffect", receiver.transform, new Color(1f, 0.92f, 0.22f), 18, 0.48f, 0.3f);
        Renderer beaconRenderer = receiver.transform.Find("Beacon")?.GetComponent<Renderer>();

        SerializedObject feedbackSO = new SerializedObject(feedback);
        feedbackSO.FindProperty("successEffect").objectReferenceValue = success;
        feedbackSO.FindProperty("failEffect").objectReferenceValue = fail;
        feedbackSO.FindProperty("pulseRenderer").objectReferenceValue = beaconRenderer;
        feedbackSO.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject zoneSO = new SerializedObject(zone);
        zoneSO.FindProperty("feedback").objectReferenceValue = feedback;
        zoneSO.ApplyModifiedPropertiesWithoutUndo();
        return zone;
    }

    private static void BuildReceiverDockVisual(Transform parent, Color color, string receiverId)
    {
        GameObject basePad = GameObject.CreatePrimitive(PrimitiveType.Cube);
        basePad.name = "BasePad";
        basePad.transform.SetParent(parent);
        basePad.transform.localPosition = new Vector3(0f, -0.12f, 0f);
        basePad.transform.localScale = new Vector3(2.1f, 0.22f, 2.1f);
        basePad.GetComponent<Renderer>().sharedMaterial = CreateMaterialAsset("ReceiverBase_Mat", new Color(0.56f, 0.48f, 0.37f));
        RemoveCollider(basePad);

        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "ColorRing";
        ring.transform.SetParent(parent);
        ring.transform.localPosition = new Vector3(0f, 0.06f, 0f);
        ring.transform.localScale = new Vector3(0.9f, 0.06f, 0.9f);
        ring.GetComponent<Renderer>().sharedMaterial = CreateMaterialAsset($"{receiverId}_ReceiverRing_Mat", color * 0.9f);
        RemoveCollider(ring);

        GameObject intake = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        intake.name = "Intake";
        intake.transform.SetParent(parent);
        intake.transform.localPosition = new Vector3(0f, 0.07f, 0f);
        intake.transform.localScale = new Vector3(0.55f, 0.1f, 0.55f);
        intake.GetComponent<Renderer>().sharedMaterial = CreateMaterialAsset($"{receiverId}_ReceiverIntake_Mat", new Color(0.35f, 0.29f, 0.22f));
        RemoveCollider(intake);

        GameObject beacon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        beacon.name = "Beacon";
        beacon.transform.SetParent(parent);
        beacon.transform.localPosition = new Vector3(0f, 0.54f, 0f);
        beacon.transform.localScale = new Vector3(0.5f, 0.4f, 0.5f);
        Material beaconMat = CreateMaterialAsset($"{receiverId}_ReceiverBeacon_Mat", color);
        EnableBeaconEmission(beaconMat, color, 0.95f);
        beacon.GetComponent<Renderer>().sharedMaterial = beaconMat;
        RemoveCollider(beacon);
        beacon.AddComponent<ReceiverBeaconFloat>();
    }

    private static ParticleSystem CreateReceiverParticle(string name, Transform parent, Color tint, short burstCount, float lifeTime, float radius)
    {
        GameObject fx = new GameObject(name);
        fx.transform.SetParent(parent);
        fx.transform.localPosition = Vector3.up * 0.7f;
        ParticleSystem ps = fx.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.38f;
        main.startLifetime = lifeTime;
        main.startSpeed = 1.9f;
        main.startSize = 0.125f;
        Color bright = tint * 1.85f;
        bright.a = tint.a;
        main.startColor = bright;
        main.gravityModifier = 0f;
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, burstCount) });
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = radius;
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.12f;
        AssignUrpParticleMaterial(ps);
        return ps;
    }

    private static void EnableBeaconEmission(Material mat, Color color, float emissionScale)
    {
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", color * emissionScale);
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
    }

    private static void ApplyConveyorSurfaceFinish(Material mat)
    {
        mat.SetFloat("_Smoothness", 0.5f);
        mat.SetFloat("_Metallic", 0.11f);
        EditorUtility.SetDirty(mat);
    }

    private static void AssignUrpParticleMaterial(ParticleSystem ps)
    {
        ParticleSystemRenderer psr = ps.GetComponent<ParticleSystemRenderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Particles/Simple Lit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Particles/Standard Unlit");
        }

        if (shader == null)
        {
            return;
        }

        Material mat = new Material(shader);
        if (mat.HasProperty("_BaseColor"))
        {
            mat.SetColor("_BaseColor", Color.white);
        }

        if (mat.HasProperty("_Color"))
        {
            mat.SetColor("_Color", Color.white);
        }

        psr.material = mat;
    }

    private static void CreateOutOfBoundsZone()
    {
        GameObject failZone = new GameObject("OutOfBoundsZone");
        failZone.transform.position = new Vector3(0f, 0.8f, 12f);
        BoxCollider col = failZone.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(24f, 4f, 4f);
        failZone.AddComponent<ConveyorOutOfBoundsZone>();
    }

    private static HUDController CreateHud()
    {
        GameObject canvasGO = new GameObject("HUDCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        HUDController hud = canvasGO.AddComponent<HUDController>();

        CreateHudPanel(canvasGO.transform, "LeftHudPanel", new Vector2(10f, -10f), new Vector2(170f, 106f), false);
        RectTransform rightHudPanel = CreateHudPanel(canvasGO.transform, "RightHudPanel", new Vector2(-10f, -10f), new Vector2(448f, 124f), true);

        TMP_Text score = CreateText(canvasGO.transform, "ScoreText", new Vector2(14f, -12f), new Vector2(152f, 36f), TextAlignmentOptions.TopLeft);
        TMP_Text defects = CreateText(canvasGO.transform, "DefectsText", new Vector2(14f, -46f), new Vector2(152f, 32f), TextAlignmentOptions.TopLeft);
        TMP_Text streak = CreateText(canvasGO.transform, "StreakText", new Vector2(14f, -78f), new Vector2(148f, 26f), TextAlignmentOptions.TopLeft);
        streak.fontSize = 19;
        TMP_Text timer = CreateText(canvasGO.transform, "TimerText", new Vector2(0f, -16f), new Vector2(280f, 40f), TextAlignmentOptions.Top, false, true);
        TMP_Text waveIndex = CreateText(canvasGO.transform, "WaveIndexText", new Vector2(0f, -52f), new Vector2(260f, 32f), TextAlignmentOptions.Top, false, true);
        waveIndex.fontSize = 22;
        waveIndex.gameObject.SetActive(false);
        TMP_Text state = CreateText(canvasGO.transform, "GameStateText", new Vector2(0f, 0f), new Vector2(780f, 80f), TextAlignmentOptions.Center, false, true);
        state.fontSize = 42;
        RectTransform stateRect = state.GetComponent<RectTransform>();
        stateRect.anchorMin = new Vector2(0.5f, 0.5f);
        stateRect.anchorMax = new Vector2(0.5f, 0.5f);
        stateRect.pivot = new Vector2(0.5f, 0.5f);
        stateRect.anchoredPosition = Vector2.zero;

        GameObject resultOverlayGO = new GameObject("ResultOverlay");
        resultOverlayGO.transform.SetParent(canvasGO.transform, false);
        CanvasGroup resultCanvasGroup = resultOverlayGO.AddComponent<CanvasGroup>();
        resultCanvasGroup.alpha = 0f;
        resultCanvasGroup.blocksRaycasts = false;
        resultCanvasGroup.interactable = false;
        Image resultOverlay = resultOverlayGO.AddComponent<Image>();
        resultOverlay.color = new Color(0.05f, 0.06f, 0.08f, 0.7f);
        RectTransform overlayRect = resultOverlayGO.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        resultOverlayGO.SetActive(false);

        GameObject resultPanelGO = new GameObject("ResultPanel");
        resultPanelGO.transform.SetParent(resultOverlayGO.transform, false);
        Image resultPanel = resultPanelGO.AddComponent<Image>();
        resultPanel.color = new Color(0.09f, 0.11f, 0.15f, 0.96f);
        RectTransform resultPanelRect = resultPanelGO.GetComponent<RectTransform>();
        resultPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
        resultPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
        resultPanelRect.pivot = new Vector2(0.5f, 0.5f);
        resultPanelRect.sizeDelta = new Vector2(708f, 228f);
        resultPanelRect.anchoredPosition = Vector2.zero;

        TMP_Text resultTitle = CreateText(resultPanelGO.transform, "ResultTitleText", new Vector2(0f, 28f), new Vector2(640f, 84f), TextAlignmentOptions.Center, false, true);
        resultTitle.fontSize = 54;
        RectTransform titleRect = resultTitle.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0f, 28f);

        TMP_Text resultHint = CreateText(resultPanelGO.transform, "ResultHintText", new Vector2(0f, -32f), new Vector2(640f, 108f), TextAlignmentOptions.Center, false, true);
        resultHint.fontSize = 24;
        RectTransform hintRect = resultHint.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0.5f, 0.5f);
        hintRect.anchorMax = new Vector2(0.5f, 0.5f);
        hintRect.pivot = new Vector2(0.5f, 0.5f);
        hintRect.anchoredPosition = new Vector2(0f, -32f);

        GameObject introOverlayGO = new GameObject("IntroOverlay");
        introOverlayGO.transform.SetParent(canvasGO.transform, false);
        Image introOverlay = introOverlayGO.AddComponent<Image>();
        introOverlay.color = new Color(0.04f, 0.05f, 0.07f, 0.68f);
        RectTransform introRect = introOverlayGO.GetComponent<RectTransform>();
        introRect.anchorMin = Vector2.zero;
        introRect.anchorMax = Vector2.one;
        introRect.offsetMin = Vector2.zero;
        introRect.offsetMax = Vector2.zero;

        TMP_Text introText = CreateText(introOverlayGO.transform, "IntroText", new Vector2(0f, 0f), new Vector2(760f, 140f), TextAlignmentOptions.Center, false, true);
        introText.fontSize = 84;
        RectTransform introTextRect = introText.GetComponent<RectTransform>();
        introTextRect.anchorMin = new Vector2(0.5f, 0.5f);
        introTextRect.anchorMax = new Vector2(0.5f, 0.5f);
        introTextRect.pivot = new Vector2(0.5f, 0.5f);
        introTextRect.anchoredPosition = Vector2.zero;
        TMP_Text best = CreateText(rightHudPanel, "BestScoreText", new Vector2(-16f, -14f), new Vector2(348f, 40f), TextAlignmentOptions.TopRight, true);
        TMP_Text hint = CreateText(rightHudPanel, "CooldownHintText", new Vector2(-16f, -52f), new Vector2(368f, 44f), TextAlignmentOptions.TopRight, true);
        hint.textWrappingMode = TextWrappingModes.NoWrap;
        hint.overflowMode = TextOverflowModes.Overflow;

        GameObject controlsPanelGO = new GameObject("ControlsHintPanel");
        controlsPanelGO.transform.SetParent(canvasGO.transform, false);
        Image controlsPanelImg = controlsPanelGO.AddComponent<Image>();
        controlsPanelImg.color = new Color(0.07f, 0.08f, 0.1f, 0.58f);
        RectTransform controlsPanelRect = controlsPanelGO.GetComponent<RectTransform>();
        controlsPanelRect.anchorMin = new Vector2(1f, 0f);
        controlsPanelRect.anchorMax = new Vector2(1f, 0f);
        controlsPanelRect.pivot = new Vector2(1f, 0f);
        controlsPanelRect.anchoredPosition = new Vector2(-10f, 10f);
        controlsPanelRect.sizeDelta = new Vector2(612f, 128f);

        TMP_Text controls = CreateText(controlsPanelGO.transform, "ControlsText", new Vector2(-12f, 20f), new Vector2(508f, 108f), TextAlignmentOptions.BottomRight, true, false, true);
        controls.fontSize = 25;
        controls.lineSpacing = -2f;
        controls.textWrappingMode = TextWrappingModes.NoWrap;
        controls.overflowMode = TextOverflowModes.Overflow;
        controls.text = "У развилки конвейера выберите номер линии:\n1 (слева), 2 (центр), 3 (справа). Допустимы стрелки.\nПробел: кратковременное замедление ленты.";

        GameObject sliderGO = new GameObject("DefectsSlider");
        sliderGO.transform.SetParent(canvasGO.transform, false);
        Slider slider = sliderGO.AddComponent<Slider>();
        RectTransform sliderRect = sliderGO.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0f, 1f);
        sliderRect.anchorMax = new Vector2(0f, 1f);
        sliderRect.pivot = new Vector2(0f, 1f);
        sliderRect.anchoredPosition = new Vector2(14f, -112f);
        sliderRect.sizeDelta = new Vector2(210f, 16f);

        GameObject trackGO = new GameObject("CooldownTrack");
        trackGO.transform.SetParent(rightHudPanel, false);
        Image trackImg = trackGO.AddComponent<Image>();
        trackImg.color = new Color(0.05f, 0.06f, 0.08f, 0.72f);
        RectTransform trackRect = trackGO.GetComponent<RectTransform>();
        trackRect.anchorMin = new Vector2(0f, 0f);
        trackRect.anchorMax = new Vector2(1f, 0f);
        trackRect.pivot = new Vector2(0.5f, 0f);
        trackRect.anchoredPosition = new Vector2(0f, 10f);
        trackRect.sizeDelta = new Vector2(-28f, 14f);

        GameObject fillGO = new GameObject("CooldownFill");
        fillGO.transform.SetParent(trackGO.transform, false);
        Image cooldownFill = fillGO.AddComponent<Image>();
        cooldownFill.type = Image.Type.Filled;
        cooldownFill.fillMethod = Image.FillMethod.Horizontal;
        cooldownFill.fillOrigin = 0; // Left (horizontal fill)
        cooldownFill.fillAmount = 0f;
        cooldownFill.color = new Color(0.25f, 0.9f, 1f, 0.85f);
        RectTransform fillRect = fillGO.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.sizeDelta = new Vector2(0f, 0f);

        SerializedObject hudSO = new SerializedObject(hud);
        hudSO.FindProperty("scoreText").objectReferenceValue = score;
        hudSO.FindProperty("bestScoreText").objectReferenceValue = best;
        hudSO.FindProperty("defectsText").objectReferenceValue = defects;
        hudSO.FindProperty("streakText").objectReferenceValue = streak;
        hudSO.FindProperty("timerText").objectReferenceValue = timer;
        hudSO.FindProperty("waveIndexText").objectReferenceValue = waveIndex;
        hudSO.FindProperty("gameStateText").objectReferenceValue = state;
        hudSO.FindProperty("cooldownHintText").objectReferenceValue = hint;
        hudSO.FindProperty("resultOverlay").objectReferenceValue = resultOverlay;
        hudSO.FindProperty("resultCanvasGroup").objectReferenceValue = resultCanvasGroup;
        hudSO.FindProperty("resultFadeSeconds").floatValue = 0.38f;
        hudSO.FindProperty("resultPanel").objectReferenceValue = resultPanel;
        hudSO.FindProperty("resultTitleText").objectReferenceValue = resultTitle;
        hudSO.FindProperty("resultHintText").objectReferenceValue = resultHint;
        hudSO.FindProperty("introOverlay").objectReferenceValue = introOverlay;
        hudSO.FindProperty("introText").objectReferenceValue = introText;
        hudSO.FindProperty("defectsSlider").objectReferenceValue = slider;
        hudSO.FindProperty("cooldownFill").objectReferenceValue = cooldownFill;
        hudSO.FindProperty("cooldownFillRect").objectReferenceValue = fillRect;
        hudSO.FindProperty("cooldownBarWidth").floatValue = 418f;
        hudSO.ApplyModifiedPropertiesWithoutUndo();

        return hud;
    }

    private static RectTransform CreateHudPanel(Transform parent, string name, Vector2 anchoredPos, Vector2 size, bool rightSide)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = new Color(0.07f, 0.08f, 0.1f, 0.46f);
        RectTransform r = go.GetComponent<RectTransform>();
        if (rightSide)
        {
            r.anchorMin = new Vector2(1f, 1f);
            r.anchorMax = new Vector2(1f, 1f);
            r.pivot = new Vector2(1f, 1f);
        }
        else
        {
            r.anchorMin = new Vector2(0f, 1f);
            r.anchorMax = new Vector2(0f, 1f);
            r.pivot = new Vector2(0f, 1f);
        }
        r.anchoredPosition = anchoredPos;
        r.sizeDelta = size;
        return r;
    }

    private static TMP_Text CreateText(Transform parent, string name, Vector2 anchoredPos, Vector2 size, TextAlignmentOptions align, bool right = false, bool centered = false, bool bottom = false)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.fontSize = 27;
        text.color = Color.white;
        text.alignment = align;
        text.text = name;

        RectTransform rect = go.GetComponent<RectTransform>();
        if (centered)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
        }
        else if (right && bottom)
        {
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
        }
        else if (right)
        {
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
        }
        else
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
        }
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;
        return text;
    }

    private static GameObject CreateDecorBlock(string name, Vector3 pos, Vector3 scale, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.position = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = CreateMaterialAsset($"{name}_Mat", color);
        return go;
    }

    private static void WriteTransformArray(SerializedObject so, string propName, Transform[] values)
    {
        SerializedProperty arr = so.FindProperty(propName);
        arr.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            arr.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }

    private static void WriteRendererArray(SerializedObject so, string propName, Renderer[] values)
    {
        SerializedProperty arr = so.FindProperty(propName);
        arr.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            arr.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }

    private static VolumeProfile CreateOrReplaceDemoVolumeProfile()
    {
        const string path = "Assets/Settings/ConveyorDemo_VolumeProfile.asset";
        VolumeProfile existing = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (existing != null)
        {
            AssetDatabase.DeleteAsset(path);
        }

        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();

        Bloom bloom = profile.Add<Bloom>(true);
        bloom.threshold.value = 1.02f;
        bloom.intensity.value = 0.24f;
        bloom.scatter.value = 0.52f;
        bloom.highQualityFiltering.value = false;

        ColorAdjustments colorAdj = profile.Add<ColorAdjustments>(true);
        colorAdj.postExposure.value = 0.08f;
        colorAdj.contrast.value = 9f;
        colorAdj.saturation.value = 12f;

        DepthOfField dof = profile.Add<DepthOfField>(true);
        dof.active = true;
        dof.mode.value = DepthOfFieldMode.Gaussian;
        dof.gaussianStart.value = 17f;
        dof.gaussianEnd.value = 38f;
        dof.gaussianMaxRadius.value = 0.52f;
        dof.highQualitySampling.value = false;

        AssetDatabase.CreateAsset(profile, path);
        foreach (VolumeComponent component in profile.components)
        {
            if (component != null)
            {
                AssetDatabase.AddObjectToAsset(component, profile);
            }
        }

        AssetDatabase.SaveAssets();
        return profile;
    }

    private static Texture2D EnsureSharedFloorNormalTexture()
    {
        const string path = "Assets/Art/ConveyorFloorPit_Normal.png";
        Texture2D cached = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (cached != null)
        {
            return cached;
        }

        const int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGB24, true, true);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size;
                float v = y / (float)size;
                float n1 = Mathf.Sin(u * 95f) * 0.035f
                    + Mathf.PerlinNoise(u * 14f, v * 14f) * 0.09f;
                float n2 = Mathf.Sin(v * 95f) * 0.035f
                    + Mathf.PerlinNoise(u * 14f + 19.7f, v * 14f + 11.3f) * 0.09f;
                Vector3 n = new Vector3(n1, n2, 1f).normalized;
                tex.SetPixel(x, y, new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f));
            }
        }

        tex.Apply();
        byte[] png = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);
        string fullPath = Path.Combine(Application.dataPath, "Art", "ConveyorFloorPit_Normal.png");
        File.WriteAllBytes(fullPath, png);

        AssetDatabase.ImportAsset(path);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.NormalMap;
            importer.sRGBTexture = false;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static void ApplySharedNormalMap(Material mat, float bumpScale)
    {
        if (mat == null || !mat.HasProperty("_BumpMap"))
        {
            return;
        }

        Texture2D nrm = EnsureSharedFloorNormalTexture();
        if (nrm == null)
        {
            return;
        }

        mat.EnableKeyword("_NORMALMAP");
        mat.SetTexture("_BumpMap", nrm);
        mat.SetFloat("_BumpScale", bumpScale);
        EditorUtility.SetDirty(mat);
    }

    private static void AttachRailMetalDetails(GameObject rail, float outwardSign)
    {
        Transform t = rail.transform;
        Vector3 c = t.position;
        Vector3 s = t.localScale;
        Material boltMat = CreateMaterialAsset("DecorBolt_Mat", new Color(0.5f, 0.51f, 0.54f));
        boltMat.SetFloat("_Metallic", 0.88f);
        boltMat.SetFloat("_Smoothness", 0.72f);
        Material padMat = CreateMaterialAsset("DecorPad_Mat", new Color(0.38f, 0.39f, 0.42f));
        padMat.SetFloat("_Metallic", 0.55f);
        padMat.SetFloat("_Smoothness", 0.48f);

        float halfLen = s.z * 0.5f;
        float outerX = c.x + outwardSign * (s.x * 0.5f + 0.04f);
        float topY = c.y + s.y * 0.5f;

        GameObject detailRoot = new GameObject("RailMetalDetails");
        detailRoot.transform.SetParent(t);

        for (int i = 0; i < 6; i++)
        {
            float z = Mathf.Lerp(c.z - halfLen + 0.6f, c.z + halfLen - 0.6f, i / 5f);
            GameObject bolt = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bolt.name = $"Bolt_{i}";
            bolt.transform.SetParent(detailRoot.transform);
            bolt.transform.position = new Vector3(outerX, topY, z);
            bolt.transform.localScale = new Vector3(0.07f, 0.035f, 0.07f);
            bolt.GetComponent<Renderer>().sharedMaterial = boltMat;
            Object.DestroyImmediate(bolt.GetComponent<Collider>());

            if (i % 2 == 0)
            {
                GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pad.name = $"Pad_{i}";
                pad.transform.SetParent(detailRoot.transform);
                pad.transform.position = new Vector3(outerX + outwardSign * 0.05f, topY - 0.02f, z + 0.35f);
                pad.transform.localScale = new Vector3(0.14f, 0.018f, 0.22f);
                pad.GetComponent<Renderer>().sharedMaterial = padMat;
                Object.DestroyImmediate(pad.GetComponent<Collider>());
            }
        }
    }

    private static void CreatePointLight(string name, Vector3 pos, Color color, float intensity, float range)
    {
        GameObject go = new GameObject(name);
        go.transform.position = pos;
        Light l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.intensity = intensity;
        l.range = range;
        l.shadows = LightShadows.None;
    }

    private static void CreateConveyorAccent(string name, Vector3 pos, Vector3 scale, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.position = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = CreateMaterialAsset($"{name}_Mat", color);
        Collider col = go.GetComponent<Collider>();
        if (col != null)
        {
            Object.DestroyImmediate(col);
        }
    }

    private static void RemoveCollider(GameObject go)
    {
        Collider c = go.GetComponent<Collider>();
        if (c != null)
        {
            Object.DestroyImmediate(c);
        }
    }

    private static void WireSpawner(CargoSpawner spawner, List<CargoBox> cargoPrefabs)
    {
        SerializedObject so = new SerializedObject(spawner);
        SerializedProperty prefArray = so.FindProperty("cargoPrefabs");
        prefArray.arraySize = cargoPrefabs.Count;
        for (int i = 0; i < cargoPrefabs.Count; i++)
        {
            prefArray.GetArrayElementAtIndex(i).objectReferenceValue = cargoPrefabs[i];
        }

        so.FindProperty("baseSpawnInterval").floatValue = 2.75f;
        so.FindProperty("minSpawnInterval").floatValue = 0.45f;
        so.FindProperty("initialBurstCount").intValue = 1;
        so.FindProperty("spawnArea").vector3Value = new Vector3(0.12f, 0f, 0.06f);
        so.FindProperty("overlapCheckExtents").vector3Value = new Vector3(0.42f, 0.42f, 0.42f);
        so.FindProperty("useRandomSpawnLane").boolValue = true;
        SerializedProperty laneX = so.FindProperty("spawnLaneX");
        laneX.arraySize = 3;
        laneX.GetArrayElementAtIndex(0).floatValue = -3.5f;
        laneX.GetArrayElementAtIndex(1).floatValue = 0f;
        laneX.GetArrayElementAtIndex(2).floatValue = 3.5f;
        so.FindProperty("spawnLaneLateralMin").floatValue = 0.2f;
        so.FindProperty("spawnLaneLateralMax").floatValue = 0.4f;
        so.ApplyModifiedPropertiesWithoutUndo();
    }


    private static Material CreateMaterialAsset(string materialName, Color color)
    {
        string path = $"Assets/Materials/{materialName}.mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            if (existing.HasProperty("_BaseMap"))
            {
                existing.SetTexture("_BaseMap", Texture2D.whiteTexture);
            }

            if (existing.HasProperty("_MainTex"))
            {
                existing.SetTexture("_MainTex", Texture2D.whiteTexture);
            }

            existing.SetColor("_BaseColor", color);
            existing.SetColor("_Color", color);
            existing.SetFloat("_Smoothness", 0.45f);
            existing.SetFloat("_Metallic", 0.08f);
            return existing;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader);
        material.SetColor("_BaseColor", color);
        material.SetColor("_Color", color);
        material.SetFloat("_Smoothness", 0.45f);
        material.SetFloat("_Metallic", 0.08f);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void MakeMaterialMatte(Material material, float smoothness, float metallic)
    {
        if (material == null)
        {
            return;
        }

        material.SetFloat("_Smoothness", smoothness);
        material.SetFloat("_Metallic", metallic);
        EditorUtility.SetDirty(material);
    }

    private static void WireHud(HUDController hud, GameManager gm)
    {
        // HUD subscribes automatically in Start, no explicit references to GM required.
        _ = hud;
        _ = gm;
    }

    private static void ConfigureDiverter(DiverterSwitch dverter)
    {
        _ = dverter;
    }

    private static void ConfigureRouter(LaneRouter router, DiverterSwitch diverter)
    {
        SerializedObject so = new SerializedObject(router);
        so.FindProperty("diverter").objectReferenceValue = diverter;
        SerializedProperty laneX = so.FindProperty("laneXPositions");
        laneX.arraySize = 3;
        laneX.GetArrayElementAtIndex(0).floatValue = -3.5f;
        laneX.GetArrayElementAtIndex(1).floatValue = 0f;
        laneX.GetArrayElementAtIndex(2).floatValue = 3.5f;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureReceiver(ReceiverZone zone, CargoType acceptedType)
    {
        SerializedObject so = new SerializedObject(zone);
        SerializedProperty accepted = so.FindProperty("acceptedTypes");
        accepted.arraySize = 1;
        accepted.GetArrayElementAtIndex(0).enumValueIndex = (int)acceptedType;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
