using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Rskanun.DialogueVisualScripting.Editor
{
    public static class EventContentFactory
    {
        private static Dictionary<string, Type> contentLookup;

        static EventContentFactory()
        {
            contentLookup = new();

            var types = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.IsSubclassOf(typeof(EventNodeData)) && !t.IsAbstract);

            foreach (var type in types)
            {
                // 타입에 맞는 이벤트 데이터 객체 임시 생성
                if (Activator.CreateInstance(type) is EventNodeData data)
                {
                    // 이벤트 이름에 맞는 Content Type 설정
                    contentLookup[data.EventName] = data.ContentType;
                }
            }
        }

        public static IEventContent Create(string eventName)
        {
            if (contentLookup.TryGetValue(eventName, out Type contentType))
            {
                return Activator.CreateInstance(contentType) as IEventContent;
            }

            return new NoneEventContent();
        }
    }
}