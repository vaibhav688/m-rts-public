using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// ICard.cs
public interface ICard
{
    string CardName { get; }
    string Description { get; }

    void ApplyEffect(Unit unit);
}

