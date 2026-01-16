#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Rskanun.DialogueVisualScripting.Editor
{
#if UNITY_EDITOR
    public class GraphFileDeletionProcessor : AssetModificationProcessor
    {
        private static AssetDeleteResult OnWillDeleteAsset(string path, RemoveAssetOptions options)
        {
            var delFile = AssetDatabase.LoadAssetAtPath<ScenarioGraph>(path);

            // 삭제되는 에셋이 GraphFile인 경우
            if (delFile != null)
            {
                // 삭제 전 프로세스 진행
                delFile.SyncLocalizationTable();
            }

            // 삭제 계속 진행
            return AssetDeleteResult.DidNotDelete;
        }
    }
#endif
}