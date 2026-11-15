using UnityEngine;
using UnityEngine.Events;

// put this on any object that should react to a game event
// wire the response in the inspector exactly like a button's on click
public class GameEventListener : MonoBehaviour
{
    [SerializeField] private GameEvent gameEvent;
    public UnityEvent response;

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

    private void onRaised()
    {
        // a misconfigured response should show an error and not stop the code that raised the event like jumping
        try
        {
            response.Invoke();
        }
        catch (System.Exception e)
        {
            Debug.LogException(e, this);
        }
    }
}
