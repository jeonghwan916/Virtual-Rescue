# Registry Components Guide

이 문서는 상황 씬 제작 담당자가 `DoorRegistryItem`과 `ModuleObjectRegistryItem`을 어떻게 사용하는지 정리한다.

핵심 목적은 상황 씬이 기본 집 모듈 씬의 오브젝트를 직접 참조하지 않게 하는 것이다. 기본 집 모듈 씬은 오브젝트를 ID로 등록하고, 상황 씬은 그 ID만 사용한다.

## 전체 구조

- `DoorRegistryItem`: 기본 집 모듈의 문을 `DoorRegistry`에 등록한다.
- `ModuleObjectRegistryItem`: 기본 집 모듈의 일반 오브젝트를 `ModuleObjectRegistry`에 등록한다.
- `SituationDoorLockOverride`: 상황 시작 시 지정한 문 ID들을 잠그고, 상황 리셋 시 원래 상태로 복구한다.
- `SituationTrapDoorTrigger`: 상황 시작 시 지정한 문 ID들을 함정 문으로 만들고, 처음 열린 문에서 화재 연출과 이벤트를 발생시킨다.
- `SituationObjectOverride`: 상황 시작 시 지정한 기본 오브젝트 ID들을 숨기고, 상황 리셋 시 다시 보이게 한다.

상황 씬에서는 기본 환경 씬의 GameObject를 Inspector에 직접 드래그하지 않는다. 대신 기본 환경 씬 담당자가 정한 ID 문자열을 사용한다.

## DoorRegistryItem

`DoorRegistryItem`은 문 ID와 `FireExitDoorController` 참조를 `DoorRegistry`에 등록하는 컴포넌트다.

### 기본 환경 씬 담당자 작업

1. 제어 대상 문 오브젝트를 선택한다.
   - 보통 `FireExitDoorController`가 붙어 있는 `DoorHinge` 오브젝트에 추가한다.
2. `DoorRegistryItem` 컴포넌트를 추가한다.
3. `Door Id`에 고유한 ID를 입력한다.
   - 예: `Exit_Stairs`
   - 예: `Kitchen_Door`
   - 예: `Bedroom_Hall_Door`
4. `Door Controller`에 해당 문 자신의 `FireExitDoorController`를 연결한다.
   - 같은 오브젝트에 `FireExitDoorController`가 있으면 자동으로 채워질 수 있다.
   - 그래도 Inspector에서 누락 여부를 확인한다.
5. 같은 씬 안에서 `Door Id`가 중복되지 않게 관리한다.

`Door Id`는 오브젝트 이름 검색용이 아니라 Registry의 Dictionary 조회 키다. 공백은 자동으로 앞뒤가 정리되지만, 대소문자나 철자가 다르면 다른 ID로 취급된다.

### 상황 씬 담당자 작업: 문 잠금

특정 상황에서 문을 잠가야 하면 상황 씬에 `SituationDoorLockOverride`를 사용한다.

1. 상황 씬의 루트 오브젝트나 `SituationController` 근처 오브젝트에 `SituationDoorLockOverride`를 추가한다.
2. `Situation Controller`가 자동 연결되지 않으면 해당 상황의 `SituationController`를 연결한다.
3. `Door IDs` 배열에 잠글 문 ID를 입력한다.
4. 상황을 실행해서 Console에 미등록 ID 경고가 없는지 확인한다.

예시:

```text
목표: 비상계단 문을 특정 상황 동안 잠근다.

기본 환경 씬:
DoorRegistryItem.Door Id = Exit_Stairs

상황 씬:
SituationDoorLockOverride.Door IDs = [Exit_Stairs]
```

상황이 활성화되면 해당 문에 `SetLocked(true)`가 적용된다. 상황이 리셋되거나 컴포넌트가 비활성화되면 기존 잠금 상태로 복구된다.

### 상황 씬 담당자 작업: 함정 문

문을 열었을 때 화재 연출 또는 실패 처리를 해야 하면 `SituationTrapDoorTrigger`를 사용한다.

1. 상황 씬의 루트 오브젝트나 `SituationController` 근처 오브젝트에 `SituationTrapDoorTrigger`를 추가한다.
2. `Situation Controller`가 자동 연결되지 않으면 해당 상황의 `SituationController`를 연결한다.
3. `Door IDs` 배열에 함정 문으로 만들 문 ID를 입력한다.
4. 상황 컨트롤러에서 `SituationTrapDoorTrigger.Triggered` 이벤트를 받아 실패, 연출, 대사 등을 처리한다.
5. 상황을 실행해서 문을 열었을 때 의도한 이벤트가 한 번만 발생하는지 확인한다.

예시:

```text
목표: 침실 문을 열면 화재 연출이 발생하고 상황이 실패한다.

기본 환경 씬:
DoorRegistryItem.Door Id = Bedroom_Hall_Door

상황 씬:
SituationTrapDoorTrigger.Door IDs = [Bedroom_Hall_Door]

상황 컨트롤러:
Triggered 이벤트에서 FailSituation 또는 상황별 실패 처리 호출
```

함정 문은 상황 활성화 시 `SetTrapped(true)`가 적용된다. 문이 처음 열리면 `ShowFire()`가 호출되고 `Triggered` 이벤트가 한 번 발생한다. 상황 리셋 시 기존 함정 상태로 복구된다.

