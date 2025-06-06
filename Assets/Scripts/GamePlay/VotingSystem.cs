using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using Photon.Pun;
using System.Collections;
using System.Linq;

public class VotingSystem : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    [SerializeField] private GameObject votingPanel;
    [SerializeField] private GameObject playerVoteButtonPrefab;
    [SerializeField] private Transform playerButtonsContainer;
    [SerializeField] private TextMeshProUGUI votingStatusText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private Button skipVoteButton;

    [Header("Voting Settings")]
    [SerializeField] private float votingTime = 30f;

    private Dictionary<string, int> votes = new Dictionary<string, int>();
    private List<Player> activePlayers = new List<Player>();
    private bool isVotingActive = false;
    private float currentVotingTime;
    private Player localVotedPlayer = null;
    private int totalVotes = 0;
    private GameManager gameManager;

    public delegate void VotingCompletedEvent();
    public GameObject VotingPanel => votingPanel;
    public event VotingCompletedEvent OnVotingCompleted;

    private PhotonView photonView;

    void Awake()
    {
        photonView = GetComponent<PhotonView>();
        if (photonView == null)
        {
            Debug.LogError("VotingSystem: PhotonView is missing!");
            photonView = gameObject.AddComponent<PhotonView>();
        }
    }

    void Start()
    {
        gameManager = GameManager.Instance;

        // Настраиваем кнопку пропуска голосования
        if (skipVoteButton != null)
        {
            skipVoteButton.onClick.AddListener(OnSkipVoteClicked);
        }
        else
        {
            Debug.LogError("VotingSystem: skipVoteButton is not assigned!");
        }
    }

    public void StartVoting(List<Player> players, Player currentPlayer)
    {
        Debug.Log($"VotingSystem: StartVoting called with {players.Count} players");

        // Проверяем UI ссылки
        if (votingPanel == null || playerButtonsContainer == null || playerVoteButtonPrefab == null)
        {
            Debug.LogError("VotingSystem: Missing UI references");
            return;
        }

        // Проверяем список игроков
        if (players == null || players.Count == 0)
        {
            Debug.LogError("VotingSystem: players list is null or empty!");
            return;
        }

        // Только мастер-клиент может начать голосование
        if (!PhotonNetwork.IsMasterClient) return;

        // Инициализируем данные голосования
        activePlayers = new List<Player>(players);
        votes.Clear();
        isVotingActive = true;
        currentVotingTime = votingTime;
        totalVotes = 0;

        foreach (Player player in activePlayers)
        {
            if (player == null || string.IsNullOrEmpty(player.Name))
            {
                Debug.LogError("VotingSystem: Player is null or has no name — skipping.");
                continue;
            }
            votes[player.Name] = 0;
        }

        // Отправляем RPC для синхронизации начала голосования
        photonView.RPC("SyncStartVoting", RpcTarget.All);
    }

    [PunRPC]
    void SyncStartVoting()
    {
        Debug.Log("VotingSystem: SyncStartVoting received");
        Debug.Log("[DEBUG] SyncStartVoting received on client.");

        // Получаем список активных игроков из GameManager
        if (gameManager == null)
        {
            gameManager = GameManager.Instance;
        }

        if (gameManager != null)
        {
            activePlayers = gameManager.GetActivePlayers();
        }
        else
        {
            Debug.LogError("VotingSystem: GameManager is null!");
            return;
        }

        // Инициализируем данные голосования
        votes.Clear();
        isVotingActive = true;
        currentVotingTime = votingTime;
        localVotedPlayer = null;
        totalVotes = 0;

        foreach (Player player in activePlayers)
        {
            if (player == null || string.IsNullOrEmpty(player.Name))
            {
                Debug.LogError("VotingSystem: Player is null or has no name — skipping.");
                continue;
            }
            votes[player.Name] = 0;
        }

        // Показываем панель голосования
        if (votingPanel != null)
        {
            votingPanel.SetActive(true);

            // Создаем кнопки голосования
            CreateVotingButtons();

            // Обновляем UI голосования
            UpdateVotingUI();

            // Запускаем таймер
            StartCoroutine(VotingTimer());
        }
        else
        {
            Debug.LogError("VotingSystem: votingPanel is null!");
        }
    }

    private HashSet<string> playersWhoVoted = new HashSet<string>();

    public void RegisterVote(Player voter, Player target)
    {
        // Проверяем входные параметры
        if (voter == null || target == null)
        {
            Debug.LogError("RegisterVote: voter or target is null!");
            return;
        }

        string voterName = voter.Name;

        // Проверяем, не голосовал ли уже этот игрок
        if (playersWhoVoted.Contains(voterName))
        {
            Debug.LogWarning($"{voterName} уже голосовал в этом раунде!");
            return;
        }

        // Проверяем, что цель голосования активна
        if (!target.IsActive)
        {
            Debug.LogWarning($"{voterName} пытается проголосовать против неактивного игрока {target.Name}!");
            return;
        }

        // Проверяем, что голосующий не голосует сам за себя
        if (voter == target)
        {
            Debug.LogWarning($"{voterName} пытается проголосовать против себя!");
            return;
        }

        // Регистрируем голос
        playersWhoVoted.Add(voterName);
        target.AddVote();

        Debug.Log($"{voterName} проголосовал против {target.Name}. Всего голосов: {target.Votes}");

        // Проверяем завершение голосования
        CheckVotingCompletion();
    }

    public void ResetVoting()
    {
        playersWhoVoted.Clear();
        Debug.Log("Система голосования сброшена");
    }

    private void CheckVotingCompletion()
    {
        int activePlayers = GameManager.Instance.GetActivePlayers().Count;
        int votesCast = playersWhoVoted.Count;

        Debug.Log($"Проголосовало {votesCast} из {activePlayers} активных игроков");

        // Если проголосовали все активные игроки
        if (votesCast >= activePlayers)
        {
            CompleteVoting();
        }
    }

    void CreateVotingButtons()
    {
        Debug.Log("VotingSystem: Creating voting buttons");

        try
        {
            // Очищаем контейнер кнопок
            foreach (Transform child in playerButtonsContainer)
            {
                Destroy(child.gameObject);
            }

            // Создаем кнопку для каждого активного игрока
            foreach (Player player in activePlayers)
            {
                // Пропускаем локального игрока (нельзя голосовать за себя)
                if (player.Name == PhotonNetwork.NickName) continue;

                GameObject buttonObj = Instantiate(playerVoteButtonPrefab, playerButtonsContainer);
                Button button = buttonObj.GetComponent<Button>();
                TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();

                if (buttonText != null)
                {
                    buttonText.text = player.Name;
                }
                else
                {
                    Debug.LogWarning("VotingSystem: Button text component not found!");
                }

                if (button != null)
                {
                    // Важно: создаем локальную копию переменной для замыкания
                    Player targetPlayer = player;

                    // Удаляем все существующие слушатели и добавляем новый
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => {
                        Debug.Log($"VotingSystem: Button clicked for {targetPlayer.Name}");
                        CastVote(targetPlayer);
                    });
                }
                else
                {
                    Debug.LogWarning("VotingSystem: Button component not found!");
                }
            }

            Debug.Log($"VotingSystem: Created {playerButtonsContainer.childCount} voting buttons");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"VotingSystem: Error in CreateVotingButtons: {e.Message}\n{e.StackTrace}");
        }
    }

    public void CastVote(Player targetPlayer)
    {
        Debug.Log($"VotingSystem: CastVote called for {targetPlayer.Name}");

        // Проверяем, что голосование активно
        if (!isVotingActive)
        {
            Debug.LogError("VotingSystem: Voting is not active!");
            return;
        }

        // Проверяем, что игрок еще не голосовал
        if (localVotedPlayer != null)
        {
            Debug.LogError("VotingSystem: Player already voted!");
            return;
        }

        // Запоминаем, за кого проголосовал локальный игрок
        localVotedPlayer = targetPlayer;

        // Синхронизируем голос со всеми клиентами
        photonView.RPC("SyncVote", RpcTarget.All, PhotonNetwork.NickName, targetPlayer.Name);
    }

    // Метод для NPC, чтобы зарегистрировать голос
    public void RegisterVote(Player targetPlayer)
    {
        Debug.Log($"VotingSystem: RegisterVote called for {targetPlayer.Name}");

        // Проверяем, что голосование активно
        if (!isVotingActive) return;

        // Получаем имя NPC (предполагается, что вызывающий скрипт принадлежит NPC)
        string npcName = "NPC";
        NPCController npcController = FindObjectOfType<NPCController>();
        if (npcController != null)
        {
            Player npcPlayer = npcController.GetPlayer();
            if (npcPlayer != null)
            {
                npcName = npcPlayer.Name;
            }
        }

        // Синхронизируем голос NPC со всеми клиентами
        photonView.RPC("SyncVote", RpcTarget.All, npcName, targetPlayer.Name);
    }

    [PunRPC]
    void SyncVote(string voterName, string targetName)
    {
        Debug.Log($"VotingSystem: SyncVote received - {voterName} voted for {targetName}");

        try
        {
            // Находим игрока по имени
            Player target = activePlayers.Find(p => p.Name == targetName);

            if (target == null)
            {
                Debug.LogError($"VotingSystem: Target player with name {targetName} not found!");
                return;
            }

            // Регистрируем голос
            if (votes.ContainsKey(target.Name))
            {
                votes[target.Name] = votes[target.Name] + 1;
            }
            else
            {
                Debug.LogError($"VotingSystem: Target name {targetName} not found in votes dictionary!");
                votes[target.Name] = 1; // Создаем запись, если её нет
            }

            totalVotes++;

            // Обновляем UI голосования
            UpdateVotingUI();

            // Проверяем, все ли проголосовали
            CheckVotingComplete();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"VotingSystem: Error in SyncVote: {e.Message}\n{e.StackTrace}");
        }
    }

    void OnSkipVoteClicked()
    {
        Debug.Log("VotingSystem: Skip vote clicked");

        // Проверяем, что голосование активно
        if (!isVotingActive) return;

        // Проверяем, что игрок еще не голосовал
        if (localVotedPlayer != null) return;

        // Отмечаем, что локальный игрок пропустил голосование
        localVotedPlayer = new Player("Skip");

        // Синхронизируем пропуск голоса со всеми клиентами
        photonView.RPC("SyncSkipVote", RpcTarget.All, PhotonNetwork.NickName);
    }

    [PunRPC]
    void SyncSkipVote(string voterName)
    {
        Debug.Log($"VotingSystem: SyncSkipVote received - {voterName} skipped voting");

        // Увеличиваем счетчик голосов
        totalVotes++;

        // Обновляем UI голосования
        UpdateVotingUI();

        // Проверяем, все ли проголосовали
        CheckVotingComplete();
    }

    void CheckVotingComplete()
    {
        // Проверяем, все ли игроки проголосовали
        int expectedVotes = PhotonNetwork.CurrentRoom.PlayerCount;

        // Добавляем голоса NPC, если они есть
        if (gameManager != null)
        {
            int npcCount = gameManager.GetActivePlayers().Count(p => p.Name.Contains("NPC"));
            expectedVotes += npcCount;
        }

        Debug.Log($"VotingSystem: Checking voting complete - {totalVotes}/{expectedVotes} votes");

        if (totalVotes >= expectedVotes)
        {
            // Только мастер-клиент завершает голосование
            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC("CompleteVoting", RpcTarget.All);
            }
        }
        Debug.Log("[DEBUG] Voting time expired, completing voting.");

    }

    [PunRPC]
    void CompleteVoting()
    {
        Debug.Log("VotingSystem: CompleteVoting received");

        try
        {
            isVotingActive = false;

            // 1. Скрываем UI голосования
            if (votingPanel != null)
            {
                votingPanel.SetActive(false);
            }
            else
            {
                Debug.LogError("Voting panel reference is missing!");
            }

            // 2. Определяем исключенного игрока
            Player eliminatedPlayer = GetVotingResult();
            Debug.Log($"Eliminated player: {(eliminatedPlayer != null ? eliminatedPlayer.Name : "none")}");

            // 3. Уведомляем GameManager (только на Master Client)
            if (PhotonNetwork.IsMasterClient)
            {
                if (OnVotingCompleted != null)
                {
                    Debug.Log("Invoking OnVotingCompleted with eliminated player");
                    OnVotingCompleted.Invoke();
                }
                else
                {
                    Debug.LogError("OnVotingCompleted event is not subscribed!");
                    // Fallback: запускаем следующий раунд напрямую
                    GameManager.Instance?.StartNextRound();
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in CompleteVoting: {e.Message}\n{e.StackTrace}");

            // Аварийный переход к следующему раунду
            if (PhotonNetwork.IsMasterClient)
            {
                GameManager.Instance?.StartNextRound();
            }
        }
    }

    void UpdateVotingUI()
    {
        Debug.Log("[DEBUG] Updating voting UI...");
        try
        {
            // Обновляем статус голосования
            if (votingStatusText != null)
            {
                int expectedVotes = PhotonNetwork.CurrentRoom.PlayerCount;
                votingStatusText.text = $"Проголосовало: {totalVotes}/{expectedVotes}";
                Debug.Log($"[DEBUG] Voting status updated: {votingStatusText.text}");

                // Добавляем голоса NPC, если они есть
                if (gameManager != null)
                {
                    int npcCount = gameManager.GetActivePlayers().Count(p => p.Name.Contains("NPC"));
                    expectedVotes += npcCount;
                }

                votingStatusText.text = $"Проголосовало: {totalVotes}/{expectedVotes}";
            }

            // Обновляем UI кнопок
            foreach (Transform child in playerButtonsContainer)
            {
                Button button = child.GetComponent<Button>();
                TextMeshProUGUI buttonText = child.GetComponentInChildren<TextMeshProUGUI>();

                if (button != null && buttonText != null)
                {
                    string playerName = buttonText.text.Split(' ')[0];
                    Player targetPlayer = activePlayers.Find(p => p.Name == playerName);
                    if (targetPlayer != null)
                    {
                        // Если игрок уже проголосовал, делаем кнопку неактивной
                        button.interactable = (localVotedPlayer == null);

                        // Показываем количество голосов за этого игрока
                        if (votes.ContainsKey(targetPlayer.Name))
                        {
                            buttonText.text = $"{targetPlayer.Name} ({votes[targetPlayer.Name]})";
                        }
                    }
                }
            }

            // Обновляем кнопку пропуска голосования
            if (skipVoteButton != null)
            {
                skipVoteButton.interactable = (localVotedPlayer == null);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"VotingSystem: Error in UpdateVotingUI: {e.Message}\n{e.StackTrace}");
        }
    }

    IEnumerator VotingTimer()
    {
        currentVotingTime = votingTime;

        while (isVotingActive && currentVotingTime > 0)
        {
            // Обновляем UI таймера
            if (timerText != null)
            {
                timerText.text = $"Время: {Mathf.CeilToInt(currentVotingTime)}";
                Debug.Log($"[DEBUG] Timer updated: {timerText.text}");
            }

            yield return new WaitForSeconds(1f);
            currentVotingTime -= 1f;

            // Если время истекло, завершаем голосование
            if (currentVotingTime <= 0f && PhotonNetwork.IsMasterClient)
            {
                photonView.RPC("CompleteVoting", RpcTarget.All);
            }
        }
    }

    public Player GetVotingResult()
    {
        if (votes.Count == 0) return null;

        string mostVotedName = null;
        int maxVotes = -1;

        foreach (var kvp in votes)
        {
            if (kvp.Value > maxVotes)
            {
                maxVotes = kvp.Value;
                mostVotedName = kvp.Key;
            }
        }

        // Вернуть Player из списка активных игроков по имени
        return activePlayers.Find(p => p.Name == mostVotedName);
    }
    // === DEBUG LOG BLOCK ===

    void DebugLogVotingState()
    {
        Debug.Log("=== [VotingSystem DEBUG] ===");
        Debug.Log($"isVotingActive: {isVotingActive}");
        Debug.Log($"activePlayers.Count: {activePlayers?.Count ?? -1}");
        Debug.Log($"votes.Count: {votes?.Count ?? -1}");
        Debug.Log($"totalVotes: {totalVotes}");
        Debug.Log($"currentVotingTime: {currentVotingTime}");

        if (votingPanel != null)
            Debug.Log($"votingPanel activeSelf: {votingPanel.activeSelf}");
        else
            Debug.Log("votingPanel is NULL!");

        if (playerButtonsContainer != null)
            Debug.Log($"playerButtonsContainer child count: {playerButtonsContainer.childCount}");
        else
            Debug.Log("playerButtonsContainer is NULL!");

        if (timerText != null)
            Debug.Log($"timerText.text: {timerText.text}");
        else
            Debug.Log("timerText is NULL!");

        if (votingStatusText != null)
            Debug.Log($"votingStatusText.text: {votingStatusText.text}");
        else
            Debug.Log("votingStatusText is NULL!");

        if (skipVoteButton != null)
            Debug.Log($"skipVoteButton interactable: {skipVoteButton.interactable}");
        else
            Debug.Log("skipVoteButton is NULL!");
    }
}
