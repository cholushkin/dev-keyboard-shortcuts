using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace GameLib
{
    /// Helper class to safely query hardware device names without heap corruption.
    public static class DevRawInputDevices
    {
        public static string GetDeviceHandleString(IntPtr hDevice)
        {
            return $"0x{hDevice.ToInt64():X8}";
        }

        public static string GetDeviceName(IntPtr hDevice)
        {
            if (hDevice == IntPtr.Zero) return "System/Synthetic Keyboard";

            uint charCount = 0;
            // 1. Query required character count (including null terminator)
            RawInputWin32.GetRawInputDeviceInfo(hDevice, RawInputWin32.RIDI_DEVICENAME, IntPtr.Zero, ref charCount);

            if (charCount == 0) return "Unknown Device";

            // CRITICAL FIX: Multiply by sizeof(char) (2 bytes in C# / Win32 Unicode) to prevent heap overflow!
            int byteSize = (int)charCount * sizeof(char);
            IntPtr pData = Marshal.AllocHGlobal(byteSize);
            try
            {
                if (RawInputWin32.GetRawInputDeviceInfo(hDevice, RawInputWin32.RIDI_DEVICENAME, pData, ref charCount) > 0)
                {
                    string devicePath = Marshal.PtrToStringAuto(pData);
                    return CleanDevicePath(devicePath);
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

            return "Unknown USB Keyboard";
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