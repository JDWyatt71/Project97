using UnityEngine;
using Unity.Services.Analytics;
using UnityEngine.UnityConsent;

public static class TelemetryConsentManager
{
    private const string TELEMETRY_KEY = "telemetry_enabled";

    public static bool IsEnabled()
    {
        return PlayerPrefs.GetInt(TELEMETRY_KEY, 1) == 1; // default ON
    }

    public static void SetTelemetry(bool enabled)
    {
        PlayerPrefs.SetInt(TELEMETRY_KEY, enabled ? 1 : 0);
        PlayerPrefs.Save();

        ApplyConsent(enabled);
    }

    public static void ApplyConsent(bool enabled)
    {
        EndUserConsent.SetConsentState(new ConsentState
        {
            AnalyticsIntent = enabled ? ConsentStatus.Granted : ConsentStatus.Denied,
            AdsIntent = ConsentStatus.Denied
        });

        Debug.Log("Telemetry Enabled: " + enabled);
    }
}