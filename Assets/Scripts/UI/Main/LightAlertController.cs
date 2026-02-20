using UnityEngine;
using TMPro;
using TapHouse.Logging;

public class LightAlertController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private float displayDuration = 2f;

    void Start()
    {
        Destroy(gameObject, displayDuration);
    }

    public void SetMessage(string message)
    {
        if (messageText != null)
        {
            // LocalizedTextがStart()でテキストを上書きするのを防止
            var localizedText = messageText.GetComponent<LocalizedText>();
            if (localizedText != null)
            {
                Destroy(localizedText);
            }

            messageText.text = message;
        }
        else
        {
            GameLogger.LogWarning(LogCategory.UI, "MessageText is null in LightAlertController");
        }
    }
}