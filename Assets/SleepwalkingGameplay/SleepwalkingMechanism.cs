using UnityEngine;

namespace Sleepwalking
{
    public sealed class SleepwalkingMechanism : MonoBehaviour
    {
        public string FeatureId;
        public string Kind;
        public string LinkedFeatureId;
        public float ActiveSeconds = 3f;
        public Vector2 MoveOffset;
        public float CycleSeconds = 2f;
        public float PhaseSeconds;

        Collider2D shape;
        Rigidbody2D movingBody;
        static PhysicsMaterial2D movingMaterial;
        Renderer[] visuals;
        Vector3 initialPosition;
        float elapsed;
        float disabledUntil;
        bool initialEnabled;
        Vector2 movementVelocity;
        SleepwalkingPlayer rider;
        int exitContactFrames;

        void Awake()
        {
            shape = GetComponent<Collider2D>();
            visuals = GetComponents<Renderer>();
            initialPosition = transform.localPosition;
            initialEnabled = shape == null || shape.enabled;
        }

        void Update()
        {
            if (SleepwalkingGame.Instance == null || !SleepwalkingGame.Instance.InDream) return;
            if (Kind != "MovingPlatform") elapsed += Time.deltaTime;
            if (Kind == "TimedPlatform")
            {
                float cycle = Mathf.Max(.3f, CycleSeconds);
                bool available = Mathf.Repeat(elapsed, cycle) < Mathf.Clamp(ActiveSeconds, 0f, cycle);
                if (shape != null) shape.enabled = available;
                SetVisible(available);
            }
            if (Kind == "Saw") transform.Rotate(0, 0, 360f * Time.deltaTime);
            if (IsHazard())
            {
                bool active = Time.time >= disabledUntil;
                if (shape != null) shape.enabled = active;
                SetVisible(active);
            }
        }

        void FixedUpdate()
        {
            if (Kind != "MovingPlatform" || movingBody == null || SleepwalkingGame.Instance == null ||
                !SleepwalkingGame.Instance.InDream) return;

            elapsed += Time.fixedDeltaTime;
            float cycle = Mathf.Max(.2f, CycleSeconds);
            // Косинус начинает движение у исходной точки и плавно разворачивается на концах пути.
            float phase = (1f - Mathf.Cos(elapsed * Mathf.PI * 2f / cycle)) * .5f;
            Vector3 localTarget = initialPosition + (Vector3)(MoveOffset * phase);
            Vector2 target = transform.parent == null ? localTarget : transform.parent.TransformPoint(localTarget);
            movementVelocity = (target - movingBody.position) / Time.fixedDeltaTime;
            movingBody.MovePosition(target);
            if (rider != null) rider.SetPlatformVelocity(this, movementVelocity);
        }

        public void DisableFor(float seconds)
        {
            disabledUntil = Mathf.Max(disabledUntil, Time.time + Mathf.Max(.1f, seconds));
        }

        public void ResetMechanism()
        {
            if (rider != null) rider.ClearPlatformVelocity(this);
            rider = null;
            exitContactFrames = 0;
            movementVelocity = Vector2.zero;
            elapsed = Kind == "MovingPlatform" ? 0f : PhaseSeconds;
            disabledUntil = 0f;
            transform.localPosition = initialPosition;
            if (Kind == "MovingPlatform")
            {
                // Кинематическое тело перемещает твёрдую платформу в физическом такте.
                if (movingBody == null) movingBody = GetComponent<Rigidbody2D>();
                if (movingBody == null) movingBody = gameObject.AddComponent<Rigidbody2D>();
                movingBody.bodyType = RigidbodyType2D.Kinematic;
                movingBody.gravityScale = 0f;
                movingBody.useFullKinematicContacts = true;
                movingBody.position = transform.position;
                if (shape != null)
                {
                    // Трение не должно второй раз прибавлять горизонтальный перенос к скорости игрока.
                    if (movingMaterial == null) movingMaterial = new PhysicsMaterial2D("Moving platform") { friction = 0f };
                    shape.sharedMaterial = movingMaterial;
                }
            }
            if (shape != null) shape.enabled = initialEnabled;
            SetVisible(true);
        }

        void OnCollisionEnter2D(Collision2D collision) { TrackRider(collision); }
        void OnCollisionStay2D(Collision2D collision) { TrackRider(collision); }
        void OnCollisionExit2D(Collision2D collision)
        {
            if (Kind != "MovingPlatform" || rider == null || !collision.gameObject.TryGetComponent(out SleepwalkingPlayer player) ||
                rider != player) return;
            rider.ClearPlatformVelocity(this);
            rider = null;
        }

        void TrackRider(Collision2D collision)
        {
            if (Kind != "MovingPlatform" || shape == null ||
                !collision.gameObject.TryGetComponent(out SleepwalkingPlayer player)) return;
            Collider2D playerShape = player.GetComponent<Collider2D>();
            if (playerShape == null || Mathf.Abs(playerShape.bounds.min.y - shape.bounds.max.y) > .22f)
            {
                if (rider == player)
                {
                    rider.ClearPlatformVelocity(this);
                    rider = null;
                }
                return;
            }
            rider = player;
            rider.SetPlatformVelocity(this, movementVelocity);
        }

        void SetVisible(bool visible)
        {
            foreach (Renderer renderer in visuals) renderer.enabled = visible;
        }

        public bool IsHazard() => Kind == "Spikes" || Kind == "Saw" || Kind == "LaserBase" ||
            Kind == "LaserBeam" || Kind == "FallReset";

        void OnTriggerEnter2D(Collider2D other) { Touch(other); }
        void OnTriggerExit2D(Collider2D other)
        {
            if (Kind == "Exit" && other.GetComponent<SleepwalkingPlayer>() != null)
                exitContactFrames = 0;
        }
        void OnTriggerStay2D(Collider2D other)
        {
            if (Kind != "Button") Touch(other);
        }

        void Touch(Collider2D other)
        {
            if (!other.TryGetComponent(out SleepwalkingPlayer player)) return;
            SleepwalkingGame game = SleepwalkingGame.Instance;
            if (game == null || !game.InDream) return;
            if (Kind == "Exit")
            {
                exitContactFrames = player.CanUseExit(transform.position.x) ? exitContactFrames + 1 : 0;
                if (exitContactFrames >= 4) game.CompleteDream();
            }
            else if (Kind == "Button") game.PressButton(this);
            else if (IsHazard() && Time.time >= disabledUntil)
                game.KillPlayer(shape.Distance(other).pointA);
        }
    }
}
