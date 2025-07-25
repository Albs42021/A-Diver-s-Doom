using UnityEngine;
using UnityEngine.InputSystem;

public class RaycastClick : MonoBehaviour
{
    public float maxDistance = 5f;

    void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
            {
                Debug.Log("Raycast hit: " + hit.collider.gameObject.name);

                // Handle fuse interaction
                var fuse = hit.collider.GetComponent<FuseClickHandler>();
                if (fuse != null)
                {
                    fuse.OnFuseClicked();
                    return;
                }

                // Handle escape pod interaction
                var pod = hit.collider.GetComponent<EscapePodClickHandler>();
                if (pod != null)
                {
                    pod.OnEscapePodClicked();
                    return;
                }

                // Handle escape pod launch button
                var launchButton = hit.collider.GetComponent<EscapePodLaunchButton>();
                if (launchButton != null)
                {
                    launchButton.OnEscapeButtonPressed();
                }
            }
        }
    }
}
