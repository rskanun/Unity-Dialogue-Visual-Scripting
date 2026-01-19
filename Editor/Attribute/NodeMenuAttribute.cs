using System;

namespace Rskanun.DialogueVisualScripting.Editor
{
    [AttributeUsage(AttributeTargets.Class)]
    public class NodeMenuAttribute : Attribute
    {
        public string MenuName { get; private set; }
        public int Order { get; set; }

        public NodeMenuAttribute(string menuName)
        {
            MenuName = menuName;
            Order = 1;
        }
    }
}