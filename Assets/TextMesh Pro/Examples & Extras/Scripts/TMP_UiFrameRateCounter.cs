using TMPro;
using UnityEngine;

namespace TextMesh_Pro.Scripts
{
    
    public class TMP_UiFrameRateCounter : MonoBehaviour
    {
        public float UpdateInterval = 5.0f;
        float m_LastInterval = 0;
        int m_Frames = 0;

        public enum FpsCounterAnchorPositions { TopLeft, BottomLeft, TopRight, BottomRight };

        public FpsCounterAnchorPositions AnchorPosition = FpsCounterAnchorPositions.TopRight;

        string htmlColorTag;
        const string fpsLabel = "{0:2}</color> <#8080ff>FPS \n<#FF8000>{1:2} <#8080ff>MS";

        TextMeshProUGUI m_TextMeshPro;
        RectTransform m_frameCounter_transform;

        FpsCounterAnchorPositions last_AnchorPosition;

        void Awake()
        {
            if (!this.enabled)
                return;

            Application.targetFrameRate = -1;

            var frameCounter = new GameObject("Frame Counter");
            this.m_frameCounter_transform = frameCounter.AddComponent<RectTransform>();

            this.m_frameCounter_transform.SetParent(this.transform, false);

            this.m_TextMeshPro = frameCounter.AddComponent<TextMeshProUGUI>();
            this.m_TextMeshPro.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            this.m_TextMeshPro.fontSharedMaterial = Resources.Load<Material>("Fonts & Materials/LiberationSans SDF - Overlay");

            this.m_TextMeshPro.enableWordWrapping = false;
            this.m_TextMeshPro.fontSize = 36;

            this.m_TextMeshPro.isOverlay = true;

            this.Set_FrameCounter_Position(this.AnchorPosition);
            this.last_AnchorPosition = this.AnchorPosition;
        }


        void Start()
        {
            this.m_LastInterval = Time.realtimeSinceStartup;
            this.m_Frames = 0;
        }


        void Update()
        {
            if (this.AnchorPosition != this.last_AnchorPosition) this.Set_FrameCounter_Position(this.AnchorPosition);

            this.last_AnchorPosition = this.AnchorPosition;

            this.m_Frames += 1;
            var timeNow = Time.realtimeSinceStartup;

            if (timeNow > this.m_LastInterval + this.UpdateInterval)
            {
                // display two fractional digits (f2 format)
                var fps = this.m_Frames / (timeNow - this.m_LastInterval);
                var ms = 1000.0f / Mathf.Max(fps, 0.00001f);

                if (fps < 30)
                    this.htmlColorTag = "<color=yellow>";
                else if (fps < 10)
                    this.htmlColorTag = "<color=red>";
                else
                    this.htmlColorTag = "<color=green>";

                this.m_TextMeshPro.SetText(this.htmlColorTag + fpsLabel, fps, ms);

                this.m_Frames = 0;
                this.m_LastInterval = timeNow;
            }
        }


        void Set_FrameCounter_Position(FpsCounterAnchorPositions anchor_position)
        {
            switch (anchor_position)
            {
                case FpsCounterAnchorPositions.TopLeft:
                    this.m_TextMeshPro.alignment = TextAlignmentOptions.TopLeft;
                    this.m_frameCounter_transform.pivot = new Vector2(0, 1);
                    this.m_frameCounter_transform.anchorMin = new Vector2(0.01f, 0.99f);
                    this.m_frameCounter_transform.anchorMax = new Vector2(0.01f, 0.99f);
                    this.m_frameCounter_transform.anchoredPosition = new Vector2(0, 1);
                    break;
                case FpsCounterAnchorPositions.BottomLeft:
                    this.m_TextMeshPro.alignment = TextAlignmentOptions.BottomLeft;
                    this.m_frameCounter_transform.pivot = new Vector2(0, 0);
                    this.m_frameCounter_transform.anchorMin = new Vector2(0.01f, 0.01f);
                    this.m_frameCounter_transform.anchorMax = new Vector2(0.01f, 0.01f);
                    this.m_frameCounter_transform.anchoredPosition = new Vector2(0, 0);
                    break;
                case FpsCounterAnchorPositions.TopRight:
                    this.m_TextMeshPro.alignment = TextAlignmentOptions.TopRight;
                    this.m_frameCounter_transform.pivot = new Vector2(1, 1);
                    this.m_frameCounter_transform.anchorMin = new Vector2(0.99f, 0.99f);
                    this.m_frameCounter_transform.anchorMax = new Vector2(0.99f, 0.99f);
                    this.m_frameCounter_transform.anchoredPosition = new Vector2(1, 1);
                    break;
                case FpsCounterAnchorPositions.BottomRight:
                    this.m_TextMeshPro.alignment = TextAlignmentOptions.BottomRight;
                    this.m_frameCounter_transform.pivot = new Vector2(1, 0);
                    this.m_frameCounter_transform.anchorMin = new Vector2(0.99f, 0.01f);
                    this.m_frameCounter_transform.anchorMax = new Vector2(0.99f, 0.01f);
                    this.m_frameCounter_transform.anchoredPosition = new Vector2(1, 0);
                    break;
            }
        }
    }
}