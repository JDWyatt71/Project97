using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class APBarUI : MonoBehaviour
{
    private SelectMoveUI selectMoveUI;
    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI apAmountText;
    public void Setup(SelectMoveUI selectMoveUI)
    {
        this.selectMoveUI = selectMoveUI;

        UpdateAPBar(selectMoveUI.GetCurrentAP());
        selectMoveUI.APChanged += UpdateAPBar;
    }

    private void UpdateAPBar(int current)
    {
        int max = GameManager.I.pC.actionPoints;
        slider.value = (float)current / (float)max;
        apAmountText.text = $"{current} / {max}";
    }
}
