# PlatformCoinLab 에이전트 지침

이 파일은 이 저장소에서 작업하는 코딩 에이전트의 공통 지침이다.

## 소통과 작업 방식

- 사용자에게 한국어로 설명한다. 코드 식별자와 API 이름은 기존 표기를 유지한다.
- 요청한 작업을 완료하는 데 필요한 범위에서 수정하고, 관련 없는 리팩터링과 일괄 포맷 변경은 피한다.
- 먼저 관련 코드와 git status를 확인한다. 사용자가 작업 중인 변경을 보존한다.
- 일반적인 구현 선택은 기존 코드에 맞춰 진행한다. 요구사항에 따라 결과가 크게 달라지는 경우에만 핵심 질문을 한다.
- 완료 시 변경 내용, 검증 결과, 남은 확인 사항을 간결하게 보고한다. 실행하지 못한 검증은 명시한다.

## 기획 및 구현 원칙

- 기획 기준 문서는 `CoinGame_GDD.md`이며, 작업을 시작할 때 항상 먼저 참고한다.
- 기획 문서에서 `[확정]`으로 표시된 항목은 변경하지 않는다.
- 수치는 코드에 하드코딩하지 않고 `ScriptableObject`로 분리한다.
- 작은 단위로 구현하고 각 단계에서 컴파일 가능한 상태를 유지한다.
- 한 번에 큰 리팩터링을 진행하지 않는다.

## 프로젝트 정보

- Unity C# 프로젝트다. 에디터 버전은 ProjectSettings/ProjectVersion.txt를 기준으로 한다.
- 주요 게임 코드는 Assets/_CoinGame/Scripts/에 있다.
- 주요 구성은 Managers, Network, Sessions, Stage, Timeline, UI, Utilities, Player 폴더다.
- 패키지와 버전은 Packages/manifest.json 및 Packages/packages-lock.json을 확인한다.
- Netcode for GameObjects, UniTask, Input System, Timeline, URP를 사용한다. 설치된 패키지 버전에 맞는 API를 사용한다.

## 코드 작성

- 주변 코드의 네임스페이스, 들여쓰기, 중괄호, 명명 규칙을 따른다.
- 모든 새 스크립트와 수정하는 스크립트는 가능한 350자 이하로 유지하고, 이를 넘으면 책임을 더 작은 스크립트나 메서드로 분리한다. Unity 생명주기·직렬화·가독성 때문에 불가피한 경우에는 기능을 억지로 축약하지 않고 예외 사유를 기록한다.
- 예외 기록: 기존 Unity 생명주기와 직렬화 참조를 보존해야 하는 통합 코드는 예외 사유를 남기고 단계적으로 기능을 분리한다.
- 350자 규칙은 파일 이름만 바꾸거나 기존 코드를 전체 복사해 붙이는 방식으로 충족하지 않는다. UI, 세션, 콘텐츠, 네트워크, 검증처럼 책임과 의존성을 실제로 분리한다.
- `if`, `else`, `for`, `foreach`, `while`, `switch`, 메서드와 타입의 중괄호는 항상 다음 줄에 배치한다. 한 줄 본문과 중괄호 생략을 사용하지 않는다.
- `[SerializeField]` 같은 attribute는 항상 단독 줄에 작성하고 바로 다음 줄에 대상 필드나 멤버를 작성한다. attribute와 멤버를 같은 줄에 작성하지 않는다.
- 일반적으로 타입과 메서드는 PascalCase, private 필드는 _camelCase를 사용한다.
- Inspector에 노출하는 필드는 기존 패턴처럼 [SerializeField] private을 사용한다.
- 기존 한글과 파일 인코딩을 보존한다. 터미널에서 글자가 깨져 보여도 실제 파일 인코딩을 확인하기 전에는 일괄 치환하지 않는다.
- 새 구조나 의존성을 추가하기 전에 기존 유틸리티와 패키지로 해결할 수 있는지 확인한다.
- Awake에서 초기화하는 대상이 아니라면 싱글톤을 별도 변수로 저장하거나 매번 null을 검사하는 방어적 코드를 불필요하게 추가하지 않는다.

