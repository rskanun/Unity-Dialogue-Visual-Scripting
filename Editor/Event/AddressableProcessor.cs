using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

namespace Rskanun.DialogueVisualScripting.Editor
{
    [InitializeOnLoad]
    public class AddressableProcessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        // InitializeOnLoad에 의해 켜질 때 한 번 호출
        static AddressableProcessor()
        {
            // 상태 변경 이벤트 구독
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        /// <summary>
        /// 에디터 내 플레이 모드 변경 시 호출
        /// </summary>
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // 플레이 모드 직전 실행
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                UpdateAddressableGroup();
            }
        }

        /// <summary>
        /// 게임 빌드 시 호출
        /// </summary>
        public void OnPreprocessBuild(BuildReport report)
        {
            UpdateAddressableGroup();
        }

        private static void UpdateAddressableGroup()
        {
            var scenarioDir = ScenarioSettings.ScenarioDirectory;

            // 경로 상에 폴더가 존재하지 않는 경우
            if (string.IsNullOrEmpty(scenarioDir) || !Directory.Exists(scenarioDir))
            {
                // 실행하지 않고 종료
                return;
            }

            // Addressable 셋팅과 그룹 찾아오기
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var group = settings.FindGroup(ScenarioSettings.AddressableGroupName);

            // 그룹이 없는 경우
            if (group == null)
            {
                // 새로 만들기
                group = settings.CreateGroup(ScenarioSettings.AddressableGroupName, false, false, true, null);
            }

            // 최상위 폴더 내 시나리오 탐색
            SetAddressableEntries(group, scenarioDir);

            // 하위 시나리오 폴더 탐색
            foreach (var path in Directory.GetDirectories(scenarioDir))
            {
                var folderName = Path.GetFileName(path);
                var label = ScenarioSettings.GetLabelName(folderName);

                SetAddressableEntries(group, path, label);
            }

            // 변경사항 저장
            EditorUtility.SetDirty(settings);
        }

        private static void SetAddressableEntries(AddressableAssetGroup group, string path, string label = null)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var guids = AssetDatabase.FindAssets("t:Scenario", new string[] { path });

            foreach (var guid in guids)
            {
                // 에셋을 그룹에 등록
                var entry = settings.CreateOrMoveEntry(guid, group);

                if (entry == null) continue;

                var assetPath = AssetDatabase.GUIDToAssetPath(guid);

                // 이름 설정
                entry.address = Path.GetFileNameWithoutExtension(assetPath);

                // 레이블이 있는 경우
                if (!string.IsNullOrEmpty(label))
                {
                    // 그룹 내 레이블 등록
                    settings.AddLabel(label);
                    entry.SetLabel(label, true, true);
                }
            }
        }
    }
}