using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class HubRoomBuilder : MonoBehaviour
{
    [Header("Room")]
    [SerializeField] Vector3 playerSpawnPosition = new Vector3(0f, 1.1f, -4f);
    [SerializeField] float roomWidth = 24f;
    [SerializeField] float roomDepth = 24f;
    [SerializeField] float roomHeight = 6f;
    [SerializeField] float wallThickness = 0.35f;
    [SerializeField] float doorWidth = 3f;
    [SerializeField] float doorHeight = 3.2f;

    [Header("Lighting")]
    [SerializeField] bool dimDirectionalLights = true;
    [SerializeField] float torchIntensity = 2.3f;
    [SerializeField] float torchRange = 8f;

    Transform buildRoot;
    Material floorMaterial;
    Material wallMaterial;
    Material woodMaterial;
    Material doorMaterial;
    Material flameMaterial;
    Material goblinMaterial;
    Material signMaterial;
    Material darkMetalMaterial;

    void Awake()
    {
        PrepareLighting();
        BuildMaterials();
        MovePlayerToSpawn();
        BuildRoom();
        BuildShop();
        BuildDoors();
        BuildTorches();
        ConfigureCameraBounds();
    }

    void PrepareLighting()
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.015f, 0.014f, 0.02f);
        RenderSettings.ambientIntensity = 0.18f;
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.015f, 0.014f, 0.02f);
        RenderSettings.fogDensity = 0.025f;

        if (!dimDirectionalLights)
            return;

        Light[] lights = FindObjectsByType<Light>(FindObjectsInactive.Exclude);
        foreach (Light sceneLight in lights)
        {
            if (sceneLight.type == LightType.Directional)
                sceneLight.intensity = 0f;
        }
    }

    void BuildMaterials()
    {
        floorMaterial = CreateMaterial("Hub Floor", new Color(0.12f, 0.115f, 0.11f));
        wallMaterial = CreateMaterial("Hub Stone Wall", new Color(0.09f, 0.085f, 0.09f));
        woodMaterial = CreateMaterial("Hub Dark Wood", new Color(0.23f, 0.13f, 0.065f));
        doorMaterial = CreateMaterial("Hub Door Wood", new Color(0.17f, 0.075f, 0.035f));
        flameMaterial = CreateMaterial("Hub Torch Flame", new Color(1f, 0.48f, 0.08f), new Color(3f, 1.35f, 0.22f));
        goblinMaterial = CreateMaterial("Shopkeeper Green", new Color(0.25f, 0.55f, 0.23f));
        signMaterial = CreateMaterial("Shop Sign", new Color(0.42f, 0.28f, 0.12f));
        darkMetalMaterial = CreateMaterial("Dark Metal", new Color(0.04f, 0.04f, 0.045f));
    }

    void MovePlayerToSpawn()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;

        player.transform.SetPositionAndRotation(playerSpawnPosition, Quaternion.identity);

        Rigidbody playerBody = player.GetComponent<Rigidbody>();
        if (playerBody == null)
            return;

        playerBody.linearVelocity = Vector3.zero;
        playerBody.angularVelocity = Vector3.zero;
    }

    void BuildRoom()
    {
        buildRoot = new GameObject("Generated Hub Room").transform;
        buildRoot.SetParent(transform, false);

        float halfWidth = roomWidth * 0.5f;
        float halfDepth = roomDepth * 0.5f;
        float wallY = roomHeight * 0.5f;

        CreateCube("Stone Floor", new Vector3(0f, -0.05f, 0f), new Vector3(roomWidth, 0.1f, roomDepth), floorMaterial, buildRoot);
        CreateCube("Low Stone Ceiling", new Vector3(0f, roomHeight, 0f), new Vector3(roomWidth, 0.25f, roomDepth), wallMaterial, buildRoot);

        CreateDoorWall("North Wall", Vector3.forward * halfDepth, true, wallY);
        CreateSolidWall("South Wall", Vector3.back * halfDepth, true, wallY);
        CreateSolidWall("East Wall", Vector3.right * halfWidth, false, wallY);
        CreateSolidWall("West Wall", Vector3.left * halfWidth, false, wallY);
    }

    void CreateDoorWall(string wallName, Vector3 center, bool runsAlongX, float wallY)
    {
        float totalLength = runsAlongX ? roomWidth : roomDepth;
        float sideLength = (totalLength - doorWidth) * 0.5f;
        float sideOffset = doorWidth * 0.5f + sideLength * 0.5f;

        Vector3 segmentSize = runsAlongX
            ? new Vector3(sideLength, roomHeight, wallThickness)
            : new Vector3(wallThickness, roomHeight, sideLength);

        Vector3 topSize = runsAlongX
            ? new Vector3(doorWidth, roomHeight - doorHeight, wallThickness)
            : new Vector3(wallThickness, roomHeight - doorHeight, doorWidth);

        Vector3 axis = runsAlongX ? Vector3.right : Vector3.forward;

        CreateCube(wallName + " Left Segment", center - axis * sideOffset + Vector3.up * wallY, segmentSize, wallMaterial, buildRoot);
        CreateCube(wallName + " Right Segment", center + axis * sideOffset + Vector3.up * wallY, segmentSize, wallMaterial, buildRoot);
        CreateCube(wallName + " Door Header", center + Vector3.up * (doorHeight + ((roomHeight - doorHeight) * 0.5f)), topSize, wallMaterial, buildRoot);
    }

    void CreateSolidWall(string wallName, Vector3 center, bool runsAlongX, float wallY)
    {
        Vector3 wallSize = runsAlongX
            ? new Vector3(roomWidth, roomHeight, wallThickness)
            : new Vector3(wallThickness, roomHeight, roomDepth);

        CreateCube(wallName, center + Vector3.up * wallY, wallSize, wallMaterial, buildRoot);
    }

    void BuildShop()
    {
        Transform shopRoot = new GameObject("Goblin Shop").transform;
        shopRoot.SetParent(buildRoot, false);
        shopRoot.localPosition = new Vector3(-8.2f, 0f, -8.2f);
        shopRoot.localRotation = Quaternion.Euler(0f, -135f, 0f);

        CreateCube("Shop Counter", new Vector3(0f, 0.55f, 0f), new Vector3(4.4f, 1.1f, 1f), woodMaterial, shopRoot);
        CreateCube("Counter Top", new Vector3(0f, 1.15f, 0f), new Vector3(4.8f, 0.18f, 1.25f), signMaterial, shopRoot);
        CreateCube("Shop Shelf", new Vector3(0f, 2.05f, 1f), new Vector3(4.2f, 0.2f, 0.35f), woodMaterial, shopRoot);
        CreateCube("Shop Sign", new Vector3(0f, 2.85f, 0.45f), new Vector3(2.6f, 0.55f, 0.12f), signMaterial, shopRoot);
        CreateLabel("SHOP", new Vector3(0f, 2.85f, 0.36f), Quaternion.identity, 0.28f, shopRoot);

        CreateGoblinShopkeeper(shopRoot);
        CreateShopProps(shopRoot);
        CreateShopInteraction(shopRoot);
    }

    void CreateGoblinShopkeeper(Transform parent)
    {
        Transform goblin = new GameObject("Goblin Shopkeeper").transform;
        goblin.SetParent(parent, false);
        goblin.localPosition = new Vector3(0f, 1.1f, 0.85f);
        goblin.localRotation = Quaternion.identity;

        CreatePrimitiveChild("Goblin Body", PrimitiveType.Capsule, new Vector3(0f, 0.25f, 0f), new Vector3(0.55f, 0.75f, 0.55f), goblinMaterial, goblin, false);
        CreatePrimitiveChild("Goblin Head", PrimitiveType.Sphere, new Vector3(0f, 0.9f, 0f), new Vector3(0.48f, 0.42f, 0.48f), goblinMaterial, goblin, false);
        CreatePrimitiveChild("Goblin Nose", PrimitiveType.Sphere, new Vector3(0f, 0.88f, -0.27f), new Vector3(0.16f, 0.11f, 0.22f), goblinMaterial, goblin, false);
        CreatePrimitiveChild("Left Ear", PrimitiveType.Sphere, new Vector3(-0.33f, 0.9f, 0f), new Vector3(0.25f, 0.12f, 0.16f), goblinMaterial, goblin, false);
        CreatePrimitiveChild("Right Ear", PrimitiveType.Sphere, new Vector3(0.33f, 0.9f, 0f), new Vector3(0.25f, 0.12f, 0.16f), goblinMaterial, goblin, false);
        CreatePrimitiveChild("Left Eye", PrimitiveType.Sphere, new Vector3(-0.11f, 0.96f, -0.22f), new Vector3(0.06f, 0.06f, 0.06f), darkMetalMaterial, goblin, false);
        CreatePrimitiveChild("Right Eye", PrimitiveType.Sphere, new Vector3(0.11f, 0.96f, -0.22f), new Vector3(0.06f, 0.06f, 0.06f), darkMetalMaterial, goblin, false);
    }

    void CreateShopProps(Transform parent)
    {
        for (int i = 0; i < 3; i++)
        {
            float x = -1.2f + (i * 1.2f);
            GameObject bottle = CreatePrimitiveChild("Potion Bottle", PrimitiveType.Cylinder, new Vector3(x, 1.42f, -0.22f), new Vector3(0.18f, 0.34f, 0.18f), flameMaterial, parent, false);
            bottle.transform.localRotation = Quaternion.Euler(0f, i * 30f, 0f);
        }

        CreatePrimitiveChild("Coin Pile", PrimitiveType.Sphere, new Vector3(1.65f, 1.32f, -0.2f), new Vector3(0.42f, 0.12f, 0.42f), signMaterial, parent, false);
    }

    void BuildDoors()
    {
        float halfDepth = roomDepth * 0.5f;

        CreateDoor("North Door", "North", new Vector3(0f, 0f, halfDepth - 0.2f), Quaternion.identity);
    }

    void CreateDoor(string objectName, string doorName, Vector3 position, Quaternion rotation)
    {
        Transform doorRoot = new GameObject(objectName).transform;
        doorRoot.SetParent(buildRoot, false);
        doorRoot.localPosition = position;
        doorRoot.localRotation = rotation;

        CreateCube("Door Panel", new Vector3(0f, doorHeight * 0.5f, 0f), new Vector3(doorWidth, doorHeight, 0.28f), doorMaterial, doorRoot);
        CreateCube("Left Door Frame", new Vector3(-doorWidth * 0.55f, doorHeight * 0.5f, -0.02f), new Vector3(0.28f, doorHeight + 0.35f, 0.45f), woodMaterial, doorRoot);
        CreateCube("Right Door Frame", new Vector3(doorWidth * 0.55f, doorHeight * 0.5f, -0.02f), new Vector3(0.28f, doorHeight + 0.35f, 0.45f), woodMaterial, doorRoot);
        CreateCube("Top Door Frame", new Vector3(0f, doorHeight + 0.1f, -0.02f), new Vector3(doorWidth + 0.85f, 0.28f, 0.45f), woodMaterial, doorRoot);
        CreateLabel(doorName, new Vector3(0f, doorHeight + 0.55f, -0.25f), Quaternion.identity, 0.22f, doorRoot);

        GameObject trigger = CreateCube("Door Choice Trigger", new Vector3(0f, 1.35f, -0.9f), new Vector3(doorWidth + 0.7f, 2.7f, 1f), null, doorRoot);
        trigger.GetComponent<Renderer>().enabled = false;

        BoxCollider triggerCollider = trigger.GetComponent<BoxCollider>();
        triggerCollider.isTrigger = true;

        HubDoor hubDoor = trigger.AddComponent<HubDoor>();
        hubDoor.Setup(doorName, doorRoot.GetComponentInChildren<Renderer>());
    }

    void BuildTorches()
    {
        float halfWidth = roomWidth * 0.5f;
        float halfDepth = roomDepth * 0.5f;
        CreateTorch("Torch A", new Vector3(-halfWidth + 1.2f, 1.7f, -halfDepth + 1.2f));
        CreateTorch("Torch B", new Vector3(halfWidth - 1.2f, 1.7f, halfDepth - 1.2f));
    }

    void CreateTorch(string torchName, Vector3 position)
    {
        Transform torch = new GameObject(torchName).transform;
        torch.SetParent(buildRoot, false);
        torch.localPosition = position;

        CreatePrimitiveChild("Torch Bracket", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.16f, 0.55f, 0.16f), woodMaterial, torch, false)
            .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        CreatePrimitiveChild("Torch Flame", PrimitiveType.Sphere, new Vector3(0f, 0.45f, 0f), new Vector3(0.32f, 0.5f, 0.32f), flameMaterial, torch, false);

        Light torchLight = torch.gameObject.AddComponent<Light>();
        torchLight.type = LightType.Point;
        torchLight.color = new Color(1f, 0.55f, 0.22f);
        torchLight.intensity = torchIntensity;
        torchLight.range = torchRange;
        torchLight.shadows = LightShadows.Soft;
    }

    void CreateShopInteraction(Transform shopRoot)
    {
        GameObject trigger = CreateCube("Shop Interaction Trigger", new Vector3(0f, 1.25f, -1.6f), new Vector3(5.2f, 2.5f, 2.4f), null, shopRoot);
        trigger.GetComponent<Renderer>().enabled = false;

        BoxCollider triggerCollider = trigger.GetComponent<BoxCollider>();
        triggerCollider.isTrigger = true;

        TextMesh promptText = CreateLabel("Press X to shop", new Vector3(0f, 2.05f, -1.65f), Quaternion.identity, 0.18f, shopRoot);
        promptText.gameObject.SetActive(false);

        Transform popupRoot = new GameObject("Shop Popup").transform;
        popupRoot.SetParent(shopRoot, false);
        popupRoot.localPosition = new Vector3(0f, 2.85f, -1.75f);
        popupRoot.localRotation = Quaternion.identity;
        popupRoot.gameObject.SetActive(false);

        CreateCube("Shop Popup Backing", Vector3.zero, new Vector3(4.1f, 1.55f, 0.08f), darkMetalMaterial, popupRoot);
        TextMesh popupText = CreateLabel("GOBLIN SHOP\nPotions and upgrades soon\nPress X to close", new Vector3(0f, 0.05f, -0.08f), Quaternion.identity, 0.14f, popupRoot);
        popupText.color = new Color(1f, 0.86f, 0.52f);

        HubShopInteraction shopInteraction = trigger.AddComponent<HubShopInteraction>();
        shopInteraction.Setup(promptText.gameObject, popupRoot.gameObject);
    }

    void ConfigureCameraBounds()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return;

        CameraFollow cameraFollow = mainCamera.GetComponent<CameraFollow>();
        if (cameraFollow == null)
            return;

        cameraFollow.ConfigureRoomBounds(roomWidth, roomDepth, roomHeight, wallThickness + 0.9f);
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

    GameObject CreatePrimitiveChild(string objectName, PrimitiveType primitiveType, Vector3 localPosition, Vector3 localScale, Material material, Transform parent, bool keepCollider)
    {
        GameObject primitive = GameObject.CreatePrimitive(primitiveType);
        primitive.name = objectName;
        primitive.transform.SetParent(parent, false);
        primitive.transform.localPosition = localPosition;
        primitive.transform.localScale = localScale;

        if (material != null)
            primitive.GetComponent<Renderer>().sharedMaterial = material;

        if (!keepCollider)
        {
            Collider primitiveCollider = primitive.GetComponent<Collider>();
            if (primitiveCollider != null)
                Destroy(primitiveCollider);
        }

        return primitive;
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

public class HubDoor : MonoBehaviour
{
    string doorName;
    Renderer doorRenderer;
    Color originalColor;
    bool hasOriginalColor;

    public void Setup(string newDoorName, Renderer newDoorRenderer)
    {
        doorName = newDoorName;
        doorRenderer = newDoorRenderer;

        if (doorRenderer != null && doorRenderer.material.HasProperty("_BaseColor"))
        {
            originalColor = doorRenderer.material.GetColor("_BaseColor");
            hasOriginalColor = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        Debug.Log("Selected hub door: " + doorName);
        SetDoorColor(new Color(0.35f, 0.18f, 0.08f));
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (hasOriginalColor)
            SetDoorColor(originalColor);
    }

    void SetDoorColor(Color color)
    {
        if (doorRenderer == null || !doorRenderer.material.HasProperty("_BaseColor"))
            return;

        doorRenderer.material.SetColor("_BaseColor", color);
        if (doorRenderer.material.HasProperty("_Color"))
            doorRenderer.material.SetColor("_Color", color);
    }
}

public class HubShopInteraction : MonoBehaviour
{
    GameObject prompt;
    GameObject popup;
    bool playerInRange;

    public void Setup(GameObject newPrompt, GameObject newPopup)
    {
        prompt = newPrompt;
        popup = newPopup;
        UpdatePrompt();
    }

    void Update()
    {
        if (!playerInRange || Keyboard.current == null || !Keyboard.current.xKey.wasPressedThisFrame)
            return;

        if (popup != null)
            popup.SetActive(!popup.activeSelf);

        UpdatePrompt();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = true;
        UpdatePrompt();
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = false;

        if (popup != null)
            popup.SetActive(false);

        UpdatePrompt();
    }

    void UpdatePrompt()
    {
        if (prompt == null)
            return;

        bool popupIsOpen = popup != null && popup.activeSelf;
        prompt.SetActive(playerInRange && !popupIsOpen);
    }
}
