using UnityEngine;
using UnityEngine.InputSystem;  // <-- Add this

public class FuseRaycastClick : MonoBehaviour
{
    public float maxDistance = 5f;

    void Update()
    {
        // Using the new Input System's Mouse.current
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
            {
                Debug.Log("Raycast hit: " + hit.collider.gameObject.name);
                FuseClickHandler fuse = hit.collider.GetComponent<FuseClickHandler>();
                if (fuse != null)
                {
                    fuse.OnFuseClicked();
                }
            }
            else
            {
                Debug.Log("Raycast did not hit anything");
            }
        }
    }
}
