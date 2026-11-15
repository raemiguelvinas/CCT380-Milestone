using UnityEngine;

public class SFXPlayer : MonoBehaviour
{
    public AudioSource audioSource;

    // the pitch is randomised a little on every play so repeats don't sound identical
    public SoundConfig config;

    public AudioClip dash;
    public AudioClip grappleWoosh;
    public AudioClip grappleLand;

    private void Awake()
    {
        if (config == null)
        {
            Debug.LogError($"{name}: No SoundConfig assigned. Using defaults.", this);
            config = ScriptableObject.CreateInstance<SoundConfig>();
        }
    }

    public void play()
    {
        audioSource.pitch = config.randomPitch();
        audioSource.Play();
    }

    public void playClip(AudioClip clip)
    {
        // pitch lives on the audio source so a random one is picked right before each play
        audioSource.pitch = config.randomPitch();
        audioSource.PlayOneShot(clip);
    }

    public void playDash()
    {
        playClip(dash);
    }

    public void playGrappleWoosh()
    {
        playClip(grappleWoosh);
    }

    public void playGrappleLand()
    {
        playClip(grappleLand);
    }
}
