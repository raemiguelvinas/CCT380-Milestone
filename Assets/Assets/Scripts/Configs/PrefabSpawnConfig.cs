using UnityEngine;

// what a prefab spawner spawns and how
// make one per effect like jump dust or a dash ring or a wavejump burst
// create it from the asset create menu under config
[CreateAssetMenu(menuName = "Config/Prefab Spawn Config", fileName = "PrefabSpawnConfig")]
public class PrefabSpawnConfig : ScriptableObject
{
    public enum AlignAxis { Up, Forward }

    public GameObject prefab;
    [Tooltip("Spawn() uses the spawn point's rotation instead of the prefab's own.")]
    public bool useSpawnPointRotation = false;
    [Tooltip("Destroy the spawned object after this many seconds. 0 = leave it (e.g. it destroys itself).")]
    public float destroyAfter = 0f;
    [Tooltip("Wait this many seconds after being told to spawn (fractions work, e.g. 0.25).")]
    public float delaySpawn = 0f;

    [Header("Surface spawning (SpawnAtSurface)")]
    [Tooltip("Which of the prefab's axes points away from the surface. Up suits effects built to shoot upward (the usual Unity particle setup); Forward suits ones that shoot along their blue Z arrow.")]
    public AlignAxis alignAxis = AlignAxis.Up;
    [Tooltip("Lift the effect off the surface by this much so it doesn't clip into it.")]
    public float surfaceOffset = 0.02f;
}
