using System.Collections.Generic;
using System.Linq;

public class Player
{
    public string Name { get; set; }
    public bool IsActive { get; set; } = true;
    public int Votes { get; private set; }
    private List<CardData> hand = new List<CardData>();
    public string Id { get; private set; }

    public Player(string name)
    {
        Name = name;
        Votes = 0;
    }

    public Player(string id, string name)
    {
        Id = id;
        Name = name;
        Votes = 0;
    }


    public void AddCard(CardData card)
    {
        if (card != null)
        {
            hand.Add(card);
        }
    }

    public bool RemoveCard(CardData card)
    {
        return hand.Remove(card);
    }

    public List<CardData> GetHand()
    {
        return new List<CardData>(hand);
    }

    public int HandCount => hand.Count;

    public void AddVote()
    {
        Votes++;
    }

    public void ResetVotes()
    {
        Votes = 0;
    }

    public bool HasCard(CardData card)
    {
        return hand.Contains(card);
    }

    public void ClearHand()
    {
        hand.Clear();
    }

    public override string ToString()
    {
        return $"{Name} (Cards: {HandCount}, Votes: {Votes}, Active: {IsActive})";
    }

    public List<CardData> GetCardsOfType(CardType type)
    {
        return hand.Where(c => c.cardType == type).ToList();
    }
}
