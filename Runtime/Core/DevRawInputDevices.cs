using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace GameLib
{
    /// Helper class to safely query hardware device names without heap corruption or GC spikes.
    public static class DevRawInputDevices
    {
        private struct DeviceInfo
        {
            public string rawPath;
            public string cleanName;
        }

        private static readonly Dictionary<IntPtr, DeviceInfo> _deviceCache = new Dictionary<IntPtr, DeviceInfo>();

        public static string GetDeviceHandleString(IntPtr hDevice)
        {
            return $"0x{hDevice.ToInt64():X8}";
        }

        public static string GetDeviceName(IntPtr hDevice)
        {
            GetDeviceNames(hDevice, out _, out string cleanName);
            return cleanName;
        }

        /// Retrieves both the uncleaned raw hardware path and the cleaned friendly name, utilizing a static cache.
        public static void GetDeviceNames(IntPtr hDevice, out string rawPath, out string cleanName)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (hDevice == IntPtr.Zero)
            {
                rawPath = "System";
                cleanName = "System/Synthetic Keyboard";
                return;
            }

            if (_deviceCache.TryGetValue(hDevice, out DeviceInfo cached))
            {
                rawPath = cached.rawPath;
                cleanName = cached.cleanName;
                return;
            }

            uint charCount = 0;
            RawInputWin32.GetRawInputDeviceInfo(hDevice, RawInputWin32.RIDI_DEVICENAME, IntPtr.Zero, ref charCount);
            
            if (charCount == 0)
            {
                rawPath = "Unknown";
                cleanName = "Unknown Device";
                return;
            }

            int byteSize = (int)charCount * sizeof(char);
            IntPtr pData = Marshal.AllocHGlobal(byteSize);
            try
            {
                if (RawInputWin32.GetRawInputDeviceInfo(hDevice, RawInputWin32.RIDI_DEVICENAME, pData, ref charCount) > 0)
                {
                    rawPath = Marshal.PtrToStringAuto(pData) ?? "Unknown";
                    cleanName = CleanDevicePath(rawPath);
                    _deviceCache[hDevice] = new DeviceInfo { rawPath = rawPath, cleanName = cleanName };
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DevRawInputDevices] Failed to read device name for {GetDeviceHandleString(hDevice)}: {e.Message}");
            }
            finally
            {
                Marshal.FreeHGlobal(pData);
            }
#endif
            rawPath = "Unsupported_Platform";
            cleanName = "Unsupported Platform Keyboard";
        }

        private static string CleanDevicePath(string rawPath)
        {
            if (string.IsNullOrEmpty(rawPath)) return "Unknown Device";
            if (rawPath.Contains("VID_") && rawPath.Contains("PID_"))
            {
                int vidIndex = rawPath.IndexOf("VID_");
                int length = Math.Min(17, rawPath.Length - vidIndex);
                return $"USB Keyboard ({rawPath.Substring(vidIndex, length)})";
            }
            return rawPath;
        }
    }
}