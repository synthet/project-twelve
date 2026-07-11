using System;
using ProjectTwelve.Sandbox.Registry;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SandboxPlayerVitals : MonoBehaviour
{
    private const string PlayerEntityId = "core:player";

    [SerializeField] private int currentHealth;
    [SerializeField] private int maxHealth;

    public int CurrentHealth
    {
        get
        {
            EnsureInitialized();
            return currentHealth;
        }
    }

    public int MaxHealth
    {
        get
        {
            EnsureInitialized();
            return maxHealth;
        }
    }

    public event Action<int, int> Changed;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        if (maxHealth > 0)
        {
            return;
        }

        int registeredMaximum = SandboxRegistries.Entities.Get(PlayerEntityId).MaxHealth;
        maxHealth = Mathf.Max(1, registeredMaximum);
        currentHealth = maxHealth;
    }

    public void ApplyDamage(int amount)
    {
        EnsureInitialized();
        if (amount <= 0)
        {
            return;
        }

        SetCurrentHealth(currentHealth - amount);
    }

    public void Heal(int amount)
    {
        EnsureInitialized();
        if (amount <= 0)
        {
            return;
        }

        SetCurrentHealth(currentHealth + amount);
    }

    public void RestoreFull()
    {
        EnsureInitialized();
        SetCurrentHealth(maxHealth);
    }

    private void SetCurrentHealth(int value)
    {
        int clamped = Mathf.Clamp(value, 0, maxHealth);
        if (currentHealth == clamped)
        {
            return;
        }

        currentHealth = clamped;
        Changed?.Invoke(currentHealth, maxHealth);
    }
}
