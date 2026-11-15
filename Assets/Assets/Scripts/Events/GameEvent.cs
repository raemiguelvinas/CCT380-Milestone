using UnityEngine;
using UnityEngine.Events;

// an event channel asset
// senders call raise and anything that cares listens through a game event listener
// or through register and unregister in code
// sender and listeners never reference each other
// create it from the asset create menu under events
[CreateAssetMenu(menuName = "Events/Game Event", fileName = "NewGameEvent")]
public class GameEvent : ScriptableObject
{
    private event UnityAction listeners;

    public void raise()
    {
        if (listeners != null)
        {
            listeners.Invoke();
        }
    }

    public void register(UnityAction a)
    {
        listeners += a;
    }

    public void unregister(UnityAction a)
    {
        listeners -= a;
    }
}
