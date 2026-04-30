using UnityEngine;

/// <summary>
/// Controla las animaciones del prefab de la mariposa.
/// Adjunta al mismo GameObject raiz que ButterflyController.
///
/// SETUP:
///   1. El prefab debe tener un Animator con los clips cargados.
///   2. En el Animator Controller crea los parametros que uses
///      (ver enum ButterflyAnimation abajo).
///   3. Llama a PlayAnimation() desde cualquier otro script.
///
/// PARAMETROS REQUERIDOS EN EL ANIMATOR CONTROLLER:
///   - "IsFlying"   (Bool)
///   - "IsPasive"  (Bool)  
///   - "IsLanding"  (Bool)
///   - "FlapSpeed"  (Float) — multiplica la velocidad del clip de aleteo
/// </summary>
[RequireComponent(typeof(Animator))]
public class ButterflyAnimator : MonoBehaviour
{
    // ── Enum con todas las animaciones disponibles ─────────────────
    public enum ButterflyAnimation
    {
        Flying,
        Passive,
        Preview,
        Idle
    }

    // ── Hashes de parametros del Animator (mas eficiente que strings) ──
    private static readonly int HashIsFlying = Animator.StringToHash("IsFlying");
    private static readonly int HashFlapSpeed = Animator.StringToHash("FlapSpeed");
    private static readonly int HashIsPreview = Animator.StringToHash("IsPreview");

    // ── Componentes ────────────────────────────────────────────────
    private Animator _animator;

    // ── Estado actual ──────────────────────────────────────────────
    private ButterflyAnimation _currentAnimation = ButterflyAnimation.Idle;
    public ButterflyAnimation CurrentAnimation => _currentAnimation;

    // ═══════════════════════════════════════════════════════════════

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    // ───────────────────────────────────────────────────────────────
    // API PUBLICA — reproduce una animacion por nombre de enum
    // ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Cambia la animacion activa.
    /// Ejemplo: butterflyAnimator.PlayAnimation(ButterflyAnimation.LayingEgg);
    /// </summary>
    public void PlayAnimation(ButterflyAnimation animation)
    {
        if (_currentAnimation == animation) return;
        _currentAnimation = animation;

        ResetAllBools();

        // Activa solo el bool correspondiente
        switch (animation)
        {
            case ButterflyAnimation.Flying:
                _animator.SetBool(HashIsFlying, true);
                break;

            case ButterflyAnimation.Passive:
                break;

            case ButterflyAnimation.Preview:
                _animator.SetBool(HashIsPreview, true);
                break;


            case ButterflyAnimation.Idle:
                // Todos los bools en false → el Animator vuelve al estado base
                break;
        }
    }

    /// <summary>
    /// Ajusta la velocidad del clip de aleteo segun la velocidad de vuelo.
    /// Llama esto desde ButterflyController cuando cambia la velocidad.
    /// </summary>
    public void SetFlapSpeed(float speed)
    {
        _animator.SetFloat(HashFlapSpeed, speed);
    }

    /// <summary>
    /// Pausa o reanuda todas las animaciones.
    /// </summary>
    public void SetPaused(bool paused)
    {
        _animator.speed = paused ? 0f : 1f;
    }

    // ── Apaga todos los parametros bool ───────────────────────────
    private void ResetAllBools()
    {
        _animator.SetBool(HashIsFlying, false);
        _animator.SetBool(HashIsPreview, false);
    }
}