using UnityEngine;
using UnityEngine.UI;

public class SettingsUITelemetry : MonoBehaviour
{
    [SerializeField] private Toggle TelemetryToggle;

    private void Start()
    {
        // Set initial state
        TelemetryToggle.isOn = TelemetryConsentManager.IsEnabled();

        // Listen for changes
        TelemetryToggle.onValueChanged.AddListener(OnTelemetryChanged);
    }

    private void OnTelemetryChanged(bool enabled)
    {
        Debug.Log($"Telemetry: {enabled}");
        TelemetryConsentManager.SetTelemetry(enabled);
    }
}