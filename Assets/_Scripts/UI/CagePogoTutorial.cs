using System.Collections;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;

namespace StarterAssets
{
    public class CagePogoTutorial : MonoBehaviour
    {
        [Header("Time Settings")]
        public float slowTimeScale = 0.15f;
        public float slowInDuration = 0.25f;

        [Header("Apex Detection")]
        public float apexVerticalVelocityThreshold = 0.5f;

        [Header("Input Layout Source")]
        public PogoDashAbility pogoAbility;

        [Header("UI Panels")]
        public GameObject jumpPanel;
        public GameObject attackPanelA;
        public GameObject attackPanelX;

        [Header("Pogo Checkmark")]
        public GameObject pogoCheckmark;
        public float checkShowDuration = 1.0f;

        [Header("Billboard / Orbit")]
        public bool enableBillboard = true;
        public Transform playerRoot;
        public Transform billboardRoot;
        public Camera targetCamera;
        public float orbitSmoothSpeed = 12f;
        public float rotationSmoothSpeed = 12f;
        public bool billboardOnlyYAxis = true;

        [Header("Second Tutorial World Canvas")]
        public bool enableSecondTutorial = true;
        public Transform secondTutorialRoot;
        public CanvasGroup secondTutorialCanvasGroup;
        public Collider secondTutorialShowTrigger;
        public Collider secondTutorialHideTrigger;
        public float secondTutorialFadeDuration = 0.5f;

        [Header("FMOD Slowmo SFX")]
        public EventReference slowmoSfx;

        [Header("FMOD Slowmo Parameter")]
        public string slowmoCountParameterName = "Cage Slow mo count";

        int pogoCount = 0;
        bool waitingForApex = false;
        bool cageBroken = false;
        bool tutorialEnded = false;

        float originalTimeScale = 1f;
        float originalFixedDeltaTime = 0.02f;
        bool isSlowmoActive = false;
        Coroutine slowInRoutine;

        Coroutine checkRoutine;
        bool isShowingCheckOverlay = false;

        bool lastGroundedState = true;

        bool billboardInitialized = false;
        float billboardRadius = 0f;
        float billboardHeight = 0f;

        bool secondBillboardInitialized = false;
        float secondBillboardRadius = 0f;
        float secondBillboardHeight = 0f;

        Coroutine secondTutorialFadeRoutine;
        bool secondTutorialConsumed = false;

        EventInstance slowmoInstance;

        int slowmoTriggerCount = 0;

        void Start()
        {
            if (pogoCheckmark != null)
                pogoCheckmark.SetActive(false);

            if (!cageBroken && !tutorialEnded)
                SetPanels(true, false);
            else
                SetPanels(false, false);

            if (enableBillboard)
                InitBillboardData();

            if (secondTutorialRoot != null)
                secondTutorialRoot.gameObject.SetActive(false);

            if (secondTutorialCanvasGroup != null)
            {
                secondTutorialCanvasGroup.alpha = 0f;
                if (secondTutorialRoot != null)
                    secondTutorialRoot.gameObject.SetActive(false);
            }
        }

        void OnDestroy()
        {
            if (slowmoInstance.isValid())
            {
                slowmoInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                slowmoInstance.release();
                slowmoInstance = default;
            }
        }

        void InitBillboardData()
        {
            if (billboardRoot == null) return;

            if (playerRoot == null)
                playerRoot = transform.root;

            if (playerRoot == null) return;

            Vector3 worldOffset = billboardRoot.position - playerRoot.position;

            billboardHeight = worldOffset.y;

            Vector2 horiz = new Vector2(worldOffset.x, worldOffset.z);
            billboardRadius = horiz.magnitude;

            if (billboardRadius < 0.001f)
                billboardRadius = 0f;

            billboardInitialized = true;
        }

        void InitSecondBillboardData()
        {
            if (secondTutorialRoot == null) return;

            if (playerRoot == null)
                playerRoot = transform.root;

            if (playerRoot == null) return;

            Vector3 worldOffset = secondTutorialRoot.position - playerRoot.position;

            secondBillboardHeight = worldOffset.y;

            Vector2 horiz = new Vector2(worldOffset.x, worldOffset.z);
            secondBillboardRadius = horiz.magnitude;

            if (secondBillboardRadius < 0.001f)
                secondBillboardRadius = 0f;

            secondBillboardInitialized = true;
        }

        public void StartFirstJumpSequence()
        {
            if (cageBroken || tutorialEnded || pogoCount >= 2) return;
            waitingForApex = true;
        }

        public void UpdateMovementState(float verticalVelocity, bool grounded)
        {
            lastGroundedState = grounded;

            if (cageBroken || tutorialEnded)
            {
                if (!isShowingCheckOverlay)
                    SetPanels(false, false);
                return;
            }

            if (!isShowingCheckOverlay)
            {
                if (pogoCount < 2)
                {
                    if (grounded)
                        SetPanels(true, false);
                    else
                        SetPanels(false, true);
                }
                else
                {
                    SetPanels(false, false);
                }
            }

            if (pogoCount < 2 && waitingForApex)
            {
                if (!grounded &&
                    verticalVelocity > 0f &&
                    Mathf.Abs(verticalVelocity) <= apexVerticalVelocityThreshold)
                {
                    waitingForApex = false;
                    StartSlowmo();
                }
            }
        }

