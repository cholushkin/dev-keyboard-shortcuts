using UnityEngine;

namespace GameLib
{
    /// Defines the editor context required for a shortcut to execute.
    public enum DevExecutionContext
    {
        [Tooltip("Executes anywhere in Unity, but is ignored when typing in text fields.")]
        GlobalIgnoreTextFields,

        [Tooltip("Executes ONLY when the mouse is hovering over or focused on the Scene View (like standard Arrow/Numpad navigation).")]
        SceneViewOnly,

        [Tooltip("Executes always, even when editing Inspector text fields.")]
        Always
    }
}