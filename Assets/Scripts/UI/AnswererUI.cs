using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KanjiFlipGame.Core;
using KanjiFlipGame.Kanji;

namespace KanjiFlipGame.UI
{
    /// <summary>
    /// 回答者用のUIを制御するクラス
    /// フリップ表示、回答入力、結果表示を管理します
    /// </summary>
    public class AnswererUI : MonoBehaviour
    {
        [Header("UI要素")]
        [SerializeField] private GameObject _answererPanel;
        [SerializeField] private GameObject _waitingPanel;
        [SerializeField] private TextMeshProUGUI _waitingMessageText;
        [SerializeField] private GameObject _resultPanel;
        [SerializeField] private TMP_InputField _answerInputField;
        [SerializeField] private Button _submitAnswerButton;
        [SerializeField] private TextMeshProUGUI _resultText;
        [SerializeField] private TextMeshProUGUI _timerText;

        [Header("設定")]
        [SerializeField] private float _answerTimeLimit = 15f;

        [Header("参照")]
        [SerializeField] private KanjiFlipper _kanjiFlipper;

        private float _currentTimerValue;
        private bool _isTimerActive = false;

        void Start()
        {
            // ボタンイベントの設定
            if (_submitAnswerButton != null)
                _submitAnswerButton.onClick.AddListener(OnSubmitAnswerClicked);

            // GameManagerのイベントを購読
            GameManager.Instance.OnGameStateChanged.AddListener(OnGameStateChanged);
            GameManager.Instance.OnAnswerResult.AddListener(OnAnswerResult);
            GameManager.Instance.OnFlipDisplayed.AddListener(DisplayFlipData);
            GameManager.Instance.OnPlayerRoleChanged.AddListener(OnPlayerRoleChanged);

            // 初期状態の設定
            UpdateUI();
        }

        void Update()
        {
            if (_isTimerActive)
            {
                _currentTimerValue -= Time.deltaTime;
                UpdateTimerUI();

                if (_currentTimerValue <= 0)
                {
                    _currentTimerValue = 0;
                    _isTimerActive = false;
                    OnSubmitAnswerClicked(); // タイムアップで自動送信
                }
            }
        }

        /// <summary>
        /// タイマーUIの表示を更新
        /// </summary>
        private void UpdateTimerUI()
        {
            if (_timerText != null)
            {
                _timerText.text = $"残り時間: {Mathf.CeilToInt(_currentTimerValue)}秒";
                
                // 残り時間が少なくなったら赤くするなどの演出も可能
                if (_currentTimerValue <= 5f)
                    _timerText.color = Color.red;
                else
                    _timerText.color = Color.white;
            }
        }

        /// <summary>
        /// 回答を送信ボタンが押された
        /// </summary>
        private void OnSubmitAnswerClicked()
        {
            if (_answerInputField == null)
                return;

            _isTimerActive = false;
            string answer = _answerInputField.text;

            // 回答を送信
            GameManager.Instance.OnAnswererSubmitted(answer);

            // 入力欄をクリア
            _answerInputField.text = "";
        }

        /// <summary>
        /// フリップデータを受信して表示（ネットワーク用）
        /// </summary>
        public void DisplayFlipData(string flipDataJson)
        {
            if (_kanjiFlipper != null)
            {
                FlipData data = JsonUtility.FromJson<FlipData>(flipDataJson);
                _kanjiFlipper.LoadFlipData(data);
                
                // 役割とパネル表示を強制更新
                bool isLocalTest = GameManager.Instance != null && GameManager.Instance.IsLocalTestMode;
                if (!isLocalTest && GameManager.Instance.LocalPlayerRole != PlayerRole.Answerer)
                {
                    GameManager.Instance.SetPlayerRole(PlayerRole.Answerer);
                }
                ShowAnswererPanel();
                StartTimer();
            }
        }

        /// <summary>
        /// ゲーム状態が変更された時の処理
        /// </summary>
        private void OnGameStateChanged(GameState newState)
        {
            if (newState == GameState.Questioning)
            {
                // 出題フェーズに戻るときは、出題者（Questioner）側だけフリップをクリアする
                // 回答者側では既に表示されたフリップを保持させておくことで、結果表示時に見えるようにする
                // 1人テストプレイ時は、次のお題に向けて両側のフリップをクリアする
                bool isLocalTest = GameManager.Instance != null && GameManager.Instance.IsLocalTestMode;
                if (isLocalTest)
                {
                    if (_kanjiFlipper != null) _kanjiFlipper.ClearAll();
                }
                else if (GameManager.Instance != null && GameManager.Instance.LocalPlayerRole == PlayerRole.Questioner)
                {
                    if (_kanjiFlipper != null) _kanjiFlipper.ClearAll();
                }
            }
            UpdateUI();
        }

        private void OnPlayerRoleChanged(PlayerRole newRole)
        {
            UpdateUI();
        }

