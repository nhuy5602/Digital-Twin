using UnityEngine;

namespace ConveyorTwin
{
    public enum BottleQualityStatus
    {
        Empty,
        DroppingToTurntable,
        InTurntableBuffer,
        MovingToOutlet,
        Filling,
        Filled,
        Passed,
        Capped,
        Rejected,
        AcceptedBin,
        RejectedBin
    }

    public class BottleProcessState : MonoBehaviour
    {
        [Range(0f, 1f)] public float liquidVolume01;
        public BottleQualityStatus status = BottleQualityStatus.Empty;
        public bool isDefective;
        public bool fillingCompleted;
        public bool inspectionCompleted;
        public bool cappingCompleted;
        public bool counted;
        public Vector2 turntableVelocity;
        public Transform capVisual;

        [Header("Visuals")]
        public Transform liquidVisual;
        public Renderer bottleRenderer;
        public Renderer liquidRenderer;
        public Renderer capRenderer;

        private Color emptyColor = new Color(0.82f, 0.95f, 1f, 0.35f);
        private Color passedColor = new Color(0.35f, 1f, 0.55f, 0.45f);
        private Color rejectedColor = new Color(1f, 0.35f, 0.25f, 0.45f);
        private Color liquidColor = new Color(0.1f, 0.55f, 1f, 0.85f);
        private Color capColor = new Color(0.96f, 0.96f, 0.92f, 1f);

        public void SetVolume(float volume01)
        {
            liquidVolume01 = Mathf.Clamp01(volume01);
            RefreshVisuals();
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
            if (liquidVisual != null)
            {
                var scale = liquidVisual.localScale;
                scale.y = Mathf.Lerp(0.02f, 0.72f, liquidVolume01);
                liquidVisual.localScale = scale;

                var localPosition = liquidVisual.localPosition;
                localPosition.y = Mathf.Lerp(-0.33f, 0.02f, liquidVolume01);
                liquidVisual.localPosition = localPosition;
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
                default:
                    bottleRenderer.material.color = emptyColor;
                    break;
            }

            if (capVisual != null)
            {
                capVisual.gameObject.SetActive(cappingCompleted || status == BottleQualityStatus.Capped || status == BottleQualityStatus.AcceptedBin);
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
    }
}
