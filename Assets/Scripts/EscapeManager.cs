using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class EscapeManager : MonoBehaviour
{
    [Header("Fade Settings")]
    public CanvasGroup fadeCanvas;
    public float fadeDuration = 2f;
    public string nextSceneName = "WinScene";

    private void Start()
    {
        if (fadeCanvas != null)
        {
            // Start fully black and fade in
            fadeCanvas.alpha = 1f;
            fadeCanvas.blocksRaycasts = true;
            StartCoroutine(FadeIn());
        }
    }

    public void TriggerWinSequence()
    {
        Debug.Log("Escape sequence triggered.");
        StartCoroutine(FadeAndLoad());
    }

    private IEnumerator FadeAndLoad()
    {
        if (fadeCanvas == null)
        {
            Debug.LogWarning("FadeCanvas is not assigned!");
            yield break;
        }

        float time = 0f;
        fadeCanvas.blocksRaycasts = true;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            fadeCanvas.alpha = Mathf.Lerp(0f, 1f, time / fadeDuration);
            yield return null;
        }

        fadeCanvas.alpha = 1f;

        // Kill DOTween animations if any remain
        DG.Tweening.DOTween.KillAll();

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.Log("No scene specified. Escape complete.");
        }
    }

    private IEnumerator FadeIn()
    {
        float time = 0f;
        fadeCanvas.blocksRaycasts = true;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            fadeCanvas.alpha = Mathf.Lerp(1f, 0f, time / fadeDuration);
            yield return null;
        }

        fadeCanvas.alpha = 0f;
        fadeCanvas.blocksRaycasts = false;
    }
}
