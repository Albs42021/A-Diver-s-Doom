// CHANGE LOG
// 
// CHANGES || version VERSION
//
// "Enable/Disable Headbob, Changed look rotations - should result in reduced camera jitters" || version 1.0.3

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;
using System.Net;
#endif

public class FirstPersonController : MonoBehaviour
{
    private Rigidbody rb;

    public Camera playerCamera;
    public float fov = 60f;
    public bool invertCamera = false;
    public bool cameraCanMove = true;
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 50f;
    public bool lockCursor = true;
    public bool crosshair = true;
    public Sprite crosshairImage;
    public Color crosshairColor = Color.white;
    private float yaw = 0.0f;
    private float pitch = 0.0f;
    private Image crosshairObject;

    public bool playerCanMove = true;
    public float walkSpeed = 5f;
    public float sprintMultiplier = 1.5f;
    public bool enableJump = true;
    public float jumpPower = 5f;
    public bool enableHeadBob = true;
    public Transform joint;
    public float bobSpeed = 10f;
    public Vector3 bobAmount = new Vector3(.15f, .05f, 0f);

    public LayerMask waterLayer;
    public float swimSpeed = 3f;
    public float waterCheckRadius = 0.5f;
    public float swimSinkSpeed = 0.5f;

    public LayerMask climbableLayer;
    public float climbSpeed = 3f;
    private bool isClimbing = false;

    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool jumpInput;
    private bool sprinting;
    private bool isInWater = false;

    private bool isGrounded = false;
    private Vector3 jointOriginalPos;
    private float timer = 0;

    public InputActionAsset inputActions;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction sprintAction;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        crosshairObject = GetComponentInChildren<Image>();
        if (playerCamera != null)
            playerCamera.fieldOfView = fov;

        if (joint != null)
            jointOriginalPos = joint.localPosition;

        if (inputActions != null)
        {
            var playerControls = inputActions.FindActionMap("Player", true);
            if (playerControls != null)
            {
                moveAction = playerControls.FindAction("Move", true);
                lookAction = playerControls.FindAction("Look", true);
                jumpAction = playerControls.FindAction("Jump", true);
                sprintAction = playerControls.FindAction("Sprint", true);

                moveAction?.Enable();
                lookAction?.Enable();
                jumpAction?.Enable();
                sprintAction?.Enable();
            }
            else
            {
                Debug.LogError("Player action map not found in InputActions.");
            }
        }
        else
        {
            Debug.LogError("InputActions asset not assigned.");
        }
    }

    void Start()
    {
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (crosshair && crosshairObject != null)
        {
            crosshairObject.sprite = crosshairImage;
            crosshairObject.color = crosshairColor;
        }
        else if (crosshairObject != null)
        {
            crosshairObject.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (moveAction == null || lookAction == null || jumpAction == null || sprintAction == null)
            return;

        moveInput = moveAction.ReadValue<Vector2>();
        lookInput = lookAction.ReadValue<Vector2>();
        jumpInput = jumpAction.triggered;
        sprinting = sprintAction.ReadValue<float>() > 0.1f;

        if (cameraCanMove && playerCamera != null)
        {
            yaw += lookInput.x * mouseSensitivity;
            pitch += (invertCamera ? 1 : -1) * lookInput.y * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, -maxLookAngle, maxLookAngle);

            transform.localRotation = Quaternion.Euler(0, yaw, 0);
            playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
        }

        CheckWater();
        CheckClimb();

        if (!isInWater && enableJump && jumpInput && isGrounded && !isClimbing)
        {
            rb.AddForce(Vector3.up * jumpPower, ForceMode.Impulse);
        }

        if (enableHeadBob && joint != null)
        {
            HeadBob();
        }
    }

    private void FixedUpdate()
    {
        if (!playerCanMove || moveAction == null)
            return;

        Vector3 move = new Vector3(moveInput.x, 0, moveInput.y);
        move = transform.TransformDirection(move);

        if (isClimbing)
        {
            Vector3 climbMove = new Vector3(moveInput.x, moveInput.y, 0);
            climbMove = transform.TransformDirection(climbMove);
            rb.linearVelocity = climbMove * climbSpeed;
        }
        else if (isInWater)
        {
            float vertical = -swimSinkSpeed;
            if (Keyboard.current.spaceKey.isPressed) vertical = 1f;
            if (Keyboard.current.leftCtrlKey.isPressed) vertical = -1f;

            float currentSwimSpeed = sprinting ? swimSpeed * sprintMultiplier : swimSpeed;

            Vector3 swimDirection = (move + Vector3.up * vertical).normalized;
            rb.linearVelocity = swimDirection * currentSwimSpeed;
        }
        else
        {
            float currentSpeed = sprinting ? walkSpeed * sprintMultiplier : walkSpeed;
            move *= currentSpeed;

            Vector3 velocity = rb.linearVelocity;
            Vector3 velocityChange = move - velocity;
            velocityChange.y = 0;

            rb.AddForce(velocityChange, ForceMode.VelocityChange);
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        isGrounded = true;
    }

    private void OnCollisionExit(Collision collision)
    {
        isGrounded = false;
    }

    private void CheckWater()
    {
        bool nowInWater = Physics.CheckSphere(transform.position, waterCheckRadius, waterLayer);

        if (nowInWater != isInWater)
        {
            isInWater = nowInWater;
            ApplyUnderwaterEffects(isInWater);
        }
    }

    private void CheckClimb()
    {
        isClimbing = Physics.Raycast(transform.position, transform.forward, 1f, climbableLayer);
    }

    private void ApplyUnderwaterEffects(bool underwater)
    {
        if (underwater)
        {
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.0f, 0.4f, 0.7f, 1f);
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.02f;
            RenderSettings.ambientLight = new Color(0.2f, 0.4f, 0.5f);
        }
        else
        {
            RenderSettings.fog = false;
            RenderSettings.fogColor = Color.clear;
            RenderSettings.fogDensity = 0f;
            RenderSettings.ambientLight = Color.white;
        }
    }

    private void HeadBob()
    {
        if (moveInput != Vector2.zero && isGrounded && !isInWater)
        {
            timer += Time.deltaTime * bobSpeed;
            joint.localPosition = jointOriginalPos + new Vector3(Mathf.Sin(timer) * bobAmount.x, Mathf.Sin(timer * 2) * bobAmount.y, 0);
        }
        else
        {
            timer = 0;
            joint.localPosition = Vector3.Lerp(joint.localPosition, jointOriginalPos, Time.deltaTime * bobSpeed);
        }
    }
}
