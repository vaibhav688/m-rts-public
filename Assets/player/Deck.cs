using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Deck : MonoBehaviour
{
    // Start is called before the first frame update


    
    [SerializeField]
    private List<cardstats> cards;

    [SerializeField]
    private List<cardstats> hand;
    [SerializeField]

    private cardstats nextCard;
    public List<cardstats> Cards{
        get {return cards;}
       

    }
    public List<cardstats> Hand{
        get {return hand;}
       

    }
    public cardstats NextCard{
        get {return nextCard;}
        set {nextCard = value;}
    
    }
    public void Start() 
    {
        nextCard=cards[0];


    }
    public cardstats DrawCard()
    {
        cardstats cs= nextCard;
        hand.Add(nextCard);
        cards.Remove(nextCard);
        nextCard=cards[0];

        return cs;

    }
    public void RemoveHand(int index)
    {
        foreach (cardstats cs in hand)
        {
            if(cs.Index==index)
            {
                hand.Remove(cs);
                cards.Add(cs);
                break;
            }
            
        }

    }


}
