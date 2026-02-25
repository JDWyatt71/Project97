using System;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    private HealthSystem healthSystem;
    [SerializeField] private Slider slider;

    public void Setup(HealthSystem healthSystem)
    {
        this.healthSystem = healthSystem;
        UpdateHealthBar(healthSystem.GetHealth(), healthSystem.GetMaxHealth());
        healthSystem.HealthChanged += UpdateHealthBar;


    }

    private void UpdateHealthBar(int current, int max)
    {
        slider.value = (float)current / (float)max;
    }
}
