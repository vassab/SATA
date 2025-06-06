using UnityEngine;
using System.Collections;
using System.Linq;
using Photon.Pun;

public class NPCController : MonoBehaviour
{
    public enum NPCStrategy { Random, Aggressive, Friendly }

    [Header("Settings")]
    public NPCStrategy strategy;
    public float minDecisionDelay = 0.5f;
    public float maxDecisionDelay = 2f;
    public string npcName = "NPC";
    public Player Player { get; private set; }

    private Player npcPlayer;
    private GameManager gameManager;
    private VotingSystem votingSystem;
    private bool hasActedThisRound;
    private string npcId;

    void Start()
    {
        gameManager = GameManager.Instance;
        votingSystem = FindObjectOfType<VotingSystem>();
        if (!gameManager) Debug.LogError("GameManager not found!");
        if (!votingSystem) Debug.LogError("VotingSystem not found!");
    }

    public void Initialize(Player player, string id)
    {
        npcPlayer = player;
        npcId = id;
        npcPlayer.Name = npcName;
        InvokeRepeating("CheckTurn", 2f, 1f);
    }

    private void CheckTurn()
    {
        if (!PhotonNetwork.IsMasterClient || hasActedThisRound) return;

        Debug.Log($"[NPCController] Checking turn: {npcPlayer.Name} | IsPlayerTurn: {gameManager.IsPlayerTurn(npcPlayer)}");
        
        if (gameManager.IsPlayerTurn(npcPlayer))
        {
            StartCoroutine(ExecuteTurn());
        }
    }

    private IEnumerator ExecuteTurn()
    {
        hasActedThisRound = true;
        yield return new WaitForSeconds(Random.Range(minDecisionDelay, maxDecisionDelay));

        if (gameManager.IsVotingPhase)
        {
            ExecuteVote();
        }
        else
        {
            ExecuteCardDiscard();
        }
    }

    private void ExecuteCardDiscard()
    {
        CardData card = ChooseCardToDiscard();
        if (card == null)
        {
            Debug.LogWarning($"{npcName} has no cards to discard");
            CompleteTurn();
            return;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            npcPlayer.RemoveCard(card);
            gameManager.RegisterDiscardedCard(npcPlayer, card);
            gameManager.RegisterPlayerAction(npcPlayer.Name);
            Debug.Log($"{npcName} discarded {card.cardName}");
        }

        CompleteTurn();
    }

    private void ExecuteVote()
    {
        Player target = ChooseVotingTarget();
        if (target == null) return;

        votingSystem.RegisterVote(npcPlayer, target);
        Debug.Log($"{npcName} voted against {target.Name}");
    }

    private CardData ChooseCardToDiscard()
    {
        var hand = npcPlayer?.GetHand();
        if (hand == null || hand.Count == 0) return null;

        switch (strategy)
        {
            case NPCStrategy.Aggressive:
                return hand.OrderBy(c => c.cardType == CardType.Health ? 0 : 1).FirstOrDefault();
            case NPCStrategy.Friendly:
                return hand.OrderByDescending(c => c.cardType == CardType.Health ? 1 : 0).FirstOrDefault();
            default:
                return hand[Random.Range(0, hand.Count)];
        }
    }

    private Player ChooseVotingTarget()
    {
        return gameManager.GetActivePlayers()
            .Where(p => p != npcPlayer && p.IsActive)
            .OrderBy(p => strategy == NPCStrategy.Aggressive ?
                p.GetCardsOfType(CardType.Health).Count :
                -p.GetCardsOfType(CardType.Health).Count)
            .FirstOrDefault();
    }

    public void StartNPCTurn()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.Log($"{npcPlayer.Name} cannot act - not master client");
            return;
        }

        if (hasActedThisRound)
        {
            Debug.Log($"{npcPlayer.Name} already acted this round");
            return;
        }

        Debug.Log($"Starting NPC turn for {npcPlayer.Name}");
        StartCoroutine(ExecuteNPCTurn());
    }

    private IEnumerator ExecuteNPCTurn()
    {
        // Небольшая задержка для реалистичности
        yield return new WaitForSeconds(Random.Range(0.5f, 1.5f));

        if (GameManager.Instance.IsVotingPhase)
        {
            ExecuteVote();
        }
        else
        {
            ExecuteCardDiscard();
        }
    }

    private void CompleteTurn()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            gameManager.EndTurn();
        }
    }

    public void ResetRoundFlags()
    {
        hasActedThisRound = false;
        Debug.Log($"{npcName} ready for new round");
    }

    void OnDisable()
    {
        CancelInvoke();
    }

    public Player GetPlayer() => Player;
}