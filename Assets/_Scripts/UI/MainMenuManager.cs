using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Cinemachine;

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

        [Header("Quit Confirm")]
        [SerializeField] private Button firstMainQuitConfirmButton;
        [SerializeField] private Button mainQuitNoButton;
        [SerializeField] private Button mainQuitYesButton;

        [Header("Credits")]
        [SerializeField] private Button firstMainCreditsButton;
        [SerializeField] private Button mainCreditsBackButton;

        [Header("HUD / Gameplay UI")]
        [SerializeField] private GameObject gameplayUIRoot;

        [Header("Camera")]
        [SerializeField] private CinemachineVirtualCamera menuCamera;

        [Header("Camera Lock")]
        [SerializeField] private float lookUnlockDelay = 1f;

        [Header("Gameplay Refs")]
        [SerializeField] private ThirdPersonController thirdPersonController;
        [SerializeField] private PogoDashAbility pogoDashAbility;
        [SerializeField] private StarterAssetsInputs starterInputs;

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

        private const float LookSensMin = 0.5f;
        private const float LookSensMax = 1.5f;

        private void Awake()
        {
            if (gameplayUIRoot != null)
                gameplayUIRoot.SetActive(false);

            if (_playerInput == null)
                _playerInput = FindFirstObjectByType<PlayerInput>();

            if (thirdPersonController == null)
                thirdPersonController = FindFirstObjectByType<ThirdPersonController>();
            if (pogoDashAbility == null)
                pogoDashAbility = FindFirstObjectByType<PogoDashAbility>();
            if (starterInputs == null)
                starterInputs = FindFirstObjectByType<StarterAssetsInputs>();

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

            if (menuCamera != null)
                menuCamera.Priority = 20;

            if (startButton != null) startButton.onClick.AddListener(OnStartClicked);
            if (optionsButton != null) optionsButton.onClick.AddListener(OnOptionsClicked);
            if (creditsButton != null) creditsButton.onClick.AddListener(OnCreditsClicked);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);

            if (mainOptionsBackButton != null) mainOptionsBackButton.onClick.AddListener(OnMainOptionsBackClicked);
            if (mainQuitNoButton != null) mainQuitNoButton.onClick.AddListener(OnMainQuitNoClicked);
            if (mainQuitYesButton != null) mainQuitYesButton.onClick.AddListener(OnMainQuitYesClicked);
            if (mainCreditsBackButton != null) mainCreditsBackButton.onClick.AddListener(OnMainCreditsBackClicked);

            if (mainSensitivitySlider != null) mainSensitivitySlider.onValueChanged.AddListener(OnMainSensitivityChanged);
            if (mainInvertYToggle != null) mainInvertYToggle.onValueChanged.AddListener(OnMainInvertYChanged);
            if (mainAutoCamToggle != null) mainAutoCamToggle.onValueChanged.AddListener(OnMainAutoCamChanged);
            if (mainAttackOnJumpToggle != null) mainAttackOnJumpToggle.onValueChanged.AddListener(OnMainAttackOnJumpChanged);

            RefreshMainOptionsUI();

            if (EventSystem.current != null && firstMainButton != null)
                EventSystem.current.SetSelectedGameObject(firstMainButton.gameObject);
        }

        private void OnDestroy()
        {
            if (startButton != null) startButton.onClick.RemoveListener(OnStartClicked);
            if (optionsButton != null) optionsButton.onClick.RemoveListener(OnOptionsClicked);
            if (creditsButton != null) creditsButton.onClick.RemoveListener(OnCreditsClicked);
            if (quitButton != null) quitButton.onClick.RemoveListener(OnQuitClicked);

            if (mainOptionsBackButton != null) mainOptionsBackButton.onClick.RemoveListener(OnMainOptionsBackClicked);
            if (mainQuitNoButton != null) mainQuitNoButton.onClick.RemoveListener(OnMainQuitNoClicked);
            if (mainQuitYesButton != null) mainQuitYesButton.onClick.RemoveListener(OnMainQuitYesClicked);
            if (mainCreditsBackButton != null) mainCreditsBackButton.onClick.RemoveListener(OnMainCreditsBackClicked);

            if (mainSensitivitySlider != null) mainSensitivitySlider.onValueChanged.RemoveListener(OnMainSensitivityChanged);
            if (mainInvertYToggle != null) mainInvertYToggle.onValueChanged.RemoveListener(OnMainInvertYChanged);
            if (mainAutoCamToggle != null) mainAutoCamToggle.onValueChanged.RemoveListener(OnMainAutoCamChanged);
            if (mainAttackOnJumpToggle != null) mainAttackOnJumpToggle.onValueChanged.RemoveListener(OnMainAttackOnJumpChanged);
        }

        private void OnEnable()
        {
            if (_backAction != null)
                _backAction.performed += OnBackPerformed;
        }

        private void OnDisable()
        {
            if (_backAction != null)
                _backAction.performed -= OnBackPerformed;
        }

        private bool BackPressedThisFrame()
        {
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

            if (_inQuitConfirm)
            {
                if (EventSystem.current.currentSelectedGameObject == null && firstMainQuitConfirmButton != null)
                    EventSystem.current.SetSelectedGameObject(firstMainQuitConfirmButton.gameObject);

                if (BackPressedThisFrame())
                    OnMainQuitNoClicked();
            }
            else if (_inOptions)
            {
                if (EventSystem.current.currentSelectedGameObject == null && firstMainOptionsButton != null)
                    EventSystem.current.SetSelectedGameObject(firstMainOptionsButton.gameObject);

                if (BackPressedThisFrame())
                    OnMainOptionsBackClicked();
            }
            else if (_inCredits)
            {
                if (EventSystem.current.currentSelectedGameObject == null && firstMainCreditsButton != null)
                    EventSystem.current.SetSelectedGameObject(firstMainCreditsButton.gameObject);

                if (BackPressedThisFrame())
                    OnMainCreditsBackClicked();
            }
            else
            {
                if (EventSystem.current.currentSelectedGameObject == null && firstMainButton != null)
                    EventSystem.current.SetSelectedGameObject(firstMainButton.gameObject);
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

        private void OnBackPerformed(InputAction.CallbackContext ctx)
        {
            if (_inOptions)
                OnMainOptionsBackClicked();
            else if (_inCredits)
                OnMainCreditsBackClicked();
            else if (_inQuitConfirm)
                OnMainQuitNoClicked();
        }

        private void OnStartClicked()
        {
            if (_gameStarted) return;
            _gameStarted = true;

            if (gameplayUIRoot != null)
                gameplayUIRoot.SetActive(true);

            if (mainMenuRoot != null)
                mainMenuRoot.SetActive(false);

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

            if (_moveAction != null) _moveAction.Enable();
            if (_pauseAction != null) _pauseAction.Enable();
            if (_jumpAction != null) _jumpAction.Enable();
            if (_dashAction != null) _dashAction.Enable();
            if (_pogoAction != null) _pogoAction.Enable();
            if (_lookAction != null) _lookAction.Disable();

            if (_unlockLookCoroutine != null)
                StopCoroutine(_unlockLookCoroutine);
            _unlockLookCoroutine = StartCoroutine(UnlockLookAfterDelay());
        }

        private IEnumerator UnlockLookAfterDelay()
        {
            yield return new WaitForSeconds(lookUnlockDelay);
            if (_lookAction != null)
                _lookAction.Enable();
            _unlockLookCoroutine = null;
        }

        private void OnOptionsClicked()
        {
            _inOptions = true;
            _inQuitConfirm = false;
            _inCredits = false;

            if (mainPanel != null) mainPanel.SetActive(false);
            if (mainOptionsPanel != null) mainOptionsPanel.SetActive(true);
            if (mainQuitConfirmPanel != null) mainQuitConfirmPanel.SetActive(false);
            if (mainCreditsPanel != null) mainCreditsPanel.SetActive(false);

            RefreshMainOptionsUI();

            if (EventSystem.current != null && firstMainOptionsButton != null)
                EventSystem.current.SetSelectedGameObject(firstMainOptionsButton.gameObject);
        }

        private void OnCreditsClicked()
        {
            _inCredits = true;
            _inOptions = false;
            _inQuitConfirm = false;

            if (mainPanel != null) mainPanel.SetActive(false);
            if (mainOptionsPanel != null) mainOptionsPanel.SetActive(false);
            if (mainQuitConfirmPanel != null) mainQuitConfirmPanel.SetActive(false);
            if (mainCreditsPanel != null) mainCreditsPanel.SetActive(true);

            if (EventSystem.current != null && firstMainCreditsButton != null)
                EventSystem.current.SetSelectedGameObject(firstMainCreditsButton.gameObject);
        }

        private void OnQuitClicked()
        {
            _inQuitConfirm = true;
            _inOptions = false;
            _inCredits = false;

            if (mainPanel != null) mainPanel.SetActive(false);
            if (mainOptionsPanel != null) mainOptionsPanel.SetActive(false);
            if (mainCreditsPanel != null) mainCreditsPanel.SetActive(false);
            if (mainQuitConfirmPanel != null) mainQuitConfirmPanel.SetActive(true);

            if (EventSystem.current != null && firstMainQuitConfirmButton != null)
                EventSystem.current.SetSelectedGameObject(firstMainQuitConfirmButton.gameObject);
        }

        private void OnMainOptionsBackClicked()
        {
            _inOptions = false;

            if (mainOptionsPanel != null) mainOptionsPanel.SetActive(false);
            if (mainQuitConfirmPanel != null) mainQuitConfirmPanel.SetActive(false);
            if (mainCreditsPanel != null) mainCreditsPanel.SetActive(false);
            if (mainPanel != null) mainPanel.SetActive(true);

            if (EventSystem.current != null && optionsButton != null)
                EventSystem.current.SetSelectedGameObject(optionsButton.gameObject);
        }

        private void OnMainQuitNoClicked()
        {
            _inQuitConfirm = false;

            if (mainQuitConfirmPanel != null) mainQuitConfirmPanel.SetActive(false);
            if (mainOptionsPanel != null) mainOptionsPanel.SetActive(false);
            if (mainCreditsPanel != null) mainCreditsPanel.SetActive(false);
            if (mainPanel != null) mainPanel.SetActive(true);

            if (EventSystem.current != null && quitButton != null)
                EventSystem.current.SetSelectedGameObject(quitButton.gameObject);
        }

        private void OnMainQuitYesClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnMainCreditsBackClicked()
        {
            _inCredits = false;

            if (mainCreditsPanel != null) mainCreditsPanel.SetActive(false);
            if (mainOptionsPanel != null) mainOptionsPanel.SetActive(false);
            if (mainQuitConfirmPanel != null) mainQuitConfirmPanel.SetActive(false);
            if (mainPanel != null) mainPanel.SetActive(true);

            if (EventSystem.current != null && creditsButton != null)
                EventSystem.current.SetSelectedGameObject(creditsButton.gameObject);
        }

        private void RefreshMainOptionsUI()
        {
            if (thirdPersonController != null && mainSensitivitySlider != null)
            {
                float sens = thirdPersonController.LookSensitivity;
                float sliderValue = Mathf.InverseLerp(LookSensMin, LookSensMax, sens);
                mainSensitivitySlider.minValue = 0f;
                mainSensitivitySlider.maxValue = 1f;
                mainSensitivitySlider.value = sliderValue;
            }

            if (thirdPersonController != null && mainInvertYToggle != null)
                mainInvertYToggle.isOn = thirdPersonController.InvertY;

            if (thirdPersonController != null && mainAutoCamToggle != null)
                mainAutoCamToggle.isOn = thirdPersonController.AutoCamGlobal;

            if (pogoDashAbility != null && mainAttackOnJumpToggle != null)
                mainAttackOnJumpToggle.isOn = pogoDashAbility.AttackOnJump;
        }

        private void OnMainSensitivityChanged(float value)
        {
            if (thirdPersonController == null || mainSensitivitySlider == null) return;
            thirdPersonController.LookSensitivity = Mathf.Lerp(LookSensMin, LookSensMax, value);
        }

        private void OnMainInvertYChanged(bool isOn)
        {
            if (thirdPersonController == null) return;
            thirdPersonController.InvertY = isOn;
        }

        private void OnMainAutoCamChanged(bool isOn)
        {
            if (thirdPersonController == null) return;
            thirdPersonController.AutoCamGlobal = isOn;
        }

        private void OnMainAttackOnJumpChanged(bool isOn)
        {
            if (pogoDashAbility == null) return;
            pogoDashAbility.AttackOnJump = isOn;
        }
    }
}
