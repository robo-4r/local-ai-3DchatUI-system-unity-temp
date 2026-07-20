using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// チャットUI管理 - 極限シンプル版
/// </summary>
public class ChatUIManager : MonoBehaviour
{
    [Header("UI参照")]
    [SerializeField] private Transform messageContainer;   // Content (VLG付き)
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button sendButton;
    [SerializeField] private GameObject typingIndicator;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("接続状態")]
    [SerializeField] private Image connectionIndicator;
    [SerializeField] private Color connectedColor = new Color(0.2f, 0.8f, 0.2f);
    [SerializeField] private Color disconnectedColor = new Color(0.8f, 0.2f, 0.2f);

    // カラー定義
    private static readonly Color USER_BUBBLE_COLOR = new Color(0.22f, 0.35f, 0.55f);
    private static readonly Color AI_BUBBLE_COLOR = new Color(0.28f, 0.30f, 0.36f);
    private static readonly Color TEXT_COLOR = Color.white;

    // フォント
    private TMP_FontAsset japaneseFont;

    private void Awake()
    {
        // Inspector の参照が外れても、Build Chat Scene 既定の Hierarchy 名なら復旧する
        WireMissingReferencesFromHierarchy();
    }

    private void WireMissingReferencesFromHierarchy()
    {
        Transform root = transform;
        if (messageContainer == null)
        {
            var t = root.Find("MessageArea/ScrollView/Viewport/Content");
            if (t != null) messageContainer = t;
        }
        if (scrollRect == null)
        {
            var t = root.Find("MessageArea/ScrollView");
            if (t != null) scrollRect = t.GetComponent<ScrollRect>();
        }
        if (inputField == null)
        {
            var t = root.Find("InputArea/InputField");
            if (t != null) inputField = t.GetComponent<TMP_InputField>();
        }
        if (sendButton == null)
        {
            var t = root.Find("InputArea/SendButton");
            if (t != null) sendButton = t.GetComponent<Button>();
        }
        if (typingIndicator == null)
        {
            var t = root.Find("MessageArea/TypingIndicator");
            if (t != null) typingIndicator = t.gameObject;
        }
        if (statusText == null)
        {
            var t = root.Find("MessageArea/StatusText");
            if (t != null) statusText = t.GetComponent<TextMeshProUGUI>();
        }
        if (connectionIndicator == null)
        {
            var t = root.Find("Header/ConnectionIndicator");
            if (t != null) connectionIndicator = t.GetComponent<Image>();
        }
    }

    private void Start()
    {
        japaneseFont = FindJapaneseFont();
        Debug.Log($"[ChatUI] フォント: {(japaneseFont != null ? japaneseFont.name : "なし")}");

        if (sendButton == null || inputField == null)
        {
            Debug.LogError("[ChatUI] SendButton または InputField が未設定です。Canvas がルートで、InputArea/InputField と InputArea/SendButton があるか確認するか、LocalAI → Auto Wire References を実行してください。");
            return;
        }

        if (ChatManager.Instance == null)
        {
            Debug.LogError("[ChatUI] ChatManager が見つかりません。シーンに [GameManager]（ChatManager 付き）を置いてください。");
            return;
        }

        sendButton.onClick.AddListener(OnSendClicked);
        inputField.onSubmit.AddListener((_) => OnSendClicked());

        ChatManager.Instance.OnMessageAdded += OnMessageAdded;
        ChatManager.Instance.OnSendingStateChanged += OnSendingStateChanged;
        ChatManager.Instance.OnError += OnError;

        if (typingIndicator != null)
            typingIndicator.SetActive(false);

        CheckConnection();
    }

    private void OnDestroy()
    {
        if (ChatManager.Instance != null)
        {
            ChatManager.Instance.OnMessageAdded -= OnMessageAdded;
            ChatManager.Instance.OnSendingStateChanged -= OnSendingStateChanged;
            ChatManager.Instance.OnError -= OnError;
        }
    }

    // ========== 送信 ==========

    private void OnSendClicked()
    {
        if (inputField == null || ChatManager.Instance == null) return;

        string text = inputField.text.Trim();
        if (string.IsNullOrEmpty(text)) return;

        inputField.text = "";
        inputField.ActivateInputField();

        ChatManager.Instance.SendMessage(text);
    }

    // ========== メッセージ表示 ==========

    private void OnMessageAdded(ChatMessage message)
    {
        Debug.Log($"[ChatUI] OnMessageAdded: role={message.role}, content={message.content}");

        if (messageContainer == null)
        {
            Debug.LogError("[ChatUI] messageContainer が null！");
            return;
        }

        bool isUser = message.role == "user";
        AddBubble(message.content, isUser, message.modelUsed, message.contextUsed);
    }

    /// <summary>
    /// バブルを追加
    /// 構造: Content(VLG) → Row(LayoutElement のみ・高さ＝吹き出し) → Bubble(アンカーで左右寄せ) → Text
    /// 行に HLG を付けない（VLG+CSF とネストすると行高が潰れてメッセージが縦に重なることがある）
    /// </summary>
    /// <summary>
    /// TMP が本文中の &lt; &gt; をリッチタグと誤解して描画が壊れないよう、本文だけ noparse で包む
    /// </summary>
    private static string TmpWrapBodyForRichSuffix(string body)
    {
        if (body == null) body = "";
        body = body.Replace("</noparse>", "");
        return "<noparse>" + body + "</noparse>";
    }

    private void AddBubble(
        string text,
        bool isUser,
        string modelUsed,
        bool contextUsed)
    {
        if (messageContainer == null)
        {
            Debug.LogError("[ChatUI] messageContainer が設定されていません。");
            return;
        }

        text ??= "";

        // -------------------------
        // サイズ設定
        // -------------------------

        const float horizontalPadding = 18f;
        const float topPadding = 12f;
        const float bottomPadding = 12f;

        const float minimumBubbleWidth = 90f;
        const float minimumBubbleHeight = 52f;

        const float sideMargin = 12f;
        const float rowSpacing = 12f;

        Canvas.ForceUpdateCanvases();

        float contentWidth = GetContentWidth();

        // 吹き出しの最大幅
        float maximumBubbleWidth =
        Mathf.Max(160f, contentWidth * 0.78f);

        float maximumTextWidth =
            maximumBubbleWidth - horizontalPadding * 2f;

        // -------------------------
        // Rowを作成
        // -------------------------

        GameObject rowObj = new GameObject(
            isUser ? "UserRow" : "AIRow",
            typeof(RectTransform),
            typeof(LayoutElement)
        );

        rowObj.transform.SetParent(messageContainer, false);

        RectTransform rowRT =
            rowObj.GetComponent<RectTransform>();

        LayoutElement rowLayout =
            rowObj.GetComponent<LayoutElement>();

        rowRT.anchorMin = new Vector2(0f, 1f);
        rowRT.anchorMax = new Vector2(1f, 1f);
        rowRT.pivot = new Vector2(0.5f, 1f);
        rowRT.sizeDelta = Vector2.zero;

        rowLayout.flexibleWidth = 1f;
        rowLayout.flexibleHeight = 0f;

        // -------------------------
        // Bubbleを作成
        // -------------------------

        GameObject bubbleObj = new GameObject(
            isUser ? "UserBubble" : "AIBubble",
            typeof(RectTransform),
            typeof(Image)
        );

        bubbleObj.transform.SetParent(rowObj.transform, false);

        RectTransform bubbleRT =
            bubbleObj.GetComponent<RectTransform>();

        Image bubbleImage =
            bubbleObj.GetComponent<Image>();

        bubbleImage.color =
            isUser ? USER_BUBBLE_COLOR : AI_BUBBLE_COLOR;

        bubbleImage.raycastTarget = false;

        // -------------------------
        // Textを作成
        // -------------------------

        GameObject textObj = new GameObject(
            "Text",
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );

        textObj.transform.SetParent(bubbleObj.transform, false);

        RectTransform textRT =
            textObj.GetComponent<RectTransform>();

        TextMeshProUGUI tmp =
            textObj.GetComponent<TextMeshProUGUI>();

        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.pivot = new Vector2(0.5f, 0.5f);

        textRT.offsetMin = new Vector2(
            horizontalPadding,
            bottomPadding
        );

        textRT.offsetMax = new Vector2(
            -horizontalPadding,
            -topPadding
        );

        tmp.fontSize = 18f;
        tmp.color = TEXT_COLOR;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.richText = true;
        tmp.raycastTarget = false;

        if (japaneseFont != null)
            tmp.font = japaneseFont;

        // -------------------------
        // 本文と補足情報
        // -------------------------

        string timeString =
            System.DateTime.Now.ToString("HH:mm");

        string suffix =
            "\n<size=11><color=#A0A5B0>";

        if (!isUser && !string.IsNullOrWhiteSpace(modelUsed))
        {
            suffix += modelUsed;

            if (contextUsed)
                suffix += " | 履歴参照";

            suffix += "  ";
        }

        suffix += timeString;
        suffix += "</color></size>";

        string fullText =
            TmpWrapBodyForRichSuffix(text) + suffix;

        tmp.text = fullText;

        // -------------------------
        // 横幅を計算
        // -------------------------

        /*
         * 最初は幅制限なしで、文章が本来必要とする横幅を取得します。
         * その後、最大幅以内に制限します。
         */
        Vector2 naturalSize = tmp.GetPreferredValues(
            fullText,
            Mathf.Infinity,
            Mathf.Infinity
        );

        float bubbleWidth = Mathf.Clamp(
            naturalSize.x + horizontalPadding * 2f,
            minimumBubbleWidth,
            maximumBubbleWidth
        );

        float textWidth = Mathf.Max(
            1f,
            bubbleWidth - horizontalPadding * 2f
        );

        // -------------------------
        // 確定した横幅で高さを計算
        // -------------------------

        Vector2 wrappedTextSize = tmp.GetPreferredValues(
            fullText,
            textWidth,
            Mathf.Infinity
        );

        float bubbleHeight = Mathf.Max(
            wrappedTextSize.y + topPadding + bottomPadding,
            minimumBubbleHeight
        );

        // -------------------------
        // Bubbleを左右へ配置
        // -------------------------

        if (isUser)
        {
            bubbleRT.anchorMin = new Vector2(1f, 0.5f);
            bubbleRT.anchorMax = new Vector2(1f, 0.5f);
            bubbleRT.pivot = new Vector2(1f, 0.5f);

            bubbleRT.anchoredPosition =
                new Vector2(-sideMargin, 0f);
        }
        else
        {
            bubbleRT.anchorMin = new Vector2(0f, 0.5f);
            bubbleRT.anchorMax = new Vector2(0f, 0.5f);
            bubbleRT.pivot = new Vector2(0f, 0.5f);

            bubbleRT.anchoredPosition =
                new Vector2(sideMargin, 0f);
        }

        bubbleRT.sizeDelta =
            new Vector2(bubbleWidth, bubbleHeight);

        // Rowは吹き出しより少し高くし、メッセージ間隔を作る
        float rowHeight = bubbleHeight + rowSpacing;

        rowLayout.minHeight = rowHeight;
        rowLayout.preferredHeight = rowHeight;

        // -------------------------
        // 初回レイアウト更新
        // -------------------------

        Canvas.ForceUpdateCanvases();

        tmp.ForceMeshUpdate(true);

        LayoutRebuilder.ForceRebuildLayoutImmediate(
            bubbleRT
        );

        LayoutRebuilder.ForceRebuildLayoutImmediate(
            rowRT
        );

        RectTransform contentRT =
            messageContainer as RectTransform;

        if (contentRT != null)
        {
         LayoutRebuilder.ForceRebuildLayoutImmediate(
                contentRT
            );
        }

        // TMPとContentSizeFitterの更新遅延に備えて次フレームにも再構築
        StartCoroutine(
            FinalizeBubbleLayout(
                rowRT,
                bubbleRT,
                tmp,
                fullText,
                horizontalPadding,
                topPadding,
                bottomPadding,
                minimumBubbleHeight,
                rowSpacing
            )
        );

        Debug.Log(
            $"[ChatUI] バブル追加: " +
            $"isUser={isUser}, " +
            $"width={bubbleWidth:F1}, " +
            $"height={bubbleHeight:F1}"
        );
    }

    private IEnumerator FinalizeBubbleLayout(
        RectTransform rowRT,
        RectTransform bubbleRT,
        TextMeshProUGUI tmp,
        string fullText,
        float horizontalPadding,
        float topPadding,
        float bottomPadding,
        float minimumBubbleHeight,
        float rowSpacing)
    {
        // UI生成直後のレイアウト処理を待つ
        yield return null;

        if (rowRT == null ||
            bubbleRT == null ||
            tmp == null)
        {
            yield break;
        }

        Canvas.ForceUpdateCanvases();

        float textWidth = Mathf.Max(
            1f,
            bubbleRT.rect.width - horizontalPadding * 2f
        );

        // 実際に確定した横幅でもう一度高さを取得
        Vector2 preferredSize = tmp.GetPreferredValues(
            fullText,
            textWidth,
            Mathf.Infinity
        );

        float correctedBubbleHeight = Mathf.Max(
            preferredSize.y + topPadding + bottomPadding,
            minimumBubbleHeight
        );

        bubbleRT.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            correctedBubbleHeight
        );

        LayoutElement rowLayout =
            rowRT.GetComponent<LayoutElement>();

        if (rowLayout != null)
        {
            float correctedRowHeight =
                correctedBubbleHeight + rowSpacing;

            rowLayout.minHeight = correctedRowHeight;
            rowLayout.preferredHeight = correctedRowHeight;
        }

        tmp.ForceMeshUpdate(true);

        LayoutRebuilder.ForceRebuildLayoutImmediate(
            bubbleRT
        );

        LayoutRebuilder.ForceRebuildLayoutImmediate(
            rowRT
        );

        RectTransform contentRT =
            messageContainer as RectTransform;

        if (contentRT != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(
             contentRT
            );
        }

        // Contentの高さが反映されるのを待つ
        yield return null;

    Canvas.ForceUpdateCanvases();

    if (contentRT != null)
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(
            contentRT
        );
    }

    if (scrollRect != null)
    {
        scrollRect.StopMovement();
        scrollRect.verticalNormalizedPosition = 0f;
    }
}





    private float GetContentWidth()
    {
        var rt = messageContainer as RectTransform;
        if (rt != null && rt.rect.width > 0)
            return rt.rect.width;
        // フォールバック: 画面幅の推定
        return Screen.width;
    }

    // ========== 状態表示 ==========

    private void OnSendingStateChanged(bool isSending)
    {
        if (sendButton != null) sendButton.interactable = !isSending;
        if (typingIndicator != null) typingIndicator.SetActive(isSending);
        if (statusText != null)
            statusText.text = isSending ? "応答待ち..." : "";
    }

    private void OnError(string error)
    {
        Debug.LogError($"[ChatUI] エラー: {error}");
        if (statusText != null)
        {
            statusText.text = "エラー: " + error;
            statusText.color = new Color(1f, 0.4f, 0.4f);
        }
    }

    // ========== 接続チェック ==========

    private void CheckConnection()
    {
        StartCoroutine(CheckConnectionCoroutine());
    }

    private IEnumerator CheckConnectionCoroutine()
    {
        var apiClient = ApiClient.Instance;
        if (apiClient == null)
        {
            Debug.LogError("[ChatUI] ApiClient が見つかりません");
            UpdateConnectionIndicator(false);
            yield break;
        }

        bool done = false;
        bool connected = false;

        apiClient.CheckHealth(
            onSuccess: (res) => { connected = true; done = true; },
            onError: (err) => { connected = false; done = true; }
        );

        while (!done) yield return null;

        UpdateConnectionIndicator(connected);
    }

    private void UpdateConnectionIndicator(bool isConnected)
    {
        if (connectionIndicator != null)
            connectionIndicator.color = isConnected ? connectedColor : disconnectedColor;
    }

    // ========== 日本語フォント検出 ==========

    private TMP_FontAsset FindJapaneseFont()
    {
        TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        foreach (var f in fonts)
        {
            string name = f.name.ToLower();
            if (name.Contains("noto") || name.Contains("japanese") || name.Contains("jp"))
                return f;
        }
        return null;
    }
}
