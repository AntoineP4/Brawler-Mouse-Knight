using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour
    {
        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 2.0f;

        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 5.335f;

        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Header("Apex Float")]
        public float ApexThreshold = 1.0f;
        public float ApexHangTime = 0.12f;
        [Range(0.01f, 1f)] public float ApexGravityScale = 0.25f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;

        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 70.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;

        [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;

        [Header("Camera Input")]
        [Tooltip("Sensitivity multiplier for look input (sera mappé par le slider)")]
        public float LookSensitivity = 1.0f;

        [Tooltip("Invert vertical look axis")]
        public bool InvertY = false;

        [Header("Auto Cam")]
        [Tooltip("Master ON/OFF controlled by options menu")]
        public bool AutoCamGlobal = true;

        [Tooltip("Per-profile auto cam enable (set by AutoCamProfile)")]
        public bool AutoCam = true;

        [Tooltip("Tilt speed (deg/sec) when going UP (in air)")]
        public float AirTiltSpeedUp = 60f;

        [Tooltip("Tilt speed (deg/sec) when going DOWN (on ground)")]
        public float AirTiltSpeedDown = 40f;

        [Tooltip("Degrees where auto cam vertical movement slows near its target (up and down)")]
        public float AutoCamTopSlowdownRange = 10f;

        [Tooltip("Time to ease in downward auto cam after ground contact")]
        public float AutoCamDownEaseInTime = 0.25f;

        [Tooltip("Minimum time in air before auto cam starts (seconds)")]
        public float AutoCamMinAirTime = 0.1f;

        [Tooltip("Delay after landing before auto cam recenters (seconds)")]
        public float AutoCamGroundDelay = 0.2f;

        [Header("External Locks")]
        [Tooltip("If false, horizontal movement from the left stick is disabled (used by recoil/dash, etc.)")]
        public bool CanMove = true;

        [Tooltip("If true, external abilities temporarily cancel gravity while in air.")]
        public bool LockGravityExternally = false;

        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

#if ENABLE_INPUT_SYSTEM
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;

        private const float _threshold = 0.01f;

        private bool _hasAnimator;

        private float _apexTimer;

        private float _autoTiltOffset = 0f;
        private float _timeSinceUngrounded = 0f;
        private float _timeSinceGrounded = 0f;
        private bool _autoCamActive = false;
        private bool _autoCamEligibleThisAir = false;
        private bool _wasGrounded = true;

        private bool _externalBounceRequested = false;

        private const float LookSensMin = 0.5f;
        private const float LookSensMax = 1.5f;
        private const float DefaultSliderValue = 0.5f;

        // ---- CAGE LOCK ----
        private bool _movementLockedByCage = false;
        private int _cageLayer = -1;
        // --------------------

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
                return false;
#endif
            }
        }

        private void Awake()
        {
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }

            float defaultSens = Mathf.Lerp(LookSensMin, LookSensMax, DefaultSliderValue);
            LookSensitivity = defaultSens;

            InvertY = false;
            AutoCamGlobal = true;
        }

        private void Start()
        {
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM
            _playerInput = GetComponent<PlayerInput>();
#else
            Debug.LogError("Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

            AssignAnimationIDs();

            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
            _apexTimer = 0f;

            // récupérer l'index du layer "Cage" (si existe)
            _cageLayer = LayerMask.NameToLayer("Cage");
        }

        private void Update()
        {
            _hasAnimator = TryGetComponent(out _animator);

            JumpAndGravity();
            GroundedCheck();
            Move();
        }

        private void LateUpdate()
        {
            CameraRotation();
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        private void GroundedCheck()
        {
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);

            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }

        private void CameraRotation()
        {
            if (Grounded)
            {
                if (!_wasGrounded)
                {
                    _timeSinceGrounded = 0f;
                }

                _timeSinceGrounded += Time.deltaTime;
                _timeSinceUngrounded = 0f;
            }
            else
            {
                if (_wasGrounded)
                {
                    _autoCamEligibleThisAir = true;
                    _autoCamActive = false;
                    _autoTiltOffset = 0f;
                    _timeSinceUngrounded = 0f;
                }

                _timeSinceUngrounded += Time.deltaTime;
                _timeSinceGrounded = 0f;
            }

            _wasGrounded = Grounded;

            if (!LockCameraPosition)
            {
                float baseMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;
                float deltaTimeMultiplier = baseMultiplier * Mathf.Clamp(LookSensitivity, LookSensMin, LookSensMax);

                bool hasLookInput = _input.look.sqrMagnitude >= _threshold;

                bool useAutoCam = AutoCamGlobal && AutoCam;

                if (hasLookInput)
                {
                    float lookX = _input.look.x;
                    float lookY = _input.look.y;

                    if (InvertY)
                        lookY = -lookY;

                    _cinemachineTargetYaw += lookX * deltaTimeMultiplier;
                    _cinemachineTargetPitch += lookY * deltaTimeMultiplier;

                    if (useAutoCam)
                    {
                        _autoCamEligibleThisAir = false;
                        _autoCamActive = false;
                        _autoTiltOffset = 0f;
                    }
                }

                if (useAutoCam)
                {
                    if (!Grounded)
                    {
                        if (_autoCamEligibleThisAir && !_autoCamActive && _timeSinceUngrounded >= AutoCamMinAirTime)
                        {
                            _autoCamActive = true;
                        }

                        if (_autoCamActive)
                        {
                            float speedUp = AirTiltSpeedUp;

                            if (AutoCamTopSlowdownRange > 0f)
                            {
                                float distanceToTop = TopClamp - _cinemachineTargetPitch;
                                if (distanceToTop <= AutoCamTopSlowdownRange)
                                {
                                    float t = Mathf.Clamp01(distanceToTop / AutoCamTopSlowdownRange);
                                    float slowFactor = Mathf.Lerp(0.1f, 1f, t);
                                    speedUp *= slowFactor;
                                }
                            }

                            float delta = speedUp * Time.deltaTime;
                            _cinemachineTargetPitch += delta;
                            _autoTiltOffset += delta;
                        }
                    }
                    else
                    {
                        if (_autoCamActive && Mathf.Abs(_autoTiltOffset) > 0.001f)
                        {
                            float moveTime = _timeSinceGrounded;
                            float easeFactor = AutoCamDownEaseInTime > 0f
                                ? Mathf.Clamp01(moveTime / AutoCamDownEaseInTime)
                                : 1f;

                            float maxStep = AirTiltSpeedDown * easeFactor * Time.deltaTime;

                            if (AutoCamTopSlowdownRange > 0f)
                            {
                                float absOffset = Mathf.Abs(_autoTiltOffset);
                                if (absOffset <= AutoCamTopSlowdownRange)
                                {
                                    float t2 = Mathf.Clamp01(absOffset / AutoCamTopSlowdownRange);
                                    float slowFactor2 = Mathf.Lerp(0.1f, 1f, t2);
                                    maxStep *= slowFactor2;
                                }
                            }

                            float step = Mathf.Min(Mathf.Abs(_autoTiltOffset), maxStep);
                            float sign = Mathf.Sign(_autoTiltOffset);

                            _cinemachineTargetPitch -= step * sign;
                            _autoTiltOffset -= step * sign;

                            if (Mathf.Abs(_autoTiltOffset) <= 0.001f)
                            {
                                _autoCamActive = false;
                                _autoTiltOffset = 0f;
                            }
                        }
                    }
                }
            }

            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            CinemachineCameraTarget.transform.rotation =
                Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride, _cinemachineTargetYaw, 0.0f);
        }

        private void Move()
        {
            // ----- LOCK DEPLACEMENT PAR CAGE -----
            if (_cageLayer >= 0)
            {
                int cageMask = 1 << _cageLayer;
                Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);

                bool cageContact = Physics.CheckSphere(
                    spherePosition,
                    GroundedRadius,
                    cageMask,
                    QueryTriggerInteraction.Ignore
                );

                if (cageContact)
                {
                    // on touche encore la cage : on verrouille
                    _movementLockedByCage = true;
                }
                else if (Grounded && !cageContact)
                {
                    // on est à nouveau grounded mais plus sur la cage : on libère
                    _movementLockedByCage = false;
                }
            }
            // -------------------------------------

            bool canMove = CanMove && !_movementLockedByCage;

            float targetSpeed = canMove ? (_input.sprint ? SprintSpeed : MoveSpeed) : 0.0f;
            if (canMove && _input.move == Vector2.zero) targetSpeed = 0.0f;

            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = canMove ? (_input.analogMovement ? _input.move.magnitude : (_input.move == Vector2.zero ? 0f : 1f)) : 0f;

            if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            Vector3 inputDirection = canMove ? new Vector3(_input.move.x, 0.0f, _input.move.y).normalized : Vector3.zero;

            if (canMove && _input.move != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);

                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }

            Vector3 targetDirection = canMove ? (Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward) : Vector3.zero;

            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }

        private void JumpAndGravity()
        {
            bool handleAsGrounded = Grounded && !_externalBounceRequested;

            if (handleAsGrounded)
            {
                _fallTimeoutDelta = FallTimeout;

                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }
                }

                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }

                _apexTimer = 0f;
            }
            else
            {
                if (Grounded && _externalBounceRequested)
                {
                    Grounded = false;
                }

                _jumpTimeoutDelta = JumpTimeout;

                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }

                _input.jump = false;

                bool nearApex = Mathf.Abs(_verticalVelocity) <= ApexThreshold;
                if (nearApex)
                {
                    _apexTimer += Time.deltaTime;
                }
                else
                {
                    _apexTimer = 0f;
                }

                float gravityScale = (nearApex && _apexTimer <= ApexHangTime) ? ApexGravityScale : 1f;

                if (LockGravityExternally)
                {
                    _verticalVelocity = 0f;
                }
                else
                {
                    if (_verticalVelocity < _terminalVelocity)
                    {
                        _verticalVelocity += Gravity * gravityScale * Time.deltaTime;
                    }
                }
            }

            _externalBounceRequested = false;
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            Gizmos.color = Grounded ? transparentGreen : transparentRed;

            Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    var index = Random.Range(0, FootstepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
                }
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }

        public void Bounce(float height)
        {
            _externalBounceRequested = true;
            _verticalVelocity = Mathf.Sqrt(height * -2f * Gravity);

            if (_hasAnimator)
            {
                _animator.SetBool(_animIDJump, true);
                _animator.SetBool(_animIDFreeFall, false);
            }
        }

        public void SetVerticalVelocity(float value)
        {
            _verticalVelocity = value;
        }

        public void SetAutoCamProfile(AutoCamProfile profile)
        {
            if (profile == null) return;

            TopClamp = profile.TopClamp;
            BottomClamp = profile.BottomClamp;

            AutoCam = profile.AutoCam;
            AirTiltSpeedUp = profile.AirTiltSpeedUp;
            AirTiltSpeedDown = profile.AirTiltSpeedDown;
            AutoCamTopSlowdownRange = profile.AutoCamTopSlowdownRange;
            AutoCamDownEaseInTime = profile.AutoCamDownEaseInTime;
            AutoCamMinAirTime = profile.AutoCamMinAirTime;
            AutoCamGroundDelay = profile.AutoCamGroundDelay;

            CameraAngleOverride = profile.CameraAngleOverride;
        }
    }
}
