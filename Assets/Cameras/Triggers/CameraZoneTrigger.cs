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
                }
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.transform.root.CompareTag("Player")) return;

            if (lastActivatedZone == this)
            {
                return;
            }

            Vector3? fromOffset = null;

            if (lastActivatedZone != null && lastActivatedZone.targetCamera != null)
            {
                var lastFollow = lastActivatedZone.targetCamera.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
                if (lastFollow != null)
                {
                    fromOffset = lastFollow.ShoulderOffset;
                }

                lastActivatedZone.targetCamera.Priority = lastActivatedZone.inactivePriority;
            }

            lastActivatedZone = this;

            if (targetCamera != null)
            {
                if (_follow != null)
                {
                    if (fromOffset.HasValue)
                    {
                        _follow.ShoulderOffset = fromOffset.Value;

                        if (_shoulderBlendRoutine != null)
                            StopCoroutine(_shoulderBlendRoutine);

                        if (shoulderBlendDuration > 0f)
                        {
                            _shoulderBlendRoutine = StartCoroutine(BlendShoulderOffset(_follow, _defaultShoulderOffset, shoulderBlendDuration));
                        }
                        else
                        {
                            _follow.ShoulderOffset = _defaultShoulderOffset;
                        }
                    }
                    else
                    {
                        _follow.ShoulderOffset = _defaultShoulderOffset;
                    }
                }

                targetCamera.Priority = activePriority;
            }

            ThirdPersonController controller = other.transform.root.GetComponent<ThirdPersonController>();
            if (controller != null)
            {
                controller.SetAutoCamProfile(autoCamProfile);
            }
        }

        IEnumerator BlendShoulderOffset(Cinemachine3rdPersonFollow follow, Vector3 targetOffset, float duration)
        {
            if (follow == null)
            {
                yield break;
            }

            Vector3 startOffset = follow.ShoulderOffset;

            if (duration <= 0f || startOffset == targetOffset)
            {
                follow.ShoulderOffset = targetOffset;
                yield break;
            }

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                Vector3 lerped = Vector3.Lerp(startOffset, targetOffset, t);
                follow.ShoulderOffset = lerped;
                yield return null;
            }

            follow.ShoulderOffset = targetOffset;
        }
    }
}
