using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Sleepwalking.Visual.Editor
{
    public static class RoomGrowthMigration
    {
        [MenuItem("Sleepwalking/Visual/Integrate Room Growth")]
        public static void Integrate()
        {
            string path = VisualAssetBuilder.MenuRoomPrefab;
            GameObject room = PrefabUtility.LoadPrefabContents(path);
            try
            {
                RoomEnvironment environment = room.GetComponent<RoomEnvironment>();
                Transform eye = room.transform.Find("Eye");
                Transform pupil = room.transform.Find("Eyeball") ?? eye?.Find("Eyeball");
                Transform crack4 = room.transform.Find("Crack 4");
                Transform crack5 = room.transform.Find("Crack 5");
                if (environment == null || eye == null || pupil == null || crack4 == null || crack5 == null)
                    throw new InvalidOperationException("The saved room must contain Eye, Eyeball, Crack 4 and Crack 5.");
                RoomOdradekDecoration eyeRow = environment.Decorations.Find(row => row != null && row.Id == "eye");
                if (eyeRow == null) throw new InvalidOperationException("The saved room must contain the eye row.");
                if (eyeRow.Group == "crack.4" && eyeRow.Mode == RoomDecorationMode.Variant) return;

                // Зафиксировать расстановку из prefab до изменения связей Odradek.
                foreach (RoomOdradekDecoration row in environment.Decorations)
                {
                    if (row?.RoomObject == null) continue;
                    Transform item = row.RoomObject.transform;
                    row.LocalPosition = item.localPosition;
                    row.LocalEulerAngles = item.localEulerAngles;
                    row.LocalScale = item.localScale;
                }

                if (pupil.parent != eye) pupil.SetParent(eye, true);
                RoomEyeTracker tracker = eye.GetComponent<RoomEyeTracker>();
                if (tracker == null) tracker = eye.gameObject.AddComponent<RoomEyeTracker>();
                tracker.Pupil = pupil;

                RoomOdradekDecoration old4 = environment.Decorations.Find(row => row != null && row.Id == "crack.4");
                RoomOdradekDecoration old5 = environment.Decorations.Find(row => row != null && row.Id == "crack.5");
                var retained = new List<RoomOdradekDecoration>();
                foreach (RoomOdradekDecoration row in environment.Decorations)
                {
                    if (row == null || row.Id == "eyeball" || row.Id == "crack.1" ||
                        row.Id == "crack.2" || row.Id == "crack.3" || row.Id == "crack.4" ||
                        row.Id == "crack.5" || row.Id != null &&
                        (row.Id.StartsWith("crack.4.stage") || row.Id.StartsWith("crack.5.stage"))) continue;
                    retained.Add(row);
                }

                AddStages(retained, crack4, old4, 4, new[] { 80, 85, 90 }, 94);
                AddStages(retained, crack5, old5, 5, new[] { 95, 97, 99 }, 100);
                eye.localPosition = new Vector3(crack4.localPosition.x, crack4.localPosition.y,
                    eye.localPosition.z);
                eyeRow.Group = "crack.4";
                eyeRow.Mode = RoomDecorationMode.Variant;
                eyeRow.MinOdradek = 95;
                eyeRow.MaxOdradek = 100;
                eyeRow.LocalPosition = eye.localPosition;
                environment.Decorations = retained;

                Texture2D texture4 = AssetDatabase.LoadAssetAtPath<Texture2D>(
                    "Assets/SleepwalkingVisual/Textures/Source/MenuDecor/Crack4.png");
                Texture2D texture5 = AssetDatabase.LoadAssetAtPath<Texture2D>(
                    "Assets/SleepwalkingVisual/Textures/Source/MenuDecor/Crack5.png");
                Material material4 = AssetDatabase.LoadAssetAtPath<Material>(
                    "Assets/SleepwalkingVisual/Materials/MenuDecor_Crack4.mat");
                Material material5 = AssetDatabase.LoadAssetAtPath<Material>(
                    "Assets/SleepwalkingVisual/Materials/MenuDecor_Crack5.mat");
                if (texture4 == null || texture5 == null || material4 == null || material5 == null)
                    throw new InvalidOperationException("Original dark crack textures or materials are missing.");
                material4.SetTexture("_MainTex", texture4);
                material5.SetTexture("_MainTex", texture5);
                EditorUtility.SetDirty(material4);
                EditorUtility.SetDirty(material5);

                PrefabUtility.SaveAsPrefabAsset(room, path);
                AssetDatabase.SaveAssets();
            }
            finally { PrefabUtility.UnloadPrefabContents(room); }
        }

        static void AddStages(List<RoomOdradekDecoration> rows, Transform crack,
            RoomOdradekDecoration previous, int number, int[] minimums, int lastMaximum)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/SleepwalkingVisual/Textures/Source/MenuDecor/Crack" + number + ".png");
            float[] scales = { .3f, .6f, 1f };
            Vector3 baseScale = crack.localScale;
            for (int q = 0; q < 3; q++)
            {
                rows.Add(new RoomOdradekDecoration
                {
                    Name = "Crack " + number + " Stage " + (q + 1),
                    Id = "crack." + number + ".stage" + (q + 1),
                    Group = "crack." + number,
                    MinOdradek = minimums[q],
                    MaxOdradek = q == 2 ? lastMaximum : minimums[q + 1] - 1,
                    Asset = "crack." + number,
                    Texture = texture,
                    Mode = RoomDecorationMode.Variant,
                    RoomObject = crack.gameObject,
                    LocalPosition = crack.localPosition,
                    LocalEulerAngles = crack.localEulerAngles,
                    LocalScale = baseScale * scales[q],
                    SortingOrder = previous?.SortingOrder ?? 0
                });
            }
        }
    }
}
