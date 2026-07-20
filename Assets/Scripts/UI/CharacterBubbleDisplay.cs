using System.Collections;
using TMPro;
using UnityEngine;

public class CharacterBubbleDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject bubbleRoot;
    [SerializeField] private TMP_Text bubbleText;

    [Header("Settings")]
    [SerializeField] private float displayTime = 10f;
    [SerializeField] private bool faceCamera = true;

    private Coroutine hideCoroutine;

    private void Awake()
    {
        if (bubbleRoot != null)
            bubbleRoot.SetActive(false);
    }

    private IEnumerator Start()
    {
        while (ChatManager.Instance == null)
            yield return null;

        ChatManager.Instance.OnMessageAdded += OnMessageAdded;
    }

    private void OnDestroy()
    {
        if (ChatManager.Instance != null)
            ChatManager.Instance.OnMessageAdded -= OnMessageAdded;
    }

    private void LateUpdate()
    {
        if (!faceCamera)
            return;

        Camera cam = Camera.main;

        if (cam == null)
            return;

        // カメラの画面と常に平行にする
        transform.rotation = cam.transform.rotation;

        //transform.rotation =
        //    cam.transform.rotation * Quaternion.Euler(0f, 180f, 0f);
    }

    private void OnMessageAdded(ChatMessage message)
    {
        if (message == null)
            return;

        if (message.role != "assistant")
                return;

        Show(message.content);
    }

    public void Show(string text)
    {
        if (bubbleRoot == null || bubbleText == null)
        {
            Debug.LogWarning(
                "[CharacterBubbleDisplay] Bubble RootまたはBubble Textが未設定です。"
            );
            return;
        }

        if (string.IsNullOrWhiteSpace(text))
            return;

        string displayText = text.Trim();

        // 頭上の吹き出しだけ短縮する
        // ChatMessage.contentは変更しないので、履歴には全文が残る
        const int maxCharacters = 28;

        if (displayText.Length > maxCharacters)
        {
            displayText =
                displayText.Substring(0, maxCharacters).TrimEnd() + "…";
        }

        bubbleRoot.SetActive(true);
        bubbleText.text = displayText;
        bubbleText.ForceMeshUpdate();

        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);

        hideCoroutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(displayTime);

        if (bubbleRoot != null)
            bubbleRoot.SetActive(false);

        hideCoroutine = null;
    }
}