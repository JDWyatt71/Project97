using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/Attack")]
public class AttackSO : MoveSO
{
   public Scale damage;
   public Accuracy accuracy;
   public bool ignoresGuard = false;
   public bool catchesDodge = false;
   public Scale catchesDodgeChance;
   
}
