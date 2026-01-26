using UnityEngine;

public class GameManager : MonoBehaviour
{
    void Start()
    {
        GameObject pCharacter = Instantiate(AssetsDatabase.I.characterPf);
        GameObject cCharacter = Instantiate(AssetsDatabase.I.characterPf);
        TurnManager turnManager = gameObject.AddComponent<TurnManager>();
        turnManager.Setup(pCharacter.GetComponent<Character>(), cCharacter.GetComponent<Character>());

    }

}
