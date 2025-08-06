using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;
    
    [Header("UI Elements")]
    public Slider healthBar;
    public Text healthText;
    
    [Header("Damage Settings")]
    public float invulnerabilityDuration = 1f;
    private bool isInvulnerable = false;
    private float invulnerabilityTimer = 0f;
    
    [Header("Effects")]
    public AudioClip damageSound;
    public AudioClip deathSound;
    private AudioSource audioSource;
    
    // Events
    public UnityEngine.Events.UnityEvent OnPlayerDamaged;
    public UnityEngine.Events.UnityEvent OnPlayerDied;
    
    void Start()
    {
        currentHealth = maxHealth;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        UpdateHealthUI();
    }
    
    void Update()
    {
        if (isInvulnerable)
        {
            invulnerabilityTimer -= Time.deltaTime;
            if (invulnerabilityTimer <= 0f)
            {
                isInvulnerable = false;
            }
        }
    }
    
    public void TakeDamage(float damage)
    {
        if (isInvulnerable || currentHealth <= 0f)
            return;
            
        currentHealth = Mathf.Max(0f, currentHealth - damage);
        
        // Play damage sound
        if (damageSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(damageSound);
        }
        
        // Start invulnerability period
        isInvulnerable = true;
        invulnerabilityTimer = invulnerabilityDuration;
        
        // Invoke damage event
        OnPlayerDamaged?.Invoke();
        
        // Update UI
        UpdateHealthUI();
        
        // Check for death
        if (currentHealth <= 0f)
        {
            Die();
        }
    }
    
    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        UpdateHealthUI();
    }
    
    private void Die()
    {
        if (deathSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(deathSound);
        }
        
        OnPlayerDied?.Invoke();
        
        // Disable player movement
        FirstPersonController playerController = GetComponent<FirstPersonController>();
        if (playerController != null)
        {
            playerController.playerCanMove = false;
        }
        
        Debug.Log("Player died!");
    }
    
    private void UpdateHealthUI()
    {
        if (healthBar != null)
        {
            healthBar.value = currentHealth / maxHealth;
        }
        
        if (healthText != null)
        {
            healthText.text = $"{currentHealth:F0}/{maxHealth:F0}";
        }
    }
    
    public bool IsAlive()
    {
        return currentHealth > 0f;
    }
    
    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }
}