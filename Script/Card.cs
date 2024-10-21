using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Card
{
    public RPS Type { get; private set; }
    public int cardID;

    public Card(RPS type, int id)
    {
        Type = type;
        cardID = id;
    }

    public override bool Equals(object obj)
    {
        if (obj is Card otherCard)
        {
            return cardID == otherCard.cardID;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return Type.GetHashCode();
    }
}
