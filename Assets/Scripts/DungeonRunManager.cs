using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum DungeonDoorAction
{
    Exit,
    Deeper,
    Finish
}

public class DungeonRunManager : MonoBehaviour
{
    static DungeonRunManager instance;

    [SerializeField] int maxLevels = 4;
    [SerializeField] float roomWidth = 28f;
    [SerializeField] float roomDepth = 26f;
    [SerializeField] float roomHeight = 6.2f;
    [SerializeField] float wallThickness = 0.4f;
    [SerializeField] Vector3 dungeonSpawnPosition = new Vector3(0f, 1.1f, -8.2f);
    [SerializeField] Vector3 maxImportedPropSize = new Vector3(4.8f, 4.2f, 4.8f);
    [SerializeField] float importedPropRoomPadding = 1.1f;
    [SerializeField] int[] levelRewards = { 2, 3, 4, 6 };

    GameObject hubRoot;
    Transform dungeonRoot;
    Vector3 hubReturnPosition = new Vector3(0f, 1.1f, 7.2f);
    float hubRoomWidth = 24f;
    float hubRoomDepth = 24f;
    float hubRoomHeight = 6f;
    float hubCameraPadding = 1.25f;
    int currentLevelIndex = -1;
    bool[] rewardedLevels;

    Material floorMaterial;
    Material wallMaterial;
    Material doorMaterial;
    Material woodMaterial;
    Material accentMaterial;
    Material propMaterial;
    Material stonePropMaterial;
    Material metalMaterial;
    Material flameMaterial;
    Material hazardMaterial;
    Material glowMaterial;
    AmbientMode savedAmbientMode;
    Color savedAmbientLight;
    float savedAmbientIntensity;
    bool savedFog;
    Color savedFogColor;
    float savedFogDensity;
    bool hasSavedLighting;

