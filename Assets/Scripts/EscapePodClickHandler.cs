using UnityEngine;

public class EscapePodClickHandler : MonoBehaviour
{
    [Tooltip("Where the player will be teleported to.")]
    public Transform teleportTarget;

    [Tooltip("Tag used to find the player.")]
    public string playerTag = "Player";

    public void OnEscapePodClicked()
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);

        if (player == null)
        {
            Debug.LogWarning("Player with tag '" + playerTag + "' not found.");
            return;
        }

        if (teleportTarget == null)
        {
            Debug.LogWarning("Teleport target not set on EscapePodClickHandler.");
            return;
        }

        CharacterController controller = player.GetComponent<CharacterController>();

        if (controller != null)
        {
            controller.enabled = false;
        }

        // Actually move the player
        player.transform.SetPositionAndRotation(
            teleportTarget.position,
            teleportTarget.rotation
        );

        if (controller != null)
        {
            controller.enabled = true;
        }

        Debug.Log("Player teleported to escape pod.");
    }
}
