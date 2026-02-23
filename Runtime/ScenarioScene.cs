using System.Collections.Generic;
using System.Collections;

namespace Rskanun.DialogueVisualScripting
{
    public class ScenarioScene : IEnumerable<Line>
    {
        private Line introLine;

        // 현재 순회 중인 열거자
        private Stack<LineEnumerator> enumeratorStack = new();

        public ScenarioScene(Line introLine)
        {
            this.introLine = introLine;
        }

        public void SelectOption(int index)
        {
            // 가장 최근 생성된 열거자가 유효하지 않는 경우
            while (enumeratorStack.Count > 0 && !enumeratorStack.Peek().IsValid())
            {
                // 유효한 열거자가 나올 때까지 제거
                enumeratorStack.Pop();
            }

            // 가장 최근 생성된 열거자의 선택 옵션 설정
            if (enumeratorStack.TryPeek(out var enumerator))
            {
                enumerator.SelectOption(index);
            }
        }

        public IEnumerator<Line> GetEnumerator()
        {
            enumeratorStack.Push(new LineEnumerator(introLine));

            // 가장 나중에 들어온 열거자 리턴
            return enumeratorStack.Peek();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class LineEnumerator : IEnumerator<Line>
    {
        private Line introLine;     // 시작점 저장
        private Line currentLine;   // 현재 진행중인 대사
        private int nextIndex;      // 다음으로 선택할 선택지 인덱스 (초기값 0)

        // 첫 문장 실행 여부 저장 객체
        private bool isReading;

        public Line Current => currentLine;
        object IEnumerator.Current => currentLine;

        public LineEnumerator(Line introLine)
        {
            this.introLine = introLine;
        }

        public bool MoveNext()
        {
            if (!isReading)
            {
                // 시작 문장이 없는 경우 종료
                if (introLine == null) return false;

                currentLine = introLine;
                isReading = true;
                return true;
            }

            // 선택 가능한 범위에서 벗어난 경우 종료
            if (currentLine.nextLines.Count <= nextIndex)
            {
                return false;
            }

            // 이어진 대사 불러오기
            currentLine = currentLine.nextLines[nextIndex];

            // 다음 대사 선택에 영향을 끼치지 않기 위해 초기화
            nextIndex = 0;

            return currentLine != null;
        }

        public void Reset()
        {
            currentLine = introLine;
            isReading = false;
            nextIndex = 0;
        }

        public void Dispose()
        {
            // break와 같이 도중에 종료된 경우
            // 유효하지 않음을 표시
            currentLine = null;
        }

        public void SelectOption(int index)
        {
            // 선택지에 따른 다음 대사 선택
            nextIndex = index;
        }

        public bool IsValid()
        {
            return !isReading || currentLine != null;
        }
    }
}