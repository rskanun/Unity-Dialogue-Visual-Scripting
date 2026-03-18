## Unity-Dialogue-Visual-Scripting

유니티에서 대화(시나리오) 흐름을 **그래프 형태로 시각적으로 설계**하고, 이를 **런타임에서 사용 가능한 객체 기반 데이터(`Scenario`, `Line` 등)**&#x200B;로 변환해 주는 도구입니다.  
에디터 전용 그래프 뷰에서 대사를 설계하고, 한 번의 저장으로 Addressables 및(선택적으로) Localization 시스템과 연동 가능한 형태의 시나리오 에셋을 생성합니다.

---

## 주요 기능

- **시각적 대화 그래프 편집기**

  - Unity Editor 상단 메뉴의 `Window/Dialogue Visual Scripting` 으로 열 수 있는 전용 그래프 에디터 제공
  - 노드 기반으로 대사 흐름, 분기, 이벤트, 이미지 표시 등을 구성

- **런타임 객체 데이터 자동 변환**

  - 그래프 상의 노드들을 `Scenario`/`ScenarioScene`/`Line` 계층 구조로 자동 변환
  - 직렬화 가능한 데이터 구조로 저장 후, 런타임 시 빠르게 복원 및 순회 가능

- **분기 선택(Select) 및 순차 진행 지원**

  - `SelectNode` 를 통해 다중 선택지 제공
  - `ScenarioScene` 의 열거자(`IEnumerator<Line>`)를 통해 코드에서 간단하게 대사를 순회하고, `SelectOption(int index)` 로 분기 선택

- **이벤트 실행 라인 지원**

  - `EventNode` → `EventLine` → `IDialogueEvent` 구조를 통해 대화 중 특정 시점에 커스텀 이벤트 실행 가능

- **이미지/오브젝트 제어 라인**

  - `ImageLine`, `DestroyLine` 등을 통해 대화 중 이미지 표시/제거, 타깃 오브젝트 제어용 데이터 표현

- **Addressables 자동 설정**

  - `ScenarioSettings` 와 `AddressableProcessor` 를 통해 시나리오 에셋을 Addressables 그룹에 자동 등록
  - 빌드 또는 플레이 모드 진입 시 해당 그룹/라벨을 자동 동기화

- **(선택) Unity Localization 연동**
  - `USE_LOCALIZATION` 심볼 및 관련 설정 사용 시, 대사/선택지를 Localization 테이블과 자동 동기화

---

## 프로젝트 구조 개요

프로젝트 루트 기준 주요 디렉터리/파일 구조는 다음과 같습니다.