## ModuleObjectRegistryItem

`ModuleObjectRegistryItem`은 기본 집 모듈의 일반 GameObject를 ID로 등록하는 컴포넌트다. 상황 씬에서 기본 오브젝트를 숨기고 상황 전용 오브젝트로 교체할 때 사용한다.

### 기본 환경 씬 담당자 작업

1. 상황에 따라 숨기거나 교체될 기본 오브젝트를 선택한다.
2. `ModuleObjectRegistryItem` 컴포넌트를 추가한다.
3. `Object Id`에 고유한 ID를 입력한다.
   - 예: `PowerStrip_Normal`
   - 예: `Kitchen_Table_Normal`
   - 예: `Balcony_Window_Normal`
4. `Target`을 설정한다.
   - 비워두면 `ModuleObjectRegistryItem`이 붙은 GameObject가 대상이다.
   - 여러 자식 오브젝트를 한 번에 숨겨야 하면 부모 GameObject를 `Target`에 넣는다.
5. 같은 씬 안에서 `Object Id`가 중복되지 않게 관리한다.

### 상황 씬 담당자 작업: 기본 오브젝트 숨김

상황 전용 오브젝트가 기본 오브젝트를 대체해야 하면 `SituationObjectOverride`를 사용한다.

1. 상황 씬에 상황 전용 오브젝트를 배치한다.
2. 상황 씬의 루트 오브젝트나 `SituationController` 근처 오브젝트에 `SituationObjectOverride`를 추가한다.
3. `Situation Controller`가 자동 연결되지 않으면 해당 상황의 `SituationController`를 연결한다.
4. `Module Object IDs` 배열에 숨길 기본 오브젝트 ID를 입력한다.
5. 상황을 실행해서 기본 오브젝트가 숨겨지고, 상황 종료 또는 리셋 시 다시 보이는지 확인한다.

예시:

```text
목표: 멀티탭 화재 상황에서 정상 멀티탭을 숨기고 불붙은 멀티탭을 보여준다.

기본 환경 씬:
정상 멀티탭에 ModuleObjectRegistryItem.Object Id = PowerStrip_Normal

상황 씬:
불붙은 멀티탭 프리팹 배치
SituationObjectOverride.Module Object IDs = [PowerStrip_Normal]
```

상황이 활성화되면 `PowerStrip_Normal` 대상 오브젝트가 `SetActive(false)` 된다. 상황 리셋 또는 비활성화 시 `SetActive(true)`로 복구된다.

## 담당자별 체크리스트

### 기본 환경 씬 담당자

- 문 제어가 필요한 오브젝트에는 `DoorRegistryItem`을 붙인다.
- 일반 오브젝트 숨김 또는 교체가 필요한 오브젝트에는 `ModuleObjectRegistryItem`을 붙인다.
- ID는 상황 씬 담당자와 공유한다.
- ID 중복을 피한다.
- 프리팹 공통 ID보다 씬 인스턴스별 ID를 우선한다.
- Play 모드에서 `DoorRegistry was not found`, `ModuleObjectRegistry was not found`, 중복 ID 경고가 없는지 확인한다.

### 상황 씬 담당자

- 기본 환경 씬 오브젝트를 직접 참조하지 않는다.
- 문 잠금은 `SituationDoorLockOverride`를 사용한다.
- 함정 문은 `SituationTrapDoorTrigger`를 사용한다.
- 기본 오브젝트 숨김은 `SituationObjectOverride`를 사용한다.
- Inspector 배열에는 기본 환경 씬 담당자가 공유한 ID를 정확히 입력한다.
- Play 모드에서 미등록 ID 경고가 없는지 확인한다.

## 주의사항

- Registry는 `Core` 또는 `LoopBase` 계열 씬에 먼저 존재해야 한다.
- 기본 집 모듈 씬이 로드될 때 Registry Item들이 자동 등록된다.
- 오브젝트가 파괴되거나 씬이 언로드되면 자동으로 Registry에서 해제된다.
- 상황 씬에서 `Find()`, 오브젝트 이름 검색, Hierarchy 직접 순회로 기본 오브젝트를 찾지 않는다.
- ID가 비어 있으면 등록되지 않고 Error가 출력된다.
- ID가 중복되면 먼저 등록된 항목이 유지되고 중복 항목은 등록되지 않는다.
- `DoorRegistryItem`은 `FireExitDoorController`가 반드시 필요하다.
- `ModuleObjectRegistryItem`의 `Target`이 비어 있으면 자기 GameObject를 제어한다.

## 빠른 예시

```text
문 잠금 상황
1. 기본 환경 문에 DoorRegistryItem 추가
2. Door Id = Exit_Stairs
3. 상황 씬에 SituationDoorLockOverride 추가
4. Door IDs = [Exit_Stairs]
```

```text
오브젝트 교체 상황
1. 기본 환경 오브젝트에 ModuleObjectRegistryItem 추가
2. Object Id = PowerStrip_Normal
3. 상황 씬에 불붙은 오브젝트 배치
4. 상황 씬에 SituationObjectOverride 추가
5. Module Object IDs = [PowerStrip_Normal]
```
