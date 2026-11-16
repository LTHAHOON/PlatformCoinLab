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
  - 코인 영역: 43개
  - 상자 영역: 12개
- `GridSpawnReservation`의 플레이어 예약 API와 `NetworkSpawner`의 grid 기반 플레이어 재배치 경로를 추가했다.
- `NetworkSpawner`와 `NetworkCrateSpawner`에 `GridSpawnMap` 참조 필드를 추가했다.
- Persistent/InGame 씬의 스포너에 grid asset 참조를 할당하는 Editor 메뉴를 추가했다.
  - 메뉴: `CoinGame/Assign Grid Spawn Map References`
- Persistent 씬에서 발생한 교차 씬 Player Zone 참조는 grid 사용을 위해 빈 목록으로 정리했다.

## 아직 완료되지 않은 작업

### 1. 플레이어 grid 재배치 검증

- 실제 Host 1명 + Client 1명으로 역할별 Player Zone에 배치되는지 확인해야 한다.
- `NetworkRoundState`의 라운드 시작, 사망 후 부활, 라운드 클리어 재배치에서 예약 해제·재예약을 확인해야 한다.
- 연결 해제 시 `GridSpawnReservation.ReleasePlayer`를 호출하는 경로를 추가해야 한다.
- grid 예약 실패 시 로그와 안전한 실패 처리를 추가해야 한다.

### 2. 코인 스포너 전환

- `NetworkSpawner.GetCoinSpawnPosition`의 랜덤 Zone 후보 선택을 grid 셀 후보 선택으로 교체해야 한다.
- 코인마다 예약 ID를 유지해야 한다.
- 코인 수집·Despawn·라운드 전환 시 셀을 반환해야 한다.
- 현재 `BaseSpawner`의 비동기 생성과 예약 수명주기를 연결해야 한다.
- 현재 코인 영역의 사용 가능 셀 수가 라운드별 요구 코인 수를 충족하는지 검증해야 한다.

### 3. 상자 스포너 전환

- `NetworkCrateSpawner`의 랜덤 위치 선택을 grid 셀 선택으로 교체해야 한다.
- 상자 라운드 재생성 시 이전 상자 예약을 모두 반환해야 한다.
- 상자 Despawn·파괴 시 예약을 반환해야 한다.
- 상자 크기와 접근 여유를 고려해 1셀 또는 주변 셀을 예약할지 확정해야 한다.

### 4. 공용 예약 시스템 확장

- 현재 `GridSpawnReservation`은 플레이어 예약 중심이다.
- `Player`, `Coin`, `Crate`별 예약 목록과 소유자 ID를 분리해야 한다.
- 같은 셀에 서로 다른 종류가 예약되지 않도록 공통 검사 API를 추가해야 한다.
- 라운드 시작 시 동적 예약을 초기화하고, 씬 종료·재접속 시 정리해야 한다.
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

현재 상자·코인 스폰은 각각 기존 이벤트와 스포너 수명주기를 사용하므로, `NetworkRoundState`와의 순서를 통합해야 한다.

### 6. 검증 도구 보완

- Scene View에서 16×16 셀, 정적 차단 셀, Player Zone 셀, 코인·상자 후보 셀을 표시하는 Gizmo를 추가해야 한다.
- Editor에서 도달 가능성, 플레이어 수용량, 코인·상자 후보 수를 검사해야 한다.
- 정적 차단 셀이 실제 복셸과 1:1에 가까운지 Collider footprint를 다시 확인해야 한다.
- Grid asset의 source scene과 현재 씬 경로가 일치하는지 검증해야 한다.

## 현재 생성된 보고서

- `Assets/_CoinGame/Generated/GridMeasurementReport.json`
- `Assets/_CoinGame/Generated/GridAlignmentReport.json`
- `Assets/_CoinGame/Generated/GridSpawnMap.asset`

## 검증 상태

- `dotnet build Assembly-CSharp.csproj --no-restore`: 오류 0개
- 빌드 경고: 외부 Toon Shaders의 기존 `tempTexHandle` 미할당 경고 1개
- Unity MCP 스크립트 검증: 측정·정렬·베이크·예약 관련 스크립트 오류 0개
- Editor 베이크: 16×16 grid 생성 성공
- 실제 Host/Client 라운드 전환 및 스폰 충돌 검증: 미실행

## 다음 세션 시작점

1. Unity Editor에서 `Persistent_CoinGameScene`과 `InGame_CoinGameScene`을 함께 연다.
2. `GridSpawnMap.asset` 참조가 PlayerSpawner, Coin_Spawner, Crate_Spawner에 저장됐는지 확인한다.
3. Host 1명 + Client 1명으로 Player grid 재배치를 먼저 검증한다.
4. 플레이어 검증이 통과하면 코인 예약을 연결한다.
5. 코인 검증 후 상자 예약과 라운드 재생성을 연결한다.
6. 마지막에 PlayMode·네트워크·재접속 검증 결과를 `CoinGame_GDD.md`에 갱신한다.
