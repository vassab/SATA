using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class MainMenuManager : MonoBehaviourPunCallbacks
{
    [Header("UI Canvases")]
    public GameObject mainMenuCanvas;
    public GameObject lobbyCanvas;
    public GameObject connectingPanel;

    [Header("Main Menu UI")]
    public Button createGameButton;
    public Button joinGameButton;
    public TMP_InputField playerNameInput;
    public TMP_InputField joinCodeInputField;
    public TextMeshProUGUI statusText;

    [Header("References")]
    public LobbyManager lobbyManager;

    private void Start()
    {
        // Инициализация UI
        if (mainMenuCanvas != null) mainMenuCanvas.SetActive(true);
        if (lobbyCanvas != null) lobbyCanvas.SetActive(false);
        if (connectingPanel != null) connectingPanel.SetActive(false);

        // Загружаем имя игрока из PlayerPrefs
        if (playerNameInput != null)
        {
            playerNameInput.text = PlayerPrefs.GetString("PlayerName", "Player_" + Random.Range(1000, 9999));
        }

        // Добавляем обработчики событий для кнопок
        AddButtonListeners();

        // Отключаем кнопки создания/присоединения к комнате до подключения к Photon
        SetButtonsInteractable(false);
    }

    private void AddButtonListeners()
    {
        if (createGameButton != null)
        {
            createGameButton.onClick.RemoveAllListeners();
            createGameButton.onClick.AddListener(OnCreateGameClicked);
        }

        if (joinGameButton != null)
        {
            joinGameButton.onClick.RemoveAllListeners();
            joinGameButton.onClick.AddListener(OnJoinGameClicked);
        }

        if (playerNameInput != null)
        {
            playerNameInput.onEndEdit.RemoveAllListeners();
            playerNameInput.onEndEdit.AddListener(OnPlayerNameChanged);
        }
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (createGameButton != null) createGameButton.interactable = interactable;
        if (joinGameButton != null) joinGameButton.interactable = interactable;
    }

    // Вызывается из PhotonManager при успешном подключении к Photon
    public void OnPhotonConnected()
    {
        SetButtonsInteractable(true);
        if (statusText != null) statusText.text = "Подключено к серверу";
    }

    private void OnCreateGameClicked()
    {
        // Сохраняем имя игрока
        SavePlayerName();

        // Создаем комнату через PhotonManager
        PhotonManager.Instance.CreateRoom();
    }

    private void OnJoinGameClicked()
    {
        // Проверяем, что код комнаты введен
        if (joinCodeInputField == null || string.IsNullOrEmpty(joinCodeInputField.text))
        {
            if (statusText != null) statusText.text = "Введите код комнаты";
            return;
        }

        // Сохраняем имя игрока
        SavePlayerName();

        // Присоединяемся к комнате через PhotonManager
        PhotonManager.Instance.JoinRoom(joinCodeInputField.text);
    }

    private void OnPlayerNameChanged(string newName)
    {
        SavePlayerName();
    }

    private void SavePlayerName()
    {
        if (playerNameInput != null && !string.IsNullOrEmpty(playerNameInput.text))
        {
            string playerName = playerNameInput.text;
            PlayerPrefs.SetString("PlayerName", playerName);
            PlayerPrefs.Save();

            // Обновляем имя в Photon
            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.NickName = playerName;
            }
        }
    }

    public void ShowMainMenu()
    {
        if (mainMenuCanvas != null) mainMenuCanvas.SetActive(true);
        if (lobbyCanvas != null) lobbyCanvas.SetActive(false);
    }

    // Photon callbacks
    public override void OnJoinedRoom()
    {
        Debug.Log("Joined room: " + PhotonNetwork.CurrentRoom.Name);

        // Скрываем панель подключения
        if (connectingPanel != null) connectingPanel.SetActive(false);

        // Переходим в лобби
        if (lobbyManager != null)
        {
            lobbyManager.InitializeLobby();
        }
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Room creation failed: {message}");

        // Показываем сообщение об ошибке
        if (statusText != null) statusText.text = $"Ошибка создания комнаты: {message}";
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Join room failed: {message}");

        // Показываем сообщение об ошибке
        if (statusText != null) statusText.text = $"Ошибка присоединения к комнате: {message}";
    }
}
