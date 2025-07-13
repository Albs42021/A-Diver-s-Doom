using UnityEngine;

public class FuseClickHandler : MonoBehaviour
{
    public AudioClip rotateSound;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogWarning("AudioSource component not found on FuseClickHandler!");
        }
    }

    public void OnFuseClicked()
    {
        var parentPuzzle = GetComponentInParent<FusePuzzle>();
        if (parentPuzzle != null)
        {
            parentPuzzle.RotateFuse(gameObject);

            if (parentPuzzle.solved == false)
                {
                    if (audioSource != null && rotateSound != null)
                    {
                        audioSource.PlayOneShot(rotateSound);
                    }
                }
        }
        else
        {
            Debug.LogWarning("No FusePuzzle script found on parent!");
        }
    }
}
