using System.Collections;
using UnityEngine;
using UnityEngine.UI;


public class AccessibilitySettings : MonoBehaviour
{
    [SerializeField] private Image brightnessOverlay;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        float savedBrightness = PlayerPrefs.GetFloat("brightness", 0.3f);
        SetBrightness(savedBrightness);
    }

    public void SetBrightness(float brightness)
    {
        Debug.Log("Brightness: " + brightness);
        Color colour = brightnessOverlay.color;
        colour.a = brightness;
        brightnessOverlay.color = colour;
        PlayerPrefs.SetFloat("brightness", brightness);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
