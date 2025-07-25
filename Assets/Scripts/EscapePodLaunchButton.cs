using UnityEngine;

public class EscapePodLaunchButton : MonoBehaviour
{
    public Door escapeDoor;                   // Reference to the door to open
    public EscapeManager escapeManager;       // Reference to the fade/escape manager

    public void OnEscapeButtonPressed()
    {
        if (escapeDoor != null)
        {
            escapeDoor.Open();
        }

        if (escapeManager != null)
        {
            escapeManager.TriggerWinSequence();
        }
        else
        {
            Debug.LogWarning("EscapeManager not assigned.");
        }
    }
}
