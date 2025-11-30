using System.Collections;
using UnityEngine;

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
        [Tooltip("Active le comportement d'UI qui tourne autour du joueur en suivant la caméra.")]
        public bool enableBillboard = true;

        [Tooltip("Transform du joueur (centre de l'orbite). Si null, on prend transform.root.")]
        public Transform playerRoot;

        [Tooltip("Transform du Canvas World (celui qui doit tourner autour du joueur).")]
        public Transform billboardRoot;

        [Tooltip("Caméra à suivre. Si null, on utilise Camera.main.")]
        public Camera targetCamera;

        [Tooltip("Vitesse de rotation/orbite vers la cible.")]
        public float orbitSmoothSpeed = 12f;

        [Tooltip("Vitesse de rotation pour regarder la caméra.")]
        public float rotationSmoothSpeed = 12f;

        [Tooltip("Si true, ne tourne que sur l'axe Y pour regarder la caméra.")]
        public bool billboardOnlyYAxis = true;

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

        // Données pour l’orbite
        bool billboardInitialized = false;
        float billboardRadius = 0f;
        float billboardHeight = 0f;

        void Start()
        {
            if (pogoCheckmark != null)
                pogoCheckmark.SetActive(false);

            if (!cageBroken && !tutorialEnded)
                SetPanels(true, false);
            else
                SetPanels(false, false);

            // Initialisation de l’orbite
            if (enableBillboard)
                InitBillboardData();
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

            if (!isSlowmoActive && Mathf.Approximately(Time.timeScale, originalTimeScale))
                return;

            float targetScale = originalTimeScale <= 0f ? 1f : originalTimeScale;
            float baseFixed = originalFixedDeltaTime <= 0f ? 0.02f : originalFixedDeltaTime;

            Time.timeScale = targetScale;
            Time.fixedDeltaTime = baseFixed;
            isSlowmoActive = false;
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
            {
                pogoCheckmark.SetActive(true);
            }

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

        void LateUpdate()
        {
            if (!enableBillboard) return;
            if (billboardRoot == null) return;

            if (!billboardInitialized)
                InitBillboardData();

            if (!billboardInitialized)
                return;

            if (targetCamera == null)
                targetCamera = Camera.main;

            if (targetCamera == null || playerRoot == null)
                return;

            // 1) On calcule la droite de la caméra sur le plan horizontal
            Vector3 camRight = targetCamera.transform.right;
            camRight.y = 0f;
            float mag = camRight.magnitude;
            if (mag < 0.0001f)
                return;
            camRight /= mag;

            // 2) Position cible = joueur + droite caméra * rayon + hauteur
            Vector3 desiredPos = playerRoot.position
                               + camRight * billboardRadius
                               + Vector3.up * billboardHeight;

            // 3) Lerp vers la position
            billboardRoot.position = Vector3.Lerp(
                billboardRoot.position,
                desiredPos,
                Time.unscaledDeltaTime * orbitSmoothSpeed
            );

            // 4) Rotation pour regarder la caméra
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
    }
}
