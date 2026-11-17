# Grid Spawn 후속 작업 기록

## 현재까지 완료된 작업

- `InGame_CoinGameScene`의 `Map/Ground`와 `Map/Environment` 구조를 측정했다.
- `Environment` Collider 89개와 `Grass` Collider 16개를 분석했다.
- `Border_6_7` 블록을 기준으로 월드 grid 간격을 계산했다.
  - X 간격: 약 1.8
  - Z 간격: 약 1.8
  - 논리 grid: 16×16
- `decorative_block_yellow` 26개의 X·Z 위치를 Border lattice에 맞춰 보정했다.
- Grass 16개는 이미 기준 lattice에 정렬되어 있어 유의미한 보정이 없었다.
- Ground mesh가 정사각형이 아니므로 Renderer bounds를 grid 기준으로 사용하지 않는다.
- grid 측정 도구를 추가했다.
  - `Assets/_CoinGame/Scripts/Editor/GridMeasurementTool.cs`
  - 메뉴: `CoinGame/Measure Grid Layout`
- Border 기준 정렬 도구를 추가했다.
  - `Assets/_CoinGame/Scripts/Editor/GridAlignmentTool.cs`
  - 메뉴: `CoinGame/Align Voxel Props To Border Grid`
- 정적 grid 데이터를 베이크했다.
  - `Assets/_CoinGame/Scripts/Utilities/GridSpawnMap.cs`
  - `Assets/_CoinGame/Scripts/Editor/GridSpawnMapBaker.cs`
  - `Assets/_CoinGame/Generated/GridSpawnMap.asset`
- 현재 베이크 결과:
  - 전체 셀: 256개
  - 정적 차단 셀: 102개
  - 플레이어 영역: 16개
  - Collector 영역: 8개
  - Disruptor 영역: 8개
  - CoinZone 메타데이터 셀: 0개
  - CrateZone 메타데이터 셀: 0개
  - 실제 동적 스폰 후보: Ground 256개 중 정적 차단 102개를 제외한 154개
- `GridSpawnReservation`에 Player/Coin/Crate 공용 셀 소유권과 동적 reservation ID를 추가했다.
- `NetworkSpawner`의 코인 위치 선택을 전체 Ground 셀 예약으로 전환했다. 정적 차단 셀만 제외하며, 현재 씬에는 CoinZone을 만들지 않는다.
- `NetworkSpawner`(Crate 종류)의 상자 위치 선택을 전체 Ground 셀 예약으로 전환했다. 정적 차단 셀만 제외하며, 현재 씬에는 CrateZone을 만들지 않는다.
- 동적 후보는 셀 배열 순서대로 선택하지 않고 무작위 유효 Ground 셀을 선택해 한 줄 생성 편향을 제거했다.
- 코인 Despawn/수집 시 `NetworkCoinController.OnNetworkDespawn`에서 예약을 반환한다.
- Coin/Crate 공통 `NetworkSpawner`에 GridSpawnMap과 SpawnPlacementSettings를 연결했다. 기존 NetworkCrateSpawner는 컴포넌트·참조 이전 후 제거했다.
- Persistent/InGame 씬의 스포너에 grid asset 참조를 할당하는 Editor 메뉴를 추가했다.
  - 메뉴: `CoinGame/Assign Grid Spawn Map References`
- Persistent 씬에서 발생한 교차 씬 Player Zone 참조는 grid 사용을 위해 빈 목록으로 정리했다.

## 아직 완료되지 않은 작업

### 1. 플레이어 grid 재배치 검증

- 실제 Host 1명 + Client 1명으로 역할별 Player Zone에 배치되는지 확인해야 한다.
- `NetworkRoundState`의 라운드 시작, 사망 후 부활, 라운드 클리어 재배치에서 예약 해제·재예약을 확인해야 한다.
- 연결 해제 시 `GridSpawnReservation.ReleasePlayer`를 호출하는 경로는 구현되어 있으며 실제 재접속 검증은 남아 있다.
- grid 예약 실패 시 로그와 안전한 실패 처리를 추가해야 한다.

### 2. 코인 스포너 전환

- 실제 Host/Client에서 라운드별 코인 수가 Ground 유효 셀 154개 범위와 플레이어 예약을 고려해 충족되는지 확인해야 한다.
- `BaseSpawner` 풀 재사용 경로에서 `OnNetworkSpawn`/`OnNetworkDespawn` 예약 ID 초기화가 유지되는지 확인해야 한다.
- 현재 코인 영역의 사용 가능 셀 수가 라운드별 요구 코인 수를 충족하는지 검증해야 한다.

