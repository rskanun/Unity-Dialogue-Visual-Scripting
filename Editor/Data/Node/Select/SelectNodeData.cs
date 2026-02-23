using System;
using System.Collections.Generic;

namespace Rskanun.DialogueVisualScripting.Editor
{
    [Serializable]
    public class SelectNodeData : NodeData
    {
        public override Type NodeType => typeof(SelectNode);

        // 로컬라이제이션에 등록된 선택지 키 값
        public List<string> optionKeys = new();

        // 로컬라이제이션을 사용하지 않는 경우 사용될 선택지
        public List<string> options = new();
    }
}