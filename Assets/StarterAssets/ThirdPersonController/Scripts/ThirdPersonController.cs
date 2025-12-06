using System.Collections;
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
        public float MoveSpeed = 2.0f;
        public float SprintSpeed = 5.335f;
        [Range(0.0f, 0.3f)] public float RotationSmoothTime = 0.12f;
        public float SpeedChangeRate = 10.0f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Space(10)]
        public float JumpHeight = 1.2f;
        public float Gravity = -15.0f;

        [Header("Cage Jump")]
        public float CageJumpHeight = 1.2f;

        [Header("Apex Float")]
        public float ApexThreshold = 1.0f;
        public float ApexHangTime = 0.12f;
        [Range(0.01f, 1f)] public float ApexGravityScale = 0.25f;

        [Space(10)]
        public float JumpTimeout = 0.50f;
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        public bool Grounded = true;
        public float GroundedOffset = -0.14f;
        public float GroundedRadius = 0.28f;
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        public GameObject CinemachineCameraTarget;
        public float TopClamp = 70.0f;
        public float BottomClamp = -30.0f;
        public float CameraAngleOverride = 0.0f;
        public bool LockCameraPosition = false;

        [Header("Camera Input")]
        public float LookSensitivity = 1.0f;
        public bool InvertY = false;

        [Header("Auto Cam")]
        public bool AutoCamGlobal = true;
        public bool AutoCam = true;
        public float AirTiltSpeedUp = 60f;
        public float AirTiltSpeedDown = 40f;
        public float AutoCamTopSlowdownRange = 10f;
        public float AutoCamDownEaseInTime = 0.25f;
        public float AutoCamMinAirTime = 0.1f;
        public float AutoCamGroundDelay = 0.2f;
        public float AutoCamTopClamp = 70.0f;
        public float AutoCamBottomClamp = -30.0f;

        [Header("External Locks")]
        public bool CanMove = true;
        public bool LockGravityExternally = false;

        [Header("Cage Pogo Tutorial")]
        public CagePogoTutorial cagePogoTutorial;

        float _cinemachineTargetYaw;
        float _cinemachineTargetPitch;

        float _speed;
        float _animationBlend;
        float _targetRotation = 0.0f;
        float _rotationVelocity;
        float _verticalVelocity;
        float _terminalVelocity = 53.0f;

        float _jumpTimeoutDelta;
        float _fallTimeoutDelta;

        int _animIDSpeed;
        int _animIDGrounded;
        int _animIDJump;
        int _animIDFreeFall;
        int _animIDMotionSpeed;

#if ENABLE_INPUT_SYSTEM
        PlayerInput _playerInput;
