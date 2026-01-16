namespace Rskanun.DialogueVisualScripting.Editor
{
    public static class EventContentFactory
    {
        public static IEventContent Create(DialogueEventType type)
        {
            return type switch
            {
                DialogueEventType.Teleport => new TeleportEventContent(),
                _ => new NoneEventContent(),
            };
        }
    }
}