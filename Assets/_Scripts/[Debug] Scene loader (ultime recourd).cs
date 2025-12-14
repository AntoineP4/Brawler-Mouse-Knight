using UnityEngine;
using UnityEngine.SceneManagement;
using FMODUnity;
using FMOD.Studio;

public class Debug_SceneLoader_UltimeRecourd : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string sceneA;
    [SerializeField] private string sceneB;
    [SerializeField] private string sceneC;
    [SerializeField] private string sceneD;

    private bool isLoading;

    private void Update()
    {
        if (isLoading) return;

        if (Input.GetKeyDown(KeyCode.F1)) Load(sceneA);
        else if (Input.GetKeyDown(KeyCode.F2)) Load(sceneB);
        else if (Input.GetKeyDown(KeyCode.F3)) Load(sceneC);
        else if (Input.GetKeyDown(KeyCode.F4)) Load(sceneD);
    }

    private void Load(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName)) return;

        isLoading = true;

        RuntimeManager.CoreSystem.mixerSuspend();
        RuntimeManager.GetBus("bus:/").stopAllEvents(FMOD.Studio.STOP_MODE.IMMEDIATE);
        RuntimeManager.CoreSystem.mixerResume();

        SceneManager.LoadScene(sceneName);
    }
}
