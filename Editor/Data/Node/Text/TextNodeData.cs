using System;

namespace Rskanun.DialogueVisualScripting.Editor
{
    [Serializable]
    public class TextNodeData : NodeData
    {
        public override Type NodeType => typeof(TextNode);
        public string speakerKey; // 로컬라이제이션에 등록된 이름 키 값
        public string speaker; // 로컬라이제이션을 쓰지 않는 경우 저장될 이름
        public string dialogueKey; // 로컬라이제이션에 등록된 대사 키 값
        public string dialogue; // 로컬라이제이션을 쓰지 않는 경우 저장될 대사
    }
}