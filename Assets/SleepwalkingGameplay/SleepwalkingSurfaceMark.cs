using UnityEngine;

namespace Sleepwalking
{
    public sealed class SleepwalkingSurfaceMark : MonoBehaviour
    {
        Transform surface;
        Vector3 localPosition;
        Quaternion localRotation;
        Sprite sprite;

        public void Attach(Transform target, Sprite ownedSprite)
        {
            surface = target;
            sprite = ownedSprite;
            localPosition = surface.InverseTransformPoint(transform.position);
            localRotation = Quaternion.Inverse(surface.rotation) * transform.rotation;
        }

        void OnDestroy()
        {
            if (sprite == null) return;
            Destroy(sprite.texture);
            Destroy(sprite);
        }

        void LateUpdate()
        {
            if (surface == null) return;
            transform.position = surface.TransformPoint(localPosition);
            transform.rotation = surface.rotation * localRotation;
        }
    }
}
