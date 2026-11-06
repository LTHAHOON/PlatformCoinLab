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
