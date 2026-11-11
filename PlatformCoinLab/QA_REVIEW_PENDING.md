# QA / Review 중단 기록

작성 시점: 2026-11-11

이번 작업은 사용자의 중단 요청으로 여기서 멈춘다. 아래 내용은 `AGENTS.md`와 `CoinGame_GDD.md`를 기준으로 Persistent 씬에서 InGame 씬으로 진입하는 흐름을 검토하면서 확인한 사항이다. 이 문서 작성 이후에는 추가 수정이나 테스트를 실행하지 않는다.

## 확인한 구조

- `GameManager`가 Persistent 씬에서 InGame 씬을 additive로 로드한다.
- `M2PrototypeBootstrap`은 InGame 단독 실행 시 Persistent 씬을 additive로 로드한다.
- `NetworkManager.NetworkConfig.EnableSceneManagement`는 비활성 상태다.
- Player prefab의 NetworkObject GUID와 `DefaultNetworkPrefabs` 등록은 정적으로 일치한다.
- 플레이어 이동은 소유자 입력, `CharacterController.Move`, `PlayerMove_VectorMinMaxData` Clamp 구조다.
- 현재 Player 이동 범위는 `±12.5`, Map Bounds는 약 `25 × 25`다.
- `_followPlayerCamera`는 false여서 고정 카메라 구조다.

## 확인된 문제와 미완료 작업

### 높은 우선순위

1. **`RoomLobbyController.OnDestroy` NullReferenceException**
   - 위치: `Assets/_CoinGame/Scripts/Network/RoomLobbyController.cs:532`
   - `_sessionService`가 null일 때 이벤트 해제와 Dispose를 호출하는 문제가 있었다.
   - null guard 수정은 작업 중 반영되었지만, Unity PlayMode 재검증은 중단되어 완료되지 않았다.

2. **Unity 종료 시 NGO `NetworkSceneManager.Dispose` NullReferenceException**
   - `NetworkManager.OnDestroy` 과정에서 발생했다.
   - 패키지 런타임 내부 스택이라 프로젝트 코드 수정만으로 해결 가능한지 확인하지 못했다.
   - NetworkManager 종료 순서와 실제 Host/Client 종료 시나리오를 별도 검증해야 한다.

3. **Missing Script 경고**
   - `The referenced script (Unknown) on this Behaviour is missing!`
   - 현재 Unity Console 경고만 확인했고, 어떤 GameObject인지 특정하지 못했다.
   - Persistent 씬과 InGame 씬을 각각 열어 Inspector의 Missing Script를 찾아 제거하거나 참조를 복구해야 한다.

### 중간 우선순위

1. **UI 겹침 가능성**
   - `StageView`의 좌상단 점수 영역과 `M3HudView`가 생성하는 좌상단 HUD가 겹칠 가능성이 있다.
   - `M3HudView`의 패널 위치를 아래쪽으로 이동하는 수정은 작업 중 반영되었지만, 실제 해상도별 스크린샷 검증은 완료하지 못했다.

2. **월드 체력바 높이**
   - 기존 `y=1.35`는 Yurowm 모델의 머리 높이와 맞지 않을 가능성이 있다.
   - `NetworkWorldHealthBar` 위치를 `y=2.25`로 조정하는 수정은 작업 중 반영되었지만, 실제 캐릭터와 겹침 여부는 확인하지 못했다.

3. **상자 크기**
   - 기존 Crate root collider는 `(4, 3, 3)`으로 플레이어 반지름 `0.42` 대비 크다.
   - Crate root 스케일을 축소하는 수정은 작업 중 반영되었지만, 마지막 수정이 root와 자식 메시에 정확히 적용됐는지 최종 확인하지 못했다.
   - Scene View에서 2인 통로 통과와 상자 엄폐물 역할을 확인해야 한다.

4. **카메라와 AudioListener 중복**
   - Persistent와 InGame에 각각 Main Camera가 있고, additive 전환 중 `2 audio listeners` 로그가 발생했다.
   - `GameManager.ConfigureInGameSceneServices()`가 Persistent 카메라를 비활성화하지만 전환 시점의 중복 여부는 실제 PlayMode 확인이 필요하다.

5. **UI 직렬화 타입 경고**
   - `m_Image`가 `Texture`를 요구하지만 `Transform`을 참조하는 경고가 있다.
   - Addressables 또는 씬 UI의 실제 필드를 Inspector에서 찾아 재할당해야 한다.

## 테스트 상태

- 정적 코드와 프리팹 검토: 완료
- Unity Editor 컴파일 오류: 이전 확인에서 0개
- 기존 EditMode/PlayMode 테스트: 이전 실행에서 통과했으나, 이번 수정 후 전체 재실행은 중단됨
- Persistent→InGame 단독 PlayMode 진입: 시도함
- Host 1 + Client 1 실제 연결: 완료하지 못함
- 플레이어 생성·이동·상자 피격·뚜껑 연출: 실제 2인 환경에서 미검증
- UI 스크린샷 검증: MCP 카메라 호출 시간 초과로 미완료

## 다음 재개 순서

1. `git status`와 이 문서부터 확인
2. `RoomLobbyController` null guard와 Crate prefab root scale 최종 diff 확인
3. Missing Script의 GameObject 식별
4. InGame 단독 PlayMode에서 Persistent additive 로드 확인
5. Host 1 + Client 1 개발용 빠른 시작
6. 두 Player 생성·이동·카메라·AudioListener 확인
7. 상자 통로 통과, 크기, 뚜껑 열림, 드롭 확인
8. StageView, M3HudView, 월드 체력바 스크린샷 확인
9. Console 오류와 경고 재분류
10. 확인 결과에 따라 남은 수정 진행

## 재개 검증 기록 (2026-11-12)

- Unity MCP 연결 상태와 InGame_CoinGameScene을 확인했다. 에디터는 Unity 6000.6.7f1, 비재생 상태에서 시작했다.
- EditMode 3개와 PlayMode 3개를 재실행해 모두 통과했다.
- InGame 단독 PlayMode 스모크에서 Persistent additive 흐름과 화면 출력을 확인했다. 첫 화면 캡처에서 M3 HUD와 StageView의 Score/Remaining Coin 영역이 겹치는 것을 확인했다.
- M3HudView 패널을 좌상단 y=-96에서 y=-540으로 이동하고 패널 높이를 180에서 240으로 조정했다. 재생 후 캡처에서 Score/Remaining Coin과 M3 HUD가 분리된 것을 확인했다.
- 수정 후 Unity 컴파일 오류는 0개였다. Console에서 프로젝트 코드 오류는 확인되지 않았고, 기존 Yurowm obsolete API 경고 1건이 남아 있다.
- 씬 YAML과 프리팹에서 m_Script가 0 또는 zero GUID인 항목은 정적 검색으로 찾지 못했다. Missing Script의 GameObject별 Inspector 확정은 MCP 연결이 끊겨 완료하지 못했다.
- Host 1 + Client 1 실제 연결, 두 플레이어 이동/전투/상자 상호작용, 종료 시 NGO NetworkSceneManager.Dispose 예외는 별도 실행 인스턴스와 MCP 재연결 후 확인이 필요하다.
