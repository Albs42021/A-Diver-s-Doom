using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class EscapeDoor : MonoBehaviour
{
    public TMP_Text escapeText;
    public Animator doorAnimator;

    public void Open()
    {
        Debug.Log("You Escaped!");

        if (doorAnimator != null)
            doorAnimator.SetTrigger("Open");

        if (escapeText != null)
        {
            escapeText.text = "You Escaped!";
            escapeText.gameObject.SetActive(true);
        }

        Invoke("GoToTransitionScene", 5f);
    }

    void GoToTransitionScene()
    {
        SceneManager.LoadScene("TransitionScene");
    }
}
