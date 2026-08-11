# Situation Authoring Wizard 개발 계획

## 1. 문서 목적

이 문서는 `7 Days Unchecked`의 상황 콘텐츠 제작 과정에서 반복되는 Unity 씬 생성, 컴포넌트 추가, 참조 연결, 에셋 등록과 검증 작업을 줄이기 위한 에디터 툴의 개발 계획을 정리한다.

툴의 임시 명칭은 `Situation Authoring Wizard`이며, 개발자가 상황별 고유 게임플레이 구현과 공간 연출에 집중할 수 있도록 공통 설정 작업을 자동화하는 것을 목표로 한다.

이 문서는 구현 전 검토용 초안이다. 세부 정책과 UI 구성은 팀 검토 및 주석을 반영해 변경할 수 있다.

## 2. 배경

현재 상황 하나를 루프 시스템에 추가하려면 다음과 같은 작업이 필요하다.

1. 상황 폴더와 오버레이 씬을 생성한다.
2. 씬에 `SituationSceneRoot`를 정확히 하나 배치한다.
3. 구체적인 `SituationController`를 생성하고 Root에 연결한다.
4. 상황에 필요한 프리팹과 상호작용 오브젝트를 배치한다.
5. 필요하면 기본 집 오브젝트 대체, 문 잠금, 함정 문 등의 컴포넌트를 추가한다.
6. `SituationDefinition` 에셋을 생성하고 ID, 단계, 가중치, 최소 등장 날짜 등을 설정한다.
7. 2단계 상황이면 제한시간과 허용 출구를 설정한다.
8. 실제 랜덤 후보로 사용할 경우 Definition을 `LoopBase`의 `SituationSelector.Candidates`에 등록한다.
9. 상황 씬을 Build Settings에 등록한다.
10. 상황 씬에 불필요한 Player, Camera, Light, EventSystem 등이 들어 있지 않은지 확인한다.
11. 각 컴포넌트의 Inspector 참조와 Registry ID가 올바른지 확인한다.

이 과정은 상황마다 반복되며, 제작자가 루프 시스템의 내부 연결 구조와 등록 순서를 모두 알고 있어야 한다. Wizard는 이 반복 작업과 연결 실수를 줄이되 상황별 고유 구현에는 관여하지 않는다.

## 3. 목표

### 3.1 핵심 목표

- 상황 씬의 공통 골격을 버튼 한 번으로 생성한다.
- 상황 전용 `SituationController` 파생 C# 스크립트의 기본 골격을 생성한다.
- `SituationDefinition` 생성과 공통 필드 입력을 하나의 화면에서 처리한다.
- 사용자가 선택한 경우에만 `SituationSelector.Candidates`에 등록하고, Build Settings 등록은 자동화한다.
- 자주 사용하는 상황 컴포넌트를 안전하게 추가하고 참조를 자동 연결한다.
- 현재 상황이 루프 시스템 계약을 만족하는지 제작 단계에서 검증한다.
- 자동으로 변경할 내용을 실행 전에 확인할 수 있게 한다.
- 모든 생성 및 편집 작업은 가능한 범위에서 Unity Undo를 지원한다.

### 3.2 비목표

초기 버전에서는 다음 작업을 자동화하지 않는다.

- 상황별 고유 성공 및 실패 로직 구현
- 복잡한 XR 상호작용 자동 구성
- 최종 오브젝트 위치와 회전 결정
- 파티클, 조명, 사운드의 연출 튜닝
- 기존 상황 Controller를 범용 노드 그래프로 대체
- 기획 문장만 입력하면 전체 상황을 자동 생성하는 기능
- 8일차 엔딩 상황 생성 및 편집

툴의 자동화 범위를 제한하는 근본적인 이유는 각 상황을 만드는 개발자의 자율성을 최대한 보장하기 위해서다.

- Level 0과 Level 1은 개발자가 적절한 시점에 `ResolveSituation()`을 호출할 수 있으면 된다.
- Level 2는 루프 시스템에서 사용할 탈출로와 제한시간 규칙을 올바르게 설정하면 된다.
- 해결 조건에 도달하는 과정, 상호작용 방식, 실패 연출과 개별 시청각 연출은 상황 개발자가 자유롭게 구현한다.
- Wizard는 상황 구현 방식을 강제하지 않고 루프 시스템과 접하는 계약만 생성하고 검증한다.

## 4. 전제와 설계 원칙

### 4.1 전제

