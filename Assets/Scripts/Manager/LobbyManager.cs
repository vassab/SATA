using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    [Header("UI Canvases/Panels")]
    public GameObject mainMenuCanvas;
    public GameObject lobbyCanvas;
    public GameObject gameCanvas;

    [Header("Lobby UI Elements")]
    public GameObject playerListContent;
    public GameObject playerListItemPrefab;
    public Button leaveLobbyButton;
    public Button startGameButton;
    public Button gameSettingsButton;
    public Button readyButton;
    public TextMeshProUGUI lobbyStatusText;
    public TextMeshProUGUI roomCodeText;

    [Header("Host Game Settings Panel UI")]
    public GameObject gameSettingsPanel;
    public Slider votingTimeSlider;
    public TextMeshProUGUI votingTimeValueText;
    public Slider cardRevealTimeSlider;
    public TextMeshProUGUI cardRevealTimeValueText;
    public Slider discussionTimeSlider;
    public TextMeshProUGUI discussionTimeValueText;
    public Button applySettingsButton;
    public Button closeSettingsPanelButton;

    private bool localPlayerIsReady = false;
    private float votingTime = 30f;
    private float cardRevealTime = 10f;
    private float discussionTime = 60f;

    // GameManager reference to start the game logic
    public GameManager gameManager;

    // Этот метод вызывается из MainMenuManager при переходе в лобби
    public void InitializeLobby()
    {
        // Активируем панель лобби
        if (lobbyCanvas != null) lobbyCanvas.SetActive(true);
        if (mainMenuCanvas != null) mainMenuCanvas.SetActive(false);
        if (gameCanvas != null) gameCanvas.SetActive(false);

        SetupLobbyUI();
        AddButtonListeners();

        if (gameSettingsPanel != null) gameSettingsPanel.SetActive(false);

        // Обновляем список игроков
        RefreshPlayerListUI();

        // Если мы хост, инициализируем настройки
        if (PhotonNetwork.IsMasterClient && gameSettingsPanel != null)
        {
            InitializeSettingsSliders();
        }

        // Показываем код комнаты
        if (roomCodeText != null && PhotonNetwork.CurrentRoom != null)
        {
            roomCodeText.text = "Room code: " + PhotonNetwork.CurrentRoom.Name;
        }
    }

    void Awake()
    {
        // Находим GameManager, если не назначен
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }
    }

    void SetupLobbyUI()
    {
        bool isHost = PhotonNetwork.IsMasterClient;

        if (startGameButton != null) startGameButton.gameObject.SetActive(isHost);
        if (gameSettingsButton != null) gameSettingsButton.gameObject.SetActive(isHost);
        if (readyButton != null && readyButton.GetComponentInChildren<TextMeshProUGUI>() != null)
        {
            readyButton.GetComponentInChildren<TextMeshProUGUI>().text = "Ready";
        }

        if (lobbyStatusText != null)
        {
            if (isHost)
            {
                lobbyStatusText.text = "Waiting for players";
            }
            else
            {
                lobbyStatusText.text = "Connected to the lobby";
            }
        }
    }

    void AddButtonListeners()
    {
        // Удаляем существующие слушатели перед добавлением новых
        if (leaveLobbyButton != null) { leaveLobbyButton.onClick.RemoveAllListeners(); leaveLobbyButton.onClick.AddListener(OnLeaveLobbyClicked); }
        if (startGameButton != null && PhotonNetwork.IsMasterClient) { startGameButton.onClick.RemoveAllListeners(); startGameButton.onClick.AddListener(OnStartGameClicked); }
        if (gameSettingsButton != null && PhotonNetwork.IsMasterClient) { gameSettingsButton.onClick.RemoveAllListeners(); gameSettingsButton.onClick.AddListener(OnGameSettingsClicked); }
        if (readyButton != null) { readyButton.onClick.RemoveAllListeners(); readyButton.onClick.AddListener(OnReadyClicked); }

        if (PhotonNetwork.IsMasterClient && gameSettingsPanel != null)
        {
            if (applySettingsButton != null) { applySettingsButton.onClick.RemoveAllListeners(); applySettingsButton.onClick.AddListener(OnApplySettingsClicked); }
            if (closeSettingsPanelButton != null) { closeSettingsPanelButton.onClick.RemoveAllListeners(); closeSettingsPanelButton.onClick.AddListener(OnCloseSettingsPanelClicked); }

            if (votingTimeSlider != null) { votingTimeSlider.onValueChanged.RemoveAllListeners(); votingTimeSlider.onValueChanged.AddListener(UpdateVotingTimeText); }
            if (cardRevealTimeSlider != null) { cardRevealTimeSlider.onValueChanged.RemoveAllListeners(); cardRevealTimeSlider.onValueChanged.AddListener(UpdateCardRevealTimeText); }
            if (discussionTimeSlider != null) { discussionTimeSlider.onValueChanged.RemoveAllListeners(); discussionTimeSlider.onValueChanged.AddListener(UpdateDiscussionTimeText); }
        }
    }

    void OnLeaveLobbyClicked()
    {
        Debug.Log("Leave Lobby button clicked.");

        // Выходим из комнаты Photon
        PhotonNetwork.LeaveRoom();
    }

    void OnStartGameClicked()
    {
        Debug.Log("Start Game button clicked by Host.");

        // Проверяем, что все игроки готовы
        if (!AreAllPlayersReady())
        {
            if (lobbyStatusText != null)
            {
                lobbyStatusText.text = "Не все игроки готовы!";
            }
            return;
        }

        // Сохраняем настройки игры
        PlayerPrefs.SetFloat("VotingTime", votingTime);
        PlayerPrefs.SetFloat("CardRevealTime", cardRevealTime);
        PlayerPrefs.SetFloat("DiscussionTime", discussionTime);
        PlayerPrefs.Save();

        // Синхронизируем настройки игры через свойства комнаты
        SyncGameSettings();

        // Закрываем комнату для новых игроков
        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;

        // Вместо загрузки новой сцены, переключаем панели и инициализируем игру
        photonView.RPC("StartGameOnAllClients", RpcTarget.All);
    }

    [PunRPC]
    void StartGameOnAllClients()
    {
        // Переключаем панели UI
        if (lobbyCanvas != null) lobbyCanvas.SetActive(false);
        if (gameCanvas != null) gameCanvas.SetActive(true);

        // Инициализируем игру, если мы мастер-клиент
        if (PhotonNetwork.IsMasterClient && gameManager != null)
        {
            gameManager.InitializeGame();
        }
    }

    void OnGameSettingsClicked()
    {
        if (gameSettingsPanel != null)
        {
            gameSettingsPanel.SetActive(true);
            InitializeSettingsSliders();
        }
    }

    void OnReadyClicked()
    {
        localPlayerIsReady = !localPlayerIsReady;
        Debug.Log("Player ready status: " + localPlayerIsReady);

        // Обновляем текст кнопки
        if (readyButton != null && readyButton.GetComponentInChildren<TextMeshProUGUI>() != null)
        {
            readyButton.GetComponentInChildren<TextMeshProUGUI>().text = localPlayerIsReady ? "Не готов" : "Готов";
        }

        // Синхронизируем статус готовности через свойства игрока
        Hashtable props = new Hashtable();
        props["IsReady"] = localPlayerIsReady;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        // Обновляем UI списка игроков
        RefreshPlayerListUI();
    }

    void InitializeSettingsSliders()
    {
        if (votingTimeSlider != null) votingTimeSlider.value = votingTime;
        if (cardRevealTimeSlider != null) cardRevealTimeSlider.value = cardRevealTime;
        if (discussionTimeSlider != null) discussionTimeSlider.value = discussionTime;
        UpdateVotingTimeText(votingTime);
        UpdateCardRevealTimeText(cardRevealTime);
        UpdateDiscussionTimeText(discussionTime);
    }

    void UpdateVotingTimeText(float value) { if (votingTimeValueText != null) votingTimeValueText.text = value.ToString("F0") + " сек"; }
    void UpdateCardRevealTimeText(float value) { if (cardRevealTimeValueText != null) cardRevealTimeValueText.text = value.ToString("F0") + " сек"; }
    void UpdateDiscussionTimeText(float value) { if (discussionTimeValueText != null) discussionTimeValueText.text = value.ToString("F0") + " сек"; }

    void OnApplySettingsClicked()
    {
        if (votingTimeSlider != null) votingTime = votingTimeSlider.value;
        if (cardRevealTimeSlider != null) cardRevealTime = cardRevealTimeSlider.value;
        if (discussionTimeSlider != null) discussionTime = discussionTimeSlider.value;

        Debug.Log($"Game Settings Applied: Vote Time: {votingTime}, Reveal Time: {cardRevealTime}, Discuss Time: {discussionTime}");

        // Синхронизируем настройки через свойства комнаты
        SyncGameSettings();

        if (gameSettingsPanel != null) gameSettingsPanel.SetActive(false);
    }

    void SyncGameSettings()
    {
        // Синхронизируем настройки игры через свойства комнаты
        Hashtable roomProps = new Hashtable();
        roomProps["VotingTime"] = votingTime;
        roomProps["CardRevealTime"] = cardRevealTime;
        roomProps["DiscussionTime"] = discussionTime;
        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
    }

    void OnCloseSettingsPanelClicked()
    {
        if (gameSettingsPanel != null) gameSettingsPanel.SetActive(false);
    }

    public void RefreshPlayerListUI()
    {
        if (playerListContent == null || playerListItemPrefab == null) return;

        // Очищаем текущий список
        foreach (Transform child in playerListContent.transform)
        {
            Destroy(child.gameObject);
        }

        // Добавляем всех игроков из комнаты
        foreach (Photon.Realtime.Player player in PhotonNetwork.PlayerList)
        {
            GameObject itemGO = Instantiate(playerListItemPrefab, playerListContent.transform);
            LobbyPlayerListItemUI itemUI = itemGO.GetComponent<LobbyPlayerListItemUI>();
            if (itemUI != null)
            {
                bool isReady = false;
                if (player.CustomProperties.ContainsKey("IsReady"))
                {
                    isReady = (bool)player.CustomProperties["IsReady"];
                }

                bool isHost = player.IsMasterClient;
                itemUI.Setup(player.NickName, isReady, isHost);
            }
        }
    }

    bool AreAllPlayersReady()
    {
        foreach (Photon.Realtime.Player p in PhotonNetwork.PlayerList)
        {
            if (p.IsMasterClient) continue; // Хост всегда считается готовым

            object isReady;
            if (p.CustomProperties.TryGetValue("IsReady", out isReady))
            {
                if (!(bool)isReady)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        return true;
    }

    // Photon callbacks
    public override void OnLeftRoom()
    {
        // Возвращаемся в главное меню
        if (lobbyCanvas != null) lobbyCanvas.SetActive(false);
        if (mainMenuCanvas != null) mainMenuCanvas.SetActive(true);

        // Вызываем метод MainMenuManager для сброса его состояния
        MainMenuManager mainMenuManager = FindObjectOfType<MainMenuManager>();
        if (mainMenuManager != null) mainMenuManager.ShowMainMenu();
    }

    // Обработчики событий Photon
    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        Debug.Log($"Player entered room: {newPlayer.NickName}");

        // Обновляем список игроков
        RefreshPlayerListUI();

        // Если мы хост, синхронизируем настройки игры
        if (PhotonNetwork.IsMasterClient)
        {
            SyncGameSettings();
        }
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        Debug.Log($"Player left room: {otherPlayer.NickName}");

        // Обновляем список игроков
        RefreshPlayerListUI();
    }

    public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
    {
        Debug.Log($"Master client switched to: {newMasterClient.NickName}");

        // Обновляем UI для нового хоста
        SetupLobbyUI();
        AddButtonListeners();
        RefreshPlayerListUI();
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        // Обновляем локальные настройки из свойств комнаты
        if (propertiesThatChanged.ContainsKey("VotingTime"))
        {
            votingTime = (float)propertiesThatChanged["VotingTime"];
        }

        if (propertiesThatChanged.ContainsKey("CardRevealTime"))
        {
            cardRevealTime = (float)propertiesThatChanged["CardRevealTime"];
        }

        if (propertiesThatChanged.ContainsKey("DiscussionTime"))
        {
            discussionTime = (float)propertiesThatChanged["DiscussionTime"];
        }

        // Обновляем слайдеры, если панель настроек открыта
        if (gameSettingsPanel != null && gameSettingsPanel.activeSelf)
        {
            InitializeSettingsSliders();
        }
    }

    public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, Hashtable changedProps)
    {
        // Обновляем UI списка игроков при изменении свойств игрока
        if (changedProps.ContainsKey("IsReady"))
        {
            RefreshPlayerListUI();
        }
    }
}
