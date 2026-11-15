using UnityEngine;

// plays a sound when an event asset fires
// put it on an object and drop in the event and the clip
// works with every event type so game event or bool or float or vector3 or point normal
// any value the event carries is ignored
// it uses play one shot so repeats overlap instead of cutting each other off
// the pitch is randomised each time using the sound config asset
// volume and 2d or 3d sound come from the audio source on the same object
[RequireComponent(typeof(AudioSource))]
public class GameEventSound : MonoBehaviour
{
    [Tooltip("Any event asset: Game Event, Bool, Float, Vector3 or Point Normal")]
    [SerializeField] private ScriptableObject gameEvent;
    [SerializeField] private AudioClip clip;
    [SerializeField] private SoundConfig config;

    private AudioSource source;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;

        if (config == null)
        {
            Debug.LogError($"{name}: No SoundConfig assigned. Using defaults", this);
            config = ScriptableObject.CreateInstance<SoundConfig>();
        }

        if (gameEvent == null)
        {
            Debug.LogWarning($"{name}: GameEventSound has no event assigned", this);
        }
        else if (!isSupported(gameEvent))
        {
            Debug.LogError($"{name}: GameEventSound can't use '{gameEvent.name}', it isn't an event asset", this);
        }

        if (clip == null)
        {
            Debug.LogWarning($"{name}: GameEventSound has no clip assigned", this);
        }
    }

    // stops a non event asset like a config from being dropped into the slot
    private void OnValidate()
    {
        if (gameEvent != null && !isSupported(gameEvent))
        {
            Debug.LogError($"GameEventSound: '{gameEvent.name}' isn't an event asset so the slot was cleared", this);
            gameEvent = null;
        }
    }

    private void OnEnable()
    {
        switch (gameEvent)
        {
            case GameEvent e:
                e.register(play);
                break;
            case BoolGameEvent e:
                e.register(play);
                break;
            case FloatGameEvent e:
                e.register(play);
                break;
            case Vector3GameEvent e:
                e.register(play);
                break;
            case PointNormalGameEvent e:
                e.register(play);
                break;
        }
    }

    private void OnDisable()
    {
        switch (gameEvent)
        {
            case GameEvent e:
                e.unregister(play);
                break;
            case BoolGameEvent e:
                e.unregister(play);
                break;
            case FloatGameEvent e:
                e.unregister(play);
                break;
            case Vector3GameEvent e:
                e.unregister(play);
                break;
            case PointNormalGameEvent e:
                e.unregister(play);
                break;
        }
    }

    private bool isSupported(ScriptableObject asset)
    {
        return asset is GameEvent || asset is BoolGameEvent || asset is FloatGameEvent
            || asset is Vector3GameEvent || asset is PointNormalGameEvent;
    }

    // one play overload per event type
    // value is ignored because we only care that it fired
    private void play()
    {
        if (clip != null)
        {
            // pitch lives on the audio source so a random one is picked right before each play
            source.pitch = config.randomPitch();
            source.PlayOneShot(clip);
        }
    }

    private void play(bool value)
    {
        play();
    }

    private void play(float value)
    {
        play();
    }

    private void play(Vector3 value)
    {
        play();
    }

    private void play(Vector3 point, Vector3 normal)
    {
        play();
    }
}