### 3. 상자 스포너 전환

- 실제 Host/Client에서 상자 라운드 재생성 시 이전 예약이 누수되지 않는지 확인해야 한다.
- 현재는 상자 크기를 1셀로 예약한다. 주변 셀 예약이 필요한지 PlayMode에서 접근성을 확인해야 한다.

### 4. 공용 예약 시스템 확장

- Player와 동적 오브젝트의 예약 ID·소유 종류를 분리했고 같은 셀 중복 예약을 공통 검사한다.
- 라운드 전환 시 Coin/Crate 종류 예약을 정리하고, 씬 종료/Despawn 시 개별 ID를 반환한다.
- 연결 해제 시 `GridSpawnReservation.ReleasePlayer`를 호출하는 경로는 추가 확인이 필요하다.
- 서버에서만 예약 상태를 변경하도록 권한 경계를 확인해야 한다.

### 5. 스폰 순서 통합

라운드 시작 순서를 아래처럼 보장해야 한다.

1. `GridSpawnMap` 준비
2. 플레이어 영역 예약
3. 플레이어 실제 위치 예약 및 배치
4. 모든 플레이어 배치 완료 확인
5. 상자 예약 및 생성
6. 코인 예약 및 생성
7. 라운드 활성화

상자·코인은 같은 NetworkSpawner와 GameManager 라운드 이벤트를 사용한다. 생성 완료를 서버가 기다리는 순서 게이트와 상자→코인 우선순위는 아직 통합하지 않았다.

### 6. 검증 도구 보완

- Scene View에서 16×16 셀, 정적 차단 셀, Player Zone 셀, 코인·상자 후보 셀을 표시하는 Gizmo를 추가해야 한다.
- Editor에서 도달 가능성, 플레이어 수용량, Ground 동적 후보 수를 검사해야 한다.
- 정적 차단 셀이 실제 복셸과 1:1에 가까운지 Collider footprint를 다시 확인해야 한다.
- Grid asset의 source scene과 현재 씬 경로가 일치하는지 검증해야 한다.

## 현재 생성된 보고서

- `Assets/_CoinGame/Generated/GridMeasurementReport.json`
- `Assets/_CoinGame/Generated/GridAlignmentReport.json`
- `Assets/_CoinGame/Generated/GridSpawnMap.asset`

## 2026-11-17 공통 스포너와 회전 배치

- Coin/Crate의 생성 종류·개수는 NetworkSpawner가 선택하며 이전 라운드 객체는 NetworkSpawnBatch가 추적해 정리한다. 예약은 실제 Despawn 시 개별 반환한다.
- ScriptableObject/Spawn/CrateRotation.asset의 Vector3 Min/Max는 기본 (0,0,0)~(0,360,0)이다. CratePlacement.asset에서 전면 방향·거리·폭·LayerMask와 회전 시도 수를 설정한다.
- 전체 Ground 후보를 무작위 순서로 검사하며 회전 Collider가 겹치는 후보는 거부한다. 전면이 막히지 않은 배치를 우선하고 불가능한 경우 가장 여유 있는 비충돌 후보로 대체한다.
- 플레이어 배치와 라운드 동기화는 기존 흐름을 유지한다. 상자→코인 생성 완료를 기다린 뒤 라운드를 활성화하는 전체 순서 게이트는 이번 통합에 포함하지 않았으며 기존 후속 항목으로 남는다.
- 로컬 Host에서 코인 10/상자 10, 상자 몸체 충돌 0, Environment 전면 차단 0, 라운드 교체 시 이전 상자 0과 예약 20을 확인했다. 0개 상자·빠른 라운드 취소·중복 요청·회전 드롭·fallback·완전 충돌 실패를 검증했다. 별도 Client 확인은 남아 있다.
## 검증 상태

- 공통 스포너 통합 후 남은 미사용 NetworkCrateSpawner 및 meta를 제거했다. 삭제 전 코드·씬·프리팹 참조를 검색했고, Unity 재컴파일에서 CS0117 해소·Console 오류/경고 0개를 확인했다. 이번 정리의 플레이 재검증은 생략했다.

