using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Cinemachine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using FMODUnity;

namespace StarterAssets
{
    public class MainMenuManager : MonoBehaviour
    {
        [Header("Main Menu Root")]
        [SerializeField] private GameObject mainMenuRoot;

        [Header("Main Panels")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject mainOptionsPanel;
        [SerializeField] private GameObject mainCreditsPanel;
        [SerializeField] private GameObject mainQuitConfirmPanel;

        [Header("Main Panels CanvasGroup")]
        [SerializeField] private CanvasGroup mainPanelCanvasGroup;
        [SerializeField] private CanvasGroup mainOptionsPanelCanvasGroup;
        [SerializeField] private CanvasGroup mainCreditsPanelCanvasGroup;
        [SerializeField] private CanvasGroup mainQuitConfirmPanelCanvasGroup;

        [Header("Main Menu Root CanvasGroup")]
        [SerializeField] private CanvasGroup mainMenuRootCanvasGroup;
        [SerializeField] private float panelFadeDuration = 0.4f;
        [SerializeField] private float startFadeDuration = 0.6f;

        [Header("Main Navigation")]
        [SerializeField] private Button firstMainButton;

        [Header("Main Buttons")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button quitButton;

        [Header("Options - Navigation")]
        [SerializeField] private Button firstMainOptionsButton;
        [SerializeField] private Button mainOptionsBackButton;

        [Header("Options - Gameplay & Camera")]
        [SerializeField] private Slider mainSensitivitySlider;
        [SerializeField] private Toggle mainInvertYToggle;
        [SerializeField] private Toggle mainAutoCamToggle;
        [SerializeField] private Toggle mainAttackOnJumpToggle;
        [SerializeField] private Toggle mainRumbleToggle;

        [Header("Quit Confirm")]
        [SerializeField] private Button firstMainQuitConfirmButton;
        [SerializeField] private Button mainQuitNoButton;
        [SerializeField] private Button mainQuitYesButton;

        [Header("Credits")]
        [SerializeField] private Button firstMainCreditsButton;
        [SerializeField] private Button mainCreditsBackButton;

        [Header("HUD / Gameplay UI")]
        [SerializeField] private GameObject gameplayUIRoot;
        [SerializeField] private CanvasGroup gameplayUICanvasGroup;
        [SerializeField] private float gameplayUIFadeDuration = 0.6f;

        [Header("Tutorial UI")]
        [SerializeField] private CanvasGroup tutorialCanvasGroup;
        [SerializeField] private float tutorialFadeDuration = 1f;

        [Header("Camera")]
        [SerializeField] private CinemachineVirtualCamera menuCamera;

        [Header("Start Lock Timing")]
        [SerializeField] private float startLockDuration = 1f;

        [Header("Gameplay Refs")]
        [SerializeField] private ThirdPersonController thirdPersonController;
        [SerializeField] private PogoDashAbility pogoDashAbility;
        [SerializeField] private StarterAssetsInputs starterInputs;
        [SerializeField] private LowHPPostProcessManager lowHpManager;

        [Header("Menu Player Anim")]
        [SerializeField] private bool enableMenuLoopAnimation = true;
        [SerializeField] private string menuLoopBoolName = "MenuLoop";

        [Header("Menu Visual Overrides")]
        [SerializeField] private Renderer menuShadowRenderer;
        [SerializeField] private GameObject menuDecalProjectorObject;
        [SerializeField] private Camera mainGameplayCamera;
        [SerializeField] private Color menuAmbientColor = new Color(60f / 255f, 60f / 255f, 60f / 255f, 1f);
        [SerializeField] private float ambientColorLerpDuration = 0.6f;

        [Header("Selection Juice")]
        [SerializeField] private float selectedScaleMultiplier = 1.06f;
        [SerializeField] private float unselectedScaleMultiplier = 0.97f;
        [SerializeField] private float scaleLerpSpeed = 12f;

        [Header("FMOD")]
        [SerializeField] private EventReference openingMusic;
        [SerializeField] private EventReference uiExitEvent;
        [SerializeField] private EventReference uiOpenEvent;
        [SerializeField] private EventReference uiScrollEvent;
        [SerializeField] private EventReference uiSelectEvent;

        [Header("Other UI Managers")]
        [SerializeField] private PauseMenuManager pauseMenuManager;

        private FMOD.Studio.EventInstance openingMusicInstance;

        private PlayerInput _playerInput;
        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _pauseAction;
        private InputAction _jumpAction;
        private InputAction _dashAction;
        private InputAction _pogoAction;
        private InputAction _backAction;

        private bool _gameStarted;
        private bool _inOptions;
        private bool _inQuitConfirm;
        private bool _inCredits;

        private Coroutine _unlockLookCoroutine;
        private Coroutine _tutorialFadeCoroutine;
        private Coroutine _panelTransitionCoroutine;
        private Coroutine _startFadeCoroutine;
        private Coroutine _gameplayUIFadeCoroutine;

        private const float LookSensMin = 0.5f;
        private const float LookSensMax = 1.5f;

        private Animator _playerAnimator;
        private bool _menuLoopAlreadyPlayed = false;

        private GameObject _currentPanel;
        private CanvasGroup _currentPanelCanvasGroup;

        private bool _menuVisualsConfigured;

        private bool _hadMeshShadowData;
        private ShadowCastingMode _originalShadowCastingMode;
        private bool _originalReceiveShadows;

        private bool _hadDecalObject;
        private bool _decalOriginalActive;

        private bool _hadCameraData;
        private AntialiasingMode _originalAAMode;
        private AntialiasingQuality _originalAAQuality;

        private bool _hadAmbientColor;
        private Color _originalAmbientColor;
        private Coroutine _ambientLerpCoroutine;

        private GameObject _pendingSelection;

        private Selectable[] _allSelectables;
        private Transform[] _selectableTransforms;
        private Vector3[] _selectableBaseScales;

        private GameObject _lastSelectedObject;
        private bool _suppressNextSelectSound;

        private float _lastUIExitTime = -1f;

        private void Awake()
        {
            if (pauseMenuManager != null)
                pauseMenuManager.gameObject.SetActive(false);

            if (gameplayUIRoot != null)
                gameplayUIRoot.SetActive(false);

            if (gameplayUIRoot != null && gameplayUICanvasGroup == null)
                gameplayUICanvasGroup = gameplayUIRoot.GetComponent<CanvasGroup>();

            if (gameplayUICanvasGroup != null)
            {
                gameplayUICanvasGroup.alpha = 0f;
                gameplayUICanvasGroup.interactable = false;
                gameplayUICanvasGroup.blocksRaycasts = false;
            }

            if (tutorialCanvasGroup != null)
            {
                tutorialCanvasGroup.gameObject.SetActive(false);
                tutorialCanvasGroup.alpha = 0f;
            }

            if (_playerInput == null)
                _playerInput = FindFirstObjectByType<PlayerInput>();

            if (thirdPersonController == null)
                thirdPersonController = FindFirstObjectByType<ThirdPersonController>();
            if (pogoDashAbility == null)
                pogoDashAbility = FindFirstObjectByType<PogoDashAbility>();
            if (starterInputs == null)
                starterInputs = FindFirstObjectByType<StarterAssetsInputs>();
            if (lowHpManager == null)
                lowHpManager = FindFirstObjectByType<LowHPPostProcessManager>();

            if (thirdPersonController != null && _playerAnimator == null)
                _playerAnimator = thirdPersonController.GetComponent<Animator>();

            if (_playerInput != null)
            {
                var actions = _playerInput.actions;
                _moveAction = actions.FindAction("Move", false);
                _lookAction = actions.FindAction("Look", false);
                _pauseAction = actions.FindAction("Pause", false);
                _jumpAction = actions.FindAction("Jump", false);
                _dashAction = actions.FindAction("Dash", false);
                _pogoAction = actions.FindAction("Pogo", false);
                _backAction = actions.FindAction("Back", false);
            }

            if (thirdPersonController != null)
                thirdPersonController.CanMove = false;
            if (pogoDashAbility != null)
                pogoDashAbility.enabled = false;
            if (starterInputs != null)
            {
                starterInputs.jump = false;
                starterInputs.dash = false;
                starterInputs.pogo = false;
            }

            if (_moveAction != null) _moveAction.Disable();
            if (_lookAction != null) _lookAction.Disable();
            if (_pauseAction != null) _pauseAction.Disable();
            if (_jumpAction != null) _jumpAction.Disable();
            if (_dashAction != null) _dashAction.Disable();
            if (_pogoAction != null) _pogoAction.Disable();

            if (mainMenuRoot != null)
                mainMenuRoot.SetActive(true);

            if (mainPanel != null) mainPanel.SetActive(true);
            if (mainOptionsPanel != null) mainOptionsPanel.SetActive(false);
            if (mainQuitConfirmPanel != null) mainQuitConfirmPanel.SetActive(false);
            if (mainCreditsPanel != null) mainCreditsPanel.SetActive(false);

            if (mainPanel != null && mainPanelCanvasGroup == null)
                mainPanelCanvasGroup = mainPanel.GetComponent<CanvasGroup>();
            if (mainOptionsPanel != null && mainOptionsPanelCanvasGroup == null)
                mainOptionsPanelCanvasGroup = mainOptionsPanel.GetComponent<CanvasGroup>();
            if (mainCreditsPanel != null && mainCreditsPanelCanvasGroup == null)
                mainCreditsPanelCanvasGroup = mainCreditsPanel.GetComponent<CanvasGroup>();
            if (mainQuitConfirmPanel != null && mainQuitConfirmPanelCanvasGroup == null)
                mainQuitConfirmPanelCanvasGroup = mainQuitConfirmPanel.GetComponent<CanvasGroup>();
            if (mainMenuRoot != null && mainMenuRootCanvasGroup == null)
                mainMenuRootCanvasGroup = mainMenuRoot.GetComponent<CanvasGroup>();

            if (mainMenuRootCanvasGroup != null)
            {
                mainMenuRootCanvasGroup.alpha = 1f;
                mainMenuRootCanvasGroup.interactable = true;
                mainMenuRootCanvasGroup.blocksRaycasts = true;
            }

            if (mainPanelCanvasGroup != null)
            {
                mainPanelCanvasGroup.alpha = 1f;
                mainPanelCanvasGroup.interactable = true;
                mainPanelCanvasGroup.blocksRaycasts = true;
            }
            if (mainOptionsPanelCanvasGroup != null)
            {
                mainOptionsPanelCanvasGroup.alpha = 0f;
                mainOptionsPanelCanvasGroup.interactable = false;
                mainOptionsPanelCanvasGroup.blocksRaycasts = false;
            }
            if (mainCreditsPanelCanvasGroup != null)
            {
                mainCreditsPanelCanvasGroup.alpha = 0f;
                mainCreditsPanelCanvasGroup.interactable = false;
                mainCreditsPanelCanvasGroup.blocksRaycasts = false;
            }
            if (mainQuitConfirmPanelCanvasGroup != null)
            {
                mainQuitConfirmPanelCanvasGroup.alpha = 0f;
                mainQuitConfirmPanelCanvasGroup.interactable = false;
                mainQuitConfirmPanelCanvasGroup.blocksRaycasts = false;
            }

            _currentPanel = mainPanel;
            _currentPanelCanvasGroup = mainPanelCanvasGroup;

            if (menuCamera != null)
                menuCamera.Priority = 20;

            RefreshMainOptionsUI();

            if (startButton != null) startButton.onClick.AddListener(OnStartClicked);
            if (optionsButton != null) optionsButton.onClick.AddListener(OnOptionsClicked);
            if (creditsButton != null) creditsButton.onClick.AddListener(OnCreditsClicked);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);

            if (mainSensitivitySlider != null) mainSensitivitySlider.onValueChanged.AddListener(OnMainSensitivityChanged);
            if (mainInvertYToggle != null) mainInvertYToggle.onValueChanged.AddListener(OnMainInvertYChanged);
            if (mainAutoCamToggle != null) mainAutoCamToggle.onValueChanged.AddListener(OnMainAutoCamChanged);
            if (mainAttackOnJumpToggle != null) mainAttackOnJumpToggle.onValueChanged.AddListener(OnMainAttackOnJumpChanged);
            if (mainRumbleToggle != null) mainRumbleToggle.onValueChanged.AddListener(OnMainRumbleChanged);

            if (EventSystem.current != null && firstMainButton != null)
            {
                _suppressNextSelectSound = true;
                EventSystem.current.SetSelectedGameObject(firstMainButton.gameObject);
            }

            if (enableMenuLoopAnimation && !_gameStarted && !_menuLoopAlreadyPlayed && _playerAnimator != null && !string.IsNullOrEmpty(menuLoopBoolName))
                _playerAnimator.SetBool(menuLoopBoolName, true);

            ApplyMenuVisualOverrides();

            if (mainMenuRoot != null)
            {
                _allSelectables = mainMenuRoot.GetComponentsInChildren<Selectable>(true);
                if (_allSelectables != null)
                {
                    int n = _allSelectables.Length;
                    _selectableTransforms = new Transform[n];
                    _selectableBaseScales = new Vector3[n];
                    for (int i = 0; i < n; i++)
                    {
                        if (_allSelectables[i] == null) continue;
                        Transform t = _allSelectables[i].transform;
                        _selectableTransforms[i] = t;
                        _selectableBaseScales[i] = t.localScale;
                    }
                }
            }
        }

        private void Start()
        {
            if (!openingMusic.IsNull)
            {
                openingMusicInstance = RuntimeManager.CreateInstance(openingMusic);
                openingMusicInstance.start();
            }
        }

        private void OnDestroy()
        {
            if (startButton != null) startButton.onClick.RemoveListener(OnStartClicked);
            if (optionsButton != null) optionsButton.onClick.RemoveListener(OnOptionsClicked);
            if (creditsButton != null) creditsButton.onClick.RemoveListener(OnCreditsClicked);
            if (quitButton != null) quitButton.onClick.RemoveListener(OnQuitClicked);

            if (mainSensitivitySlider != null) mainSensitivitySlider.onValueChanged.RemoveListener(OnMainSensitivityChanged);
            if (mainInvertYToggle != null) mainInvertYToggle.onValueChanged.RemoveListener(OnMainInvertYChanged);
            if (mainAutoCamToggle != null) mainAutoCamToggle.onValueChanged.RemoveListener(OnMainAutoCamChanged);
            if (mainAttackOnJumpToggle != null) mainAttackOnJumpToggle.onValueChanged.RemoveListener(OnMainAttackOnJumpChanged);
            if (mainRumbleToggle != null) mainRumbleToggle.onValueChanged.RemoveListener(OnMainRumbleChanged);

            StopOpeningMusic();
        }

        private void StopOpeningMusic()
        {
            if (openingMusicInstance.isValid())
            {
                openingMusicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                openingMusicInstance.release();
                openingMusicInstance.clearHandle();
            }
        }

        private void PlayUIExit()
        {
            if (uiExitEvent.IsNull)
                return;

            float t = Time.unscaledTime;
            if (t - _lastUIExitTime < 0.02f)
                return;

            _lastUIExitTime = t;
            RuntimeManager.PlayOneShot(uiExitEvent);
        }

        private void PlayUIOpen()
        {
            if (!uiOpenEvent.IsNull)
                RuntimeManager.PlayOneShot(uiOpenEvent);
        }

        private void PlayUIScroll()
        {
            if (!uiScrollEvent.IsNull)
                RuntimeManager.PlayOneShot(uiScrollEvent);
        }

        private void PlayUISelect()
        {
            if (!uiSelectEvent.IsNull)
                RuntimeManager.PlayOneShot(uiSelectEvent);
        }

        private bool BackPressedThisFrame()
        {
            if (_panelTransitionCoroutine != null)
                return false;

            bool gamepadBack = _backAction != null && _backAction.triggered;
            bool keyboardEsc = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
            return gamepadBack || keyboardEsc;
        }

        private void Update()
        {
            if (_gameStarted) return;
            if (EventSystem.current == null) return;

            if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
                ExecuteCurrentButton();

            bool canAutoSelect = _panelTransitionCoroutine == null && _pendingSelection == null;

            if (_inQuitConfirm)
            {
                if (canAutoSelect &&
                    EventSystem.current.currentSelectedGameObject == null &&
                    firstMainQuitConfirmButton != null)
                {
                    _suppressNextSelectSound = true;
                    EventSystem.current.SetSelectedGameObject(firstMainQuitConfirmButton.gameObject);
                }

                if (BackPressedThisFrame())
                    OnMainQuitNoClicked();
            }
            else if (_inOptions)
            {
                if (canAutoSelect &&
                    EventSystem.current.currentSelectedGameObject == null &&
                    firstMainOptionsButton != null)
                {
                    _suppressNextSelectSound = true;
                    EventSystem.current.SetSelectedGameObject(firstMainOptionsButton.gameObject);
                }

                if (BackPressedThisFrame())
                    OnMainOptionsBackClicked();
            }
            else if (_inCredits)
            {
                if (canAutoSelect &&
                    EventSystem.current.currentSelectedGameObject == null &&
                    firstMainCreditsButton != null)
                {
                    _suppressNextSelectSound = true;
                    EventSystem.current.SetSelectedGameObject(firstMainCreditsButton.gameObject);
                }

                if (BackPressedThisFrame())
                    OnMainCreditsBackClicked();
            }
            else
            {
                if (canAutoSelect &&
                    EventSystem.current.currentSelectedGameObject == null &&
                    firstMainButton != null)
                {
                    _suppressNextSelectSound = true;
                    EventSystem.current.SetSelectedGameObject(firstMainButton.gameObject);
                }
            }

            if (_pendingSelection != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                _suppressNextSelectSound = true;
                EventSystem.current.SetSelectedGameObject(_pendingSelection);
                _pendingSelection = null;
            }

            UpdateSelectionJuice();
        }

        private void QueueSelection(GameObject target)
        {
            if (target == null) return;
            if (EventSystem.current == null) return;
            _pendingSelection = target;
        }

        private void UpdateSelectionJuice()
        {
            if (_selectableTransforms == null || _selectableBaseScales == null)
                return;

            GameObject selected = null;
            if (EventSystem.current != null)
                selected = EventSystem.current.currentSelectedGameObject;

            if (selected != _lastSelectedObject)
            {
                if (selected != null)
                {
                    if (_suppressNextSelectSound)
                    {
                        _suppressNextSelectSound = false;
                    }
                    else
                    {
                        PlayUISelect();
                    }
                }
                _lastSelectedObject = selected;
            }

            float dt = Time.unscaledDeltaTime;
            float lerpFactor = 1f - Mathf.Exp(-scaleLerpSpeed * dt);

            for (int i = 0; i < _selectableTransforms.Length; i++)
            {
                Transform t = _selectableTransforms[i];
                if (t == null) continue;

                Vector3 baseScale = _selectableBaseScales[i];

                bool isSelected = false;
                if (selected != null)
                {
                    if (t.gameObject == selected || t == selected.transform || t.IsChildOf(selected.transform))
                        isSelected = true;
                }

                float mul = isSelected ? selectedScaleMultiplier : unselectedScaleMultiplier;
                Vector3 targetScale = baseScale * mul;
                t.localScale = Vector3.Lerp(t.localScale, targetScale, lerpFactor);
            }
        }

        private void ExecuteCurrentButton()
        {
            var current = EventSystem.current.currentSelectedGameObject;
            if (current == null) return;

            Button btn = current.GetComponent<Button>();
            if (btn == null) btn = current.GetComponentInParent<Button>();
            if (btn == null) return;

            btn.onClick.Invoke();
        }

        private void OnStartClicked()
        {
            if (_gameStarted) return;
            _gameStarted = true;

            if (pauseMenuManager != null)
                pauseMenuManager.gameObject.SetActive(true);

            StopOpeningMusic();

            if (enableMenuLoopAnimation && _playerAnimator != null && !string.IsNullOrEmpty(menuLoopBoolName))
            {
                _playerAnimator.SetBool(menuLoopBoolName, false);
                _menuLoopAlreadyPlayed = true;
            }

            if (menuCamera != null)
                menuCamera.Priority = 0;

            if (thirdPersonController != null)
                thirdPersonController.CanMove = true;
            if (pogoDashAbility != null)
                pogoDashAbility.enabled = true;

            if (starterInputs != null)
            {
                starterInputs.jump = false;
                starterInputs.dash = false;
                starterInputs.pogo = false;
            }

            if (lowHpManager != null)
                lowHpManager.ActivateLowHpSystem();

            if (_moveAction != null) _moveAction.Disable();
            if (_pauseAction != null) _pauseAction.Disable();
            if (_jumpAction != null) _jumpAction.Disable();
            if (_dashAction != null) _dashAction.Disable();
            if (_pogoAction != null) _pogoAction.Disable();
            if (_lookAction != null) _lookAction.Disable();

            if (_unlockLookCoroutine != null)
                StopCoroutine(_unlockLookCoroutine);
            _unlockLookCoroutine = StartCoroutine(StartLockRoutine());

            if (_startFadeCoroutine != null)
                StopCoroutine(_startFadeCoroutine);
            _startFadeCoroutine = StartCoroutine(StartMenuFadeOutRoutine());
        }

        private void ShowGameplayUI()
        {
            if (gameplayUIRoot == null) return;

            gameplayUIRoot.SetActive(true);

            if (gameplayUICanvasGroup == null)
            {
                gameplayUICanvasGroup = gameplayUIRoot.GetComponent<CanvasGroup>();
                if (gameplayUICanvasGroup == null) return;
            }

            gameplayUICanvasGroup.alpha = 0f;
            gameplayUICanvasGroup.interactable = false;
            gameplayUICanvasGroup.blocksRaycasts = false;

            if (_gameplayUIFadeCoroutine != null)
                StopCoroutine(_gameplayUIFadeCoroutine);

            _gameplayUIFadeCoroutine = StartCoroutine(FadeInGameplayUI());
        }

        private IEnumerator FadeInGameplayUI()
        {
            if (gameplayUIFadeDuration <= 0f)
            {
                gameplayUICanvasGroup.alpha = 1f;
                gameplayUICanvasGroup.interactable = true;
                gameplayUICanvasGroup.blocksRaycasts = true;
                _gameplayUIFadeCoroutine = null;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < gameplayUIFadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / gameplayUIFadeDuration);
                gameplayUICanvasGroup.alpha = t;
                yield return null;
            }

            gameplayUICanvasGroup.alpha = 1f;
            gameplayUICanvasGroup.interactable = true;
            gameplayUICanvasGroup.blocksRaycasts = true;
            _gameplayUIFadeCoroutine = null;
        }

