using UnityEngine;

public class MusicManager : MonoBehaviour
{
    [SerializeField]
    private AudioSource source;

    public void Play(AudioClip clip)
    {
        source.clip = clip;
        source.loop = true;
        source.Play();
    }

    public void Stop()
    {
        source.Stop();
    }

    public void ChangeVolume(float volume)
    {
        source.volume = volume;
    }
}
