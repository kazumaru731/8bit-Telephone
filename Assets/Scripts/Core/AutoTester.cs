using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using KanjiFlipGame.Core;
using KanjiFlipGame.Network;
using KanjiFlipGame.UI;
using KanjiFlipGame.Kanji;
using Fusion;
using System.Linq;

namespace KanjiFlipGame.Core
{
    public class AutoTester : MonoBehaviour
    {
        private string _testErrorMessage = null;

        private void Awake()
        {
            // コマンドライン引数に -runAutoTest が含まれている場合、またはエディタ実行用トリガーファイルが存在する場合のみ実行
            if (System.Environment.GetCommandLineArgs().Contains("-runAutoTest") ||
                (Application.isEditor && System.IO.File.Exists("run_test_in_editor.txt")))
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void Start()
        {
            // コマンドライン引数に -runAutoTest が含まれている場合、またはエディタ実行用トリガーファイルが存在する場合のみ実行
            if (System.Environment.GetCommandLineArgs().Contains("-runAutoTest") ||
                (Application.isEditor && System.IO.File.Exists("run_test_in_editor.txt")))
            {
                StartCoroutine(TestSequence());
            }
        }

        private IEnumerator TestSequence()
        {
            var args = System.Environment.GetCommandLineArgs();
            bool isHost = args.Contains("-roleHost");
            bool isClient = args.Contains("-roleClient");
            
            // どちらの引数もない場合は、旧シングルプレイヤー検証フォールバック
            if (!isHost && !isClient)
            {
                isHost = true; // デフォルトでホストとして動かす
            }

            string roleStr = isHost ? "HOST" : "CLIENT";
            Debug.Log($"🤖 [AutoTester] 自動テストを開始します... ロール: {roleStr}");
            string resultPath = System.IO.Path.Combine(Application.dataPath, $"../test_result_{roleStr}.txt");

            // 旧テスト結果の削除
            if (System.IO.File.Exists(resultPath))
            {
                try { System.IO.File.Delete(resultPath); } catch {}
            }

            // 1. ネットワークの初期化待ち (クライアントはホストの起動を待つため長めに待機)
            if (isClient)
            {
                yield return new WaitForSeconds(8f);
            }
            else
            {
                yield return new WaitForSeconds(2f);
            }

            // 2. ルームの作成/接続
            RunStep(() => {
                var launcher = NetworkLauncher.Instance;
                if (launcher == null) throw new System.Exception("NetworkLauncherが見つかりません");
                
                if (isHost)
                {
                    Debug.Log("🤖 [AutoTester] ホストとしてルーム TESTROOM_AUTO を作成中...");
                    launcher.StartFriendMatch("TESTROOM_AUTO", PlayerRole.None, GameMode.Host);
                }
                else
                {
                    Debug.Log("🤖 [AutoTester] クライアントとしてルーム TESTROOM_AUTO に参加中...");
                    launcher.StartFriendMatch("TESTROOM_AUTO", PlayerRole.None, GameMode.Client);
                }
            });
            if (_testErrorMessage != null) { WriteResultAndQuit(resultPath, _testErrorMessage); yield break; }

            // シーン再ロード時間
            yield return new WaitForSeconds(3f);

            // 3. GameManagerのスポーン待ち
            float timeout = 40f;
            float elapsed = 0f;
            while (elapsed < timeout)
            {
                bool ready = false;
                RunStep(() => {
                    ready = GameManager.Instance != null && GameManager.Instance.IsSpawnedCompleted;
                });
                if (_testErrorMessage != null) { WriteResultAndQuit(resultPath, _testErrorMessage); yield break; }
                if (ready) break;
                
                yield return new WaitForSeconds(0.5f);
                elapsed += 0.5f;
            }

            if (GameManager.Instance == null || !GameManager.Instance.IsSpawnedCompleted)
            {
                WriteResultAndQuit(resultPath, "FAILURE: GameManagerのスポーン待ちがタイムアウトしました");
                yield break;
            }

            Debug.Log($"🤖 [AutoTester][{roleStr}] GameManagerがスポーンされました。");

            var runner = NetworkLauncher.Instance.Runner;

            if (isHost)
            {
                // ホスト側：プレイヤーが2人揃うのを待つ
                Debug.Log("🤖 [AutoTester][HOST] 回答者の接続を待っています...");
                elapsed = 0f;
                while (elapsed < timeout)
                {
                    int playerCount = 0;
                    RunStep(() => {
                        playerCount = runner.ActivePlayers.Count();
                    });
                    if (playerCount >= 2) break;
                    yield return new WaitForSeconds(0.5f);
                    elapsed += 0.5f;
                }

                if (runner.ActivePlayers.Count() < 2)
                {
                    WriteResultAndQuit(resultPath, "FAILURE: クライアントが接続してきませんでした（タイムアウト）");
                    yield break;
                }

                Debug.Log("🤖 [AutoTester][HOST] プレイヤーが2人揃いました。");
                yield return new WaitForSeconds(1.5f);

                // ゲーム開始
                RunStep(() => {
                    GameManager.Instance.Host_StartGame();
                });
                if (_testErrorMessage != null) { WriteResultAndQuit(resultPath, _testErrorMessage); yield break; }
            }
            else
            {
                // クライアント側：準備完了状態にする
                yield return new WaitForSeconds(1f);
                RunStep(() => {
                    GameManager.Instance.RPC_SetReady(runner.LocalPlayer, true);
                    Debug.Log("🤖 [AutoTester][CLIENT] 準備完了を送信しました。");
                });
                if (_testErrorMessage != null) { WriteResultAndQuit(resultPath, _testErrorMessage); yield break; }
            }

            // 6. 出題状態（Questioning）待ち
            elapsed = 0f;
            while (elapsed < timeout)
            {
                bool questioning = false;
                RunStep(() => {
                    questioning = GameManager.Instance.CurrentState == GameState.Questioning;
                });
                if (_testErrorMessage != null) { WriteResultAndQuit(resultPath, _testErrorMessage); yield break; }
                if (questioning) break;

                yield return new WaitForSeconds(0.5f);
                elapsed += 0.5f;
            }

            if (GameManager.Instance.CurrentState != GameState.Questioning)
            {
                WriteResultAndQuit(resultPath, "FAILURE: GameStateがQuestioningになりませんでした");
                yield break;
            }

            Debug.Log($"🤖 [AutoTester][{roleStr}] 出題フェーズに入りました。");

            // ロールの割り当てを確認
            yield return new WaitForSeconds(2f);
            PlayerRole myRole = PlayerRole.None;
            RunStep(() => {
                myRole = GameManager.Instance.LocalPlayerRole;
            });
            Debug.Log($"🤖 [AutoTester][{roleStr}] 自プレイヤーの割り当てロール: {myRole}");

            if (myRole == PlayerRole.Questioner)
            {
                // ホスト（または出題者）側の検証・操作
                RunStep(() => {
                    var questionerUI = FindObjectOfType<QuestionerUI>();
                    if (questionerUI == null) throw new System.Exception("QuestionerUIが見つかりません");

                    var topicTextGo = GameObject.Find("TopicDisplayText");
                    if (topicTextGo == null) throw new System.Exception("TopicDisplayText がシーン内に見つかりません");
                    var topicText = topicTextGo.GetComponent<TextMeshProUGUI>();
                    if (!topicText.gameObject.activeSelf) throw new System.Exception("TopicDisplayText が非表示です");

                    var kanjiInputFieldGo = GameObject.Find("KanjiInputField");
                    if (kanjiInputFieldGo == null) throw new System.Exception("KanjiInputField がシーン内に見つかりません");
                    var kanjiInputField = kanjiInputFieldGo.GetComponent<TMP_InputField>();
                    if (!kanjiInputField.gameObject.activeSelf) throw new System.Exception("KanjiInputField が非表示です");

                    var flipper = FindObjectOfType<KanjiFlipper>();
                    if (flipper == null) throw new System.Exception("KanjiFlipperが見つかりません");

                    // 1. 漢字テキストボックスに入力
                    kanjiInputField.text = "山";
                    
                    var onValueChangedField = typeof(TMP_InputField).GetField("m_OnValueChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (onValueChangedField != null)
                    {
                        var ev = onValueChangedField.GetValue(kanjiInputField) as TMP_InputField.OnChangeEvent;
                        if (ev != null) ev.Invoke("山");
                    }

                    // 2. 候補から「山」を選択
                    var suggestionContentField = typeof(QuestionerUI).GetField("_suggestionContent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var suggestionContent = suggestionContentField.GetValue(questionerUI) as RectTransform;
                    
                    Button yamaBtn = null;
                    for (int i = 0; i < suggestionContent.childCount; i++)
                    {
                        var child = suggestionContent.GetChild(i);
                        var textMesh = child.GetComponentInChildren<TextMeshProUGUI>();
                        if (textMesh != null && textMesh.text.Contains("山"))
                        {
                            yamaBtn = child.GetComponent<Button>();
                            break;
                        }
                    }

                    if (yamaBtn == null) throw new System.Exception("「山」の候補ボタンが見つかりません");
                    yamaBtn.onClick.Invoke();

                    // 3. 決定して追加
                    var addKanjiBtnField = typeof(QuestionerUI).GetField("_addKanjiButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var addKanjiBtn = addKanjiBtnField.GetValue(questionerUI) as Button;
                    addKanjiBtn.onClick.Invoke();
                });
                if (_testErrorMessage != null) { WriteResultAndQuit(resultPath, _testErrorMessage); yield break; }

                yield return new WaitForSeconds(0.5f);

                // 漢字追加の確認
                RunStep(() => {
                    var flipper = FindObjectOfType<KanjiFlipper>();
                    if (flipper.CurrentKanjiCount != 1) throw new System.Exception("フリップに漢字が追加されていません");
                });
                if (_testErrorMessage != null) { WriteResultAndQuit(resultPath, _testErrorMessage); yield break; }

                // 完成ボタンをクリック
                RunStep(() => {
                    var questionerUI = FindObjectOfType<QuestionerUI>();
                    var completeBtnField = typeof(QuestionerUI).GetField("_completeButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var completeBtn = completeBtnField.GetValue(questionerUI) as Button;
                    completeBtn.onClick.Invoke();
                    Debug.Log("🤖 [AutoTester][HOST] 出題完了ボタンを押しました。");
                });
                if (_testErrorMessage != null) { WriteResultAndQuit(resultPath, _testErrorMessage); yield break; }

                yield return new WaitForSeconds(2.0f);

                // 「回答中」待機画面の検証（操作パネルが残ったまま、上部に表示されること）
                RunStep(() => {
                    var questionerUI = FindObjectOfType<QuestionerUI>();
                    var qWaitingPanel = typeof(QuestionerUI).GetField("_waitingPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(questionerUI) as GameObject;
                    if (qWaitingPanel == null || !qWaitingPanel.activeSelf)
                    {
                        throw new System.Exception("出題完了後に、回答中ステータスを示す待機パネルが表示されていません。");
                    }
                    var qPanelField = typeof(QuestionerUI).GetField("_questionerPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var qPanel = qPanelField.GetValue(questionerUI) as GameObject;
                    if (qPanel == null || !qPanel.activeSelf)
                    {
                        throw new System.Exception("出題完了後に、出題パネル（QuestionerPanel）が非表示になっています。");
                    }
                });
                if (_testErrorMessage != null) { WriteResultAndQuit(resultPath, _testErrorMessage); yield break; }

                // クライアントが回答して状態遷移するのを待つ
                elapsed = 0f;
                while (elapsed < timeout)
                {
                    bool finished = false;
                    RunStep(() => {
                        finished = GameManager.Instance.CurrentState == GameState.Answering;
                    });
                    if (finished) break;
                    yield return new WaitForSeconds(0.5f);
                    elapsed += 0.5f;
                }
            }
            else
            {
                // 回答者（Client）側の検証・操作
                // ホスト側が出題して GameState が Answering になるのを待つ
                elapsed = 0f;
                while (elapsed < timeout)
                {
                    bool answering = false;
                    RunStep(() => {
                        answering = GameManager.Instance.CurrentState == GameState.Answering;
                    });
                    if (_testErrorMessage != null) { WriteResultAndQuit(resultPath, _testErrorMessage); yield break; }
                    if (answering) break;

                    yield return new WaitForSeconds(0.5f);
                    elapsed += 0.5f;
                }

                if (GameManager.Instance.CurrentState != GameState.Answering)
                {
                    WriteResultAndQuit(resultPath, "FAILURE: GameStateがAnsweringになりませんでした（出題待ちタイムアウト）");
                    yield break;
                }

                yield return new WaitForSeconds(1.5f); // データ同期のバッファ

                // 回答画面にフリップと漢字「山」および入力欄が表示されているか検証
                RunStep(() => {
                    var answererUI = FindObjectOfType<AnswererUI>();
                    if (answererUI == null) throw new System.Exception("AnswererUIが見つかりません");

                    var ansPanelField = typeof(AnswererUI).GetField("_answererPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var ansPanel = ansPanelField.GetValue(answererUI) as GameObject;
                    if (ansPanel == null || !ansPanel.activeSelf)
                    {
                        throw new System.Exception("回答入力画面が表示されていません");
                    }

                    var ansFlipperField = typeof(AnswererUI).GetField("_kanjiFlipper", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var ansFlipper = ansFlipperField.GetValue(answererUI) as KanjiFlipper;
                    if (ansFlipper.CurrentKanjiCount != 1)
                    {
                        throw new System.Exception($"回答者用フリップに漢字が正しく転送されていません。カウント: {ansFlipper.CurrentKanjiCount}");
                    }

                    var elements = ansFlipper.GetAllKanjiElements();
                    if (elements.Count == 0 || elements[0].GetKanji() != "山")
                    {
                        throw new System.Exception($"転送された漢字が「山」ではありません。実際: '{(elements.Count > 0 ? elements[0].GetKanji() : "None")}'");
                    }

                    var ansInputFieldField = typeof(AnswererUI).GetField("_answerInputField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var ansInputField = ansInputFieldField.GetValue(answererUI) as TMP_InputField;
                    var submitBtnField = typeof(AnswererUI).GetField("_submitAnswerButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var submitBtn = submitBtnField.GetValue(answererUI) as Button;

                    // お題（太陽）を入力して送信
                    ansInputField.text = GameManager.Instance.CurrentTopic;
                    submitBtn.onClick.Invoke();
                    Debug.Log($"🤖 [AutoTester][CLIENT] 正解 '{GameManager.Instance.CurrentTopic}' を入力して確定しました。");
                });
                if (_testErrorMessage != null) { WriteResultAndQuit(resultPath, _testErrorMessage); yield break; }

                yield return new WaitForSeconds(1.5f);

                // 結果画面で「○」が出ているかアサーション
                RunStep(() => {
                    var answererUI = FindObjectOfType<AnswererUI>();
                    var resPanelField = typeof(AnswererUI).GetField("_resultPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var resPanel = resPanelField.GetValue(answererUI) as GameObject;
                    if (resPanel == null || !resPanel.activeSelf)
                    {
                        throw new System.Exception("正解送信後に結果パネルが表示されていません");
                    }

                    var resTextField = typeof(AnswererUI).GetField("_resultText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var resText = resTextField.GetValue(answererUI) as TextMeshProUGUI;
                    if (resText == null || !resText.text.Contains("○"))
                    {
                        throw new System.Exception($"正解判定（○）が確認できません。実際: '{resText?.text}'");
                    }
                });
                if (_testErrorMessage != null) { WriteResultAndQuit(resultPath, _testErrorMessage); yield break; }
            }

            // 成功終了
            WriteResultAndQuit(resultPath, "SUCCESS");
        }

        private void RunStep(System.Action action)
        {
            try
            {
                action();
            }
            catch (System.Exception e)
            {
                _testErrorMessage = "FAILURE: " + e.Message + "\n" + e.StackTrace;
            }
        }

        private void WriteResultAndQuit(string path, string content)
        {
            System.IO.File.WriteAllText(path, content);
            if (Application.isEditor && System.IO.File.Exists("run_test_in_editor.txt"))
            {
                try { System.IO.File.Delete("run_test_in_editor.txt"); } catch {}
            }
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
