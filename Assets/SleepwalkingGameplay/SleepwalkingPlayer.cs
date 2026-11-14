using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Sleepwalking
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class SleepwalkingPlayer : MonoBehaviour
    {
        public SleepwalkingMovementProfile Profile;
        public float RunSpeed = 8.5f;
        public float GroundAcceleration = 65f;
        public float AirAcceleration = 38f;
        public float JumpSpeed = 12f;
        public float GravityScale = 3.2f;
        public float DashSpeed = 18f;
        public float DashSeconds = .18f;
        public float WallSlideSpeed = 2f;
        public float CoyoteSeconds = .11f;
        public float JumpBufferSeconds = .13f;
        public float DashCooldownSeconds = .35f;
        public bool ControlsEnabled = true;

        Rigidbody2D body;
        BoxCollider2D shape;
        SpriteRenderer sprite;
        float jumpBuffer;
        float coyote;
        float dashRemaining;
        float dashReady;
        int dashCharges = 1;
        int facing = 1;
        int wall;
        bool grounded;
        bool wasGrounded;
        bool wasSliding;
        float lastWallMark;
        SleepwalkingMechanism standingPlatform;
        Vector2 platformVelocity;
        bool simulatedInputEnabled;
        float simulatedHorizontal;
        bool simulatedJump;
        bool simulatedDash;

        public bool IsGrounded => grounded;
        public bool CanUseExit(float bedX)
        {
            if (!grounded || body == null || shape == null || Mathf.Abs(body.linearVelocity.y) >= .3f)
                return false;
            Collider2D[] contacts = new Collider2D[8];
            int count = shape.GetContacts(contacts);
            for (int q = 0; q < count; q++)
            {
                Collider2D support = contacts[q];
                if (support == null || support.isTrigger ||
                    Mathf.Abs(shape.bounds.min.y - support.bounds.max.y) >= .15f)
                    continue;
                Bounds surface = support.bounds;
                float inset = shape.bounds.extents.x + .15f;
                if (bedX >= surface.min.x && bedX <= surface.max.x &&
                    body.position.x >= surface.min.x + inset && body.position.x <= surface.max.x - inset)
                    return true;
            }
            return false;
        }

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            shape = GetComponent<BoxCollider2D>();
            sprite = GetComponent<SpriteRenderer>();
            ApplyProfile();
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        void Update()
        {
            if (!ControlsEnabled) return;
            if (PressedJump()) jumpBuffer = JumpBufferSeconds;
            else jumpBuffer -= Time.deltaTime;
            if (PressedDash() && dashReady <= 0f && dashCharges > 0)
            {
                dashRemaining = DashSeconds;
                dashReady = DashCooldownSeconds;
                dashCharges--;
                body.gravityScale = 0f;
                SleepwalkingGame.Instance?.PlayCue("Dash");
            }
            dashReady -= Time.deltaTime;
        }

        void FixedUpdate()
        {
            if (!ControlsEnabled) return;
            ContactFilter2D filter = new ContactFilter2D();
            filter.useTriggers = false;
            RaycastHit2D[] hits = new RaycastHit2D[4];
            grounded = shape.Cast(Vector2.down, filter, hits, .09f) > 0;
            wall = WallDirection(filter, hits);
            if (grounded && !wasGrounded) SleepwalkingGame.Instance?.PlayCue("Landing");
            wasGrounded = grounded;
            if (grounded)
            {
                coyote = CoyoteSeconds;
                if (dashRemaining <= 0f) { dashReady = 0f; dashCharges = 1; }
            }
            else coyote -= Time.fixedDeltaTime;
            if (!grounded && standingPlatform != null) ClearPlatformVelocity(standingPlatform);

            float horizontal = Horizontal();
            if (horizontal > .01f) facing = 1;
            if (horizontal < -.01f) facing = -1;
            if (sprite != null) sprite.flipX = facing < 0;

            if (dashRemaining > 0f)
            {
                dashRemaining -= Time.fixedDeltaTime;
                body.linearVelocity = new Vector2(facing * DashSpeed + platformVelocity.x, 0);
                if (dashRemaining <= 0f) body.gravityScale = GravityScale;
                return;
            }

            Vector2 velocity = body.linearVelocity;
            // При отсутствии ввода игрок следует за платформой, но может бежать в любую сторону.
            velocity.x = Mathf.MoveTowards(velocity.x, horizontal * RunSpeed + platformVelocity.x,
                (grounded ? GroundAcceleration : AirAcceleration) * Time.fixedDeltaTime);
            if (grounded && platformVelocity.y < 0f) velocity.y = Mathf.Min(velocity.y, platformVelocity.y);
            bool sliding = wall != 0 && !grounded && velocity.y < -WallSlideSpeed && horizontal * wall > 0f;
            if (sliding && !wasSliding) SleepwalkingGame.Instance?.PlayCue("WallGrab");
            wasSliding = sliding;
            if (sliding)
                velocity.y = -WallSlideSpeed;
            if (jumpBuffer > 0f && (coyote > 0f || wall != 0))
            {
                if (wall != 0 && !grounded) velocity.x = -wall * RunSpeed;
                velocity.y = JumpSpeed;
                jumpBuffer = 0f;
                coyote = 0f;
                SleepwalkingGame.Instance?.PlayCue("Jump");
            }
            body.linearVelocity = velocity;
        }

        public void ResetAt(Vector2 position)
        {
            body.simulated = true;
            body.position = position;
            body.linearVelocity = Vector2.zero;
            body.gravityScale = GravityScale;
            dashRemaining = dashReady = jumpBuffer = coyote = 0f;
            dashCharges = 1;
            simulatedHorizontal = 0f;
            simulatedJump = simulatedDash = false;
            wasGrounded = wasSliding = false;
            if (standingPlatform != null) ClearPlatformVelocity(standingPlatform);
            ControlsEnabled = true;
            if (sprite != null) sprite.color = Color.white;
        }

        public void SetImpact()
        {
            ControlsEnabled = false;
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
            if (standingPlatform != null) ClearPlatformVelocity(standingPlatform);
            if (sprite != null) sprite.color = new Color(1f, .25f, .25f);
        }

        public void SetPlatformVelocity(SleepwalkingMechanism platform, Vector2 velocity)
        {
            standingPlatform = platform;
            platformVelocity = velocity;
        }

        public void ClearPlatformVelocity(SleepwalkingMechanism platform)
        {
            if (standingPlatform != platform) return;
            standingPlatform = null;
            platformVelocity = Vector2.zero;
        }

        public void ApplyProfile()
        {
            if (Profile != null)
            {
                RunSpeed = Profile.RunSpeed;
                GroundAcceleration = Profile.GroundAcceleration;
                AirAcceleration = Profile.AirAcceleration;
                JumpSpeed = Profile.JumpSpeed;
                GravityScale = Profile.GravityScale;
                DashSpeed = Profile.DashSpeed;
                DashSeconds = Profile.DashSeconds;
                WallSlideSpeed = Profile.WallSlideSpeed;
                CoyoteSeconds = Profile.CoyoteSeconds;
                JumpBufferSeconds = Profile.JumpBufferSeconds;
                DashCooldownSeconds = Profile.DashCooldownSeconds;
            }
            if (body == null) body = GetComponent<Rigidbody2D>();
            body.gravityScale = GravityScale;
        }

        // Проверка маршрута подаёт те же команды, что клавиатура, в реальный контроллер.
        public void SetSimulatedInput(float horizontal, bool jump = false, bool dash = false)
        {
            simulatedInputEnabled = true;
            simulatedHorizontal = Mathf.Clamp(horizontal, -1f, 1f);
            simulatedJump |= jump;
            simulatedDash |= dash;
        }

        public void ClearSimulatedInput()
        {
            simulatedInputEnabled = false;
            simulatedHorizontal = 0f;
            simulatedJump = simulatedDash = false;
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (!ControlsEnabled || Time.time - lastWallMark < .18f) return;
            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (Mathf.Abs(contact.normal.x) < .6f) continue;
                SleepwalkingGame.Instance?.PaintAt(contact.point);
                lastWallMark = Time.time;
                break;
            }
        }

        int WallDirection(ContactFilter2D filter, RaycastHit2D[] hits)
        {
            int count = shape.Cast(Vector2.right, filter, hits, .07f);
            for (int q = 0; q < count; q++)
                if (!OneWaySurface(hits[q].collider)) return 1;
            count = shape.Cast(Vector2.left, filter, hits, .07f);
            for (int q = 0; q < count; q++)
                if (!OneWaySurface(hits[q].collider)) return -1;
            return 0;
        }

        static bool OneWaySurface(Collider2D collider) => collider != null &&
            collider.usedByEffector && collider.GetComponent<PlatformEffector2D>() != null;

        float Horizontal()
        {
            if (simulatedInputEnabled) return simulatedHorizontal;
#if ENABLE_INPUT_SYSTEM
            Keyboard k = Keyboard.current;
            if (k == null) return 0f;
            return (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1f : 0f) -
                (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1f : 0f);
#else
            return Input.GetAxisRaw("Horizontal");
#endif
        }

        bool PressedJump()
        {
            if (simulatedInputEnabled)
            {
                bool pressed = simulatedJump;
                simulatedJump = false;
                return pressed;
            }
#if ENABLE_INPUT_SYSTEM
            Keyboard k = Keyboard.current;
            return k != null && (k.spaceKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame || k.upArrowKey.wasPressedThisFrame);
#else
            return Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
#endif
        }

        bool PressedDash()
        {
            if (simulatedInputEnabled)
            {
                bool pressed = simulatedDash;
                simulatedDash = false;
                return pressed;
            }
#if ENABLE_INPUT_SYSTEM
            Keyboard k = Keyboard.current;
            return k != null && (k.leftShiftKey.wasPressedThisFrame || k.rightShiftKey.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);
#endif
        }
    }
}