## Unity 에셋과 실행 수명주기

- 에셋 이동과 이름 변경 시 대응하는 .meta 파일과 GUID 참조를 보존한다.
- 직렬화된 필드 이름을 변경할 때 씬, 프리팹, ScriptableObject의 참조와 저장된 값을 확인한다. 필요하면 FormerlySerializedAs를 사용한다.
- 씬, 프리팹, Timeline 파일을 직접 수정해야 한다면 관련 참조를 먼저 확인하고 필요한 부분만 변경한다.
- Library/, Temp/, Logs/, obj/ 및 자동 생성된 .csproj, 솔루션 파일은 소스 수정 대상으로 취급하지 않는다.
- 서드파티 에셋과 패키지 캐시는 직접 수정하지 않고 프로젝트 코드에서 해결하는 방법을 우선한다.
- 비동기 작업은 파괴, 비활성화, 네트워크 Despawn, 풀 반환 등 실제 수명주기에 맞춰 취소하고 정리한다.
- 이벤트 구독과 해제, CancellationTokenSource의 취소 및 Dispose가 짝을 이루도록 한다.
- 네트워크 로직은 서버 권한과 소유자 권한을 구분하고 Spawn/Despawn 및 재접속 시 동작을 확인한다.
- 풀링 객체는 재사용 시 이전 상태, 이벤트 구독, 비동기 작업이 남지 않도록 한다.

## 검증

- 변경 범위에 맞는 컴파일 확인과 기존 테스트를 우선한다. 문서만 변경한 경우 게임 빌드는 생략할 수 있다.
- Unity 에디터에서의 컴파일과 실행 결과를 기준으로 판단한다. dotnet build만으로 Unity 동작 검증이 완료됐다고 보고하지 않는다.
- Unity 검증에는 프로젝트에 지정된 에디터 버전을 사용한다. 같은 프로젝트를 연 에디터가 있으면 별도 배치 에디터 실행 전에 충돌 여부를 확인한다.
- 플레이 검증이 필요한 경우 재현 절차와 기대 동작을 보고한다. 네트워크 변경은 관련되는 호스트와 클라이언트 양쪽 동작을 확인한다.
- 검증을 위해 패키지 버전, 빌드 타깃, 프로젝트 설정을 임의로 변경하지 않는다.
- 최종 diff를 검토하여 의도하지 않은 에셋 변경과 공백 오류가 없는지 확인한다.

## Unity MCP 스킬

- Unity 작업에서는 설치된 `unity-mcp-orchestrator` 스킬과 `unityMCP` 서버를 우선 사용한다.
- 복잡한 씬 작업 전 에디터 상태와 대상 GameObject를 먼저 확인한다.
- 스크립트 생성·수정 후 컴파일 완료를 기다리고 Unity Console의 오류를 확인한다.
- 여러 독립적인 Unity 작업은 가능한 경우 `batch_execute`로 묶고, 결과는 씬 상태·콘솔·필요한 스크린샷으로 검증한다.
- 프로젝트 에디터 버전과 설치된 패키지 API를 기준으로 작업하며, Unity MCP 템플릿의 예시는 현재 프로젝트에서 확인한 뒤 적용한다.

## VFX Mesh Lab 사용 규칙

