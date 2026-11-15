using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Sleepwalking.Tests.PlayMode
{
    public sealed class RoomDecorExternalJsonTests
    {
        [UnityTest]
        public IEnumerator ExternalJsonOverridesBundledThresholdOnRoomCreation()
        {
            string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string external = Path.Combine(project, "SleepwalkingMenuDecor.json");
            Assert.That(File.Exists(external), Is.False, "An existing external room configuration must be preserved.");

            try
            {
                File.WriteAllText(external,
                    "{\"version\":1,\"decorations\":[{\"id\":\"crack.4.stage1\",\"group\":\"crack.4\",\"min\":81,\"max\":84,\"asset\":\"crack.4\"}]}");

                AsyncOperation loading = SceneManager.LoadSceneAsync("Assets/Scenes/Sleepwalking.unity");
                Assert.That(loading, Is.Not.Null);
                while (!loading.isDone) yield return null;
                yield return null;

                GameObject room = GameObject.Find("Waking room");
                GameObject gameObject = GameObject.Find("Sleepwalking Game");
                Assert.That(room, Is.Not.Null);
                Assert.That(gameObject, Is.Not.Null);

                MonoBehaviour environment = FindComponent(room, "Sleepwalking.Visual.RoomEnvironment");
                MonoBehaviour game = FindComponent(gameObject, "Sleepwalking.SleepwalkingGame");
                Assert.That(environment, Is.Not.Null);
                Assert.That(game, Is.Not.Null);

                IList rows = (IList)environment.GetType().GetField("Decorations").GetValue(environment);
                object crack = null;
                foreach (object row in rows)
                    if ((string)row.GetType().GetField("Id").GetValue(row) == "crack.4.stage1") crack = row;
                Assert.That(crack, Is.Not.Null);
                Assert.That((int)crack.GetType().GetField("MinOdradek").GetValue(crack), Is.EqualTo(81));

                FieldInfo odradek = game.GetType().GetField("odradek", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(odradek, Is.Not.Null);
                odradek.SetValue(game, 50);
                yield return null;
                GameObject instance = (GameObject)crack.GetType().GetField("Instance").GetValue(crack);
                Assert.That(instance.activeSelf, Is.False, "The external file must override the bundled threshold.");
                odradek.SetValue(game, 81);
                yield return null;
                Assert.That(instance.activeSelf, Is.True);
            }
            finally
            {
                File.Delete(external);
            }
        }

        static MonoBehaviour FindComponent(GameObject root, string name)
        {
            foreach (MonoBehaviour component in root.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().FullName == name) return component;
            return null;
        }
    }
}
