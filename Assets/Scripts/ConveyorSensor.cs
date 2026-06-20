using UnityEngine;

namespace ConveyorTwin
{
    public class ConveyorSensor : MonoBehaviour
    {
        public string sensorId = "S-IN-01";
        public ConveyorBeltDigitalTwin belt;
        public int detectedPackages;

        public float LastDetectionTime { get; private set; }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.GetComponentInParent<ConveyorPackage>())
            {
                return;
            }

            detectedPackages++;
            LastDetectionTime = Time.time;
        }

        public float PackagesPerMinute(float sampleWindowSeconds)
        {
            if (belt == null || sampleWindowSeconds <= 0f)
            {
                return 0f;
            }

            return detectedPackages / sampleWindowSeconds * 60f;
        }
    }
}