- `dotnet build Assembly-CSharp.csproj --no-restore`: 오류 0개
- 빌드 경고: 외부 Toon Shaders의 기존 `tempTexHandle` 미할당 경고 1개
- Unity MCP 스크립트 검증: 측정·정렬·베이크·예약 관련 스크립트 오류 0개
- 이번 변경의 C# 정적 빌드: 오류 0개, 기존 외부 Toon Shaders 경고 1개
- Editor 베이크: 16×16 grid 생성 성공
- Unity MCP에서 Persistent + InGame additive PlayMode 스모크를 실행하고 정상 종료를 확인했다.
- 실행 후 Unity Console Error/Warning 0개를 확인했다.
- Unity MCP 예약 좌표 테스트에서 20개 동적 후보가 Ground 전역으로 분산되는 것을 확인했다.
- 프로젝트 데이터 에셋 이동에 맞춰 GDD 경로를 `ScriptableObject/ItemData`, `Player`, `Session` 기준으로 갱신했다.
- 부활 유예 중 승패 재평가, 최종 `WIN`/`LOSS` 결과 UI, Score 데이터·RPC·함수·저장 키·UI 참조 제거, ItemKind별 픽업 컴포넌트 참조를 교정하고 Gun/Sword 전용 픽업을 등록했다. 로컬 Host에서 9종 실제 드롭 경로를 검증했다.
- 실제 Host/Client 라운드 전환 및 스폰 충돌 검증: 미실행
- Unity EditMode 전체 3/3 통과

## 다음 세션 시작점

1. Unity Editor에서 `Persistent_CoinGameScene`과 `InGame_CoinGameScene`을 함께 연다.
2. `GridSpawnMap.asset` 참조가 PlayerSpawner, Coin_Spawner, Crate_Spawner에 저장됐는지 확인한다.
3. Host 1명 + Client 1명으로 Player grid 재배치를 먼저 검증한다.
4. 코인·상자 후보 수와 예약 반환을 Host 1 + Client 1에서 검증한다.
5. 연결 해제·재접속과 PlayMode 콘솔을 확인한다.
6. 검증 결과를 `CoinGame_GDD.md`에 갱신한다.


## 2026-11-17 표시 이름 및 킬 HUD

- Persistent 이름 입력 팝업, 로컬 이름 저장, 소유자 RPC/서버 검증을 통한 이름 복제, UserName 및 Collector/Disruptor 이름 목록을 연결했다.
- Remaining Coin 아래 같은 스타일의 경기 누적 Kills를 추가했고 Disruptor는 Remaining Coin만 숨긴다. Score 데이터는 복원하지 않는다.
- 로컬 Host에서 한글 이름 화면과 킬 집계 경계를 검증했다. 별도 Client 및 플랫폼 Player 검증은 남아 있다. 상세 근거는 GDD 표시 이름과 킬 HUD 항목을 참조한다.

## 2026-11-17 목록 중복과 Ground 접촉

- 역할 목록은 소유자별 대표 PlayerObject만 표시하며 동명이인은 (2)로 구별한다. 긴 한글 이름도 구분자가 보이도록 TMP 자동 크기를 적용했다. 비동기 Player 생성 직전에 연결·기존 PlayerObject를 재확인한다.
- Grid의 +0.8m를 제거하고 기존 256개 셀의 Y를 Ground 표면으로 갱신했다. Player 이동은 X/Z만 Clamp하고 CharacterController 중심은 SkinWidth를 보정해 발을 바닥에 맞췄다. 코인의 기존 월드 높이는 설정 에셋으로 보존했다.
- 로컬 Host에서 회전 Crate 7개의 Ground 접촉, 8.66m Player 이동 후 Y 유지, 반복 생성 요청 후 소유자 Player 1개, 합성 소유자 2개의 동명이인 구분을 확인했다. 원격 Client·Player 빌드는 남아 있다.


## 2026-11-17 MPP 이름 분리 및 번호

- 공용 PlayerPrefs 재조회 대신 프로세스별 확정 이름을 보존하고, MPP -name별 저장 키를 분리했다. Main의 기존 저장 이름은 유지한다.
- 서버가 방 안에서 겹치지 않는 네 자리 번호를 배정해 UserName/역할 목록에 이름#번호를 표시한다. 이전 (2) 표시를 제거했다.
- 실제 MPP Main+Player2 로컬 Host/Client 연결에서 서로 다른 이름, 양쪽 목록/계정 이름, Client 이름 변경, 재접속, 같은 이름의 번호 구분을 확인했다. Relay 및 Player 빌드 검증은 별도다.
