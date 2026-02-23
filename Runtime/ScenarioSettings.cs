using UnityEngine;
using System.IO;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Rskanun.DialogueVisualScripting
{
    public class ScenarioSettings : ScriptableObject
    {
        private const string FILE_DIRECTORY = "Assets/Resources";
        private const string FILE_PATH = "Assets/Resources/Scenario Settings.asset";

        private static ScenarioSettings _instance;
        public static ScenarioSettings Instance
        {
            get
            {
                if (_instance != null) return _instance;

                // 생성 경로 탐색
                _instance = Resources.Load<ScenarioSettings>("Scenario Settings");

                if (_instance == null)
                {
                    // Resource 전체 탐색
                    var assets = Resources.LoadAll<ScenarioSettings>("");

                    if (assets != null && assets.Length > 0)
                    {
                        // 가장 처음 발견한 에셋 사용
                        return _instance = assets[0];
                    }
                }

#if UNITY_EDITOR
                if (_instance == null)
                {
                    // 파일 경로가 없을 경우 폴더 생성
                    if (!AssetDatabase.IsValidFolder(FILE_DIRECTORY))
                    {
                        Directory.CreateDirectory(FILE_DIRECTORY);

                        // 유니티에게 인식
                        AssetDatabase.ImportAsset(FILE_DIRECTORY);
                    }

                    // Resource.Load가 실패했을 경우
                    _instance = AssetDatabase.LoadAssetAtPath<ScenarioSettings>(FILE_PATH);

                    if (_instance == null)
                    {
                        // 에셋 생성
                        _instance = CreateInstance<ScenarioSettings>();
                        AssetDatabase.CreateAsset(_instance, FILE_PATH);

                        // 변경 사항 저장 및 새로고침
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                    }
                }
#endif

                return _instance;
            }
        }

        [SerializeField]
#if ODIN_INSPECTOR
        [FolderPath(RequireExistingPath = true)]
#endif
        private string _scenarioDirectory;
        public static string ScenarioDirectory => Instance._scenarioDirectory;

#if ODIN_INSPECTOR
        [Title("Addressable Settings")]
#else
        [Header("Addressable Settings")]
#endif
        [SerializeField]
        private string _addressableGroupName;
        public static string AddressableGroupName => Instance._addressableGroupName;
        [SerializeField]
        private string _labelPrefix;
        public static string LabelPrefix => Instance._labelPrefix;

        public static string GetLabelName(string id)
        {
            var prefix = Instance._labelPrefix;

            if (string.IsNullOrEmpty(prefix)) return id;
            return $"{Instance._labelPrefix}_{id}";
        }
    }
}