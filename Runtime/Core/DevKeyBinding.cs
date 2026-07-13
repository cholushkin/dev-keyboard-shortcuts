using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GameLib
{
    [Serializable]
    public struct DevKeyModifier
    {
        [Tooltip("Paste the USB Hardware ID or unique substring for the modifier key. Leave empty to match ANY keyboard.")]
        public string deviceHardwareId;

        [Tooltip("Virtual Key (VK) code in decimal for the modifier key (e.g., 162 for Left Ctrl, 160 for Left Shift, 98 for Numpad 2).")]
        public int virtualKeyCode;

        public bool IsHeld()
        {
            if (virtualKeyCode <= 0) return true;
            return DevRawInputRouter.IsKeyCurrentlyHeld(virtualKeyCode, deviceHardwareId);
        }
    }

    [Serializable]
    public struct DevKeyBinding
    {
        [Tooltip("Enable or disable this shortcut without deleting it.")]
        public bool isEnabled;

        [Header("Execution Context")]
        [Tooltip("Where this shortcut is allowed to trigger.")]
        public DevExecutionContext executionContext;

        [Header("Hardware Device Mapping")]
        [Tooltip("Paste the USB Hardware ID (e.g., 'VID_046D&PID_C31C') or a unique substring. Leave empty to match ANY keyboard.")]
        public string deviceHardwareId;

        [Tooltip("A human-readable note for yourself (e.g., 'Main Keyboard' or 'External Numpad').")]
        public string deviceFriendlyName;

        [Header("Key & Action")]
        [Tooltip("The standard Windows Virtual Key (VK) code in decimal (e.g., 96 for Numpad 0, 97 for Numpad 1).")]
        public int virtualKeyCode;

        [Header("Modifiers (Up to 2)")]
        [Tooltip("Optional modifier keys that must be held down simultaneously. Up to 2 modifiers are evaluated.")]
        public List<DevKeyModifier> modifiers;

        [Tooltip("The tool asset to execute when this key is pressed on the specified device.")]
        public DevActionTool boundTool;

        /// Evaluates if an incoming raw key press matches this binding.
        public bool Matches(int vkCode, string incomingHardwareId)
        {
            if (!isEnabled || boundTool == null) return false;
            if (this.virtualKeyCode != vkCode) return false;

            // 1. Validate Execution Context (Focus & Text Field Checks)
            if (!IsContextValid()) return false;

            // 2. Validate Hardware ID
            if (!string.IsNullOrEmpty(deviceHardwareId))
            {
                if (string.IsNullOrEmpty(incomingHardwareId) ||
                    incomingHardwareId.IndexOf(deviceHardwareId, StringComparison.OrdinalIgnoreCase) == -1)
                {
                    return false;
                }
            }

            // 3. Validate Modifiers
            if (modifiers != null)
            {
                int count = Mathf.Min(modifiers.Count, 2);
                for (int i = 0; i < count; i++)
                {
                    if (!modifiers[i].IsHeld()) return false;
                }
            }

            return true;
        }

        /// Checks Editor focus state and text-field editing status before allowing execution.
        private bool IsContextValid()
        {
#if UNITY_EDITOR
            // 1. Check standard IMGUI text fields (Inspector, Hierarchy renaming, etc.)
            bool isEditingTextField = EditorGUIUtility.editingTextField;

            // 2. Check UI Toolkit text fields (Unity 2020+ / Unity 6 Editor Windows)
            if (!isEditingTextField && EditorWindow.focusedWindow != null)
            {
                var focusedElement = EditorWindow.focusedWindow.rootVisualElement?.focusController?.focusedElement;
                if (focusedElement != null && focusedElement.GetType().Name.Contains("TextField"))
                {
                    isEditingTextField = true;
                }
            }

            switch (executionContext)
            {
                case DevExecutionContext.GlobalIgnoreTextFields:
                    if (isEditingTextField) return false;
                    break;

                case DevExecutionContext.SceneViewOnly:
                    if (isEditingTextField) return false;

                    // Execute ONLY if the mouse is hovering over the SceneView OR if the SceneView is focused
                    bool isSceneViewActive = (EditorWindow.mouseOverWindow as SceneView != null) ||
                                             (EditorWindow.focusedWindow as SceneView != null);

                    if (!isSceneViewActive) return false;
                    break;

                case DevExecutionContext.Always:
                    break;
            }
#endif
            return true;
        }
    }
}