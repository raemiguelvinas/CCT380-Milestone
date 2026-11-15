using UnityEngine;
using UnityEngine.Events;

// same as game event but carries one number
// for example the fall speed on landing
// create it from the asset create menu under events
[CreateAssetMenu(menuName = "Events/Float Game Event", fileName = "NewFloatGameEvent")]
public class FloatGameEvent : ScriptableObject
{
    private event UnityAction<float> listeners;

    public void raise(float value)
    {
        if (listeners != null)
        {
            listeners.Invoke(value);
        }
    }

    public void register(UnityAction<float> a)
    {
        listeners += a;
    }

    public void unregister(UnityAction<float> a)
    {
        listeners -= a;
    }
}
