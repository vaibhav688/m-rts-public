using UnityEngine;

[CreateAssetMenu(fileName = "HPTBoostCard", menuName = "Card/HPTBoostCard")]
public class HPTBoostCard : Card
{
    public float healthBoostAmount = 20f;

    public override void ApplyEffect(Unit unit)
    {
        unit.health += healthBoostAmount;
        Debug.Log($"Health of unit {unit.gameObject.name} increased by {healthBoostAmount}");
    }
}