- VFX Mesh Lab은 `https://github.com/PudinKiller/VFXMeshLab`의 Unity 6 URP 에디터 패키지이며 `Packages/manifest.json`의 `com.pudinkiller.vfx-mesh-lab` Git 의존성으로 설치한다.
- Unity에서 `Tools > VFX Mesh Lab`을 열고 Shape 또는 내장 Template을 선택한 뒤, 프로파일·Modifier·UV0·Vertex Data를 설정하고 `Shaded Wireframe` 또는 `UV Checker`로 검증한다.
- Capsule형 Shield는 Sphere 또는 Hemisphere Shape를 기본으로 사용하고, 충분한 longitude/latitude 해상도와 `Flat Shading`을 적용해 삼각형 조각 경계를 만든다. `Generate New`로 표준 Mesh 에셋을 생성하고 기존 참조가 있는 경우 `Update Existing`만 사용한다.
- 셰이더에 필요한 마스크·시간 오프셋·랜덤 변형은 Vertex Color 또는 UV1~UV3의 Packed Vector4로 저장할 수 있다. Rim은 법선과 시선 방향의 Fresnel 계산으로 만들고, Sin 기반 애니메이션은 시간·채널별 위상 오프셋을 사용해 조각별 스케일 또는 발광을 분리한다.
- VFX Mesh Lab은 에디터 전용 패키지이며 생성된 Mesh는 런타임에 패키지 의존성이 없다. 메시 생성 전 토폴로지 경고와 삼각형 수를 확인하고, 생성·수정 후 Unity Console 오류와 프리팹 참조를 검증한다.
- Shield 제작 레시피는 Sphere Shape(세로 스케일을 늘려 Capsule 비율)와 높은 longitude/latitude, `Flat Shading`, Spherical UV0를 사용한다. UV1의 Random 또는 Radial Distance를 위상 오프셋으로 저장하면 셰이더에서 조각별 Sin 펄스를 분리할 수 있다. 생성 결과는 `Assets/AgentVFX/Generated/ShieldCapsule/ShieldCapsule_Flat.asset`에 저장하고 `M_ShieldTriangles` 머티리얼을 연결한다.
- 셰이더는 삼각형 셀 경계의 `Cell Gap`과 Fresnel `Rim Power`를 함께 조절하며, `_PulseSpeed`, `_PulseAmplitude`, `_PulseScale`로 시간 기반 팽창·수축을 조절한다. 프리팹 참조를 바꿀 때는 `Update Existing` 또는 Unity Prefab 저장으로 GUID를 보존한다.

## Additional code style

- Keep [SerializeField] on its own line directly above the member field.
- Add null checks only where the lifecycle or data flow requires them; avoid repeated defensive checks for the same object.

## 사용자 코딩 스타일 기준

- 사용자가 별도로 요청하지 않으면 기존 동작을 유지하는 작은 수정만 한다. 이미 한 번 보장한 참조를 같은 메서드 안에서 반복 검사하지 않는다.
- 필드는 실제로 상태를 보존해야 할 때만 둔다. 파생 가능한 `bool` 필드와 상태 플래그를 여러 개 만들지 말고, 기존 객체의 상태나 단일 상태값으로 판단한다.
- `Find`, `AddComponent`, 런타임 탐색은 씬 참조와 `[SerializeField]`로 해결할 수 없을 때만 사용한다. Unity 생명주기상 자동 생성이 필요한 서비스는 예외 사유를 남긴다.
- `Action`과 이벤트는 화면 갱신처럼 외부에 변화를 알려야 할 때만 사용한다. 단순 메서드 호출을 델리게이트로 감싸지 않는다. 구독과 해제는 같은 생명주기에서 짝으로 작성한다.
- `??=`, 비트 연산, 삼항 연산자, LINQ와 중첩 조건은 읽기 쉬운 명시적 코드가 되는 경우 우선 줄인다. 조건문은 조기 반환으로 깊이를 낮추고 중괄호를 항상 줄바꿈한다.
- 한 줄 `if`, 한 줄 메서드 본문, 한 줄 중괄호는 사용하지 않는다. `if`의 본문과 다음 문장 사이도 줄을 나눈다.
- 새 스크립트는 한 책임만 갖고 짧게 유지한다. Unity 직렬화·네트워크·생명주기 통합으로 긴 파일이 불가피하면 책임을 메서드와 보조 컴포넌트로 나눌 근거를 남긴다.
- 이름은 기존 저장소처럼 PascalCase 타입·메서드, `_camelCase` private 필드, 명시적인 타입 선언을 기본으로 한다. 공개 API는 꼭 필요한 것만 둔다.
- UI는 View가 이벤트를 노출하되, 컨트롤러가 구독을 과도하게 늘리지 않는다. 한 화면에서 직접 연결 가능한 동작은 직접 메서드로 연결한다.

