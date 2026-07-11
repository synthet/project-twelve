using System;
using ProjectTwelve.Sandbox.Registry;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SandboxPlayerVitals : MonoBehaviour
{
    private const string PlayerEntityId = "core:player";

    [SerializeField] private int currentHealth;
    [SerializeField] private int maxHealth;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    public event Action<int, int> Changed;

    private void Awake()
    {
        int registeredMaximum = SandboxRegistries.Entities.Get(PlayerEntityId).MaxHealth;
        maxHealth = Mathf.Max(1, registeredMaximum);
        currentHealth = maxHealth;
    }

    public void ApplyDamage(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        SetCurrentHealth(currentHealth - amount);
    }

    public void Heal(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        SetCurrentHealth(currentHealth + amount);
    }

    public void RestoreFull()
    {
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
