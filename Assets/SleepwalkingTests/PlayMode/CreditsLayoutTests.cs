using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Sleepwalking.Tests.PlayMode
{
    public sealed class CreditsLayoutTests
    {
        [UnityTest]
        public IEnumerator FourLinksFitAboveBackAndRemainClickable()
        {
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/Sleepwalking.unity");
            yield return null;

            GameObject uiObject = GameObject.Find("Sleepwalking UI");
            Assert.That(uiObject, Is.Not.Null);
            MonoBehaviour ui = null;
            foreach (MonoBehaviour component in uiObject.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().Name == "SleepwalkingUI") ui = component;
            Assert.That(ui, Is.Not.Null);

            Transform canvas = uiObject.transform.Find("Sleepwalking Canvas");
            RectTransform credits = canvas.Find("Credits") as RectTransform;
            RectTransform panel = credits.Find("Credits Panel") as RectTransform;
            RectTransform title = panel.Find("CREDITS Title") as RectTransform;
            RectTransform developers = panel.Find("Developers Label") as RectTransform;
            RectTransform back = panel.Find("BACK Button") as RectTransform;
            Assert.That(panel, Is.Not.Null);
            Assert.That(title, Is.Not.Null);
            Assert.That(developers, Is.Not.Null);
            Assert.That(back, Is.Not.Null);

            Assert.That(panel.rect.height, Is.EqualTo(560).Within(.1f));
            Assert.That(Bounds(title).yMax, Is.LessThan(panel.rect.yMax - 16));
            Assert.That(Bounds(title).yMin, Is.GreaterThan(Bounds(developers).yMax + 10));

            string[] names = { "Timbuhtuk", "Mongol-dev", "Raccoon-Reed", "StuarTTdesa" };
            Rect last = Bounds(developers);
            foreach (string name in names)
            {
                RectTransform link = panel.Find(name + " Link") as RectTransform;
                Assert.That(link, Is.Not.Null, name);
                Rect rect = Bounds(link);
                Assert.That(rect.xMin, Is.GreaterThan(panel.rect.xMin + 12), name);
                Assert.That(rect.xMax, Is.LessThan(panel.rect.xMax - 12), name);
                Assert.That(rect.yMax, Is.LessThanOrEqualTo(last.yMin - 8), name + " spacing");
                Assert.That(rect.yMin, Is.GreaterThan(panel.rect.yMin + 12), name);
                last = rect;
            }
            Assert.That(last.yMin, Is.GreaterThan(Bounds(back).yMax + 24),
                "The fourth name must not touch BACK.");
            Assert.That(Bounds(back).yMin, Is.GreaterThan(panel.rect.yMin + 12));

            MethodInfo fit = ui.GetType().GetMethod("Fit", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(fit, Is.Not.Null);
            foreach (Vector2 size in new[] { new Vector2(1920, 1080), new Vector2(1024, 768),
                new Vector2(800, 600) })
            {
                fit.Invoke(null, new object[] { credits.gameObject, new Rect(0, 0, size.x, size.y), 800f, 620f });
                float scale = credits.localScale.x;
                Assert.That(panel.rect.width * scale, Is.LessThan(size.x - 16), size + " width");
                Assert.That(panel.rect.height * scale, Is.LessThan(size.y - 16), size + " height");
            }
            fit.Invoke(null, new object[] { credits.gameObject,
                ((RectTransform)canvas).rect, 800f, 620f });

            MethodInfo handle = ui.GetType().GetMethod("Handle");
            Type command = handle.GetParameters()[0].ParameterType;
            handle.Invoke(ui, new[] { Enum.Parse(command, "Credits") });
            Assert.That(credits.gameObject.activeSelf, Is.True);
            Canvas.ForceUpdateCanvases();
            Assert.That(EventSystem.current, Is.Not.Null);
            yield return null;

            foreach (string name in names)
            {
                Text link = panel.Find(name + " Link").GetComponent<Text>();
                Button button = link.GetComponent<Button>();
                Assert.That(link.text, Is.EqualTo(name));
                Assert.That(link.raycastTarget && button.interactable, Is.True, name);
                Assert.That(link.gameObject.activeInHierarchy, Is.True, name);
            }

            var click = new PointerEventData(EventSystem.current);
            click.button = PointerEventData.InputButton.Left;
            ExecuteEvents.Execute<IPointerClickHandler>(back.gameObject, click, ExecuteEvents.pointerClickHandler);
            Assert.That(credits.gameObject.activeSelf, Is.False, "BACK must close Credits.");
        }

        static Rect Bounds(RectTransform rect)
        {
            Vector2 center = rect.anchoredPosition;
            Vector2 size = rect.sizeDelta;
            return new Rect(center.x - size.x * .5f, center.y - size.y * .5f, size.x, size.y);
        }
    }
}
