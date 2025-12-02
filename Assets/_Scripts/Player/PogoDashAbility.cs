using UnityEngine;
using UnityEngine.InputSystem;
using Unity.VisualScripting;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;
using UnityEngine.Serialization;

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(StarterAssetsInputs))]
    [RequireComponent(typeof(ThirdPersonController))]
    public class PogoDashAbility : MonoBehaviour
    {
        [Header("Dash")]
        public float dashDuration = 0.18f;
        public float dashCooldown = 0.6f;
        public float dashSpeed = 16f;
        public string dashAnimState = "Roll";
        public float airDashLiftPerSecond = 4f;

        [Header("Pogo")]
        public float pogoDownSpeed = 18f;
        public float pogoCheckRadius = 0.6f;
        public float pogoCheckAhead = 1.0f;
        public float pogoBounceHeight = 4f;
        public float pogoBounceMushroomHeight = 8f;

        [Header("Cage Pogo Overrides")]
        public float pogoBounceCageHeight = 4f;
        public float pogoDownSpeedCage = 18f;
        public float pogoCheckRadiusCage = 0.6f;
        public float pogoCheckAheadCage = 1.0f;

        public float pogoCooldown = 0.30f;
        public string pogoHitAnimState = "PogoHit";
        public string pogoLandAnimState = "PogoLand";

        [Header("Pogo Timing")]
        [HideInInspector] public float minAirTimeBeforePogo = 0.05f;

        [Header("Hit Detection")]
        public LayerMask enemyLayers;
        [FormerlySerializedAs("nonPushableEnemyLayers")]
        public LayerMask mushroomLayers;
        public LayerMask cageLayers;

        [Header("Enemy Knockback")]
        public float enemyKnockbackDistance = 2.5f;
        public float enemyKnockbackDuration = 0.12f;
        public float enemyKnockbackUpClamp = 0.25f;

        [Header("Environment")]
        [HideInInspector] public LayerMask environmentLayers;
        [HideInInspector] public float knockbackSkin = 0.08f;

        [Header("Screen Shake")]
        public bool enableScreenShake = true;
        public float shakeDuration = 0.08f;
        public float shakeAmplitude = 0.12f;
        public float shakeFrequency = 28f;

        [Header("Cage")]
        public int cagePogosToBreak = 3;
        public float cageShakeDuration = 0.12f;
        public float cageShakeAmplitude = 0.08f;
        public bool cageDisableInsteadOfDestroy = true;

        [Header("Gamepad Rumble")]
        public bool enableRumble = true;
        public float pogoRumbleLow = 0.4f;
        public float pogoRumbleHigh = 0.8f;
        public float pogoRumbleDuration = 0.12f;

        [Header("Dash / Pogo Trail")]
        [SerializeField] private GameObject trailRoot;

        [Header("Pogo VFX")]
        public GameObject pogoHitVfxPrefab;
        public GameObject cageBreakVfxPrefab;
        public float cageBreakVfxYOffset = 0f;

        [Header("Cage Pogo Tutorial")]
        public CagePogoTutorial cagePogoTutorial;

        public bool IsDashing { get; private set; }

        CharacterController cc;
        StarterAssetsInputs inputs;
        ThirdPersonController ctrl;
        Transform cam;
        Animator anim;

        bool isPogoDown;
        float dashTime;
        float dashCooldownTimer;
        float pogoCooldownTimer;

        Vector3 dashDir;
        float speedThisDash;
        int dashHash, pogoHitHash, pogoLandHash;

        bool airDashAvailable = true;
        bool wasGrounded;

        Transform camTarget;
        Vector3 camTargetBaseLocalPos;
        Coroutine shakeCo;
        Coroutine rumbleCo;

        [SerializeField] private bool pogoOnXLayout = false;
        bool kbPogoHeldLast;

        float airTime;
        bool pogoSystemArmed;

        const int FIXED_POGO_DAMAGE = 1;

        Dictionary<Transform, int> cagePogoCounts = new Dictionary<Transform, int>();

        bool dashStartedInAir;

        TrailRenderer[] dashTrails;

        float currentPogoDownSpeed;
        float currentPogoCheckRadius;
        float currentPogoCheckAhead;

        public bool AttackOnJump
        {
            get => !pogoOnXLayout;
            set => pogoOnXLayout = !value;
        }

        public bool UseXLayout => pogoOnXLayout;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            inputs = GetComponent<StarterAssetsInputs>();
            ctrl = GetComponent<ThirdPersonController>();
            cam = Camera.main ? Camera.main.transform : null;
            anim = GetComponent<Animator>();

            dashHash = string.IsNullOrEmpty(dashAnimState) ? 0 : Animator.StringToHash(dashAnimState);
            pogoHitHash = string.IsNullOrEmpty(pogoHitAnimState) ? 0 : Animator.StringToHash(pogoHitAnimState);
            pogoLandHash = string.IsNullOrEmpty(pogoLandAnimState) ? 0 : Animator.StringToHash(pogoLandAnimState);

            camTarget = ctrl != null && ctrl.CinemachineCameraTarget != null ? ctrl.CinemachineCameraTarget.transform : null;
            if (camTarget != null) camTargetBaseLocalPos = camTarget.localPosition;

            AttackOnJump = true;

            currentPogoDownSpeed = pogoDownSpeed;
            currentPogoCheckRadius = pogoCheckRadius;
            currentPogoCheckAhead = pogoCheckAhead;

            if (trailRoot != null)
            {
                dashTrails = trailRoot.GetComponentsInChildren<TrailRenderer>(true);
                SetTrailEmission(false);
            }
        }

        void OnEnable()
        {
            IsDashing = false;
            isPogoDown = false;
            dashTime = 0f;
            dashCooldownTimer = 0f;
            pogoCooldownTimer = 0f;
            airDashAvailable = true;
            wasGrounded = false;
            kbPogoHeldLast = false;
            airTime = 0f;
            pogoSystemArmed = false;
            dashStartedInAir = false;
            if (ctrl != null) ctrl.LockGravityExternally = false;

            currentPogoDownSpeed = pogoDownSpeed;
            currentPogoCheckRadius = pogoCheckRadius;
            currentPogoCheckAhead = pogoCheckAhead;

            SetTrailEmission(false);
        }

        void OnDisable()
        {
            if (camTarget != null) camTarget.localPosition = camTargetBaseLocalPos;
            if (shakeCo != null) StopCoroutine(shakeCo);
            shakeCo = null;

#if ENABLE_INPUT_SYSTEM
            if (rumbleCo != null) StopCoroutine(rumbleCo);
            rumbleCo = null;
            var gamepad = Gamepad.current;
            if (gamepad != null) gamepad.SetMotorSpeeds(0f, 0f);
#endif

            if (ctrl != null) ctrl.LockGravityExternally = false;

            SetTrailEmission(false);
        }

        void Update()
        {
            if (dashCooldownTimer > 0f) dashCooldownTimer -= Time.deltaTime;
            if (pogoCooldownTimer > 0f) pogoCooldownTimer -= Time.deltaTime;

            if (ctrl.Grounded && !wasGrounded)
            {
                airDashAvailable = true;
                pogoCooldownTimer = 0f;
                SetTrailEmission(false);
            }

            if (ctrl.Grounded)
            {
                airTime = 0f;
                pogoSystemArmed = true;
            }
            else
            {
                airTime += Time.deltaTime;
            }

            wasGrounded = ctrl.Grounded;

            if ((IsDashing || dashCooldownTimer > 0f) && inputs.dash)
                inputs.dash = false;

            if (!IsDashing && dashCooldownTimer <= 0f && DashPressed())
            {
                if (!ctrl.IsMovementLockedByCage && (ctrl.Grounded || airDashAvailable))
                {
                    dashDir = ComputeDir();
                    StartDashForward();
                    if (!ctrl.Grounded) airDashAvailable = false;
                }
            }

            if (!ctrl.Grounded && PogoPressed())
            {
                if (IsDashing && !isPogoDown)
                    StopDashInternal(true);

                StartPogoDown();
            }

            if (IsDashing)
            {
                Vector3 preCenter = cc.bounds.center;

                Vector3 step = dashDir * speedThisDash * Time.deltaTime;

                if (dashStartedInAir && !ctrl.Grounded && !isPogoDown && airDashLiftPerSecond != 0f)
                    step += Vector3.up * airDashLiftPerSecond * Time.deltaTime;

                cc.Move(step);

                Vector3 postCenter = cc.bounds.center;
                float stepDist = (postCenter - preCenter).magnitude;
                if (stepDist < 0.0001f) stepDist = step.magnitude;

                if (!isPogoDown)
                {
                    dashTime -= Time.deltaTime;
                    if (dashTime <= 0f)
                        StopDashInternal(false);
                }
                else
                {
                    float castDownDist = Mathf.Max(currentPogoCheckAhead, stepDist);

                    if (TryGetPogoHitSegment(preCenter, postCenter, out Collider enemyCol2, enemyLayers, QueryTriggerInteraction.Collide) ||
                        TryGetPogoHitDown(postCenter, castDownDist, out enemyCol2, enemyLayers, QueryTriggerInteraction.Collide))
                    {
                        ApplyDamage(enemyCol2, FIXED_POGO_DAMAGE);
                        ApplyEnemyKnockbackConstrained(enemyCol2);
                        if (anim != null && pogoHitHash != 0) anim.CrossFadeInFixedTime(pogoHitAnimState, 0.05f, 0, 0f);
                        TriggerScreenShake();
                        TriggerPogoRumble();
                        SpawnPogoVfx(enemyCol2);
                        StopDashInternal(false);
                        ctrl.Bounce(pogoBounceHeight);
                        airDashAvailable = true;

                        if (cagePogoTutorial != null)
                            cagePogoTutorial.OnPogoPerformed();
                    }
                    else if (TryGetPogoHitSegment(preCenter, postCenter, out Collider npCol2, mushroomLayers, QueryTriggerInteraction.Ignore) ||
                             TryGetPogoHitDown(postCenter, castDownDist, out npCol2, mushroomLayers, QueryTriggerInteraction.Ignore))
                    {
                        ApplyDamage(npCol2, FIXED_POGO_DAMAGE);
                        CustomEvent.Trigger(npCol2.gameObject, "OnHitReceived");
                        if (anim != null && pogoHitHash != 0) anim.CrossFadeInFixedTime(pogoHitAnimState, 0.05f, 0, 0f);
                        TriggerScreenShake();
                        TriggerPogoRumble();
                        StopDashInternal(false);
                        ctrl.Bounce(pogoBounceMushroomHeight);
                        airDashAvailable = true;

                        if (cagePogoTutorial != null)
                            cagePogoTutorial.OnPogoPerformed();
                    }
                    else if (TryGetPogoHitSegment(preCenter, postCenter, out Collider cageCol2, cageLayers, QueryTriggerInteraction.Collide) ||
                             TryGetPogoHitDown(postCenter, castDownDist, out cageCol2, cageLayers, QueryTriggerInteraction.Collide))
                    {
                        Transform cageTr = cageCol2.attachedRigidbody ? cageCol2.attachedRigidbody.transform : cageCol2.transform;
                        int count = 0;
                        cagePogoCounts.TryGetValue(cageTr, out count);
                        count++;
                        cagePogoCounts[cageTr] = count;

                        StartCoroutine(ShakeCageRoutine(cageTr, cageShakeDuration, cageShakeAmplitude));
                        TriggerScreenShake();
                        TriggerPogoRumble();
                        SpawnPogoVfx(cageCol2);
                        ctrl.Bounce(pogoBounceCageHeight);
                        airDashAvailable = true;
                        StopDashInternal(false);

                        if (cagePogoTutorial != null)
                            cagePogoTutorial.OnPogoPerformed();

                        if (count >= cagePogosToBreak)
                        {
                            SpawnCageBreakVfx();
                            cagePogoCounts.Remove(cageTr);
                            if (cageDisableInsteadOfDestroy)
                                cageTr.gameObject.SetActive(false);
                            else
                                Destroy(cageTr.gameObject);

                            if (cagePogoTutorial != null)
                                cagePogoTutorial.OnCageBroken();
                        }

                        if (anim != null && pogoHitHash != 0) anim.CrossFadeInFixedTime(pogoHitAnimState, 0.05f, 0, 0f);
                    }
                    else if (ctrl.Grounded || (cc.collisionFlags & CollisionFlags.Below) != 0 || CheckHitSurface(stepDist))
                    {
                        if (anim != null && pogoLandHash != 0)
                            anim.CrossFadeInFixedTime(pogoLandAnimState, 0.05f, 0, 0f);

                        StopDashInternal(false);
                    }
                }
            }
        }

        void StartDashForward()
        {
            IsDashing = true;
            isPogoDown = false;

            dashTime = dashDuration;
            dashCooldownTimer = dashCooldown;
            speedThisDash = dashSpeed;

            dashStartedInAir = !ctrl.Grounded;

            if (!ctrl.Grounded)
            {
                ctrl.SetVerticalVelocity(0f);
                ctrl.LockGravityExternally = true;
                StartTrail();
            }

            if (anim != null && dashHash != 0)
                anim.CrossFadeInFixedTime(dashHash, 0.05f, 0, 0f);
        }

        void StartPogoDown()
        {
            if (ctrl != null && ctrl.IsMovementLockedByCage)
            {
                currentPogoDownSpeed = pogoDownSpeedCage;
                currentPogoCheckRadius = pogoCheckRadiusCage;
                currentPogoCheckAhead = pogoCheckAheadCage;
            }
            else
            {
                currentPogoDownSpeed = pogoDownSpeed;
                currentPogoCheckRadius = pogoCheckRadius;
                currentPogoCheckAhead = pogoCheckAhead;
            }

            IsDashing = true;
            isPogoDown = true;
            dashDir = Vector3.down;
            speedThisDash = currentPogoDownSpeed;
            pogoCooldownTimer = Mathf.Max(pogoCooldownTimer, pogoCooldown);
            ctrl.SetVerticalVelocity(0f);
            ctrl.LockGravityExternally = false;

            if (cagePogoTutorial != null)
                cagePogoTutorial.OnPogoInput();

            StartTrail();
        }

        void StopDashInternal(bool noAnimReset)
        {
            IsDashing = false;
            isPogoDown = false;
            dashTime = 0f;
            dashStartedInAir = false;
            ctrl.LockGravityExternally = false;
        }

        bool DashPressed()
        {
            if (!inputs.dash) return false;
            inputs.dash = false;
            return true;
        }

        bool PogoPressed()
        {
            if (!pogoSystemArmed) return false;
            if (ctrl.Grounded) return false;
            if (pogoCooldownTimer > 0f) return false;

#if ENABLE_INPUT_SYSTEM
            if (Gamepad.current != null)
            {
                bool pressed = false;

                if (!pogoOnXLayout)
                {
                    if (airTime >= minAirTimeBeforePogo)
                        pressed = Gamepad.current.buttonSouth.wasPressedThisFrame;
                }
                else
                {
                    pressed = Gamepad.current.buttonWest.wasPressedThisFrame;
                }

                return pressed;
            }
#endif

            bool raw = inputs.pogo;
            bool front = raw && !kbPogoHeldLast;
            kbPogoHeldLast = raw;

            if (front)
            {
                inputs.pogo = false;
                return true;
            }

            return false;
        }

        Vector3 ComputeDir()
        {
            Vector2 m = inputs.move;
            if (m.sqrMagnitude > 0.01f && cam != null)
            {
                Vector3 f = cam.forward;
                f.y = 0f;
                f.Normalize();

                Vector3 r = cam.right;
                r.y = 0f;
                r.Normalize();

                return (f * m.y + r * m.x).normalized;
            }

            Vector3 dir = transform.forward;
            dir.y = 0f;
            return dir.normalized;
        }

        bool TryGetPogoHitSegment(Vector3 start, Vector3 end, out Collider col, LayerMask mask, QueryTriggerInteraction triggerMode)
        {
            Vector3 dir = end - start;
            float dist = dir.magnitude;
            if (dist < 0.0001f)
            {
                col = null;
                return false;
            }

            dir /= dist;

            if (Physics.SphereCast(start, currentPogoCheckRadius, dir, out RaycastHit hit, dist, mask, triggerMode))
            {
                col = hit.collider;
                return true;
            }

            col = null;
            return false;
        }

        bool TryGetPogoHitDown(Vector3 origin, float distance, out Collider enemyCol, LayerMask mask, QueryTriggerInteraction triggerMode)
        {
            float ahead = distance;
            float radius = currentPogoCheckRadius;

            if (Physics.SphereCast(origin, radius, Vector3.down, out RaycastHit hit, ahead, mask, triggerMode))
            {
                enemyCol = hit.collider;
                return true;
            }

            Vector3 end = origin + Vector3.down * ahead;
            var cols = Physics.OverlapSphere(end, radius, mask, triggerMode);
            if (cols != null && cols.Length > 0)
            {
                enemyCol = cols[0];
                return true;
            }

            enemyCol = null;
            return false;
        }

        bool CheckHitSurface(float stepDist)
        {
            Vector3 origin = cc.bounds.center;
            float ahead = Mathf.Max(0.2f, stepDist + 0.1f);
            int mask = ~LayerMask.GetMask(LayerMask.LayerToName(gameObject.layer));
            return Physics.SphereCast(origin, currentPogoCheckRadius, Vector3.down, out _, ahead, mask, QueryTriggerInteraction.Ignore);
        }

        void ApplyDamage(Collider enemyCol, int damage)
        {
            var vs = Variables.Object(enemyCol.gameObject);
            int hp = 0;

            if (vs.IsDefined("PV_ennemy"))
            {
                try { hp = vs.Get<int>("PV_ennemy"); } catch { }
            }

            hp = Mathf.Max(0, hp - damage);

            try { vs.Set("PV_ennemy", hp); } catch { }

            CustomEvent.Trigger(enemyCol.gameObject, "OnHitReceived");
        }

        void ApplyEnemyKnockbackConstrained(Collider enemyCol)
        {
            Transform tr = enemyCol.attachedRigidbody ? enemyCol.attachedRigidbody.transform : enemyCol.transform;
            if (!tr) return;

            Vector3 back = -tr.forward;
            back.y = 0f;

            if (back.sqrMagnitude < 0.0001f)
            {
                back = tr.position - transform.position;
                back.y = 0f;
            }

            back = back.sqrMagnitude > 0.0001f ? back.normalized : Vector3.back;

            Vector3 start = tr.position;
            Vector3 desired = start + back * Mathf.Max(0f, enemyKnockbackDistance);
            desired.y = start.y;

            NavMeshAgent agent = tr.GetComponent<NavMeshAgent>();
            Bounds b = GetColliderBounds(enemyCol);
            float radius = Mathf.Max(0.1f, Mathf.Max(b.extents.x, b.extents.z));
            Vector3 target = ConstrainToNavmeshAndEnvironment(start, desired, back, radius, agent);

            StartCoroutine(KnockbackEnemyRoutineConstrained(tr, agent, start, target, enemyKnockbackDuration));
        }

        Bounds GetColliderBounds(Collider c)
        {
            if (c is CharacterController ch) return new Bounds(ch.bounds.center, ch.bounds.size);
            return c.bounds;
        }

        Vector3 ConstrainToNavmeshAndEnvironment(Vector3 start, Vector3 desired, Vector3 dirBack, float radius, NavMeshAgent agent)
        {
            Vector3 a = start;
            Vector3 b = desired;

            if (NavMesh.SamplePosition(a, out NavMeshHit sHit, 1f, agent ? agent.areaMask : NavMesh.AllAreas))
                a = sHit.position;

            if (NavMesh.SamplePosition(b, out NavMeshHit dHit, 1f, agent ? agent.areaMask : NavMesh.AllAreas))
                b = dHit.position;
            else
                b = a + (b - a).normalized * 0.1f;

            if (NavMesh.Raycast(a, b, out NavMeshHit wallHit, agent ? agent.areaMask : NavMesh.AllAreas))
            {
                b = wallHit.position - dirBack * Mathf.Max(knockbackSkin, 0.02f);
                b.y = a.y;
            }

            Vector3 castDir = (b - a);
            float dist = castDir.magnitude;

            if (dist > 0.0001f)
            {
                castDir /= dist;

                if (Physics.SphereCast(a + Vector3.up * 0.1f, radius, castDir, out RaycastHit phyHit, dist, environmentLayers, QueryTriggerInteraction.Ignore))
                {
                    b = phyHit.point - castDir * Mathf.Max(knockbackSkin, 0.02f);
                    b.y = a.y;
                }
            }

            if (NavMesh.SamplePosition(b, out NavMeshHit finalHit, 1f, agent ? agent.areaMask : NavMesh.AllAreas))
                b = finalHit.position;

            b.y = a.y;
            return b;
        }

        IEnumerator KnockbackEnemyRoutineConstrained(Transform tr, NavMeshAgent agent, Vector3 start, Vector3 target, float duration)
        {
            if (!tr) yield break;

            bool hadAgent = agent && agent.enabled;
            ObstacleAvoidanceType savedAvoid = ObstacleAvoidanceType.NoObstacleAvoidance;
            bool savedUpdatePos = false;
            bool savedUpdateRot = false;

            if (hadAgent)
            {
                if (agent.isOnNavMesh)
                {
                    agent.ResetPath();
                    agent.isStopped = true;
                }

                savedAvoid = agent.obstacleAvoidanceType;
                savedUpdatePos = agent.updatePosition;
                savedUpdateRot = agent.updateRotation;

                agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
                agent.updateRotation = false;
                agent.updatePosition = true;
            }

            float t = 0f;
            while (t < duration && tr)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / Mathf.Max(0.0001f, duration));
                Vector3 pos = Vector3.Lerp(start, target, k);

                if (hadAgent && agent && agent.isOnNavMesh)
                    agent.Warp(pos);
                else
                    tr.position = pos;

                t += Time.deltaTime;
                yield return null;
            }

            if (tr)
            {
                if (hadAgent && agent && agent.isOnNavMesh)
                    agent.Warp(target);
                else
                    tr.position = target;
            }

            if (hadAgent && agent)
            {
                agent.obstacleAvoidanceType = savedAvoid;
                agent.updateRotation = savedUpdateRot;
                agent.updatePosition = savedUpdatePos;
                if (agent.isOnNavMesh) agent.isStopped = false;
            }
        }

        void TriggerScreenShake()
        {
            if (!enableScreenShake || camTarget == null) return;
            if (shakeCo != null) StopCoroutine(shakeCo);
            shakeCo = StartCoroutine(ShakeRoutine());
        }

        void TriggerPogoRumble()
        {
#if ENABLE_INPUT_SYSTEM
            if (!enableRumble) return;
            var gamepad = Gamepad.current;
            if (gamepad == null) return;
            if (rumbleCo != null) StopCoroutine(rumbleCo);
            rumbleCo = StartCoroutine(RumbleRoutine(pogoRumbleLow, pogoRumbleHigh, pogoRumbleDuration));
#endif
        }

        IEnumerator ShakeRoutine()
        {
            float t = 0f;
            Vector3 basePos = camTargetBaseLocalPos;

            while (t < shakeDuration)
            {
                float decay = 1f - (t / Mathf.Max(0.0001f, shakeDuration));
                float amp = shakeAmplitude * decay;

                Vector3 jitter = new Vector3(
                    Random.value * 2f - 1f,
                    (Random.value * 2f - 1f) * 0.5f,
                    Random.value * 2f - 1f
                );

                camTarget.localPosition = basePos + jitter * amp;
                t += Time.deltaTime;
                yield return null;
            }

            camTarget.localPosition = basePos;
            shakeCo = null;
        }

        IEnumerator RumbleRoutine(float low, float high, float duration)
        {
#if ENABLE_INPUT_SYSTEM
            var gamepad = Gamepad.current;
            if (gamepad == null) yield break;

            gamepad.SetMotorSpeeds(low, high);
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                yield return null;
            }
            gamepad.SetMotorSpeeds(0f, 0f);
            rumbleCo = null;