- 상황은 기본 집 모듈 위에 Additive로 로드되는 오버레이 씬이다.
- 상황 씬에는 `SituationSceneRoot`가 정확히 하나 존재해야 한다.
- 상황 씬에는 구체적인 `SituationController`가 하나 이상 필요하지만, Root가 참조하는 주 Controller는 하나다.
- 상황 Definition의 `SceneName`은 실제 상황 씬과 일치해야 한다.
- 실제 게임의 랜덤 후보로 사용할 상황만 `SituationSelector.Candidates`에 등록한다.
- 기본 집 오브젝트와 문은 직접 씬 참조하지 않고 Registry ID를 사용한다.
- 상황 씬에는 XR Player, Main Camera, Audio Listener, EventSystem, 전역 조명을 포함하지 않는다.
- 상황 제작 미리보기에 사용할 기본 집 모듈 목록은 `LoopBase`의 `DaySceneCoordinator`에 연결된 `HomeLayoutDefinition`에서 가져온다.

### 4.2 설계 원칙

- 기존 런타임 구조와 직렬화 필드를 가능한 한 변경하지 않는다.
- 에디터 전용 코드는 `Editor` 폴더 아래에 둔다.
- 생성 작업은 여러 번 실행해도 중복 등록이 생기지 않아야 한다.
- 자동 수정과 검증을 분리한다. 검증 버튼이 사용자 동의 없이 씬을 변경하지 않게 한다.
- 경로 또는 오브젝트 이름보다 Unity 오브젝트 참조와 Registry ID를 우선한다.
- 오류는 생성 작업을 막고, 경고는 사용자가 확인 후 진행할 수 있게 구분한다.
- 상황별 예외를 Wizard 본체에 계속 추가하지 않는다.
- Wizard는 제작 완료 여부를 별도 에셋 값으로 저장하지 않는다. 제작 상태는 현재 검증 결과로만 표시하며, 런타임의 상황 해결 상태와 혼동하지 않는다.

## 5. 사용자 작업 흐름

### 5.1 새 상황 생성

```text
Tools > Virtual Rescue > Situation Authoring
→ New Situation 탭 선택
→ 기본 정보 입력
→ 단계별 규칙 입력
→ Controller 클래스명, 저장 경로와 초기 프리팹 입력
→ 실제 랜덤 후보로 사용할 경우에만 Register as Candidate 체크
→ 변경 예정 항목 미리보기
→ Create Situation 실행
→ Controller 스크립트 생성 및 Unity 컴파일
→ 컴파일 완료 후 나머지 씬 생성 작업 자동 재개
→ 생성된 상황 씬을 기본 집과 Additive로 열기
  - 기본 집은 LoopBase의 DaySceneCoordinator에 연결된 HomeLayoutDefinition의 모듈 목록을 사용
→ 개발자가 위치, 연출, 고유 로직 작업
→ Validate Current Situation 실행
```

### 5.2 기존 상황 편집

```text
SituationDefinition 또는 상황 씬 선택
→ Open Existing 탭에서 불러오기
→ 현재 등록 및 참조 상태 확인
→ 필요한 공통 설정 수정
→ Apply Changes 실행
→ Validate 실행
```

### 5.3 상황 제작 완료 확인

```text
Validate Current Situation
→ 오류와 경고 목록 확인
→ 항목을 클릭해 대상 오브젝트 또는 에셋 선택
→ 안전한 항목은 Fix 버튼으로 수정
→ 전체 필수 검증 통과 여부 표시
```

## 6. 에디터 창 구성

메뉴 경로:

`Tools/Virtual Rescue/Situation Authoring`

초기 버전은 하나의 `EditorWindow` 안에 다음 탭을 제공한다.

### 6.1 New Situation

새 상황 씬과 Definition을 생성한다.

기본 입력 항목:

| 항목 | 설명 | 필수 여부 |
| --- | --- | --- |
| Display Name | 에디터에서 표시할 상황 이름 | 필수 |
| Situation ID | 저장 및 라디오 매칭에 사용하는 고유 ID | 필수 |
| Home Layout | `LoopBase`에서 확인한 기본 집 구성, 읽기 전용 표시 | 자동 |
| Location | `SituationLocation` enum으로 선택하는 씬 폴더 분류 | 필수 |
| Level | Level 0, Level 1, Level 2 | 필수 |
| Scene Name | 기본값은 `Scenario_{Location}_{Name}` | 필수 |
| Controller Class Name | 생성할 `SituationController` 파생 클래스명 | 필수 |
| Controller Namespace | 생성할 클래스의 namespace | 필수 |
| Controller Script Path | C# 스크립트를 저장할 폴더 | 필수 |
| Weight | 무작위 선택 가중치 | 필수 |
| Minimum Day | 최초 등장 가능 날짜 | 필수 |
| Register as Candidate | 생성 후 `LoopBase` 후보 배열에 등록할지 선택, 기본값 Off | 선택 |
| Uses Time Limit | Level 2 제한시간 사용 여부 | 조건부 |
| Time Limit Seconds | 제한시간 | 조건부 |
| Allowed Exits | Level 2에서 허용할 출구 | 조건부 |
| Initial Prefabs | 생성 시 상황 씬에 배치할 프리팹 | 선택 |
| Module Object IDs | 숨길 기본 집 오브젝트 ID | 선택 |
| Locked Door IDs | 잠글 문 ID | 선택 |
| Trap Door IDs | 함정으로 만들 문 ID | 선택 |

