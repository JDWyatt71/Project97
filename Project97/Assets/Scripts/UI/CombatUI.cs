using UnityEngine;
using TMPro;

public class CombatUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI combatLogText;
    [SerializeField] private TextMeshProUGUI defendingMovesText;

    [SerializeField] private GameObject damagePopup;
    [SerializeField] private Transform playerDamagePopupSpawn; // Spawn point for player damage popups
    [SerializeField] private Transform enemyDamagePopupSpawn; // Spawn point for enemy damage popups

    private void OnEnable()
    {
        CombatEvents.OnLogUpdate += UpdateCombatLog;
        CombatEvents.OnDamageDealt += ShowDamagePopup;
        CombatEvents.DefendingMovesSelected += DisplayDefendingMoves;
    }

    private void OnDisable()
    {
        CombatEvents.OnLogUpdate -= UpdateCombatLog;
        CombatEvents.OnDamageDealt -= ShowDamagePopup;
        CombatEvents.DefendingMovesSelected -= DisplayDefendingMoves;
    }

    private void UpdateCombatLog(string message)
    {
        combatLogText.text += "\n" + message + "\n";
    }

    private void ClearCombatLog()
    {
        combatLogText.text = "";
    }
    private void DisplayDefendingMoves(string message)
    {
        ClearCombatLog();
        defendingMovesText.text = message;
    }

    private void ShowDamagePopup(int damageAmount, Character target)
    {
        Transform spawnPoint = target == GameManager.I.pC ? playerDamagePopupSpawn : enemyDamagePopupSpawn;

        GameObject popup = Instantiate(damagePopup, spawnPoint);
        popup.GetComponent<DamageNumber>().Setup(damageAmount);
    }
}
