using UnityEngine;

namespace GameLib
{
    /// Abstract base class for all modular developer tools.
    /// Can be subclassed in runtime gameplay code or editor-only code.
    public abstract class DevActionTool : ScriptableObject
    {
        /// Executes the action. Called automatically by the input router when the associated key/device is pressed.
        public abstract void Execute();
    }
}