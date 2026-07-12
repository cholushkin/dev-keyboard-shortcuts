using UnityEngine;

namespace GameLib
{
    /// Controls game time scale using a hardcoded static sequence.
    [CreateAssetMenu(menuName = "GameLib/Debug/DevKeyboardShortcuts/DevTools/Time Scale Tool", fileName = "TimeScaleTool")]
    public class TimeScaleDevTool : DevActionTool
    {
        public enum TimeScaleAction
        {
            DecreaseScale,
            IncreaseScale,
            TogglePause,
            ResetToNormal
        }

        [Header("Action Configuration")]
        [Tooltip("The specific time control function this tool asset will execute when triggered.")]
        public TimeScaleAction actionType = TimeScaleAction.TogglePause;

        // Unified ascending sequence so increasing and decreasing step smoothly across the entire speed range.
        private static readonly float[] _timeScaleSequence = 
        { 
            0.0f, 0.05f, 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f, 0.9f, 
            1.0f, 1.1f, 1.2f, 1.5f, 2.0f, 3.0f, 4.0f, 5.0f, 10.0f 
        };

        private static float _targetTimeScale = 1.0f;
        private static bool _isPaused = false;

        /// Executes the configured time scale function.
        public override void Execute()
        {
            switch (actionType)
            {
                case TimeScaleAction.DecreaseScale:
                    StepDecrease();
                    break;

                case TimeScaleAction.IncreaseScale:
                    StepIncrease();
                    break;

                case TimeScaleAction.TogglePause:
                    TogglePauseState();
                    break;

                case TimeScaleAction.ResetToNormal:
                    ResetToNormalState();
                    break;
            }
        }

        private void StepDecrease()
        {
            float current = _targetTimeScale;
            int targetIndex = 0;

            // Scan backwards from the top to find the first step strictly smaller than our current scale
            for (int i = _timeScaleSequence.Length - 1; i >= 0; i--)
            {
                if (_timeScaleSequence[i] < current - 0.001f)
                {
                    targetIndex = i;
                    break;
                }
            }

            _targetTimeScale = _timeScaleSequence[targetIndex];
            ApplyTimeScale();
        }

        private void StepIncrease()
        {
            float current = _targetTimeScale;
            int targetIndex = _timeScaleSequence.Length - 1;

            // Scan forwards from zero to find the first step strictly greater than our current scale
            for (int i = 0; i < _timeScaleSequence.Length; i++)
            {
                if (_timeScaleSequence[i] > current + 0.001f)
                {
                    targetIndex = i;
                    break;
                }
            }

            _targetTimeScale = _timeScaleSequence[targetIndex];
            ApplyTimeScale();
        }

        private static void TogglePauseState()
        {
            _isPaused = !_isPaused;
            ApplyTimeScale();
        }

        private static void ResetToNormalState()
        {
            _targetTimeScale = 1.0f;
            _isPaused = false;
            ApplyTimeScale();
        }

        private static void ApplyTimeScale()
        {
            Time.timeScale = _isPaused ? 0.0f : _targetTimeScale;
        }

        public static float CurrentTimeScale => _targetTimeScale;
        public static bool IsPaused => _isPaused;
    }
}