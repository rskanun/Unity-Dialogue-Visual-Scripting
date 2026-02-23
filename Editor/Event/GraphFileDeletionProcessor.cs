using UnityEditor;

namespace Rskanun.DialogueVisualScripting.Editor
{
    public class GraphFileDeletionProcessor : AssetModificationProcessor
    {
        private static AssetDeleteResult OnWillDeleteAsset(string path, RemoveAssetOptions options)
        {
#if USE_LOCALIZATION
            var delFile = AssetDatabase.LoadAssetAtPath<ScenarioGraph>(path);

            // 삭제되는 에셋이 GraphFile인 경우
            if (delFile != null)
            {
                // 삭제 전 프로세스 진행
                delFile.SyncLocalizationTable();
            }
#endif

            // 삭제 계속 진행
            return AssetDeleteResult.DidNotDelete;
        }
    }
}