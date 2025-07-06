using UnityEngine;

public class FuseClickHandler : MonoBehaviour
{
    public void OnFuseClicked()
    {
        Debug.Log($"Fuse clicked: {gameObject.name}");
        GetComponentInParent<FusePuzzle>().RotateFuse(gameObject);
    }
}
