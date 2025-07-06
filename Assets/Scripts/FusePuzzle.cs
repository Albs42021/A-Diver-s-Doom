using DG.Tweening; // ← Add this for DOTween
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class FusePuzzle : MonoBehaviour
{
    public List<GameObject> fuses;
    private bool[] fuseStates;
    public bool solved = false;

    void Start()
    {
        fuseStates = new bool[fuses.Count];
    }

    public void RotateFuse(GameObject fuse)
    {
        if (solved) return;

        int index = fuses.IndexOf(fuse);
        if (index == -1) return;

        fuseStates[index] = !fuseStates[index];

        Transform rotatingPart = fuse.transform.Find("RotatingPart");
        if (rotatingPart != null)
        {
            float targetAngle = fuseStates[index] ? 90f : 0f;

            // Kill any ongoing tweens on this object
            rotatingPart.DOKill();

            // Smoothly rotate over 0.25 seconds
            rotatingPart
                .DOLocalRotate(new Vector3(0, targetAngle, 0), 0.25f, RotateMode.Fast)
                .SetEase(Ease.OutQuad);
        }

        CheckPuzzle();
    }

    private void CheckPuzzle()
    {
        if (fuseStates.All(state => state))
        {
            solved = true;
            Debug.Log("Puzzle Solved!");
        }
    }
}
