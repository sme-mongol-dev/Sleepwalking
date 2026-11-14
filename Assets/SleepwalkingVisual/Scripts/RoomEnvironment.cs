using UnityEngine;

namespace Sleepwalking.Visual
{
    public sealed class RoomEnvironment : MonoBehaviour
    {
        public Transform ClockHand;
        public Transform Picture;
        public Transform LooseBook;
        public Transform Curtain;
        public GameObject ExtraStar;

        Quaternion clockStart;
        Quaternion pictureStart;
        Quaternion bookStart;
        Vector3 curtainStart;
        bool captured;

        void Awake()
        {
            CaptureInitialState();
            SetWakeCount(0);
        }

        void CaptureInitialState()
        {
            if (captured) return;
            if (ClockHand != null) clockStart = ClockHand.localRotation;
            if (Picture != null) pictureStart = Picture.localRotation;
            if (LooseBook != null) bookStart = LooseBook.localRotation;
            if (Curtain != null) curtainStart = Curtain.localPosition;
            captured = true;
        }

        public void SetWakeCount(int completedDreams)
        {
            CaptureInitialState();
            int phase = Mathf.Max(0, completedDreams) % 4;
            if (ClockHand != null) ClockHand.localRotation = clockStart * Quaternion.Euler(0, 0, phase * 35f);
            if (Picture != null) Picture.localRotation = pictureStart * Quaternion.Euler(0, 0, phase == 0 ? 0 : -3f - phase * 2f);
            if (LooseBook != null) LooseBook.localRotation = bookStart * Quaternion.Euler(0, 0, phase == 0 ? 0 : phase * 8f);
            if (Curtain != null) Curtain.localPosition = curtainStart + Vector3.right * (phase * .09f);
            if (ExtraStar != null) ExtraStar.SetActive(phase >= 2);
            foreach (RoomHotspot hotspot in GetComponentsInChildren<RoomHotspot>(true))
                if (hotspot.Action == RoomAction.Dream)
                    hotspot.Label = completedDreams > 0 ? "Next Dream" : "New Game";
        }
    }
}
