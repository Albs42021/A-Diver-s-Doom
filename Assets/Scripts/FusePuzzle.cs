using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine.Events;

public class FusePuzzle : MonoBehaviour
{
    [Header("Fuse Setup")]
    public List<GameObject> fuses;
    private int[] fuseAngles;       // current fuse rotation
    private int[] targetAngles;     // correct rotation

    [Header("Puzzle State")]
    public bool solved = false;

    [Header("Events")]
    public UnityEvent onPuzzleSolved;

    private ScreenShake screenShake;

    void Start()
    {
        fuseAngles = new int[fuses.Count];
        targetAngles = new int[fuses.Count];
        screenShake = Camera.main.GetComponent<ScreenShake>();

        GenerateRandomSolution();
        ScrambleFuses();
    }

    private void GenerateRandomSolution()
    {
        for (int i = 0; i < targetAngles.Length; i++)
        {
            targetAngles[i] = RandomAngle(); // e.g., 90°
        }
    }

    private void ScrambleFuses()
    {
        for (int i = 0; i < fuses.Count; i++)
        {
            int scrambleOffset = 90 * Random.Range(0, 4); // 0°, 90°, 180°, 270°
            fuseAngles[i] = (targetAngles[i] + scrambleOffset) % 360;

            Transform rp = fuses[i].transform.Find("RotatingPart");
            if (rp != null)
            {
                rp.localEulerAngles = new Vector3(0, fuseAngles[i], 0);
            }

            Debug.Log($"Fuse {i}: Target = {targetAngles[i]}, Start = {fuseAngles[i]}");
        }
    }

    private int RandomAngle()
    {
        int[] options = { 0, 90, 180, 270 };
        return options[Random.Range(0, options.Length)];
    }

    public void RotateFuse(GameObject fuse)
    {
        if (solved) return;

        int i = fuses.IndexOf(fuse);
        if (i < 0) return;

        fuseAngles[i] = (fuseAngles[i] + 90) % 360;

        Transform rotatingPart = fuse.transform.Find("RotatingPart");
        if (rotatingPart != null)
        {
            rotatingPart.DOKill();
            rotatingPart.DOLocalRotate(new Vector3(0, fuseAngles[i], 0), 0.3f).SetEase(Ease.OutQuad);
        }

        CheckPuzzle();
    }

    private void CheckPuzzle()
    {
        for (int i = 0; i < fuseAngles.Length; i++)
        {
            if (fuseAngles[i] != targetAngles[i])
                return;
        }

        if (!solved)
        {
            solved = true;
            Debug.Log("Puzzle solved!");
            screenShake?.Shake();
            onPuzzleSolved?.Invoke();
        }
    }
}
