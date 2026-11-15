using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class TeleportTrigger : MonoBehaviour
{
    // where the player gets sent
    public Transform teleportTarget;

    private void Reset()
    {
        // make the collider a trigger so the player passes through it
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        CharacterController controller = other.GetComponent<CharacterController>();
        Debug.Log("hi");

        // only the player has a character controller so ignore anything else
        if (controller == null)
        {
            Debug.Log("uh oh");
            return;
        }

        // the controller overrides position changes while it's enabled so switch it off for the move
        controller.enabled = false;
        other.transform.position = teleportTarget.position;
        controller.enabled = true;
    }
}