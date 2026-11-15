using UnityEngine;
using UnityEngine.Events;

// event channel that carries a true or false value
// for example whether the crosshair is on a valid target
// create it from the asset create menu under events
[CreateAssetMenu(menuName = "Events/Bool Game Event", fileName = "NewBoolGameEvent")]
public class BoolGameEvent : ScriptableObject
{
    private event UnityAction<bool> listeners;

    public void raise(bool value)
    {
        if (listeners != null)
        {
            listeners.Invoke(value);
        }
    }

    public void register(UnityAction<bool> a)
    {
        listeners += a;
    }

    public void unregister(UnityAction<bool> a)
    {
        listeners -= a;
    }
}
