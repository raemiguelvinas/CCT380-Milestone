using UnityEngine;
using UnityEngine.Events;

// listener for a point normal game event
// the response receives the point and the normal
public class PointNormalGameEventListener : MonoBehaviour
{
    [SerializeField] private PointNormalGameEvent gameEvent;
    public UnityEvent<Vector3, Vector3> response;

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

    private void onRaised(Vector3 point, Vector3 normal)
    {
        try
        {
            response.Invoke(point, normal);
        }
        catch (System.Exception e)
        {
            Debug.LogException(e, this);
        }
    }
}
