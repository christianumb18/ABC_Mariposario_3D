using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public enum GameState
{
    Exploring,
    ApproachingButterfly,
    Inspecting
}

/// <summary>
/// Coordinador del flujo de inspección de mariposas NPC.
/// Estados: Exploring → ApproachingButterfly → Inspecting → (botón Volver) → Exploring.
///
/// Durante Inspecting:
///   - Congela la cámara (FreezeForInspect en CameraManager).
///   - Oculta joystick, touchfield, personaje del jugador y otras NPCs.
///   - Muestra un botón "Volver" flotante en pantalla.
/// </summary>
public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    public GameState CurrentState { get; private set; } = GameState.Exploring;

    [Header("Sistemas")]
    public CameraManager       cameraManager;
    public ButterflyVideoPanel videoPanel;

    [Header("Personaje del jugador (se oculta al inspeccionar)")]
    [Tooltip("Si se deja vacío se autodetecta por CameraControl.target o tag 'Player'.")]
    public GameObject playerCharacter;

    [Header("Acercamiento de la mariposa")]
    [Tooltip("Duración fija del vuelo desde su posición actual hasta llegar (segundos)")]
    public float approachDuration   = 2.0f;
    public float approachDistance   = 2.0f;
    public float approachUpOffset   = 0f;
    [Tooltip("Cuánto a la IZQUIERDA de la cámara queda la mariposa (m). Positivo = más a la izquierda.")]
    public float approachSideOffset = 1.2f;

    [Header("Datos de especies")]
    [Tooltip("Lista de ButterflySpeciesData — usada como fallback si el prefab no tiene ButterflyIdentity")]
    public ButterflyData[] speciesDataList;

    public ButterflyNPCBehavior  SelectedNPC         { get; private set; }
    public ButterflyData  SelectedSpeciesData { get; private set; }

    private Coroutine  _approachCoroutine;
    private Camera     _cam;

    private GameObject[]            _joystickGOs;
    private GameObject[]            _touchFieldGOs;
    private GameObject[]            _minimapGOs;
    private ButterflyNPCBehavior[]  _hiddenNPCs;
    private GameObject              _returnButton;

    // ─────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        _cam = Camera.main;

        _joystickGOs   = GameObjectsOfType<FixedJoystick>();
        _touchFieldGOs = GameObjectsOfType<FixedTouchField>();
        _minimapGOs    = CollectMinimapGOs();

        // Sanity check: descartar asignación incorrecta (NPC_Manager, una NPC, etc.)
        if (playerCharacter != null &&
            (playerCharacter.GetComponentInChildren<ButterflyNPCManager>() != null ||
             playerCharacter.GetComponent<ButterflyNPCBehavior>() != null))
        {
            Debug.LogWarning("[GameStateManager] playerCharacter apuntaba a un Manager/NPC. Se descarta para autodetectar.", this);
            playerCharacter = null;
        }

        // Filtro seguro: el "jugador" es un ButterflyController ACTIVO y SIN ButterflyNPCBehavior.
        // (Las 40 NPCs también tienen ButterflyController pero están disabled y con NPCBehavior.)
        if (playerCharacter == null)
        {
            var allCtrls = FindObjectsByType<ButterflyController>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var bc in allCtrls)
            {
                if (!bc.enabled) continue;
                if (bc.GetComponent<ButterflyNPCBehavior>() != null) continue;
                playerCharacter = bc.transform.root.gameObject;
                break;
            }
        }
        if (playerCharacter == null)
        {
            var cc = FindFirstObjectByType<CameraControl>();
            if (cc != null && cc.target != null)
            {
                var t = cc.target.root.gameObject;
                if (t.GetComponentInChildren<ButterflyNPCManager>() == null)
                    playerCharacter = t;
            }
        }
        if (playerCharacter == null)
            playerCharacter = GameObject.FindWithTag("Player");
    }

    // ── API pública ───────────────────────────────────────────────────

    public void SelectButterfly(ButterflyNPCBehavior npc)
    {
        if (CurrentState != GameState.Exploring) return;
        SelectedNPC = npc;

        // Prioridad: ButterflyIdentity en el prefab → fallback por nombre de especie
        SelectedSpeciesData = FindSpeciesData(npc.Data?.speciesName);

        TransitionTo(GameState.ApproachingButterfly);
    }

    private ButterflyData FindSpeciesData(string speciesName)
    {
        if (string.IsNullOrEmpty(speciesName) || speciesDataList == null) return null;
        return System.Array.Find(speciesDataList,
            sd => sd != null && sd.speciesName.Equals(speciesName, System.StringComparison.OrdinalIgnoreCase));
    }

    public void OnReturnToExploring()
    {
        if (CurrentState != GameState.Inspecting) return;
        // ── ButterflyAnimator ──────────────────────────────────────
        ButterflyAnimator _activeAnimator = SelectedNPC.GetComponent<ButterflyAnimator>();
        if (_activeAnimator != null)
        {
            // Activa la animacion de vuelo apenas aparece la mariposa
            _activeAnimator.PlayAnimation(ButterflyAnimator.ButterflyAnimation.Passive);
        }
        ReleaseNPC();
        TransitionTo(GameState.Exploring);
    }

    /// <summary>
    /// La mariposa inspeccionada se convierte en el nuevo personaje del jugador.
    /// Desactiva la mariposa anterior, activa el ButterflyController de la nueva,
    /// y redirige cámara, joystick y minimapa para que sigan a la nueva.
    /// </summary>
    public void OnChooseBecomeButterfly()
    {
        if (CurrentState != GameState.Inspecting || SelectedNPC == null) return;

        var npc           = SelectedNPC;
        var npcBehavior   = npc.GetComponent<ButterflyNPCBehavior>();
        var npcController = npc.GetComponent<ButterflyController>();

        // 1. Apagar comportamiento autónomo de la nueva mariposa
        if (npcBehavior != null) npcBehavior.enabled = false;

        // 1b. Restaurar el Rigidbody a modo "jugador" para que el controller pueda moverla
        var rb = npc.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity  = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 1c. Cambiar layer a "Butterfly" para que PreventCollisions del controller
        // excluya su propio collider en raycasts (si no, la mariposa se auto-detecta y se atasca).
        int butterflyLayer = LayerMask.NameToLayer("Butterfly");
        if (butterflyLayer >= 0)
        {
            foreach (Transform t in npc.GetComponentsInChildren<Transform>(includeInactive: true))
                t.gameObject.layer = butterflyLayer;
        }

        // 2. Activar el ButterflyController del nuevo personaje + inicializar datos
        if (npcController != null)
        {
            npcController.enabled = true;
            if (npcBehavior != null && npcBehavior.Data != null)
                npcController.Initialize(npcBehavior.Data);
        }

        // 3. Apagar el controller del personaje anterior (si tenía)
        if (playerCharacter != null && playerCharacter != npc.gameObject)
        {
            var prevCtrl = playerCharacter.GetComponentInChildren<ButterflyController>();
            if (prevCtrl != null) prevCtrl.enabled = false;
        }

        // 4. Redirigir el ButterflyUserControl (joystick + cámara orbital) a la nueva
        var userControl = FindFirstObjectByType<ButterflyUserControl>();
        if (userControl != null && npcController != null)
            userControl.SetTarget(npcController);

        // 5. Redirigir el CameraControl legacy si existe
        var cc = FindFirstObjectByType<CameraControl>();
        if (cc != null) cc.target = npc.transform;

        //// 6. Redirigir el minimapa (si su componente tiene SetTarget vía SendMessage)
        //var minimap = FindFirstObjectByType<MinimapController>();
        //if (minimap != null)
        //    minimap.SendMessage("SetTarget", npc.transform, SendMessageOptions.DontRequireReceiver);

        //// ── ButterflyAnimator ──────────────────────────────────────
        //ButterflyAnimator _activeAnimator = SelectedNPC.GetComponent<ButterflyAnimator>();
        //if (_activeAnimator != null)
        //{
        //    // Activa la animacion de vuelo apenas aparece la mariposa
        //    _activeAnimator.PlayAnimation(ButterflyAnimator.ButterflyAnimation.Flying);
        //}

        // 7. Marcar la mariposa nueva como el personaje del jugador
        var previousPlayer = playerCharacter;
        playerCharacter    = npc.gameObject;

        // 7b. Reasignar la ActiveButterfly del spawner para que HostPlant
        // valide la especie del nuevo personaje (sin esto, sigue comparando
        // contra la mariposa original oculta y la planta hospedera correcta
        // no se reconoce tras el cambio de personaje).
        var spawner = FindFirstObjectByType<MariposarioSpawner>();
        if (spawner != null && npc.Data != null)
            spawner.SetActiveButterfly(npc.Data);

        // 8. Resetear la selección (ya no es una NPC seleccionada)
        SelectedNPC         = null;
        SelectedSpeciesData = null;

        // 9. Ocultar el personaje anterior (no destruir, por si se quiere reactivar luego)
        if (previousPlayer != null && previousPlayer != npc.gameObject)
            previousPlayer.SetActive(false);

        // 10. Volver a Exploring — UI se restaura y ya controlas la nueva mariposa
        TransitionTo(GameState.Exploring);
    }

    // ── Máquina de estados ────────────────────────────────────────────

    private void TransitionTo(GameState next)
    {
        CurrentState = next;

        switch (next)
        {
            case GameState.ApproachingButterfly:
                // Congelar cámara y ocultar UI desde el inicio del approach
                // para que la mariposa persiga un target fijo.
                cameraManager?.FreezeForInspect();
                SetPlayerVisible(false);
                SetJoystickVisible(false);
                SetTouchFieldVisible(false);
                SetMinimapVisible(false);
                HideOtherNPCs(true);
                if (_approachCoroutine != null) StopCoroutine(_approachCoroutine);
                _approachCoroutine = StartCoroutine(ApproachCoroutine());
                break;

            case GameState.Inspecting:
                if (SelectedNPC != null)
                {
                    var rb = SelectedNPC.GetComponent<Rigidbody>();
                    if (rb != null) rb.constraints = RigidbodyConstraints.None;
                }
                videoPanel?.Show(SelectedSpeciesData);
                // ── ButterflyAnimator ──────────────────────────────────────
                ButterflyAnimator _activeAnimator = SelectedNPC.GetComponent<ButterflyAnimator>();
                if (_activeAnimator != null)
                {
                    // Activa la animacion de vuelo apenas aparece la mariposa
                    _activeAnimator.PlayAnimation(ButterflyAnimator.ButterflyAnimation.Preview);
                }
                ShowReturnButton(true);
                break;

            case GameState.Exploring:
                videoPanel?.Hide();
                HideOtherNPCs(false);
                cameraManager?.UnfreezeForInspect();
                SetPlayerVisible(true);
                SetJoystickVisible(true);
                SetTouchFieldVisible(true);
                SetMinimapVisible(true);
                ShowReturnButton(false);
                break;
        }
    }

    // ── Corrutina de acercamiento ─────────────────────────────────────

    private IEnumerator ApproachCoroutine()
    {
        var npc     = SelectedNPC;
        npc.enabled = false;

        var animator = npc.GetComponentInChildren<Animator>();
        if (animator != null) animator.applyRootMotion = false;

        if (_cam == null) _cam = Camera.main;
        Transform cam = _cam.transform;

        // Ancho del frustum a la distancia del target (basado en FOV)
        float halfHFovTan   = Mathf.Tan(_cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * _cam.aspect;
        float halfWidthAtZ  = halfHFovTan * approachDistance;

        // Target = en el cuarto IZQUIERDO de la pantalla (visible)
        Vector3 fixedTarget = cam.position
                            + cam.forward * approachDistance
                            - cam.right   * (halfWidthAtZ * 0.5f)
                            + cam.up      * approachUpOffset;

        // Start = posición ORIGINAL de la mariposa (donde el usuario la tocó)
        // — vuela desde ahí hasta el target a la izquierda.
        Vector3 startPos = npc.transform.position;

        Vector3    flightDir = (fixedTarget - startPos).normalized;
        Quaternion startRot  = flightDir.sqrMagnitude > 0.01f
            ? Quaternion.LookRotation(flightDir, Vector3.up)
            : npc.transform.rotation;
        npc.transform.rotation = startRot;

        Vector3    toCamFinal = cam.position - fixedTarget;
        Quaternion endRot     = toCamFinal.sqrMagnitude > 0.01f
            ? Quaternion.LookRotation(toCamFinal.normalized, Vector3.up)
            : startRot;

        float duration = Mathf.Max(0.1f, approachDuration);
        float t        = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float eased = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));

            npc.transform.position = Vector3.Lerp(startPos, fixedTarget, eased);
            npc.transform.rotation = Quaternion.Slerp(startRot, endRot, eased);

            yield return null;
        }

        npc.transform.position = fixedTarget;
        npc.transform.rotation = endRot;

        TransitionTo(GameState.Inspecting);
    }

    private Vector3 ApproachTarget()
    {
        if (_cam == null) _cam = Camera.main;
        Transform cam = _cam.transform;
        return cam.position
             + cam.forward * approachDistance
             - cam.right   * approachSideOffset
             + cam.up      * approachUpOffset;
    }

    // ── Utilidades ────────────────────────────────────────────────────

    private void ReleaseNPC()
    {
        if (SelectedNPC != null)
        {
            SelectedNPC.enabled = true;
            var rb = SelectedNPC.GetComponent<Rigidbody>();
            if (rb != null) rb.constraints = RigidbodyConstraints.FreezeRotation;
            SelectedNPC = null;
        }
    }

    private void HideOtherNPCs(bool hide)
    {
        if (hide)
        {
            var all = FindObjectsByType<ButterflyNPCBehavior>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            var toHide = System.Array.FindAll(all, n => n != SelectedNPC && n.gameObject.activeSelf);
            foreach (var n in toHide) n.gameObject.SetActive(false);
            _hiddenNPCs = toHide;
        }
        else
        {
            if (_hiddenNPCs != null)
            {
                foreach (var n in _hiddenNPCs)
                    if (n != null) n.gameObject.SetActive(true);
                _hiddenNPCs = null;
            }
        }
    }

    private void SetPlayerVisible(bool visible)
    {
        if (playerCharacter != null) playerCharacter.SetActive(visible);

        // Respaldo: solo ocultar ButterflyController ACTIVOS (el del jugador).
        // Las mariposas NPC también tienen ButterflyController pero deshabilitado.
        var ctrls = FindObjectsByType<ButterflyController>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var bc in ctrls)
        {
            if (!bc.enabled) continue;
            if (bc.GetComponent<ButterflyNPCBehavior>() != null) continue;
            var root = bc.transform.root.gameObject;
            if (root != null && root != playerCharacter)
                root.SetActive(visible);
        }
    }

    private void SetJoystickVisible(bool visible)
    {
        if (_joystickGOs == null || _joystickGOs.Length == 0)
            _joystickGOs = GameObjectsOfType<FixedJoystick>();
        foreach (var go in _joystickGOs)
            if (go != null) go.SetActive(visible);
    }

    private void SetTouchFieldVisible(bool visible)
    {
        if (_touchFieldGOs == null || _touchFieldGOs.Length == 0)
            _touchFieldGOs = GameObjectsOfType<FixedTouchField>();
        foreach (var go in _touchFieldGOs)
            if (go != null) go.SetActive(visible);
    }

    private void SetMinimapVisible(bool visible)
    {
        if (_minimapGOs == null || _minimapGOs.Length == 0)
            _minimapGOs = CollectMinimapGOs();
        foreach (var go in _minimapGOs)
            if (go != null) go.SetActive(visible);
    }

    // Busca todos los GameObjects del minimapa: MinimapController + cualquier
    // GameObject cuyo nombre contenga "minimap" (case-insensitive).
    private static GameObject[] CollectMinimapGOs()
    {
        var set = new HashSet<GameObject>();

        var mcs = FindObjectsByType<MinimapController>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var mc in mcs)
            set.Add(mc.gameObject);

        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            SearchByName(root.transform, "minimap", set);

        var arr = new GameObject[set.Count];
        set.CopyTo(arr);
        return arr;
    }

    private static void SearchByName(Transform t, string needle, HashSet<GameObject> bag)
    {
        if (t.name.ToLowerInvariant().Contains(needle)) bag.Add(t.gameObject);
        for (int i = 0; i < t.childCount; i++)
            SearchByName(t.GetChild(i), needle, bag);
    }

    private static GameObject[] GameObjectsOfType<T>() where T : Component
    {
        var comps = FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var gos   = new GameObject[comps.Length];
        for (int i = 0; i < comps.Length; i++) gos[i] = comps[i].gameObject;
        return gos;
    }

    // ── Botón "Volver" autoconstruido ─────────────────────────────────

    private void ShowReturnButton(bool show)
    {
        if (show && _returnButton == null) CreateReturnButton();
        if (_returnButton != null) _returnButton.SetActive(show);
    }

    private void CreateReturnButton()
    {
        var canvasGO = new GameObject("ReturnButtonCanvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode      = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder    = 100;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Botón "Volver" — izquierda-abajo
        BuildBottomButton(canvasGO.transform, "ReturnButton", "Volver",
            new Vector2(-160f, 40f),
            new Color(0.15f, 0.15f, 0.2f, 0.92f),
            OnReturnToExploring);

        // Botón "Cambiar personaje" — derecha-abajo
        BuildBottomButton(canvasGO.transform, "BecomeButton", "Cambiar personaje",
            new Vector2(160f, 40f),
            new Color(0.18f, 0.45f, 0.20f, 0.92f),
            OnChooseBecomeButterfly);

        _returnButton = canvasGO;
    }

    private static void BuildBottomButton(
        Transform parent, string goName, string label,
        Vector2 anchoredPos, Color bgColor, UnityEngine.Events.UnityAction onClick)
    {
        var btnGO = new GameObject(goName);
        btnGO.transform.SetParent(parent, false);

        var rt = btnGO.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0f);
        rt.anchorMax        = new Vector2(0.5f, 0f);
        rt.pivot            = new Vector2(0.5f, 0f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = new Vector2(300f, 70f);

        var img = btnGO.AddComponent<Image>();
        img.color = bgColor;

        var btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        var lblGO = new GameObject("Label");
        lblGO.transform.SetParent(btnGO.transform, false);
        var lblRT = lblGO.AddComponent<RectTransform>();
        lblRT.anchorMin = Vector2.zero;
        lblRT.anchorMax = Vector2.one;
        lblRT.sizeDelta = Vector2.zero;
        var tmp = lblGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 24f;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
    }
}