        /// <summary>
        /// UIの表示を更新
        /// </summary>
        private void UpdateUI()
        {
            bool isLocalTest = GameManager.Instance != null && GameManager.Instance.IsLocalTestMode;

            if (isLocalTest)
            {
                // 1人テストプレイ時：回答フェーズまたは結果表示時のみ回答者画面を表示する
                GameState state = GameManager.Instance.CurrentState;
                if (state != GameState.Answering && state != GameState.ShowingResult)
                {
                    HideAllPanels();
                    return;
                }
            }
            else if (GameManager.Instance.LocalPlayerRole != PlayerRole.Answerer)
            {
                HideAllPanels();
                return;
            }

            GameState currentState = GameManager.Instance.CurrentState;

            switch (currentState)
            {
                case GameState.Waiting:
                case GameState.Questioning:
                    ShowWaitingPanel("出題者がフリップを作成中...");
                    _isTimerActive = false;
                    break;

                case GameState.Answering:
                    // すでにフリップデータを受信してロード済み（要素が1つ以上ある）の場合は
                    // 待機パネルではなく回答用パネルを明示的に表示する
                    if (_kanjiFlipper != null && _kanjiFlipper.CurrentKanjiCount > 0)
                    {
                        ShowAnswererPanel();
                    }
                    else if (_answererPanel != null && !_answererPanel.activeSelf)
                    {
                        ShowWaitingPanel("次の出題を待っています...");
                    }
                    break;

                case GameState.ShowingResult:
                    _isTimerActive = false;
                    break;

                default:
                    HideAllPanels();
                    _isTimerActive = false;
                    break;
            }
        }

        private void AdjustLayoutForLocalTest()
        {
            if (_answererPanel == null) return;

            var rectTrans = _answererPanel.GetComponent<RectTransform>();
            if (rectTrans != null)
            {
                // 右半分にアンカーを設定
                rectTrans.anchorMin = new Vector2(0.5f, 0f);
                rectTrans.anchorMax = new Vector2(1f, 1f);
                rectTrans.offsetMin = Vector2.zero;
                rectTrans.offsetMax = Vector2.zero;
            }

            if (_waitingPanel != null && _waitingPanel.transform.parent == _answererPanel.transform.parent)
            {
                var waitingRect = _waitingPanel.GetComponent<RectTransform>();
                if (waitingRect != null)
                {
                    waitingRect.anchorMin = new Vector2(0.5f, 0f);
                    waitingRect.anchorMax = new Vector2(1f, 1f);
                    waitingRect.offsetMin = Vector2.zero;
                    waitingRect.offsetMax = Vector2.zero;
                }
            }

            if (_resultPanel != null && _resultPanel.transform.parent == _answererPanel.transform.parent)
            {
                var resultRect = _resultPanel.GetComponent<RectTransform>();
                if (resultRect != null)
                {
                    resultRect.anchorMin = new Vector2(0.5f, 0f);
                    resultRect.anchorMax = new Vector2(1f, 1f);
                    resultRect.offsetMin = Vector2.zero;
                    resultRect.offsetMax = Vector2.zero;
                }
            }
        }

        /// <summary>
        /// タイマーを開始
        /// </summary>
        private void StartTimer()
        {
            _currentTimerValue = _answerTimeLimit;
            _isTimerActive = true;
            UpdateTimerUI();
        }

        /// <summary>
        /// 待機パネルを表示
        /// </summary>
        private void ShowWaitingPanel(string message)
        {
            if (_answererPanel != null)
                _answererPanel.SetActive(false);
                
            if (_waitingPanel != null)
            {
                _waitingPanel.SetActive(true);
                if (_waitingMessageText != null)
                    _waitingMessageText.text = message;
            }
                
            if (_resultPanel != null)
                _resultPanel.SetActive(false);
        }

        /// <summary>
        /// 回答者パネルを表示
        /// </summary>
        private void ShowAnswererPanel()
        {
            if (_answererPanel != null)
                _answererPanel.SetActive(true);
                
            if (_waitingPanel != null)
                _waitingPanel.SetActive(false);
                
            if (_resultPanel != null)
                _resultPanel.SetActive(false);

            // TODO: ネットワーク経由でフリップデータを受け取り、表示
            // 現在はローカルプロトタイプなので省略
        }

        /// <summary>
        /// 結果パネルを表示
        /// </summary>
        private void ShowResultPanel(bool isCorrect)
        {
            if (_answererPanel != null)
                _answererPanel.SetActive(false);
                
            if (_waitingPanel != null)
                _waitingPanel.SetActive(false);
                
            if (_resultPanel != null)
            {
                _resultPanel.SetActive(true);
                
                if (_resultText != null)
                    _resultText.text = isCorrect ? "○ 正解！" : "× 不正解";
            }
        }

        /// <summary>
        /// 全てのパネルを非表示
        /// </summary>
        private void HideAllPanels()
        {
            if (_answererPanel != null)
                _answererPanel.SetActive(false);
                
            if (_waitingPanel != null)
                _waitingPanel.SetActive(false);
                
            if (_resultPanel != null)
                _resultPanel.SetActive(false);
        }

        /// <summary>
        /// 回答結果を受け取った時の処理
        /// </summary>
        private void OnAnswerResult(bool isCorrect)
        {
            ShowResultPanel(isCorrect);
        }

        void OnDestroy()
        {
            // イベントリスナーのクリーンアップ
            if (_submitAnswerButton != null)
                _submitAnswerButton.onClick.RemoveListener(OnSubmitAnswerClicked);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged.RemoveListener(OnGameStateChanged);
                GameManager.Instance.OnAnswerResult.RemoveListener(OnAnswerResult);
                GameManager.Instance.OnFlipDisplayed.RemoveListener(DisplayFlipData);
                GameManager.Instance.OnPlayerRoleChanged.RemoveListener(OnPlayerRoleChanged);
            }
        }
    }
}
