using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

[InitializeOnLoad]
public static class AutoUIFixer
{
    static AutoUIFixer()
    {
        // Schedule the fix after the editor finishes loading
        EditorApplication.delayCall += RunFix;
    }

    private static void RunFix()
    {
        // Skip when running in batch mode (e.g., CI builds)
        if (Application.isBatchMode) return;

        // Skip if editor is in play mode
        if (EditorApplication.isPlaying) return;

        // Execute the UI building / fixing logic
        UIBuilder.Build();
        Debug.Log("AutoUIFixer: UIBuilder.Build executed on editor load.");
    }
}
#endif
