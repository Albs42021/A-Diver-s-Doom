using UnityEngine;
using DG.Tweening;

public class Door : MonoBehaviour
{
    public Vector3 openPositionOffset = new Vector3(0, 3, 0);
    public float openDuration = 1f;

    public AudioClip openSound;
    private AudioSource audioSource;

    private Vector3 closedPosition;
    private bool isOpen = false;

    void Start()
    {
        closedPosition = transform.position;
        audioSource = GetComponent<AudioSource>();
    }

    public void Open()
    {
        if (isOpen) return;

        isOpen = true;

        if (audioSource != null && openSound != null)
        {
            audioSource.PlayOneShot(openSound);
        }

        transform.DOMove(closedPosition + openPositionOffset, openDuration).SetEase(Ease.OutQuad);
    }
}
