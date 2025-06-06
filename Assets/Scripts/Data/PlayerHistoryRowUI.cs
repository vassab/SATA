using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

public class PlayerHistoryRowUI : MonoBehaviour
{
    public TextMeshProUGUI playerNameText;
    // You'll need to create a mapping for CardType to its display TextMeshProUGUI element
    // This could be a list of TextMeshProUGUI elements in the same order as displayableCardTypes
    // or a Dictionary<CardType, TextMeshProUGUI> that you populate.
    // For simplicity, let's assume a list that corresponds to the order in displayableCardTypes.
    public List<TextMeshProUGUI> cardDescriptionTexts; // Assign these in the Inspector for the prefab

    public void Setup(Player player, Dictionary<CardType, List<CardData>> revealedCardsForPlayer, List<CardType> displayOrder)
    {
        if (playerNameText != null) playerNameText.text = player.Name;

        if (cardDescriptionTexts == null || cardDescriptionTexts.Count != displayOrder.Count)
        {
            Debug.LogError("cardDescriptionTexts list in PlayerHistoryRowUI is not configured correctly or its size doesn't match displayOrder.");
            return;
        }

        for (int i = 0; i < displayOrder.Count; i++)
        {
            CardType currentType = displayOrder[i];
            TextMeshProUGUI descriptionTextComponent = cardDescriptionTexts[i];
            string description = "-"; // Default if no card of this type is revealed

            if (revealedCardsForPlayer != null && revealedCardsForPlayer.ContainsKey(currentType))
            {
                List<CardData> cardsOfType = revealedCardsForPlayer[currentType];
                if (cardsOfType != null && cardsOfType.Count > 0)
                {
                    // For now, display the description of the first card of this type.
                    // You can expand this later to show multiple or allow selection.
                    description = cardsOfType.First().description;
                }
            }
            if (descriptionTextComponent != null) descriptionTextComponent.text = description;
        }
    }
}
