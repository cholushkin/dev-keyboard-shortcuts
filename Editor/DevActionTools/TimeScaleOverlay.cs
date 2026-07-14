using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameLib
{
    /// Scene View overlay displaying real-time game time scale with emoji formatting.
    [Overlay(typeof(SceneView), "Time Scale")]
    public class TimeScaleOverlay : Overlay
    {
        private Label _statusLabel;

        public override VisualElement CreatePanelContent()
        {
            _statusLabel = new Label();
            _statusLabel.style.fontSize = 13;
            _statusLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _statusLabel.style.paddingLeft = 8;
            _statusLabel.style.paddingRight = 8;
            _statusLabel.style.paddingTop = 4;
            _statusLabel.style.paddingBottom = 4;

            _statusLabel.RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            _statusLabel.RegisterCallback<DetachFromPanelEvent>(OnDetachedFromPanel);

            return _statusLabel;
        }

        private void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
            OnEditorUpdate();
        }

        private void OnDetachedFromPanel(DetachFromPanelEvent evt)
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (_statusLabel == null) return;

            float value = TimeScaleDevTool.CurrentTimeScale;
            bool paused = TimeScaleDevTool.IsPaused;

            string statusIcon = paused ? "⏸️" : "🕑";
            
            /// Render the target timescale value even while paused so knob adjustments are visible in real time
            string formattedScale = paused ? $"x{value:0.##} (PAUSED)" : $"x{value:0.##}";

            _statusLabel.text = $"{statusIcon}scale: {formattedScale}";
            _statusLabel.style.color = paused ? new StyleColor(new Color(1.0f, 0.4f, 0.4f)) : new StyleColor(Color.white);
        }
    }
}