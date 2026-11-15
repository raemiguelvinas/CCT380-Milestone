using System.Collections;
using UnityEngine;

// spawns a prefab like a dust puff or a ring or a burst at a point when told to
// hook spawn or spawn facing or spawn at or spawn at surface up to a listener's response
// so effects happen from events instead of from the gameplay code
// what gets spawned and how lives in the prefab spawn config asset
// the spawn point lives here because it's in the scene
public class PrefabSpawner : MonoBehaviour
{
    [SerializeField] private PrefabSpawnConfig config;
    [Tooltip("Where to spawn. Leave empty to use this object.")]
    [SerializeField] private Transform spawnPoint;

    private void Awake()
    {
        if (config == null)
        {
            Debug.LogError($"{name}: No PrefabSpawnConfig assigned, so nothing will spawn.", this);
            config = ScriptableObject.CreateInstance<PrefabSpawnConfig>();
        }
    }

    // note these must stay void
    // a unity event can't call methods that return something like an async task
    // and that throws an error every time the event fires

    // spawn at the spawn point after the delay if one is set
    public void spawn()
    {
        if (config.prefab == null)
        {
            return;
        }

        if (config.delaySpawn > 0f)
        {
            StartCoroutine(spawnAfterDelay());
        }
        else
        {
            spawnNow();
        }
    }

    // spawn at the spawn point pointing along a direction like the dash direction
    public void spawnFacing(Vector3 direction)
    {
        if (config.prefab == null)
        {
            return;
        }

        Transform point = getSpawnPoint();

        // look rotation turns the forward direction to face the given direction
        // a direction with no length has nothing to face so keep the prefab's own rotation
        Quaternion rot = config.prefab.transform.rotation;
        if (direction.sqrMagnitude > 0.0001f)
        {
            rot = Quaternion.LookRotation(direction);
        }
        create(point.position, rot);
    }

    // spawn at an exact world position like where the grapple rope landed
    // wire this to a vector3 event
    public void spawnAt(Vector3 position)
    {
        if (config.prefab == null)
        {
            return;
        }

        create(position, config.prefab.transform.rotation);
    }

    // spawn at a point on a surface turned so the prefab points away from it along the surface normal
    public void spawnAtSurface(Vector3 point, Vector3 normal)
    {
        if (config.prefab == null)
        {
            return;
        }

        if (normal.sqrMagnitude < 0.0001f)
        {
            normal = Vector3.up;
        }
        normal.Normalize();

        // turn the whole prefab while keeping its own authored rotation so its up or forward becomes the normal
        // from to rotation gives the turn that carries up onto the normal
        // look rotation does the same for the forward direction
        Quaternion align;
        if (config.alignAxis == PrefabSpawnConfig.AlignAxis.Up)
        {
            align = Quaternion.FromToRotation(Vector3.up, normal);
        }
        else
        {
            align = Quaternion.LookRotation(normal);
        }

        // lift the effect off the surface along the normal so it doesn't clip into it
        // multiplying the two rotations applies the prefab's own rotation first and then the alignment
        create(point + normal * config.surfaceOffset, align * config.prefab.transform.rotation);
    }

    private IEnumerator spawnAfterDelay()
    {
        yield return new WaitForSeconds(config.delaySpawn);
        spawnNow();
    }

    private void spawnNow()
    {
        Transform point = getSpawnPoint();

        Quaternion rot = config.prefab.transform.rotation;
        if (config.useSpawnPointRotation)
        {
            rot = point.rotation;
        }
        create(point.position, rot);
    }

    private Transform getSpawnPoint()
    {
        if (spawnPoint != null)
        {
            return spawnPoint;
        }
        return transform;
    }

    private void create(Vector3 position, Quaternion rotation)
    {
        var instance = Instantiate(config.prefab, position, rotation);
        if (config.destroyAfter > 0f)
        {
            Destroy(instance, config.destroyAfter);
        }
    }
}
