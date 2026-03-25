using System.Collections.Generic;
using UnityEngine;

public class AssetsDatabase : MonoBehaviour
{
    public static AssetsDatabase I; //Instance
    void Awake()
    {
        I = this;
    }
    public List<AttackSO> aMoves;
    public List<DefendSO> dMoves;
    public GameObject characterPf;
    public DefendSO defaultDefendSO;
    public List<ItemSO> items;
    public List<UpgradesSO> upgradesSOs;
    public CharacterSO pCSO;
    public List<Sprite> effectsSprites;
    public GameObject effectItemPf;
}
