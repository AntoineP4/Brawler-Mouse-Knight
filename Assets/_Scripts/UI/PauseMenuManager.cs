using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace StarterAssets
{
    public class PauseMenuManager : MonoBehaviour
    {
        [Header("UI Root")]
        [SerializeField] private GameObject pauseMenuRoot;

        [Header("Panels")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject optionsPanel;
        [SerializeField] private GameObject quitConfirmPanel;

        [Header("Quit Target Scene")]
        [SerializeField] private string quitSceneName;

        [Header("Pause Menu Root CanvasGroup")]
        [SerializeField] private CanvasGroup pauseMenuRootCanvasGroup;
        [SerializeField] private float panelFadeDuration = 0.25f;
        [SerializeField] private float rootFadeDuration = 0.25f;

        [Header("Panels CanvasGroup")]
        [SerializeField] private CanvasGroup pausePanelCanvasGroup;
        [SerializeField] private CanvasGroup optionsPanelCanvasGroup;
        [SerializeField] private CanvasGroup quitConfirmPanelCanvasGroup;

        [Header("Cadres")]
        [SerializeField] private CanvasGroup cadre1CanvasGroup;
        [SerializeField] private CanvasGroup cadre2CanvasGroup;

        [Header("Navigation - Sélection par défaut")]
        [SerializeField] private Button firstPauseButton;
        [SerializeField] private Button firstOptionsButton;
        [SerializeField] private Button firstQuitConfirmButton;

        [Header("Navigation - Boutons de retour")]
        [SerializeField] private Button pauseOptionsButton;
        [SerializeField] private Button pauseQuitButton;

        [Header("Options - Audio")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;

        [Header("Options - Gameplay & Camera")]
        [SerializeField] private Slider sensitivitySlider;
        [SerializeField] private Toggle invertYToggle;
        [SerializeField] private Toggle autoCamToggle;
        [SerializeField] private Toggle attackOnJumpToggle;
        [SerializeField] private Toggle rumbleToggle;
        [SerializeField] private Button optionsBackButton;

        [Header("Selection Juice")]
        [SerializeField] private float selectedScaleMultiplier = 1.06f;
        [SerializeField] private float unselectedScaleMultiplier = 0.97f;
        [SerializeField] private float scaleLerpSpeed = 12f;

        private PlayerInput _playerInput;
        private InputAction _pauseAction;
        private InputAction _jumpAction;
        private InputAction _backAction;
        private InputAction _pogoAction;
        private InputAction _moveAction;
        private InputAction _lookAction;

        private bool _isPaused;

        private PogoDashAbility _pogoAbility;
        private StarterAssetsInputs _starterInputs;
        private ThirdPersonController _thirdPersonController;

        private const float LookSensMin = 0.5f;
        private const float LookSensMax = 1.5f;

        private Coroutine _panelTransitionCoroutine;
        private Coroutine _rootFadeCoroutine;

        private GameObject _currentPanel;
        private CanvasGroup _currentPanelCanvasGroup;

        private GameObject _pendingSelection;

        private Selectable[] _allSelectables;
        private Transform[] _selectableTransforms;
        private Vector3[] _selectableBaseScales;

        private void Awake()
        {
            _playerInput = FindFirstObjectByType<PlayerInput>();

            if (_playerInput != null)
            {
                var actions = _playerInput.actions;
                _pauseAction = actions.FindAction("Pause", true);
                _jumpAction = actions.FindAction("Jump", false);
                _backAction = actions.FindAction("Back", false);
                _pogoAction = actions.FindAction("Pogo", false);
                _moveAction = actions.FindAction("Move", false);
                _lookAction = actions.FindAction("Look", false);
            }

            _pogoAbility = FindFirstObjectByType<PogoDashAbility>();
            _starterInputs = FindFirstObjectByType<StarterAssetsInputs>();
            _thirdPersonController = FindFirstObjectByType<ThirdPersonController>();

            if (pauseMenuRoot != null)
                pauseMenuRoot.SetActive(false);

            if (pausePanel != null) pausePanel.SetActive(true);
            if (optionsPanel != null) optionsPanel.SetActive(false);
            if (quitConfirmPanel != null) quitConfirmPanel.SetActive(false);

            if (pauseMenuRoot != null && pauseMenuRootCanvasGroup == null)
                pauseMenuRootCanvasGroup = pauseMenuRoot.GetComponent<CanvasGroup>();

            if (pausePanel != null && pausePanelCanvasGroup == null)
                pausePanelCanvasGroup = pausePanel.GetComponent<CanvasGroup>();
            if (optionsPanel != null && optionsPanelCanvasGroup == null)
                optionsPanelCanvasGroup = optionsPanel.GetComponent<CanvasGroup>();
            if (quitConfirmPanel != null && quitConfirmPanelCanvasGroup == null)
                quitConfirmPanelCanvasGroup = quitConfirmPanel.GetComponent<CanvasGroup>();

            if (pauseMenuRootCanvasGroup != null)
            {
                pauseMenuRootCanvasGroup.alpha = 0f;
                pauseMenuRootCanvasGroup.interactable = false;
                pauseMenuRootCanvasGroup.blocksRaycasts = false;
            }

            if (pausePanelCanvasGroup != null)
            {
                pausePanelCanvasGroup.alpha = 1f;
                pausePanelCanvasGroup.interactable = true;
                pausePanelCanvasGroup.blocksRaycasts = true;
            }
            if (optionsPanelCanvasGroup != null)
            {
                optionsPanelCanvasGroup.alpha = 0f;
                optionsPanelCanvasGroup.interactable = false;
                optionsPanelCanvasGroup.blocksRaycasts = false;
            }
            if (quitConfirmPanelCanvasGroup != null)
            {
                quitConfirmPanelCanvasGroup.alpha = 0f;
                quitConfirmPanelCanvasGroup.interactable = false;
                quitConfirmPanelCanvasGroup.blocksRaycasts = false;
            }

            if (cadre1CanvasGroup != null)
            {
                cadre1CanvasGroup.gameObject.SetActive(true);
                cadre1CanvasGroup.alpha = 1f;
                cadre1CanvasGroup.interactable = false;
                cadre1CanvasGroup.blocksRaycasts = false;
            }
            if (cadre2CanvasGroup != null)
            {
                cadre2CanvasGroup.gameObject.SetActive(true);
                cadre2CanvasGroup.alpha = 0f;
                cadre2CanvasGroup.interactable = false;
                cadre2CanvasGroup.blocksRaycasts = false;
            }

            _currentPanel = pausePanel;
            _currentPanelCanvasGroup = pausePanelCanvasGroup;

            if (pauseMenuRoot != null)
            {
                _allSelectables = pauseMenuRoot.GetComponentsInChildren<Selectable>(true);
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

            RefreshOptionsUI();
        }

        private void OnEnable()
        {
            if (_pauseAction != null)
                _pauseAction.performed += OnPausePerformed;

            if (_backAction != null)
                _backAction.performed += OnBackPerformed;

            if (sensitivitySlider != null)
                sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);

            if (invertYToggle != null)
                invertYToggle.onValueChanged.AddListener(OnInvertYChanged);

            if (autoCamToggle != null)
                autoCamToggle.onValueChanged.AddListener(OnAutoCamChanged);

            if (attackOnJumpToggle != null)
                attackOnJumpToggle.onValueChanged.AddListener(OnAttackOnJumpChanged);

            if (rumbleToggle != null)
                rumbleToggle.onValueChanged.AddListener(OnRumbleChanged);
        }

        private void OnDisable()
        {
            if (_pauseAction != null)
                _pauseAction.performed -= OnPausePerformed;

            if (_backAction != null)
                _backAction.performed -= OnBackPerformed;

            if (sensitivitySlider != null)
                sensitivitySlider.onValueChanged.RemoveListener(OnSensitivityChanged);

            if (invertYToggle != null)
                invertYToggle.onValueChanged.RemoveListener(OnInvertYChanged);

            if (autoCamToggle != null)
                autoCamToggle.onValueChanged.RemoveListener(OnAutoCamChanged);

            if (attackOnJumpToggle != null)
                attackOnJumpToggle.onValueChanged.RemoveListener(OnAttackOnJumpChanged);

            if (rumbleToggle != null)
                rumbleToggle.onValueChanged.RemoveListener(OnRumbleChanged);
        }

        private void Update()
        {
            if (_isPaused)
            {
                if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
                {
                    if (EventSystem.current != null)
                    {
                        GameObject current = EventSystem.current.currentSelectedGameObject;
                        if (current != null)
                        {
                            ExecuteEvents.Execute(current, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
                        }
                    }
                }
            }

            if (_pendingSelection != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(_pendingSelection);
                _pendingSelection = null;
            }

            UpdateSelectionJuice();
        }

        private void UpdateSelectionJuice()
        {
            if (_selectableTransforms == null || _selectableBaseScales == null)
                return;

            GameObject selected = null;
            if (EventSystem.current != null)
                selected = EventSystem.current.currentSelectedGameObject;

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

        private void QueueSelection(GameObject target)
        {
            if (target == null) return;
            if (EventSystem.current == null) return;
            _pendingSelection = target;
        }

        private void OnPausePerformed(InputAction.CallbackContext ctx)
        {
            TogglePause();
        }

        private void OnBackPerformed(InputAction.CallbackContext ctx)
        {
            if (!_isPaused) return;

            if (optionsPanel != null && optionsPanel.activeSelf)
            {
                OnOptionsBackButton();
            }
            else if (quitConfirmPanel != null && quitConfirmPanel.activeSelf)
            {
                OnQuitNoButton();
            }
            else if (pausePanel != null && pausePanel.activeSelf)
            {
                StartCoroutine(ResumeWithDelay());
            }
        }

        public void TogglePause()
        {
            if (_isPaused)
            {
                StartCoroutine(ResumeWithDelay());
            }
            else
            {
                PauseGame();
            }
        }

        public void PauseGame()
        {
            if (_isPaused) return;

            _isPaused = true;
            Time.timeScale = 0f;

            if (_pogoAbility == null)
                _pogoAbility = FindFirstObjectByType<PogoDashAbility>();
            if (_starterInputs == null)
                _starterInputs = FindFirstObjectByType<StarterAssetsInputs>();
            if (_thirdPersonController == null)
                _thirdPersonController = FindFirstObjectByType<ThirdPersonController>();

            if (_pogoAbility != null)
            {
                _pogoAbility.ForceStopScreenShake();
                _pogoAbility.enabled = false;
            }

            if (_jumpAction != null) _jumpAction.Disable();
            if (_pogoAction != null) _pogoAction.Disable();
            if (_moveAction != null) _moveAction.Disable();
            if (_lookAction != null) _lookAction.Disable();

            if (_starterInputs != null)
            {
                _starterInputs.jump = false;
                _starterInputs.pogo = false;
            }

            if (pauseMenuRoot != null)
                pauseMenuRoot.SetActive(true);

            ShowPausePanelOnPause();

            if (pauseMenuRootCanvasGroup == null && pauseMenuRoot != null)
                pauseMenuRootCanvasGroup = pauseMenuRoot.GetComponent<CanvasGroup>();

            if (pauseMenuRootCanvasGroup != null)
            {
                pauseMenuRootCanvasGroup.alpha = 0f;
                pauseMenuRootCanvasGroup.interactable = false;
                pauseMenuRootCanvasGroup.blocksRaycasts = false;

                if (_rootFadeCoroutine != null)
                    StopCoroutine(_rootFadeCoroutine);
                _rootFadeCoroutine = StartCoroutine(FadeInRootRoutine());
            }

            if (firstPauseButton != null)
                QueueSelection(firstPauseButton.gameObject);
        }

        private void ResumeGameCore()
        {
            _isPaused = false;
            Time.timeScale = 1f;

            if (_pogoAbility == null)
                _pogoAbility = FindFirstObjectByType<PogoDashAbility>();

            if (_pogoAbility != null)
                _pogoAbility.enabled = true;

            if (pauseMenuRootCanvasGroup == null && pauseMenuRoot != null)
                pauseMenuRootCanvasGroup = pauseMenuRoot.GetComponent<CanvasGroup>();

            if (pauseMenuRootCanvasGroup != null)
            {
                if (_rootFadeCoroutine != null)
                    StopCoroutine(_rootFadeCoroutine);
                _rootFadeCoroutine = StartCoroutine(FadeOutRootRoutine());
            }
            else
            {
                if (pauseMenuRoot != null)
                    pauseMenuRoot.SetActive(false);
            }

            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);

            _pendingSelection = null;
        }

        private IEnumerator ResumeWithDelay()
        {
            if (_jumpAction != null) _jumpAction.Disable();
            if (_pogoAction != null) _pogoAction.Disable();
            if (_moveAction != null) _moveAction.Disable();
            if (_lookAction != null) _lookAction.Disable();

            if (_starterInputs != null)
            {
                _starterInputs.jump = false;
                _starterInputs.pogo = false;
            }

            ResumeGameCore();

            yield return new WaitForSecondsRealtime(0.1f);

            if (_starterInputs != null)
            {
                _starterInputs.jump = false;
                _starterInputs.pogo = false;
            }

            if (_jumpAction != null) _jumpAction.Enable();
            if (_pogoAction != null) _pogoAction.Enable();
            if (_moveAction != null) _moveAction.Enable();
            if (_lookAction != null) _lookAction.Enable();
        }

        private IEnumerator FadeInRootRoutine()
        {
            if (pauseMenuRootCanvasGroup == null)
            {
                _rootFadeCoroutine = null;
                yield break;
            }

            float duration = Mathf.Max(0.0001f, rootFadeDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                pauseMenuRootCanvasGroup.alpha = t;
                yield return null;
            }

            pauseMenuRootCanvasGroup.alpha = 1f;
            pauseMenuRootCanvasGroup.interactable = true;
            pauseMenuRootCanvasGroup.blocksRaycasts = true;

            _rootFadeCoroutine = null;
        }

        private IEnumerator FadeOutRootRoutine()
        {
            if (pauseMenuRootCanvasGroup == null)
            {
                if (pauseMenuRoot != null)
                    pauseMenuRoot.SetActive(false);
                _rootFadeCoroutine = null;
                yield break;
            }

            pauseMenuRootCanvasGroup.interactable = false;
            pauseMenuRootCanvasGroup.blocksRaycasts = false;

            float duration = Mathf.Max(0.0001f, rootFadeDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                pauseMenuRootCanvasGroup.alpha = 1f - t;
                yield return null;
            }

            pauseMenuRootCanvasGroup.alpha = 0f;

            if (pauseMenuRoot != null)
                pauseMenuRoot.SetActive(false);

            _rootFadeCoroutine = null;
        }

        private void TransitionToPanel(GameObject targetPanel, CanvasGroup targetCanvasGroup, GameObject firstSelected)
        {
            if (_panelTransitionCoroutine != null)
                return;

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

            bool goingToOptions = (targetPanel == optionsPanel);
            bool leavingOptions = (_currentPanel == optionsPanel);
            bool involvesOptions = goingToOptions || leavingOptions;

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

                if (involvesOptions)
                {
                    if (goingToOptions)
                    {
                        if (cadre1CanvasGroup != null) cadre1CanvasGroup.alpha = 0f;
                        if (cadre2CanvasGroup != null) cadre2CanvasGroup.alpha = 1f;
                    }
                    else if (leavingOptions)
                    {
                        if (cadre1CanvasGroup != null) cadre1CanvasGroup.alpha = 1f;
                        if (cadre2CanvasGroup != null) cadre2CanvasGroup.alpha = 0f;
                    }
                }
                else
                {
                    if (cadre1CanvasGroup != null) cadre1CanvasGroup.alpha = 1f;
                    if (cadre2CanvasGroup != null) cadre2CanvasGroup.alpha = 0f;
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
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / panelFadeDuration);

                if (_currentPanelCanvasGroup != null)
                    _currentPanelCanvasGroup.alpha = 1f - t;

                if (involvesOptions)
                {
                    if (goingToOptions && cadre1CanvasGroup != null)
                        cadre1CanvasGroup.alpha = 1f - t;
                    if (leavingOptions && cadre2CanvasGroup != null)
                        cadre2CanvasGroup.alpha = 1f - t;
                }

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
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / panelFadeDuration);

                if (targetCanvasGroup != null)
                    targetCanvasGroup.alpha = t;

                if (involvesOptions)
                {
                    if (goingToOptions && cadre2CanvasGroup != null)
                        cadre2CanvasGroup.alpha = t;
                    if (leavingOptions && cadre1CanvasGroup != null)
                        cadre1CanvasGroup.alpha = t;
                }

                yield return null;
            }

            if (targetCanvasGroup != null)
            {
                targetCanvasGroup.alpha = 1f;
                targetCanvasGroup.interactable = true;
                targetCanvasGroup.blocksRaycasts = true;
            }

            if (involvesOptions)
            {
                if (goingToOptions)
                {
                    if (cadre1CanvasGroup != null) cadre1CanvasGroup.alpha = 0f;
                    if (cadre2CanvasGroup != null) cadre2CanvasGroup.alpha = 1f;
                }
                else
                {
                    if (cadre1CanvasGroup != null) cadre1CanvasGroup.alpha = 1f;
                    if (cadre2CanvasGroup != null) cadre2CanvasGroup.alpha = 0f;
                }
            }
            else
            {
                if (cadre1CanvasGroup != null) cadre1CanvasGroup.alpha = 1f;
                if (cadre2CanvasGroup != null) cadre2CanvasGroup.alpha = 0f;
            }

            _currentPanel = targetPanel;
            _currentPanelCanvasGroup = targetCanvasGroup;

            if (firstSelected != null)
                QueueSelection(firstSelected);

            _panelTransitionCoroutine = null;
        }

        private void ShowPausePanelOnPause()
        {
            if (pausePanel != null) pausePanel.SetActive(true);
            if (optionsPanel != null) optionsPanel.SetActive(false);
            if (quitConfirmPanel != null) quitConfirmPanel.SetActive(false);

            if (pausePanelCanvasGroup != null)
            {
                pausePanelCanvasGroup.alpha = 1f;
                pausePanelCanvasGroup.interactable = true;
                pausePanelCanvasGroup.blocksRaycasts = true;
            }
            if (optionsPanelCanvasGroup != null)
            {
                optionsPanelCanvasGroup.alpha = 0f;
                optionsPanelCanvasGroup.interactable = false;
                optionsPanelCanvasGroup.blocksRaycasts = false;
            }
            if (quitConfirmPanelCanvasGroup != null)
            {
                quitConfirmPanelCanvasGroup.alpha = 0f;
                quitConfirmPanelCanvasGroup.interactable = false;
                quitConfirmPanelCanvasGroup.blocksRaycasts = false;
            }

            if (cadre1CanvasGroup != null) cadre1CanvasGroup.alpha = 1f;
            if (cadre2CanvasGroup != null) cadre2CanvasGroup.alpha = 0f;

            _currentPanel = pausePanel;
            _currentPanelCanvasGroup = pausePanelCanvasGroup;
        }

        private void ShowPausePanel()
        {
            GameObject target = firstPauseButton != null ? firstPauseButton.gameObject : null;
            TransitionToPanel(pausePanel, pausePanelCanvasGroup, target);
        }

        private void ShowPausePanel(GameObject openerButton)
        {
            GameObject target = openerButton != null
                ? openerButton
                : (firstPauseButton != null ? firstPauseButton.gameObject : null);

            TransitionToPanel(pausePanel, pausePanelCanvasGroup, target);
        }

        private void ShowOptionsPanel()
        {
            RefreshOptionsUI();

            GameObject target = firstOptionsButton != null
                ? firstOptionsButton.gameObject
                : (optionsBackButton != null ? optionsBackButton.gameObject : null);

            TransitionToPanel(optionsPanel, optionsPanelCanvasGroup, target);
        }

        private void ShowQuitConfirmPanel()
        {
            GameObject target = firstQuitConfirmButton != null ? firstQuitConfirmButton.gameObject : null;
            TransitionToPanel(quitConfirmPanel, quitConfirmPanelCanvasGroup, target);
        }

        public void OnResumeButton()
        {
            StartCoroutine(ResumeWithDelay());
        }

        public void OnOptionsButton()
        {
            ShowOptionsPanel();
        }

        public void OnQuitButton()
        {
            ShowQuitConfirmPanel();
        }

        public void OnOptionsBackButton()
        {
            ShowPausePanel(pauseOptionsButton != null ? pauseOptionsButton.gameObject : null);
        }

        public void OnQuitNoButton()
        {
            ShowPausePanel(pauseQuitButton != null ? pauseQuitButton.gameObject : null);
        }

        public void OnQuitYesButton()
        {
            _isPaused = false;
            Time.timeScale = 1f;

            if (pauseMenuRoot != null)
                pauseMenuRoot.SetActive(false);

            SceneManager.LoadScene(quitSceneName, LoadSceneMode.Single);
        }

        private void RefreshOptionsUI()
        {
            if (_thirdPersonController == null)
                _thirdPersonController = FindFirstObjectByType<ThirdPersonController>();
            if (_pogoAbility == null)
                _pogoAbility = FindFirstObjectByType<PogoDashAbility>();

            if (sensitivitySlider != null && _thirdPersonController != null)
            {
                float sens = _thirdPersonController.LookSensitivity;
                float sliderValue = Mathf.InverseLerp(LookSensMin, LookSensMax, sens);
                sensitivitySlider.minValue = 0f;
                sensitivitySlider.maxValue = 1f;
                sensitivitySlider.value = sliderValue;
            }

            if (invertYToggle != null && _thirdPersonController != null)
                invertYToggle.isOn = _thirdPersonController.InvertY;

            if (autoCamToggle != null && _thirdPersonController != null)
                autoCamToggle.isOn = _thirdPersonController.AutoCamGlobal;

            if (attackOnJumpToggle != null && _pogoAbility != null)
                attackOnJumpToggle.isOn = _pogoAbility.AttackOnJump;

            if (rumbleToggle != null)
                rumbleToggle.isOn = GameRumbleSettings.RumbleEnabled;
        }

        private void OnSensitivityChanged(float value)
        {
            if (_thirdPersonController == null)
                _thirdPersonController = FindFirstObjectByType<ThirdPersonController>();

            if (_thirdPersonController != null)
                _thirdPersonController.LookSensitivity = Mathf.Lerp(LookSensMin, LookSensMax, value);
        }

        private void OnInvertYChanged(bool isOn)
        {
            if (_thirdPersonController == null)
                _thirdPersonController = FindFirstObjectByType<ThirdPersonController>();

            if (_thirdPersonController != null)
                _thirdPersonController.InvertY = isOn;
        }

        private void OnAutoCamChanged(bool isOn)
        {
            if (_thirdPersonController == null)
                _thirdPersonController = FindFirstObjectByType<ThirdPersonController>();

            if (_thirdPersonController != null)
                _thirdPersonController.AutoCamGlobal = isOn;
        }

        private void OnAttackOnJumpChanged(bool isOn)
        {
            if (_pogoAbility == null)
                _pogoAbility = FindFirstObjectByType<PogoDashAbility>();

            if (_pogoAbility != null)
                _pogoAbility.AttackOnJump = isOn;
        }

        private void OnRumbleChanged(bool isOn)
        {
            GameRumbleSettings.SetRumbleEnabled(isOn);
        }
    }
}