### GitHub 스타일 분석 기록

- 2026-11-12에 `LTHAHOON` 공개 저장소 `GamePortfolio_Heaven`, `Unity_Effect`, `GameNetworkProject`의 Unity C# 파일을 확인했다.
- 공통적으로 `[SerializeField]`를 단독 줄에 두고 private 필드는 `_camelCase`를 사용한다. 컴포넌트는 기능별로 나뉘며, UI와 게임 로직의 연결은 이벤트가 필요한 지점에만 둔다.
- 이후 사용자가 코딩 스타일 적용을 요청하면 이 절을 먼저 읽고, 매번 GitHub를 다시 분석하지 않는다. 저장소 스타일이 크게 바뀌었다고 사용자가 명시한 경우에만 재분석한다.

## Codex 멀티 에이전트 역할 분배

현재 설치된 Codex IDE/CLI(`codex-cli 0.164.0-alpha.11.1`)에서는 프로젝트별 `agents/*.toml`로 Subagent를 등록하는 공식 로컬 스키마가 확인되지 않는다. 역할 정의는 이 문서에 보관하고, 실제 위임은 Codex 세션의 멀티 에이전트 기능으로 수행한다.

- Main Agent: 전체 작업 분석, 역할 분배, Unity MCP 변경 조율, 통합 및 최종 검증을 담당한다.
- gameplay: Unity 6 C# 게임플레이, 플레이어·전투·인벤토리·상호작용을 담당하며 관련 범위 밖 파일 수정을 피한다.
- ui: uGUI/UI Toolkit/HUD/메뉴를 담당하며 게임플레이 로직과 표현을 분리한다.
- vfx: Particle System, Visual Effect Graph, Shader Graph, 머티리얼·셰이더·전투 및 환경 효과를 담당한다. URP/HDRP와 GPU/CPU 성능·오버드로우를 확인하고, Unity MCP로 VFX·씬을 변경하기 전 Main Agent의 승인을 받는다.
- reviewer: C# 품질, Unity 수명주기·이벤트·직렬화 위험을 읽기 전용으로 검토하고 프로덕션 파일을 수정하지 않는다.
- qa: EditMode/PlayMode 자동 테스트, 기능·회귀·버그 재현, Console·성능·GC Alloc 검토를 담당한다. 실행하지 않은 테스트를 통과로 보고하지 않으며 문제별 심각도·재현 방법과 수정 후 재검증 결과를 보고한다.
- server: Unity Netcode for GameObjects, Unity Multiplayer Services, 세션·로비·룸·매치메이킹, Dedicated/Listen Server 구조, RPC·상태 동기화, Addressables 기반 원격 콘텐츠, ASP.NET Core ContentServer, 인증·권한·입력 검증·영속 데이터·관측성을 담당한다. 구현 전 현재 네트워크 라이브러리와 ContentServer 구조를 조사하고 확인되지 않은 백엔드 기술이나 외부 서비스를 임의로 도입하지 않는다. 운영 서버·운영 데이터·비용이 발생하는 클라우드 리소스를 승인 없이 변경하지 않으며, Unity 씬·프리팹·ProjectSettings 변경은 Main Agent와 조율한다.

기존 `tester` 역할은 `qa`로 확장·이관했다. 저장소에 별도 Tester 설정 파일은 존재하지 않았으므로 삭제하지 않았고, 기존 역할 규칙의 의미는 QA 프롬프트에 보존했다.

