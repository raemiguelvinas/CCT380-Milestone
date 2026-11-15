using UnityEngine;
using UnityEngine.Events;

// event channel that carries a point and the surface normal there
// for example where the grapple rope landed
// create it from the asset create menu under events
[CreateAssetMenu(menuName = "Events/Point Normal Game Event", fileName = "NewPointNormalGameEvent")]
public class PointNormalGameEvent : ScriptableObject
{
    private event UnityAction<Vector3, Vector3> listeners;

    public void raise(Vector3 point, Vector3 normal)
    {
        if (listeners != null)
        {
            listeners.Invoke(point, normal);
        }
    }

    public void register(UnityAction<Vector3, Vector3> a)
    {
        listeners += a;
    }

    public void unregister(UnityAction<Vector3, Vector3> a)
    {
        listeners -= a;
    }
}
