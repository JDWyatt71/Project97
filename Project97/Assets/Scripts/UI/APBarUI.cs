using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class APBarUI : MonoBehaviour
{
    private SelectMoveUI selectMoveUI;
    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI apAmountText;
    private int max;
    public void Setup(SelectMoveUI selectMoveUI)
    {
        this.selectMoveUI = selectMoveUI;
        max = GameManager.I.pC.actionPoints;
        UpdateAPBar(selectMoveUI.GetCurrentAP());
        selectMoveUI.APChanged += UpdateAPBar;
    }

    private void UpdateAPBar(int current)
    {
        slider.value = (float)current / (float)max;
        apAmountText.text = $"{current} / {max}";
    }
}
