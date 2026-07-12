using UnityEditor;
using UnityEngine;

namespace GameLib
{
    /// Controls Unity Scene View camera navigation with authentic Blender numpad shortcuts and parity.
    [CreateAssetMenu(menuName = "GameLib/Debug/DevKeyboardShortcuts/DevTools/Blender Camera Nav Tool", fileName = "BlenderCameraNavTool")]
    public class BlenderCameraNavDevTool : DevActionTool
    {
        public enum CameraNavAction
        {
            // Standard Orthographic Views
            ViewTop,             // Numpad 7
            ViewBottom,          // Ctrl + Numpad 7
            ViewFront,           // Numpad 1
            ViewBack,            // Ctrl + Numpad 1
            ViewRight,           // Numpad 3
            ViewLeft,            // Ctrl + Numpad 3
            ViewFlip180,         // Numpad 9 (Orbit 180° to opposite side) 

            // Projection & Focus & Isolation
            TogglePerspOrtho,    // Numpad 5
            FrameSelected,       // Numpad . (Period)
            ToggleIsolation,     // Numpad / (Slash - Local / Global View toggle)

            // Active Camera Control
            LookThroughActiveCamera,   // Numpad 0
            AlignActiveCameraToView,   // Ctrl + Alt + Numpad 0

            // Orbiting (Numpad 4, 6, 8, 2)
            OrbitLeft,
            OrbitRight,
            OrbitUp,
            OrbitDown,

            // Panning (Ctrl + Numpad 4, 6, 8, 2)
            PanLeft,
            PanRight,
            PanUp,
            PanDown,

            // Zooming (Numpad +, -)
            ZoomIn,
            ZoomOut
        }

        [Header("Action Configuration")]
        [Tooltip("The specific Blender camera navigation action this tool asset will execute.")]
        public CameraNavAction actionType = CameraNavAction.ViewFront;

        [Header("Navigation Sensitivity")]
        [Tooltip("Angle in degrees to rotate the camera when using Orbit keys (4, 6, 8, 2).")]
        public float orbitStepAngle = 15.0f;

        [Tooltip("Multiplier for panning speed relative to current viewport zoom size.")]
        public float panStepMultiplier = 0.1f;

        [Tooltip("Percentage to zoom in or out per step relative to current viewport zoom size.")]
        public float zoomStepMultiplier = 0.15f;

        /// Executes the selected camera navigation action on the active Scene View.
        public override void Execute()
        {
            SceneView sceneView = GetActiveSceneView();
            if (sceneView == null)
            {
                Debug.LogWarning("[BlenderCameraNav] No active Scene View found to navigate.");
                return;
            }

            switch (actionType)
            {
                // Standard Orthographic Views
                case CameraNavAction.ViewTop:
                    SetViewOrientation(sceneView, Vector3.down, Vector3.forward);
                    break;

                case CameraNavAction.ViewBottom:
                    SetViewOrientation(sceneView, Vector3.up, Vector3.forward);
                    break;

                case CameraNavAction.ViewFront:
                    SetViewOrientation(sceneView, Vector3.forward, Vector3.up);
                    break;

                case CameraNavAction.ViewBack:
                    SetViewOrientation(sceneView, Vector3.back, Vector3.up);
                    break;

                case CameraNavAction.ViewRight:
                    SetViewOrientation(sceneView, Vector3.left, Vector3.up);
                    break;

                case CameraNavAction.ViewLeft:
                    SetViewOrientation(sceneView, Vector3.right, Vector3.up);
                    break;

                case CameraNavAction.ViewFlip180:
                    OrbitCamera(sceneView, 180f, 0f);
                    sceneView.ShowNotification(new GUIContent("View Flipped 180°"));
                    break;

                // Projection & Focus & Isolation
                case CameraNavAction.TogglePerspOrtho:
                    sceneView.orthographic = !sceneView.orthographic;
                    sceneView.ShowNotification(new GUIContent(sceneView.orthographic ? "Orthographic" : "Perspective"));
                    break;

                case CameraNavAction.FrameSelected:
                    sceneView.FrameSelected();
                    break;

                case CameraNavAction.ToggleIsolation:
                    ToggleLocalViewIsolation(sceneView);
                    break;

                // Active Camera Control
                case CameraNavAction.LookThroughActiveCamera:
                    LookThroughCamera(sceneView);
                    break;

                case CameraNavAction.AlignActiveCameraToView:
                    AlignCameraToSceneView(sceneView);
                    break;

                // Orbiting
                case CameraNavAction.OrbitLeft:
                    OrbitCamera(sceneView, -orbitStepAngle, 0f);
                    break;

                case CameraNavAction.OrbitRight:
                    OrbitCamera(sceneView, orbitStepAngle, 0f);
                    break;

                case CameraNavAction.OrbitUp:
                    OrbitCamera(sceneView, 0f, orbitStepAngle);
                    break;

                case CameraNavAction.OrbitDown:
                    OrbitCamera(sceneView, 0f, -orbitStepAngle);
                    break;

                // Panning
                case CameraNavAction.PanLeft:
                    PanCamera(sceneView, -panStepMultiplier, 0f);
                    break;

                case CameraNavAction.PanRight:
                    PanCamera(sceneView, panStepMultiplier, 0f);
                    break;

                case CameraNavAction.PanUp:
                    PanCamera(sceneView, 0f, panStepMultiplier);
                    break;

                case CameraNavAction.PanDown:
                    PanCamera(sceneView, 0f, -panStepMultiplier);
                    break;

                // Zooming
                case CameraNavAction.ZoomIn:
                    ZoomCamera(sceneView, -zoomStepMultiplier);
                    break;

                case CameraNavAction.ZoomOut:
                    ZoomCamera(sceneView, zoomStepMultiplier);
                    break;
            }

            // Force an immediate redraw of the Scene View viewport
            sceneView.Repaint();
        }