    const string BarrelPrefab = "Assets/Wand and Circles/Polygon City Free Pack - Environment and Interior/Prefabs/Props/Barrel.prefab";
    const string BoxPrefab = "Assets/Wand and Circles/Polygon City Free Pack - Environment and Interior/Prefabs/Props/Box.prefab";
    const string BigStonePrefab = "Assets/Wand and Circles/Polygon City Free Pack - Environment and Interior/Prefabs/Stones/Big Stones/Big stone_2.prefab";
    const string LittleStonePrefab = "Assets/Wand and Circles/Polygon City Free Pack - Environment and Interior/Prefabs/Stones/Little Stones/Little Stone_4.prefab";
    const string WoodenPillarPrefab = "Assets/Wand and Circles/Polygon City Free Pack - Environment and Interior/Prefabs/Props/Wooden pillar.prefab";
    const string SpikePrefab = "Assets/Sprites/FBX/trap-spikes-large.fbx";

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        rewardedLevels = new bool[Mathf.Max(1, maxLevels)];
        BuildMaterials();
    }

    public void Setup(GameObject newHubRoot, Vector3 newHubReturnPosition, float newHubWidth, float newHubDepth, float newHubHeight, float newHubCameraPadding)
    {
        instance = this;
        hubRoot = newHubRoot;
        hubReturnPosition = newHubReturnPosition;
        hubRoomWidth = newHubWidth;
        hubRoomDepth = newHubDepth;
        hubRoomHeight = newHubHeight;
        hubCameraPadding = newHubCameraPadding;
    }

    public static void EnterDungeon(GameObject player)
    {
        DungeonRunManager manager = GetOrCreate();
        manager.StartDungeon(player);
    }

    static DungeonRunManager GetOrCreate()
    {
        if (instance != null)
            return instance;

        DungeonRunManager foundManager = FindAnyObjectByType<DungeonRunManager>();
        if (foundManager != null)
        {
            instance = foundManager;
            return instance;
        }

        GameObject managerObject = new GameObject("Dungeon Run Manager");
        instance = managerObject.AddComponent<DungeonRunManager>();
        return instance;
    }

    void StartDungeon(GameObject player)
    {
        if (player == null)
            return;

        if (currentLevelIndex >= 0 && dungeonRoot != null)
            return;

        KnightShopUI.CloseIfOpen();
        SaveLighting();
        ApplyDungeonLighting();
        rewardedLevels = new bool[Mathf.Max(1, maxLevels)];
        currentLevelIndex = 0;

        if (hubRoot != null)
            hubRoot.SetActive(false);

        BuildLevel(currentLevelIndex, player);
    }

    public void UseDungeonDoor(DungeonDoorAction action, GameObject player)
    {
        if (player == null)
            return;

        switch (action)
        {
            case DungeonDoorAction.Exit:
                ReturnToHub(player);
                break;
            case DungeonDoorAction.Deeper:
                AwardCurrentLevel(player);
                BuildLevel(Mathf.Min(currentLevelIndex + 1, maxLevels - 1), player);
                break;
            case DungeonDoorAction.Finish:
                AwardCurrentLevel(player);
                ReturnToHub(player);
                break;
        }
    }

    void BuildLevel(int levelIndex, GameObject player)
    {
        currentLevelIndex = Mathf.Clamp(levelIndex, 0, maxLevels - 1);

        if (dungeonRoot != null)
            Destroy(dungeonRoot.gameObject);

        dungeonRoot = new GameObject($"Dungeon Level {currentLevelIndex + 1}").transform;
        dungeonRoot.SetParent(transform, false);

        BuildShell();
        BuildChoiceDoors();

        switch (currentLevelIndex)
        {
            case 0:
                BuildTorchHall();
                break;
            case 1:
                BuildSplitLibrary();
                break;
            case 2:
                BuildSpikeGallery();
                break;
            default:
                BuildVaultRoom();
                break;
        }

        MovePlayer(player, dungeonSpawnPosition, Quaternion.identity);
        ConfigureCamera(roomWidth, roomDepth, roomHeight, wallThickness + 0.9f);
    }

    void ReturnToHub(GameObject player)
    {
        if (dungeonRoot != null)
        {
            Destroy(dungeonRoot.gameObject);
            dungeonRoot = null;
        }

        currentLevelIndex = -1;

        if (hubRoot != null)
            hubRoot.SetActive(true);

        RestoreLighting();
        MovePlayer(player, hubReturnPosition, Quaternion.Euler(0f, 180f, 0f));
        ConfigureCamera(hubRoomWidth, hubRoomDepth, hubRoomHeight, hubCameraPadding);
    }

    void AwardCurrentLevel(GameObject player)
    {
        if (currentLevelIndex < 0 || currentLevelIndex >= rewardedLevels.Length || rewardedLevels[currentLevelIndex])
            return;

        LightbulbWallet wallet = player.GetComponent<LightbulbWallet>();
        if (wallet != null)
        {
            int reward = levelRewards.Length > currentLevelIndex ? levelRewards[currentLevelIndex] : currentLevelIndex + 2;
            wallet.AddLightbulbs(reward);
        }

        rewardedLevels[currentLevelIndex] = true;
    }

    void BuildShell()
    {
        float halfWidth = roomWidth * 0.5f;
        float halfDepth = roomDepth * 0.5f;
        float wallY = roomHeight * 0.5f;

        CreateCube("Dungeon Floor", new Vector3(0f, -0.05f, 0f), new Vector3(roomWidth, 0.1f, roomDepth), floorMaterial, dungeonRoot);
        CreateCube("Dungeon Ceiling", new Vector3(0f, roomHeight, 0f), new Vector3(roomWidth, 0.18f, roomDepth), wallMaterial, dungeonRoot);
        CreateCube("East Dungeon Wall", new Vector3(halfWidth, wallY, 0f), new Vector3(wallThickness, roomHeight, roomDepth), wallMaterial, dungeonRoot);
        CreateCube("West Dungeon Wall", new Vector3(-halfWidth, wallY, 0f), new Vector3(wallThickness, roomHeight, roomDepth), wallMaterial, dungeonRoot);
        CreateCube("North Dungeon Wall", new Vector3(0f, wallY, halfDepth), new Vector3(roomWidth, roomHeight, wallThickness), wallMaterial, dungeonRoot);
        CreateCube("South Dungeon Wall", new Vector3(0f, wallY, -halfDepth), new Vector3(roomWidth, roomHeight, wallThickness), wallMaterial, dungeonRoot);

        CreateLabel($"Dungeon {currentLevelIndex + 1}", new Vector3(0f, 3.2f, -halfDepth + 0.55f), Quaternion.Euler(0f, 180f, 0f), 0.2f, dungeonRoot);
    }

    void BuildChoiceDoors()
    {
        float halfDepth = roomDepth * 0.5f;
        bool finalLevel = currentLevelIndex >= maxLevels - 1;

        BuildDungeonDoor("Exit Door", "Exit", DungeonDoorAction.Exit, new Vector3(0f, 0f, -halfDepth + 0.25f), Quaternion.Euler(0f, 180f, 0f));
        BuildDungeonDoor("Deeper Door", finalLevel ? "Finish" : "Deeper", finalLevel ? DungeonDoorAction.Finish : DungeonDoorAction.Deeper,
            new Vector3(0f, 0f, halfDepth - 0.25f), Quaternion.identity);
    }

    void BuildTorchHall()
    {
        SetAccent(new Color(0.9f, 0.42f, 0.13f, 1f), new Color(3f, 1.1f, 0.22f));

        for (int i = 0; i < 4; i++)
        {
            float z = -4.8f + (i * 3.2f);
            CreatePillar(new Vector3(-5.4f, 2.8f, z), accentMaterial);
            CreatePillar(new Vector3(5.4f, 2.8f, z), accentMaterial);
        }

        CreateLowWall("Hall Left Barricade", new Vector3(-2.4f, 0.45f, -1.2f), new Vector3(3.4f, 0.9f, 0.5f));
        CreateLowWall("Hall Right Barricade", new Vector3(2.4f, 0.45f, 2.2f), new Vector3(3.4f, 0.9f, 0.5f));
        CreateTorch(new Vector3(-10.8f, 1.7f, -8.5f));
        CreateTorch(new Vector3(10.8f, 1.7f, -2.5f));
        CreateTorch(new Vector3(-10.8f, 1.7f, 4.5f));
        CreateTorch(new Vector3(10.8f, 1.7f, 9f));
        PlaceProp(BarrelPrefab, new Vector3(-8.4f, 0f, -5.2f), Quaternion.Euler(0f, 40f, 0f), Vector3.one, PrimitiveType.Cylinder);
        PlaceProp(BoxPrefab, new Vector3(8.1f, 0f, -7.1f), Quaternion.Euler(0f, -20f, 0f), Vector3.one, PrimitiveType.Cube);
        PlaceProp(BigStonePrefab, new Vector3(-7.8f, 0f, 7.1f), Quaternion.Euler(0f, 25f, 0f), Vector3.one * 1.25f, PrimitiveType.Sphere);
    }

    void BuildSplitLibrary()
    {
        SetAccent(new Color(0.44f, 0.36f, 0.22f, 1f), new Color(1.5f, 0.8f, 0.25f));

        CreateCube("Central Divider", new Vector3(0f, 1.4f, -0.4f), new Vector3(1f, 2.8f, 9.5f), wallMaterial, dungeonRoot);
        CreateCube("Left Shelf Wall", new Vector3(-5.8f, 1.2f, -1.5f), new Vector3(0.65f, 2.4f, 6.5f), woodMaterial, dungeonRoot);
        CreateCube("Right Shelf Wall", new Vector3(5.8f, 1.2f, 1.5f), new Vector3(0.65f, 2.4f, 6.5f), woodMaterial, dungeonRoot);
        CreateCube("Broken Shelf", new Vector3(-2.4f, 0.9f, 5.8f), new Vector3(3.3f, 1.8f, 0.55f), woodMaterial, dungeonRoot);
        CreateCube("Reading Table", new Vector3(3.4f, 0.55f, -5.8f), new Vector3(3.2f, 1.1f, 1f), woodMaterial, dungeonRoot);

        PlaceProp("Assets/Wand and Circles/Polygon City Free Pack - Environment and Interior/Prefabs/Props/Shelf.prefab",
            new Vector3(-8.9f, 0f, 2.4f), Quaternion.Euler(0f, 90f, 0f), Vector3.one, PrimitiveType.Cube);
        PlaceProp("Assets/Wand and Circles/Polygon City Free Pack - Environment and Interior/Prefabs/Props/Table.prefab",
            new Vector3(7.9f, 0f, -5.8f), Quaternion.Euler(0f, -25f, 0f), Vector3.one, PrimitiveType.Cube);
        PlaceProp(BarrelPrefab, new Vector3(-7.5f, 0f, -7.6f), Quaternion.identity, Vector3.one, PrimitiveType.Cylinder);
        PlaceProp(BoxPrefab, new Vector3(8.7f, 0f, 6.9f), Quaternion.Euler(0f, 14f, 0f), Vector3.one, PrimitiveType.Cube);

        CreateTorch(new Vector3(-10.8f, 1.7f, -8.8f));
        CreateTorch(new Vector3(10.8f, 1.7f, -8.8f));
        CreateTorch(new Vector3(-10.8f, 1.7f, 8.8f));
        CreateTorch(new Vector3(10.8f, 1.7f, 8.8f));
    }

    void BuildSpikeGallery()
    {
        SetAccent(new Color(0.36f, 0.45f, 0.5f, 1f), new Color(0.4f, 1.4f, 1.8f));

        CreateHazardStrip(new Vector3(-4.8f, 0.02f, -1.5f), new Vector3(2.8f, 0.05f, 10.5f));
        CreateHazardStrip(new Vector3(4.8f, 0.02f, 1.5f), new Vector3(2.8f, 0.05f, 10.5f));

        for (int i = 0; i < 5; i++)
        {
            float z = -6f + (i * 3f);
            PlaceSpike(new Vector3(-4.8f, 0.08f, z), Quaternion.identity);
            PlaceSpike(new Vector3(4.8f, 0.08f, z + 1.2f), Quaternion.Euler(0f, 180f, 0f));
        }

        CreateLowWall("Spike Gallery Left Wall", new Vector3(-0.8f, 0.45f, -5f), new Vector3(4.3f, 0.9f, 0.48f));
        CreateLowWall("Spike Gallery Right Wall", new Vector3(0.8f, 0.45f, 4.8f), new Vector3(4.3f, 0.9f, 0.48f));
        CreateCrystal(new Vector3(-9.5f, 0.85f, -2.5f));
        CreateCrystal(new Vector3(9.4f, 0.85f, 4.2f));
        CreateCrystal(new Vector3(0f, 0.85f, 0f));
        CreateTorch(new Vector3(-10.8f, 1.7f, -8.8f));
        CreateTorch(new Vector3(10.8f, 1.7f, 8.8f));
    }

    void BuildVaultRoom()
    {
        SetAccent(new Color(0.72f, 0.64f, 0.28f, 1f), new Color(2.3f, 1.8f, 0.45f));

        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f;
            Vector3 position = Quaternion.Euler(0f, angle, 0f) * new Vector3(7.3f, 0f, 0f);
            CreatePillar(new Vector3(position.x, 2.8f, position.z), accentMaterial);
        }

        CreateCube("Vault Altar", new Vector3(0f, 0.75f, 0f), new Vector3(3.6f, 1.5f, 3.6f), woodMaterial, dungeonRoot);
        CreateCrystal(new Vector3(0f, 2f, 0f));
        CreateLowWall("Vault West Bench", new Vector3(-5.3f, 0.45f, -4.6f), new Vector3(4.5f, 0.9f, 0.45f));
        CreateLowWall("Vault East Bench", new Vector3(5.3f, 0.45f, 4.6f), new Vector3(4.5f, 0.9f, 0.45f));
        PlaceProp(BigStonePrefab, new Vector3(-8.7f, 0f, -7.3f), Quaternion.Euler(0f, 20f, 0f), Vector3.one * 1.2f, PrimitiveType.Sphere);
        PlaceProp(LittleStonePrefab, new Vector3(8.1f, 0f, -6.4f), Quaternion.Euler(0f, -12f, 0f), Vector3.one * 1.3f, PrimitiveType.Sphere);
        PlaceProp(WoodenPillarPrefab, new Vector3(-9.8f, 0f, 6.4f), Quaternion.identity, Vector3.one, PrimitiveType.Cylinder);
        PlaceProp(WoodenPillarPrefab, new Vector3(9.8f, 0f, -1.2f), Quaternion.identity, Vector3.one, PrimitiveType.Cylinder);
        CreateTorch(new Vector3(-10.8f, 1.7f, -8.8f));
        CreateTorch(new Vector3(10.8f, 1.7f, -8.8f));
        CreateTorch(new Vector3(-10.8f, 1.7f, 8.8f));
        CreateTorch(new Vector3(10.8f, 1.7f, 8.8f));
    }

    void BuildDungeonDoor(string objectName, string label, DungeonDoorAction action, Vector3 localPosition, Quaternion localRotation)
    {
        Transform doorRoot = new GameObject(objectName).transform;
        doorRoot.SetParent(dungeonRoot, false);
        doorRoot.localPosition = localPosition;
        doorRoot.localRotation = localRotation;

        CreateCube("Door Panel", new Vector3(0f, 1.6f, 0f), new Vector3(3.2f, 3.2f, 0.28f), doorMaterial, doorRoot);
        CreateCube("Left Door Frame", new Vector3(-1.9f, 1.75f, -0.02f), new Vector3(0.28f, 3.7f, 0.45f), woodMaterial, doorRoot);
        CreateCube("Right Door Frame", new Vector3(1.9f, 1.75f, -0.02f), new Vector3(0.28f, 3.7f, 0.45f), woodMaterial, doorRoot);
        CreateCube("Top Door Frame", new Vector3(0f, 3.55f, -0.02f), new Vector3(4.1f, 0.28f, 0.45f), woodMaterial, doorRoot);
        CreateLabel(label, new Vector3(0f, 4.1f, -0.24f), Quaternion.identity, 0.24f, doorRoot);
        TextMesh prompt = CreateLabel("Press X", new Vector3(0f, 4.55f, -0.24f), Quaternion.identity, 0.16f, doorRoot);
        prompt.color = new Color(0.9f, 0.95f, 1f);
        prompt.gameObject.SetActive(false);

        GameObject trigger = CreateCube("Dungeon Door Trigger", new Vector3(0f, 1.35f, -1.1f), new Vector3(4.2f, 2.7f, 1f), null, doorRoot);
        trigger.GetComponent<Renderer>().enabled = false;
        BoxCollider triggerCollider = trigger.GetComponent<BoxCollider>();
        triggerCollider.isTrigger = true;

        DungeonDoorTrigger doorTrigger = trigger.AddComponent<DungeonDoorTrigger>();
        doorTrigger.Setup(this, action, prompt.gameObject);
    }

    void CreatePillar(Vector3 localPosition, Material material)
    {
        GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pillar.name = "Dungeon Pillar";
        pillar.transform.SetParent(dungeonRoot, false);
        pillar.transform.localPosition = localPosition;
        pillar.transform.localScale = new Vector3(0.85f, 2.8f, 0.85f);

        if (material != null)
            pillar.GetComponent<Renderer>().sharedMaterial = material;
    }

    void CreateLowWall(string objectName, Vector3 localPosition, Vector3 localScale)
    {
        CreateCube(objectName, localPosition, localScale, wallMaterial, dungeonRoot);
    }

    void CreateHazardStrip(Vector3 localPosition, Vector3 localScale)
    {
        CreateCube("Spike Pit Shadow", localPosition, localScale, hazardMaterial, dungeonRoot);
    }

    void PlaceSpike(Vector3 localPosition, Quaternion localRotation)
    {
        GameObject spike = PlaceImportedAsset(SpikePrefab, localPosition, localRotation, Vector3.one * 0.9f, dungeonRoot, true);
        if (spike != null)
            return;

        CreateCube("Spike Base", localPosition + Vector3.up * 0.05f, new Vector3(1.8f, 0.1f, 1.8f), hazardMaterial, dungeonRoot);
        for (int i = 0; i < 3; i++)
        {
            GameObject spikePrimitive = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spikePrimitive.name = "Spike";
            spikePrimitive.transform.SetParent(dungeonRoot, false);
            spikePrimitive.transform.localPosition = localPosition + new Vector3(-0.45f + (i * 0.45f), 0.34f, 0f);
            spikePrimitive.transform.localScale = new Vector3(0.14f, 0.38f, 0.14f);
            spikePrimitive.GetComponent<Renderer>().sharedMaterial = accentMaterial;
        }
    }

    void CreateCrystal(Vector3 localPosition)
    {
        GameObject crystal = GameObject.CreatePrimitive(PrimitiveType.Cube);
        crystal.name = "Glow Crystal";
        crystal.transform.SetParent(dungeonRoot, false);
        crystal.transform.localPosition = localPosition;
        crystal.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
        crystal.transform.localScale = new Vector3(0.65f, 1.25f, 0.65f);
        crystal.GetComponent<Renderer>().sharedMaterial = glowMaterial;

        Light light = crystal.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(0.72f, 0.92f, 1f);
        light.intensity = 3.1f;
        light.range = 8.5f;
    }

    void CreateTorch(Vector3 localPosition)
    {
        Transform torch = new GameObject("Dungeon Torch").transform;
        torch.SetParent(dungeonRoot, false);
        torch.localPosition = localPosition;

        GameObject bracket = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        bracket.name = "Torch Bracket";
        bracket.transform.SetParent(torch, false);
        bracket.transform.localPosition = Vector3.zero;
        bracket.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        bracket.transform.localScale = new Vector3(0.16f, 0.55f, 0.16f);
        bracket.GetComponent<Renderer>().sharedMaterial = woodMaterial;
        Destroy(bracket.GetComponent<Collider>());

        GameObject flame = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flame.name = "Torch Flame";
        flame.transform.SetParent(torch, false);
        flame.transform.localPosition = new Vector3(0f, 0.45f, 0f);
        flame.transform.localScale = new Vector3(0.32f, 0.5f, 0.32f);
        flame.GetComponent<Renderer>().sharedMaterial = flameMaterial;
        Destroy(flame.GetComponent<Collider>());

        Light light = torch.gameObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.55f, 0.22f);
        light.intensity = 4.4f;
        light.range = 12f;
        light.shadows = LightShadows.Soft;
    }

    GameObject PlaceProp(string assetPath, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, PrimitiveType fallbackType)
    {
        GameObject importedProp = PlaceImportedAsset(assetPath, localPosition, localRotation, localScale, dungeonRoot, true);
        if (importedProp != null)
            return importedProp;

        GameObject fallback = GameObject.CreatePrimitive(fallbackType);
        fallback.name = "Dungeon Prop";
        fallback.transform.SetParent(dungeonRoot, false);
        fallback.transform.localPosition = localPosition + Vector3.up * 0.45f;
        fallback.transform.localRotation = localRotation;
        fallback.transform.localScale = localScale;
        fallback.GetComponent<Renderer>().sharedMaterial = woodMaterial;
        return fallback;
    }

    GameObject PlaceImportedAsset(string assetPath, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Transform parent, bool stripColliders)
    {
#if UNITY_EDITOR
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null)
            return null;

        UnityEngine.Object prefabInstance = PrefabUtility.InstantiatePrefab(prefab, parent);
        GameObject instanceObject = prefabInstance as GameObject;
        if (instanceObject == null)
            return null;

        instanceObject.name = prefab.name;
        instanceObject.transform.localPosition = localPosition;
        instanceObject.transform.localRotation = localRotation;
        instanceObject.transform.localScale = localScale;

        if (stripColliders)
            StripColliders(instanceObject);

        ApplyImportedMaterials(instanceObject, assetPath);
        if (!ImportedAssetFitsRoom(instanceObject, assetPath))
        {
            DestroyGeneratedObject(instanceObject);
            return null;
        }

        return instanceObject;
