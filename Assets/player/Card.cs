using UnityEngine;

public abstract class Card : ScriptableObject
{
    public string cardName;
    public string description;

    // Method to apply the card effect to a unit
    public abstract void ApplyEffect(Unit unit);
}
