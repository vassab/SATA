using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;

public class HistoryPanelUI : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    public GameObject panelRoot; // The root GameObject of the history panel
    public Transform playerRowsContainer; // The parent Transform for player history rows
    public GameObject playerHistoryRowPrefab; // Prefab for a single player's history row
    public Button closeButton; // Button to close the panel

    [Header("Configuration")]
    // Define the order and types of cards to display as columns
    public List<CardType> displayableCardTypes = new List<CardType>
    {
        CardType.Profession,
        CardType.Biology,
        CardType.Health,
        CardType.Hobby,
        CardType.Facts,
        CardType.Baggage
    };

    private PhotonView photonView;

    void Awake()
    {
        photonView = GetComponent<PhotonView>();
        if (photonView == null)
        {
            Debug.LogError("HistoryPanelUI: PhotonView component is missing!");
        }
    }

    void Start()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false); // Ensure panel is hidden at start
        }
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(HidePanel);
        }
    }

    public void TogglePanel()
    {
        if (panelRoot == null) return;

        bool isActive = !panelRoot.activeSelf;
        panelRoot.SetActive(isActive);

        if (isActive)
        {
            PopulateHistory();
        }
    }

    public void ShowPanel()
    {
        if (panelRoot == null) return;
        panelRoot.SetActive(true);
        PopulateHistory();
    }

    public void HidePanel()
    {
        if (panelRoot == null) return;
        panelRoot.SetActive(false);
    }

    private void PopulateHistory()
    {
        if (GameManager.Instance == null || playerRowsContainer == null || playerHistoryRowPrefab == null)
        {
            Debug.LogError("HistoryPanelUI is not properly configured or GameManager is missing.");
            return;
        }

        // 1. Clear previous entries
        foreach (Transform child in playerRowsContainer)
        {
            Destroy(child.gameObject);
        }

        // 2. Get data from GameManager
        Dictionary<Player, Dictionary<CardType, List<CardData>>> historyData = GameManager.Instance.GetRevealedCardsHistory();
        List<Player> players = GameManager.Instance.GetActivePlayers();

        if (historyData == null || players == null || players.Count == 0)
        {
            Debug.LogError("Failed to retrieve history data or player list.");
            return;
        }

        // 3. Iterate through players and populate rows
        foreach (Player player in players)
        {
            if (player == null)
            {
                Debug.LogError("HistoryPanelUI: Null player in players list!");
                continue;
            }

            GameObject rowInstance = Instantiate(playerHistoryRowPrefab, playerRowsContainer);
            PlayerHistoryRowUI rowUI = rowInstance.GetComponent<PlayerHistoryRowUI>();

            if (rowUI != null)
            {
                rowUI.Setup(player, historyData.ContainsKey(player) ? historyData[player] : null, displayableCardTypes);
            }
            else
            {
                Debug.LogError($"PlayerHistoryRow_Prefab does not have a PlayerHistoryRowUI script attached to its root for player {player.Name}");

                // Fallback: Try to find TextMeshPro components by name if PlayerHistoryRowUI is missing
                Transform playerNameTextTransform = rowInstance.transform.Find("PlayerNameText");
                if (playerNameTextTransform != null)
                {
                    TextMeshProUGUI playerNameText = playerNameTextTransform.GetComponent<TextMeshProUGUI>();
                    if (playerNameText != null) playerNameText.text = player.Name;
                }

                Dictionary<CardType, List<CardData>> playerRevealedCards =
                    historyData.ContainsKey(player) ? historyData[player] : new Dictionary<CardType, List<CardData>>();
            }
        }
    }

    // ћетод дл€ обновлени€ истории при получении новых данных через сеть
    public void UpdateHistory()
    {
        if (panelRoot != null && panelRoot.activeSelf)
        {
            PopulateHistory();
        }
    }

    // ќбработчик событи€ обновлени€ истории карт
    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        // ≈сли среди обновленных свойств есть истори€ карт, обновл€ем панель
        if (propertiesThatChanged.ContainsKey("CardHistory"))
        {
            UpdateHistory();
        }
    }
}
