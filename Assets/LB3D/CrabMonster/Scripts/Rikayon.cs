using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


public class Rikayon : MonoBehaviour {

    public Animator animator;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {

        // Use new Input System instead of legacy Input.GetKeyDown
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) {
            animator.SetTrigger("Attack_1");
        }

	}
}
