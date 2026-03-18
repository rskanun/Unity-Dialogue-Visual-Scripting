namespace Rskanun.DialogueVisualScripting
{
    public class ScenarioScene
    {
        private Line current;
        private bool isReading;

        public ScenarioScene(Line intro)
        {
            current = intro;
        }

        public Line GetNext(int nextIndex = 0)
        {
            // 첫 시작인 경우
            if (!isReading)
            {
                isReading = true;
                return current;
            }

            // 다음 대사가 없거나 범위에서 벗어난 경우
            if (current == null || current.nextLines == null || current.nextLines.Count <= nextIndex)
            {
                return null;
            }

            return current = current.nextLines[nextIndex];
        }

        public void Reset()
        {
            current = null;
            isReading = false;
        }
    }
}