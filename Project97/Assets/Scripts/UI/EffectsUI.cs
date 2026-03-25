using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EffectsUI : MonoBehaviour
{
    private Character character;
    private Transform effectsContainerTransform;
    void Start()
    {
        character.OnEffectsChanged += UpdateEffectsDisplay;
    }
    public void Setup(Character character, Transform effectsContainerTransform)
    {
        this.character = character;
        this.effectsContainerTransform = effectsContainerTransform;
    }
    public void UpdateEffectsDisplay(Dictionary<Effect, EffectData> currentEffects)
    {
        CleanupEffects();

        foreach (var kvp in currentEffects)
        {
            Effect effect = kvp.Key;
            EffectData data = kvp.Value;

            GameObject item = Instantiate(AssetsDatabase.I.effectItemPf, effectsContainerTransform);

            item.transform.Find("text").GetComponent<TextMeshProUGUI>().SetText($"{effect}\nDuration: {data.duration}");

            if (data.sprite != null) item.transform.Find("image").GetComponent<Image>().sprite = data.sprite; 
        }
    }
    void OnDestroy()
    {
        CleanupEffects();
        if(character != null)
        {
            character.OnEffectsChanged -= UpdateEffectsDisplay;
        }
    }

    private void CleanupEffects()
    {
        if (effectsContainerTransform == null) return;
        foreach (Transform child in effectsContainerTransform)
        {
            Destroy(child.gameObject);
        }
    }
}
