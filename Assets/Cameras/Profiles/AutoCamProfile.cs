using UnityEngine;

namespace StarterAssets
{
    [CreateAssetMenu(menuName = "Camera/AutoCamProfile")]
    public class AutoCamProfile : ScriptableObject
    {
        public float TopClamp = 70.0f;
        public float BottomClamp = -30.0f;

        public bool AutoCam = true;
        public float AirTiltSpeedUp = 60f;
        public float AirTiltSpeedDown = 40f;
        public float AutoCamTopSlowdownRange = 10f;
        public float AutoCamDownEaseInTime = 0.25f;
        public float AutoCamMinAirTime = 0.1f;
        public float AutoCamGroundDelay = 0.2f;

        public float CameraAngleOverride = 0.0f;
    }
}
