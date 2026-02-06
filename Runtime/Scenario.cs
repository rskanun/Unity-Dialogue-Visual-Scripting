using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Rskanun.DialogueVisualScripting
{
    public class Scenario : ScriptableObject, ISerializationCallbackReceiver
    {
        private Dictionary<int, ScenarioScene> scenarios = new();
        public IEnumerable<int> IDs => scenarios.Keys;

        [SerializeField]
        private List<ScenarioEntry> serializedScenarios = new();

#if USE_LOCALIZATION
        [SerializeField]
        private string _nameTable;
        public string nameTable
        {
            get => _nameTable;
            set => _nameTable = value;
        }

        [SerializeField]
        private string _dialogueTable;
        public string dialogueTable
        {
            get => _dialogueTable;
            set => _dialogueTable = value;
        }

        [SerializeField]
        private string _selectionTable;
        public string selectionTable
        {
            get => _selectionTable;
            set => _selectionTable = value;
        }
#endif

        public void OnBeforeSerialize() { }
        public void OnAfterDeserialize()
        {
            // 직렬화시킨 구조를 Dictionary 형태로 변경
            scenarios = serializedScenarios.ToDictionary(
                entry => entry.id,
                entry => new ScenarioScene(FindIntroLine(entry.lines))
            );

            // guid로 저장된 연결 라인 값에 실제 값 넣기
            // 빠른 탐색을 위한 Dictionary 타입으로 변경
            var dict = serializedScenarios.SelectMany(entry => entry.lines)
                        .ToDictionary(line => line.guid, line => line);

            foreach (var line in dict.Values)
            {
                // 다음 라인 값 설정
                line.nextLines = new List<Line>();
                foreach (var guid in line.nextLineGuids)
                {
                    if (dict.TryGetValue(guid, out var nextLine))
                    {
                        line.nextLines.Add(nextLine);
                    }
                }
            }
        }

        private Line FindIntroLine(List<Line> lines)
        {
            var unused = lines.ToHashSet();

            // 가장 처음 시작되는 라인 찾기
            foreach (var line in lines)
            {
                // 연결된 라인이 없는 경우 넘기기
                if (line.nextLines == null) continue;

                // 연결된 라인은 첫 라인이 아니므로 제거
                foreach (var nextLine in line.nextLines)
                {
                    unused.Remove(nextLine);
                }
            }

            return unused.FirstOrDefault();
        }

#if UNITY_EDITOR
        /// <summary>
        /// 시나리오의 대사 데이터 초기화
        /// </summary>
        public void LineClear()
        {
            serializedScenarios.Clear();
            scenarios.Clear();
        }
#endif

        /// <summary>
        /// 해당 번호의 시나리오에 Line 추가
        /// </summary>
        /// <param name="num">Line을 추가할 시나리오 번호</param>
        public void AddLine(int num, Line line)
        {
            // 직렬화 형태로 임시 추가
            var entry = serializedScenarios.FirstOrDefault(e => e.id == num);
            if (entry == null)
            {
                entry = new ScenarioEntry(num, new List<Line>());
                serializedScenarios.Add(entry);
            }

            entry.lines.Add(line);
        }

        /// <summary>
        /// 번호에 맞는 Line 배열의 시작 부분 가져오기
        /// </summary>
        /// <param name="num">가져올 시나리오 번호</param>
        public ScenarioScene GetScenarioScene(int num)
        {
            // 해당 번호의 시나리오가 없거나 로드되지 않았다면
            if (ContainsKey(num) == false)
            {
                // 빈 값 리턴
                return null;
            }

            // 해당 번호의 씬 리턴
            return scenarios[num];
        }

        public bool ContainsKey(int id)
        {
            return scenarios.ContainsKey(id);
        }

        // 직렬화 저장용 객체
        [System.Serializable]
        private class ScenarioEntry
        {
            public int id;
            [SerializeReference]
            public List<Line> lines = new();

            public ScenarioEntry(int id, List<Line> lines)
            {
                this.id = id;
                this.lines = lines;
            }
        }
    }
}