단계가 바뀌면 관련 없는 입력 항목은 숨기거나 비활성화한다.

#### 입력 UI 표시 규칙

- 모든 입력 항목의 Label에 `GUIContent` Tooltip을 제공한다.
- 마우스를 항목 위에 올리면 표의 설명에 해당하는 도움말과 값이 런타임에 미치는 영향을 표시한다.
- Tooltip은 단순 필드명 풀이보다 실제 사용 예시와 잘못 설정했을 때의 결과를 우선 설명한다.
- 필수 항목에는 `필수` 배지 또는 붉은 별표를 표시한다.
- 현재 선택에 따라 필요한 항목에는 `조건부` 배지를 표시한다.
- 입력하지 않아도 되는 항목에는 `선택` 배지를 표시한다.
- 필수, 조건부, 선택 항목은 색상만으로 구분하지 않고 텍스트와 아이콘을 함께 사용한다.
- 조건부 항목은 활성화 조건을 Tooltip과 항목 하단 HelpBox에 표시한다.
- 잘못된 필수 값은 생성 버튼을 누른 뒤가 아니라 입력 단계에서 바로 표시한다.
- `Register as Candidate`는 테스트용 상황이 실수로 실제 랜덤 선택에 포함되지 않도록 기본값을 Off로 둔다.
- `Register as Candidate`가 Off여도 Weight와 Minimum Day는 Definition에 저장한다. 나중에 후보로 등록할 때 Definition을 다시 만들 필요가 없게 하기 위함이다.

#### Location 폴더 규칙

- Location은 자유 문자열이 아니라 `SituationLocation` enum으로 고정한다.
- 각 enum 값은 상황 씬 루트 아래의 폴더 경로 하나와 대응한다.
- 선택한 Location 폴더가 아직 없으면 생성 예정 목록에 표시하고 생성 시 자동으로 만든다.
- 새로운 공간 폴더가 필요하면 `SituationLocation`에 값을 추가하고 경로 매핑 한 곳만 갱신하도록 구현한다.
- enum 값과 실제 경로의 대응이 없거나 중복되면 Wizard 초기화 단계에서 오류를 표시한다.

### 6.2 Building Blocks

현재 열린 상황 씬에 공통 기능을 추가하는 팔레트다.

초기 제공 후보:

- `SituationObjectOverride`
- `SituationDoorLockOverride`
- `SituationTrapDoorTrigger`
- 화재 프리팹
- 소화기 프리팹
- 관찰 영역 및 관찰 Target
- 비상계단 관련 구성
- 대피공간 관련 구성

컴포넌트를 추가할 때 현재 상황 Controller를 자동으로 연결한다. ID 기반 컴포넌트는 문자열 직접 입력 대신 프로젝트에 존재하는 `ModuleObjectId`와 `DoorId` 에셋 목록에서 선택하도록 한다.

### 6.3 Validate

현재 상황 씬, Definition, LoopBase 등록 상태와 Build Settings를 검사한다.

검사 결과는 다음 세 단계로 표시한다.

- Error: 플레이 또는 로드가 실패할 수 있으므로 완료 처리 불가
- Warning: 실행은 가능하지만 잘못된 동작 가능성이 있음
- Info: 권장 규칙 또는 수동 확인 항목

각 결과에는 다음 기능을 제공한다.

- 대상 오브젝트 또는 에셋 선택
- 관련 씬 열기
- 안전하게 자동 수정할 수 있는 항목의 `Fix` 버튼
- 검사 규칙에 대한 짧은 설명

### 6.4 Overview

프로젝트의 전체 상황 목록과 상태를 보여준다.

표시 후보:

| 열 | 내용 |
| --- | --- |
| ID | SituationDefinition ID |
| Level | 상황 단계 |
| Scene | 연결된 씬 |
| Controller | 주 Controller 타입 |
| Registered | SituationSelector 등록 여부 |
| Build | Build Settings 등록 여부 |
| Validation | Error, Warning 개수 |

이 탭은 1차 버전에 반드시 포함하지 않아도 된다.

## 7. 자동 생성 결과

`Create Situation` 실행 시 다음 작업을 순서대로 수행한다.

1. 입력값을 검증한다.
2. 생성될 에셋과 변경될 기존 에셋 목록을 미리 보여준다.
3. 상황 폴더와 Controller 스크립트 폴더가 없으면 생성한다.
4. `SituationController`를 상속한 상황 전용 C# 스크립트의 기본 골격을 생성한다.
5. 나머지 생성 입력을 `SessionState` 또는 에디터 전용 임시 상태에 보관한다.
6. `AssetDatabase.Refresh()`로 Unity 컴파일을 시작한다.
7. `[DidReloadScripts]` 이후 생성된 Controller 타입을 확인하고 보관한 생성 작업을 재개한다.
8. 빈 Additive 상황 씬을 생성한다.
9. 루트 GameObject를 만들고 `SituationSceneRoot`를 추가한다.
10. 상황용 자식 GameObject를 만들고 생성된 Controller를 추가한다.
11. Root에 Controller 참조를 연결한다.
12. 선택한 초기 프리팹을 상황용 자식 아래에 생성한다.
13. 선택 사항에 따라 Override 및 Door 관련 컴포넌트를 추가한다.
14. `SituationDefinition` 에셋을 생성하고 입력값을 저장한다.
15. `Register as Candidate`가 체크된 경우에만 Definition을 `SituationSelector.Candidates`에 중복 없이 등록한다.
16. 상황 씬을 Build Settings에 중복 없이 등록하고 활성화한다.
17. 에셋과 씬을 저장한다.
18. 생성 결과를 검증한다.
19. `LoopBase`의 `DaySceneCoordinator`에 연결된 `HomeLayoutDefinition`을 확인한다.
20. 해당 Definition의 `ModuleSceneNames`에 등록된 기본 집 모듈과 생성된 상황 씬을 함께 열 수 있는 버튼을 제공한다.

스크립트 생성은 Unity Domain Reload를 발생시키므로 하나의 동기 메서드 안에서 모든 생성 작업을 끝낼 수 없다. 컴파일 전 입력을 보관하고 컴파일 후 재개하는 2단계 작업으로 구현한다. 컴파일 오류가 발생하면 씬과 Definition 생성을 진행하지 않고, 생성된 스크립트와 재시도 방법을 사용자에게 보여준다.

중간 단계에서 실패하면 어떤 파일과 설정이 생성되었는지 명확히 보고해야 한다. 스크립트 파일 생성은 Unity Undo만으로 완전히 되돌리기 어려우므로 생성 전 최종 확인을 받고, 파일이 이미 존재하면 덮어쓰지 않는다. 씬 오브젝트와 에셋 편집은 가능한 범위에서 Undo를 지원한다.

## 8. 검증 규칙

### 8.1 Definition 검증

- ID가 비어 있지 않다.
- ID 앞뒤 공백이 없다.
- ID가 다른 Definition과 중복되지 않는다.
- 가중치가 1 이상이다.
- 최소 등장 날짜가 1~7 범위다.
- Scene Name이 비어 있지 않다.
- Scene Name과 실제 씬 에셋 이름이 일치한다.
- Level 2가 아니면 Level 2 전용 값이 결과 판정에 영향을 주지 않는다.
- 제한시간을 사용하는 Level 2는 시간이 0보다 크다.
- Level 2는 허용 출구를 하나 이상 가진다.
- Level 2 허용 출구에 Elevator가 포함되지 않는다.

### 8.2 상황 씬 검증

- `SituationSceneRoot`가 정확히 하나다.
- Root가 참조하는 Controller가 존재한다.
- Controller가 Root 또는 Root 자식이며 같은 씬에 속한다.
- Main Camera가 없다.
- Audio Listener가 없다.
- EventSystem이 없다.
- XR Player가 없다.
- 전역 조명 또는 전역 Volume이 없다.
- 기본 출구 오브젝트가 상황 씬에 포함되지 않는다.
- 저장되지 않은 변경 사항이 있으면 상태를 표시한다.

### 8.3 등록 상태 검증

