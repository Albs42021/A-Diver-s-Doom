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

    // Surface Detection
    [Header("Surface Detection")]
    public float groundCheckDistance = 1.5f;
    public LayerMask groundCheckLayer = -1;
    private string currentSurfaceType = "Default";
    private GameObject currentGroundObject;

    // Footstep Audio
    [Header("Footstep Audio")]
    public bool enableFootsteps = true;
    public float footstepInterval = 0.5f;
    public float sprintFootstepInterval = 0.3f;
    public float footstepVolume = 0.5f;
    private AudioSource footstepAudioSource;
    private float nextFootstepTime = 0f;

    [Header("Surface-Specific Footstep Sounds")]
    public SurfaceFootstepSounds[] surfaceFootstepSounds;

    // Default fallback sounds
    [Header("Default Footstep Sounds")]
    public AudioClip[] defaultFootstepSounds;
    public AudioClip[] defaultSprintFootstepSounds;
    
    [Header("Water/Swimming Sounds")]
    public AudioClip[] waterFootstepSounds;
    public AudioClip[] swimSounds;
    public float swimSoundInterval = 0.6f;
    public float swimSprintSoundInterval = 0.4f;

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
        
        // Initialize footstep audio source
        footstepAudioSource = GetComponent<AudioSource>();
        if (footstepAudioSource == null)
        {
            footstepAudioSource = gameObject.AddComponent<AudioSource>();
        }
        footstepAudioSource.spatialBlend = 0f; // 2D audio
        footstepAudioSource.volume = footstepVolume;
        
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
        DetectSurface();

        if (!isInWater && enableJump && jumpInput && isGrounded && !isClimbing)
        {
            rb.AddForce(Vector3.up * jumpPower, ForceMode.Impulse);
        }

        if (enableHeadBob && joint != null)
        {
            HeadBob();
        }

        // Handle footstep sounds
        if (enableFootsteps)
        {
            HandleFootsteps();
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

    private void DetectSurface()
    {
        RaycastHit hit;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
        
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, groundCheckDistance, groundCheckLayer))
        {
            currentGroundObject = hit.collider.gameObject;
            
            // Try to get surface type from various sources
            string newSurfaceType = GetSurfaceType(hit);
            
            if (newSurfaceType != currentSurfaceType)
            {
                currentSurfaceType = newSurfaceType;
            }
        }
        else
        {
            currentSurfaceType = "Default";
            currentGroundObject = null;
        }
    }

    private string GetSurfaceType(RaycastHit hit)
    {
        try
        {
            // Priority 1: Check for SurfaceType component
            SurfaceType surfaceTypeComponent = hit.collider.GetComponent<SurfaceType>();
            if (surfaceTypeComponent != null)
            {
                try
                {
                    // If it's a terrain surface, get the surface type at the specific position
                    if (surfaceTypeComponent.isTerrainSurface)
                    {
                        string terrainSurfaceType = surfaceTypeComponent.GetSurfaceTypeAtPosition(hit.point);
                        return terrainSurfaceType;
                    }
                    else
                    {
                        return surfaceTypeComponent.surfaceTypeName;
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"Error getting terrain surface type: {ex.Message}. Using fallback.");
                    return surfaceTypeComponent.surfaceTypeName;
                }
            }

            // Priority 2: Check GameObject tag
            string tag = hit.collider.tag;
            if (!string.IsNullOrEmpty(tag) && tag != "Untagged")
            {
                return tag;
            }

            // Priority 3: Check layer name
            string layerName = LayerMask.LayerToName(hit.collider.gameObject.layer);
            if (!string.IsNullOrEmpty(layerName) && layerName != "Default")
            {
                return layerName;
            }

            // Priority 4: Check GameObject name for common surface keywords
            string objectName = hit.collider.name.ToLower();
            if (objectName.Contains("metal")) return "Metal";
            if (objectName.Contains("wood")) return "Wood";
            if (objectName.Contains("concrete") || objectName.Contains("stone")) return "Concrete";
            if (objectName.Contains("grass")) return "Grass";
            if (objectName.Contains("dirt") || objectName.Contains("ground")) return "Dirt";
            if (objectName.Contains("sand")) return "Sand";
            if (objectName.Contains("gravel")) return "Gravel";
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Error in GetSurfaceType: {ex.Message}. Using default.");
        }

        return "Default";
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

    private void HandleFootsteps()
    {
        // Play footsteps/swimming sounds if player is moving and can move
        if (moveInput != Vector2.zero && playerCanMove)
        {
            // Swimming sounds: play when in water and moving
            if (isInWater)
            {
                if (Time.time >= nextFootstepTime)
                {
                    PlayFootstepSound();
                    
                    // Use swimming-specific intervals
                    float interval = sprinting ? swimSprintSoundInterval : swimSoundInterval;
                    nextFootstepTime = Time.time + interval;
                }
            }
            // Ground footsteps: play when grounded and not climbing
            else if (isGrounded && !isClimbing)
            {
                if (Time.time >= nextFootstepTime)
                {
                    PlayFootstepSound();
                    
                    // Set next footstep time based on movement speed
                    float interval = sprinting ? sprintFootstepInterval : footstepInterval;
                    nextFootstepTime = Time.time + interval;
                }
            }
        }
    }

    private void PlayFootstepSound()
    {
        if (footstepAudioSource == null) return;

        AudioClip[] soundsToUse = null;

        // Handle water/swimming sounds first
        if (isInWater)
        {
            // Use swimming sounds if available, otherwise fall back to water footstep sounds
            if (swimSounds != null && swimSounds.Length > 0)
            {
                soundsToUse = swimSounds;
            }
            else if (waterFootstepSounds != null && waterFootstepSounds.Length > 0)
            {
                soundsToUse = waterFootstepSounds;
            }
        }
        else
        {
            // Find surface-specific sounds
            SurfaceFootstepSounds surfaceSounds = GetSurfaceFootstepSounds(currentSurfaceType);
            
            if (surfaceSounds != null)
            {
                // Use surface-specific sounds based on movement type
                if (sprinting && surfaceSounds.sprintSounds != null && surfaceSounds.sprintSounds.Length > 0)
                {
                    soundsToUse = surfaceSounds.sprintSounds;
                }
                else if (surfaceSounds.walkSounds != null && surfaceSounds.walkSounds.Length > 0)
                {
                    soundsToUse = surfaceSounds.walkSounds;
                }
            }
            
            // Fallback to default sounds
            if (soundsToUse == null)
            {
                if (sprinting && defaultSprintFootstepSounds != null && defaultSprintFootstepSounds.Length > 0)
                {
                    soundsToUse = defaultSprintFootstepSounds;
                }
                else if (defaultFootstepSounds != null && defaultFootstepSounds.Length > 0)
                {
                    soundsToUse = defaultFootstepSounds;
                }
            }
        }

        // Play random sound from the selected set
        if (soundsToUse != null)
        {
            AudioClip clipToPlay = soundsToUse[Random.Range(0, soundsToUse.Length)];
            if (clipToPlay != null)
            {
                // Adjust pitch for swimming sounds
                if (isInWater)
                {
                    footstepAudioSource.pitch = Random.Range(0.8f, 1.2f); // Wider pitch variation for swimming
                }
                else
                {
                    footstepAudioSource.pitch = Random.Range(0.9f, 1.1f); // Normal pitch variation for walking
                }
                
                footstepAudioSource.PlayOneShot(clipToPlay, footstepVolume);
            }
        }
    }

    private SurfaceFootstepSounds GetSurfaceFootstepSounds(string surfaceType)
    {
        if (surfaceFootstepSounds != null)
        {
            foreach (var surface in surfaceFootstepSounds)
            {
                if (surface.surfaceTypeName.Equals(surfaceType, System.StringComparison.OrdinalIgnoreCase))
                {
                    return surface;
                }
            }
        }
        return null;
    }
}

// Serializable class for surface-specific footstep sounds
[System.Serializable]
public class SurfaceFootstepSounds
{
    public string surfaceTypeName;
    public AudioClip[] walkSounds;
    public AudioClip[] sprintSounds;
}
