using System;
using UnityEngine;
using UnityEngine.UI;

public class APBarUI : MonoBehaviour
{
    private TurnManager turnManager;
    [SerializeField] private Slider slider;

    public void Setup(TurnManager turnManager)
    {
        this.turnManager = turnManager;
        UpdateAPBar(turnManager.GetCurrentAP(), turnManager.GetMaxAP());
        turnManager.APChanged += UpdateAPBar;


    }

    private void UpdateAPBar(int current, int max)
    {
        slider.value = (float)current / (float)max;
    }
}
