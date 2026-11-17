# CoinGame

> 현재 개발 중인 **프로토타입**입니다.

코인 수집가와 코인 방해자가 대결하는 탑뷰 비대칭 캐주얼 슈팅 아케이드 게임입니다. 수집가는 스테이지의 코인을 모으고, 방해자는 수집을 방해하고 상대를 처치합니다.

> 기획 기준: [`PlatformCoinLab\CoinGame_GDD.md`](PlatformCoinLab/CoinGame_GDD.md)
> 이 문서는 기획서와 현재 프로젝트에 있는 코드·설정 및 기획서에 기록된 검증 결과를 요약합니다. 검증 기록이 없는 결과는 완료로 간주하지 않습니다.

## 프로젝트 소개

| 항목 | 내용 |
|---|---|
| 게임 이름 | 미지정(CoinGame) |
| 장르 | 탑뷰 비대칭 캐주얼 슈팅 아케이드 |
| 대상 유저 | 10대 |
| 대상 플랫폼 | PC, Android, Web(WebGL) 지원 목표. 실제 Player 빌드·기기 실행 결과는 아래 플랫폼 대응 참고 |
| 핵심 플레이 | Collector는 코인을 모아 라운드를 통과하고, Disruptor는 전투와 능력으로 Collector의 수집을 방해합니다. Collector는 한 명이라도 마지막까지 생존하면 승리하고, Disruptor는 Collector 전원을 처치하면 승리합니다. |
| 아트 방향 | 스타일라이즈 카툰풍 |

## 개발 정보

| 항목 | 내용 |
|---|---|
| 개발 기간 | [미지정] |
| Unity | 6000.6.7f1 |
| 렌더 파이프라인 | Universal Render Pipeline 17.6.0 |
| Netcode for GameObjects | 2.13.3 |
| Input System | 1.20.1 |
| Addressables | 2.11.2 |
| Cinemachine | 6.6.0 |
| UniTask | 2.5.11 / Git 커밋 a77a1b4a89824fb6df293099de3060daf2a432c1 |

패키지 버전의 기준 파일은 `PlatformCoinLab\Packages\manifest.json` 및 `PlatformCoinLab\Packages\packages-lock.json`입니다.

## 실행 자료

