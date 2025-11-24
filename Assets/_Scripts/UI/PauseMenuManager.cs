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
        [SerializeField] private Button optionsBackButton;

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
        }

        private void Update()
        {
            if (!_isPaused) return;
            if (Keyboard.current == null) return;
            if (!Keyboard.current.enterKey.wasPressedThisFrame) return;

            if (EventSystem.current == null) return;
            GameObject current = EventSystem.current.currentSelectedGameObject;
            if (current == null) return;

            ExecuteEvents.Execute(current, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
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

            ShowPausePanel();

            if (EventSystem.current != null && firstPauseButton != null)
                EventSystem.current.SetSelectedGameObject(firstPauseButton.gameObject);
        }

        private void ResumeGameCore()
        {
            _isPaused = false;
            Time.timeScale = 1f;

            if (_pogoAbility == null)
                _pogoAbility = FindFirstObjectByType<PogoDashAbility>();

            if (_pogoAbility != null)
                _pogoAbility.enabled = true;

            if (pauseMenuRoot != null)
                pauseMenuRoot.SetActive(false);

            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
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

        private void ShowPausePanel()
        {
            if (pausePanel != null) pausePanel.SetActive(true);
            if (optionsPanel != null) optionsPanel.SetActive(false);
            if (quitConfirmPanel != null) quitConfirmPanel.SetActive(false);
        }

        private void ShowOptionsPanel()
        {
            if (pausePanel != null) pausePanel.SetActive(false);
            if (optionsPanel != null) optionsPanel.SetActive(true);
            if (quitConfirmPanel != null) quitConfirmPanel.SetActive(false);

            RefreshOptionsUI();

            Button target = firstOptionsButton != null ? firstOptionsButton : optionsBackButton;

            if (EventSystem.current != null && target != null)
                EventSystem.current.SetSelectedGameObject(target.gameObject);
        }

        private void ShowQuitConfirmPanel()
        {
            if (pausePanel != null) pausePanel.SetActive(false);
            if (optionsPanel != null) optionsPanel.SetActive(false);
            if (quitConfirmPanel != null) quitConfirmPanel.SetActive(true);

            if (EventSystem.current != null && firstQuitConfirmButton != null)
                EventSystem.current.SetSelectedGameObject(firstQuitConfirmButton.gameObject);
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
            ShowPausePanel();

            if (EventSystem.current != null)
            {
                Button target = pauseOptionsButton != null ? pauseOptionsButton : firstPauseButton;
                if (target != null)
                    EventSystem.current.SetSelectedGameObject(target.gameObject);
            }
        }

        public void OnQuitNoButton()
        {
            ShowPausePanel();

            if (EventSystem.current != null)
            {
                Button target = pauseQuitButton != null ? pauseQuitButton : firstPauseButton;
                if (target != null)
                    EventSystem.current.SetSelectedGameObject(target.gameObject);
            }
        }

        public void OnQuitYesButton()
        {
            _isPaused = false;
            Time.timeScale = 1f;

            if (pauseMenuRoot != null)
                pauseMenuRoot.SetActive(false);

            var scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.buildIndex, LoadSceneMode.Single);
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
    }
}
