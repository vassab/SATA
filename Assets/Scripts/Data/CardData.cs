using UnityEngine;

// Make sure this enum is defined, ideally in this file or a globally accessible one.
public enum CardType
{
    Profession,
    Hobby,
    Health,
    Baggage,
    Facts,
    Biology,
    // Add any other card types you need
    Generic // A default or placeholder type if needed
}

[CreateAssetMenu(fileName = "New CardData", menuName = "Card Game/Card Data")]
public class CardData : ScriptableObject
{
    [Header("General Card Info")]
    public string cardId; // Unique identifier for the card
    public string cardName;
    [TextArea(3, 5)]
    public string description;
    public Sprite cardImage;
    public CardType cardType; // This is the primary field for card type

    /*[Header("Type-Specific Data")]
    // These fields are only relevant for specific card types.
    // You can use the Inspector to fill them out only when cardType matches.

    // For Health cards
    [Tooltip("Relevant only if cardType is Health")]
    public string healthCondition;

    // For Baggage cards
    [Tooltip("Relevant only if cardType is Baggage")]
    public string baggageItem;

    // For Facts cards
    [Tooltip("Relevant only if cardType is Facts")]
    public string fact;

    // For Biology cards (if it has specific data, e.g., biological trait)
    // public string biologicalTrait; 

    // For Profession cards (if it has specific data, e.g., salary, skill)
    // public string professionSkill;

    // For Hobby cards (if it has specific data, e.g., equipment needed)
    // public string hobbyEquipment;

    // Note: The GameManager and CardDisplay scripts will need to correctly
    // access these fields based on the cardType.
    // The CardDisplay_Interactive.cs script already has logic to show/hide
    // text fields based on cardType.*/
}

