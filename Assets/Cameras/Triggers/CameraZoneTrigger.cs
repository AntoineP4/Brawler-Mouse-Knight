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

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            if (targetCamera != null)
                targetCamera.Priority = activePriority;

            ThirdPersonController controller = other.GetComponent<ThirdPersonController>();
            if (controller != null)
                controller.SetAutoCamProfile(autoCamProfile);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            if (targetCamera != null)
                targetCamera.Priority = inactivePriority;
        }
    }
}
