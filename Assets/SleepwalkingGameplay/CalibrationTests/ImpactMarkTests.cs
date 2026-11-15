using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Sleepwalking.Tests.PlayMode
{
    public sealed class ImpactMarkTests
    {
        [UnityTest]
        public IEnumerator WallAndThreeHazardsKeepSurfaceMarksUntilDreamExit()
        {
            AsyncOperation loading = SceneManager.LoadSceneAsync("Assets/Scenes/Sleepwalking.unity");
            while (!loading.isDone) yield return null;
            yield return null;

            GameObject root = GameObject.Find("Sleepwalking Game");
            MonoBehaviour game = null;
            foreach (MonoBehaviour component in root.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().Name == "SleepwalkingGame") game = component;
            Assert.That(game, Is.Not.Null);
            Type type = game.GetType();
            const BindingFlags privateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
            type.GetField("completedDreams", privateInstance).SetValue(game, 6);
            type.GetMethod("StartDream").Invoke(game, null);
            float deadline = Time.realtimeSinceStartup + 10f;
            while ((!(bool)type.GetProperty("InDream").GetValue(game) ||
                (bool)type.GetField("transitioning", privateInstance).GetValue(game)) &&
                Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That((bool)type.GetProperty("InDream").GetValue(game), Is.True);

            GameObject dreamer = GameObject.Find("Dreamer");
            MonoBehaviour player = null;
            foreach (MonoBehaviour component in dreamer.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().Name == "SleepwalkingPlayer") player = component;
            Assert.That(player, Is.Not.Null);
            MethodInfo reset = player.GetType().GetMethod("ResetAt");
            GameObject paint = (GameObject)type.GetField("paint", privateInstance).GetValue(game);
            Camera camera = (Camera)type.GetField("GameCamera").GetValue(game);
            Collider2D wall = GameObject.Find("Cave_LeftWall").GetComponent<Collider2D>();
            float unit = 32f;

            reset.Invoke(player, new object[] { new Vector2(wall.bounds.max.x + .35f * unit,
                wall.bounds.center.y) });
            dreamer.GetComponent<Rigidbody2D>().linearVelocity = Vector2.left * 6f * unit;
            deadline = Time.realtimeSinceStartup + 3f;
            while (paint.transform.childCount == 0 && Time.realtimeSinceStartup < deadline)
                yield return new WaitForFixedUpdate();
            Assert.That(paint.transform.childCount, Is.GreaterThan(0), "Wall collision left no mark.");
            Transform wallMark = paint.transform.GetChild(0);
            AssertMark(wallMark, wall);
            Vector3 wallStart = wall.transform.position;
            Vector3 markStart = wallMark.position;
            wall.transform.position += Vector3.right * (.25f * unit);
            yield return null;
            Assert.That(wallMark.position.x - markStart.x, Is.EqualTo(.25f * unit).Within(.05f),
                "Mark must follow the surface that received the impact.");
            wall.transform.position = wallStart;
            yield return null;
            Capture(camera, "impact-wall-L07");
            Capture(camera, "impact-wall-close-L07", wallMark);

            string[] hazards = { "L07_PitSpikes_1", "L07_Saw", "L07_Laser_Beam", "L07_Saw", "L07_Saw" };
            string[] frames = { "spikes", "saw", "laser", "repeat", "repeat2" };
            for (int q = 0; q < hazards.Length; q++)
            {
                Collider2D hazard = GameObject.Find(hazards[q]).GetComponent<Collider2D>();
                Assert.That(hazard, Is.Not.Null);
                int previous = paint.transform.childCount;
                reset.Invoke(player, new object[] { (Vector2)hazard.bounds.center });
                Physics2D.SyncTransforms();
                deadline = Time.realtimeSinceStartup + 3f;
                while (paint.transform.childCount == previous && Time.realtimeSinceStartup < deadline)
                    yield return new WaitForFixedUpdate();
                Assert.That(paint.transform.childCount, Is.GreaterThan(previous),
                    hazards[q] + " did not leave a mark on contact.");
                AssertMark(paint.transform.GetChild(paint.transform.childCount - 1), hazard);
                Capture(camera, "impact-" + frames[q] + "-L07");
                Capture(camera, "impact-" + frames[q] + "-close-L07",
                    paint.transform.GetChild(paint.transform.childCount - 1));
                deadline = Time.realtimeSinceStartup + 3f;
                while ((bool)type.GetField("dying", privateInstance).GetValue(game) &&
                    Time.realtimeSinceStartup < deadline) yield return null;
                Assert.That((bool)type.GetField("dying", privateInstance).GetValue(game), Is.False);
                Capture(camera, "impact-" + frames[q] + "-settled-close-L07",
                    paint.transform.GetChild(paint.transform.childCount - 1));
            }

            int total = paint.transform.childCount;
            type.GetMethod("RestartDream").Invoke(game, null);
            yield return null;
            Assert.That(paint.transform.childCount, Is.EqualTo(total));
            type.GetMethod("ReturnToRoom").Invoke(game, null);
            yield return null;
            Assert.That(paint == null, Is.True, "Leaving the dream must destroy its marks.");
            Assert.That(type.GetField("paint", privateInstance).GetValue(game), Is.Null);
            type.GetMethod("StartDream").Invoke(game, null);
            deadline = Time.realtimeSinceStartup + 10f;
            while ((!(bool)type.GetProperty("InDream").GetValue(game) ||
                (bool)type.GetField("transitioning", privateInstance).GetValue(game)) &&
                Time.realtimeSinceStartup < deadline) yield return null;
            GameObject newPaint = (GameObject)type.GetField("paint", privateInstance).GetValue(game);
            Assert.That(newPaint, Is.Not.Null);
            Assert.That(newPaint.transform.childCount, Is.Zero,
                "A new entrance must start without marks from the previous dream.");
        }

        static void AssertMark(Transform mark, Collider2D surface)
        {
            MonoBehaviour attachment = null;
            foreach (MonoBehaviour component in mark.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().Name == "SleepwalkingSurfaceMark") attachment = component;
            Assert.That(attachment, Is.Not.Null);
            Assert.That(attachment.GetType().GetField("surface", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(attachment), Is.EqualTo(surface.transform));
            Sprite sprite = mark.GetComponent<SpriteRenderer>().sprite;
            Assert.That(sprite, Is.Not.Null);
            Assert.That(sprite.texture.width, Is.GreaterThan(1));
            Assert.That(sprite.texture.GetPixel(0, 0).a, Is.Zero);
            Assert.That(sprite.texture.GetPixel(12, 12).a, Is.GreaterThan(.5f));
            Assert.That(Vector2.Distance(mark.position, surface.ClosestPoint(mark.position)),
                Is.LessThan(.1f), "Mark center must sit on the touched collider.");
            int visible = 0;
            for (int y = 0; y < sprite.texture.height; y++)
            for (int x = 0; x < sprite.texture.width; x++)
            {
                if (sprite.texture.GetPixel(x, y).a <= .5f) continue;
                visible++;
                Vector3 local = new Vector3((x + .5f) / sprite.texture.width - .5f,
                    (y + .5f) / sprite.texture.height - .5f, 0f);
                Assert.That(surface.OverlapPoint(mark.TransformPoint(local)), Is.True,
                    "An opaque paint pixel hangs outside the touched collider.");
            }
            Assert.That(visible, Is.GreaterThan(10));
        }

        static void Capture(Camera camera, string name, Transform focus = null)
        {
            RenderTexture oldTarget = camera.targetTexture;
            Vector3 oldPosition = camera.transform.position;
            float oldSize = camera.orthographicSize;
            RenderTexture target = new RenderTexture(960, 540, 24);
            Texture2D image = new Texture2D(960, 540, TextureFormat.RGBA32, false);
            RenderTexture oldActive = RenderTexture.active;
            try
            {
                if (focus != null)
                {
                    camera.transform.position = new Vector3(focus.position.x, focus.position.y, oldPosition.z);
                    camera.orthographicSize = 64f;
                }
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
                image.Apply();
                string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                File.WriteAllBytes(Path.Combine(project, "Docs", "QA", name + ".png"), image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = oldTarget;
                camera.transform.position = oldPosition;
                camera.orthographicSize = oldSize;
                RenderTexture.active = oldActive;
                target.Release();
                UnityEngine.Object.Destroy(target);
                UnityEngine.Object.Destroy(image);
            }
        }
    }
}
