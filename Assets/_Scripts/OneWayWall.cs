using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class MusicTriggerOnExit : MonoBehaviour
{
    [SerializeField] private EventReference musicEvent;
    [SerializeField] private GameObject childToActivate;

    private bool hasTriggered;
    private EventInstance musicInstance;
    private bool isPlaying;

    private void OnTriggerExit(Collider other)
    {
        if (hasTriggered) return;
        if (!other.CompareTag("Player")) return;

        hasTriggered = true;

        PlayMusic();

        if (childToActivate != null)
            childToActivate.SetActive(true);
    }

    public void PlayMusic()
    {
        if (isPlaying) return;
        if (musicEvent.IsNull) return;

        musicInstance = RuntimeManager.CreateInstance(musicEvent);
        RuntimeManager.AttachInstanceToGameObject(musicInstance, gameObject);
        musicInstance.start();
        isPlaying = true;
    }

    public void StopMusic()
    {
        StopMusicFade();
    }

    public void StopMusicFade()
    {
        StopInternal(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
    }

    public void StopMusicImmediate()
    {
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
        if (!musicInstance.isValid()) return;
        musicInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        musicInstance.release();
    }
}
