using UnityEngine;

[CreateAssetMenu(fileName = "AutoCamProfile", menuName = "Camera/Auto Cam Profile")]
public class AutoCamProfile : ScriptableObject
{
    [Header("Manual Camera Clamp")]
    public float TopClamp = 70f;
    public float BottomClamp = -30f;

    [Header("Auto Cam Clamp")]
    public float AutoCamTopClamp = 70f;
    public float AutoCamBottomClamp = -30f;

    [Header("Auto Cam Toggle")]
    public bool AutoCam = true;

    [Header("Auto Cam Movement Speeds")]
    public float AirTiltSpeedUp = 70f;
    public float AirTiltSpeedDown = 44f;

    [Header("Air Slowdown Near Clamp")]
    public float AutoCamTopSlowdownRange = 3f;

    [Header("Ground Transition Timing")]
    public float AutoCamDownEaseInTime = 0.25f;
    public float AutoCamMinAirTime = 0.1f;
    public float AutoCamGroundDelay = 0.4444f;

    [Header("Rotation Override")]
    public float CameraAngleOverride = 0f;
}