독립적인 읽기·분석 업무는 가능한 경우 병렬로 위임하되 동시 실행은 최대 3~4개로 제한한다. server는 네트워크·백엔드 구조와 데이터 흐름을 먼저 읽기 전용으로 조사하고, gameplay와 서버 권한 게임 로직·상태 동기화를 조율하며, ui에는 로그인·로비·매치메이킹 API와 연결 상태·오류 규격을 제공하고, vfx에는 서버 이벤트와 시각 효과 분리 규격을 제공한다. reviewer는 서버 코드의 보안·동시성·데이터 무결성을 검토하고, qa는 지연·패킷 손실·재접속·API·부하·회귀 검증을 담당한다. 같은 Unity Editor 인스턴스 또는 씬·프리팹·ProjectSettings를 여러 역할이 동시에 변경하지 않는다. Unity MCP의 씬·프리팹·ProjectSettings 변경은 Main Agent가 순차 조율하고 실행 전 승인을 받는다. Library/Temp/Logs/obj는 수정하지 않는다. 모든 역할은 한국어로 결과를 보고한다.

## 네트워크 구조 요약

- 네트워크 솔루션은 Unity Netcode for GameObjects 2.13.3이며 Unity Transport를 사용한다.
- 방 생성·참가는 Unity Services Multiplayer Session과 익명 인증을 사용하고, 세션 생성 시 RelayNetworkOptions를 적용한다.
- 호스트는 `StartHost()`, 클라이언트는 `StartClient()`로 연결한다.
- `NetworkManager.NetworkConfig.EnableSceneManagement`는 비활성화되어 있다. 씬 전환은 `GameManager`가 로컬로 수행하고 `NetworkRoundState`의 준비·씬 로드·플레이어 배치 상태로 동기화한다.
- 플레이어는 서버의 `NetworkSpawner`가 `Player.prefab`을 `SpawnAsPlayerObject(clientId)`로 생성한다.
- `Player.prefab`은 `NetworkObject`, `NetworkTransform`, `NetworkPlayerController`, `NetworkPlayerStats`, `CharacterController`로 구성된다.
- 이동 입력은 소유자 클라이언트가 `Player` Action Map에서 직접 읽고 `CharacterController`로 이동한다. 위치와 Y축 회전은 `NetworkTransform`으로 동기화한다. 현재 입력 벡터를 서버로 보내는 `PlayerInputState` 구조체는 없다.
- 코인 충돌과 라운드 판정은 서버에서 처리한다. `NetworkCoinController`가 서버에서 코인을 수집하고 `NetworkRoundState`가 잔여 코인·라운드·제한 시간을 `NetworkVariable`로 동기화한다.
- 역할 선택과 방해 능력 사용은 RPC로 서버에 요청하고, 서버가 역할 정원·대상·효과를 검증한다.
- 현재 네트워크 상태에는 체력·피해·사망·부활·무기·투사체가 구현되어 있지 않다. GDD 구현 시 기존 NGO·Relay·스폰·라운드 구조를 재사용하고, 신규 게임 판정은 기존 서버 권한 모델에 맞춘다.
- 주요 네트워크 스크립트는 `NetworkConnector`, `RoomSessionService`, `RoomLobbyController`, `RoomSettings`, `RoomSettingsValidator`, `RoomStartCountdown`, `NetworkRoundState`, `NetworkPlayerController`, `NetworkPlayerStats`, `NetworkCoinController`, `NetworkSpawner`, `NetworkGameObjectPool`이다.
- 주요 씬은 `Persistent_CoinGameScene`과 `InGame_CoinGameScene`이다. 전자는 `NetworkManager`, 로비, `NetworkConnector`, 플레이어 스포너, `GameManager`를 포함하고, 후자는 코인 스포너, HUD, 라운드 UI, 게임플레이 환경을 포함한다.
- 주요 네트워크 프리팹은 `Player.prefab`, `Coin.prefab`, `Coin_Spawner.prefab`, `NetworkRoundState.prefab`이다.
- GDD의 `Gameplay` Action Map, `PlayerInputState`, 체력·사망·부활, 수집가 전용 코인 수집, 무기·공격·투사체는 현재 구조와 차이가 있으므로 구현 전에 기존 `Player` 입력 체계와 권한 경계를 조정해야 한다.

