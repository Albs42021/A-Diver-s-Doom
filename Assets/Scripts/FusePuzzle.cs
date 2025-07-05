using UnityEngine;
using TMPro;

public class FusePuzzle : MonoBehaviour
{
    public GameObject[] fuses;
    private int solvedCount = 0;
    public TMP_Text puzzleStatusText;

    void Start()
    {
        foreach (var fuse in fuses)
            fuse.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
        if (puzzleStatusText != null)
            puzzleStatusText.text = "Align all fuses to solve.";
    }

    public void RotateFuse(GameObject fuse)
    {
        fuse.transform.Rotate(0, 90, 0);
        CheckSolved();
    }

    void CheckSolved()
    {
        solvedCount = 0;
        foreach (var fuse in fuses)
            if (Mathf.Abs(fuse.transform.localEulerAngles.y % 360) < 5f)
                solvedCount++;

        if (solvedCount == fuses.Length)
        {
            Debug.Log("Puzzle Solved!");
            if (puzzleStatusText != null)
                puzzleStatusText.text = "Puzzle Solved!";
            GetComponentInParent<RoomConnector>().enabled = true;
        }
    }
}
