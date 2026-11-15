using UnityEngine;
using UnityEngine.Events;

// listener for a vector3 game event
// the response receives the vector
public class Vector3GameEventListener : MonoBehaviour
{
    [SerializeField] private Vector3GameEvent gameEvent;
    public UnityEvent<Vector3> response;

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

    private void onRaised(Vector3 value)
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