```text
Runtime/
  Scenario.cs              # 전체 시나리오 ScriptableObject, ID → ScenarioScene 매핑 및 직렬화/복원
  ScenarioScene.cs         # 개별 시나리오 흐름을 나타내는 열거 가능한 라인 시퀀스
  ScenarioSettings.cs      # 시나리오 저장 디렉터리 및 Addressables 관련 설정용 ScriptableObject (Resources)
  Line/
    Line.cs                # 모든 라인의 기반 클래스 (guid, nextLineGuids, nextLines)
    Text/TextLine.cs       # 이름/대사를 표현하는 라인
    Select/SelectLine.cs   # 여러 선택지를 제공하는 분기 라인
    Image/ImageLine.cs     # 이미지, 위치, 색상 정보를 가진 라인
    Image/DestroyLine.cs   # 이미지/타깃 제거용 라인
    Event/EventLine.cs     # IDialogueEvent 를 실행하는 라인
    Event/IDialogueEvent.cs# 런타임 이벤트 인터페이스

Editor/
  VisualScriptingEditor.cs     # 메인 에디터 윈도우 (툴바, 파일 열기/저장 등)
  VisualScriptingGraphView.cs  # GraphView 구현, 노드/엣지 로드·저장 및 Scenario 빌드 로직 포함
  VisualScriptingGraphState.cs # 현재 열려 있는 그래프/테이블 상태 관리
  VisualScriptingProvider.cs   # 에디터와 그래프 상태를 연결하는 유틸리티
  VisualScriptingSettings.cs   # 에디터 설정 (Localization 사용 여부, 키 프리픽스, 최대 선택지 수 등)

  Node/
    LineNode.cs                # 공통 노드 베이스 클래스
    Tag/LineTag.cs             # 시나리오 시작을 나타내는 태그 노드(시나리오 ID 지정)
    Line/Text/TextNode.cs      # 대사 노드, TextLine 으로 변환
    Line/Select/SelectNode.cs  # 선택지 노드, SelectLine 으로 변환
    Line/Image/...             # 이미지 생성/변환/제거 노드, ImageLine/DestroyLine 으로 변환
    Line/Event/EventNode.cs    # 이벤트 노드, EventLine/IDialogueEvent 로 변환

  Data/
    GraphData.cs, EdgeData.cs                 # 그래프 뷰 상태 저장용 데이터
    ScenarioGraph.cs                          # 그래프 + 시나리오 메타데이터 에셋
    Node/*NodeData.cs                         # 각 노드의 직렬화 데이터

  Tool/
    NodeSearchWindow.cs, ImagePreviewer.cs    # 그래프 편의 기능
    GraphSettingEditor.cs, StyleSheetManager.cs

  Event/
    AddressableProcessor.cs                   # 빌드/플레이 직전 Addressables 그룹 자동 갱신
    GraphFileDeletionProcessor.cs             # 관련 그래프/시나리오 파일 삭제 처리

Styles/
  NodeStyle.uss, SettingStyle.uss             # GraphView 및 설정 UI 스타일

package.json                                  # Unity Package 메타 정보
LICENSE                                       # MIT License
README.md                                     # 본 문서
```

---

## 핵심 로직 설명

### 1. 그래프 → 시나리오 데이터 변환

- 에디터에서 사용자는 `LineTag`, `TextNode`, `SelectNode`, `EventNode`, `ImageNode` 등 노드를 배치하고 엣지로 연결합니다.
- `VisualScriptingGraphView.SaveScenario(ScenarioGraph graph)` 가 호출되면:
  - 현재 GraphView 의 노드/엣지 상태를 `GraphData` 에 저장 (`SaveGraph`).
  - 이어서 `BuildScenario(Scenario scenario)` 를 통해 실제 런타임에서 사용할 `Scenario` 객체를 구성합니다.
- `BuildScenario` 의 흐름
  - 모든 `LineTag` 노드(시나리오 ID의 시작점)를 찾고, 각각에 대해 DFS(깊이 우선 탐색)를 수행합니다.
  - 탐색 과정에서 `ILineProvider` 를 구현한 노드(`TextNode`, `SelectNode`, `EventNode`, `Image 관련 노드` 등)를 만나면:
    - 각 노드의 `ToLine()` 을 호출해 런타임용 `Line` 파생 클래스(`TextLine`, `SelectLine`, ...)로 변환
    - 출력 포트와 연결된 다음 노드들의 guid 를 `line.nextLineGuids` 에 기록
    - `Scenario.AddLine(시나리오ID, line)` 으로 시나리오에 추가

### 2. 시리얼라이즈 및 복원 구조 (`Scenario`)

- `Scenario`는 `ScriptableObject, ISerializationCallbackReceiver` 를 구현합니다.
- 에디터에서 저장 시:
  - 내부적으로 `ScenarioEntry(id, List<Line>)` 리스트(`serializedScenarios`) 형태로 직렬화됩니다.
- 런타임에서 로드 시(`OnAfterDeserialize`):
  - `serializedScenarios` 를 바탕으로 `Dictionary<int, ScenarioScene>` 를 구성
  - `guid` 를 키로 하는 `Line` 딕셔너리를 만든 후, 각 `nextLineGuids` 를 실제 `nextLines` 참조로 복원
  - `FindIntroLine` 을 통해, **다른 어떤 라인의 nextLines 에도 포함되지 않는 라인**을 해당 시나리오의 시작 라인으로 선택합니다.

