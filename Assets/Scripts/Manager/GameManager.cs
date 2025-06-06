using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance { get; private set; }
    public bool IsVotingPhase { get; private set; }

    [Header("Game Settings")]
    [SerializeField]
    private List<CardType> startingCardTypes = new List<CardType>
    {
        CardType.Profession,
        CardType.Biology,
        CardType.Health,
        CardType.Hobby,
        CardType.Baggage,
        CardType.Facts
    };

    [SerializeField] private int playersCount = 4;
    [SerializeField] private float timePerTurn = 60f;

    [Header("UI References")]
    [SerializeField] private CardDisplay_Interactive cardPrefab;
    [SerializeField] private Transform cardsContainer;
    [SerializeField] private VotingSystem votingSystem;
    [SerializeField] private GameObject endGamePanel;

    private List<Player> players = new List<Player>();
    private int currentPlayerIndex;
    private float currentTurnTime;
    private bool isGameActive;
    private List<CardDisplay_Interactive> activeCards = new List<CardDisplay_Interactive>();

    private PhotonView photonView;
    private Dictionary<string, Dictionary<CardType, List<CardData>>> revealedCardsHistory = new Dictionary<string, Dictionary<CardType, List<CardData>>>();
    private Dictionary<string, bool> playerMoveStatus = new Dictionary<string, bool>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        photonView = GetComponent<PhotonView>();
        if (photonView == null)
        {
            Debug.LogError("GameManager: PhotonView component is missing!");
        }
    }

    void Start()
    {
        InitializeGame();
    }

    public void InitializeGame()
    {
        if (startingCardTypes.Count == 0)
        {
            Debug.LogError("No starting card types assigned!");
            return;
        }

        CreatePlayers();

        if (PhotonNetwork.IsMasterClient)
        {
            DealCards();
        }

        StartGame();
    }

    void CreatePlayers()
    {
        players.Clear();
        playerMoveStatus.Clear();

        // Только для Master Client
        if (!PhotonNetwork.IsMasterClient) return;

        // Создаём реальных игроков
        foreach (var photonPlayer in PhotonNetwork.PlayerList)
        {
            string playerId = photonPlayer.UserId;
            string playerName = photonPlayer.NickName;
            players.Add(new Player(playerId, playerName));
            playerMoveStatus[playerName] = false;
        }

        // Создаём NPC (только если не хватает игроков)
        int npcNeeded = playersCount - PhotonNetwork.PlayerList.Length;
        for (int i = 0; i < npcNeeded; i++)
        {
            string npcId = $"NPC_{i + 1}";
            string npcName = $"NPC_{i + 1}";

            // Проверяем, не создан ли уже NPC с таким именем
            if (!players.Any(p => p.Name == npcName))
            {
                players.Add(new Player(npcId, npcName));
                playerMoveStatus[npcName] = false;

                // Создаём GameObject для NPC
                if (GameObject.Find(npcName) == null)
                {
                    GameObject npcObj = new GameObject(npcName);
                    NPCController npcController = npcObj.AddComponent<NPCController>();
                    npcController.npcName = npcName;
                    npcController.Initialize(players.Last(), npcId);
                    Debug.Log($"[DEBUG] NPC {npcName} initialized with Player {players.Last().Name}");
                }
            }
        }
    }

    private HashSet<string> playersWhoActedThisRound = new HashSet<string>();

    public bool CanPlayerAct(string playerName)
    {
        return !playersWhoActedThisRound.Contains(playerName);
    }

    public void RegisterPlayerAction(string playerName)
    {
        playersWhoActedThisRound.Add(playerName);
    }

    public void ClearRoundActions()
    {
        playersWhoActedThisRound.Clear();
    }

    void StartGame()
    {
        isGameActive = true;
        currentPlayerIndex = 0;
        currentTurnTime = timePerTurn;
        StartTurn(currentPlayerIndex);
    }

    void StartTurn(int playerIndex)
    {
        if (!isGameActive || players.Count == 0)
        {
            Debug.LogWarning("Cannot start turn - game not active or no players");
            return;
        }

        // Устанавливаем нового текущего игрока
        currentPlayerIndex = playerIndex;
        currentTurnTime = timePerTurn;

        Player currentPlayer = GetCurrentPlayer();
        if (currentPlayer == null)
        {
            Debug.LogError("Failed to get current player!");
            return;
        }

        Debug.Log($"Starting turn for {currentPlayer.Name} (Local: {PhotonNetwork.NickName})");

        // Синхронизация хода для всех клиентов
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("SyncCurrentTurn", RpcTarget.All, currentPlayerIndex, currentTurnTime);
        }

        // Обновляем UI только для локального игрока
        if (currentPlayer.Name == PhotonNetwork.NickName)
        {
            Debug.Log("Displaying cards for local player");
            DisplayPlayerHand(currentPlayer);
        }
        else
        {
            Debug.Log("Clearing cards container - not local player's turn");
            ClearCardsContainer();

            // Если это NPC, запускаем его ход
            if (currentPlayer.Name.StartsWith("NPC_"))
            {
                NPCController npc = FindNPCController(currentPlayer.Name);
                if (npc != null)
                {
                    npc.StartNPCTurn();
                }
            }
        }
    }

    [PunRPC]
    void SyncCardHistory(string playerName, string cardId, int cardTypeInt)
    {
        CardType cardType = (CardType)cardTypeInt;
        CardData card = DeckManager.Instance.GetCardById(cardId);

        if (card == null)
        {
            Debug.LogError($"[GameManager] Card with ID {cardId} not found in SyncCardHistory!");
            return;
        }

        if (!revealedCardsHistory.ContainsKey(playerName))
        {
            revealedCardsHistory[playerName] = new Dictionary<CardType, List<CardData>>();
        }

        if (!revealedCardsHistory[playerName].ContainsKey(cardType))
        {
            revealedCardsHistory[playerName][cardType] = new List<CardData>();
        }

        revealedCardsHistory[playerName][cardType].Add(card);

        Debug.Log($"[GameManager] Synced card {card.cardName} for {playerName}");
    }


    [PunRPC]
    void SyncCurrentTurn(int playerIndex, float turnTime)
    {
        try
        {
            // Обновляем состояние на всех клиентах
            currentPlayerIndex = playerIndex;
            currentTurnTime = turnTime;

            Player currentPlayer = GetCurrentPlayer();
            if (currentPlayer == null)
            {
                Debug.LogError("SyncCurrentTurn: Current player is null!");
                return;
            }

            Debug.Log($"[SYNC] Turn sync received. Current player: {currentPlayer.Name} (Local: {PhotonNetwork.NickName})");

            // Проверяем, является ли текущий игрок локальным игроком
            if (currentPlayer.Name == PhotonNetwork.NickName)
            {
                Debug.Log("Displaying cards for local player");
                DisplayPlayerHand(currentPlayer);
            }
            else
            {
                Debug.Log("Clearing UI - not local player's turn");
                ClearCardsContainer();

                // Если это NPC, запускаем его ход (только на Master Client)
                if (currentPlayer.Name.StartsWith("NPC_") && PhotonNetwork.IsMasterClient)
                {
                    NPCController npc = FindNPCController(currentPlayer.Name);
                    if (npc != null)
                    {
                        Debug.Log($"Starting NPC turn: {currentPlayer.Name}");
                        npc.StartNPCTurn();
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in SyncCurrentTurn: {e.Message}\n{e.StackTrace}");
        }
    }

    bool IsLocalPlayerTurn()
    {
        Player currentPlayer = GetCurrentPlayer();
        return currentPlayer != null && currentPlayer.Name == PhotonNetwork.NickName;
    }

    public void EndTurn()
    {
        if (!isGameActive) return;

        Debug.Log($"Ending turn for {GetCurrentPlayer().Name}");

        if (GetCurrentPlayer() != null)
        {
            playerMoveStatus[GetCurrentPlayer().Name] = true;

            if (photonView != null)
            {
                photonView.RPC("SyncPlayerMoveStatus", RpcTarget.Others, GetCurrentPlayer().Name, true);
            }
        }

        bool allPlayersMoved = players.Where(p => p.IsActive).All(p => playerMoveStatus[p.Name]);
        Debug.Log($"[DEBUG] All players moved? {allPlayersMoved}");

        if (allPlayersMoved)
        {
            StartVotingPhase();
        }
        else
        {
            int nextPlayer = (currentPlayerIndex + 1) % players.Count;
            StartTurn(nextPlayer);
        }
        var currentPlayer = GetCurrentPlayer();
        if (currentPlayer == null)
        {
            Debug.LogError("GetCurrentPlayer() returned null!");
            return;
        }
        Debug.Log($"[DEBUG] CurrentPlayer: {currentPlayer.Name}");
    }

    [PunRPC]
    void SyncPlayerMoveStatus(string playerName, bool hasMoved)
    {
        playerMoveStatus[playerName] = hasMoved;
    }

    void Update()
    {
        if (!isGameActive) return;

        currentTurnTime -= Time.deltaTime;
        if (currentTurnTime <= 0)
        {
            EndTurn();
        }
    }

    public void OnCardSelected(CardDisplay_Interactive cardUI)
    {
        if (!isGameActive || cardUI == null) return;

        if (IsPlayerTurn(cardUI.GetOwner()))
        {
            Debug.Log($"Selected card: {cardUI.GetCardData().cardName}");
            RegisterDiscardedCard(cardUI.GetOwner(), cardUI.GetCardData());
        }
    }

    public void RegisterDiscardedCard(Player player, CardData card)
    {

        Debug.Log($"[DEBUG] RegisterDiscardedCard called for {player.Name} with card {card.cardName}");
        if (player == null || card == null) return;

        string playerName = player.Name;

        if (!revealedCardsHistory.ContainsKey(playerName))
        {
            revealedCardsHistory[playerName] = new Dictionary<CardType, List<CardData>>();
        }

        if (!revealedCardsHistory[playerName].ContainsKey(card.cardType))
        {
            revealedCardsHistory[playerName][card.cardType] = new List<CardData>();
        }

        revealedCardsHistory[playerName][card.cardType].Add(card);

        Debug.Log($"[GameManager] Registered {card.cardName} for {playerName}");


        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("SyncCardHistory", RpcTarget.All, playerName, card.cardId, (int)card.cardType);
        }
    }


    public Dictionary<Player, Dictionary<CardType, List<CardData>>> GetRevealedCardsHistory()
    {
        Dictionary<Player, Dictionary<CardType, List<CardData>>> result = new Dictionary<Player, Dictionary<CardType, List<CardData>>>();

        foreach (var kvp in revealedCardsHistory)
        {
            string playerName = kvp.Key;
            Player player = players.Find(p => p.Name == playerName);

            if (player != null)
            {
                result[player] = kvp.Value;
            }
        }

        return result;
    }




    private bool isVotingInProgress;

    public void StartVotingPhase()
    {
        if (isVotingInProgress) return;

        isVotingInProgress = true;
        IsVotingPhase = true;

        // Сбрасываем голоса
        votingSystem.ResetVoting();

        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("[DEBUG] Sending RPCOpenVotingPanel");
            photonView.RPC("RPCOpenVotingPanel", RpcTarget.All);

            Debug.Log("[DEBUG] Sending SyncStartVoting RPC");
            votingSystem.photonView.RPC("SyncStartVoting", RpcTarget.All);
        }
    }


    [PunRPC]
    void RPCOpenVotingPanel()
    {
        votingSystem.VotingPanel.SetActive(true); // Ваш метод открытия панели
        Debug.Log("[DEBUG] Voting phase started, waiting for votes.");
    }

    public void OnVotingCompleted()
    {

        IsVotingPhase = false;
        Player eliminated = votingSystem.GetVotingResult();
        if (eliminated != null)
        {
            eliminated.IsActive = false;
            Debug.Log($"{eliminated.Name} eliminated!");

            if (photonView != null && PhotonNetwork.IsMasterClient)
            {
                photonView.RPC("SyncPlayerStatus", RpcTarget.All, eliminated.Name, false);
            }

            CheckGameEnd();
        }
        StartNextRound();
    }

    [PunRPC]
    void SyncPlayerStatus(string playerName, bool isActive)
    {
        Player player = players.Find(p => p.Name == playerName);
        if (player != null)
        {
            player.IsActive = isActive;
        }
    }

    public void StartNextRound()
    {
        // Сбрасываем голоса всех игроков
        players.ForEach(p => p.ResetVotes());

        // Сбрасываем статусы ходов
        foreach (string playerName in playerMoveStatus.Keys.ToList())
        {
            playerMoveStatus[playerName] = false;
        }

        // Сбрасываем флаги NPC
        var npcControllers = FindObjectsOfType<NPCController>();
        foreach (var npc in npcControllers)
        {
            if (npc != null)
            {
                npc.ResetRoundFlags();
            }
        }

        // Синхронизируем с другими клиентами
        if (photonView != null && PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("SyncResetMoveStatus", RpcTarget.All);
        }

        // Проверяем количество активных игроков
        int activePlayers = players.Count(p => p.IsActive);

        if (activePlayers >= 2)
        {
            // Начинаем новый раунд
            currentPlayerIndex = 0;
            IsVotingPhase = false;
            StartTurn(currentPlayerIndex);
        }
        else
        {
            CheckGameEnd();
        }
    }

    [PunRPC]
    void SyncResetMoveStatus()
    {
        foreach (string playerName in playerMoveStatus.Keys.ToList())
        {
            playerMoveStatus[playerName] = false;
        }
    }

    void DealCards()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (DeckManager.Instance == null)
        {
            Debug.LogError("DeckManager instance not found!");
            return;
        }

        Dictionary<string, List<string>> playerCards = new Dictionary<string, List<string>>();

        foreach (Player player in players)
        {
            List<string> cardIds = new List<string>();
            foreach (CardType type in startingCardTypes)
            {
                CardData card = DeckManager.Instance.GetRandomCard(type);
                if (card != null)
                {
                    cardIds.Add(card.cardId);
                }
            }
            playerCards[player.Name] = cardIds;
        }

        string serializedCards = SerializePlayerCards(playerCards);
        photonView.RPC("SyncPlayerCards", RpcTarget.All, serializedCards);
    }

    [PunRPC]
    void SyncPlayerCards(string serializedCards)
    {
        try
        {
            Debug.Log($"Received cards data: {serializedCards}");

            // Десериализация данных
            Dictionary<string, List<string>> playerCards = DeserializePlayerCards(serializedCards);
            if (playerCards == null)
            {
                Debug.LogError("Failed to deserialize player cards");
                return;
            }

            foreach (var kvp in playerCards)
            {
                string playerName = kvp.Key;
                List<string> cardIds = kvp.Value;

                // Ищем игрока в списке
                Player player = players.Find(p => p.Name == playerName);

                // Если игрока нет — создаём и добавляем
                if (player == null)
                {
                    Debug.LogWarning($"Player {playerName} not found, creating new Player instance");
                    player = new Player(playerName, playerName); // id = name (если нет отдельного id)
                    players.Add(player);
                }

                // Очищаем старую руку
                player.ClearHand();

                // Добавляем карты
                foreach (string cardId in cardIds)
                {
                    CardData card = DeckManager.Instance?.GetCardById(cardId);
                    if (card == null)
                    {
                        Debug.LogWarning($"Card {cardId} not found in deck");
                        continue;
                    }
                    player.AddCard(card);
                }

                Debug.Log($"Updated {player.Name}'s hand with {player.GetHand().Count} cards");
            }

            // Обновляем UI для локального игрока, если его ход
            Player currentPlayer = GetCurrentPlayer();
            if (currentPlayer != null && currentPlayer.Name == PhotonNetwork.NickName)
            {
                Debug.Log($"Updating UI for local player: {currentPlayer.Name}");
                DisplayPlayerHand(currentPlayer);
            }
            else
            {
                Debug.Log($"Not local player's turn. Current: {currentPlayer?.Name}, Local: {PhotonNetwork.NickName}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in SyncPlayerCards: {e.Message}\n{e.StackTrace}");
        }
    }

    string SerializePlayerCards(Dictionary<string, List<string>> playerCards)
    {
        string result = "";
        foreach (var kvp in playerCards)
        {
            result += kvp.Key + ":" + string.Join(",", kvp.Value) + ";";
        }
        return result;
    }

    Dictionary<string, List<string>> DeserializePlayerCards(string serialized)
    {
        var result = new Dictionary<string, List<string>>();
        var entries = serialized.Split(';');

        foreach (var entry in entries)
        {
            if (string.IsNullOrEmpty(entry)) continue;

            var parts = entry.Split(':');
            if (parts.Length != 2) continue;

            string name = parts[0];
            var ids = parts[1].Split(',').Where(id => !string.IsNullOrEmpty(id)).ToList();
            result[name] = ids;
        }

        return result;
    }

    void DisplayPlayerHand(Player player)
    {
        ClearCardsContainer();

        if (player == null || cardPrefab == null || cardsContainer == null)
        {
            Debug.LogWarning("Invalid player or UI references");
            return;
        }

        bool wasActive = cardPrefab.gameObject.activeSelf;
        cardPrefab.gameObject.SetActive(true);

        foreach (CardData card in player.GetHand())
        {
            CardDisplay_Interactive cardUI = Instantiate(cardPrefab, cardsContainer);
            cardUI.Initialize(card, player);
            activeCards.Add(cardUI);
        }

        cardPrefab.gameObject.SetActive(wasActive);
        LayoutRebuilder.ForceRebuildLayoutImmediate(cardsContainer as RectTransform);
    }

    private NPCController FindNPCController(string npcName)
    {
        if (string.IsNullOrEmpty(npcName))
        {
            Debug.LogError("FindNPCController: npcName is null or empty!");
            return null;
        }

        // Ищем среди всех NPC контроллеров в сцене
        NPCController[] allNPCs = FindObjectsOfType<NPCController>();
        foreach (NPCController npc in allNPCs)
        {
            if (npc.GetPlayer()?.Name == npcName)
            {
                Debug.Log($"Found NPC controller for {npcName}");
                return npc;
            }
        }

        Debug.LogError($"NPC controller for {npcName} not found!");
        return null;
    }

    /// <summary>
    /// Запускает ход для NPC
    /// </summary>
    public void StartNPCTurn(Player npcPlayer)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.Log("Only master client can start NPC turns");
            return;
        }

        if (npcPlayer == null)
        {
            Debug.LogError("StartNPCTurn: npcPlayer is null!");
            return;
        }

        Debug.Log($"Starting NPC turn for {npcPlayer.Name}");

        // Находим контроллер NPC
        NPCController npcController = FindNPCController(npcPlayer.Name);
        if (npcController == null)
        {
            Debug.LogError($"Failed to find controller for NPC {npcPlayer.Name}");
            return;
        }

        // Запускаем ход NPC
        npcController.StartNPCTurn();

        // Синхронизируем начало хода для всех клиентов
        photonView.RPC("SyncNPCTurn", RpcTarget.Others, npcPlayer.Name);
    }

    [PunRPC]
    private void SyncNPCTurn(string npcName)
    {
        // Этот RPC получают все клиенты, кроме мастера (который уже обработал ход)
        Player npcPlayer = players.Find(p => p.Name == npcName);
        if (npcPlayer == null) return;

        Debug.Log($"Syncing NPC turn for {npcName}");

        // Обновляем UI - очищаем карты (так как это не наш ход)
        ClearCardsContainer();
    }

    void ClearCardsContainer()
    {
        foreach (var card in activeCards)
        {
            if (card != null) Destroy(card.gameObject);
        }
        activeCards.Clear();
    }

    public Player GetCurrentPlayer()
    {
        if (players.Count == 0 || currentPlayerIndex < 0 || currentPlayerIndex >= players.Count)
            return null;
        return players[currentPlayerIndex];
    }

    public bool IsPlayerTurn(Player player)
    {
        return player != null && player.Name == GetCurrentPlayer().Name && player.IsActive;
    }

    void CheckGameEnd()
    {
        var activePlayers = players.Where(p => p.IsActive).ToList();
        if (activePlayers.Count <= 1)
        {
            EndGame(activePlayers.FirstOrDefault());
        }
    }

    void EndGame(Player winner)
    {
        isGameActive = false;
        string result = winner != null ? $"{winner.Name} wins!" : "Game ended in a draw";
        Debug.Log(result);

        if (endGamePanel != null)
        {
            endGamePanel.SetActive(true);
            TMP_Text text = endGamePanel.GetComponentInChildren<TMP_Text>();
            if (text != null)
            {
                text.text = result;
            }
        }

        if (photonView != null && PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("SyncGameEnd", RpcTarget.All, winner != null ? winner.Name : "");
        }
    }

    [PunRPC]
    void SyncGameEnd(string winnerName)
    {
        isGameActive = false;
        string result = !string.IsNullOrEmpty(winnerName) ? $"{winnerName} wins!" : "Game ended in a draw";

        if (endGamePanel != null)
        {
            endGamePanel.SetActive(true);
            TMP_Text text = endGamePanel.GetComponentInChildren<TMP_Text>();
            if (text != null)
            {
                text.text = result;
            }
        }
    }

    public List<Player> GetActivePlayers()
    {
        return players.Where(p => p.IsActive).ToList();
    }

    public List<Player> GetAllPlayers()
    {
        return new List<Player>(players);
    }
}