        private IEnumerator StartLockRoutine()
        {
            yield return new WaitForSeconds(startLockDuration);

            if (_moveAction != null) _moveAction.Enable();
            if (_pauseAction != null) _pauseAction.Enable();
            if (_jumpAction != null) _jumpAction.Enable();
            if (_dashAction != null) _dashAction.Enable();
            if (_pogoAction != null) _pogoAction.Enable();
            if (_lookAction != null) _lookAction.Enable();

            ShowGameplayUI();
            ShowTutorialCanvas();

            _unlockLookCoroutine = null;
        }

        private IEnumerator StartMenuFadeOutRoutine()
        {
            if (mainMenuRootCanvasGroup == null)
            {
                if (mainMenuRoot != null)
                    mainMenuRoot.SetActive(false);

                RestoreMenuVisualOverrides();

                _startFadeCoroutine = null;
                yield break;
            }

            float elapsed = 0f;
            mainMenuRootCanvasGroup.interactable = false;
            mainMenuRootCanvasGroup.blocksRaycasts = false;

            while (elapsed < startFadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / startFadeDuration);
                mainMenuRootCanvasGroup.alpha = 1f - t;
                yield return null;
            }

            mainMenuRootCanvasGroup.alpha = 0f;

            if (mainMenuRoot != null)
                mainMenuRoot.SetActive(false);

