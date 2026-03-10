using UnityEngine;
using TMPro;

public class CombatUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI combatLogText;
    [SerializeField] private GameObject damagePopup;
    [SerializeField] private Transform playerDamagePopupSpawn; // Spawn point for player damage popups
    [SerializeField] private Transform enemyDamagePopupSpawn; // Spawn point for enemy damage popups

    private Character player;

    public void Setup(Character newCharacter)
    {
        player = newCharacter;
    }

    private void OnEnable()
    {
        CombatEvents.OnLogUpdate += UpdateCombatLog;
        CombatEvents.OnDamageDealt += ShowDamagePopup;
    }

    private void OnDisable()
    {
        CombatEvents.OnLogUpdate -= UpdateCombatLog;
        CombatEvents.OnDamageDealt -= ShowDamagePopup;
    }

    private void UpdateCombatLog(string message)
    {
        combatLogText.text += "\n" + message;
    }

    private void ShowDamagePopup(int damageAmount, Character target)
    {
        Transform spawnPoint;

        if (target == player)
        {
            spawnPoint = playerDamagePopupSpawn;
        }
        else
        {
            spawnPoint = enemyDamagePopupSpawn;
        }

        GameObject popup = Instantiate(damagePopup, spawnPoint.position, Quaternion.identity);
        popup.GetComponentInChildren<TextMeshProUGUI>().text = damageAmount.ToString();
    }
}
