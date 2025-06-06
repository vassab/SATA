using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class DeckManager : MonoBehaviour
{
    [Header("Card Settings")]
    [Tooltip("Paths to folders in Resources where cards are stored")]
    [SerializeField] private List<string> cardResourcePaths = new List<string>();

    [Header("Debug")]
    [SerializeField] private bool logCardLoading = true;

    private Dictionary<CardType, List<CardData>> cardsByType = new Dictionary<CardType, List<CardData>>();
    private Dictionary<string, CardData> allCards = new Dictionary<string, CardData>();
    public static DeckManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Для сохранения между сценами
        InitializeDecks();
    }

    private void InitializeDecks()
    {
        cardsByType.Clear();
        allCards.Clear();

        // Инициализация пустых колод для всех типов
        foreach (CardType type in System.Enum.GetValues(typeof(CardType)))
        {
            cardsByType[type] = new List<CardData>();
        }

        // Загрузка карт
        LoadAllCards();

        if (logCardLoading)
        {
            Debug.Log($"DeckManager initialized. Loaded {allCards.Count} total cards.");
            foreach (var pair in cardsByType)
            {
                Debug.Log($"Type {pair.Key}: {pair.Value.Count} cards");
            }
        }
    }

    private void LoadAllCards()
    {
        foreach (string path in cardResourcePaths)
        {
            if (string.IsNullOrEmpty(path)) continue;

            CardData[] loadedCards = Resources.LoadAll<CardData>(path);
            foreach (CardData card in loadedCards)
            {
                if (card == null) continue;

                // Добавляем в общий словарь
                if (!allCards.ContainsKey(card.cardId))
                {
                    allCards.Add(card.cardId, card);
                }

                // Добавляем в соответствующую колоду
                if (cardsByType.ContainsKey(card.cardType))
                {
                    cardsByType[card.cardType].Add(card);
                }
                else
                {
                    Debug.LogWarning($"Card {card.cardName} has unsupported type: {card.cardType}");
                }
            }
        }
    }

    public CardData GetRandomCard(CardType type)
    {
        if (!cardsByType.ContainsKey(type) || cardsByType[type].Count == 0)
        {
            Debug.LogWarning($"No cards available for type: {type}");
            return null;
        }

        return cardsByType[type][Random.Range(0, cardsByType[type].Count)];
    }

    public CardData GetCardById(string cardId)
    {
        if (allCards.TryGetValue(cardId, out CardData card))
        {
            return card;
        }
        return null;
    }

    public List<CardData> GetShuffledDeck(CardType type, int count = -1)
    {
        if (!cardsByType.ContainsKey(type)) return new List<CardData>();

        List<CardData> deck = new List<CardData>(cardsByType[type]);
        Shuffle(deck);

        if (count > 0 && count < deck.Count)
        {
            return deck.GetRange(0, count);
        }

        return deck;
    }
    public CardData GetRandomCardByType(CardType type)
    {
        return GetRandomCard(type); // Просто вызывает существующий метод
    }

    public List<CardData> GetCardsByType(CardType type)
    {
        if (cardsByType.ContainsKey(type))
        {
            return new List<CardData>(cardsByType[type]);
        }
        return new List<CardData>();
    }

    private void Shuffle<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }

    #region Editor Helpers
#if UNITY_EDITOR
    public void ReloadCardsInEditor()
    {
        InitializeDecks();
        Debug.Log("Cards reloaded in editor");
    }
#endif
    #endregion
}