## M1 완료 상태

- 기존 `NetworkCoinController`가 서버에서 `NetworkPlayerStats.Role`을 확인해 Collector만 코인을 수집하도록 수정했다. Disruptor의 접촉은 코인, 점수, 잔여 코인에 영향을 주지 않는다.
- `StageConfig`의 기존 `BaseCoinCount`/`CoinIncreasePerRound`와 `GetRequiredCoins`/`GetSpawnCoinCount` 분리는 유지하고, 기본값을 10/3으로 맞췄다. 로비의 코인 배율 기능은 유지하며 기본값을 1로 사용한다.
- `StageView`는 서버의 target/remaining 상태를 바탕으로 수집 진행도를 `n/m`으로 표시한다. 클리어와 시간 초과 판정은 `NetworkRoundState` 서버 권한 흐름을 유지한다.
- `M1Prototype`은 네트워크가 수신 대기 중이면 자동 비활성화되어 `M1Coin` 로컬 경로와 네트워크 코인 경로가 동시에 실행되지 않는다.
- `NetworkPlayerController`의 기존 XZ 이동과 `PlayerMove_VectorMinMaxData` 제한은 유지하고, 로컬 플레이어를 기준으로 기존 Main Camera 오프셋을 추적한다.

## M2 진행 상태

- 서버 권한 체력·피해·사망·부활 흐름을 기존 `NetworkPlayerStats`에 확장했다.
- `NetworkRoundState`에 Collector 전원 사망 승리, 마지막 라운드 생존자 승패, 시간 초과 페널티, `MatchWinner` 동기화를 확장했다.
- 기존 `NetworkSpawner.TryRelocatePlayers()`는 유지하고 단일 플레이어 안전 지점 재배치를 추가했다.
- 기존 방해자 능력(`UsePushRpc`, `UseLockRpc`, `UseStealRpc`)과 소유자 이동·`NetworkTransform`을 유지했다.
- 2인 방에서 Collector와 Disruptor가 모두 존재하도록 `RoomSettingsValidator` 검증을 보완했다.
- `PlayerStatsConfig`의 `.meta`와 ScriptableObject `m_Script` GUID를 복구했다.
- `Assembly-CSharp.csproj` 컴파일 검증은 오류 0개·경고 0개다.
- Unity MCP에서 기존 `Player.prefab`의 `BrokenPrefabAsset` 상태를 확인하고 기존 GUID를 유지한 정상 프리팹으로 복구했다.
- Unity PlayMode 3개와 EditMode 3개를 모두 통과했고, Play Mode 진입·종료 스모크 테스트에서 Console 오류 0개를 확인했다.
- `RelocateForGameRpc`에서 서버만 `NetworkTransform.Teleport`을 호출하도록 수정해 non-authoritative Teleport 예외를 제거했다.
- `DefaultNetworkPrefabs.asset`의 깨진 Player 중복 등록을 정리했고, Play Mode 재실행에서 Console 오류 0개를 확인했다.
- `GameManager`가 Persistent additive 실행 시 InGame Camera/EventSystem을 비활성화하고 Persistent 객체를 재사용하도록 확장했다.
- `M2PrototypeBootstrap`을 추가해 InGame 단독 실행 시 Persistent 씬을 additive로 로드한다.
- InGame의 M1 프로토타입 실행은 분리했지만 `M1Prototype`과 `M1StageConfig` 자산은 보존했다.
- InGame 단독 PlayMode 스모크에서 Persistent additive 로드와 Persistent Camera/EventSystem 재사용을 확인했다.
- 최종 Unity 테스트 재실행은 스크립트 재컴파일 후 MCP 세션이 끊겨 대기 중이며, C# 빌드는 오류 0개다.
- 호스트 1 + 클라이언트 1 실제 연결 테스트는 별도 실행 인스턴스가 필요해 수동 검증 절차로 남아 있다.

