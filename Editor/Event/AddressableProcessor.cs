using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

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
                ProcessScenario();
            }
        }

        /// <summary>
        /// 게임 빌드 시 호출
        /// </summary>
        /// <param name="report"></param>
        public void OnPreprocessBuild(BuildReport report)
        {
            ProcessScenario();
        }

        private static void ProcessScenario()
        {
            ScenarioManager.Instance.UpdateAddressableGroup();
        }
    }
}