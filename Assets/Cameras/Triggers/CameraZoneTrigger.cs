using System.Collections;
using UnityEngine;
using Cinemachine;

namespace StarterAssets
{
    public class CameraZoneTrigger : MonoBehaviour
    {
        public CinemachineVirtualCamera targetCamera;
        public AutoCamProfile autoCamProfile;
        public int activePriority = 20;
        public int inactivePriority = 0;

        public float shoulderBlendDuration = 0.4f;

        static CameraZoneTrigger lastActivatedZone;

        Cinemachine3rdPersonFollow _follow;
        Vector3 _defaultShoulderOffset;
        Coroutine _shoulderBlendRoutine;

        void Awake()
        {
            if (targetCamera != null)
            {
                _follow = targetCamera.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
                if (_follow != null)
                {
                    _defaultShoulderOffset = _follow.ShoulderOffset;
                    Debug.Log("[CAM-ZONE] Awake – default offset = " + _defaultShoulderOffset);
                }
                else
                {
                    Debug.LogWarning("[CAM-ZONE] Awake – Pas de 3rdPersonFollow sur " + targetCamera.name);
                }
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.transform.root.CompareTag("Player")) return;

            Debug.Log("\n========== TRIGGER ENTER : " + gameObject.name + " ==========");

            if (lastActivatedZone == this)
            {
                Debug.Log("[CAM-ZONE] déjà zone active → rien faire");
                return;
            }

            Vector3? fromOffset = null;

            if (lastActivatedZone != null && lastActivatedZone.targetCamera != null)
            {
                var lastFollow = lastActivatedZone.targetCamera.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
                if (lastFollow != null)
                {
                    fromOffset = lastFollow.ShoulderOffset;
                    Debug.Log("[CAM-ZONE] ancien offset = " + fromOffset.Value);
                }
                else
                {
                    Debug.LogWarning("[CAM-ZONE] ancien vcam n’a pas de 3rdPersonFollow");
                }

                lastActivatedZone.targetCamera.Priority = lastActivatedZone.inactivePriority;
                Debug.Log("[CAM-ZONE] old vcam priority → " + lastActivatedZone.inactivePriority);
            }

            lastActivatedZone = this;

            if (targetCamera != null)
            {
                Debug.Log("[CAM-ZONE] new vcam = " + targetCamera.name);

                if (_follow != null)
                {
                    if (fromOffset.HasValue)
                    {
                        Debug.Log("[CAM-ZONE] apply TEMP offset (start) = " + fromOffset.Value);
                        _follow.ShoulderOffset = fromOffset.Value;

                        if (_shoulderBlendRoutine != null)
                            StopCoroutine(_shoulderBlendRoutine);

                        if (shoulderBlendDuration > 0f)
                        {
                            Debug.Log("[CAM-ZONE] start blend → target = " + _defaultShoulderOffset + " | duration=" + shoulderBlendDuration);
                            _shoulderBlendRoutine = StartCoroutine(BlendShoulderOffset(_follow, _defaultShoulderOffset, shoulderBlendDuration));
                        }
                        else
                        {
                            Debug.Log("[CAM-ZONE] duration=0 → snap to target offset");
                            _follow.ShoulderOffset = _defaultShoulderOffset;
                        }
                    }
                    else
                    {
                        Debug.Log("[CAM-ZONE] pas d’ancien offset, on set direct = " + _defaultShoulderOffset);
                        _follow.ShoulderOffset = _defaultShoulderOffset;
                    }
                }

                targetCamera.Priority = activePriority;
                Debug.Log("[CAM-ZONE] new vcam priority → " + activePriority);
            }

            ThirdPersonController controller = other.transform.root.GetComponent<ThirdPersonController>();
            if (controller != null)
            {
                Debug.Log("[CAM-ZONE] Apply auto cam profile to controller");
                controller.SetAutoCamProfile(autoCamProfile);
            }
        }

        IEnumerator BlendShoulderOffset(Cinemachine3rdPersonFollow follow, Vector3 targetOffset, float duration)
        {
            if (follow == null)
            {
                Debug.LogError("[CAM-ZONE] follow = null → abort blend");
                yield break;
            }

            Vector3 startOffset = follow.ShoulderOffset;

            Debug.Log("[CAM-ZONE] Coroutine start — from: " + startOffset + "  to: " + targetOffset);

            if (duration <= 0f || startOffset == targetOffset)
            {
                Debug.Log("[CAM-ZONE] duration=0 ou égal → snap final offset");
                follow.ShoulderOffset = targetOffset;
                yield break;
            }

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                Vector3 lerped = Vector3.Lerp(startOffset, targetOffset, t);
                follow.ShoulderOffset = lerped;

                Debug.Log("[CAM-ZONE] LERP t=" + t.ToString("0.00") + " → offset=" + lerped);

                yield return null;
            }

            Debug.Log("[CAM-ZONE] Coroutine end — final offset=" + targetOffset);
            follow.ShoulderOffset = targetOffset;
        }
    }
}