- Definition이 `SituationSelector.Candidates`에 등록되어 있지 않은 상태는 테스트용 상황일 수 있으므로 Error로 처리하지 않고 Info로 표시한다.
- 미등록 Info에는 사용자가 명시적으로 후보에 추가할 수 있는 `Register Candidate` Fix를 제공한다.
- 등록된 Definition은 `SituationSelector.Candidates`에 정확히 한 번만 존재한다.
- 후보 목록에 null 항목이 없다.
- 후보 목록에 중복 ID가 없다.
- 실제 상황 씬이 Build Settings에 등록되어 있다.
- Build Settings 항목이 활성화되어 있다.

### 8.4 Registry 및 참조 검증

- `LoopBase`의 `DaySceneCoordinator`에 `HomeLayoutDefinition`이 연결되어 있다.
- `HomeLayoutDefinition.ModuleSceneNames`의 모든 이름에 대응하는 씬 에셋이 존재한다. 이 검사는 씬을 열지 않고 AssetDatabase에서 경로만 확인한다.
- `SituationObjectOverride`의 모든 `ModuleObjectId` 참조가 null이 아니며 유효한 ID 값을 가진다.
- `SituationDoorLockOverride`와 `SituationTrapDoorTrigger`의 모든 `DoorId` 참조가 null이 아니며 유효한 ID 값을 가진다.
- 같은 컴포넌트 안에 중복 ID가 없다.
- Override 컴포넌트가 유효한 상황 Controller를 참조한다.
- 상황 컴포넌트가 기본 집 씬 오브젝트를 직접 직렬화 참조하지 않는다.
- ID가 현재 Home Layout의 Registry Item에 실제 등록되었는지는 일반 Wizard 검증에서 모듈 씬을 열어 확인하지 않는다.

마지막 두 항목은 일반 검증만으로 완전한 보장이 어렵다. 실제 Registry 등록 여부는 Play Mode 통합 확인에서 검증하고, 필요성이 확인될 경우에만 사용자가 명시적으로 실행하는 별도의 심층 검증 기능을 추가한다.

## 9. 자동 수정 정책

다음 항목은 비교적 안전하므로 개별 `Fix` 기능을 제공할 수 있다.

- 누락된 Build Settings 등록
- 미등록 Definition의 `SituationSelector.Candidates` 등록
- 누락된 `SituationSceneRoot` 생성
- Root 아래 Controller가 하나뿐일 때 참조 자동 연결
- 중복된 동일 Definition 후보 제거
- 씬 이름과 Definition의 Scene Name 동기화
- 현재 상황 Controller 참조 연결

다음 항목은 의미 판단이 필요하므로 자동 수정하지 않는다.

- 여러 Controller 중 주 Controller 선택
- 허용 출구 자동 결정
- 어떤 기본 오브젝트를 숨길지 결정
- 어떤 문을 잠그거나 함정으로 만들지 결정
- 잘못 배치된 게임플레이 오브젝트 삭제
- 상황 단계 자동 변경

## 10. 템플릿 전략

초기에는 거대한 범용 템플릿 시스템을 만들지 않고, 다음 세 가지 기본 골격만 제공한다.

### Level 0 템플릿

- 기본 상황 Root와 Controller
- 선택적인 기본 오브젝트 대체
- 해결 후 Elevator 출구 사용

### Level 1 템플릿

- 기본 상황 Root와 Controller
- 화재 또는 대응 상호작용 배치 지점
- 해결 전 Elevator 사용 시 실패
- 해결 후 Elevator 출구 사용

### Level 2 템플릿

- 기본 상황 Root와 Controller
- 선택적인 제한시간 설정
- 허용 출구 설정
- 선택적인 문 잠금 및 함정 문 구성
- Elevator 사용 불가

템플릿은 실제 상황별 고유 프리팹을 포함하지 않고 공통 구성만 담당한다.

## 11. 기존 콘텐츠와의 관계

- Wizard는 현재 런타임 계약인 `SituationDefinition`, `SituationSceneRoot`, `SituationController`와 Registry 구조만 기준으로 동작한다.
- 기존 상황은 수정하거나 재생성하지 않고 읽기 전용 검증 대상으로 사용할 수 있다.
- 새 상황 생성 기능은 기존 상황별 구현을 참조하거나 특정 상황의 배치 규칙에 의존하지 않는다.
- 상황별 오브젝트 구성과 배치는 각 개발자가 직접 관리한다.
- Wizard 도입을 이유로 기존 상황 Controller의 고유 로직을 공통화하거나 변경하지 않는다.

## 12. 제안 코드 구조

구현 시 사용할 수 있는 초기 구조안이다. 이름과 분리는 구현 전에 재검토한다.