#else
            yield break;
#endif
        }

        IEnumerator ShakeCageRoutine(Transform tr, float duration, float amplitude)
        {
            float t = 0f;
            Vector3 baseLocalPos = tr.localPosition;

            while (t < duration)
            {
                float decay = 1f - (t / Mathf.Max(0.0001f, duration));
                float amp = amplitude * decay;

                Vector3 jitter = new Vector3(
                    Random.value * 2f - 1f,
                    Random.value * 2f - 1f,
                    Random.value * 2f - 1f
                );

                tr.localPosition = baseLocalPos + jitter * amp;
                t += Time.deltaTime;
                yield return null;
            }

            tr.localPosition = baseLocalPos;
        }

        public void ForceStopScreenShake()
        {
            if (camTarget != null)
                camTarget.localPosition = camTargetBaseLocalPos;

            if (shakeCo != null)
            {
                StopCoroutine(shakeCo);
                shakeCo = null;
            }

#if ENABLE_INPUT_SYSTEM
            if (rumbleCo != null)
            {
                StopCoroutine(rumbleCo);
                rumbleCo = null;
            }
            var gamepad = Gamepad.current;
            if (gamepad != null) gamepad.SetMotorSpeeds(0f, 0f);
#endif
        }

        void SetTrailEmission(bool emitting)
        {
            if (dashTrails == null) return;
            for (int i = 0; i < dashTrails.Length; i++)
            {
                if (dashTrails[i] != null)
                    dashTrails[i].emitting = emitting;
            }
        }

        void StartTrail()
        {
            SetTrailEmission(true);
        }

        void SpawnPogoVfx(Collider col)
        {
            if (pogoHitVfxPrefab == null || col == null) return;
            Vector3 origin = cc != null ? cc.bounds.center : transform.position;
            Vector3 hitPos = col.ClosestPoint(origin);
            Instantiate(pogoHitVfxPrefab, hitPos, Quaternion.identity);
        }

        void SpawnCageBreakVfx()
        {
            if (cageBreakVfxPrefab == null) return;
            Vector3 pos = transform.position + Vector3.up * cageBreakVfxYOffset;
            Instantiate(cageBreakVfxPrefab, pos, Quaternion.identity);
        }
    }
}
