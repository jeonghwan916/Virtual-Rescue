# Situation Authoring Wizard 사용 설명서

상황 씬, `SituationController` 스크립트, `SituationDefinition`을 만들고 관리하는 Unity Editor 도구입니다.

## Wizard 열기

Unity 상단 메뉴에서 다음 항목을 선택합니다.

```text
Tools > Virtual Rescue > Situation Authoring
```

## 새 상황 만들기

`New Situation` 탭에서 작업합니다.

1. `Display Name`과 `Situation ID`를 입력합니다.
2. `Level`과 `Location`을 선택합니다.
3. Scene 이름과 Controller 클래스 정보를 입력합니다.
4. 필요한 프리팹, Module Object ID, Door ID를 선택합니다.
5. 실제 게임의 무작위 후보로 사용할 경우에만 `Register as Candidate`를 체크합니다.
6. `Planned Assets`에서 생성될 경로를 확인합니다.
7. `Create Situation`을 누릅니다.
8. Unity의 스크립트 컴파일이 끝날 때까지 기다립니다. 컴파일 후 씬과 Definition 생성이 자동으로 이어집니다.

기본 입력값은 다음과 같습니다.

```text
Display Name: Location_Situation
Situation ID: location.situation
```

두 값은 실제 상황에 맞게 변경해야 합니다. `Situation ID`는 다른 상황과 중복될 수 없습니다.

### 자동 생성 경로

Scene과 Controller 스크립트는 선택한 Location과 Level에 따라 자동 저장됩니다.

```text
Scene
Assets/01_Scenes/Situation/{Location}/{Level}

Controller
Assets/02_Scripts/10_Situations/{Location}/{Level}
```

예를 들어 `Balcony`, `Level2`를 선택하면 다음 폴더를 사용합니다.

```text
Assets/01_Scenes/Situation/Balcony/Level2
Assets/02_Scripts/10_Situations/Balcony/Level2
```

## 새 Location 추가하기

필요한 공간이 Location 목록에 없다면 `New Situation` 탭에서 추가할 수 있습니다.

1. Location 영역의 `Add New Location`을 누릅니다.
2. 다음 값을 입력합니다.
   - `Display Name`: 드롭다운에 표시할 이름. 예: `Room B`
   - `Location ID`: 내부 구분용 고유 ID. 예: `room-b`
   - `Scene Folder`: 씬 폴더 이름. 예: `RoomB`
   - `Controller Folder`: 스크립트 폴더 이름. 예: `RoomB`
3. `Save Location`을 누릅니다.

저장된 항목은 `SituationLocationCatalog.asset`에 추가되고 Location 드롭다운에서 바로 선택할 수 있습니다.

`Location ID`에는 영문 소문자, 숫자, `.`, `_`, `-`만 사용할 수 있습니다. 첫 글자는 영문 소문자 또는 숫자여야 합니다.

```text
사용 가능: roomb, room-b, room_b, room.b
사용 불가: roomB, Room B, 욕실
```

## Door ID 배치도 확인하기

`Locked Door IDs`와 `Trap Door IDs` 아래의 `Door ID 배치도 보기` 버튼을 누르면 문 위치 참고 이미지가 별도 창에 표시됩니다.

참고 이미지 파일:

```text
Assets/02_Scripts/Editor/SituationAuthoring/doorIDs.png
```

이미지를 변경하려면 같은 경로와 파일명으로 새 PNG를 교체합니다.

## 기존 상황 설정 수정하기

`Edit Existing` 탭은 기존 `SituationDefinition`의 설정을 수정할 때 사용합니다.

1. `Definition`에서 수정할 에셋을 선택합니다.
2. Level, Weight, Minimum Day와 Level 2 규칙을 수정합니다.
3. `Apply Definition Changes`를 누릅니다.

이 탭에서는 다음 작업도 할 수 있습니다.

- Candidate 등록 또는 해제
- Build Settings 등록
- 상황 씬 열기
- Home Layout과 함께 열기
- 선택한 상황 검증

`ID`와 `Scene Name`은 이 탭에서 직접 수정할 수 없습니다.

## 상황 씬에 기능 추가하기

`Building Blocks` 탭은 현재 열려 있는 상황 씬의 GameObject에 공통 기능을 추가할 때 사용합니다.

1. 수정할 상황 씬을 엽니다.
2. `Situation Controller`와 `Target Object`를 확인합니다.
3. 필요한 Module Object ID 또는 Door ID를 선택합니다.
4. 목적에 맞는 버튼을 누릅니다.

```text
Add Object Override     기본 집 오브젝트를 상황 중 대체하거나 숨길 때
Add Door Lock Override  선택한 문을 잠글 때
Add Trap Door Trigger   선택한 문에 함정 동작을 추가할 때
Add Selected Prefab     대상 오브젝트 아래에 프리팹을 추가할 때
```

`Building Blocks`는 성공·실패 판정 같은 상황 고유 로직을 작성하지 않습니다. 해당 로직은 생성된 `SituationController` 스크립트에 구현해야 합니다.

## 상황 검증하기

`Validate` 탭에서 `SituationDefinition`을 선택하고 `Validate Current Situation`을 누릅니다.

주요 확인 항목:

- 상황 씬과 Definition 연결 상태
- `SituationSceneRoot`와 Controller 구성
- Candidate 등록 상태
- Build Settings 등록 상태
- Level 2 제한시간과 허용 출구
- 누락되거나 중복된 ID

안전하게 자동 수정할 수 있는 항목은 결과 옆의 `Fix` 버튼으로 처리할 수 있습니다.

## Candidate 등록 주의사항

`Register as Candidate`의 기본값은 꺼져 있습니다.

- 체크함: 생성 후 실제 게임의 무작위 상황 후보에 추가
- 체크하지 않음: 테스트용 씬과 Definition만 생성

테스트용 상황을 실수로 게임 후보에 포함하지 않도록, 실제 플레이에 사용할 준비가 끝났을 때만 체크하는 것을 권장합니다.

## 문제가 발생했을 때

### Create Situation 버튼이 비활성화됨

화면에 표시된 오류를 먼저 확인합니다. 필수 입력 누락, 잘못된 클래스명, 중복 파일 경로 또는 Level 2 규칙 오류가 원인일 수 있습니다.

### Location이 저장되지 않음

`Save Location` 실패 팝업의 내용을 확인합니다. ID의 대문자·공백, 중복 ID, 중복 Scene 폴더 또는 잘못된 폴더명이 주된 원인입니다.

### Controller 생성 후 작업이 멈춤

Unity Console의 컴파일 오류를 해결합니다. Wizard 상단에 보류 중인 요청이 표시되면 컴파일 해결 후 재개하거나 `Cancel Pending Request`로 취소할 수 있습니다.

### 기존 상황과 같은 이름의 파일이 있음

Wizard는 기존 Scene, Controller 또는 Definition 파일을 덮어쓰지 않습니다. 이름을 변경하거나 기존 상황은 `Edit Existing`에서 수정합니다.
