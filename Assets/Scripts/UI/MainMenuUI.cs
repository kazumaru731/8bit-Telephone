using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KanjiFlipGame.Core;
using KanjiFlipGame.Network;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using System.Threading.Tasks;

namespace KanjiFlipGame.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject _mainMenuPanel;
        [SerializeField] private GameObject _selectionPanel;
        [SerializeField] private GameObject _friendMatchModePanel; // ホスト/ゲスト選択
        [SerializeField] private GameObject _friendMatchInputPanel; // ID入力
        [SerializeField] private GameObject _topicInputPanel;
        [SerializeField] private GameObject _roomWaitingPanel;
        [SerializeField] private GameObject _confirmationDialog;
        [SerializeField] private GameObject _consentDialog;

        [Header("Selection Buttons")]
        [SerializeField] private Button _randomMatchButton;
        [SerializeField] private Button _friendMatchMenuButton;

        private Button _localTestButton;

        [Header("Friend Match Mode Buttons")]
        [SerializeField] private Button _hostMatchButton;
        [SerializeField] private Button _guestMatchButton;
        [SerializeField] private Button _backToMainFromFriendModeButton;

        [Header("Friend Match Input UI")]
        [SerializeField] private TMP_InputField _roomIdInputField;
        [SerializeField] private Button _searchRoomButton;
        [SerializeField] private Button _backToFriendModeButton;

        [Header("Confirmation Dialog UI")]
        [SerializeField] private TextMeshProUGUI _confirmText;
        [SerializeField] private Button _confirmJoinButton;
        [SerializeField] private Button _cancelJoinButton;

        [Header("Consent Dialog UI")]
        [SerializeField] private TextMeshProUGUI _consetText;
        [SerializeField] private Button _consentYesButton;
        [SerializeField] private Button _consentNoButton;

        [Header("Waiting UI")]
        [SerializeField] private TextMeshProUGUI _roomIdText;
        [SerializeField] private TextMeshProUGUI _playerCountText;
        [SerializeField] private Button _readyButton;
        [SerializeField] private TextMeshProUGUI _readyButtonText;
        [SerializeField] private Button _startGameButton;
        [SerializeField] private Button _leaveRoomButton; // 追加
        [SerializeField] private TextMeshProUGUI _statusText;

        private SessionInfo _foundSession;
        private bool _isFriendMatch = false;
        private bool _isInitialized = false;
        private bool _hasAutoReadied = false;
        private bool _hasAutoStarted = false;

        void OnEnable()
        {
            Debug.Log($"MainMenuUI: OnEnable() called. GameObject: {gameObject.name}");
            // すでに初期化済みの場合はボタンリスナーの再登録のみ行い、パネル状態は変更しない
            if (_isInitialized)
            {
                RegisterButtonListeners();
                return;
            }
        }

        void Start()
        {
            Debug.Log("MainMenuUI: Start() called");
            InitializeUI();
            _isInitialized = true;

            if (IsTestMode())
            {
                Invoke(nameof(AutoStartMatch), 1.5f);
            }
        }

        private bool IsTestMode()
        {
            var args = System.Environment.GetCommandLineArgs();
            // -runAutoTest の自動テスト中は、通常のデモ用自動起動をバイパスする
            if (args.Contains("-runAutoTest")) return false;
            return args.Contains("-testMode");
        }

        private void AutoStartMatch()
        {
            Debug.Log("[TestMode] 自動でランダムマッチを開始します...");
            OnRandomMatchClicked();
        }

        /// <summary>
        /// 完全初期化（Start()から1度だけ呼ぶ）
        /// </summary>
        private void InitializeUI()
        {
            Debug.Log("MainMenuUI: Initializing UI...");
            RegisterButtonListeners();
            CreateLocalTestButton();

            // ネットワークイベントの登録
            if (NetworkLauncher.Instance != null)
            {
                NetworkLauncher.Instance.OnFriendRoomFound -= OnFriendRoomFound;
                NetworkLauncher.Instance.OnFriendRoomFound += OnFriendRoomFound;
                NetworkLauncher.Instance.OnFriendRoomNotFound -= OnFriendRoomNotFound;
                NetworkLauncher.Instance.OnFriendRoomNotFound += OnFriendRoomNotFound;
            }

            // 初期パネル表示: すでに接続中（シーン再ロード後）ならWaitingPanelを復元
            bool isConnected = NetworkLauncher.Instance != null && NetworkLauncher.Instance.Runner != null;
            if (isConnected)
            {
                // シーン再ロード後のリカバリ: 待機画面を復元
                ShowPanel(_roomWaitingPanel);
                Debug.Log("MainMenuUI: シーン再ロード後の復元 - RoomWaitingPanelを表示");
            }
            else
            {
                ShowPanel(_selectionPanel);
            }
            if (_confirmationDialog != null) _confirmationDialog.SetActive(false);
            if (_consentDialog != null) _consentDialog.SetActive(false);
            if (_startGameButton != null) _startGameButton.gameObject.SetActive(false);
            
            Debug.Log($"MainMenuUI: Initial panel set to {(isConnected ? _roomWaitingPanel?.name : _selectionPanel?.name)}");
        }

        private void CreateLocalTestButton()
        {
            if (_friendMatchMenuButton == null) return;

            // FriendMatchMenuButtonをベースに複製する
            GameObject testBtnGo = Instantiate(_friendMatchMenuButton.gameObject, _friendMatchMenuButton.transform.parent);
            testBtnGo.name = "LocalTestButton";

            var textComp = testBtnGo.GetComponentInChildren<TextMeshProUGUI>();
            if (textComp != null)
            {
                textComp.text = "1人テストプレイ";
            }

            _localTestButton = testBtnGo.GetComponent<Button>();
            _localTestButton.onClick.RemoveAllListeners();
            _localTestButton.onClick.AddListener(OnLocalTestPlayClicked);

            var parentLayout = _friendMatchMenuButton.transform.parent.GetComponent<LayoutGroup>();
            if (parentLayout == null)
            {
                var rectTrans = testBtnGo.GetComponent<RectTransform>();
                var origRect = _friendMatchMenuButton.GetComponent<RectTransform>();
                rectTrans.anchoredPosition = origRect.anchoredPosition + new Vector2(0, -100f);
            }
        }

        private void OnLocalTestPlayClicked()
        {
            Debug.Log("MainMenuUI: LocalTestPlayClicked");
            _isFriendMatch = true; // フレンドマッチ扱いにし、自動マッチングは走らせない
            GameManager.IsLocalTestModeRequested = true;
            NetworkLauncher.Instance.StartFriendMatch("LOCAL_TEST_ROOM", PlayerRole.None, GameMode.Host);
            ShowWaitingPanel("1人テストモード待機中...\nルームID: LOCAL_TEST_ROOM");
            if (_roomIdText != null) _roomIdText.text = "ROOM ID: LOCAL_TEST_ROOM";
        }

        /// <summary>
        /// ボタンリスナーの登録（OnEnable/Startから呼ばれる）
        /// </summary>
        private void RegisterButtonListeners()
        {
            if (_randomMatchButton != null)
            {
                _randomMatchButton.onClick.RemoveAllListeners();
                _randomMatchButton.onClick.AddListener(OnRandomMatchClicked);
            }
            if (_friendMatchMenuButton != null)
            {
                _friendMatchMenuButton.onClick.RemoveAllListeners();
                _friendMatchMenuButton.onClick.AddListener(() => ShowPanel(_friendMatchModePanel));
            }
            
            if (_hostMatchButton != null)
            {
                _hostMatchButton.onClick.RemoveAllListeners();
                _hostMatchButton.onClick.AddListener(OnHostMatchClicked);
            }
            if (_guestMatchButton != null)
            {
                _guestMatchButton.onClick.RemoveAllListeners();
                _guestMatchButton.onClick.AddListener(OnGuestMatchClicked);
            }
            if (_backToMainFromFriendModeButton != null)
            {
                _backToMainFromFriendModeButton.onClick.RemoveAllListeners();
                _backToMainFromFriendModeButton.onClick.AddListener(() => ShowPanel(_selectionPanel));
            }

            if (_searchRoomButton != null)
            {
                _searchRoomButton.onClick.RemoveAllListeners();
                _searchRoomButton.onClick.AddListener(OnSearchRoomClicked);
            }
            if (_backToFriendModeButton != null)
            {
                _backToFriendModeButton.onClick.RemoveAllListeners();
                _backToFriendModeButton.onClick.AddListener(() => ShowPanel(_friendMatchModePanel));
            }

            if (_confirmJoinButton != null)
            {
                _confirmJoinButton.onClick.RemoveAllListeners();
                _confirmJoinButton.onClick.AddListener(OnConfirmJoinClicked);
            }
            if (_cancelJoinButton != null)
            {
                _cancelJoinButton.onClick.RemoveAllListeners();
                _cancelJoinButton.onClick.AddListener(() => _confirmationDialog.SetActive(false));
            }

            if (_consentYesButton != null)
            {
                _consentYesButton.onClick.RemoveAllListeners();
                _consentYesButton.onClick.AddListener(() => OnConsentClicked(true));
            }
            if (_consentNoButton != null)
            {
                _consentNoButton.onClick.RemoveAllListeners();
                _consentNoButton.onClick.AddListener(() => OnConsentClicked(false));
            }

            if (_readyButton != null)
            {
                _readyButton.onClick.RemoveAllListeners();
                _readyButton.onClick.AddListener(OnReadyClicked);
            }
            if (_startGameButton != null)
            {
                _startGameButton.onClick.RemoveAllListeners();
                _startGameButton.onClick.AddListener(OnStartGameButtonClicked);
            }
            
            if (_leaveRoomButton != null)
            {
                _leaveRoomButton.onClick.RemoveAllListeners();
                _leaveRoomButton.onClick.AddListener(OnLeaveRoomClicked);
            }
        }

        private void Update()
        {
            if (GameManager.Instance == null) return;

            // RunnerやLocalPlayerが有効になるまでは初期画面を維持
            if (NetworkLauncher.Instance == null || NetworkLauncher.Instance.Runner == null || NetworkLauncher.Instance.Runner.LocalPlayer == PlayerRef.None)
            {
                return;
            }

            // GameManagerがネットワーク上にスポーンされ、初期化が完了するまでは同期処理や状態監視を行わない
            if (GameManager.Instance.Object == null || !GameManager.Instance.Object.IsValid || !GameManager.Instance.IsSpawnedCompleted)
            {
                return;
            }

            try
            {
                bool isLocalTest = GameManager.Instance.IsLocalTestMode;

                // 待機画面の更新
                if (_roomWaitingPanel.activeSelf)
                {
                    var activePlayers = NetworkLauncher.Instance.Runner.ActivePlayers.ToList();
                    int playerCount = activePlayers.Count;
                    _playerCountText.text = $"参加人数: {playerCount} 人";

                    bool isReady = GameManager.Instance.IsPlayerReady(NetworkLauncher.Instance.Runner.LocalPlayer);
                    _readyButtonText.text = isReady ? "準備解除" : "準備完了";

                    // テストモード時の自動準備完了
                    if (IsTestMode() && !isReady && !_hasAutoReadied)
                    {
                        Debug.Log("[TestMode] 自動で準備完了にします...");
                        _hasAutoReadied = true;
                        OnReadyClicked();
                    }

                    // ホストの開始ボタン制御
                    bool isMaster = NetworkLauncher.Instance.IsMaster;
                    if (isMaster)
                    {
                        bool allReady = true;
                        if (isLocalTest)
                        {
                            allReady = isReady; // 1人テストプレイ時は自分が準備完了なら開始可能
                        }
                        else
                        {
                            foreach (var p in activePlayers)
                            {
                                if (!GameManager.Instance.IsPlayerReady(p)) { allReady = false; break; }
                            }
                        }
                        _startGameButton.gameObject.SetActive(true);
                        _startGameButton.interactable = allReady;

                        // テストモード時の自動ゲーム開始
                        if (IsTestMode() && allReady && !_hasAutoStarted)
                        {
                            Debug.Log("[TestMode] 全員準備完了のため、自動でゲームを開始します...");
                            _hasAutoStarted = true;
                            OnStartGameButtonClicked();
                        }
                    }
                    else
                    {
                        _startGameButton.gameObject.SetActive(false);
                    }

                    // 4人未満同意ダイアログの表示チェック（ランダムマッチかつ全員準備完了時）
                    if (!isLocalTest && !_isFriendMatch && GameManager.Instance.CurrentState == GameState.Lobby)
                    {
                        CheckConsentDialog(playerCount);
                    }
                }

                // ゲーム開始検知
                if (GameManager.Instance.CurrentState != GameState.Lobby && GameManager.Instance.CurrentState != GameState.Waiting)
                {
                    if (_mainMenuPanel != null) _mainMenuPanel.SetActive(false);
                }
                else
                {
                    if (_mainMenuPanel != null) _mainMenuPanel.SetActive(true);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"MainMenuUI.Update()で例外が発生しました: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void ShowPanel(GameObject panel)
        {
            if (_selectionPanel != null) _selectionPanel.SetActive(panel == _selectionPanel);
            if (_friendMatchModePanel != null) _friendMatchModePanel.SetActive(panel == _friendMatchModePanel);
            if (_friendMatchInputPanel != null) _friendMatchInputPanel.SetActive(panel == _friendMatchInputPanel);
            if (_topicInputPanel != null) _topicInputPanel.SetActive(panel == _topicInputPanel);
            if (_roomWaitingPanel != null) _roomWaitingPanel.SetActive(panel == _roomWaitingPanel);
        }

        #region マッチング操作

        private void OnRandomMatchClicked()
        {
            Debug.Log("MainMenuUI: RandomMatchButton clicked");
            
            if (_confirmationDialog == null)
            {
                StartNormalRandomMatch();
                return;
            }

            _confirmText.text = "プレイモードを選択してください。";
            
            var confirmTextComp = _confirmJoinButton.GetComponentInChildren<TextMeshProUGUI>();
            string originalConfirmText = confirmTextComp != null ? confirmTextComp.text : "参加";
            if (confirmTextComp != null) confirmTextComp.text = "対戦マッチ";

            _confirmJoinButton.onClick.RemoveAllListeners();
            _confirmJoinButton.onClick.AddListener(() => {
                if (confirmTextComp != null) confirmTextComp.text = originalConfirmText;
                _confirmationDialog.SetActive(false);
                StartNormalRandomMatch();
            });

            var cancelTextComp = _cancelJoinButton.GetComponentInChildren<TextMeshProUGUI>();
            string originalCancelText = cancelTextComp != null ? cancelTextComp.text : "キャンセル";
            if (cancelTextComp != null) cancelTextComp.text = "1人テスト";

            _cancelJoinButton.onClick.RemoveAllListeners();
            _cancelJoinButton.onClick.AddListener(() => {
                if (confirmTextComp != null) confirmTextComp.text = originalConfirmText;
                if (cancelTextComp != null) cancelTextComp.text = originalCancelText;
                _confirmationDialog.SetActive(false);
                
                // 元のボタン動作を復元
                RegisterButtonListeners();
                
                OnLocalTestPlayClicked();
            });

            _confirmationDialog.SetActive(true);
        }

        private void StartNormalRandomMatch()
        {
            RegisterButtonListeners();
            _isFriendMatch = false;
            NetworkLauncher.Instance.StartRandomMatch(PlayerRole.None);
            ShowWaitingPanel("ランダムマッチング中...");
        }

        private void OnHostMatchClicked()
        {
            _isFriendMatch = true;
            string roomId = GenerateRoomId();
            NetworkLauncher.Instance.StartFriendMatch(roomId, PlayerRole.None);
            ShowWaitingPanel($"フレンドマッチ待機中\nルームID: {roomId}");
            _roomIdText.text = $"ROOM ID: {roomId}";
        }

        private async void OnGuestMatchClicked()
        {
            _isFriendMatch = true;
            await NetworkLauncher.Instance.JoinLobby();
            ShowPanel(_friendMatchInputPanel);
        }

        private void OnSearchRoomClicked()
        {
            string id = _roomIdInputField.text;
            if (string.IsNullOrEmpty(id)) return;
            NetworkLauncher.Instance.FindFriendRoom(id);
        }

        private void OnFriendRoomFound(SessionInfo session)
        {
            _foundSession = session;
            _confirmText.text = $"ルーム '{session.Name}' が見つかりました。\n参加しますか？";
            _confirmationDialog.SetActive(true);
        }

        private void OnFriendRoomNotFound()
        {
            _statusText.text = "ルームが見つかりませんでした。";
            Debug.LogWarning("Room not found");
        }

        private void OnConfirmJoinClicked()
        {
            _confirmationDialog.SetActive(false);
            NetworkLauncher.Instance.StartFriendMatch(_foundSession.Name, PlayerRole.None);
            ShowWaitingPanel($"フレンドマッチ待機中\nルームID: {_foundSession.Name}");
            _roomIdText.text = $"ROOM ID: {_foundSession.Name}";
        }

        /// <summary>
        /// ルームを退出して初期画面に戻る
        /// </summary>
        private void OnLeaveRoomClicked()
        {
            NetworkLauncher.Instance.Shutdown();
            ShowPanel(_selectionPanel);
            _confirmationDialog.SetActive(false);
            _consentDialog.SetActive(false);
        }

        #endregion

        #region 準備完了・同意操作

        private void OnReadyClicked()
        {
            if (GameManager.Instance == null || GameManager.Instance.Object == null || !GameManager.Instance.Object.IsValid)
            {
                Debug.LogWarning("GameManager is not spawned yet.");
                return;
            }
            var player = NetworkLauncher.Instance.Runner.LocalPlayer;
            bool currentReady = GameManager.Instance.IsPlayerReady(player);
            GameManager.Instance.RPC_SetReady(player, !currentReady);
        }

        private void CheckConsentDialog(int playerCount)
        {
            if (playerCount < 4 && playerCount >= 2)
            {
                // 全員が準備完了かチェック
                bool allReady = true;
                foreach (var p in NetworkLauncher.Instance.Runner.ActivePlayers)
                {
                    if (!GameManager.Instance.IsPlayerReady(p)) { allReady = false; break; }
                }

                if (allReady && !_consentDialog.activeSelf)
                {
                    bool alreadyConsented = GameManager.Instance.IsPlayerConsented(NetworkLauncher.Instance.Runner.LocalPlayer);
                    if (!alreadyConsented)
                    {
                        _consetText.text = $"現在 {playerCount} 人です。\nこの人数で開始してもよろしいですか？";
                        _consentDialog.SetActive(true);
                    }
                }
            }
            else if (playerCount >= 4)
            {
                _consentDialog.SetActive(false);
            }
        }

        private void OnConsentClicked(bool agreed)
        {
            _consentDialog.SetActive(false);
            if (GameManager.Instance == null || GameManager.Instance.Object == null || !GameManager.Instance.Object.IsValid)
            {
                Debug.LogWarning("GameManager is not spawned yet.");
                return;
            }
            if (agreed)
            {
                GameManager.Instance.RPC_SetConsent(NetworkLauncher.Instance.Runner.LocalPlayer, true);
            }
            else
            {
                // 同意しない場合は準備完了も解除するなどの検討が必要だが、一旦解除
                GameManager.Instance.RPC_SetReady(NetworkLauncher.Instance.Runner.LocalPlayer, false);
            }
        }

        public void OnStartGameButtonClicked()
        {
            if (GameManager.Instance == null || GameManager.Instance.Object == null || !GameManager.Instance.Object.IsValid)
            {
                Debug.LogWarning("GameManager is not spawned yet.");
                return;
            }
            GameManager.Instance.Host_StartGame();
        }

        #endregion

        private void ShowWaitingPanel(string status)
        {
            ShowPanel(_roomWaitingPanel);
            _statusText.text = status;
            _roomIdText.text = "";
        }

        private string GenerateRoomId()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            char[] stringChars = new char[6];
            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = chars[Random.Range(0, chars.Length)];
            }
            return new string(stringChars);
        }

        void OnDestroy()
        {
            if (NetworkLauncher.Instance != null)
            {
                NetworkLauncher.Instance.OnFriendRoomFound -= OnFriendRoomFound;
                NetworkLauncher.Instance.OnFriendRoomNotFound -= OnFriendRoomNotFound;
            }
        }
    }
}