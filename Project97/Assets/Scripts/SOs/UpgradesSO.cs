using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(menuName = "ScriptableObjects/Upgrades")]
public class UpgradesSO : ScriptableObject
{
    public List<AttackSO> aSOs;
    public bool allDefendAvailable = true; //When false, this means only first 4 defensive are available 
    public bool statsUpgradesPossible = true;
}