            RestoreMenuVisualOverrides();

            _startFadeCoroutine = null;
        }

        private void ShowTutorialCanvas()
        {
            if (tutorialCanvasGroup == null) return;

            tutorialCanvasGroup.gameObject.SetActive(true);
            tutorialCanvasGroup.alpha = 0f;

            if (_tutorialFadeCoroutine != null)
                StopCoroutine(_tutorialFadeCoroutine);

            _tutorialFadeCoroutine = StartCoroutine(FadeInTutorial());
        }

        private IEnumerator FadeInTutorial()
        {
            float elapsed = 0f;
            while (elapsed < tutorialFadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / tutorialFadeDuration);
                tutorialCanvasGroup.alpha = t;
                yield return null;
            }
            tutorialCanvasGroup.alpha = 1f;
            _tutorialFadeCoroutine = null;
        }

        private void TransitionToPanel(GameObject targetPanel, CanvasGroup targetCanvasGroup, bool inOptions, bool inCredits, bool inQuitConfirm, GameObject firstSelected)
        {
            if (_panelTransitionCoroutine != null)
                return;

            _inOptions = inOptions;
            _inCredits = inCredits;
            _inQuitConfirm = inQuitConfirm;

            _panelTransitionCoroutine = StartCoroutine(PanelTransitionRoutine(targetPanel, targetCanvasGroup, firstSelected));
        }

        private IEnumerator PanelTransitionRoutine(GameObject targetPanel, CanvasGroup targetCanvasGroup, GameObject firstSelected)
        {
            if (targetPanel == null)
            {
                _panelTransitionCoroutine = null;
                yield break;
            }

            if (targetPanel == _currentPanel)
            {
                if (firstSelected != null)
                    QueueSelection(firstSelected);

                _panelTransitionCoroutine = null;
                yield break;
            }

            bool canFade = _currentPanelCanvasGroup != null && targetCanvasGroup != null && panelFadeDuration > 0f;

            if (!canFade)
            {
                if (_currentPanel != null && _currentPanel != targetPanel)
                    _currentPanel.SetActive(false);

                if (targetPanel != null)
                    targetPanel.SetActive(true);

                _currentPanel = targetPanel;
                _currentPanelCanvasGroup = targetCanvasGroup;

                if (targetCanvasGroup != null)
                {
                    targetCanvasGroup.alpha = 1f;
                    targetCanvasGroup.interactable = true;
                    targetCanvasGroup.blocksRaycasts = true;
                }

                if (firstSelected != null)
                    QueueSelection(firstSelected);

                _panelTransitionCoroutine = null;
                yield break;
            }

            float elapsed = 0f;
            if (_currentPanelCanvasGroup != null)
            {
                _currentPanelCanvasGroup.interactable = false;
                _currentPanelCanvasGroup.blocksRaycasts = false;
            }

            while (elapsed < panelFadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / panelFadeDuration);

                if (_currentPanelCanvasGroup != null)
                    _currentPanelCanvasGroup.alpha = 1f - t;

                yield return null;
            }

            if (_currentPanelCanvasGroup != null)
                _currentPanelCanvasGroup.alpha = 0f;

            if (EventSystem.current != null)
            {
                var currentSel = EventSystem.current.currentSelectedGameObject;
                if (currentSel != null && _currentPanel != null && currentSel.transform.IsChildOf(_currentPanel.transform))
                {
                    EventSystem.current.SetSelectedGameObject(null);
                }
            }

            if (_currentPanel != null && _currentPanel != targetPanel)
                _currentPanel.SetActive(false);

            if (targetPanel != null)
                targetPanel.SetActive(true);

            if (targetCanvasGroup != null)
            {
                targetCanvasGroup.alpha = 0f;
                targetCanvasGroup.interactable = false;
                targetCanvasGroup.blocksRaycasts = false;
            }

            elapsed = 0f;
            while (elapsed < panelFadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / panelFadeDuration);

                if (targetCanvasGroup != null)
                    targetCanvasGroup.alpha = t;

                yield return null;
            }

            if (targetCanvasGroup != null)
            {
                targetCanvasGroup.alpha = 1f;
                targetCanvasGroup.interactable = true;
                targetCanvasGroup.blocksRaycasts = true;
            }

            _currentPanel = targetPanel;
            _currentPanelCanvasGroup = targetCanvasGroup;

            if (firstSelected != null)
                QueueSelection(firstSelected);

            _panelTransitionCoroutine = null;
        }

        private void OnOptionsClicked()
        {
            PlayUIOpen();

            TransitionToPanel(
                mainOptionsPanel,
                mainOptionsPanelCanvasGroup,
                true,
                false,
                false,
                firstMainOptionsButton != null ? firstMainOptionsButton.gameObject : null
            );
        }

        public void OnCreditsClicked()
        {
            PlayUIOpen();

            TransitionToPanel(
                mainCreditsPanel,
                mainCreditsPanelCanvasGroup,
                false,
                true,
                false,
                firstMainCreditsButton != null ? firstMainCreditsButton.gameObject : null
            );
        }

        public void OnQuitClicked()
        {
            PlayUIOpen();

            TransitionToPanel(
                mainQuitConfirmPanel,
                mainQuitConfirmPanelCanvasGroup,
                false,
                false,
                true,
                firstMainQuitConfirmButton != null ? firstMainQuitConfirmButton.gameObject : null
            );
        }

        public void OnMainOptionsBackClicked()
        {
            _suppressNextSelectSound = true;
            PlayUIExit();

            TransitionToPanel(
                mainPanel,
                mainPanelCanvasGroup,
                false,
                false,
                false,
                optionsButton != null ? optionsButton.gameObject : null
            );
        }

        public void OnMainQuitNoClicked()
        {
            _suppressNextSelectSound = true;
            PlayUIExit();

            TransitionToPanel(
                mainPanel,
                mainPanelCanvasGroup,
                false,
                false,
                false,
                quitButton != null ? quitButton.gameObject : null
            );
        }

        public void OnMainQuitYesClicked()
        {
            StopOpeningMusic();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void OnMainCreditsBackClicked()
        {
            _suppressNextSelectSound = true;
            PlayUIExit();

            TransitionToPanel(
                mainPanel,
                mainPanelCanvasGroup,
                false,
                false,
                false,
                creditsButton != null ? creditsButton.gameObject : null
            );
        }

        private void RefreshMainOptionsUI()
        {
            if (thirdPersonController != null && mainSensitivitySlider != null)
            {
                float sens = thirdPersonController.LookSensitivity;
                float sliderValue = Mathf.InverseLerp(LookSensMin, LookSensMax, sens);
                mainSensitivitySlider.minValue = 0f;
                mainSensitivitySlider.maxValue = 1f;
                mainSensitivitySlider.SetValueWithoutNotify(sliderValue);
            }

            if (thirdPersonController != null && mainInvertYToggle != null)
                mainInvertYToggle.SetIsOnWithoutNotify(thirdPersonController.InvertY);

            if (thirdPersonController != null && mainAutoCamToggle != null)
                mainAutoCamToggle.SetIsOnWithoutNotify(thirdPersonController.AutoCamGlobal);

            if (pogoDashAbility != null && mainAttackOnJumpToggle != null)
                mainAttackOnJumpToggle.SetIsOnWithoutNotify(pogoDashAbility.AttackOnJump);

            if (mainRumbleToggle != null)
                mainRumbleToggle.SetIsOnWithoutNotify(GameRumbleSettings.RumbleEnabled);
        }

        private void OnMainSensitivityChanged(float value)
        {
            if (thirdPersonController == null || mainSensitivitySlider == null) return;
            thirdPersonController.LookSensitivity = Mathf.Lerp(LookSensMin, LookSensMax, value);
            PlayUIScroll();
        }

        private void OnMainInvertYChanged(bool isOn)
        {
            if (thirdPersonController == null) return;
            thirdPersonController.InvertY = isOn;
            PlayUIScroll();
        }

        private void OnMainAutoCamChanged(bool isOn)
        {
            if (thirdPersonController == null) return;
            thirdPersonController.AutoCamGlobal = isOn;
            PlayUIScroll();
        }

        private void OnMainAttackOnJumpChanged(bool isOn)
        {
            if (pogoDashAbility == null) return;
            pogoDashAbility.AttackOnJump = isOn;
            PlayUIScroll();
        }

        private void OnMainRumbleChanged(bool isOn)
        {
            GameRumbleSettings.SetRumbleEnabled(isOn);
            PlayUIScroll();
        }

        private void ApplyMenuVisualOverrides()
        {
            if (_menuVisualsConfigured) return;

            if (menuShadowRenderer != null)
            {
                _hadMeshShadowData = true;
                _originalShadowCastingMode = menuShadowRenderer.shadowCastingMode;
                _originalReceiveShadows = menuShadowRenderer.receiveShadows;

                menuShadowRenderer.shadowCastingMode = ShadowCastingMode.On;
                menuShadowRenderer.receiveShadows = true;
            }

            if (menuDecalProjectorObject != null)
            {
                _hadDecalObject = true;
                _decalOriginalActive = menuDecalProjectorObject.activeSelf;

                if (_decalOriginalActive)
                    menuDecalProjectorObject.SetActive(false);
            }

            if (mainGameplayCamera != null)
            {
                var camData = mainGameplayCamera.GetComponent<UniversalAdditionalCameraData>();
                if (camData != null)
                {
                    _hadCameraData = true;
                    _originalAAMode = camData.antialiasing;
                    _originalAAQuality = camData.antialiasingQuality;

                    camData.antialiasing = AntialiasingMode.TemporalAntiAliasing;
                }
            }

            _hadAmbientColor = true;
            _originalAmbientColor = RenderSettings.ambientLight;
            RenderSettings.ambientLight = menuAmbientColor;

            _menuVisualsConfigured = true;
        }

        private void RestoreMenuVisualOverrides()
        {
            if (!_menuVisualsConfigured) return;

            if (_hadMeshShadowData && menuShadowRenderer != null)
            {
                menuShadowRenderer.shadowCastingMode = _originalShadowCastingMode;
                menuShadowRenderer.receiveShadows = _originalReceiveShadows;
            }

            if (_hadDecalObject && menuDecalProjectorObject != null)
            {
                menuDecalProjectorObject.SetActive(_decalOriginalActive);
            }

            if (_hadCameraData && mainGameplayCamera != null)
            {
                var camData = mainGameplayCamera.GetComponent<UniversalAdditionalCameraData>();
                if (camData != null)
                {
                    camData.antialiasing = _originalAAMode;
                    camData.antialiasingQuality = _originalAAQuality;
                }
            }

            if (_hadAmbientColor)
            {
                if (_ambientLerpCoroutine != null)
                    StopCoroutine(_ambientLerpCoroutine);
                _ambientLerpCoroutine = StartCoroutine(LerpAmbientColor(_originalAmbientColor));
            }
        }

        private IEnumerator LerpAmbientColor(Color targetColor)
        {
            Color startColor = RenderSettings.ambientLight;

            if (ambientColorLerpDuration <= 0f)
            {
                RenderSettings.ambientLight = targetColor;
                _ambientLerpCoroutine = null;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < ambientColorLerpDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / ambientColorLerpDuration);
                RenderSettings.ambientLight = Color.Lerp(startColor, targetColor, t);
                yield return null;
            }

            RenderSettings.ambientLight = targetColor;
            _ambientLerpCoroutine = null;
        }
    }

    public static class GameRumbleSettings
    {
        private const string PrefKey = "RumbleEnabled";
        private static bool _rumbleEnabled = true;
        private static bool _loaded;

        public static bool RumbleEnabled
        {
            get
            {
                EnsureLoaded();
                return _rumbleEnabled;
            }
        }

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _rumbleEnabled = PlayerPrefs.GetInt(PrefKey, 1) == 1;
            _loaded = true;
        }

        public static void SetRumbleEnabled(bool enabled)
        {
            EnsureLoaded();
            _rumbleEnabled = enabled;
            PlayerPrefs.SetInt(PrefKey, enabled ? 1 : 0);
        }
    }
}