## M3 완료 상태

- `NetworkInventory`가 서버 권한 무기 슬롯 2개, 현재 무기, 탄창/예비 탄약, 부스트/부활 보유를 동기화한다.
- `WeaponData`, `WeaponBase`, `Gun`, `Sword`를 추가했다. 기본 총은 데미지 12/탄창 12/간격 0.3초, 검은 데미지 20/간격 0.6초/사거리 1.5m다.
- `NetworkCombat`가 K/R/Q/E 입력을 서버 RPC로 전달하고, 서버가 쿨타임·탄약·팀킬·명중·피해를 판정한다. 투사체 런타임 경로는 `NetworkProjectile`과 `NetworkGameObjectPool`을 사용한다.
- `NetworkPlayerStats`에 피격 무적 0.3초, 경직 0.1초, 수동 부활 3초, 부활 후 2초 무적, 은신, 이동 부스트, 아이템 습득 불가 상태를 확장했다.
- `NetworkCrate`, `NetworkCrateSpawner`, `NetworkItemPickup`, `ItemData`를 추가했다. 상자 HP와 가중치 드롭, 보유 제한 초과 시 미획득 규칙을 서버에서 처리한다.
- 방해자 `L`은 기존 코인 잠금과 GDD 아이템 습득 불가를 함께 적용하며, 습득 불가 지속 5초/쿨타임 10초로 설정했다. `O` 밀치기 기본값은 쿨타임 3초/스턴 0.4초로 맞췄다.
- `M3DebugTools`를 Player 프리팹에 연결했다. Development Build에서 F1~F9로 총/검/탄약/부스트/부활/무적/은신/체력 회복/랜덤 아이템을 지급한다.
- 패키지 원본은 수정하지 않고 `Assets/_Project/Prefabs/M3`에 BTM 아이템 복사본, Low Poly Guns 기반 총 외형, 기본 Cube 검 외형, Capsule 투사체를 만들었다.
- Player 프리팹에 `WeaponSocket/Muzzle`을 추가하고 현재 무기 `NetworkVariable`에 따라 각 클라이언트가 총/검 외형을 로컬 장착한다.
- 기존 InGame `Crate_Spawner`를 재사용해 `Crate.prefab`을 연결했다. 씬 관리 비활성화 환경에서 클라이언트 스폰 오류가 발생하지 않도록 스포너의 씬 `NetworkObject`는 제거하고 서버 대기 후 상자를 생성한다.
- `DefaultNetworkPrefabs.asset`에 드롭 아이템 7종과 Capsule 투사체를 등록하고, `Crate.prefab`에 ItemData 가중치 드롭 테이블을 연결했다.
- 상자 드롭 픽업은 Variant의 `GlobalObjectHash=0` 문제를 피하도록 정규 `NetworkItemPickup.prefab`으로 변환해 등록했고, `Crate.prefab`의 `_pickupPrefab`과 드롭 테이블 참조를 재연결했다.
- `M3HudView`와 `NetworkWorldHealthBar`를 추가해 체력/무기/탄약/퀵슬롯/상태/부활 대기/스킬 쿨타임과 월드 체력 바를 최소 표시한다. 키 바인딩은 변경하지 않았다.
- `WeaponData`와 `ItemData`에 손 장착·드롭 스케일/오프셋 및 픽업 반경 보정값을 추가하고, BTM 아이템은 기본 드롭 스케일 0.22와 월드 픽업 반경 0.3을 사용한다.

## 프로젝트 필수 요소 및 문서화 규칙

