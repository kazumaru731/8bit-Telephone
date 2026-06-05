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

        void OnEnable()
        {
            Debug.Log($"MainMenuUI: OnEnable() called. GameObject: {gameObject.name}");
            InitializeUI();
        }

        void Start()
        {
            Debug.Log("MainMenuUI: Start() called");
            // Startでも初期化（念のため）
            InitializeUI();
        }

        private void InitializeUI()
        {
            Debug.Log("MainMenuUI: Initializing UI...");
            // ボタンイベントの登録
            // (Remove existing first to avoid duplicates)
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

            // ネットワークイベントの登録
            if (NetworkLauncher.Instance != null)
            {
                NetworkLauncher.Instance.OnFriendRoomFound -= OnFriendRoomFound;
                NetworkLauncher.Instance.OnFriendRoomFound += OnFriendRoomFound;
                NetworkLauncher.Instance.OnFriendRoomNotFound -= OnFriendRoomNotFound;
                NetworkLauncher.Instance.OnFriendRoomNotFound += OnFriendRoomNotFound;
            }

            // 初期状態
            ShowPanel(_selectionPanel);
            if (_confirmationDialog != null) _confirmationDialog.SetActive(false);
            if (_consentDialog != null) _consentDialog.SetActive(false);
            
            Debug.Log($"MainMenuUI: Initial panel set to {_selectionPanel?.name}");
        }

        private void Update()
        {
            if (GameManager.Instance == null) return;

            // Runnerがいない間は初期画面を維持
            if (NetworkLauncher.Instance.Runner == null)
            {
                return;
            }

            // 待機画面の更新
            if (_roomWaitingPanel.activeSelf)
            {
                int playerCount = NetworkLauncher.Instance.Runner.ActivePlayers.Count();
                _playerCountText.text = $"参加人数: {playerCount} 人";

                bool isReady = GameManager.Instance.IsPlayerReady(NetworkLauncher.Instance.Runner.LocalPlayer);
                _readyButtonText.text = isReady ? "準備解除" : "準備完了";

                // ホストの開始ボタン制御
                bool isMaster = NetworkLauncher.Instance.IsMaster;
                if (isMaster)
                {
                    bool allReady = true;
                    foreach (var p in NetworkLauncher.Instance.Runner.ActivePlayers)
                    {
                        if (!GameManager.Instance.IsPlayerReady(p)) { allReady = false; break; }
                    }
                    _startGameButton.gameObject.SetActive(true);
                    _startGameButton.interactable = allReady;
                }
                else
                {
                    _startGameButton.gameObject.SetActive(false);
                }

                // 4人未満同意ダイアログの表示チェック（ランダムマッチかつ全員準備完了時）
                if (!_isFriendMatch && GameManager.Instance.CurrentState == GameState.Lobby)
                {
                    CheckConsentDialog(playerCount);
                }
            }

            // ゲーム開始検知
            if (GameManager.Instance.CurrentState != GameState.Lobby && GameManager.Instance.CurrentState != GameState.Waiting)
            {
                gameObject.SetActive(false);
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