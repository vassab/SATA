/*using UnityEngine;
using System.Collections.Generic;

// Класс для хранения данных игрока
public class PlayerData
{
    [Tooltip("Уникальный идентификатор игрока (может быть сетевым ID или сгенерированным)")]
    public string playerId;

    [Tooltip("Имя игрока, которое он вводит в главном меню")]
    public string playerName;

    [Tooltip("Список карт на руках у игрока")]
    public List<CardData> handCards;

    [Tooltip("Статус игрока (например, активен, выбыл)")]
    public PlayerStatus status;

    [Tooltip("Количество голосов, полученных игроком в текущем раунде голосования")]
    public int votesReceived;

    [Tooltip("Флаг, указывающий, является ли этот игрок хостом лобби")]
    public bool isHost;

    // Конструктор для создания нового игрока
    public PlayerData(string id, string name, bool host = false)
    {
        playerId = id;
        playerName = name;
        handCards = new List<CardData>();
        status = PlayerStatus.Active; // По умолчанию игрок активен
        votesReceived = 0;
        isHost = host;
    }

    // Метод для добавления карты в руку игрока
    public void AddCardToHand(CardData card)
    {
        if (card != null)
        {
            handCards.Add(card);
        }
    }

    // Метод для удаления карты из руки игрока (например, при сбросе)
    public bool RemoveCardFromHand(CardData card)
    {
        if (card != null && handCards.Contains(card))
        {
            handCards.Remove(card);
            return true;
        }
        return false;
    }

    // Метод для сброса голосов (например, в начале нового раунда)
    public void ResetVotes()
    {
        votesReceived = 0;
    }

    // Метод для изменения статуса игрока
    public void SetStatus(PlayerStatus newStatus)
    {
        status = newStatus;
    }
}

// Enum для статуса игрока
public enum PlayerStatus
{
    Active,         // Активен в игре
    Eliminated,     // Выбыл из игры
    Disconnected    // Отключился
}*/