#endif
        Animator _animator;
        CharacterController _controller;
        StarterAssetsInputs _input;
        GameObject _mainCamera;

        const float _threshold = 0.01f;

        bool _hasAnimator;

        float _apexTimer;

        float _autoTiltOffset = 0f;
        float _timeSinceUngrounded = 0f;
        float _timeSinceGrounded = 0f;
        bool _autoCamActive = false;
        bool _autoCamEligibleThisAir = false;
        bool _wasGrounded = true;

        bool _externalBounceRequested = false;

        const float LookSensMin = 0.5f;
        const float LookSensMax = 1.5f;
        const float DefaultSliderValue = 0.5f;

        bool _movementLockedByCage = false;
        int _cageLayer = -1;
        bool _groundedOnCage = false;
        bool _wasGroundedOnCage = false;

        float _currentTopClamp;
        float _currentBottomClamp;

        AutoCamProfile _pendingProfile;
        bool _hasPendingProfile;

        public bool IsMovementLockedByCage
        {
            get { return _movementLockedByCage; }
        }

        bool IsCurrentDeviceMouse
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

        void Awake()
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

        void Start()
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

            _cageLayer = LayerMask.NameToLayer("Cage");

            _currentTopClamp = TopClamp;
            _currentBottomClamp = BottomClamp;
        }

        void Update()
        {
            _hasAnimator = TryGetComponent(out _animator);

            JumpAndGravity();
            GroundedCheck();

            if (Grounded && _hasPendingProfile)
            {
                ApplyAutoCamProfileNow();
            }

            Move();
        }

        void LateUpdate()
        {
            CameraRotation();
        }

        void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        void GroundedCheck()
        {
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);

            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }

        void CameraRotation()
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
                            float targetTop = AutoCamTopClamp;

                            float distanceToTop = targetTop - _cinemachineTargetPitch;

                            if (distanceToTop <= 0f)
                            {
                                speedUp = 0f;
                            }
                            else if (AutoCamTopSlowdownRange > 0f && distanceToTop <= AutoCamTopSlowdownRange)
                            {
                                float t = Mathf.Clamp01(distanceToTop / AutoCamTopSlowdownRange);
                                float slowFactor = Mathf.Lerp(0.1f, 1f, t);
                                speedUp *= slowFactor;
                            }

                            float delta = speedUp * Time.deltaTime;

                            if (delta > distanceToTop)
                                delta = distanceToTop;

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
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, _currentBottomClamp, _currentTopClamp);

            if (_currentTopClamp > TopClamp && _cinemachineTargetPitch <= TopClamp)
            {
                _currentTopClamp = TopClamp;
            }

            if (_currentBottomClamp < BottomClamp && _cinemachineTargetPitch >= BottomClamp)
            {
                _currentBottomClamp = BottomClamp;
            }

            CinemachineCameraTarget.transform.rotation =
                Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride, _cinemachineTargetYaw, 0.0f);
        }

        void Move()
        {
            _groundedOnCage = false;

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

                bool groundedOnCageNow = Grounded && cageContact;

                if (groundedOnCageNow)
                {
                    _movementLockedByCage = true;
                }
                else if (Grounded && !cageContact)
                {
                    _movementLockedByCage = false;
                }

                if (groundedOnCageNow && !_wasGroundedOnCage && cagePogoTutorial != null)
                {
                    cagePogoTutorial.OnLandedBackOnCage();
                }

                _groundedOnCage = groundedOnCageNow;
                _wasGroundedOnCage = groundedOnCageNow;
            }

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

        void JumpAndGravity()
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
                    float usedJumpHeight = _groundedOnCage ? CageJumpHeight : JumpHeight;
                    _verticalVelocity = Mathf.Sqrt(usedJumpHeight * -2f * Gravity);

                    if (_groundedOnCage && cagePogoTutorial != null)
                    {
                        cagePogoTutorial.StartFirstJumpSequence();
                    }

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

            if (cagePogoTutorial != null)
            {
                cagePogoTutorial.UpdateMovementState(_verticalVelocity, Grounded);
            }

            _externalBounceRequested = false;
        }

        static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            Gizmos.color = Grounded ? transparentGreen : transparentRed;

            Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
        }

        void OnFootstep(AnimationEvent animationEvent)
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

        void OnLand(AnimationEvent animationEvent)
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

        void ApplyAutoCamProfileNow()
        {
            if (_pendingProfile == null) return;

            AutoCamProfile profile = _pendingProfile;
            _hasPendingProfile = false;
            _pendingProfile = null;

            TopClamp = profile.TopClamp;
            BottomClamp = profile.BottomClamp;

            AutoCamTopClamp = profile.AutoCamTopClamp;
            AutoCamBottomClamp = profile.AutoCamBottomClamp;

            AutoCam = profile.AutoCam;
            AirTiltSpeedUp = profile.AirTiltSpeedUp;
            AirTiltSpeedDown = profile.AirTiltSpeedDown;
            AutoCamTopSlowdownRange = profile.AutoCamTopSlowdownRange;
            AutoCamDownEaseInTime = profile.AutoCamDownEaseInTime;
            AutoCamMinAirTime = profile.AutoCamMinAirTime;
            AutoCamGroundDelay = profile.AutoCamGroundDelay;

            CameraAngleOverride = profile.CameraAngleOverride;

            if (_cinemachineTargetPitch > TopClamp)
            {
                _currentTopClamp = _cinemachineTargetPitch;
            }
            else
            {
                _currentTopClamp = TopClamp;
            }

            if (_cinemachineTargetPitch < BottomClamp)
            {
                _currentBottomClamp = _cinemachineTargetPitch;
            }
            else
            {
                _currentBottomClamp = BottomClamp;
            }
        }

        public void SetAutoCamProfile(AutoCamProfile profile)
        {
            if (profile == null) return;

            _pendingProfile = profile;
            _hasPendingProfile = true;

            if (Grounded)
            {
                ApplyAutoCamProfileNow();
            }
        }
    }
}
