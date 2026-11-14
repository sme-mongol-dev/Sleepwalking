using System;
using UnityEngine;

namespace Sleepwalking
{
    public sealed partial class SleepwalkingGame
    {
        void BuildRoom()
        {
            if (RoomPrefab != null)
            {
                room = Instantiate(RoomPrefab);
                room.name = "Waking room";
                Camera prefabCamera = room.GetComponentInChildren<Camera>(true);
                if (prefabCamera != null)
                {
                    prefabCamera.transform.SetParent(transform, true);
                    GameCamera = prefabCamera;
                    roomCameraPosition = prefabCamera.transform.position;
                    roomCameraRotation = prefabCamera.transform.rotation;
                }
                return;
            }

            room = new GameObject("Waking room");
            RoomBlock("Back wall", new Vector3(0f, 1f, 4f), new Vector3(15f, 9f, .3f), Hex("222036"));
            RoomBlock("Floor", new Vector3(0f, -3.2f, 1f), new Vector3(15f, 1f, 5f), Hex("3C2030"));
            RoomBlock("Bed frame", new Vector3(-2f, -2.2f, 0f), new Vector3(3f, .5f, 1.4f), Hex("8C5050"));
            RoomBlock("Bed mattress", new Vector3(-2f, -1.8f, -.2f), new Vector3(2.8f, .35f, 1.3f), Hex("F8D4B8"));
            RoomBlock("Bed pillow", new Vector3(-3.05f, -1.52f, -.3f), new Vector3(.6f, .2f, .8f), Hex("B4B8D4"));
            RoomBlock("Hero", new Vector3(-1.5f, -1.25f, -.5f), new Vector3(.65f, .85f, .5f), Hex("F8D4B8"));
            RoomBlock("Clock", new Vector3(2f, 1.2f, 2f), new Vector3(1f, 1f, .2f), Hex("229EDE"));
            RoomBlock("Bookshelf", new Vector3(4f, -.5f, 2f), new Vector3(1.6f, 3.4f, .5f), Hex("6E1830"));
            RoomBlock("Door", new Vector3(-5.6f, -.5f, 2f), new Vector3(1.5f, 4.3f, .2f), Hex("8C5050"));
        }

        void RoomBlock(string name, Vector3 position, Vector3 size, Color color)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(room.transform);
            block.transform.position = position;
            block.transform.localScale = size;
            Renderer renderer = block.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            renderer.material.color = color;
        }

        void EnterRoom()
        {
            inDream = false;
            if (room != null) room.SetActive(true);
            GameCamera.orthographic = roomOrthographic;
            GameCamera.orthographicSize = roomOrthographicSize;
            GameCamera.backgroundColor = Hex("161318");
            Sleepwalking.Audio.SleepwalkingAudioDirector audio = Sleepwalking.Audio.SleepwalkingAudioDirector.CreateOrGet();
            if (audio != null) audio.EnterRoom();
            CallUi("EnterRoom");
        }

