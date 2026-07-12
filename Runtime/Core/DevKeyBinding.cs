using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameLib
{
    /// Represents a modifier key that must be held down simultaneously on a specific hardware device.
    [Serializable]
    public struct DevKeyModifier
    {
        [Tooltip("Paste the USB Hardware ID or unique substring for the modifier key. Leave empty to match ANY keyboard.")]
        public string deviceHardwareId;

        [Tooltip("Virtual Key (VK) code in decimal for the modifier key (e.g., 162 for Left Ctrl, 160 for Left Shift, 98 for Numpad 2).")]
        public int virtualKeyCode;

        /// Checks if this specific modifier key is currently held down on the target hardware device.
        public bool IsHeld()
        {
            if (virtualKeyCode <= 0) return true;
            return DevRawInputRouter.IsKeyCurrentlyHeld(virtualKeyCode, deviceHardwareId);
        }
    }

    /// Represents a mapping between a physical keyboard, a key, optional modifiers, and an executable tool.
    [Serializable]
    public struct DevKeyBinding
    {
        [Tooltip("Enable or disable this shortcut without deleting it.")]
        public bool isEnabled;

        [Tooltip("If checked, this shortcut will only trigger on a double-click of the key.")]
        public bool requireDoubleClick;

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

        /// Checks if an incoming key press matches this specific binding, including modifiers and double-click state.
        public bool Matches(int vkCode, string incomingHardwareId, bool isDoubleClick)
        {
            if (!isEnabled || boundTool == null) return false;
            if (this.virtualKeyCode != vkCode) return false;
            if (this.requireDoubleClick != isDoubleClick) return false;

            if (!string.IsNullOrEmpty(deviceHardwareId))
            {
                if (string.IsNullOrEmpty(incomingHardwareId) || 
                    !incomingHardwareId.IndexOf(deviceHardwareId, StringComparison.OrdinalIgnoreCase).Equals(-1))
                {
                    // Matches hardware substring
                }
                else
                {
                    return false;
                }
            }

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
    }
}