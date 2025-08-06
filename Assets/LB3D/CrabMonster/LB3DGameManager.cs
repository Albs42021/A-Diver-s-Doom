using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class LB3DGameManager : MonoBehaviour {

    public Animator animator;

    public GameObject cam;
    public GameObject product;
    public Slider rotate;
    public float rotationSpeed = 1;
    public Slider zoom;
    public Vector3 originalCamPosition;

    public Dropdown loops;
    public Dropdown actions;
    public Button play;

    // Use this for initialization
    void Start () {
        originalCamPosition = cam.transform.position;
    }
	
	// Update is called once per frame
	void Update () {
        Quaternion startRotation = product.transform.rotation;        
        Quaternion toRotation = Quaternion.Euler(new Vector3(product.transform.rotation.eulerAngles.x, 360.0f-rotate.value, product.transform.rotation.eulerAngles.z));        
        product.transform.rotation = Quaternion.Lerp(startRotation, toRotation, Time.deltaTime * rotationSpeed);

        cam.transform.position = originalCamPosition + new Vector3(0, 0, zoom.value)*5f;

        // Use new Input System instead of legacy Input.GetKeyDown
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

    }

    public void PlayAction() {
        int actionIndex = actions.value;
        string actionString = actions.options[actionIndex].text;
        animator.SetTrigger(actionString);
    }

    public void PlayLoop() {
        int loopIndex = loops.value;
        string loopString = loops.options[loopIndex].text;
        animator.SetTrigger(loopString);
    }

}
