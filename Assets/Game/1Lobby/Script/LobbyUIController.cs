using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 대기실 UI — IP 표시, 방 생성/참가, 준비, 게임 시작.
/// </summary>
public class LobbyUIController : MonoBehaviour
{
    private LobbyNetworkManager network;
    private string localIp;

    private TextMeshProUGUI localIpText;
    private TextMeshProUGUI statusText;
    private TextMeshProUGUI playerListText;
    private TextMeshProUGUI errorText;

    private TMP_InputField nameInput;
    private TMP_InputField portInput;
    private TMP_InputField joinIpInput;

    private GameObject connectPanel;
    private GameObject roomPanel;
    private Button readyButton;
    private Button startButton;

    private bool isReady;

    private void Start()
    {
        EnsureEventSystem();
        localIp = LobbyNetworkAddress.GetLocalIPv4();
        BuildUI();
        BindNetwork();
        ShowConnectPanel();

        var bootstrapError = network?.ConsumePendingBootstrapError();
        if (!string.IsNullOrWhiteSpace(bootstrapError))
            ShowError(bootstrapError);
    }

    private void OnDestroy()
    {
        if (network == null)
            return;

        network.OnPlayerListChanged -= RefreshPlayerList;
        network.OnStatusChanged -= SetStatus;
        network.OnError -= ShowError;
        network.OnLeftRoom -= ShowConnectPanel;
        network.OnGameStarting -= () => SetStatus("게임 씬으로 이동 중...");
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        var eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private void BindNetwork()
    {
        network = LobbyNetworkManager.Instance;
        if (network == null)
        {
            var managerObject = new GameObject("LobbyNetworkManager");
            network = managerObject.AddComponent<LobbyNetworkManager>();
        }

        network.OnPlayerListChanged += RefreshPlayerList;
        network.OnStatusChanged += SetStatus;
        network.OnError += ShowError;
        network.OnLeftRoom += ShowConnectPanel;
        network.OnGameStarting += () => SetStatus("게임 씬으로 이동 중...");
    }

    private void BuildUI()
    {
        var canvasObject = new GameObject("LobbyCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var background = CreatePanel("Background", canvas.transform, new Color(0.08f, 0.1f, 0.16f, 1f));
        Stretch(background);

        CreateLabel("Title", background.transform, "멀티플레이 대기실", 42,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(900f, 60f));

        localIpText = CreateLabel("LocalIp", background.transform, $"내 IP: {localIp}", 24,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(900f, 36f));

        var copyIpButton = CreateButton("CopyIpButton", background.transform, "IP 복사",
            new Vector2(0f, -140f), new Vector2(140f, 36f), new Color(0.3f, 0.35f, 0.45f, 1f));
        copyIpButton.onClick.AddListener(CopyLocalIp);

        statusText = CreateLabel("Status", background.transform, "대기실", 22,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -185f), new Vector2(900f, 32f));

        errorText = CreateLabel("Error", background.transform, "", 20,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(900f, 32f));
        errorText.color = new Color(1f, 0.45f, 0.45f, 1f);

        connectPanel = CreatePanel("ConnectPanel", background.transform, new Color(0f, 0f, 0f, 0.2f));
        SetupCenterPanel(connectPanel, 460f);

        CreateLabel("NameLabel", connectPanel.transform, "닉네임", 22,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-220f, -40f), new Vector2(160f, 30f)).alignment = TextAlignmentOptions.MidlineLeft;
        nameInput = CreateInputField("NameInput", connectPanel.transform, "Player", new Vector2(120f, -40f), new Vector2(360f, 44f));

        CreateLabel("PortLabel", connectPanel.transform, "포트", 22,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-220f, -110f), new Vector2(160f, 30f)).alignment = TextAlignmentOptions.MidlineLeft;
        portInput = CreateInputField("PortInput", connectPanel.transform, LobbyNetworkManager.DefaultPort.ToString(), new Vector2(120f, -110f), new Vector2(360f, 44f));
        portInput.contentType = TMP_InputField.ContentType.IntegerNumber;

