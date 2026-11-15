using UnityEngine;
using UnityEngine.Events;

// listener for a float game event
// the response receives the number
public class FloatGameEventListener : MonoBehaviour
{
    [SerializeField] private FloatGameEvent gameEvent;
    public UnityEvent<float> response;

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

    private void onRaised(float value)
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
