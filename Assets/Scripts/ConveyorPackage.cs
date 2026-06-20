using UnityEngine;

namespace ConveyorTwin
{
    [RequireComponent(typeof(Rigidbody))]
    public class ConveyorPackage : MonoBehaviour
    {
        [Min(0.01f)] public float massKg = 4f;
        [Range(0f, 1f)] public float staticFrictionCoefficient = 0.45f;
        public ConveyorBeltDigitalTwin belt;

        [Header("Demo motion")]
        public bool forceKinematicMotion = true;
        public bool loopOnBelt = true;
        public float startZ = -2.75f;
        public float endZ = 2.75f;
        public float laneX = 0f;
        public float rideHeightY = 0.86f;

        public bool IsSlipping { get; private set; }

        private Rigidbody body;
        private float previousSpeed;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.mass = massKg;
            body.isKinematic = forceKinematicMotion;
        }

        private void FixedUpdate()
        {
            if (belt == null || belt.EmergencyStop)
            {
                if (forceKinematicMotion && body != null)
                {
                    body.linearVelocity = Vector3.zero;
                }

                return;
            }

            var speed = Mathf.Max(0f, belt.CurrentTelemetry.beltSpeedMps);
            var targetVelocity = Vector3.forward * speed;
            UpdateSlipEstimate(speed);

            if (forceKinematicMotion)
            {
                MovePackageOnBelt(speed);
                previousSpeed = speed;
                return;
            }

            var velocityDelta = targetVelocity - body.linearVelocity;
            var desiredAcceleration = velocityDelta / Time.fixedDeltaTime;

            // Maximum transferable force before sliding: Fmax = mu_s * m * g.
            var maxFrictionForce = staticFrictionCoefficient * body.mass * Physics.gravity.magnitude;
            var requiredForce = body.mass * desiredAcceleration.magnitude;
            IsSlipping = requiredForce > maxFrictionForce;

            var appliedForce = desiredAcceleration.normalized * Mathf.Min(requiredForce, maxFrictionForce);
            body.AddForce(appliedForce, ForceMode.Force);
            previousSpeed = speed;
        }

        private void MovePackageOnBelt(float speed)
        {
            var position = body.position;
            position.x = laneX;
            position.y = rideHeightY;
            position.z += speed * Time.fixedDeltaTime;

            if (loopOnBelt && position.z > endZ)
            {
                position.z = startZ;
            }

            body.MovePosition(position);
        }

        private void UpdateSlipEstimate(float speed)
        {
            var acceleration = Mathf.Abs(speed - previousSpeed) / Mathf.Max(Time.fixedDeltaTime, 0.0001f);
            var requiredForce = body.mass * acceleration;
            var maxFrictionForce = staticFrictionCoefficient * body.mass * Physics.gravity.magnitude;
            IsSlipping = requiredForce > maxFrictionForce;
        }
    }
}
