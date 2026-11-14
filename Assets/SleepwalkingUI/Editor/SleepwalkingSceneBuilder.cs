using Sleepwalking;
using PixelRoomView = Sleepwalking.Visual.PixelRoomView;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SleepwalkingSceneBuilder
{
    [MenuItem("Sleepwalking/Build UI Scene")]
    public static void Build()
    {
        const string path = "Assets/Scenes/SleepwalkingUI.unity";
        EditorSceneManager.OpenScene(path);
        GameObject root = GameObject.Find("Sleepwalking UI");
        if (root == null) root = new GameObject("Sleepwalking UI");
        SleepwalkingUI ui = root.GetComponent<SleepwalkingUI>();
        if (ui == null) ui = root.AddComponent<SleepwalkingUI>();
        if (root.GetComponent<SleepwalkingUIShowcase>() == null) root.AddComponent<SleepwalkingUIShowcase>();
        ui.Build();
        Transform oldRoom = GameObject.Find("UI Room Prototype")?.transform;
        if (oldRoom != null) Object.DestroyImmediate(oldRoom.gameObject);
        BuildPrototype(ui);
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), path);
        AssetDatabase.SaveAssets();
        Debug.Log("Sleepwalking UI showcase updated: " + path);
    }

    static void BuildPrototype(SleepwalkingUI ui)
    {
        // Демонстрационная комната живёт только в UI-сцене; общую комнату создаёт Visual Director.
        Transform room = new GameObject("UI Room Prototype").transform;
        Camera camera = Camera.main;
        if (camera == null)
        {
            GameObject cameraObject = new GameObject("UI Room Camera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            camera = cameraObject.GetComponent<Camera>();
        }
        camera.transform.position = new Vector3(0, 2.8f, -11);
        camera.transform.rotation = Quaternion.Euler(10, 0, 0);
        camera.fieldOfView = 54;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = SleepwalkingUI.Background;
        if (camera.GetComponent<PixelRoomView>() == null) camera.gameObject.AddComponent<PixelRoomView>();
        ui.roomCamera = camera;

        Block(room, "Back Wall", new Vector3(0, 2, 5), new Vector3(14, 7, .3f), SleepwalkingUI.Panel);
        Block(room, "Floor", new Vector3(0, -.75f, 1.5f), new Vector3(14, .3f, 8), SleepwalkingUI.Shadow);

        Transform bed = new GameObject("Bed - New Game").transform;
        bed.SetParent(room);
        bed.gameObject.AddComponent<SleepwalkingRoomTarget>().Action = RoomAction.Dream;
        Block(bed, "Bed Frame", new Vector3(-3.5f, .1f, 1.4f), new Vector3(3, .5f, 2), SleepwalkingUI.FrameColor);
        Block(bed, "Mattress", new Vector3(-3.5f, .5f, 1.4f), new Vector3(2.8f, .35f, 1.8f), SleepwalkingUI.MainText);
        Block(bed, "Pillow", new Vector3(-4.25f, .73f, 1.4f), new Vector3(.8f, .17f, 1.4f), SleepwalkingUI.SecondaryText);

        Transform clock = new GameObject("Clock - Settings").transform;
        clock.SetParent(room);
        clock.gameObject.AddComponent<SleepwalkingRoomTarget>().Action = RoomAction.Settings;
        Block(clock, "Clock Body", new Vector3(-1.1f, 2.25f, 4.55f), new Vector3(1.1f, 1.1f, .24f), SleepwalkingUI.FrameColor);
        Block(clock, "Clock Face", new Vector3(-1.1f, 2.25f, 4.38f), new Vector3(.9f, .9f, .12f), SleepwalkingUI.MainText);
        Block(clock, "Clock Hand", new Vector3(-1.1f, 2.4f, 4.28f), new Vector3(.08f, .42f, .1f), SleepwalkingUI.Accent);

        Transform shelf = new GameObject("Bookshelf - Credits").transform;
        shelf.SetParent(room);
        shelf.gameObject.AddComponent<SleepwalkingRoomTarget>().Action = RoomAction.Credits;
        Block(shelf, "Shelf", new Vector3(1.3f, 1.25f, 4.55f), new Vector3(1.65f, 2.7f, .65f), SleepwalkingUI.FrameColor);
        for (int q = 0; q < 5; q++)
            Block(shelf, "Book " + q, new Vector3(.78f + q * .24f, 1.1f, 4.13f), new Vector3(.18f, .8f + .08f * (q % 2), .35f), q % 2 == 0 ? SleepwalkingUI.Accent : SleepwalkingUI.SecondaryText);

        Transform door = new GameObject("Door - Quit").transform;
        door.SetParent(room);
        door.gameObject.AddComponent<SleepwalkingRoomTarget>().Action = RoomAction.Quit;
        Block(door, "Door Panel", new Vector3(4.25f, 1.25f, 4.55f), new Vector3(1.9f, 3.7f, .4f), SleepwalkingUI.Background);
        Block(door, "Door Handle", new Vector3(3.55f, 1.1f, 4.27f), new Vector3(.16f, .16f, .16f), SleepwalkingUI.Hover);
    }

    static GameObject Block(Transform parent, string name, Vector3 position, Vector3 size, Color color)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = name;
        block.transform.SetParent(parent);
        block.transform.position = position;
        block.transform.localScale = size;
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        Material material = new Material(shader);
        material.color = color;
        block.GetComponent<Renderer>().sharedMaterial = material;
        return block;
    }
}
