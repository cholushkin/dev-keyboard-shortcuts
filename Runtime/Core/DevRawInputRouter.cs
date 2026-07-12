using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GameLib
{
    /// Crash-proof engine with double-click detection, held key tracking for modifiers, and self-healing watchdog.
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public static class DevRawInputRouter
    {
        private struct DeviceKey : IEquatable<DeviceKey>
        {
            public string deviceName;
            public int vkCode;

            public DeviceKey(string deviceName, int vkCode)
            {
                this.deviceName = deviceName ?? "";
                this.vkCode = vkCode;
            }

            public bool Equals(DeviceKey other) => vkCode == other.vkCode && string.Equals(deviceName, other.deviceName, StringComparison.OrdinalIgnoreCase);
            public override int GetHashCode() => (deviceName.ToLowerInvariant().GetHashCode() * 397) ^ vkCode;
        }

        private static IntPtr hookHandle = IntPtr.Zero;
        private static IntPtr registeredWindowHandle = IntPtr.Zero;
        private static RawInputWin32.HookProc hookProc;
        private static readonly HashSet<DeviceKey> pressedKeys = new HashSet<DeviceKey>();
        private static readonly Dictionary<DeviceKey, double> lastPressTimes = new Dictionary<DeviceKey, double>();
        private static double lastWatchdogTime = 0;
        private static bool isTearingDown = false;
        private const double DoubleClickThreshold = 0.2;

        /// Fired whenever any physical keyboard key is pressed.
        /// Parameters: virtualKeyCode, hardwareId, deviceFriendlyName, isDoubleClick
        public static event Action<int, string, string, bool> OnRawKeyPressed;

#if UNITY_EDITOR
        static DevRawInputRouter()
        {
            isTearingDown = false;
            EditorApplication.delayCall += Initialize;
            
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeReload;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterReload;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.quitting += OnQuitting;
            
            EditorApplication.update -= WatchdogTick;
            EditorApplication.update += WatchdogTick;
        }

        private static void WatchdogTick()
        {
            if (isTearingDown || EditorApplication.isCompiling || EditorApplication.isUpdating) return;

            if (EditorApplication.timeSinceStartup - lastWatchdogTime > 1.5)
            {
                lastWatchdogTime = EditorApplication.timeSinceStartup;
                VerifyAndHealHook();
            }
        }

        private static void OnBeforeReload()
        {
            isTearingDown = true;
            Cleanup();
        }

        private static void OnAfterReload()
        {
            isTearingDown = false;
            Initialize();
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.ExitingPlayMode)
            {
                Cleanup();
            }
            else if (state == PlayModeStateChange.EnteredEditMode || state == PlayModeStateChange.EnteredPlayMode)
            {
                isTearingDown = false;
                Initialize();
            }
        }

        private static void OnQuitting()
        {
            isTearingDown = true;
            Cleanup();
        }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnGameStart()
        {
            isTearingDown = false;
            Initialize();
            Application.quitting += Cleanup;
        }

        /// Checks if a specific virtual key code is currently held down on a matching hardware device.
        public static bool IsKeyCurrentlyHeld(int vkCode, string hardwareIdSubstring)
        {
            foreach (var item in pressedKeys)
            {
                if (item.vkCode == vkCode)
                {
                    if (string.IsNullOrEmpty(hardwareIdSubstring)) return true;
                    if (item.deviceName.IndexOf(hardwareIdSubstring, StringComparison.OrdinalIgnoreCase) >= 0) return true;
                }
            }
            return false;
        }

        private static void VerifyAndHealHook()
        {
            if (isTearingDown) return;

            if (hookHandle == IntPtr.Zero)
            {
                Debug.LogWarning("[DevRawInputRouter Watchdog] Hook handle is zero! Performing auto-recovery...");
                Initialize();
                return;
            }

            IntPtr currentWindow = GetUnityWindowHandle();
            if (currentWindow != IntPtr.Zero && currentWindow != registeredWindowHandle)
            {
                Debug.LogWarning("[DevRawInputRouter Watchdog] Target window handle changed! Re-registering Raw Input sink...");
                Cleanup();
                Initialize();
            }
        }

        private static void Initialize()
        {
            if (isTearingDown) return;
            if (hookHandle != IntPtr.Zero) return;

            // Touch DevInputMap to refresh active modular maps and check console debugging flags
            DevInputMap.ReloadActiveMaps();
            bool isDebug = DevInputMap.IsAnyDebugEnabled();

            IntPtr targetWindow = GetUnityWindowHandle();
            if (targetWindow == IntPtr.Zero)
            {
#if UNITY_EDITOR
                EditorApplication.delayCall += Initialize;
#endif
                return;
            }

            var rid = new RawInputWin32.RAWINPUTDEVICE[1];
            rid[0].usUsagePage = RawInputWin32.HID_USAGE_PAGE_GENERIC;
            rid[0].usUsage = RawInputWin32.HID_USAGE_KEYBOARD;
            rid[0].dwFlags = RawInputWin32.RIDEV_INPUTSINK;
            rid[0].hwndTarget = targetWindow;

            if (!RawInputWin32.RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(typeof(RawInputWin32.RAWINPUTDEVICE))))
            {
                int errorCode = Marshal.GetLastWin32Error();
                Debug.LogError($"[DevRawInputRouter] Failed to register Windows Raw Input devices. Win32 Error Code: {errorCode}");
                return;
            }

            registeredWindowHandle = targetWindow;
            if (isDebug) Debug.Log($"[DevRawInputRouter] Raw Input successfully registered to Window Handle: 0x{targetWindow.ToInt64():X}");

            hookProc = HookCallback;
            uint threadId = RawInputWin32.GetCurrentThreadId();
            hookHandle = RawInputWin32.SetWindowsHookEx(RawInputWin32.WH_GETMESSAGE, hookProc, IntPtr.Zero, threadId);

            if (hookHandle == IntPtr.Zero)
            {
                int errorCode = Marshal.GetLastWin32Error();
                Debug.LogError($"[DevRawInputRouter] Failed to install Windows hook. Win32 Error Code: {errorCode}");
            }
            else if (isDebug)
            {
                Debug.Log($"[DevRawInputRouter] Win32 Hook successfully installed on Thread ID: {threadId} (Hook Handle: 0x{hookHandle.ToInt64():X})");
            }
        }

        private static IntPtr GetUnityWindowHandle()
        {
            try
            {
                IntPtr handle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
                if (handle != IntPtr.Zero) return handle;
            }
            catch (Exception) { }

            IntPtr activeHandle = RawInputWin32.GetActiveWindow();
            if (activeHandle != IntPtr.Zero) return activeHandle;

            return RawInputWin32.GetForegroundWindow();
        }

        private static void Cleanup()
        {
            if (hookHandle != IntPtr.Zero)
            {
                if (DevInputMap.IsAnyDebugEnabled())
                {
                    Debug.Log("[DevRawInputRouter] Cleaning up Win32 Hook...");
                }
                RawInputWin32.UnhookWindowsHookEx(hookHandle);
                hookHandle = IntPtr.Zero;
                registeredWindowHandle = IntPtr.Zero;
                pressedKeys.Clear();
            }
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                if (nCode >= 0 && lParam != IntPtr.Zero)
                {
                    var msg = Marshal.PtrToStructure<RawInputWin32.MSG>(lParam);
                    if (msg.message == RawInputWin32.WM_INPUT)
                    {
                        ProcessRawInputMessage(msg.lParam);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[DevRawInputRouter] Suppressed fatal crash in Win32 Hook: {e}");
            }

            return RawInputWin32.CallNextHookEx(hookHandle, nCode, wParam, lParam);
        }

        private static void ProcessRawInputMessage(IntPtr hRawInput)
        {
            uint dwSize = 0;
            uint headerSize = (uint)Marshal.SizeOf(typeof(RawInputWin32.RAWINPUTHEADER));

            RawInputWin32.GetRawInputData(hRawInput, RawInputWin32.RID_INPUT, IntPtr.Zero, ref dwSize, headerSize);
            if (dwSize < headerSize) return;

            IntPtr buffer = Marshal.AllocHGlobal((int)dwSize);
            try
            {
                if (RawInputWin32.GetRawInputData(hRawInput, RawInputWin32.RID_INPUT, buffer, ref dwSize, headerSize) == dwSize)
                {
                    var rawInput = Marshal.PtrToStructure<RawInputWin32.RAWINPUT>(buffer);
                    
                    if (rawInput.header.dwType == 1) // 1 = RIM_TYPEKEYBOARD
                    {
                        int vkCode = rawInput.keyboard.VKey;
                        bool isKeyUp = (rawInput.keyboard.Flags & RawInputWin32.RI_KEY_BREAK) != 0;
                        string deviceName = DevRawInputDevices.GetDeviceName(rawInput.header.hDevice);
                        var devKey = new DeviceKey(deviceName, vkCode);

                        if (!isKeyUp)
                        {
                            if (!pressedKeys.Contains(devKey))
                            {
                                pressedKeys.Add(devKey);

#if UNITY_EDITOR
                                double now = UnityEditor.EditorApplication.timeSinceStartup;
#else
                                double now = UnityEngine.Time.realtimeSinceStartupAsDouble;
#endif
                                bool isDoubleClick = false;
                                if (lastPressTimes.TryGetValue(devKey, out double lastTime))
                                {
                                    if (now - lastTime < DoubleClickThreshold)
                                    {
                                        isDoubleClick = true;
                                        lastPressTimes[devKey] = 0.0;
                                    }
                                    else
                                    {
                                        lastPressTimes[devKey] = now;
                                    }
                                }
                                else
                                {
                                    lastPressTimes[devKey] = now;
                                }

                                if (DevInputMap.IsAnyDebugEnabled())
                                {
                                    Debug.Log($"[DevRawInputRouter] Intercepted Key Press -> VK: {vkCode} | Device: '{deviceName}' | DoubleClick: {isDoubleClick}");
                                }

                                OnRawKeyPressed?.Invoke(vkCode, deviceName, deviceName, isDoubleClick);

                                // Broadcast to all active modular maps (overridden maps are excluded automatically!)
                                DevInputMap.ProcessAllRawKeyPresses(vkCode, deviceName, isDoubleClick);
                            }
                        }
                        else
                        {
                            pressedKeys.Remove(devKey);
                        }
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
    }
}