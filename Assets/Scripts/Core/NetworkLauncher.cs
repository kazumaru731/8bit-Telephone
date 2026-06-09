using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using KanjiFlipGame.Core;

namespace KanjiFlipGame.Network
{
    /// <summary>
    /// Photon Fusionを使用した接続とマッチングを管理するクラス
    /// </summary>
    public class NetworkLauncher : MonoBehaviour, INetworkRunnerCallbacks
    {
        [SerializeField] private NetworkRunner _runnerPrefab;
        private NetworkRunner _currentRunner;
        private List<SessionInfo> _sessions = new List<SessionInfo>();

        private static NetworkLauncher _instance;
        public static NetworkLauncher Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<NetworkLauncher>();
                }
                return _instance;
            }
            private set => _instance = value;
        }

        public bool IsMaster => _currentRunner != null && _currentRunner.IsSharedModeMasterClient;
        public NetworkRunner Runner => _currentRunner;

        // イベント
        public event Action OnLobbyJoined;
        public event Action<List<SessionInfo>> OnSessionListChanged;
        public event Action<SessionInfo> OnFriendRoomFound;
        public event Action OnFriendRoomNotFound;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// ロビーに参加する（ゲストの検索用）
        /// </summary>
        public async Task JoinLobby()
        {
            if (_currentRunner == null)
            {
                _currentRunner = Instantiate(_runnerPrefab);
                _currentRunner.AddCallbacks(this);
            }

            var result = await _currentRunner.JoinSessionLobby(SessionLobby.ClientServer);
            if (result.Ok)
            {
                Debug.Log("ロビーに参加しました");
                OnLobbyJoined?.Invoke();
            }
            else
            {
                Debug.LogError($"ロビー参加失敗: {result.ShutdownReason}");
            }
        }

        /// <summary>
        /// 特定のルームIDを検索
        /// </summary>
        public void FindFriendRoom(string roomId)
        {
            var session = _sessions.Find(s => s.Name.Equals(roomId, StringComparison.OrdinalIgnoreCase));
            if (session != null)
            {
                OnFriendRoomFound?.Invoke(session);
            }
            else
            {
                OnFriendRoomNotFound?.Invoke();
            }
        }

        /// <summary>
        /// ランダムマッチを開始
        /// </summary>
        public async void StartRandomMatch(PlayerRole role, string topic = "")
        {
            await StartGame(GameMode.Shared, null, role, topic);
        }

        /// <summary>
        /// フレンドマッチを開始（ルームID指定）
        /// </summary>
        public async Task StartFriendMatch(string roomId, PlayerRole role, GameMode mode = GameMode.Shared, string topic = "")
        {
            await StartGame(mode, roomId, role, topic);
        }

        private async Task StartGame(GameMode mode, string roomName, PlayerRole role, string topic)
        {
            // 以前のランナーがあれば強制的にシャットダウン・破棄する
            if (_currentRunner != null)
            {
                try
                {
                    await _currentRunner.Shutdown();
                }
                catch (System.Exception) {}
                if (_currentRunner != null && _currentRunner.gameObject != null)
                {
                    Destroy(_currentRunner.gameObject);
                }
                _currentRunner = null;
            }

            _currentRunner = Instantiate(_runnerPrefab);
            _currentRunner.AddCallbacks(this);

            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var sceneRef = SceneRef.FromIndex(activeScene.buildIndex);

            // SceneManagerは重複追加しないようGetComponent優先で取得
            var sceneManager = _currentRunner.gameObject.GetComponent<NetworkSceneManagerDefault>()
                               ?? _currentRunner.gameObject.AddComponent<NetworkSceneManagerDefault>();

            var result = await _currentRunner.StartGame(new StartGameArgs()
            {
                GameMode = mode,
                SessionName = roomName, // nullの場合はランダムマッチ
                Scene = sceneRef,
                SceneManager = sceneManager,
            });

            if (result.Ok)
            {
                Debug.Log($"接続成功: Room={_currentRunner.SessionInfo.Name}");
            }
            else
            {
                Debug.LogError($"接続失敗: {result.ShutdownReason}");
            }
        }

        public async void Shutdown()
        {
            if (_currentRunner != null)
            {
                await _currentRunner.Shutdown();
                _currentRunner = null;
            }
        }

        #region INetworkRunnerCallbacks

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { Debug.Log($"Player Joined: {player}"); }
        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
            _sessions = sessionList;
            OnSessionListChanged?.Invoke(sessionList);
            Debug.Log($"セッションリスト更新: {sessionList.Count}件");
        }

        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

        #endregion
    }
}