        public void OnPogoInput()
        {
            if (cageBroken) return;
            waitingForApex = false;
            RestoreTimeNow();
        }

        public void OnPogoPerformed()
        {
            if (cageBroken) return;

            pogoCount++;
            waitingForApex = false;

            RestoreTimeNow();

            if (pogoCount >= 2)
                tutorialEnded = true;

            if (pogoCount <= 2)
                StartCheckSequence();
        }

        public void OnLandedBackOnCage()
        {
            if (cageBroken) return;
            waitingForApex = false;
            RestoreTimeNow();
        }

        public void OnCageBroken()
        {
            if (cageBroken) return;
            cageBroken = true;
            tutorialEnded = true;
            waitingForApex = false;

            RestoreTimeNow();

            if (checkRoutine != null)
                StopCoroutine(checkRoutine);

            isShowingCheckOverlay = false;
            if (pogoCheckmark != null)
                pogoCheckmark.SetActive(false);

            SetPanels(false, false);
        }

        void StartSlowmo()
        {
            if (isSlowmoActive) return;

            originalTimeScale = Time.timeScale;
            originalFixedDeltaTime = Time.fixedDeltaTime;

            if (slowInRoutine != null)
                StopCoroutine(slowInRoutine);

            if (!slowmoSfx.IsNull)
            {
                if (slowmoInstance.isValid())
                {
                    slowmoInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                    slowmoInstance.release();
                    slowmoInstance = default;
                }

                slowmoInstance = RuntimeManager.CreateInstance(slowmoSfx);

                float paramValue = (slowmoTriggerCount == 0) ? 0f : 1f;
                slowmoTriggerCount++;

                if (!string.IsNullOrEmpty(slowmoCountParameterName))
                    slowmoInstance.setParameterByName(slowmoCountParameterName, paramValue);

                slowmoInstance.start();
            }

            slowInRoutine = StartCoroutine(SlowInRoutine());
        }

        IEnumerator SlowInRoutine()
        {
            float startScale = originalTimeScale;
            float targetScale = slowTimeScale <= 0f ? 0.01f : slowTimeScale;
            float baseFixed = originalFixedDeltaTime <= 0f ? 0.02f : originalFixedDeltaTime;

            if (slowInDuration <= 0f)
            {
                Time.timeScale = targetScale;
                Time.fixedDeltaTime = baseFixed * targetScale;
                isSlowmoActive = true;
                slowInRoutine = null;
                yield break;
            }

            float t = 0f;
            while (t < slowInDuration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / slowInDuration);

                float s = Mathf.Lerp(startScale, targetScale, k);
                Time.timeScale = s;
                Time.fixedDeltaTime = baseFixed * s;

                yield return null;
            }

            Time.timeScale = targetScale;
            Time.fixedDeltaTime = baseFixed * targetScale;
            isSlowmoActive = true;
            slowInRoutine = null;
        }

        void RestoreTimeNow()
        {
            if (slowInRoutine != null)
            {
                StopCoroutine(slowInRoutine);
                slowInRoutine = null;
            }

            float targetScale = originalTimeScale <= 0f ? 1f : originalTimeScale;
            float baseFixed = originalFixedDeltaTime <= 0f ? 0.02f : originalFixedDeltaTime;

            Time.timeScale = targetScale;
            Time.fixedDeltaTime = baseFixed;
            isSlowmoActive = false;

            if (slowmoInstance.isValid())
            {
                slowmoInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                slowmoInstance.release();
                slowmoInstance = default;
            }
        }

        void StartCheckSequence()
        {
            if (pogoCheckmark == null) return;

            if (checkRoutine != null)
                StopCoroutine(checkRoutine);

            checkRoutine = StartCoroutine(CheckmarkRoutine());
        }

        IEnumerator CheckmarkRoutine()
        {
            isShowingCheckOverlay = true;

            SetPanels(false, false);

            if (pogoCheckmark != null)
                pogoCheckmark.SetActive(true);

            float t = 0f;
            while (t < checkShowDuration)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            pogoCheckmark.SetActive(false);
            isShowingCheckOverlay = false;

            if (cageBroken || tutorialEnded || pogoCount >= 2)
            {
                SetPanels(false, false);
                checkRoutine = null;
                yield break;
            }

            if (lastGroundedState)
                SetPanels(true, false);
            else
                SetPanels(false, true);

            checkRoutine = null;
        }

