using UnityEngine;
using UnityEngine.Events;

// listener for a bool game event
// the response receives true or false
public class BoolGameEventListener : MonoBehaviour
{
    [SerializeField] private BoolGameEvent gameEvent;
    public UnityEvent<bool> response;

    private void OnEnable()
    {
        if (gameEvent != null)
        {
            gameEvent.register(onRaised);
        }
    }

    private void OnDisable()
    {
        if (gameEvent != null)
        {
            gameEvent.unregister(onRaised);
        }
    }

    private void onRaised(bool value)
    {
        try
        {
            response.Invoke(value);
        }
        catch (System.Exception e)
        {
            Debug.LogException(e, this);
        }
    }
}
