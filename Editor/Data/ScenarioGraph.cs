using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using System;



#if USE_LOCALIZATION
using UnityEditor.Localization;
using UnityEngine.Localization.Tables;
#endif

namespace Rskanun.DialogueVisualScripting.Editor
{
    public class ScenarioGraph : Scenario
    {
        // 복사 확인용 에셋 GUID
        [SerializeField, HideInInspector] private string _assetInstanceId;

        // 빌드 이후 변경 내용 존재 여부
        private bool _isDirty;
        public bool IsDirty => _isDirty;

        // 에디터 전용 그래프 데이터
        [SerializeField]
        private GraphData _graphData = new GraphData();
        public GraphData graphData
        {
            get => _graphData;
            set => _graphData = value;
        }

        // 설정 데이터
        [SerializeField]
        private StringTableCollection _nameTableCollection;
        public StringTableCollection nameTableCollection
        {
            get => _nameTableCollection;
            set
            {
                if (_nameTableCollection == value) return;

                // 테이블 이름 값 업데이트
                nameTable = value?.name;

                _nameTableCollection = value;
            }
        }
        [SerializeField]
        private StringTableCollection _dialogueTableCollection;
        public StringTableCollection dialogueTableCollection
        {
            get => _dialogueTableCollection;
            set
            {
                if (_dialogueTableCollection == value) return;

                // 테이블 이름 값 업데이트
                dialogueTable = value?.name;

                // 대사 노드만 뽑아서 업데이트(다른 테이블의 노드 지우기 방지)
                var entries = _graphData.nodes.OfType<TextNodeData>()
                    .ToDictionary(data => data.dialogueKey, data => data.dialogue);

#if USE_LOCALIZATION
                // 로컬라이제이션 테이블 업데이트
                OnTableChanged(_dialogueTableCollection, value, entries);
#endif

                _dialogueTableCollection = value;
            }
        }
        [SerializeField]
        private StringTableCollection _selectionTableCollection;
        public StringTableCollection selectionTableCollection
        {
            get => _selectionTableCollection;
            set
            {
                if (_selectionTableCollection == value) return;

                // 테이블 이름 값 업데이트
                selectionTable = value?.name;

                // 대사 노드만 뽑아서 업데이트(다른 테이블의 노드 지우기 방지)
                var entries = _graphData.nodes.OfType<SelectNodeData>()
                    .SelectMany(data => data.optionKeys.Zip(data.options, (k, v) => new { k, v }))
                    .ToDictionary(pair => pair.k, pair => pair.v);

#if USE_LOCALIZATION
                // 로컬라이제이션 테이블 업데이트
                OnTableChanged(_selectionTableCollection, value, entries);
#endif

                _selectionTableCollection = value;
            }
        }

        private void OnValidate()
        {
            string curAssetGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(this));

            if (string.IsNullOrEmpty(_assetInstanceId))
            {
                _assetInstanceId = curAssetGuid;
            }

            // 복사 확인
            bool isAssetDuplicated = !string.IsNullOrEmpty(curAssetGuid) && _assetInstanceId != curAssetGuid;
            if (isAssetDuplicated)
            {
                // 복사 되었으면 모든 노드 GUID 새로 생성
                _assetInstanceId = curAssetGuid;
                RegeneratedAllNodesGuid();

                MarkAsUnbuilt();
                EditorUtility.SetDirty(this);

                return;
            }
        }

        private void RegeneratedAllNodesGuid()
        {
            // 기존 GUID -> 새 GUID 맵핑 테이블
            var guidMap = new Dictionary<string, string>(graphData.nodes.Count);

            // 새 GUID 설정
            foreach (var node in graphData.nodes)
            {
                string newGuid = Guid.NewGuid().ToString();
                guidMap[node.guid] = newGuid;
                node.guid = newGuid;
            }

            // 새 GUID에 맞춰 엣지 정보 변경
            foreach (var edge in graphData.edges)
            {
                if (guidMap.TryGetValue(edge.outputNodeGuid, out var newOutputGuid))
                {
                    edge.outputNodeGuid = newOutputGuid;
                }

                if (guidMap.TryGetValue(edge.inputNodeGuid, out var newInputGuid))
                {
                    edge.inputNodeGuid = newInputGuid;
                }
            }
        }

        /// <summary>
        /// 빌드 이후와 달라진 데이터 값이 있다고 알림
        /// </summary>
        public void MarkAsUnbuilt()
        {
            _isDirty = true;
        }

        /// <summary>
        /// 빌드 완료 후 내용 변경이 없어졌음을 알림
        /// </summary>
        public void MarkAsBuilt()
        {
            _isDirty = false;
        }

#if USE_LOCALIZATION
        /// <summary>
        /// 해당 에셋이 삭제될 때, 로컬라이제이션 테이블에 등록된 값들 삭제
        /// </summary>
        public void SyncLocalizationTable()
        {
            // 로컬라이제이션을 이용하는 경우에만 실행
            if (!VisualScriptingSettings.UseLocalization)
            {
                return;
            }

            // 로컬라이제이션을 사용하지만 테이블이 없는 경우에도 실행 X
            if (dialogueTableCollection == null || selectionTableCollection == null)
            {
                return;
            }

            // 현재 노드로 인해 생성된 로컬라이제이션 테이블 키 삭제
            foreach (var node in _graphData.nodes)
            {
                if (node is TextNodeData textNode)
                {
                    dialogueTableCollection.SharedData.RemoveKey(textNode.dialogueKey);
                }
                else if (node is SelectNodeData selectNode)
                {
                    foreach (var key in selectNode.optionKeys)
                    {
                        selectionTableCollection.SharedData.RemoveKey(key);
                    }
                }
            }
        }

        /// <summary>
        /// 사용되는 테이블 값이 바뀐 경우 데이터 이전
        /// </summary>
        private void OnTableChanged(StringTableCollection origin, StringTableCollection newTable, Dictionary<string, string> entries)
        {
            if (origin == null) return;

            var locale = VisualScriptingSettings.ProjectLocale;
            var table = newTable.GetTable(locale.Identifier) as StringTable;

            // 기존 테이블에 저장된 값은 지우고, 새로운 테이블로 옮기기
            foreach (var (k, v) in entries)
            {
                origin.SharedData.RemoveKey(k);
                table.AddEntry(k, v);
            }
        }
#endif
    }
}