| 자료 | 위치 |
|---|---|
| 대표 이미지·플레이 영상 | 아래 모바일 컨트롤 검증 화면 참고. 플레이 영상은 미등록 |
| Windows/Android/Web 실행 파일 | Android: 로컬 [CoinGame.apk](PlatformCoinLab/Build/Android/CoinGame.apk). Web: `PlatformCoinLab/Build/Web` (HTTP 서버로 실행). Windows Player는 미생성 |
| Unity 프로젝트 소스 | `PlatformCoinLab\` |
| 게임 코드 | `PlatformCoinLab\Assets\_CoinGame\Scripts\` |
| 기획서 | `PlatformCoinLab\CoinGame_GDD.md` |
| 콘텐츠 서버 코드 | `ContentServer\Program.cs` |
| 제출용 빌드 산출물 폴더 | 로컬 Android 산출물: `PlatformCoinLab/Build/Android/`. 빌드 파일은 저장소에 포함되지 않으므로 배포 시 별도 첨부 |

## 주요 구현 기능

| 기능 | 목적 및 구현 방식·처리 흐름 | 핵심 코드 위치 |
|---|---|---|
| 방·플레이어 연결 | Unity Multiplayer Session 및 NGO 기반으로 방 참가와 Player 스폰을 관리합니다. Relay를 통한 실제 방 생성·참가, 서버 통신과 이동 동기화의 정상 동작을 사용자가 확인했습니다. 게임 판정은 서버 권한 흐름을 따릅니다. | `Network\Room\RoomSessionService.cs`, `Network\NetworkConnector.cs`, `Network\NetworkSpawner.cs` |
| 역할·라운드 진행 | 역할 정원과 선택을 검증하고, 서버가 라운드·제한 시간·잔여 코인·승패 상태를 관리해 네트워크 변수로 공유합니다. | `Network\Room\RoomSettingsValidator.cs`, `Network\NetworkRoundState.cs`, `Gameplay\Stage\RoundOutcomeEvaluator.cs` |
| 코인 수집 | 서버가 플레이어 역할과 코인 상태를 확인해 Collector의 수집만 처리하고, 라운드 진행 상태를 갱신합니다. HUD는 목표/잔여 코인 진행도를 표시합니다. | `Network\NetworkCoinController.cs`, `Network\NetworkRoundState.cs`, `UI\StageView.cs` |
| 전투·체력·인벤토리 | 공격 요청을 서버에서 검증하고 무기·탄약·피해·사망·부활 및 승패 판정에 반영합니다. 무기와 아이템 정보는 데이터 에셋으로 분리합니다. | `Network\NetworkCombat.cs`, `Network\NetworkPlayerStats.cs`, `Network\NetworkInventory.cs`, `Gameplay\Combat\`, `Gameplay\Items\` |
| 상자·아이템 스폰 | 서버가 상자 체력, 파괴, 가중치 기반 드롭과 픽업을 처리합니다. 공용 스포너와 네트워크 오브젝트 풀을 사용합니다. | `Network\NetworkCrate.cs`, `Network\NetworkItemPickup.cs`, `Utilities\NetworkSpawner.cs`, `Utilities\NetworkGameObjectPool.cs` |
| 입력 전달 | PC Gameplay 입력과 모바일 터치를 `PlayerInputState`로 합치고 Owner만 소비합니다. 자유형 조이스틱과 공격·장전·무기·아이템·역할 스킬 버튼을 씬에 연결했습니다. | `Input/PlayerInputStateProvider.cs`, `Input/MobileControlInput.cs`, `UI/FloatingJoystick.cs`, `UI/MobileControlsView.cs` |
| HUD·플레이어 표시 | 역할·라운드·체력·킬 정보를 UI로 표시하며, 플레이어 이름과 역할 목록을 동기화합니다. 잔디 위장 시 상대방에게 플레이어 렌더러를 감추는 네트워크 상태도 처리합니다. | `UI\InGameHudView.cs`, `UI\PlayerHealthBarView.cs`, `Network\NetworkPlayerIdentity.cs`, `Network\NetworkGrassConcealment.cs` |
| 설정 저장·앱 생명주기 | JSON 설정을 버전과 검증 함수가 포함된 형태로 저장하고, 앱 일시 중단/포커스 상실 시 PlayerPrefs를 저장합니다. | `Utilities\JsonSettingsStorage.cs`, `Managers\ApplicationLifecycleService.cs`, `Player\PlayerProfileStorage.cs` |
| 원격 콘텐츠 기반 | Bootstrap에서 카탈로그 갱신·required 용량 확인·다운로드·재시도 후 로비에 진입합니다. 인게임 씬은 Addressables로 additive 로드·해제합니다. | `Content/ContentBootstrap.cs`, `Utilities/AddressableSceneLoader.cs`, `ContentServer/Program.cs` |

## 플랫폼 대응

| 항목 | 현재 상태 |
|---|---|
| PC 입력 | WASD 이동, K 공격, R 장전, 무기 1/2/3, 아이템 4/5, 방해 능력 O/L. 기존 키보드 경로를 유지하고 PC에서 터치 HUD 숨김을 Editor에서 확인했습니다. P 입력 상태는 있으나 실제 상호작용 처리는 미구현입니다. |
| Android 입력 | 왼쪽 터치 위치에서 나타나는 자유형 조이스틱, 오른쪽 공격·장전·주먹/총/검·부스트/부활·방해자 스킬 버튼. 보유/역할/사망·쿨타임 표시와 배치·크기 JSON 저장 구현. Editor 로컬 Host에서 이동·무기·공격·부활 및 다음 Play의 배치 복원을 확인했습니다. 새 터치 UI의 실제 기기 검증은 남아 있습니다. |
| Web 입력·화면 | WebGL 개발 Player 빌드와 Addressables 초기화 파일·로컬/원격 번들 HTTP 200을 확인했습니다. 브라우저 내 화면·조작·경기는 추가 검증 대상입니다. |
| 화면·해상도 | Safe Area 변경을 갱신하고 버튼 위치를 화면 안으로 제한합니다. Editor 1920×1080·2160×1080 배치를 확인했습니다. 실제 노치·기기별 화면 및 Web Player 검증은 남아 있습니다. |
| 저장 | PlayerPrefs 기반 프로필 저장과 JSON 버전 래핑/검증 코드가 있습니다. 플랫폼별 저장 지속성·복구 테스트는 [미지정] |
| 생명주기 | 앱 Pause/Focus 이벤트에서 저장을 수행하는 공용 서비스가 있습니다. PC·Android·Web에서의 중단/복귀 전체 흐름 검증은 [미지정] |
| 콘텐츠 처리 | 원격 씬/환경/UI atlas/폰트 4그룹과 로컬 공유 그룹으로 분리했습니다. Windows Editor의 다운로드·캐시·씬 로드/해제·서버 복구를 확인했습니다. Android 원격 다운로드 완료·로비 진입은 다른 네트워크의 실제 휴대폰에서 사용자 확인했습니다. |
| 실행 결과 | Unity 6000.6.7f1 Editor 검증 및 Android APK 빌드 성공. 실제 Android 다운로드·로비 진입과 Relay 연결을 통한 서버 통신·플레이어 이동 동기화의 정상 동작을 사용자가 확인했습니다. |

## 추가·개선한 부분

수업 구현과의 상세 비교 기준은 [미지정]입니다. 프로젝트 기획서와 구현 기록에 명시된 개선 사항은 다음과 같습니다.

- 기존 NGO·Relay·스폰·라운드 구조를 유지하면서 역할, 코인, 전투와 승패 판정을 서버 권한으로 확장했습니다.
- M1~M3 범위로 Collector 전용 코인 수집, 체력·사망·부활, 무기·아이템 인벤토리, 상자 드롭 및 HUD를 추가했습니다.
- 플레이어 이름 저장을 Main/Multiplayer Play Mode 인스턴스별로 분리하고, 이름과 방 내 번호를 네트워크로 공유합니다.
- 맵 Grid 높이와 플레이어 바닥 접촉을 보정하고, 툰 머티리얼·잔디 위장·Canvas 기반 체력 바를 적용했습니다.
- 구현 및 검증 근거는 `PlatformCoinLab\CoinGame_GDD.md`의 M1~M3, 프로젝트 소개 및 2026-11-17 기록을 참고하세요. 각 항목의 전체 Player·플랫폼 검증이 완료된 것은 아닙니다.

## 문제 해결 사례

| 문제·재현 조건 | 원인 및 수정 | 재검증 결과 / 한계 |
|---|---|---|
| Unity Multiplayer Play Mode에서 Main과 Player2가 같은 인증 프로필을 사용해 Lobby 세션 초기화가 실패함 | Editor 인스턴스별 `CoinDevHost`/`CoinDevClient` 프로필을 사용하도록 분리 | 기획서에 Main+Player2 로컬 Unity Transport 연결 및 이름 동기화 결과가 기록되어 있습니다. 이후 실제 Relay 방 생성·참가, 서버 통신과 이동 동기화도 사용자가 정상 동작을 확인했습니다. |
| Grid 기반 스폰 위치에서 Player가 바닥과 어긋나거나 이동 중 Y축이 변함 | Ground 표면 높이를 기준으로 셀 높이를 갱신하고, X/Z만 제한하도록 조정했으며 CharacterController 중심을 보정 | 기획서에 Grid 256개 셀 높이 오차 0m 및 이동 후 root Y 변화 0m 측정이 기록되어 있습니다. PC/Android/Web Player 재현 시험은 미실행 |
| 상자에서 나온 아이템을 플레이어가 습득하지 못함 | `Crate_Default`의 드롭 오프셋을 낮추고 기존 서버 픽업 흐름은 유지 | 기획서에 로컬 Host에서 실제 탄약 획득 및 상자/픽업 Despawn 확인이 기록되어 있습니다. 별도 Client와 Player 빌드는 미검증 |

## 성능 개선

| 항목 | 내용 |
|---|---|
| 측정 환경 | [미지정] |
| 수정 전·후 프레임 시간, FPS, GC Alloc, 메모리·로딩 수치 | [미지정] |
| 성능 개선 변경 및 기능 유지 확인 | 정량 비교 자료 [미지정] |
| 측정 한계 | 동일 기기·동일 빌드 설정에서 Profiler/Frame Timing/Memory Profiler 측정이 필요합니다. 기획서에도 성능 프로파일링 미실행 항목이 남아 있습니다. |

시각 효과·게임플레이 변경이 적용되어 있지만, 이 README에서는 정량 측정 자료가 없는 항목을 성능 개선 결과로 주장하지 않습니다.

## 실행·복원 방법과 한계

### Unity 프로젝트 실행

1. Unity Hub에서 Unity **6000.6.7f1**을 설치합니다.
2. `C:\Users\82108\Desktop\PlatformCoinLab\PlatformCoinLab` 폴더를 프로젝트로 엽니다.
3. 시작 씬은 `Assets/_CoinGame/Scenes/Bootstrap_CoinGameScene.unity`입니다. 필수 원격 리소스를 확인·다운로드한 뒤 Persistent 로비로 이동합니다. 인게임 씬은 로비의 정상 매치 시작 흐름으로 진입합니다.
4. Unity Editor에서 실행 후 로비에서 방을 만들거나 참가하고 역할을 선택합니다. 실제 Relay 연결을 통한 방 생성·참가, 서버 통신과 플레이어 이동 동기화는 사용자가 정상 동작을 확인했습니다.

### 조작

기획서 기준 조작은 다음과 같습니다. 최종 빌드의 실제 키 바인딩은 확인 후 보완해야 합니다.

- 이동: WASD
- 공격·상자 공격: K
- 상호작용: P
- 무기 선택: 1(Fist), 2(Gun), 3(Sword)
- 장전: R
- 이동 부스트 / 부활: 4 / 5
- 방해자 밀치기 / 스킬: O / L
- Android: 왼쪽 아래 빈 영역을 누르면 해당 위치에 조이스틱이 나타납니다. 오른쪽 버튼으로 공격·장전·무기·아이템·역할 스킬을 사용합니다. `배치`에서 버튼을 드래그하고 크기를 바꾼 뒤 `완료`를 누르면 저장됩니다. 아이템은 접촉 습득 방식이며, 미구현 P 상호작용의 터치 버튼은 표시하지 않습니다.

### 콘텐츠 서버 및 빌드

- **Web 빌드 시 네트워크 설정:** `NetworkManager` 오브젝트의 `Unity Transport` 컴포넌트에서 `Use Web Sockets`를 켜 주세요. Web 이외의 플랫폼(Android/PC 등)으로 빌드할 때는 꺼 주세요.

- .NET 10 ASP.NET Core 정적 파일 서버이며 `Content`와 `/health`를 제공합니다. 현재 개발용 수신 주소는 `http://0.0.0.0:5250`입니다. 별도 API 서버는 사용하지 않습니다.
- `ContentServer` 폴더에서 `dotnet run --project ContentServer.csproj --no-launch-profile`로 실행합니다. 터미널 실행은 Ctrl+C로 종료하며, 백그라운드 실행은 해당 ContentServer 프로세스를 종료합니다. 휴대폰은 자신이 접근 가능한 서버 IP로 접속해야 합니다.
- `CoinGame > Content > Build Android to ContentServer`로 번들을 `ContentServer/Content/Android`에 생성하고 서버를 실행합니다. 원격 그룹은 Remote_Scenes/Remote_Environment/Remote_UIAtlas/Remote_Fonts이며 `required` 라벨로 함께 다운로드합니다.
- 콘텐츠와 Player의 타깃·Addressables 프로필을 맞춘 뒤 Android 빌드 프로필로 APK를 빌드합니다. 새 C# 입력 스크립트가 추가되었으므로 모바일 UI 적용 시 APK도 갱신해야 합니다. 상세 순서·캐시·복구 절차: [CONTENT_PIPELINE.md](PlatformCoinLab/CONTENT_PIPELINE.md).

### 알려진 한계 및 미확인 환경

- 측정 가능한 성능 기준선과 수정 전·후 비교 자료가 없습니다.
- 모바일 컨트롤 검증 화면은 아래에 첨부했습니다. 영상·개발 기간·정량 성능 수치는 미등록입니다.

## 모바일 컨트롤 검증 화면

인게임에 적용된 모바일 컨트롤의 UI 검증 화면입니다. 기본 버튼 배치와 터치 위치에서 나타나는 자유형 조이스틱, 스킬 쿨타임 표시를 보여 줍니다.

![누르지 않았을 때 숨겨진 조이스틱과 모바일 버튼](PlatformCoinLab/Captures/mobile-controls-landscape.png)

![누른 위치에서 나타난 자유형 조이스틱과 스킬 쿨타임](PlatformCoinLab/Captures/mobile-controls-floating.png)