```text
Assets/02_Scripts/Editor/SituationAuthoring/
├─ SituationAuthoringWindow.cs
├─ SituationCreationRequest.cs
├─ SituationControllerScriptGenerator.cs
├─ SituationCreationResumeHandler.cs
├─ SituationCreationService.cs
├─ SituationRegistrationService.cs
├─ SituationValidationService.cs
├─ SituationFixService.cs
├─ SituationValidationResult.cs
├─ SituationLocation.cs
├─ SituationLocationPathMap.cs
└─ Inspectors/
   └─ SituationDefinitionEditor.cs
```

책임 분리:

- `SituationAuthoringWindow`: 입력과 결과 표시만 담당
- `SituationControllerScriptGenerator`: Controller C# 기본 골격 파일 생성
- `SituationCreationResumeHandler`: Domain Reload 이후 보류된 생성 작업 복원 및 재개
- `SituationCreationService`: 폴더, 씬, Root, Definition 생성
- `SituationRegistrationService`: Selector와 Build Settings 등록
- `SituationValidationService`: 읽기 전용 검사 수행
- `SituationFixService`: 사용자가 명시적으로 요청한 안전한 자동 수정 수행
- `SituationValidationResult`: 심각도, 메시지, 대상, Fix 가능 여부 표현

클래스 분리가 오히려 복잡해질 경우 초기 버전에서는 Creation과 Registration을 하나로 합칠 수 있다.

## 13. 단계별 개발 계획

### 1단계: 검증기와 안전한 자동 수정

구현 범위:

- 선택한 Definition과 씬 연결 확인
- Root 및 Controller 계약 검사
- SituationSelector 등록 여부 표시와 등록된 항목의 중복 검사
- Build Settings 등록 검사
- 상황 씬 금지 오브젝트 검사
- Level 2 설정 검사
- 누락된 등록, 명확한 단일 Controller 참조 등 안전한 항목의 개별 `Fix` 기능

검증 기준:

- 기존 모든 상황을 검사할 수 있다.
- 일반 검증 실행은 씬과 에셋을 변경하지 않는다.
- 사용자가 명시적으로 누른 개별 `Fix`만 해당 항목을 수정한다.
- 오류 항목을 클릭하면 관련 오브젝트 또는 에셋을 선택할 수 있다.
- 자동 수정은 1차 버전에 포함하며 Undo 또는 수정 전 확인 절차를 지원한다.

### 2단계: 새 상황 생성 마법사

구현 범위:

- 입력 UI
- Controller C# 기본 골격 생성
- Domain Reload 전 생성 요청 보관 및 컴파일 후 작업 재개
- 폴더와 씬 생성
- Root와 Controller 구성
- Definition 생성
- 선택적인 Selector 후보 등록 및 Build Settings 등록
- 생성 후 자동 검증

검증 기준:

- 생성 직후 기본 검증 오류가 0개다.
- 생성된 Controller가 `SituationController`를 상속하고 상황 씬 Root에 연결된다.
- 컴파일 실패 시 씬과 Definition 생성은 진행되지 않으며 생성 요청을 복구하거나 취소할 수 있다.
- `Register as Candidate`가 On이면 같은 Definition이 중복 없이 한 번 등록된다.
- `Register as Candidate`가 Off이면 Definition이 후보 배열에 추가되지 않으며 검증 오류도 발생하지 않는다.
- 생성 취소 또는 입력 오류 시 기존 에셋이 변경되지 않는다.

### 3단계: Building Blocks

구현 범위:

- Object Override 추가
- Door Lock Override 추가
- Trap Door Trigger 추가
- Registry ID 선택 UI
- 선택된 프리팹 추가

검증 기준:

- 추가한 컴포넌트에 Controller 참조가 자동 연결된다.
- null이거나 유효하지 않은 Registry ID 에셋은 선택할 수 없거나 명확한 경고가 표시된다.
- 같은 기능을 반복 추가할 때 중복 여부를 사용자에게 알린다.

### 4단계: 전체 상황 Overview 및 일괄 검증

구현 범위:

- 전체 Definition 목록
- 씬, 등록, Build 상태 표시
- 전체 프로젝트 일괄 검증
- 결과 필터와 정렬

검증 기준:

- 누락된 씬과 중복 ID를 전체 목록에서 바로 찾을 수 있다.
- 특정 결과에서 해당 Definition 또는 씬으로 이동할 수 있다.

## 14. 테스트 계획

### Edit Mode 테스트

