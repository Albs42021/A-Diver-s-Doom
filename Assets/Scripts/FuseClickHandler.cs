using UnityEngine;

public class FuseClickHandler : MonoBehaviour
{
    void OnMouseDown()
    {
        GetComponentInParent<FusePuzzle>()
            .RotateFuse(gameObject);
    }
}
