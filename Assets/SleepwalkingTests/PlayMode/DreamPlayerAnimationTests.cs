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
    public sealed class DreamPlayerAnimationTests
    {
        [UnityTest]
        public IEnumerator DreamerSwitchesIdleRunJumpClimbAndFacing()
        {
            AsyncOperation loading = SceneManager.LoadSceneAsync("Assets/Scenes/Sleepwalking.unity");
            Assert.That(loading, Is.Not.Null);
            while (!loading.isDone) yield return null;
            yield return null;

            GameObject gameObject = GameObject.Find("Sleepwalking Game");
            Assert.That(gameObject, Is.Not.Null);
            MonoBehaviour game = FindComponent(gameObject, "Sleepwalking.SleepwalkingGame");
            Assert.That(game, Is.Not.Null);
            Type gameType = game.GetType();
            gameType.GetField("completedDreams", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(game, 0);
            gameType.GetMethod("StartDream").Invoke(game, null);
            PropertyInfo inDream = gameType.GetProperty("InDream");
            FieldInfo transitioning = gameType.GetField("transitioning",
                BindingFlags.Instance | BindingFlags.NonPublic);
            float deadline = Time.realtimeSinceStartup + 10f;
            while ((!((bool)inDream.GetValue(game)) || (bool)transitioning.GetValue(game)) &&
                Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That((bool)inDream.GetValue(game), Is.True);

            GameObject dreamer = GameObject.Find("Dreamer");
            Assert.That(dreamer, Is.Not.Null);
            MonoBehaviour player = FindComponent(dreamer, "Sleepwalking.SleepwalkingPlayer");
            Rigidbody2D body = dreamer.GetComponent<Rigidbody2D>();
            BoxCollider2D shape = dreamer.GetComponent<BoxCollider2D>();
            SpriteRenderer original = dreamer.GetComponent<SpriteRenderer>();
            Assert.That(player, Is.Not.Null);
            Assert.That(body, Is.Not.Null);
            Assert.That(shape, Is.Not.Null);
            MethodInfo input = player.GetType().GetMethod("SetSimulatedInput");
            MethodInfo reset = player.GetType().GetMethod("ResetAt");
            Assert.That(input, Is.Not.Null);
            Assert.That(reset, Is.Not.Null);

            for (int q = 0; q < 6; q++) yield return new WaitForFixedUpdate();
            yield return null;
            SpriteRenderer art = dreamer.transform.Find("Dreamer Animation")?.GetComponent<SpriteRenderer>();
            Assert.That(art, Is.Not.Null);
            Assert.That(original.enabled, Is.False);
            Assert.That(art.sprite.name, Does.StartWith("S_PizdukIdle_"));
            Assert.That(art.flipX, Is.False);
            CheckFrameSheets(art);
            Vector2 startPosition = body.position;
            Capture((Camera)gameType.GetField("GameCamera").GetValue(game), art, "animation-idle");
            string idleFrame = art.sprite.name;
            yield return new WaitForSeconds(.15f);
            yield return null;
            Assert.That(art.sprite.name, Does.StartWith("S_PizdukIdle_"));
            Assert.That(art.sprite.name, Is.Not.EqualTo(idleFrame));

            input.Invoke(player, new object[] { 1f, false, false });
            for (int q = 0; q < 9; q++) yield return new WaitForFixedUpdate();
            yield return null;
            Assert.That(art.sprite.name, Does.StartWith("S_PizdukRun_"));
            Assert.That(art.flipX, Is.False);
            Capture((Camera)gameType.GetField("GameCamera").GetValue(game), art, "animation-run-right");

            input.Invoke(player, new object[] { -1f, false, false });
            for (int q = 0; q < 14; q++) yield return new WaitForFixedUpdate();
            yield return null;
            Assert.That(art.sprite.name, Does.StartWith("S_PizdukRun_"));
            Assert.That(art.flipX, Is.True);
            Capture((Camera)gameType.GetField("GameCamera").GetValue(game), art, "animation-run-left");

            reset.Invoke(player, new object[] { startPosition });
            for (int q = 0; q < 3; q++) yield return new WaitForFixedUpdate();
            input.Invoke(player, new object[] { -1f, true, false });
            for (int q = 0; q < 3; q++) yield return new WaitForFixedUpdate();
            yield return null;
            Assert.That(art.sprite.name, Does.StartWith("S_Pizduk_Jump_"));
            Assert.That(art.flipX, Is.True);
            Capture((Camera)gameType.GetField("GameCamera").GetValue(game), art, "animation-jump");
            string jumpFrame = art.sprite.name;
            yield return new WaitForSeconds(.12f);
            yield return null;
            Assert.That(art.sprite.name, Does.StartWith("S_Pizduk_Jump_"));
            Assert.That(art.sprite.name, Is.Not.EqualTo(jumpFrame));

            Collider2D wall = GameObject.Find("Cave_RightWall")?.GetComponent<Collider2D>();
            Assert.That(wall, Is.Not.Null);
            float unit = (float)player.GetType().GetField("WorldUnitsPerCell").GetValue(player);
            Vector2 atWall = new Vector2(wall.bounds.min.x - shape.bounds.extents.x - .02f * unit,
                Mathf.Min(wall.bounds.max.y - shape.bounds.extents.y - .5f * unit,
                    wall.bounds.center.y + 2f * unit));
            reset.Invoke(player, new object[] { atWall });
            body.linearVelocity = new Vector2(0f, -2f * unit);
            input.Invoke(player, new object[] { 1f, false, false });
            for (int q = 0; q < 3; q++) yield return new WaitForFixedUpdate();
            yield return null;
            Capture((Camera)gameType.GetField("GameCamera").GetValue(game), art, "animation-climb");
            Assert.That(art.sprite.name, Does.StartWith("S_Pizduk_Climb_"),
                "Player=" + body.position + " Velocity=" + body.linearVelocity +
                " Grounded=" + player.GetType().GetProperty("IsGrounded").GetValue(player) +
                " ControlsEnabled=" + player.GetType().GetField("ControlsEnabled").GetValue(player) +
                " Wall=" + wall.bounds);
            Assert.That(art.flipX, Is.False);
            string climbFrame = art.sprite.name;
            yield return new WaitForSeconds(.12f);
            yield return null;
            Assert.That(art.sprite.name, Does.StartWith("S_Pizduk_Climb_"));
            Assert.That(art.sprite.name, Is.Not.EqualTo(climbFrame));
            Capture((Camera)gameType.GetField("GameCamera").GetValue(game), art, "animation-climb-next");
            LogAssert.NoUnexpectedReceived();
        }

        static MonoBehaviour FindComponent(GameObject owner, string fullName)
        {
            foreach (MonoBehaviour component in owner.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().FullName == fullName) return component;
            return null;
        }

        static void CheckFrameSheets(SpriteRenderer art)
        {
            MonoBehaviour appearance = null;
            foreach (MonoBehaviour component in GameObject.Find("Dream style").GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().Name == "DreamPlayerAppearance")
                    appearance = component;
            Assert.That(appearance, Is.Not.Null);
            Sprite[][] sheets = (Sprite[][])appearance.GetType().GetField("frames",
                BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Assert.That(sheets.Length, Is.EqualTo(4));
            foreach (Sprite[] sheet in sheets)
            {
                Assert.That(sheet, Is.Not.Null);
                Assert.That(sheet.Length, Is.EqualTo(9));
                for (int q = 0; q < sheet.Length; q++)
                {
                    Assert.That(sheet[q].rect.x, Is.EqualTo(q * 64f));
                    Assert.That(sheet[q].rect.width, Is.EqualTo(64f));
                    Assert.That(sheet[q].rect.height, Is.EqualTo(64f));
                }
            }
            Assert.That(art.transform.localScale.x, Is.EqualTo(1.7f).Within(.001f));
        }

        static void Capture(Camera camera, SpriteRenderer art, string name)
        {
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture target = new RenderTexture(960, 540, 24);
            Texture2D image = new Texture2D(960, 540, TextureFormat.RGBA32, false);
            try
            {
                target.Create();
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
                image.Apply();
                string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                string output = Path.Combine(root, "Docs", "QA", name + ".png");
                File.WriteAllBytes(output, image.EncodeToPNG());
                Vector3 viewport = camera.WorldToViewportPoint(art.transform.position);
                File.WriteAllText(Path.ChangeExtension(output, ".txt"),
                    art.sprite.name + " FlipX=" + art.flipX + " Bounds=" + art.bounds +
                    " Camera=" + camera.transform.position + " Player=" + art.transform.position +
                    " PlayerViewport=" + viewport);
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                target.Release();
                UnityEngine.Object.Destroy(target);
                UnityEngine.Object.Destroy(image);
            }
        }
    }
}