- 유효한 입력으로 상황 씬과 Definition이 생성된다.
- Controller 클래스명과 namespace가 유효하지 않으면 생성을 거부한다.
- 같은 경로의 C# 스크립트가 이미 있으면 덮어쓰지 않는다.
- 생성된 C# 클래스가 `SituationController`를 상속한다.
- 빈 ID를 거부한다.
- 중복 ID를 거부한다.
- 기존 씬과 같은 경로 생성을 거부한다.
- Selector 후보 등록이 멱등적이다.
- `Register as Candidate`가 Off인 생성 요청은 Selector 후보 배열을 변경하지 않는다.
- 후보 미등록 Definition은 검증 결과에서 Error가 아니라 Info로 표시된다.
- Build Settings 등록이 멱등적이다.
- Level 2의 잘못된 출구 구성을 검출한다.
- Root가 없거나 여러 개인 씬을 검출한다.
- 잘못된 Controller 참조를 검출한다.
- null, 빈 값 또는 중복된 Registry ID 참조를 검출한다.

### 수동 에디터 테스트

- 현재 수정 중인 씬이 있을 때 저장 여부를 안전하게 확인한다.
- 생성된 씬이 기본 집과 Additive로 정상적으로 열린다.
- Undo 또는 취소 동작이 예상대로 작동한다.
- 검증 결과 클릭 시 올바른 대상이 선택된다.
- Unity 재시작 후 생성된 에셋과 등록 상태가 유지된다.
- Domain Reload 이후 보류된 생성 작업이 정확히 한 번만 재개된다.
- 컴파일 오류가 있을 때 불완전한 상황 씬과 Definition이 생성되지 않는다.

### Play Mode 통합 확인

- `Register as Candidate`가 On인 상황은 무작위 후보로 선택될 수 있다.
- `Register as Candidate`가 Off인 테스트용 상황은 무작위 후보로 선택되지 않는다.
- 상황 씬 로드 후 `Playing` 상태에 진입한다.
- 상황 해결과 출구 판정이 기존 루프 계약을 따른다.
- 날짜 전환 시 상황 씬이 정상적으로 언로드된다.
- 이전 상황의 Override와 이벤트 구독이 남지 않는다.

## 15. 완료 조건

최소 기능 버전은 다음 조건을 모두 만족하면 완료로 본다.

1. 개발자가 직접 Controller 파일을 만들거나 상속 코드를 작성하지 않고 새 상황의 공통 골격을 만들 수 있다.
2. 씬, Definition과 Build Settings 연결이 한 흐름에서 처리되며 Selector 등록 여부는 사용자가 선택할 수 있다.
3. Level 0, Level 1, Level 2 상황을 생성할 수 있다.
4. 생성된 상황이 기본 구조 검증을 통과한다.
5. 기존 상황도 동일한 검증기로 검사할 수 있다.
6. 생성 과정이 기존 에셋을 임의로 덮어쓰지 않는다.
7. 상황별 고유 로직과 공간 배치는 개발자가 계속 직접 제어할 수 있다.

## 16. 기대 효과

- 신규 상황의 초기 설정 시간을 단축한다.
- Inspector 참조 누락과 잘못된 씬 구성을 조기에 발견한다.
- 상황 제작자가 전체 루프 시스템의 내부 구조를 모두 외울 필요가 없어진다.
- 프로젝트의 상황 제작 규칙을 코드와 검증 결과로 일관되게 유지한다.

## 17. 확정 설계와 남은 검토 사항

### 17.1 Controller 스크립트 생성 지원

Wizard가 상황 전용 Controller C# 스크립트 생성까지 담당하는 것으로 확정한다. 상황 제작 담당자가 매번 파일을 만들고 `SituationController` 상속을 직접 작성하는 반복 작업을 제거하기 위함이다.

생성되는 기본 스크립트는 다음 기준을 따른다.

- 사용자가 입력한 클래스명과 namespace를 사용한다.
- `SituationController`를 상속한 `sealed` 클래스를 생성한다.
- 상황 개발자가 필요한 경우 구현할 수 있도록 `OnActivated()`, `OnResolved()`, `OnFailed()`, `OnReset()` 재정의 위치를 제공한다.
- `ResolveSituation()`과 `FailSituation()`을 호출하는 구체적인 조건은 생성하지 않는다.
- 클래스명, namespace, 저장 경로와 동일 파일 존재 여부를 생성 전에 검증한다.
- 기존 파일은 자동으로 덮어쓰지 않는다.
- 생성된 스크립트가 컴파일된 뒤 Wizard가 해당 타입을 찾아 상황 GameObject에 추가하고 `SituationSceneRoot`에 연결한다.

