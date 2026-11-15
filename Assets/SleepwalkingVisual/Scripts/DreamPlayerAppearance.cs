using UnityEngine;

namespace Sleepwalking.Visual
{
    public sealed class DreamPlayerAppearance : MonoBehaviour
    {
        public Sprite TemporarySprite;

        const int FrameSize = 64;
        static readonly string[] SheetNames =
        {
            "Character/S_PizdukIdle",
            "Character/S_PizdukRun",
            "Character/S_Pizduk_Jump",
            "Character/S_Pizduk_Climb",
            "Character/S_Pizduk_Dash",
            "Character/S_Pizduk_Death"
        };
        static readonly int[] FrameCounts = { 9, 9, 9, 9, 14, 9 };
        static readonly float[] FrameSeconds = { .12f, .075f, .085f, .10f, .013f, .08f };

        enum Motion { Idle, Run, Jump, Climb, Dash, Death }

        static Sprite[][] frames;
        static bool framesLoaded;

        SleepwalkingPlayer player;
        Rigidbody2D body;
        BoxCollider2D shape;
        SpriteRenderer original;
        SpriteRenderer visual;
        Motion motion;
        int frame;
        float elapsed;

        void LateUpdate()
        {
            if (visual == null && !Attach()) return;

            Motion next = CurrentMotion();
            if (motion != next)
            {
                motion = next;
                frame = 0;
                elapsed = 0f;
            }
            else
            {
                elapsed += motion == Motion.Death ? Time.unscaledDeltaTime : Time.deltaTime;
                float interval = FrameSeconds[(int)motion];
                while (elapsed >= interval)
                {
                    elapsed -= interval;
                    int count = FrameCounts[(int)motion];
                    frame = motion == Motion.Jump || motion == Motion.Dash || motion == Motion.Death ?
                        Mathf.Min(frame + 1, count - 1) : (frame + 1) % count;
                }
            }

            Sprite[] sequence = frames[(int)motion];
            visual.sprite = sequence != null ? sequence[frame] : TemporarySprite;
            visual.flipX = original.flipX;
            visual.color = player.IsReturning ? new Color(1f, 1f, 1f, .65f) :
                motion != Motion.Death && !player.ControlsEnabled && original.color.g < .4f
                ? new Color(1f, .25f, .25f)
                : Color.white;
        }

        bool Attach()
        {
            GameObject dreamer = GameObject.Find("Dreamer");
            if (dreamer == null) return false;
            player = dreamer.GetComponent<SleepwalkingPlayer>();
            body = dreamer.GetComponent<Rigidbody2D>();
            shape = dreamer.GetComponent<BoxCollider2D>();
            original = dreamer.GetComponent<SpriteRenderer>();
            if (player == null || body == null || shape == null || original == null) return false;

            LoadFrames();
            if (frames[0] == null && TemporarySprite == null) return false;

            GameObject child = new GameObject("Dreamer Animation", typeof(SpriteRenderer));
            child.transform.SetParent(dreamer.transform, false);
            child.transform.localPosition = new Vector3(0f, 0f, -.05f);
            // The collider is .55 cells wide. The art occupies about half of each 64 px frame.
            child.transform.localScale = new Vector3(1.7f, 1f, 1f);
            visual = child.GetComponent<SpriteRenderer>();
            visual.sortingLayerID = original.sortingLayerID;
            visual.sortingOrder = original.sortingOrder;
            visual.sprite = frames[0] != null ? frames[0][0] : TemporarySprite;
            original.enabled = false;
            return true;
        }

        Motion CurrentMotion()
        {
            if (player.IsImpact) return Motion.Death;
            if (player.IsReturning) return Motion.Idle;
            if (player.IsDashing) return Motion.Dash;
            if (!player.ControlsEnabled) return Motion.Idle;
            Vector2 velocity = body.linearVelocity;
            float unit = player.WorldUnitsPerCell;
            if (!player.IsGrounded)
            {
                if (velocity.y < -.1f * unit && TouchingFacingWall() && frames[(int)Motion.Climb] != null)
                    return Motion.Climb;
                return Motion.Jump;
            }
            return Mathf.Abs(velocity.x) > .12f * unit ? Motion.Run : Motion.Idle;
        }

        bool TouchingFacingWall()
        {
            ContactFilter2D filter = new ContactFilter2D();
            filter.useTriggers = false;
            RaycastHit2D[] hits = new RaycastHit2D[4];
            Vector2 direction = original.flipX ? Vector2.left : Vector2.right;
            return shape.Cast(direction, filter, hits, .07f * player.WorldUnitsPerCell) > 0;
        }

        static void LoadFrames()
        {
            if (framesLoaded) return;
            framesLoaded = true;
            frames = new Sprite[SheetNames.Length][];
            for (int q = 0; q < SheetNames.Length; q++)
            {
                Texture2D sheet = Resources.Load<Texture2D>(SheetNames[q]);
                int count = FrameCounts[q];
                if (sheet == null || sheet.width != FrameSize * count || sheet.height != FrameSize)
                {
                    Debug.LogError("Dreamer animation sheet is missing or has the wrong size: " + SheetNames[q]);
                    continue;
                }

                frames[q] = new Sprite[count];
                for (int e = 0; e < count; e++)
                {
                    Rect area = new Rect(e * FrameSize, 0f, FrameSize, FrameSize);
                    frames[q][e] = Sprite.Create(sheet, area, new Vector2(.5f, .5f), FrameSize, 0,
                        SpriteMeshType.FullRect);
                    frames[q][e].name = sheet.name + "_" + e;
                }
            }
        }

        void OnDestroy()
        {
            if (original != null) original.enabled = true;
            if (visual != null) Destroy(visual.gameObject);
        }
    }
}
