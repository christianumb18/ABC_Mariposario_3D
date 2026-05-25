using UnityEngine;

/// <summary>
/// Controla la camara del minimapa.
/// Adjunta este script a la GameObject "MinimapCamera" en la escena del mariposario.
///
/// FUNCIONAMIENTO:
///   - Sigue a la mariposa activa desde una altura fija.
///   - Rota con la mariposa para que esta siempre apunte hacia arriba en el minimapa.
///   - Recibe el target via SetTarget(), igual que ButterflyUserControl.
///
/// SETUP:
///   1. Adjunta este script a "MinimapCamera".
///   2. Configura cameraHeight segun la altura del mariposario.
///   3. El MariposarioSpawner debe llamar a SetTarget() despues de instanciar la mariposa.
/// </summary>
public class MinimapController : MonoBehaviour
{
    [Header("Altura sobre la mariposa")]
    [Tooltip("Distancia vertical desde la mariposa hasta la camara del minimapa")]
    public float cameraHeight = 60f;

    [Header("Suavizado")]
    [Tooltip("Que tan rapido la camara sigue a la mariposa (mayor = mas rapido)")]
    [Range(1f, 20f)] public float followSmoothing = 10f;

    [Tooltip("Que tan rapido la camara rota con la mariposa")]
    [Range(1f, 20f)] public float rotationSmoothing = 8f;

    [Header("Rotacion")]
    [Tooltip("Si esta activado, el minimapa rota con la mariposa. " +
             "Si esta desactivado, el norte siempre apunta hacia arriba.")]
    public bool rotateWithButterfly = true;

    // ── Estado ────────────────────────────────────────────────────
    private Transform _target;

    // ═══════════════════════════════════════════════════════════════

    private void LateUpdate()
    {
        if (_target == null) return;

        FollowTarget();
        RotateWithTarget();
    }

    // ── Sigue la posicion de la mariposa ──────────────────────────
    private void FollowTarget()
    {
        // La camara se mantiene exactamente arriba de la mariposa,
        // sin importar la altura de vuelo
        Vector3 desiredPos = new Vector3(
            _target.position.x,
            _target.position.y + cameraHeight,
            _target.position.z
        );

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPos,
            Time.deltaTime * followSmoothing
        );
    }

    // ── Rota la camara con la mariposa ────────────────────────────
    private void RotateWithTarget()
    {
        // X siempre 90 para mirar hacia abajo.
        // Y se alinea con la mariposa para que el "frente" de la mariposa
        // siempre quede hacia arriba en el minimapa.
        // Z siempre 0.
        float targetYaw = rotateWithButterfly ? _target.eulerAngles.y : 0f;

        Quaternion desiredRot = Quaternion.Euler(90f, targetYaw, 0f);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            desiredRot,
            Time.deltaTime * rotationSmoothing
        );
    }

    // ── API publica ───────────────────────────────────────────────

    /// <summary>
    /// Asigna la mariposa que el minimapa debe seguir.
    /// Llamar desde MariposarioSpawner despues de instanciar la mariposa.
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        _target = newTarget;

        // Posiciona la camara inmediatamente sobre el target para evitar
        // que arranque "viajando" desde su posicion inicial al primer frame
        if (_target != null)
        {
            transform.position = new Vector3(
                _target.position.x,
                _target.position.y + cameraHeight,
                _target.position.z
            );
        }
    }

    /// <summary>Limpia el target. Util cuando se destruye la mariposa.</summary>
    public void ClearTarget() => _target = null;
}