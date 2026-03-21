[System.Serializable]
public class AttackChance
{
    public AttackSO attackSO;
    public MoveWeight weight;
    public AttackChance(AttackSO attackSO, MoveWeight weight)
    {
        this.attackSO = attackSO;
        this.weight = weight;
    }
}