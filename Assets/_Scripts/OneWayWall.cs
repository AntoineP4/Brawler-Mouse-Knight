using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class MusicTriggerOnExit : MonoBehaviour
{
    [Header("Music")]
    [SerializeField] private bool disableMusic = false;
    [SerializeField] private EventReference musicEvent;

    [Header("Other")]
    [SerializeField] private GameObject childToActivate;

    private bool hasTriggered;
    private EventInstance musicInstance;
    private bool isPlaying;

    private void OnTriggerExit(Collider other)
    {
        if (hasTriggered) return;
        if (!other.CompareTag("Player")) return;

        hasTriggered = true;

        if (!disableMusic)
            PlayMusic();

        if (childToActivate != null)
            childToActivate.SetActive(true);
    }

    public void PlayMusic()
    {
        if (disableMusic) return;
        if (isPlaying) return;
        if (musicEvent.IsNull) return;

        musicInstance = RuntimeManager.CreateInstance(musicEvent);
        RuntimeManager.AttachInstanceToGameObject(musicInstance, gameObject);
        musicInstance.start();
        isPlaying = true;
    }

    public void StopMusic()
    {
        if (disableMusic) return;
        StopMusicFade();
    }

    public void StopMusicFade()
    {
        if (disableMusic) return;
        StopInternal(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
    }

    public void StopMusicImmediate()
    {
        if (disableMusic) return;
        StopInternal(FMOD.Studio.STOP_MODE.IMMEDIATE);
    }

    private void StopInternal(FMOD.Studio.STOP_MODE mode)
    {
        if (!isPlaying) return;

        if (musicInstance.isValid())
        {
            musicInstance.stop(mode);
            musicInstance.release();
        }

        isPlaying = false;
    }

    private void OnDestroy()
    {
        if (disableMusic) return;
        if (!musicInstance.isValid()) return;

        musicInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        musicInstance.release();
    }
}
