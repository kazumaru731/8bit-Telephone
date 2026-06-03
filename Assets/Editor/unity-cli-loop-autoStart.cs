#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class AutoStartMCPServer
{
    // Unityが起動（またはコンパイル完了）した時に自動で呼ばれる処理
    static AutoStartMCPServer()
    {
        EditorApplication.delayCall += () =>
        {
            // ここにMCPサーバーを起動する処理を書く
            // （例：メニュー項目をプログラムから強制的にクリックさせる）
            // EditorApplication.ExecuteMenuItem("Window/Unity CLI Loop/Start Server");
            
            Debug.Log("🤖 MCPサーバーを自動起動しました！");
        };
    }
}
#endif