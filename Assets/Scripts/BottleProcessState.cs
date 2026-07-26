using UnityEngine;

namespace ConveyorTwin
{
    public enum BottleQualityStatus
    {
        Empty = 0,
        DroppingToTurntable = 1,
        InTurntableBuffer = 2,
        Filling = 4,
        Filled = 5,
        Passed = 6,
        Capped = 7,
        Rejected = 8,
        AcceptedBin = 9,
        RejectedBin = 10,
        RejectEscaped = 11
    }

    public enum InfeedBottleState
    {
        None,
        DroppingToTurntable,
        OnTurntable,
        TransitioningToInfeedGuide,
        OnInfeedGuide,
        OnStarWheel
    }

    public class BottleProcessState : MonoBehaviour
    {
        [Min(0f)] public float liquidVolume01;
        public BottleQualityStatus status = BottleQualityStatus.Empty;
        public bool isDefective;
        public bool isOverflowed;
        public bool overflowCounted;
        public bool fillingCompleted;
        public bool inspectionCompleted;
        public bool capPlaced;
        public bool cappingCompleted;
        public bool counted;
        public Vector2 turntableVelocity;
        public InfeedBottleState infeedState = InfeedBottleState.None;
        public Transform capVisual;

        [Header("Visuals")]
        public Transform liquidVisual;
        public Transform overflowBodyVisual;
        public Transform overflowNeckVisual;
        public Renderer bottleRenderer;
        public Renderer liquidRenderer;
        public Renderer capRenderer;
        [HideInInspector] public float liquidVerticalScale = 1f;

        private Color emptyColor = new Color(0.82f, 0.95f, 1f, 0.35f);
        private Color passedColor = new Color(0.35f, 1f, 0.55f, 0.45f);
        private Color rejectedColor = new Color(1f, 0.35f, 0.25f, 0.45f);
        private Color escapedRejectColor = new Color(1f, 0.72f, 0.18f, 0.55f);
        private Color liquidColor = new Color(0.1f, 0.55f, 1f, 0.85f);
        private Color capColor = new Color(0.02f, 0.35f, 0.95f, 1f);
        private bool overflowVisualsConfigured;

        public void SetVolume(float volume01)
        {
            liquidVolume01 = Mathf.Max(0f, volume01);
            isOverflowed = liquidVolume01 > 1f;
            RefreshVisuals();
        }

        public bool IsFillWithinSpecification(float passThreshold)
        {
            return TwinProcessMath.IsFillWithinSpecification(liquidVolume01, passThreshold);
        }

        public void MarkPassed()
        {
            status = BottleQualityStatus.Passed;
            RefreshVisuals();
        }

        public void MarkRejected()
        {
            status = BottleQualityStatus.Rejected;
            RefreshVisuals();
        }

        public void RefreshVisuals()
        {
            var displayedVolume = Mathf.Clamp01(liquidVolume01);
            if (liquidVisual != null)
            {
                var scale = liquidVisual.localScale;
                scale.y = Mathf.Lerp(0.02f, 0.38f, displayedVolume) * liquidVerticalScale;
                liquidVisual.localScale = scale;

                var localPosition = liquidVisual.localPosition;
                localPosition.y = Mathf.Lerp(-0.30f, -0.04f, displayedVolume) * liquidVerticalScale;
                liquidVisual.localPosition = localPosition;
            }

            if (isOverflowed)
            {
                EnsureOverflowVisuals();
            }

            if (overflowBodyVisual != null)
            {
                overflowBodyVisual.gameObject.SetActive(isOverflowed);
            }

            if (overflowNeckVisual != null)
            {
                overflowNeckVisual.gameObject.SetActive(isOverflowed);
            }

            if (liquidRenderer != null)
            {
                liquidRenderer.material.color = liquidColor;
            }

            if (bottleRenderer == null)
            {
                return;
            }

            switch (status)
            {
                case BottleQualityStatus.Passed:
                case BottleQualityStatus.Capped:
                case BottleQualityStatus.AcceptedBin:
                    bottleRenderer.material.color = passedColor;
                    break;
                case BottleQualityStatus.Rejected:
                case BottleQualityStatus.RejectedBin:
                    bottleRenderer.material.color = rejectedColor;
                    break;
                case BottleQualityStatus.RejectEscaped:
                    bottleRenderer.material.color = escapedRejectColor;
                    break;
                default:
                    bottleRenderer.material.color = emptyColor;
                    break;
            }

            if (capVisual != null)
            {
                capVisual.gameObject.SetActive(capPlaced || cappingCompleted || status == BottleQualityStatus.Capped || status == BottleQualityStatus.AcceptedBin);
                var rendererToTint = capRenderer;
                if (rendererToTint == null)
                {
                    capVisual.TryGetComponent(out rendererToTint);
                }

                if (rendererToTint != null)
                {
                    rendererToTint.material.color = capColor;
                    rendererToTint.material.SetColor("_BaseColor", capColor);
                    rendererToTint.material.SetColor("_Color", capColor);
                }
            }
        }

        private void EnsureOverflowVisuals()
        {
            if (overflowBodyVisual == null && bottleRenderer != null)
            {
                overflowBodyVisual = CreateOverflowShell("Overflow Water - Bottle Body", bottleRenderer.transform, 1.10f, 1.04f);
            }

            if (overflowNeckVisual == null)
            {
                var neck = transform.Find("Bottle Neck");
                if (neck != null)
                {
                    overflowNeckVisual = CreateOverflowShell("Overflow Water - Bottle Neck", neck, 1.16f, 1.06f);
                }
            }

            if (overflowVisualsConfigured)
            {
                return;
            }

            ConfigureOverflowRenderer(overflowBodyVisual);
            ConfigureOverflowRenderer(overflowNeckVisual);
            overflowVisualsConfigured = overflowBodyVisual != null || overflowNeckVisual != null;
        }

        private Transform CreateOverflowShell(string objectName, Transform source, float radialScale, float verticalScale)
        {
            var shell = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shell.name = objectName;
            shell.transform.SetParent(source.parent);
            shell.transform.localPosition = source.localPosition;
            shell.transform.localRotation = source.localRotation;
            var sourceScale = source.localScale;
            shell.transform.localScale = new Vector3(sourceScale.x * radialScale, sourceScale.y * verticalScale, sourceScale.z * radialScale);

            var shellRenderer = shell.GetComponent<Renderer>();
            if (liquidRenderer != null)
            {
                shellRenderer.sharedMaterial = liquidRenderer.sharedMaterial;
            }

            var shellCollider = shell.GetComponent<Collider>();
            if (shellCollider != null)
            {
                shellCollider.enabled = false;
            }

            shell.SetActive(false);
            return shell.transform;
        }

        private static void ConfigureOverflowRenderer(Transform overflowVisual)
        {
            if (overflowVisual == null || !overflowVisual.TryGetComponent<Renderer>(out var overflowRenderer))
            {
                return;
            }

            var material = overflowRenderer.material;
            var overflowColor = new Color(0.02f, 0.38f, 1f, 0.92f);
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", overflowColor);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", overflowColor);
            }
            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", new Color(0.01f, 0.10f, 0.65f, 1f));
                material.EnableKeyword("_EMISSION");
            }
        }
    }
}