        CreateLabel("JoinIpLabel", connectPanel.transform, "호스트 IP", 22,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-220f, -180f), new Vector2(160f, 30f)).alignment = TextAlignmentOptions.MidlineLeft;
        joinIpInput = CreateInputField("JoinIpInput", connectPanel.transform, "127.0.0.1", new Vector2(120f, -180f), new Vector2(360f, 44f));

        var hostButton = CreateButton("HostButton", connectPanel.transform, "방 만들기 (호스트)",
            new Vector2(-150f, -300f), new Vector2(260f, 52f), new Color(0.2f, 0.55f, 0.95f, 1f));
        hostButton.onClick.AddListener(OnHostClicked);

        var joinButton = CreateButton("JoinButton", connectPanel.transform, "참가하기",
            new Vector2(150f, -300f), new Vector2(260f, 52f), new Color(0.2f, 0.75f, 0.45f, 1f));
        joinButton.onClick.AddListener(OnJoinClicked);

        roomPanel = CreatePanel("RoomPanel", background.transform, new Color(0f, 0f, 0f, 0.2f));
        SetupCenterPanel(roomPanel, 460f);
        roomPanel.SetActive(false);

        CreateLabel("PlayerListTitle", roomPanel.transform, "플레이어 목록", 24,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(640f, 36f));

        playerListText = CreateLabel("PlayerList", roomPanel.transform, "", 22,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -180f), new Vector2(640f, 240f));
        playerListText.alignment = TextAlignmentOptions.TopLeft;

        readyButton = CreateButton("ReadyButton", roomPanel.transform, "준비",
            new Vector2(-150f, -320f), new Vector2(220f, 52f), new Color(0.95f, 0.72f, 0.2f, 1f));
        readyButton.onClick.AddListener(OnReadyClicked);

        startButton = CreateButton("StartButton", roomPanel.transform, "게임 시작",
            new Vector2(150f, -320f), new Vector2(220f, 52f), new Color(0.2f, 0.55f, 0.95f, 1f));
        startButton.onClick.AddListener(OnStartClicked);

        var leaveButton = CreateButton("LeaveButton", roomPanel.transform, "나가기",
            new Vector2(0f, -380f), new Vector2(220f, 44f), new Color(0.75f, 0.25f, 0.25f, 1f));
        leaveButton.onClick.AddListener(() => network.LeaveRoom());
    }

    private void CopyLocalIp()
    {
        GUIUtility.systemCopyBuffer = localIp;
        SetStatus($"IP 복사됨: {localIp}");
    }

    private void ShowConnectPanel()
    {
        isReady = false;
        connectPanel.SetActive(true);
        roomPanel.SetActive(false);
        errorText.text = string.Empty;
        SetStatus("대기실");
        localIpText.text = $"내 IP: {localIp}  (다른 사람에게 이 주소를 알려주세요)";
        UpdateReadyButtonLabel();
    }

    private void ShowRoomPanel()
    {
        connectPanel.SetActive(false);
        roomPanel.SetActive(true);
        errorText.text = string.Empty;
        SyncReadyButtonFromNetwork();
        RefreshPlayerList();
    }

    private void OnHostClicked()
    {
        if (!TryGetPort(out var port))
            return;

        network.LocalPlayerName = nameInput.text;
        network.HostRoom(port, nameInput.text);
        ShowRoomPanel();
        SetStatus($"방 생성 — TCP {port}, 게임 UDP {CwslGameConstants.GameNetcodePort} 개방 필요");
    }

    private void OnJoinClicked()
    {
        if (!TryGetPort(out var port))
            return;

        network.LocalPlayerName = nameInput.text;
        network.JoinRoom(joinIpInput.text, port, nameInput.text);
    }

    private void OnReadyClicked()
    {
        isReady = !isReady;
        UpdateReadyButtonLabel();
        network.SetReady(isReady);
        UpdateStartButton();
    }

    private void OnStartClicked() => network.StartGame();

    private bool TryGetPort(out int port)
    {
        if (int.TryParse(portInput.text, out port))
            return true;

        ShowError("포트 번호를 확인해 주세요.");
        port = LobbyNetworkManager.DefaultPort;
        return false;
    }

    private void RefreshPlayerList()
    {
        if (network == null || playerListText == null)
            return;

        if (network.IsInRoom && connectPanel != null && connectPanel.activeSelf)
            ShowRoomPanel();

        var builder = new StringBuilder();
        foreach (var player in network.Players)
        {
            var hostMark = player.isHost ? " [호스트]" : string.Empty;
            var readyMark = player.isReady ? "준비 완료" : "대기 중";
            builder.AppendLine($"• {player.playerName}{hostMark} — {readyMark}");
        }

        if (builder.Length == 0)
            builder.Append("플레이어 없음");

        playerListText.text = builder.ToString();
        SyncReadyButtonFromNetwork();
        UpdateStartButton();
    }

    private void SyncReadyButtonFromNetwork()
    {
        if (network == null || readyButton == null)
            return;

        isReady = network.LocalReadyState;
        UpdateReadyButtonLabel();
    }

    private void UpdateReadyButtonLabel()
    {
        if (readyButton == null)
            return;

        var label = readyButton.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
            label.text = isReady ? "준비 취소" : "준비";
    }

    private void UpdateStartButton()
    {
        if (startButton == null || network == null)
            return;

        startButton.gameObject.SetActive(network.IsHost);
        startButton.interactable = network.CanStartGame();

        var label = startButton.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
        {
            label.text = string.IsNullOrWhiteSpace(network.GameSceneName)
                ? "게임 씬 미설정"
                : "게임 시작";
        }
    }

    private void SetStatus(string status)
    {
        if (statusText != null)
            statusText.text = status;
    }

    private void ShowError(string message)
    {
        if (errorText != null)
            errorText.text = message;
    }

    private static void SetupCenterPanel(GameObject panel, float height)
    {
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(720f, height);
        rect.anchoredPosition = new Vector2(0f, -30f);
    }

    private static GameObject CreatePanel(string name, Transform parent, Color color)
    {
        var panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(parent, false);
        panel.GetComponent<Image>().color = color;
        return panel;
    }

    private static void Stretch(GameObject panel)
    {
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static TextMeshProUGUI CreateLabel(
        string name, Transform parent, string text, float fontSize,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size)
    {
        var labelObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(parent, false);

        var rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        var label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        return label;
    }

    private static TMP_InputField CreateInputField(string name, Transform parent, string defaultText, Vector2 anchoredPosition, Vector2 size)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        root.transform.SetParent(parent, false);

        var rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        root.GetComponent<Image>().color = new Color(0.12f, 0.14f, 0.2f, 1f);

        var textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        textArea.transform.SetParent(root.transform, false);
        var textAreaRect = textArea.GetComponent<RectTransform>();
        Stretch(textArea);

        var placeholderObject = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
        placeholderObject.transform.SetParent(textArea.transform, false);
        var placeholder = placeholderObject.GetComponent<TextMeshProUGUI>();
        placeholder.text = defaultText;
        placeholder.fontSize = 20f;
        placeholder.color = new Color(1f, 1f, 1f, 0.35f);
        Stretch(placeholderObject);

        var textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(textArea.transform, false);
        var text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = defaultText;
        text.fontSize = 20f;
        text.color = Color.white;
        Stretch(textObject);

        var input = root.GetComponent<TMP_InputField>();
        input.textViewport = textAreaRect;
        input.textComponent = text;
        input.placeholder = placeholder;
        input.text = defaultText;
        return input;
    }

    private static Button CreateButton(string name, Transform parent, string label, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        var rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        buttonObject.GetComponent<Image>().color = color;

        var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(buttonObject.transform, false);
        var labelText = labelObject.GetComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = 22f;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = Color.white;
        Stretch(labelObject);

        return buttonObject.GetComponent<Button>();
    }
}
