using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
            TogglePause
        }

        [Header("Action Configuration")]
        [Tooltip("The specific time control function this tool asset will execute when triggered.")]
        public TimeScaleAction actionType = TimeScaleAction.TogglePause;

        /// Unified ascending sequence. 0.0f is removed so the knob stops at 0.05x speed; 0.0f is reserved strictly for TogglePause.
        private static readonly float[] _timeScaleSequence =
        {
            0.05f, 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f, 0.9f,
            1.0f, 1.1f, 1.2f, 1.5f, 2.0f, 3.0f, 4.0f, 5.0f, 10.0f
        };

        private static float _targetTimeScale = 1.0f;
        private static bool _isPaused = false;

        /// Resets static state when entering Play Mode (fixes Disable Domain Reload persistence bugs).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            _targetTimeScale = 1.0f;
            _isPaused = false;
            ApplyTimeScale();
        }

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
            }
        }

        private void StepDecrease()
        {
            float current = _targetTimeScale;
            int targetIndex = 0;

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

#if UNITY_EDITOR
            /// If Unity Editor's native pause was triggered (e.g., via Step tool or Editor button), unpause it when unpausing timescale!
            if (!_isPaused && EditorApplication.isPaused)
            {
                EditorApplication.isPaused = false;
            }
#endif

            /// Safety catch: if target scale was somehow 0 or less, reset to 1.0x so unpausing actually resumes gameplay.
            if (!_isPaused && _targetTimeScale <= 0.001f)
            {
                _targetTimeScale = 1.0f;
            }

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