        void ApplyRoomChange()
        {
            foreach (MonoBehaviour component in room.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null || component.GetType().Name != "RoomEnvironment") continue;
                component.GetType().GetMethod("SetWakeCount")?.Invoke(component, new object[] { roomChange });
                return;
            }
            Transform clock = room.transform.Find("Clock");
            if (clock != null) clock.localRotation = Quaternion.Euler(0, 0, roomChange % 4 * 35f);
        }

        void BuildFallbackLevel(int index)
        {
            level = new GameObject("Dream L" + (index + 1).ToString("00") + " fallback");
            GameObject marker = new GameObject("PlayerStart");
            marker.transform.SetParent(level.transform);
            marker.transform.position = new Vector3(-3.8f, -.9f);
            start = marker.transform;

            Solid("Start floor", new Vector2(-3f, -1.25f), new Vector2(4f, .7f));
            Solid("Middle floor", new Vector2(2f, -1.25f), new Vector2(index >= 3 ? 2f : 3f, .7f));
            Solid("End floor", new Vector2(7.3f, -1.25f), new Vector2(5.4f, .7f));
            Solid("Upper ledge", new Vector2(index >= 4 ? 4.7f : 3.6f, 3.4f), new Vector2(3f, .45f));
            if (index >= 1) Solid("Wall jump", new Vector2(4.4f, .15f), new Vector2(.45f, 2.2f));
            if (index >= 2) Mechanism("Spikes", "Spikes", new Vector2(1.6f, -.58f), new Vector2(.65f, .24f), true, false);
            if (index >= 2) Mechanism("Saw", "Saw", new Vector2(6.3f, -.42f), new Vector2(.48f, .48f), true, false);
            if (index >= 3)
            {
                Mechanism("Laser", "LaserBase", new Vector2(5.1f, -.53f), new Vector2(.52f, .5f), true, false);
                Mechanism("Laser_Beam", "LaserBeam", new Vector2(5.1f, 1.25f), new Vector2(BeamWidth(), 3.1f), true, false);
                Mechanism("Laser button", "Button", new Vector2(2.5f, -.55f), new Vector2(.45f, .27f), true, false, "Laser", 3f);
            }
            if (index >= 4) Mechanism("Timed bridge", "TimedPlatform", new Vector2(4.8f, -.95f), new Vector2(1.5f, .3f), false, true);
            if (index >= 5)
            {
                SleepwalkingMechanism moving = Mechanism("Moving platform", "MovingPlatform", new Vector2(4.2f, .8f), new Vector2(1.3f, .3f), false, true);
                moving.MoveOffset = new Vector2(0, 1.4f);
                moving.CycleSeconds = 2.4f;
            }
            if (index >= 6) Mechanism("Saw button", "Button", new Vector2(-.1f, -.55f), new Vector2(.45f, .27f), true, false, "Saw", 2.5f);
            Mechanism("Bed exit", "Exit", new Vector2(9.1f, -.15f), new Vector2(.9f, 1.25f), true, false);
        }

        void Solid(string name, Vector2 position, Vector2 size)
        {
            Mechanism(name, "Solid", position, size, false, true);
        }

        SleepwalkingMechanism Mechanism(string name, string kind, Vector2 position, Vector2 size, bool trigger, bool solid,
            string linked = "", float seconds = 3f)
        {
            GameObject obj = new GameObject(name, typeof(SpriteRenderer));
            obj.transform.SetParent(level.transform);
            obj.transform.position = position;
            obj.transform.localScale = new Vector3(size.x, size.y, 1f);
            SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>();
            bool triangle = kind == "LaserBase" || kind == "Spikes";
            renderer.sprite = triangle ? TriangleSprite() : SquareSprite();
            renderer.color = kind == "Exit" ? Hex("F8D4B8") : kind == "Button" ? Hex("229EDE") :
                kind == "Solid" || solid ? Hex("8C5050") : Hex("B4202A");
            Collider2D collider = triangle ? obj.AddComponent<PolygonCollider2D>() : obj.AddComponent<BoxCollider2D>();
            if (triangle)
                ((PolygonCollider2D)collider).points = new[]
                {
                    new Vector2(-.5f, -.5f), new Vector2(.5f, -.5f), new Vector2(0f, .5f)
                };
            collider.isTrigger = trigger;
            collider.enabled = trigger || solid;
            SleepwalkingMechanism mechanism = obj.AddComponent<SleepwalkingMechanism>();
            mechanism.FeatureId = name;
            mechanism.Kind = kind;
            mechanism.LinkedFeatureId = linked;
            mechanism.ActiveSeconds = seconds;
            return mechanism;
        }

        static Sprite triangleSprite;
        static Sprite TriangleSprite()
        {
            if (triangleSprite != null) return triangleSprite;
            Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
                texture.SetPixel(x, y, Mathf.Abs(x - 7.5f) <= (15 - y) * .5f ? Color.white : Color.clear);
            texture.Apply();
            triangleSprite = Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(.5f, .5f), 16f);
            return triangleSprite;
        }
    }
}