        /// Snaps the camera to a standard axis view while preserving pivot and zoom size.
        private static void SetViewOrientation(SceneView view, Vector3 lookDirection, Vector3 upDirection)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection, upDirection);
            view.LookAt(view.pivot, targetRotation, view.size, view.orthographic);
            view.ShowNotification(new GUIContent(GetViewName(lookDirection)));
        }

        /// Rotates the camera around its current pivot point by specified yaw and pitch angles.
        private static void OrbitCamera(SceneView view, float deltaYaw, float deltaPitch)
        {
            Quaternion currentRotation = view.rotation;

            // Yaw rotates around the world Up axis (Vector3.up)
            Quaternion yawRotation = Quaternion.AngleAxis(deltaYaw, Vector3.up);
            currentRotation = yawRotation * currentRotation;

            // Pitch rotates around the camera's local Right axis
            Vector3 localRight = currentRotation * Vector3.right;
            Quaternion pitchRotation = Quaternion.AngleAxis(deltaPitch, localRight);
            currentRotation = pitchRotation * currentRotation;

            view.LookAt(view.pivot, currentRotation, view.size, view.orthographic);
        }

        /// Pans the Scene View pivot along the camera's local X/Y plane proportionally to zoom distance.
        private static void PanCamera(SceneView view, float deltaX, float deltaY)
        {
            float stepDistance = view.size;
            Vector3 right = view.camera.transform.right;
            Vector3 up = view.camera.transform.up;

            Vector3 newPivot = view.pivot + (right * deltaX * stepDistance) + (up * deltaY * stepDistance);
            view.LookAt(newPivot, view.rotation, view.size, view.orthographic);
        }

        /// Zooms the Scene View in or out by scaling the view size.
        private static void ZoomCamera(SceneView view, float zoomDeltaMultiplier)
        {
            float newSize = view.size * (1.0f + zoomDeltaMultiplier);
            newSize = Mathf.Clamp(newSize, 0.001f, 100000f);
            view.LookAt(view.pivot, view.rotation, newSize, view.orthographic);
        }

        /// Toggles object isolation using Unity's native SceneVisibilityManager (like Blender's Numpad /).
        private static void ToggleLocalViewIsolation(SceneView view)
        {
            var visMgr = SceneVisibilityManager.instance;
            if (visMgr == null) return;

            // CRITICAL FIX: Use IsCurrentStageIsolated() instead of IsIsolated()
            if (visMgr.IsCurrentStageIsolated())
            {
                visMgr.ExitIsolation();
                view.ShowNotification(new GUIContent("Global View"));
            }
            else
            {
                if (Selection.gameObjects != null && Selection.gameObjects.Length > 0)
                {
                    visMgr.Isolate(Selection.gameObjects, true);
                    view.ShowNotification(new GUIContent("Local View (Isolated Selected)"));
                }
                else
                {
                    view.ShowNotification(new GUIContent("Select an Object to Isolate!"));
                }
            }
        }

        /// Snaps Scene View to look through the active or main camera (like Blender's Numpad 0).
        private static void LookThroughCamera(SceneView view)
        {
            Camera targetCam = Selection.activeGameObject?.GetComponent<Camera>() ?? Camera.main;
            if (targetCam == null)
            {
                targetCam = Object.FindFirstObjectByType<Camera>();
            }

            if (targetCam != null)
            {
                view.AlignViewToObject(targetCam.transform);
                view.ShowNotification(new GUIContent($"Camera View ({targetCam.name})"));
            }
            else
            {
                view.ShowNotification(new GUIContent("No Camera Found in Scene!"));
            }
        }

        /// Aligns the actual game camera to match the current Scene View perspective (like Ctrl + Alt + Numpad 0).
        private static void AlignCameraToSceneView(SceneView view)
        {
            Camera targetCam = Selection.activeGameObject?.GetComponent<Camera>() ?? Camera.main;
            if (targetCam == null)
            {
                targetCam = Object.FindFirstObjectByType<Camera>();
            }

            if (targetCam != null)
            {
                Undo.RecordObject(targetCam.transform, "Align Camera to View");
                targetCam.transform.position = view.camera.transform.position;
                targetCam.transform.rotation = view.camera.transform.rotation;
                view.ShowNotification(new GUIContent($"Aligned '{targetCam.name}' to View"));
                Debug.Log($"[BlenderCameraNav] Successfully aligned camera '{targetCam.name}' to current Scene View position/rotation.");
            }
            else
            {
                view.ShowNotification(new GUIContent("No Camera Found to Align!"));
            }
        }

        /// Safely retrieves the last active Scene View or fallbacks to the first available one.
        private static SceneView GetActiveSceneView()
        {
            if (SceneView.lastActiveSceneView != null)
                return SceneView.lastActiveSceneView;

            if (SceneView.sceneViews.Count > 0)
                return (SceneView)SceneView.sceneViews[0];

            return null;
        }

        private static string GetViewName(Vector3 dir)
        {
            if (dir == Vector3.down) return "Top View";
            if (dir == Vector3.up) return "Bottom View";
            if (dir == Vector3.forward) return "Front View";
            if (dir == Vector3.back) return "Back View";
            if (dir == Vector3.left) return "Right View";
            if (dir == Vector3.right) return "Left View";
            return "Custom View";
        }
    }
}