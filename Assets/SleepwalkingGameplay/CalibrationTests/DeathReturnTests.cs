using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Sleepwalking.Tests.PlayMode
{
    public sealed class DeathReturnTests
    {
        [UnityTest]
        public IEnumerator DashAndDeathFramesPlayBeforeSmoothProtectedReturn()
        {
            AsyncOperation loading = SceneManager.LoadSceneAsync("Assets/Scenes/Sleepwalking.unity");
            while (!loading.isDone) yield return null;
            yield return null;

            GameObject root = GameObject.Find("Sleepwalking Game");
            Assert.That(root, Is.Not.Null);
            MonoBehaviour game = null;
            foreach (MonoBehaviour component in root.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().Name == "SleepwalkingGame") game = component;
            Assert.That(game, Is.Not.Null);
            Type gameType = game.GetType();
            const BindingFlags privateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
            gameType.GetField("completedDreams", privateInstance).SetValue(game, 6);
            gameType.GetMethod("StartDream").Invoke(game, null);
            float deadline = Time.realtimeSinceStartup + 10f;
            while ((!(bool)gameType.GetProperty("InDream").GetValue(game) ||
                (bool)gameType.GetField("transitioning", privateInstance).GetValue(game)) &&
                Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That((bool)gameType.GetProperty("InDream").GetValue(game), Is.True);

            GameObject dreamer = GameObject.Find("Dreamer");
            Assert.That(dreamer, Is.Not.Null);
            MonoBehaviour player = null;
            foreach (MonoBehaviour component in dreamer.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().Name == "SleepwalkingPlayer") player = component;
            Assert.That(player, Is.Not.Null);
            Type playerType = player.GetType();
            MethodInfo reset = playerType.GetMethod("ResetAt");
            MethodInfo input = playerType.GetMethod("SetSimulatedInput");
            MethodInfo update = playerType.GetMethod("Update", privateInstance);
            Rigidbody2D body = dreamer.GetComponent<Rigidbody2D>();
            Camera camera = (Camera)gameType.GetField("GameCamera").GetValue(game);
            RenderTexture target = new RenderTexture(960, 540, 24);
            target.Create();
            camera.targetTexture = target;
            yield return null;
            SpriteRenderer animated = dreamer.transform.Find("Dreamer Animation")?.GetComponent<SpriteRenderer>();
            Assert.That(animated, Is.Not.Null, "Production dream theme did not attach character animation.");

            input.Invoke(player, new object[] { 1f, false, true });
            update.Invoke(player, null);
            yield return null;
            Assert.That((bool)playerType.GetProperty("IsDashing").GetValue(player), Is.True);
            Assert.That(animated.sprite.name, Does.Contain("S_Pizduk_Dash_"));
            Capture(camera, "dash-animation-playmode");

            Transform start = GameObject.Find("PlayerStart").transform;
            float unit = (float)playerType.GetField("WorldUnitsPerCell").GetValue(player);
            Vector3 spawn = start.position + Vector3.up * (.5f * unit);
            reset.Invoke(player, new object[] { (Vector2)spawn });
            GameObject hazard = GameObject.Find("L07_SecondLaser_Beam");
            Assert.That(hazard, Is.Not.Null);
            Collider2D hazardShape = hazard.GetComponent<Collider2D>();
            Assert.That(hazardShape.enabled, Is.True);
            body.simulated = false;
            dreamer.transform.position = new Vector3(hazardShape.bounds.center.x,
                hazardShape.bounds.min.y + .35f * unit, dreamer.transform.position.z);
            Physics2D.SyncTransforms();
            yield return new WaitForSeconds(.7f);
            float farCameraX = camera.transform.position.x;
            Assert.That(farCameraX, Is.GreaterThan(spawn.x + unit));

            body.simulated = true;
            Physics2D.SyncTransforms();
            deadline = Time.realtimeSinceStartup + 2f;
            while ((int)gameType.GetProperty("DreamDeaths").GetValue(game) == 0 &&
                Time.realtimeSinceStartup < deadline) yield return new WaitForFixedUpdate();
            Assert.That((int)gameType.GetProperty("DreamDeaths").GetValue(game), Is.EqualTo(1),
                "Physical laser contact did not start the death sequence.");
            Vector3 impactPosition = dreamer.transform.position;
            Assert.That((bool)playerType.GetProperty("IsImpact").GetValue(player), Is.True);
            Assert.That((bool)playerType.GetField("ControlsEnabled").GetValue(player), Is.False);
            Assert.That(body.simulated, Is.False);
            float deathStarted = Time.realtimeSinceStartup;
            List<string> timeline = new List<string> { "seconds,playerX,cameraX" };
            while (Time.realtimeSinceStartup - deathStarted < .38f)
            {
                Record(timeline, deathStarted, dreamer, camera);
                yield return null;
            }
            Assert.That(Vector3.Distance(dreamer.transform.position, impactPosition), Is.LessThan(.02f * unit),
                "Player moved before the death animation finished.");
            Assert.That(animated.sprite.name, Does.Contain("S_Pizduk_Death_"));
            Capture(camera, "death-animation-playmode");

            while (Time.realtimeSinceStartup - deathStarted < .68f)
            {
                Record(timeline, deathStarted, dreamer, camera);
                yield return null;
            }
            Assert.That(animated.sprite.name, Does.EndWith("S_Pizduk_Death_8"),
                "The full nine-frame death sequence did not finish before the return.");
            Assert.That(Vector3.Distance(dreamer.transform.position, impactPosition), Is.LessThan(.02f * unit));
            Capture(camera, "death-animation-final-playmode");

            while (Time.realtimeSinceStartup - deathStarted < .93f)
            {
                Record(timeline, deathStarted, dreamer, camera);
                yield return null;
            }
            Assert.That((bool)gameType.GetField("dying", privateInstance).GetValue(game), Is.True);
            Assert.That((bool)playerType.GetProperty("IsReturning").GetValue(player), Is.True);
            Assert.That((bool)playerType.GetField("ControlsEnabled").GetValue(player), Is.False);
            Assert.That(body.simulated, Is.False);
            Assert.That(animated.sprite.name, Does.EndWith("S_Pizduk_Death_8"),
                "The last death frame must remain visible during the return.");
            Assert.That(dreamer.transform.position.x, Is.LessThan(impactPosition.x - .2f * unit));
            Assert.That(dreamer.transform.position.x, Is.GreaterThan(spawn.x + .2f * unit));
            Assert.That(camera.transform.position.x, Is.LessThan(farCameraX - .2f * unit));
            gameType.GetMethod("KillPlayer").Invoke(game, new object[] { (Vector2)impactPosition });
            Assert.That((int)gameType.GetProperty("DreamDeaths").GetValue(game), Is.EqualTo(1),
                "A second death was counted during the return.");
            Capture(camera, "death-return-playmode");

            bool sawReverseAtSpawn = false;
            deadline = Time.realtimeSinceStartup + 3f;
            while ((bool)gameType.GetField("dying", privateInstance).GetValue(game) &&
                Time.realtimeSinceStartup < deadline)
            {
                if (Vector3.Distance(dreamer.transform.position, spawn) < .15f * unit &&
                    animated.sprite.name.EndsWith("S_Pizduk_Death_7")) sawReverseAtSpawn = true;
                Record(timeline, deathStarted, dreamer, camera);
                yield return null;
            }
            Assert.That(sawReverseAtSpawn, Is.True, "Death must rewind only after arrival at spawn.");
            Record(timeline, deathStarted, dreamer, camera);
            string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            File.WriteAllLines(Path.Combine(project, "Docs", "QA", "death-return-timeline.csv"), timeline);
            yield return null;
            Assert.That((bool)gameType.GetField("dying", privateInstance).GetValue(game), Is.False);
            Assert.That(Vector3.Distance(dreamer.transform.position, spawn), Is.LessThan(.15f * unit),
                $"Spawn {spawn}, current {dreamer.transform.position} after physics resumed.");
            Assert.That((bool)playerType.GetProperty("IsImpact").GetValue(player), Is.False);
            Assert.That((bool)playerType.GetProperty("IsReturning").GetValue(player), Is.False);
            Assert.That((bool)playerType.GetField("ControlsEnabled").GetValue(player), Is.True);
            Assert.That(body.simulated, Is.True);
            GameObject paint = (GameObject)gameType.GetField("paint", privateInstance).GetValue(game);
            Assert.That(paint.transform.childCount, Is.GreaterThan(0), "Retry erased the impact mark.");
            Assert.That(camera.transform.position.x, Is.LessThan(farCameraX - unit));
            Assert.That(animated.sprite.name, Does.Not.Contain("S_Pizduk_Death_"));

            camera.targetTexture = null;
            target.Release();
            UnityEngine.Object.Destroy(target);
        }

        static void Record(List<string> timeline, float started, GameObject dreamer, Camera camera)
        {
            timeline.Add(string.Format(CultureInfo.InvariantCulture, "{0:F3},{1:F3},{2:F3}",
                Time.realtimeSinceStartup - started, dreamer.transform.position.x, camera.transform.position.x));
        }

        static void Capture(Camera camera, string name)
        {
            RenderTexture target = camera.targetTexture;
            RenderTexture previous = RenderTexture.active;
            Texture2D image = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            try
            {
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                image.Apply();
                string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                File.WriteAllBytes(Path.Combine(project, "Docs", "QA", name + ".png"), image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                UnityEngine.Object.Destroy(image);
            }
        }
    }
}
