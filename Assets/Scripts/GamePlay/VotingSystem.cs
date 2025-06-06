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

        // ����������� ������ �������� �����������
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

        // ��������� UI ������
        if (votingPanel == null || playerButtonsContainer == null || playerVoteButtonPrefab == null)
        {
            Debug.LogError("VotingSystem: Missing UI references");
            return;
        }

        // ��������� ������ �������
        if (players == null || players.Count == 0)
        {
            Debug.LogError("VotingSystem: players list is null or empty!");
            return;
        }

        // ������ ������-������ ����� ������ �����������
        if (!PhotonNetwork.IsMasterClient) return;

        // �������������� ������ �����������
        activePlayers = new List<Player>(players);
        votes.Clear();
        isVotingActive = true;
        currentVotingTime = votingTime;
        totalVotes = 0;

        foreach (Player player in activePlayers)
        {
            if (player == null || string.IsNullOrEmpty(player.Name))
            {
                Debug.LogError("VotingSystem: Player is null or has no name � skipping.");
                continue;
            }
            votes[player.Name] = 0;
        }

        // ���������� RPC ��� ������������� ������ �����������
        photonView.RPC("SyncStartVoting", RpcTarget.All);
    }

    [PunRPC]
    void SyncStartVoting()
    {
        Debug.Log("VotingSystem: SyncStartVoting received");
        Debug.Log("[DEBUG] SyncStartVoting received on client.");

        // �������� ������ �������� ������� �� GameManager
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

        // �������������� ������ �����������
        votes.Clear();
        isVotingActive = true;
        currentVotingTime = votingTime;
        localVotedPlayer = null;
        totalVotes = 0;

        foreach (Player player in activePlayers)
        {
            if (player == null || string.IsNullOrEmpty(player.Name))
            {
                Debug.LogError("VotingSystem: Player is null or has no name � skipping.");
                continue;
            }
            votes[player.Name] = 0;
        }

        // ���������� ������ �����������
        if (votingPanel != null)
        {
            votingPanel.SetActive(true);

            // ������� ������ �����������
            CreateVotingButtons();

            // ��������� UI �����������
            UpdateVotingUI();

            // ��������� ������
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
        // ��������� ������� ���������
        if (voter == null || target == null)
        {
            Debug.LogError("RegisterVote: voter or target is null!");
            return;
        }

        string voterName = voter.Name;

        // ���������, �� ��������� �� ��� ���� �����
        if (playersWhoVoted.Contains(voterName))
        {
            Debug.LogWarning($"{voterName} ��� ��������� � ���� ������!");
            return;
        }

        // ���������, ��� ���� ����������� �������
        if (!target.IsActive)
        {
            Debug.LogWarning($"{voterName} �������� ������������� ������ ����������� ������ {target.Name}!");
            return;
        }

        // ���������, ��� ���������� �� �������� ��� �� ����
        if (voter == target)
        {
            Debug.LogWarning($"{voterName} �������� ������������� ������ ����!");
            return;
        }

        // ������������ �����
        playersWhoVoted.Add(voterName);
        target.AddVote();

        Debug.Log($"{voterName} ������������ ������ {target.Name}. ����� �������: {target.Votes}");

        // ��������� ���������� �����������
        CheckVotingCompletion();
    }

    public void ResetVoting()
    {
        playersWhoVoted.Clear();
        Debug.Log("������� ����������� ��������");
    }

    private void CheckVotingCompletion()
    {
        int activePlayers = GameManager.Instance.GetActivePlayers().Count;
        int votesCast = playersWhoVoted.Count;

        Debug.Log($"������������� {votesCast} �� {activePlayers} �������� �������");

        // ���� ������������� ��� �������� ������
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
            // ������� ��������� ������
            foreach (Transform child in playerButtonsContainer)
            {
                Destroy(child.gameObject);
            }

            // ������� ������ ��� ������� ��������� ������
            foreach (Player player in activePlayers)
            {
                // ���������� ���������� ������ (������ ���������� �� ����)
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
                    // �����: ������� ��������� ����� ���������� ��� ���������
                    Player targetPlayer = player;

                    // ������� ��� ������������ ��������� � ��������� �����
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

        // ���������, ��� ����������� �������
        if (!isVotingActive)
        {
            Debug.LogError("VotingSystem: Voting is not active!");
            return;
        }

        // ���������, ��� ����� ��� �� ���������
        if (localVotedPlayer != null)
        {
            Debug.LogError("VotingSystem: Player already voted!");
            return;
        }

        // ����������, �� ���� ������������ ��������� �����
        localVotedPlayer = targetPlayer;

        // �������������� ����� �� ����� ���������
        photonView.RPC("SyncVote", RpcTarget.All, PhotonNetwork.NickName, targetPlayer.Name);
    }

    // ����� ��� NPC, ����� ���������������� �����
    public void RegisterVote(Player targetPlayer)
    {
        Debug.Log($"VotingSystem: RegisterVote called for {targetPlayer.Name}");

        // ���������, ��� ����������� �������
        if (!isVotingActive) return;

        // �������� ��� NPC (��������������, ��� ���������� ������ ����������� NPC)
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

        // �������������� ����� NPC �� ����� ���������
        photonView.RPC("SyncVote", RpcTarget.All, npcName, targetPlayer.Name);
    }

    [PunRPC]
    void SyncVote(string voterName, string targetName)
    {
        Debug.Log($"VotingSystem: SyncVote received - {voterName} voted for {targetName}");

        try
        {
            // ������� ������ �� �����
            Player target = activePlayers.Find(p => p.Name == targetName);

            if (target == null)
            {
                Debug.LogError($"VotingSystem: Target player with name {targetName} not found!");
                return;
            }

            // ������������ �����
            if (votes.ContainsKey(target.Name))
            {
                votes[target.Name] = votes[target.Name] + 1;
            }
            else
            {
                Debug.LogError($"VotingSystem: Target name {targetName} not found in votes dictionary!");
                votes[target.Name] = 1; // ������� ������, ���� � ���
            }

            totalVotes++;

            // ��������� UI �����������
            UpdateVotingUI();

            // ���������, ��� �� �������������
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

        // ���������, ��� ����������� �������
        if (!isVotingActive) return;

        // ���������, ��� ����� ��� �� ���������
        if (localVotedPlayer != null) return;

        // ��������, ��� ��������� ����� ��������� �����������
        localVotedPlayer = new Player("Skip");

        // �������������� ������� ������ �� ����� ���������
        photonView.RPC("SyncSkipVote", RpcTarget.All, PhotonNetwork.NickName);
    }

    [PunRPC]
    void SyncSkipVote(string voterName)
    {
        Debug.Log($"VotingSystem: SyncSkipVote received - {voterName} skipped voting");

        // ����������� ������� �������
        totalVotes++;

        // ��������� UI �����������
        UpdateVotingUI();

        // ���������, ��� �� �������������
        CheckVotingComplete();
    }

    void CheckVotingComplete()
    {
        // ���������, ��� �� ������ �������������
        int expectedVotes = PhotonNetwork.CurrentRoom.PlayerCount;

        // ��������� ������ NPC, ���� ��� ����
        if (gameManager != null)
        {
            int npcCount = gameManager.GetActivePlayers().Count(p => p.Name.Contains("NPC"));
            expectedVotes += npcCount;
        }

        Debug.Log($"VotingSystem: Checking voting complete - {totalVotes}/{expectedVotes} votes");

        if (totalVotes >= expectedVotes)
        {
            // ������ ������-������ ��������� �����������
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

            // 1. �������� UI �����������
            if (votingPanel != null)
            {
                votingPanel.SetActive(false);
            }
            else
            {
                Debug.LogError("Voting panel reference is missing!");
            }

            // 2. ���������� ������������ ������
            Player eliminatedPlayer = GetVotingResult();
            Debug.Log($"Eliminated player: {(eliminatedPlayer != null ? eliminatedPlayer.Name : "none")}");

            // 3. ���������� GameManager (������ �� Master Client)
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
                    // Fallback: ��������� ��������� ����� ��������
                    GameManager.Instance?.StartNextRound();
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in CompleteVoting: {e.Message}\n{e.StackTrace}");

            // ��������� ������� � ���������� ������
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
            // ��������� ������ �����������
            if (votingStatusText != null)
            {
                int expectedVotes = PhotonNetwork.CurrentRoom.PlayerCount;
                votingStatusText.text = $"�������������: {totalVotes}/{expectedVotes}";
                Debug.Log($"[DEBUG] Voting status updated: {votingStatusText.text}");

                // ��������� ������ NPC, ���� ��� ����
                if (gameManager != null)
                {
                    int npcCount = gameManager.GetActivePlayers().Count(p => p.Name.Contains("NPC"));
                    expectedVotes += npcCount;
                }

                votingStatusText.text = $"�������������: {totalVotes}/{expectedVotes}";
            }

            // ��������� UI ������
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
                        // ���� ����� ��� ������������, ������ ������ ����������
                        button.interactable = (localVotedPlayer == null);

                        // ���������� ���������� ������� �� ����� ������
                        if (votes.ContainsKey(targetPlayer.Name))
                        {
                            buttonText.text = $"{targetPlayer.Name} ({votes[targetPlayer.Name]})";
                        }
                    }
                }
            }

            // ��������� ������ �������� �����������
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
            // ��������� UI �������
            if (timerText != null)
            {
                timerText.text = $"�����: {Mathf.CeilToInt(currentVotingTime)}";
                Debug.Log($"[DEBUG] Timer updated: {timerText.text}");
            }

            yield return new WaitForSeconds(1f);
            currentVotingTime -= 1f;

            // ���� ����� �������, ��������� �����������
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

        // ������� Player �� ������ �������� ������� �� �����
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
