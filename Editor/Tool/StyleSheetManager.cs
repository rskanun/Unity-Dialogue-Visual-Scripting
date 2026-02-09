using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Rskanun.DialogueVisualScripting.Editor
{
    public static class StyleSheetManager
    {
        public static StyleSheet GetStyleSheet(string styleName)
        {
            // 찾고자 하는 스타일 시트 경로 찾기
            var stylePath = FindStylePath(styleName);

            // 스타일시트 가져오기
            return AssetDatabase.LoadAssetAtPath<StyleSheet>(stylePath);
        }

        /// <summary>
        /// 스타일 시트 경로 반환
        /// </summary>
        /// <param name="styleName">찾고자 하는 스타일 시트</param>
        /// <returns></returns>
        private static string FindStylePath(string styleName)
        {
            // 확장자 제거
            var searchName = Path.GetFileNameWithoutExtension(styleName);

            // 프로젝트 내부 탐색
            var guids = AssetDatabase.FindAssets($"{searchName} t:StyleSheet");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                if (Path.GetFileName(path) == styleName)
                {
                    return path;
                }
            }

            Debug.LogWarning($"StyleSheet not found: {{styleName}}");
            return null;
        }
    }
}