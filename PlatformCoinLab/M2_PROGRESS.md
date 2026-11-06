# M2 구현 진행 메모

작성일: 2026-11-06
상태: 진행 중

## 사용자 승인

- M2 구현 설계 승인
- 방해자 능력 선택: A
  - 밀치기 `O` 유지
  - 기존 코인 잠금은 `L` 스킬 입력으로 유지
  - 점수 탈취 기존 코드 유지
  - GDD의 아이템 습득 불가는 M3에서 추가

## 반드시 유지할 구조

- NGO 2.13.3 및 기존 `NetworkRoundState`, `NetworkPlayerStats`, `NetworkSpawner`, `NetworkGameObjectPool` 재사용
- 서버 권한으로 상태 변경과 판정 처리
- 소유자 이동 + `NetworkTransform` 유지
- `EnableSceneManagement = false` 유지
- 기존 기능 삭제 금지
- 수치는 ScriptableObject로 분리

## 사전 확인

- `CoinGame_GDD.md`와 `AGENTS.md`를 읽음
- 기존 M1 변경 사항과 작업 트리의 사용자 변경을 보존해야 함
- 현재 작업 트리에는 M1/VFX/씬/패키지 관련 변경이 다수 존재하므로 되돌리거나 일괄 정리하지 말 것

## 이번 세션에서 추가·수정한 파일

- `Assets/_CoinGame/Scripts/Input/PlayerInputState.cs`
  - 공통 입력 구조체 추가
- `Assets/_CoinGame/Scripts/Input/PlayerInputStateProvider.cs`
  - `Gameplay` Action Map에서 입력을 읽어 `PlayerInputState`로 제공
  - 모바일 입력 주입용 `SetMobileState` 훅 추가
- `Assets/_CoinGame/Scripts/Gameplay/Combat/DamageInfo.cs`
  - `DamageInfo`, `DamageType` 추가
- `Assets/_CoinGame/Scripts/Gameplay/Player/PlayerLifeState.cs`
  - `Alive`, `Dead`, `Spectating` 추가
- `Assets/_CoinGame/Scripts/Gameplay/Player/PlayerStatsConfig.cs`
  - 최대 체력, 방해자 리스폰 지연, 부활 대기 지연, 시간 초과 피해 계수 정의
- `Assets/_CoinGame/Scripts/Gameplay/Stage/RoundOutcomeEvaluator.cs`
  - 순수 C# 승패/시간 초과 페널티 계산 클래스 추가
- `Assets/_CoinGame/Tests/EditMode/RoundOutcomeEvaluatorTests.cs`
  - 기본 EditMode 테스트 3개 추가
- `Assets/_CoinGame/ScriptableObject/Player Stats Config.asset`
  - 기본값 100 HP, 5초 리스폰, 3초 부활 대기, 계수 1 설정
- `Assets/_CoinGame/Scripts/Stage/StageConfig.cs`
  - 시간 초과 피해 계수 필드 추가를 시도함
- `Assets/_CoinGame/Scripts/Network/NetworkPlayerStats.cs`
  - 체력/최대 체력/생존 상태/부활 대기 NetworkVariable과 `ApplyDamageServer` 초안 추가
  - 수집가 사망 시 관전 전환, 방해자 사망 시 리스폰 대기 초안 추가
- `Assets/_CoinGame/Scripts/Network/NetworkPlayerController.cs`
  - `PlayerInputStateProvider` 참조 추가
  - 입력 Map 이름을 `Gameplay`로 변경
  - 이동·기존 방해자 입력을 provider 상태로 읽도록 일부 변경
  - 사망 상태 이동 차단 조건 추가
- `Assets/_CoinGame/CoinGame_Actions.inputactions`
  - `Player` Map을 `Gameplay`로 변경
  - `WeaponPrevious`, `WeaponNext`, `Reload`, `Boost`, `Revive`, `Push`, `Skill` 액션 및 PC 키 추가
  - 기존 액션은 유지
  - 기존 `Interact` 키를 P, `Attack` 키를 K로 변경
- `Assets/_CoinGame/Scenes/Persistent_CoinGameScene.unity`
  - `Player/` 입력 경로와 기본 Map을 `Gameplay/`로 변경
- `Assets/_CoinGame/Prefab/Player.prefab`
  - `PlayerInputStateProvider` 컴포넌트 추가 시도

## 아직 하지 않은 핵심 구현

1. Unity 컴파일 확인 및 문법 오류 수정
2. `NetworkPlayerStats`의 `OnNetworkSpawn` 초기화와 서버 Update 흐름 검토
3. `NetworkRoundState` 승패 결과 확장
   - 수집가 전원 사망 즉시 방해자 승리
   - 마지막 라운드 생존 수집가 기준 승패
   - 시간 초과 HP 페널티 후 다음 라운드
   - `MatchWinner` NetworkVariable 또는 기존 결과 확장
4. `NetworkRoundState.NotifyPlayerDeathServer()`와 `RelocatePlayerServer()` 구현
5. `NetworkSpawner`에서 특정 플레이어 안전 지점 재배치 구현
6. `AssignMissingRolesServer()` 및 `RoomSettingsValidator` 반반 규칙 적용
7. `Player.prefab`의 `PlayerStatsConfig` 참조 연결 여부 확인
8. `StageConfig.GetTimeoutPenalty()`가 실제 파일에 정상 삽입됐는지 확인
9. Input System JSON 재생성 후 Unity import 및 PlayerInput 연결 확인
10. Unity MCP 에디터 상태/Console 확인
11. 호스트 1 + 클라이언트 1 테스트
12. 최종적으로 `AGENTS.md`의 M2 진행 단계 갱신

## 다음 세션 시작 순서

1. `git diff --check`와 `git status --short` 확인
2. 아래 파일의 실제 내용을 먼저 검토
   - `NetworkPlayerStats.cs`
   - `StageConfig.cs`
   - `NetworkPlayerController.cs`
   - `CoinGame_Actions.inputactions`
   - `Player.prefab`
3. Unity MCP의 editor state와 Console 확인
4. 컴파일 오류를 먼저 해결한 뒤 `NetworkRoundState`와 역할 배정 구현
5. EditMode 테스트 실행
6. 호스트/클라이언트 플레이 테스트 후 `AGENTS.md` 갱신

## 주의

- 이 문서는 M2 완료 보고서가 아니라 중단 시점 복구용 메모임
- 기존 방해자 능력 구현(`UsePushRpc`, `UseLockRpc`, `UseStealRpc`)은 선택지 A에 따라 삭제하거나 교체하지 말 것
- `NetworkSpawner.TryRelocatePlayers()`의 기존 전체 플레이어 재배치 흐름은 라운드 전환에 계속 사용하고, 사망 리스폰은 별도 단일 플레이어 안전 지점 흐름으로 추가할 것
- C# 스타일 지침상 `[SerializeField]`는 필드와 분리된 줄에 두고, 중괄호는 다음 줄에 둘 것