        void SetPanels(bool showJump, bool showAttack)
        {
            if (jumpPanel != null) jumpPanel.SetActive(false);
            if (attackPanelA != null) attackPanelA.SetActive(false);
            if (attackPanelX != null) attackPanelX.SetActive(false);

            bool useX = false;
            if (pogoAbility != null)
                useX = !pogoAbility.AttackOnJump;

            if (showJump && jumpPanel != null)
                jumpPanel.SetActive(true);

            if (showAttack)
            {
                if (useX && attackPanelX != null)
                    attackPanelX.SetActive(true);
                else if (!useX && attackPanelA != null)
                    attackPanelA.SetActive(true);
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (!enableSecondTutorial || secondTutorialConsumed)
                return;

            if (secondTutorialShowTrigger != null && other == secondTutorialShowTrigger)
            {
                ShowSecondTutorial();
            }

            if (secondTutorialHideTrigger != null && other == secondTutorialHideTrigger)
            {
                HideSecondTutorial();
                secondTutorialConsumed = true;
            }
        }

        void ShowSecondTutorial()
        {
            if (!enableSecondTutorial || secondTutorialRoot == null || secondTutorialConsumed)
                return;

            if (secondTutorialFadeRoutine != null)
                StopCoroutine(secondTutorialFadeRoutine);

            secondTutorialFadeRoutine = StartCoroutine(FadeSecondTutorial(true));
        }

        void HideSecondTutorial()
        {
            if (!enableSecondTutorial || secondTutorialRoot == null)
                return;

            if (secondTutorialFadeRoutine != null)
                StopCoroutine(secondTutorialFadeRoutine);

            secondTutorialFadeRoutine = StartCoroutine(FadeSecondTutorial(false));
        }

        IEnumerator FadeSecondTutorial(bool fadeIn)
        {
            if (secondTutorialCanvasGroup == null)
            {
                secondTutorialRoot.gameObject.SetActive(fadeIn);
                secondTutorialFadeRoutine = null;
                yield break;
            }

            if (fadeIn)
                secondTutorialRoot.gameObject.SetActive(true);

            float start = secondTutorialCanvasGroup.alpha;
            float end = fadeIn ? 1f : 0f;
            float duration = secondTutorialFadeDuration <= 0f ? 0.01f : secondTutorialFadeDuration;

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);
                secondTutorialCanvasGroup.alpha = Mathf.Lerp(start, end, k);
                yield return null;
            }

            secondTutorialCanvasGroup.alpha = end;

            if (!fadeIn)
                secondTutorialRoot.gameObject.SetActive(false);

            secondTutorialFadeRoutine = null;
        }

        void LateUpdate()
        {
            if (enableBillboard && billboardRoot != null)
            {
                if (!billboardInitialized)
                    InitBillboardData();

                if (!billboardInitialized)
                    return;

                if (targetCamera == null)
                    targetCamera = Camera.main;

                if (targetCamera == null || playerRoot == null)
                    return;

                Vector3 camRight = targetCamera.transform.right;
                camRight.y = 0f;
                float mag = camRight.magnitude;
                if (mag < 0.0001f)
                    return;
                camRight /= mag;

                Vector3 desiredPos = playerRoot.position
                                   + camRight * billboardRadius
                                   + Vector3.up * billboardHeight;

                billboardRoot.position = Vector3.Lerp(
                    billboardRoot.position,
                    desiredPos,
                    Time.unscaledDeltaTime * orbitSmoothSpeed
                );

                Vector3 dir = billboardRoot.position - targetCamera.transform.position;

                if (billboardOnlyYAxis)
                    dir.y = 0f;

                if (dir.sqrMagnitude < 0.0001f)
                    return;

                Quaternion targetRot = Quaternion.LookRotation(dir);
                billboardRoot.rotation = Quaternion.Slerp(
                    billboardRoot.rotation,
                    targetRot,
                    Time.unscaledDeltaTime * rotationSmoothSpeed
                );
            }

            if (enableSecondTutorial && !secondTutorialConsumed && secondTutorialRoot != null && secondTutorialRoot.gameObject.activeInHierarchy)
            {
                if (!secondBillboardInitialized)
                    InitSecondBillboardData();

                if (!secondBillboardInitialized)
                    return;

                if (targetCamera == null)
                    targetCamera = Camera.main;

                if (targetCamera == null || playerRoot == null)
                    return;

                Vector3 camRight2 = targetCamera.transform.right;
                camRight2.y = 0f;
                float mag2 = camRight2.magnitude;
                if (mag2 < 0.0001f)
                    return;
                camRight2 /= mag2;

                Vector3 desiredPos2 = playerRoot.position
                                    + camRight2 * secondBillboardRadius
                                    + Vector3.up * secondBillboardHeight;

                secondTutorialRoot.position = Vector3.Lerp(
                    secondTutorialRoot.position,
                    desiredPos2,
                    Time.unscaledDeltaTime * orbitSmoothSpeed
                );

                Vector3 dir2 = secondTutorialRoot.position - targetCamera.transform.position;

                if (billboardOnlyYAxis)
                    dir2.y = 0f;

                if (dir2.sqrMagnitude < 0.0001f)
                    return;

                Quaternion rot2 = Quaternion.LookRotation(dir2);
                secondTutorialRoot.rotation = Quaternion.Slerp(
                    secondTutorialRoot.rotation,
                    rot2,
                    Time.unscaledDeltaTime * rotationSmoothSpeed
                );
            }
        }
    }
}