- 프로젝트 목표는 PC와 Android에서 동일한 게임 흐름을 제공하는 것이다. PC 입력과 Android 터치 입력을 `PlayerInputState` 계층으로 추상화하고, 화면 비율·Safe Area·해상도별 UI 배치를 검증한다.
- 시작·진행·종료·재시작 흐름, 일시정지·앱 중단·복귀, 네트워크 재접속을 명시적인 상태로 관리한다. 저장 데이터는 버전과 기본값을 포함해 안전하게 마이그레이션한다.
- 효과음·배경음은 공용 오디오 데이터와 재생 서비스로 분리하고, 씬 전환·일시정지·앱 백그라운드 복귀에서 재생 상태를 정리한다.
- Editor 전용 코드는 `Editor` 폴더/어셈블리로 제한하고 Player 런타임 코드와 참조를 분리한다. 플랫폼별 분기는 컴파일 심볼과 런타임 플랫폼 검사를 함께 검증한다.
- Addressables는 비동기 로드·실패 안내·재시도·해제를 포함한 수명주기를 구현한다. 핸들 누수, 중복 로드, 씬 종료 후 콜백 실행을 방지하고 로컬/원격 카탈로그를 각각 확인한다.
- ASP.NET Core HTTPS 파일 서버를 사용하는 콘텐츠 경로는 개발용 인증서, health 확인, 다운로드 실패·재시도·복구 결과를 기록한다. 운영 서버·인증서·포트는 승인 없이 변경하지 않는다.
- JSON 설정은 스키마/범위 검증 후 적용하며 읽기 실패·누락·버전 불일치 시 ScriptableObject 또는 코드의 안전한 기본값으로 대체하고 사용자에게 복구 상태를 알린다.
- 플랫폼별 Player 실행 검증은 실제 빌드에서 수행한다. 오류는 재현 조건·원인·수정·재검증 결과를 기록하고, 프로파일러로 CPU·GPU·메모리·GC Alloc·로딩 시간을 수정 전후 비교한다. 측정 환경과 한계를 함께 기록한다.
- 위 항목을 구현할 때 수치와 플랫폼 설정은 가능한 한 ScriptableObject 또는 검증 가능한 설정 파일로 분리하고, 기존 NGO·Relay·Input System·Addressables 패키지 API를 우선 사용한다.

### 프로젝트 소개 문서 갱신 규칙

- 기능·플랫폼·서버·저장·성능·실행 절차를 수정할 때마다 `CoinGame_GDD.md`의 **프로젝트 소개 문서 구성** 표와 관련 세부 항목을 같은 작업에서 갱신한다.
- 표에는 실제 구현 상태와 미확인 상태를 구분해 기록한다. 실행하지 않은 Player·서버·프로파일 검증을 완료로 보고하지 않는다.
- 문서 갱신은 기존 `[확정]` 기획을 변경하지 않는 범위에서 수행하고, 변경 파일·검증 결과·남은 확인 사항을 표 또는 문제 해결 사례에 반영한다.
- 현재 공용 기반에는 `JsonSettingsStorage`, `ApplicationLifecycleService`, `SafeAreaLayout`, `SoundPlayer`, `AddressableRetryLoader`가 포함되어야 하며, 기능을 씬에 연결할 때 실제 Player·서버·기기 검증 상태를 GDD 표에 갱신한다.
## 웹 및 Lands 맵 작업 규칙

- 지원 플랫폼에 Web(WebGL)을 포함하고, 문서의 플랫폼 대응·실행 자료·실행/복원 절차에 Web 결과를 기록한다.
- 맵 작업은 `CoinGame_GDD.md` 17장의 Lands 규칙을 따른다. `Location with environment` 통째 사용을 금지하고 Lands 2x2, 표준 자식 구조, 시드 배치, 금지 구역, 레이어 마스크, 도달 가능성 검증을 유지한다.
- 맵 프리팹과 씬을 수정할 때마다 프로젝트 소개 문서 표와 맵 제작 섹션의 구현 상태·검증 결과·미확인 항목을 함께 갱신한다.
