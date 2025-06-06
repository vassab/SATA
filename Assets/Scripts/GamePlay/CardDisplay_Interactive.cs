using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class CardDisplay_Interactive : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("UI Elements")]
    public TextMeshProUGUI cardNameText;
    public TextMeshProUGUI cardDescriptionText;
    public Image cardImageUI;
    public TextMeshProUGUI cardTypeText;

    [Header("Settings")]
    public float hoverScale = 1.1f;

    private CardData currentCard;
    private Vector3 originalScale;
    private Player owner;

    void Awake()
    {
        originalScale = transform.localScale;
    }

    public void Initialize(CardData card, Player player)
    {
        currentCard = card;
        owner = player;
        UpdateDisplay();
    }

    void UpdateDisplay()
    {
        if (currentCard == null) return;

        cardNameText.text = currentCard.cardName;
        cardDescriptionText.text = currentCard.description;
        //cardTypeText.text = currentCard.cardType.ToString();

        if (currentCard.cardImage != null)
        {
            cardImageUI.sprite = currentCard.cardImage;
            cardImageUI.enabled = true;
        }
        else
        {
            cardImageUI.enabled = false;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (IsInteractable())
        {
            transform.localScale = originalScale * hoverScale;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = originalScale;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && IsInteractable())
        {
            GameManager.Instance.OnCardSelected(this);
            GameManager.Instance.EndTurn(); // Добавляем переход к следующему ходу
        }
    }

    private bool IsInteractable()
    {
        return owner != null &&
               GameManager.Instance != null &&
               GameManager.Instance.GetCurrentPlayer()?.Name == owner?.Name;
    }

    public CardData GetCardData() => currentCard;
    public Player GetOwner() => owner;
}