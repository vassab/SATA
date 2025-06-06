using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using TMPro;

public class PhotonManager : MonoBehaviourPunCallbacks
{
    public static PhotonManager Instance { get; private set; }

    [Header("Connection Settings")]
    [SerializeField] private string gameVersion = "1.0";
    [SerializeField] private int maxPlayersPerRoom = 10;

    [Header("UI References")]
    [SerializeField] private GameObject mainMenuCanvas;
    [SerializeField] private GameObject connectingPanel;
    [SerializeField] private TextMeshProUGUI connectionStatusText;
    [SerializeField] private Button reconnectButton;

    private MainMenuManager mainMenuManager;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Настройка Photon
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    void Start()
    {
        mainMenuManager = FindObjectOfType<MainMenuManager>();

        if (reconnectButton != null)
        {
            reconnectButton.onClick.RemoveAllListeners();
            reconnectButton.onClick.AddListener(ConnectToPhoton);
        }

        ConnectToPhoton();
    }

    public void ConnectToPhoton()
    {
        if (connectingPanel != null) connectingPanel.SetActive(true);
        if (connectionStatusText != null) connectionStatusText.text = "Подключение к серверу...";
        if (reconnectButton != null) reconnectButton.gameObject.SetActive(false);

        if (PhotonNetwork.IsConnected)
        {
            if (connectingPanel != null) connectingPanel.SetActive(false);
            return;
        }

        // Устанавливаем имя игрока из PlayerPrefs
        string playerName = PlayerPrefs.GetString("PlayerName", "Player_" + Random.Range(1000, 9999));
        PhotonNetwork.NickName = playerName;

        // Подключаемся к серверу Photon
        PhotonNetwork.ConnectUsingSettings();
        PhotonNetwork.GameVersion = gameVersion;

        Debug.Log("Connecting to Photon Network...");
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Photon Master Server");
        if (connectionStatusText != null) connectionStatusText.text = "Подключено к серверу";

        // Автоматически присоединяемся к лобби
        PhotonNetwork.JoinLobby();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"Disconnected from Photon: {cause}");

        if (connectingPanel != null) connectingPanel.SetActive(true);
        if (connectionStatusText != null) connectionStatusText.text = $"Отключено от сервера: {cause}";
        if (reconnectButton != null) reconnectButton.gameObject.SetActive(true);

        // Показываем главное меню при отключении
        if (mainMenuCanvas != null) mainMenuCanvas.SetActive(true);
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Joined Photon Lobby");

        if (connectingPanel != null) connectingPanel.SetActive(false);

        // Уведомляем MainMenuManager, что мы готовы к созданию/присоединению к комнатам
        if (mainMenuManager != null)
        {
            mainMenuManager.OnPhotonConnected();
        }
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Failed to join room: {message}");

        if (connectionStatusText != null) connectionStatusText.text = $"Ошибка подключения к комнате: {message}";
        if (reconnectButton != null) reconnectButton.gameObject.SetActive(true);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Failed to create room: {message}");

        if (connectionStatusText != null) connectionStatusText.text = $"Ошибка создания комнаты: {message}";
        if (reconnectButton != null) reconnectButton.gameObject.SetActive(true);
    }

    // Метод для создания комнаты
    public void CreateRoom(string roomName = "")
    {
        if (string.IsNullOrEmpty(roomName))
        {
            roomName = "Room_" + Random.Range(1000, 9999);
        }

        // Настройки комнаты
        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = (byte)maxPlayersPerRoom,
            IsVisible = true,
            IsOpen = true,
            PublishUserId = true
        };

        PhotonNetwork.CreateRoom(roomName, roomOptions);

        if (connectingPanel != null) connectingPanel.SetActive(true);
        if (connectionStatusText != null) connectionStatusText.text = "Создание комнаты...";
    }

    // Метод для присоединения к комнате
    public void JoinRoom(string roomName)
    {
        if (string.IsNullOrEmpty(roomName))
        {
            Debug.LogError("Room name is empty");
            return;
        }

        PhotonNetwork.JoinRoom(roomName);

        if (connectingPanel != null) connectingPanel.SetActive(true);
        if (connectionStatusText != null) connectionStatusText.text = "Присоединение к комнате...";
    }

    // Метод для выхода из комнаты
    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();

        if (connectingPanel != null) connectingPanel.SetActive(true);
        if (connectionStatusText != null) connectionStatusText.text = "Выход из комнаты...";
    }

    // Метод для отключения от Photon
    public void Disconnect()
    {
        PhotonNetwork.Disconnect();
    }

    public Player CreatePlayerFromPhotonPlayer(Photon.Realtime.Player photonPlayer)
    {
        string playerName = photonPlayer.NickName;
        return new Player(playerName);
    }

}
