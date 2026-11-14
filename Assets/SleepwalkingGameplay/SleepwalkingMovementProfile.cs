using UnityEngine;

namespace Sleepwalking
{
    [CreateAssetMenu(fileName = "Movement Profile", menuName = "Sleepwalking/Movement Profile")]
    public sealed class SleepwalkingMovementProfile : ScriptableObject
    {
        public string Description;
        [Min(0.1f)] public float RunSpeed = 8.5f;
        [Min(0.1f)] public float GroundAcceleration = 65f;
        [Min(0.1f)] public float AirAcceleration = 38f;
        [Min(0.1f)] public float JumpSpeed = 12f;
        [Min(0.1f)] public float GravityScale = 3.2f;
        [Min(0.1f)] public float DashSpeed = 18f;
        [Min(0.01f)] public float DashSeconds = .18f;
        [Min(0.1f)] public float WallSlideSpeed = 2f;
        [Min(0f)] public float CoyoteSeconds = .11f;
        [Min(0f)] public float JumpBufferSeconds = .13f;
        [Min(0f)] public float DashCooldownSeconds = .35f;
    }
}