Unity는 새 C# 파일을 인식할 때 컴파일과 Domain Reload를 수행하므로 생성 과정은 두 단계로 나눈다.

```text
1단계: 입력 검증 → Controller 스크립트 생성 → 생성 요청 임시 보관
Domain Reload
2단계: Controller 타입 확인 → 씬/Definition 생성 → 등록 및 검증
```

컴파일 오류가 발생하면 2단계를 실행하지 않는다. Wizard는 보류된 요청과 생성된 스크립트 경로를 표시하고, 오류 수정 후 재개하거나 생성 요청을 취소할 수 있게 한다.

### 17.2 Registry ID 목록의 경량 조회 방식

모듈 씬을 자동으로 열어 Registry Item을 탐색하는 방식은 일반적인 에디터 Wizard 역할에 비해 무거울 수 있다. 씬 수가 늘어날수록 로딩 시간이 증가하고, 현재 열린 씬과 저장되지 않은 변경 사항을 보존하고 복원하는 처리도 필요하다. 따라서 이 탐색을 기본 제작 흐름에 포함하지 않는다.

1차 버전은 다음과 같이 동작한다.

1. `AssetDatabase.FindAssets()`로 `ModuleObjectId`와 `DoorId` ScriptableObject 에셋만 조회한다.
2. 조회 결과는 Wizard가 열린 동안 캐시하고, 프로젝트 변경 또는 사용자의 새로고침 요청이 있을 때만 갱신한다.
3. Building Blocks의 ID 선택 UI는 이 에셋 목록을 사용한다.
4. 일반 검증에서는 null 참조, 빈 ID, 중복 ID와 잘못된 에셋 타입만 검사한다.
5. `HomeLayoutDefinition.ModuleSceneNames`는 씬을 열지 않고 대응하는 씬 에셋 경로가 존재하는지만 확인한다.
6. 선택한 ID가 실제 모듈 씬의 Registry Item에 등록되었는지는 Play Mode 통합 확인에서 검증한다.

이 방식은 실제 Registry 등록 누락을 Edit Mode에서 완전히 검출하지 못한다는 한계가 있지만, Wizard의 일반 사용 비용과 씬 상태 변경 위험을 크게 줄인다.

추후 실제 누락 사고가 반복되어 필요성이 확인되면 `Deep Validate Home Layout` 같은 별도 명령을 추가할 수 있다. 이 기능은 사용자가 명시적으로 실행할 때만 모듈 씬을 순차 검사하며, 1차 버전 범위에는 포함하지 않는다.

### 17.3 확정된 범위

- 기본 집 미리보기의 기준은 `LoopBase`의 `DaySceneCoordinator`에 연결된 `HomeLayoutDefinition`으로 한다.
- Location은 `SituationLocation` enum으로 관리하며 새 공간 추가 시 enum과 중앙 경로 매핑을 확장한다.
- 안전한 자동 수정 기능은 1차 버전에 포함한다.
- Controller C# 기본 골격 생성과 컴파일 후 작업 재개를 1차 버전에 포함한다.
- `SituationSelector.Candidates` 등록은 기본값이 Off인 선택 사항으로 제공한다.
- 8일차 엔딩 상황은 별도로 제작하며 현재 Wizard 범위에 포함하지 않는다.
- 제작 완료 여부는 에셋에 기록하지 않고 현재 검증 결과로만 표시한다.
- 런타임의 상황 해결 여부는 기존 게임 흐름이 관리하며 Wizard가 변경하지 않는다.

## 18. 검토 메모

이 구역은 팀 검토와 주석을 위한 공간이다.

- [ ] Wizard의 사용자 범위: 프로그래머만 사용하는가, 기획자와 아티스트도 사용하는가?
- [ ] 상황 생성 시 반드시 배치해야 하는 공통 오브젝트가 더 있는가?
- [ ] Level별 기본 템플릿 구성이 실제 제작 흐름과 일치하는가?
- [ ] 자동으로 수정하면 안 되는 프로젝트 설정이 있는가?
- [ ] 검증 실패 시 빌드를 막을 필요가 있는가?
- [ ] 생성할 Controller 기본 스크립트에 네 개의 생명주기 메서드를 모두 표시할 것인가, 필요한 최소 예시만 표시할 것인가?
- [ ] 컴파일 오류 후 보류된 생성 요청을 자동 재개할 것인가, 사용자가 `Resume` 버튼을 눌러 재개할 것인가?
- [ ] Registry 심층 검증이 실제로 필요해질 경우 2차 기능으로 추가할 것인가?
