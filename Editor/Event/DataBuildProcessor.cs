using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Rskanun.DialogueVisualScripting.Editor
{
    [InitializeOnLoad]
    public class DataBuildProcessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => -100;

        static DataBuildProcessor()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        /// <summary>
        /// 플레이 모드 변경 시 확인 로직
        /// </summary>
        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode)
            {
                return;
            }

            CheckScenarioBuild();
        }

        /// <summary>
        /// 빌드 시 확인 로직
        /// </summary>
        public void OnPreprocessBuild(BuildReport repport)
        {
            CheckScenarioBuild();
        }

        public static void BuildScenarios()
        {
            // 빌드되지 않은 그래프 탐색
            var unbuiltGraphs = GetUnbuiltGraphs();

            // 그래프 빌드 진행
            BuildScenarios(unbuiltGraphs);
        }

        private static void CheckScenarioBuild()
        {
            // 빌드 안 된 그래프 확인
            var unbuiltGraphs = GetUnbuiltGraphs();
            if (unbuiltGraphs == null || unbuiltGraphs.Count == 0)
            {
                return;
            }

            bool isBuild = EditorUtility.DisplayDialog(
                "Dialogue Visual Scripting Alert",
                "빌드 되지 않은 새 대화를 확인했습니다.\n지금 빌드하겠습니까?",
                "Build Now", "Skip"
            );

            // 빌드에 동의한 경우만 빌드 진행
            if (isBuild)
                BuildScenarios(unbuiltGraphs);
        }

        private static List<ScenarioGraph> GetUnbuiltGraphs()
        {
            var unbuiltGraphs = new List<ScenarioGraph>();

            var dir = ScenarioSettings.ScenarioDirectory;

            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            {
                Debug.LogWarning("[DataBuildProcessor] 시나리오 폴더 경로를 찾을 수 없습니다.");
                return unbuiltGraphs;
            }

            var guids = AssetDatabase.FindAssets("t:ScenarioGraph", new string[] { dir });
            foreach (string guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var graph = AssetDatabase.LoadAssetAtPath<ScenarioGraph>(assetPath);

                // 빌드 이후와 달라진 그래프만 선택
                if (graph != null && graph.IsDirty)
                {
                    unbuiltGraphs.Add(graph);
                }
            }

            return unbuiltGraphs;
        }

        private static void BuildScenarios(List<ScenarioGraph> graphs)
        {
            foreach (var graph in graphs)
            {
                if (graph == null) continue;

                // 빌드 에셋 저장 경로(기존 에셋 경로)
                var assetPath = AssetDatabase.GetAssetPath(graph);
                var fileDir = Path.GetDirectoryName(assetPath);
                var fileName = graph.name + ".build.asset";
                var buildPath = Path.Combine(fileDir, fileName);

                // 이전 빌드된 파일이 있는 지 탐색
                var scenario = AssetDatabase.LoadAssetAtPath<Scenario>(buildPath);
                bool isNewAsset = false;

                if (scenario == null)
                {
                    // 이전에 빌드된 파일이 없는 경우 새로 생성
                    scenario = ScriptableObject.CreateInstance<Scenario>();
                    isNewAsset = true;
                }

                // 시나리오 에셋 생성 및 복사
                scenario.CopyTo(graph);

                // 에셋 새로 만들기 또는 덮어쓰기 형태로 저장
                if (isNewAsset) AssetDatabase.CreateAsset(scenario, buildPath);
                else EditorUtility.SetDirty(scenario);

                // 빌드 처리
                graph.MarkAsBuilt();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}