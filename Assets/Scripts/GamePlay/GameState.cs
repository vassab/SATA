/*using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// Enum для определения текущей фазы игры
public enum GamePhase
{
    Lobby,              // Ожидание игроков в лобби
    DealingCards,       // Раздача карт
    CardDiscard,        // Фаза сброса карт игроками
    Voting,             // Фаза голосования
    RoundEnd,           // Конец раунда, подсчет результатов
    GameEnd             // Игра завершена, определены победители
}

// Класс для управления общим состоянием игры
// Этот класс может быть MonoBehaviour и существовать как синглтон в сцене, 
// либо быть обычным классом, управляемым другим менеджером.
// Для начала сделаем его обычным классом.
public class GameState
{
    [Tooltip("Список всех игроков в текущей игре")]
    public List<PlayerData> players;

    [Tooltip("Текущая активная катастрофа (данные карты катастрофы или просто текстовое описание)")]
    public CardData currentCatastrophe; // Предполагаем, что катастрофы тоже могут быть CardData, либо это будет отдельный класс
                                        // Или просто string, если это только текст, как было решено.
                                        // Для гибкости оставим CardData, но можно будет упростить до string.
    public string currentCatastropheDescription; // Если катастрофа - это просто текст


    [Tooltip("Текущая фаза игры (лобби, голосование и т.д.)")]
    public GamePhase currentPhase;

    [Tooltip("Идентификатор игрока, чей сейчас ход (например, для сброса карты)")]
    public string currentPlayerTurnId;

    [Tooltip("Номер текущего раунда игры")]
    public int currentRound;

    // Настройки игры, задаваемые хостом
    [Tooltip("Время на выбор карты для сброса (в секундах)")]
    public float timeToDiscardCard = 30f;

    [Tooltip("Время на голосование (в секундах)")]
    public float timeToVote = 60f;

    [Tooltip("Количество карт 'Факт', выдаваемых каждому игроку")]
    public int numberOfFactCards = 1;

    [Tooltip("Количество карт 'Багаж', выдаваемых каждому игроку")]
    public int numberOfBaggageCards = 1;

    public enum TieBreakRule
    {
        RevoteOnTied, // Повторное голосование по тем, кто набрал ничью
        NoOneEliminated // Никто не выбывает
    }
    [Tooltip("Правило разрешения ничьей при голосовании")]
    public TieBreakRule tieBreakRule = TieBreakRule.RevoteOnTied;

    // Конструктор
    public GameState()
    {
        players = new List<PlayerData>();
        currentPhase = GamePhase.Lobby;
        currentRound = 0;
        // Инициализация настроек по умолчанию может быть здесь или при создании лобби
    }

    // Метод для добавления нового игрока
    public void AddPlayer(PlayerData player)
    {
        if (player != null && !players.Any(p => p.playerId == player.playerId))
        {
            players.Add(player);
        }
    }

    // Метод для удаления игрока (например, при отключении)
    public void RemovePlayer(string playerId)
    {
        players.RemoveAll(p => p.playerId == playerId);
    }

    // Метод для начала игры (переход из лобби)
    public void StartGame(List<CardData> allAvailableCatastrophes)
    {
        if (currentPhase == GamePhase.Lobby && players.Count >= 4) // Минимальное количество игроков для старта
        {
            currentRound = 1;
            // Здесь должна быть логика раздачи карт игрокам
            // DealInitialCards(); // Потребуется список всех доступных карт для раздачи
            
            // Определение первой катастрофы
            // SelectRandomCatastrophe(allAvailableCatastrophes);
            
            // Определение первого ходящего
            // SetInitialPlayerTurn();
            
            currentPhase = GamePhase.DealingCards; // Или сразу CardDiscard, если раздача мгновенная
            Debug.Log("Игра началась! Раунд: " + currentRound);
        }
    }

    // Метод для установки текущей катастрофы (если это просто текст)
    public void SetCatastrophe(string catastropheDescription)
    {
        currentCatastropheDescription = catastropheDescription;
        Debug.Log("Новая катастрофа: " + currentCatastropheDescription);
    }

    // Метод для перехода к следующей фазе игры
    public void AdvancePhase(GamePhase nextPhase)
    {
        currentPhase = nextPhase;
        Debug.Log("Новая фаза игры: " + currentPhase.ToString());
        // Дополнительная логика для каждой фазы может быть здесь или вызываться отдельно
        // Например, сброс таймеров, определение следующего игрока и т.д.
    }

    // Метод для определения следующего игрока для хода
    public PlayerData GetNextPlayerTurn(string previousPlayerId)
    {
        List<PlayerData> activePlayers = players.Where(p => p.status == PlayerStatus.Active).ToList();
        if (activePlayers.Count == 0) return null;

        int currentIndex = activePlayers.FindIndex(p => p.playerId == previousPlayerId);
        int nextIndex = (currentIndex + 1) % activePlayers.Count;
        currentPlayerTurnId = activePlayers[nextIndex].playerId;
        return activePlayers[nextIndex];
    }

    // Метод для получения игрока по ID
    public PlayerData GetPlayerById(string playerId)
    {
        return players.FirstOrDefault(p => p.playerId == playerId);
    }

    // Метод для сброса состояния раунда (голоса, возможно, какие-то временные эффекты)
    public void ResetRoundState()
    {
        foreach (var player in players)
        {
            player.ResetVotes();
        }
        // currentPlayerTurnId = null; // Или установить на первого игрока нового раунда
    }

    // Другие методы для управления ходом игры, голосованием, применением правил и т.д.
    // будут добавляться по мере разработки.
}*/

