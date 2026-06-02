using UnityEngine;

/// <summary>
/// Congela y restaura los controladores de cámara durante la inspección de una mariposa.
/// Auto-detecta ButterflyUserControl (Main Camera) y CameraControl si existen.
/// </summary>
public class CameraManager : MonoBehaviour
{
    [Header("Auto-detectado en Start si está vacío")]
    public ButterflyUserControl playerCameraControl;
    public CameraControl        legacyCameraControl;

    private bool _frozen;
    private bool _frozenPlayerWas;
    private bool _frozenLegacyWas;

    // ─────────────────────────────────────────────────────────────────

    private void Start()
    {
        if (playerCameraControl == null)
            playerCameraControl = FindFirstObjectByType<ButterflyUserControl>();
        if (legacyCameraControl == null)
            legacyCameraControl = FindFirstObjectByType<CameraControl>();
    }

    public void FreezeForInspect()
    {
        if (_frozen) return;
        _frozen = true;

        if (playerCameraControl != null)
        {
            _frozenPlayerWas            = playerCameraControl.enabled;
            playerCameraControl.enabled = false;
        }
        if (legacyCameraControl != null)
        {
            _frozenLegacyWas            = legacyCameraControl.enabled;
            legacyCameraControl.enabled = false;
        }
    }

    public void UnfreezeForInspect()
    {
        if (!_frozen) return;
        _frozen = false;

        if (playerCameraControl != null)
            playerCameraControl.enabled = _frozenPlayerWas;
        if (legacyCameraControl != null)
            legacyCameraControl.enabled = _frozenLegacyWas;
    }
}
