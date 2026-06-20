using UnityEngine;

namespace ConveyorTwin
{
    [RequireComponent(typeof(Rigidbody))]
    public class ConveyorPackage : MonoBehaviour
    {
        [Min(0.01f)] public float massKg = 4f;
        [Range(0f, 1f)] public float staticFrictionCoefficient = 0.45f;
        public ConveyorBeltDigitalTwin belt;

        public bool IsSlipping { get; private set; }

        private Rigidbody body;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.mass = massKg;
        }

        private void FixedUpdate()
        {
            if (belt == null || belt.EmergencyStop)
            {
                return;
            }

            var targetVelocity = transform.forward * belt.CurrentTelemetry.beltSpeedMps;
            var velocityDelta = targetVelocity - body.velocity;
            var desiredAcceleration = velocityDelta / Time.fixedDeltaTime;

            // Maximum transferable force before sliding: Fmax = mu_s * m * g.
            var maxFrictionForce = staticFrictionCoefficient * body.mass * Physics.gravity.magnitude;
            var requiredForce = body.mass * desiredAcceleration.magnitude;
            IsSlipping = requiredForce > maxFrictionForce;

            var appliedForce = desiredAcceleration.normalized * Mathf.Min(requiredForce, maxFrictionForce);
            body.AddForce(appliedForce, ForceMode.Force);
        }
    }
}
