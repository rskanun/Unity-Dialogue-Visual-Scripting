using System.IO;
using UnityEditor;
using UnityEngine.UIElements;

namespace Rskanun.DialogueVisualScripting.Editor
{
    public static class StyleSheetManager
    {
        public static StyleSheet GetStyle(string path)
        {
            var folderPath = VisualScriptingSettings.StyleSheetDirectory;
            var stylePath = Path.Combine(folderPath, path);

            // 스타일시트 가져오기
            return AssetDatabase.LoadAssetAtPath<StyleSheet>(stylePath);
        }
    }
}