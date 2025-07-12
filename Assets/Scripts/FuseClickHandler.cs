using UnityEngine;

public class FuseClickHandler : MonoBehaviour
{
    public void OnFuseClicked()
    {
        var parentPuzzle = GetComponentInParent<FusePuzzle>();
        if (parentPuzzle != null)
        {
            parentPuzzle.RotateFuse(gameObject);
        }
        else
        {
            Debug.LogWarning("No FusePuzzle script found on parent!");
        }
    }
}
