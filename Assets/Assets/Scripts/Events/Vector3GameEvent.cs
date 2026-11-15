using UnityEngine;
using UnityEngine.Events;

// same as game event but carries a direction or position
// for example the dash direction
// create it from the asset create menu under events
[CreateAssetMenu(menuName = "Events/Vector3 Game Event", fileName = "NewVector3GameEvent")]
public class Vector3GameEvent : ScriptableObject
{
    private event UnityAction<Vector3> listeners;

    public void raise(Vector3 value)
    {
        if (listeners != null)
        {
            listeners.Invoke(value);
        }
    }

    public void register(UnityAction<Vector3> a)
    {
        listeners += a;
    }

    public void unregister(UnityAction<Vector3> a)
    {
        listeners -= a;
    }
}
