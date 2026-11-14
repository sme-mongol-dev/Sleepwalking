using UnityEngine;

namespace Sleepwalking.Visual
{
    public sealed class DreamPlayerAppearance : MonoBehaviour
    {
        public Sprite TemporarySprite;

        SpriteRenderer original;
        SpriteRenderer visual;

        void LateUpdate()
        {
            if (TemporarySprite == null) return;
            if (visual == null)
            {
                GameObject dreamer = GameObject.Find("Dreamer");
                if (dreamer == null) return;
                original = dreamer.GetComponent<SpriteRenderer>();
                if (original == null) return;
                GameObject child = new GameObject("Temporary Dreamer Silhouette", typeof(SpriteRenderer));
                child.transform.SetParent(dreamer.transform, false);
                child.transform.localPosition = new Vector3(0, 0, -.05f);
                child.transform.localScale = new Vector3(1.5f, 1, 1);
                visual = child.GetComponent<SpriteRenderer>();
                visual.sprite = TemporarySprite;
                visual.color = Color.white;
                visual.sortingOrder = original.sortingOrder;
                original.enabled = false;
            }
            visual.flipX = original.flipX;
        }
    }
}
