using UnityEngine;

// settings for how sound effects vary
// right now that's a random pitch on every play so repeats don't sound identical
// create it from the asset create menu under config
[CreateAssetMenu(menuName = "Config/Sound Config", fileName = "SoundConfig")]
public class SoundConfig : ScriptableObject
{
    [Tooltip("Lowest pitch a sound can get. 1 is the normal pitch.")]
    [Range(0.5f, 1.5f)] public float minPitch = 0.95f;
    [Tooltip("Highest pitch a sound can get. 1 is the normal pitch.")]
    [Range(0.5f, 1.5f)] public float maxPitch = 1.05f;

    // a pitch of 1 is the normal sound and 2 is an octave higher and 0.5 is an octave lower
    // so small changes around 1 sound like natural variation and not a different sound
    public float randomPitch()
    {
        // swap them if they were entered the wrong way round
        float low = Mathf.Min(minPitch, maxPitch);
        float high = Mathf.Max(minPitch, maxPitch);
        return Random.Range(low, high);
    }
}