### 3. 시나리오 진행 (`ScenarioScene` / `LineEnumerator`)

- `ScenarioScene` 은 하나의 시작 라인(`introLine`)과 내부 `LineEnumerator` 스택을 관리합니다.
- 대사 진행 절차:
  1. `var scene = scenario.GetScenarioScene(id);`
  2. `foreach (var line in scene)` 또는 `var enumerator = scene.GetEnumerator();`
  3. `Line` 타입에 따라(`TextLine`, `SelectLine`, `ImageLine`, `EventLine` 등) UI 및 게임 로직을 분기 처리
  4. 사용자가 선택지를 고르면 `scene.SelectOption(index)` 로 다음 분기 인덱스를 지정
- `LineEnumerator` 는:
  - 첫 호출 시 `introLine` 을 반환
  - 이후에는 현재 라인의 `nextLines[nextIndex]` 로 이동
  - `SelectOption(int index)` 로 다음 인덱스를 변경해 분기를 제어

### 4. Addressables 연동 (`ScenarioSettings` / `AddressableProcessor`)

- `ScenarioSettings` (`Resources/Scenario Settings.asset`) 를 통해:
  - 시나리오가 저장될 폴더(`ScenarioDirectory`)
  - Addressables 그룹 이름(`AddressableGroupName`)
  - 시나리오 레이블 프리픽스(`LabelPrefix`)
    를 설정합니다.
- `AddressableProcessor` 는:
  - 플레이 모드 진입 직전, 빌드 직전마다 `UpdateAddressableGroup()` 를 호출
  - 설정된 `ScenarioDirectory` 및 하위 폴더에서 `Scenario` 타입 에셋을 검색하여 지정된 그룹에 등록
  - 하위 폴더 이름을 기준으로 라벨을 생성하고, 각 시나리오에 라벨을 부여

### 5. Localization 연동 (선택)

- `USE_LOCALIZATION` 컴파일 심볼과 Unity Localization 패키지가 활성화된 경우:
  - `VisualScriptingSettings.UseLocalization` 이 `true` 일 때, `TextNode`, `SelectNode` 등은 Localization 테이블을 사용합니다.
  - `VisualScriptingGraphView.SyncDialogueTable`, `SyncSelectionTable` 이 그래프 저장 시 호출되어:
    - 텍스트 노드/선택지에 대한 키를 생성 (`DialogueKeyPrefix`, `SelectOptionKeyPrefix` 기반)
    - 해당 키와 실제 문자열을 Localization 테이블에 추가/갱신 및 삭제
  - 런타임에서는 키 기반으로 실제 문자열을 가져와 UI에 표시할 수 있습니다.

---

## 설치 방법

### 1. 요구 사항

- **Unity 2021.3 이상** (패키지 메타 정보 기준)
- **필수 패키지**
  - `com.unity.addressables` (버전 `1.19.19` 또는 호환 버전)
- **선택 패키지**
  - `Unity Localization` 패키지 (Localization 연동을 사용할 경우)

### 2. Git/UPM 패키지로 설치

1. Unity 메뉴에서 `Window > Package Manager` 를 엽니다.
2. 좌측 상단 `+` 버튼을 눌러 **"Add package from git URL..."** 을 선택합니다.
3. 이 저장소의 Git URL 을 입력하고 `Add` 를 누릅니다.
   - 예) `https://github.com/rskanun/Unity-Dialogue-Visual-Scripting.git`

### 3. 로컬 패키지로 설치

