using UnityEditor;
using UnityEngine;

namespace GameLib
{
    /// Controls Unity Editor play, pause, and step states from keyboard shortcuts.
    [CreateAssetMenu(menuName = "GameLib/Debug/DevKeyboardShortcuts/DevTools/Play State Tool", fileName = "PlayStateTool")]
    public class PlayStateDevTool : DevActionTool
    {
        public enum PlayAction
        {
            TogglePlay,
            TogglePause,
            Step
        }

        [Header("Action Configuration")]
        [Tooltip("The specific play state action this tool will execute.")]
        public PlayAction actionType;

        /// Executes the selected editor play state action.
        public override void Execute()
        {
            switch (actionType)
            {
                case PlayAction.TogglePlay:
                    EditorApplication.isPlaying = !EditorApplication.isPlaying;
                    break;

                case PlayAction.TogglePause:
                    EditorApplication.isPaused = !EditorApplication.isPaused;
                    break;

                case PlayAction.Step:
                    if (!EditorApplication.isPlaying) return;
                    if (!EditorApplication.isPaused)
                    {
                        EditorApplication.isPaused = true;
                    }
                    EditorApplication.Step();
                    break;
            }
        }
    }
}