using UnityEngine;
using UnityEngine.UI;

public class LobbyPlayerListItemUI : MonoBehaviour
{
    public Text playerNameText;
    public Text playerStatusText; // e.g., "Ready", "Not Ready"
    public Image hostIcon; // Optional: an icon to show who is the host

    public void Setup(string playerName, bool isReady, bool isHost = false)
    {
        if (playerNameText != null) playerNameText.text = playerName;
        if (playerStatusText != null) playerStatusText.text = isReady ? "<color=green>Готов</color>" : "<color=red>Не готов</color>";

        if (hostIcon != null)
        {
            hostIcon.gameObject.SetActive(isHost);
        }
    }
}