1. 이 프로젝트를 로컬에 클론(또는 다운로드)합니다.
2. Unity 프로젝트의 `Packages` 폴더 하위에 이 패키지 폴더를 위치시키거나,
3. `Window > Package Manager` 에서 `+` → **"Add package from disk..."** 를 선택한 뒤  
   이 패키지 루트의 `package.json` 파일을 선택합니다.

---

## 사용 방법 (기본 워크플로우)

### 1. 시나리오 설정 파일 준비

1. 메뉴 `Window > Dialogue Visual Scripting` 을 열면, 처음 사용 시 `ScenarioSettings` 가 자동 생성되거나 로드됩니다.
2. `ScenarioSettings` 에서:
   - `Scenario Directory` : 시나리오 에셋(Scenario)을 저장할 폴더 경로
   - `Addressable Group Name` : 시나리오를 등록할 Addressables 그룹 이름
   - `Label Prefix` : 하위 폴더명으로부터 생성될 라벨 접두어
     를 지정합니다.

### 2. 시나리오 그래프 생성

1. `Window > Dialogue Visual Scripting` 윈도우를 엽니다.
2. 상단 툴바의 `File > New File` 로 새로운 `ScenarioGraph` 를 생성합니다.
3. 그래프 뷰 배경에서 우클릭하여 `Create/Line Tag`, `Create/Text`, `Create/Select`, `Create/Event`, `Create/...` 등의 노드를 추가합니다.
4. `Line Tag` 노드에서 시나리오 ID 를 지정하고, 각 노드들을 포트로 연결하여 전체 흐름을 설계합니다.

### 3. 시나리오 에셋 저장

1. 툴바에서 `File > Save As...` 를 선택하여 원하는 위치에 `ScenarioGraph` 에셋을 저장합니다.
2. 이후부터는 `Save File` 또는 `Ctrl/Cmd + S` 로 덮어쓰기 저장이 가능합니다.
3. 저장 시:
   - 그래프 상태(`GraphData`)가 `ScenarioGraph` 에 저장
   - 동시에 런타임용 `Scenario` 에셋과 Addressables/Localization 설정이 업데이트됩니다.

### 4. 런타임에서 시나리오 사용 예시

아래는 `Scenario` 와 `ScenarioScene` 을 사용하는 간단한 예시 코드입니다.

```csharp
using UnityEngine;
using Rskanun.DialogueVisualScripting;

public class SimpleDialogueRunner : MonoBehaviour
{
    [SerializeField] private Scenario scenario;
    [SerializeField] private int scenarioId = 1;

    private ScenarioScene _scene;

    private void Start()
    {
        // ID 에 해당하는 시나리오 흐름 가져오기
        _scene = scenario.GetScenarioScene(scenarioId);

        // 코루틴으로 순차 진행
        StartCoroutine(Run());
    }

    private System.Collections.IEnumerator Run()
    {
        foreach (var line in _scene)
        {
            if (line is TextLine text)
            {
                Debug.Log($"{text.name}: {text.dialogue}");
                // 여기서 UI에 텍스트 출력 등 처리
            }
            else if (line is SelectLine select)
            {
                // 선택지 UI 노출 후 플레이어가 선택할 때까지 대기
                int chosenIndex = 0; // UI 결과로부터 선택 인덱스 결정
                _scene.SelectOption(chosenIndex);
            }
            else if (line is ImageLine image)
            {
                // 이미지 표시 처리
            }
            else if (line is DestroyLine destroy)
            {
                // 타깃 제거 처리
            }
            else if (line is EventLine evt)
            {
                evt.dialogueEvent?.Execute();
            }

            yield return null; // 프레임 대기 또는 입력 대기 로직 삽입 가능
        }
    }
}
```

위 코드는 기본적인 진행 방식 예시이며, 실제 프로젝트 상황에 맞게 UI 시스템, 입력 처리, Localization 등과 통합하여 사용하면 됩니다.

---

## 커스텀 Line / Node 확장 방법