#else
        return null;
#endif
    }

    void StripColliders(GameObject target)
    {
        Collider[] colliders = target.GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            Destroy(collider);
        }
    }

    void ApplyImportedMaterials(GameObject target, string assetPath)
    {
        Material material = GetImportedMaterial(assetPath);
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = material;
            }

            renderer.sharedMaterials = materials;
        }
    }

    Material GetImportedMaterial(string assetPath)
    {
        string lowerPath = assetPath.ToLowerInvariant();
        if (lowerPath.Contains("stone"))
            return stonePropMaterial;

        if (lowerPath.Contains("spike") || lowerPath.Contains("metall") || lowerPath.Contains("metal"))
            return metalMaterial;

        if (lowerPath.Contains("barrel") || lowerPath.Contains("box") || lowerPath.Contains("pillar") ||
            lowerPath.Contains("shelf") || lowerPath.Contains("table") || lowerPath.Contains("door"))
            return propMaterial;

        return propMaterial;
    }

    bool ImportedAssetFitsRoom(GameObject target, string assetPath)
    {
        if (!TryGetRendererBounds(target, out Bounds bounds))
            return false;

        Vector3 size = bounds.size;
        if (size.x > maxImportedPropSize.x || size.y > maxImportedPropSize.y || size.z > maxImportedPropSize.z)
        {
            Debug.Log($"Skipped oversized dungeon prop '{assetPath}' with size {size}.");
            return false;
        }

        float halfWidth = (roomWidth * 0.5f) - importedPropRoomPadding;
        float halfDepth = (roomDepth * 0.5f) - importedPropRoomPadding;
        if (bounds.min.x < -halfWidth || bounds.max.x > halfWidth || bounds.min.z < -halfDepth || bounds.max.z > halfDepth || bounds.max.y > roomHeight)
        {
            Debug.Log($"Skipped dungeon prop outside room bounds '{assetPath}' with bounds {bounds}.");
            return false;
        }

        return true;
    }

    bool TryGetRendererBounds(GameObject target, out Bounds bounds)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        bounds = new Bounds(target.transform.position, Vector3.zero);
        bool hasBounds = false;

        foreach (Renderer renderer in renderers)
        {
            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    void DestroyGeneratedObject(GameObject target)
    {
        if (target == null)
            return;

        target.SetActive(false);
        Destroy(target);
    }

    GameObject CreateCube(string objectName, Vector3 localPosition, Vector3 localScale, Material material, Transform parent)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = objectName;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = localPosition;
        cube.transform.localScale = localScale;

        if (material != null)
            cube.GetComponent<Renderer>().sharedMaterial = material;

        return cube;
    }

    TextMesh CreateLabel(string labelText, Vector3 localPosition, Quaternion localRotation, float characterSize, Transform parent)
    {
        GameObject label = new GameObject(labelText + " Label");
        label.transform.SetParent(parent, false);
        label.transform.localPosition = localPosition;
        label.transform.localRotation = localRotation;

        TextMesh textMesh = label.AddComponent<TextMesh>();
        textMesh.text = labelText;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.characterSize = characterSize;
        textMesh.fontSize = 64;
        textMesh.color = new Color(1f, 0.78f, 0.36f);

        return textMesh;
    }

    void MovePlayer(GameObject player, Vector3 position, Quaternion rotation)
    {
        player.transform.SetPositionAndRotation(position, rotation);

        Rigidbody playerBody = player.GetComponent<Rigidbody>();
        if (playerBody == null)
            return;

        playerBody.linearVelocity = Vector3.zero;
        playerBody.angularVelocity = Vector3.zero;
    }

    void ConfigureCamera(float width, float depth, float height, float padding)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return;

        CameraFollow cameraFollow = mainCamera.GetComponent<CameraFollow>();
        if (cameraFollow == null)
            return;

        cameraFollow.ConfigureRoomBounds(width, depth, height, padding);
    }

    void SetAccent(Color color, Color emissionColor)
    {
        accentMaterial = CreateMaterial("Dungeon Accent", color);
        glowMaterial = CreateMaterial("Dungeon Glow", color, emissionColor);
    }

    void BuildMaterials()
    {
        floorMaterial = CreateMaterial("Dungeon Floor", new Color(0.14f, 0.135f, 0.13f));
        wallMaterial = CreateMaterial("Dungeon Wall", new Color(0.12f, 0.118f, 0.13f));
        doorMaterial = CreateMaterial("Dungeon Door", new Color(0.2f, 0.09f, 0.045f));
        woodMaterial = CreateMaterial("Dungeon Wood", new Color(0.28f, 0.17f, 0.085f));
        propMaterial = CreateMaterial("Dungeon Prop Wood", new Color(0.36f, 0.22f, 0.12f));
        stonePropMaterial = CreateMaterial("Dungeon Prop Stone", new Color(0.24f, 0.235f, 0.23f));
        metalMaterial = CreateMaterial("Dungeon Metal", new Color(0.32f, 0.32f, 0.34f));
        accentMaterial = CreateMaterial("Dungeon Accent", new Color(0.62f, 0.44f, 0.22f));
        flameMaterial = CreateMaterial("Dungeon Flame", new Color(1f, 0.48f, 0.08f), new Color(3f, 1.35f, 0.22f));
        hazardMaterial = CreateMaterial("Dungeon Hazard", new Color(0.025f, 0.022f, 0.027f));
        glowMaterial = CreateMaterial("Dungeon Glow", new Color(0.72f, 0.92f, 1f), new Color(0.4f, 1.4f, 1.8f));
    }

    void SaveLighting()
    {
        if (hasSavedLighting)
            return;

        savedAmbientMode = RenderSettings.ambientMode;
        savedAmbientLight = RenderSettings.ambientLight;
        savedAmbientIntensity = RenderSettings.ambientIntensity;
        savedFog = RenderSettings.fog;
        savedFogColor = RenderSettings.fogColor;
        savedFogDensity = RenderSettings.fogDensity;
        hasSavedLighting = true;
    }

    void ApplyDungeonLighting()
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.22f, 0.21f, 0.24f);
        RenderSettings.ambientIntensity = 0.92f;
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.09f, 0.085f, 0.1f);
        RenderSettings.fogDensity = 0.007f;
    }

    void RestoreLighting()
    {
        if (!hasSavedLighting)
            return;

        RenderSettings.ambientMode = savedAmbientMode;
        RenderSettings.ambientLight = savedAmbientLight;
        RenderSettings.ambientIntensity = savedAmbientIntensity;
        RenderSettings.fog = savedFog;
        RenderSettings.fogColor = savedFogColor;
        RenderSettings.fogDensity = savedFogDensity;
        hasSavedLighting = false;
    }

    Material CreateMaterial(string materialName, Color color)
    {
        return CreateMaterial(materialName, color, Color.black);
    }

    Material CreateMaterial(string materialName, Color color, Color emissionColor)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = new Material(shader);
        material.name = materialName;

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);

        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);

        if (emissionColor.maxColorComponent > 0f && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emissionColor);
        }

        return material;
    }
}

public class DungeonDoorTrigger : MonoBehaviour
{
    DungeonRunManager manager;
    DungeonDoorAction action;
    GameObject prompt;
    GameObject player;
    bool playerInRange;

    public void Setup(DungeonRunManager newManager, DungeonDoorAction newAction, GameObject newPrompt)
    {
        manager = newManager;
        action = newAction;
        prompt = newPrompt;
        UpdatePrompt();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = true;
        player = other.gameObject;
        UpdatePrompt();
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = false;
        player = null;
        UpdatePrompt();
    }

    void Update()
    {
        if (!playerInRange || manager == null || player == null)
            return;

        if (Keyboard.current == null || !Keyboard.current.xKey.wasPressedThisFrame)
            return;

        manager.UseDungeonDoor(action, player);
    }

    void OnDisable()
    {
        playerInRange = false;
        player = null;
        UpdatePrompt();
    }

    void UpdatePrompt()
    {
        if (prompt != null)
            prompt.SetActive(playerInRange);
    }
}
