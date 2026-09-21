using TMPro;
using UnityEngine;

public class ChatPrefab : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _text;

    public void SetContent(string playerName, string message)
    {
        var safeName = SanitizeRichText(playerName);
        var safeMessage = SanitizeRichText(message);

        _text.text = $"<color=green><b>{safeName}:</b></color> {safeMessage}";
    }

    private string SanitizeRichText(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return input.Replace("<", "‹").Replace(">", "›");
    }
}