이 패키지는 기본적으로 `TextLine`, `SelectLine`, `ImageLine`, `DestroyLine`, `EventLine` 등을 제공하지만,  
프로젝트 특성에 따라 **사용자 정의 대사 타입(커스텀 Line)** 을 추가해 확장할 수 있도록 설계되어 있습니다.  
커스텀 Line 을 추가하기 위해서는 **런타임용 `Line` 파생 클래스**와 이를 생성하는 **에디터용 노드(`LineNode + ILineProvider`)** 를 함께 구현해야 합니다.

### 1. 런타임용 커스텀 Line 클래스 작성

1. `Runtime/Line` 하위(또는 그와 유사한 위치)에 `Line` 을 상속하는 새 클래스를 생성합니다.
2. 해당 클래스는 시나리오에서 필요로 하는 데이터를 **순수 데이터 형태**로만 보관하며, 실제 표시/처리는 게임 코드에서 수행합니다.

예시:

```csharp
using UnityEngine;

[System.Serializable]
public class MyCustomLine : Line
{
    [SerializeField] private string _myValue;
    public string myValue => _myValue;

    public MyCustomLine(string guid, string value) : base(guid)
    {
        _myValue = value;
    }
}
```

- **중요 사항**
  - `Line` 기반 클래스가 이미 `guid`, `nextLineGuids`, `nextLines` 를 관리하므로, **분기/연결 정보는 따로 들고 있을 필요가 없습니다.**

### 2. 에디터용 NodeData 정의

에디터에서 **그래프 저장/로드 시 추가 상태를 유지**해야 한다면, `NodeData` 를 상속하는 전용 데이터 클래스를 정의합니다.

```csharp
using System;
using UnityEngine;
using Rskanun.DialogueVisualScripting.Editor;

[Serializable]
public class MyCustomNodeData : NodeData
{
    public string value;

    public override Type NodeType => typeof(MyCustomNode);
}
```

- `NodeType` 는 이 데이터를 사용하는 에디터 노드 타입을 반환해야 하며,  
  `VisualScriptingGraphView.LoadNode` 에서 이 정보를 이용해 노드를 복원합니다.

### 3. 에디터용 커스텀 Line 노드 구현 (`LineNode + ILineProvider`)

1. `Editor/Node/Line` 하위에 `LineNode` 를 상속하고 `ILineProvider` 를 구현하는 새 노드를 정의합니다.
2. `Draw()` 메서드에서 그래프 상에서 사용할 UI(필드, 포트 등)를 구성합니다.
3. `ToData()` 를 통해 노드를 직렬화 가능한 `NodeData` 로 변환하고,  
   `ToLine()` 에서 해당 데이터를 기반으로 **런타임용 커스텀 Line 인스턴스를 생성**합니다.

예시:

```csharp
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using Rskanun.DialogueVisualScripting;
using Rskanun.DialogueVisualScripting.Editor;

[NodeMenu("My Custom Line", Order = 50)]
public class MyCustomNode : LineNode, ILineProvider
{
    private TextField valueField;

    public MyCustomNode() : base() { }
    public MyCustomNode(string guid) : base(guid) { }
    public MyCustomNode(NodeData data) : base(data)
    {
        if (data is not MyCustomNodeData nodeData)
        {
            return;
        }

        // Draw() 이후에 필드가 초기화되어야 하므로, 생성자에서는 값만 저장/복원에 집중
        valueField?.SetValueWithoutNotify(nodeData.value);
    }

    public override NodeData ToData()
    {
        var data = new MyCustomNodeData();

        data.guid = guid;
        data.name = nodeName;
        data.pos = position;
        data.value = valueField.value;

        return data;
    }

    public Line ToLine()
    {
        var data = ToData() as MyCustomNodeData;
        return new MyCustomLine(data.guid, data.value);
    }

    public override void Draw()
    {
        base.Draw();

        // Input 포트
        var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
        inputPort.portName = "Prev";
        inputContainer.Add(inputPort);

        // Output 포트
        var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
        outputPort.portName = "Next";
        outputContainer.Add(outputPort);

        // 커스텀 데이터 입력용 필드
        valueField = new TextField("Value");
        extensionContainer.Add(valueField);

        RefreshExpandedState();
    }
}
```

