using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
#endif

public class CameraZoomController : MonoBehaviour
{
    public float minFov = 15f;
    public float maxFov = 60f;
    public float zoomSpeedMouse = 10f;
    public float zoomSpeedTouch = 0.1f;

    private Camera cam;

    private void Awake() => cam = GetComponent<Camera>();

    private void OnEnable()
    {
#if ENABLE_INPUT_SYSTEM
        EnhancedTouchSupport.Enable();
#endif
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        // Zoom con rueda del raton (PC/Mac)
        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                cam.fieldOfView = Mathf.Clamp(cam.fieldOfView - (scroll * 0.01f * zoomSpeedMouse), minFov, maxFov);
            }
        }

        // Pinch to Zoom (Movil)
        if (UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count == 2)
        {
            var touch0 = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches[0];
            var touch1 = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches[1];

            Vector2 touch0PrevPos = touch0.screenPosition - touch0.delta;
            Vector2 touch1PrevPos = touch1.screenPosition - touch1.delta;

            float prevMagnitude = (touch0PrevPos - touch1PrevPos).magnitude;
            float currentMagnitude = (touch0.screenPosition - touch1.screenPosition).magnitude;
            float difference = prevMagnitude - currentMagnitude;

            cam.fieldOfView = Mathf.Clamp(cam.fieldOfView + (difference * zoomSpeedTouch), minFov, maxFov);
        }
#endif
    }
}