using UnityEngine;

[CreateAssetMenu(fileName = "AttackBoostCard", menuName = "Card/AttackBoostCard")]
public class AttackBoostCard : Card
{
    public float attackBoostPercentage = 1.5f; // 50% attack boost

    public override void ApplyEffect(Unit unit)
    {
        unit.attackDamage *= attackBoostPercentage;
        Debug.Log($"Attack of unit {unit.gameObject.name} boosted by {attackBoostPercentage * 100}%");
    }
}