- **핵심 포인트**
  - `ILineProvider.ToLine()` 에서 반드시 **런타임용 `Line` 파생 클래스**를 반환해야 합니다.
  - `VisualScriptingGraphView.BuildScenario` 는 `ILineProvider` 를 구현한 노드만 대상으로 `ToLine()` 을 호출하고,  
    해당 결과를 `Scenario.AddLine()` 에 넘기므로, 이 구현이 누락되면 런타임 시나리오에 반영되지 않습니다.
  - `NodeMenuAttribute` 를 통해 **그래프 우클릭 메뉴의 `Create/` 하위에 항목이 등록**됩니다.

### 4. 그래프 메뉴에 노드 노출 (`NodeMenuAttribute`)

`NodeMenuAttribute` 를 사용하면 별도의 등록 코드 없이 **그래프 우클릭 메뉴에 자동으로 노드가 노출**됩니다.

```csharp
[NodeMenu("My Custom Line", Order = 50)]
public class MyCustomNode : LineNode, ILineProvider
{
    ...
}
```

- `MenuName` : `Create/My Custom Line` 과 같이 메뉴 경로에 표시될 이름
- `Order` : 메뉴 정렬 순서(10 단위로 구분선이 추가되며, 기본 제공 노드들과의 위치를 제어 가능)

`VisualScriptingGraphView.GraphContextMenu` 가 `TypeCache.GetTypesWithAttribute<NodeMenuAttribute>()` 를 통해  
에디터 어셈블리 내 클래스를 자동 검색하고 메뉴를 구성합니다.

### 5. 빌드/런타임 시 동작 방식

- 커스텀 Line / Node 를 위 구조대로 구현하고 저장하면,
  - 그래프 저장 시 `GraphData` 와 커스텀 `NodeData` 가 함께 직렬화됩니다.
  - `BuildScenario` 호출 시 커스텀 노드의 `ToLine()` 이 호출되어 커스텀 `Line` 인스턴스가 `Scenario` 에 포함됩니다.
  - `Scenario.OnAfterDeserialize` 에서 다른 Line 과 동일하게 `guid` 및 `nextLines` 가 복원되며,
    런타임에서는 `foreach (var line in scene)` 순회 중 `MyCustomLine` 타입으로 캐스팅해 사용할 수 있습니다.

게임 코드에서는 다음과 같이 기존 분기 처리에 **커스텀 Line 타입을 추가**하면 됩니다.

```csharp
foreach (var line in scene)
{
    if (line is MyCustomLine custom)
    {
        // custom.myValue 를 이용한 UI/연출 처리
    }
    // 나머지 TextLine / SelectLine / ... 처리
}
```

이와 같은 구조를 통해, **핵심 프레임워크 코드는 그대로 유지한 채 프로젝트별로 다양한 대사 표현 타입을 손쉽게 추가**할 수 있습니다.

---

## 기술 스택

- **언어**

  - C# (Unity)

- **런타임/엔진**

  - Unity 2021.3 이상

- **에디터 기술**

  - `UnityEditor.Experimental.GraphView` 를 이용한 그래프 에디터
  - UIElements / UIToolkit (`UnityEngine.UIElements`, `UnityEditor.UIElements`)
  - `ScriptableObject`, `ScriptableSingleton` 기반 설정/데이터 관리

- **패키지/연동**
  - `com.unity.addressables` : 시나리오 에셋 Addressables 관리
  - (선택) `Unity Localization` : 대사/선택지 로컬라이제이션 연동

---

## 라이선스

이 프로젝트는 **MIT License** 하에 배포됩니다.  
자세한 내용은 `LICENSE` 파일을 참고하세요.
