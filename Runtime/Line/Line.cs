using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rskanun.DialogueVisualScripting
{
    [Serializable]
    public class Line
    {
        [SerializeField, HideInInspector]
        private string _guid;
        public string guid => _guid;

        [SerializeField]
        // 에셋 저장 시 가지게 될 연결 라인 guid
        private List<string> _nextLineGuids = new();
        public List<string> nextLineGuids
        {
            get => _nextLineGuids;
            set => _nextLineGuids = value;
        }

        [NonSerialized]
        // 연결 리스트 형태로 연결된 라인 소지
        private List<Line> _nextLines = new();
        public List<Line> nextLines
        {
            get => _nextLines;
            set => _nextLines = value;
        }

        public Line(string guid)
        {
            _guid = guid;
        }
    }
}