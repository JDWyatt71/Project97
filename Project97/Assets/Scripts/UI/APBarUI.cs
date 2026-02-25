using System;
using UnityEngine;
using UnityEngine.UI;

public class APBarUI : MonoBehaviour
{
    private SelectMoveUI selectMoveUI;
    [SerializeField] private Slider slider;
    private int max;
    public void Setup(SelectMoveUI selectMoveUI)
    {
        this.selectMoveUI = selectMoveUI;
        max = GameManager.I.pCharacter.GetComponent<Character>().actionPoints;
        UpdateAPBar(selectMoveUI.GetCurrentAP());
        selectMoveUI.APChanged += UpdateAPBar;
    }

    private void UpdateAPBar(int current)
    {
        slider.value = (float)current / (float)max;
    }
}
