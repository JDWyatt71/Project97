using TMPro;
using UnityEngine;

public class EndScreenUI : MonoBehaviour
{
    [SerializeField] private GameObject endScreenObj;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private TextMeshProUGUI analyticsText;


    public void DisplayEndScreen(string result, int round, int attackAttempt, int attackSuccess, int defendAttempt, int defendSuccess, int hpLeft, float runStartTime)
    {
        endScreenObj.SetActive(true);
        resultText.text = result;

        string status = result == "Defeat" ? $"Finished at Round {round}" : $"Completed all {round} Rounds";
        string analytics = $"{status}\nAttacks Successful: {attackSuccess}/{attackAttempt}\nDefends Successful: {defendSuccess}/{defendAttempt}\nHP Left: {hpLeft}\nPlaytime Duration: {Mathf.FloorToInt(Time.time - runStartTime)}s";
        analyticsText.text = analytics;
    }
}
