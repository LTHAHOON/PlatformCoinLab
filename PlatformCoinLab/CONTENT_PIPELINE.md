# 부트스트랩과 Addressables 콘텐츠

시작 씬은 `Assets/_CoinGame/Scenes/Bootstrap_CoinGameScene.unity`다. Bootstrap → 카탈로그 초기화/갱신 → `required` 용량 확인 → 다운로드 확인 버튼 → 바이트 진행률 → Persistent 로비 순서다. 캐시 용량이 0이면 로비로 바로 진입하며 실패 시 다시 시도할 수 있다. 별도 API 서버는 사용하지 않는다.

## 그룹 구성

| 그룹 | 내용 | 이유 |
|---|---|---|
| Local_Core | 기존 Player, Coin, Crate, 이동/역할/Stage 설정 | Persistent 로비와 NGO 프리팹 등록의 직접 참조를 유지 |
| Local_Shared | 입력 에셋, Toon/TMP 공용 셰이더 | 로컬/원격 번들의 중복 의존성 제거 |
| Remote_Scenes | InGame_CoinGameScene, 주소는 기존 전체 씬 경로 | 씬 배치·정적 배칭 메시·HUD·연출 구성의 업데이트 단위 |
| Remote_Environment | 현재 맵의 재질 4개·텍스처 2개·잔디 셰이더 | 환경 표현을 씬·폰트와 독립적으로 갱신 |
| Remote_UIAtlas | 기존 GUI.png와 Sprite 하위 에셋 | UI 이미지 변경을 다른 콘텐츠와 분리 |
| Remote_Fonts | NotoSansKR_Dynamic 및 암시적 원본 글꼴·아틀라스 | 큰 한글 폰트를 씬 변경 때 다시 받지 않도록 분리 |
| Default Local Group | 기본 그룹 유지 | Addressables 공용 shader/MonoScript 번들 생성 기준 |

Bootstrap/Persistent/Waiting은 일반 Player 씬이다. InGame은 일반 Build Settings에서 제외했고 GameManager가 Addressables로 additive 로드하며 AddressableSceneLoader가 씬 핸들과 전환을 관리한다. 맵 오브젝트와 Lands 배치는 변경하지 않았다. 씬은 주소를 유지하지만 큰 의존 에셋은 별도 번들이다. Remote_Scenes는 Pack Separately, 나머지는 같은 수명주기·변경 단위끼리 Pack Together를 사용한다. 모든 Remote 그룹은 `required` 라벨을 사용하므로 기존 부트스트랩 다운로드 흐름이 함께 처리한다.

그룹별 명시 에셋은 프로젝트 루트 `AddressablesLayout.json`에 저장했다. `CoinGame > Content > Configure Content Groups`로 현재 프로필 URL이나 Bootstrap UI를 덮어쓰지 않고 적용할 수 있다. 기존 Remote_Required 그룹은 GUID를 보존해 Remote_Scenes로 이름을 변경했다. 에셋 파일 자체는 이동하지 않았고 패키지 원본·씬·프리팹을 수정하지 않았다. 새 맵이 생기면 씬별 번들을 추가하고 그 맵에서 공유하는 환경 데이터만 추출한다. 메시는 현재 씬의 정적 배칭 데이터로도 저장되므로 원본 FBX/Prefab을 무작정 명시 등록해 쓰지 않는 모델까지 추가하지 않는다.

Local_Shared가 새 로컬 번들이므로 이 구조로 전환할 때는 **콘텐츠와 Player를 함께 다시 빌드**해야 한다. 이전 Player에 새 원격 카탈로그만 제공하면 새 로컬 의존 번들이 없을 수 있다. Resources 및 일반 Player 씬에 직접 참조된 글꼴·UI의 Player/Addressables 간 중복은 별도 구조 문제이며 이번 bundle 간 중복 0건과 구분한다.

## 빌드와 실행

1. Unity 6000.6.7f1에서 Addressables 프로필을 선택한다. `CoinGame_Profile`은 PC/Editor `http://localhost:5250/[BuildTarget]`, `CoinGame_Android`는 현재 실제 기기용 공인 주소 `http://222.120.162.193:5250/[BuildTarget]`다. PC 공인 주소가 바뀌면 Android 프로필 URL과 콘텐츠·Player를 함께 다시 빌드한다.
2. `CoinGame > Content > Build Active Target to ContentServer`를 실행한다. Remote.BuildPath가 `../ContentServer/Content/[BuildTarget]`이므로 수동 복사 없이 같은 서버 폴더로 출력된다.
3. Android는 `CoinGame > Content > Build Android to ContentServer`를 실행한다. Android 프로필과 타깃을 선택하고 재컴파일 이후 번들을 `ContentServer/Content/Android`에 생성한다. 활성 타깃은 Android로 남는다. Windows 번들을 Android 폴더로 복사하지 않는다.
4. ContentServer 폴더에서 `dotnet run --project ContentServer.csproj --no-launch-profile`을 실행한다. appsettings.json의 기본 수신 주소는 `http://0.0.0.0:5250`이다. PC 내부와 외부 기기에서 같은 5250 포트로 접근한다. HTTPS가 필요하면 신뢰하는 인증서를 준비하고 --urls로 별도 지정한다.
5. Editor에서 다운로드를 검사할 때 Addressables Play Mode Script를 **Use Existing Build**로 선택한다. Use Asset Database는 파일을 직접 읽어 용량이 0이 되므로 원격 검증에 사용하지 않는다.
6. 같은 프로필/타깃의 Addressables 빌드 뒤 Player를 빌드한다. 공용 Player Settings는 DevelopmentOnly이며 Android™ 개발 프로필의 override는 로컬 HTTP 서버 검증을 위해 AlwaysAllowed다. 배포용은 신뢰 가능한 HTTPS URL과 적절한 HTTP 정책으로 바꾸고 콘텐츠와 Player를 다시 빌드한다.

에디터의 Use Existing Build 검증은 **StandaloneWindows64 + CoinGame_Profile + Windows용 Addressables**를 사용한다. Android 번들을 빌드한 상태에서 그대로 Editor Play를 시작하지 않는다. Android 커스텀 Build Profile이 활성화돼 있으면 Player Settings override도 적용되므로 전역 설정과 실제 컴파일 심벌이 다를 수 있다. JSON 카탈로그를 사용할 때 활성 프로필의 Scripting Defines에도 `ENABLE_JSON_CATALOG`가 적용돼 있어야 한다. 현재 Android™ 프로필에는 해당 심벌을 추가해 활성화했다. 기존 APK에는 소급 적용되지 않으므로 다시 빌드해야 한다.

**Android 프로필을 유지한 에디터 실행**은 Addressables Play Mode Script의 **Use Asset Database**를 사용한다. 현재 이 모드로 설정했다. 이때 부트스트랩은 로컬 에셋의 필수 용량을 0으로 판단해 로비로 진행하며 Android 원격 번들 다운로드를 수행하지 않는다. 실제 APK는 Play Mode Script와 관계없이 Packed Player Build와 Android 원격 카탈로그/번들을 사용한다. Android 네이티브 다운로드는 Android 에뮬레이터/기기에서 검증한다.

`dotnet run`은 서버가 실행되지 않았을 때 한 번만 실행한다. 이미 health와 카탈로그가 HTTP 200이면 같은 포트로 서버를 추가 실행하지 않는다. 현재 서버는 그대로 유지했고 코드·포트·인증서는 변경하지 않았다.

실제 Android 기기에서 `localhost`와 `10.0.2.2`는 PC 서버 주소가 아니다. 현재 PC의 이더넷 주소와 외부 조회 공인 주소가 모두 222.120.162.193으로 일치했다. 다른 네트워크의 실제 휴대폰에서 이 주소의 /health 응답을 사용자 확인했다. Windows 방화벽에는 ContentServer.exe만 TCP 5250을 수신하는 `CoinGame.ContentServer.Http5250` 규칙을 추가했다. 재설정은 ContentServer/Enable-ContentServerFirewall.ps1을 관리자 권한으로 실행한다. 방화벽을 비활성화하거나 다른 포트를 열지 않는다. 서버는 실행 중으로 유지한다.

Web은 WebGL 타깃으로 별도 콘텐츠/Player를 빌드한다. 서버에 정적 파일용 CORS를 추가했으며 HTTPS 페이지에서는 Remote.LoadPath도 HTTPS로 맞춰야 한다. 실제 Web Player는 별도 검증이 필요하다.

그룹 초기 설정 메뉴 `Configure Groups and Bootstrap`은 기본 PC 개발 URL을 재설정한다. 기존 Bootstrap 씬이 있으면 UI를 덮어쓰지 않는다. 평상시에는 빌드 메뉴만 사용한다. 기존 해시 번들은 원격 카탈로그의 구버전 클라이언트를 위해 자동 삭제하지 않는다.

## 종료 처리

RoomSessionService는 중복 Dispose 요청에 같은 완료 작업을 반환한다. 일반 Host 종료는 DeleteAsync, Client 종료는 LeaveAsync다. Play Mode 종료에서는 SDK의 OnQuitting보다 먼저 LeaveAsync를 시작하여 SDK 자동 종료와 OnDestroy의 재진입을 막는다. MPP의 Play Mode 전환을 취소하지 않는다. 이때 SDK가 이미 폐기한 lobby 채널의 ObjectDisposed 예외만 종료 상황으로 한정 처리하고 다른 실패는 경고로 남긴다. 실제 애플리케이션 종료는 wantsToQuit에서 정상 종료를 기다린다.

## 검증 기록

### 다른 네트워크의 실제 휴대폰 접속 경로

- 기존 APK는 에뮬레이터 전용 10.0.2.2 주소를 포함했고 서버는 localhost에만 수신했다. Android 프로필을 현재 PC 공인 주소로 변경하고 서버 appsettings.json의 Urls를 0.0.0.0:5250으로 변경했다. 기존 개발 서버의 실행 파일 경로를 확인한 뒤 종료·재시작했다.
- 관리자 권한으로 프로그램 경로와 TCP 5250을 제한한 Windows 수신 허용 규칙을 추가했다. netsh 조회에서 Enabled Yes / Direction In / Action Allow / LocalPort 5250 및 ContentServer.exe 경로를 확인했다. 사용자 PC 자체의 공인 주소 요청 HTTP 200과 별개로, 다른 네트워크의 휴대폰 브라우저에서 /health의 status ok를 사용자 확인했다.
- Android 원격 카탈로그의 4개 번들 URL이 공인 주소를 사용하는 것을 확인했다. 콘텐츠 및 APK 재빌드는 Succeeded, 오류 0개·경고 0개다. 당시 Build/Android/CoinGame.apk는 69,080,625 bytes였다.
- 사용자가 PC와 다른 네트워크의 실제 휴대폰에 최신 APK를 설치한 뒤 부트스트랩 다운로드 완료·로비 진입을 확인했다. 이 결과는 사용자 확인이며 ADB 로그 수집이나 실제 경기·재접속·성능 검증을 포함하지 않는다. ContentServer는 실행 상태로 유지했다.

### Android 프로필의 HTTP 차단 수정

- Android™의 Player Settings override가 `NotAllowed`여서 JSON 심벌 수정 후에도 `Insecure connection not allowed`와 TextDataProvider/DownloadDependencies 오류가 발생했다. 개발 프로필의 Allow downloads over HTTP를 AlwaysAllowed로 저장했으며 공용 Player Settings나 서버 포트는 변경하지 않았다.
- Android 프로필을 유지한 Editor Play는 Use Asset Database로 설정했다. 같은 프로필에서 Bootstrap → Persistent → InGame 로드/언로드와 Console 오류/경고 0개를 확인했다. UnityWebRequest로 ContentServer health를 호출해 Success/HTTP 200도 확인했다. 이 검사는 Android 네이티브 번들 다운로드 검사가 아니다.
- JSON 심벌과 HTTP 허용 정책을 반영한 APK 재빌드는 성공했다. BuildReport는 Succeeded, 오류 0개·경고 4개, 약 145초다. Build/Android/CoinGame.apk는 69,080,589 bytes이며 Player의 Unity.Addressables 어셈블리 컴파일 심벌에 ENABLE_JSON_CATALOG도 확인했다. 경고는 기존 Toon 셰이더와 TMP 네이티브 컴파일 안내다.
- 현재 adb devices에서 연결된 기기/에뮬레이터는 없었다. 실제 기기 설치·실행·원격 다운로드는 미확인이다. 10.0.2.2는 Android 에뮬레이터의 호스트 PC 별칭이며 실제 휴대폰에는 기기가 접근 가능한 서버 주소가 필요하다.

### 에디터 JSON 카탈로그 실행 오류 수정

- Console에서 바이너리 리더가 JSON 카탈로그를 읽는 형식 오류와 Android 번들의 Editor 호환 경고를 확인했다. 활성 Android 프로필의 컴파일 심벌에는 ENABLE_JSON_CATALOG가 없었다.
- Android 프로필의 추가 심벌을 활성화한 뒤 컴파일 반영을 확인했다. 에디터는 Windows 타깃·PC 주소 프로필로 전환하고 콘텐츠를 재생성했다.
- 정상 서버에서 실제 부족 콘텐츠 다운로드 → 남은 필수 크기 0 → 로비 진입 → InGame 로드/언로드를 검증했다. 마지막 구간 Console 오류/경고 0개, Missing Material 0개다. Android APK 재빌드와 실제 기기 실행은 이번 검사에 포함하지 않았다. Android 프로필의 HTTP 허용 정책·기기용 URL도 실행 환경에 맞춰 별도 확인해야 한다.

### Android Player의 NUnit Linker 오류 수정

- `Assets/_CoinGame/Tests/EditMode`를 Unity의 Editor 전용 경로인 `Tests/Editor`로 이동했다. 테스트와 meta 파일은 원본 해시 및 GUID를 보존했다. 기본 Assembly-CSharp 런타임 구조를 유지했고 패키지나 스트리핑 설정을 바꾸지 않았다.
- Player 소스 목록에서 테스트가 제외됐으며 Editor 어셈블리에서 게임 규칙 테스트 4개를 포함한 전체 EditMode 7/7개가 통과했다.
- 기존 `Assets/Settings/Build Profiles/Android™.asset`으로 실제 APK 재빌드에 성공했다. Unity 6000.6.7f1 BuildReport: Succeeded, 오류 0개·경고 7개, 약 249초. `Build/Android/CoinGame.apk`의 크기는 69,102,553 bytes다. 제보한 NUnit IL1005는 재발하지 않았다.
- Diagnostics 심벌·Toon 셰이더·TMP 컴파일 분리·MCP 연결 관련 경고는 별도 항목으로 남긴다. 실제 Android 기기 설치/실행과 콘텐츠 다운로드는 이번 빌드 검사에 포함하지 않았다. HTTP/실기기 서버 주소 조건은 위 실행 절차를 따른다.

### 그룹 분리 후 검증

| 항목 | 이전 | 분리 후 |
|---|---:|---:|
| Windows 원격 합계 | 8,847,719 bytes | 8,481,126 bytes |
| Windows 씬 번들 | 8,847,719 bytes | 1,550,364 bytes |
| Android 원격 합계 | 10,159,134 bytes | 8,525,637 bytes |
| Android 씬 번들 | 10,159,134 bytes | 1,550,365 bytes |
| Addressables 번들 간 중복 에셋 | 2개 | 0개 |

- Windows/Android 모두 Build Layout의 BuildError 없음. ContentServer의 각 타깃 폴더에 씬·환경·UI·폰트 번들 4개와 새 카탈로그를 생성했다. 해시가 바뀐 이전 번들은 구버전 참조 보존을 위해 삭제하지 않았다.
- Windows Use Existing Build에서 4개 필수 번들 다운로드 → 남은 용량 0 → 로비 진입 → 원격 InGame 로드 → 맵/HUD 화면 확인 → 언로드를 확인했다. Missing Material 0개, 해당 Play Mode Console 오류/경고 0개였다.
- 씬 번들의 감소는 최초 다운로드 전체가 같은 비율로 줄었다는 뜻이 아니다. 씬 구성만 바뀌고 의존 에셋이 그대로일 때 다른 번들의 캐시를 재사용할 수 있는 구조다. 실제 씬 수정 후 증분 배포 전송량은 아직 측정하지 않았다.
- `Assets/_CoinGame/Generated/AddressablesGroupingReport.json`에 타깃별 크기와 번들 의존성·중복 수를 기록했다. Player/Resources 직접 참조 중복, 실제 Android 기기/Web/PC Player와 2인 네트워크는 이번 그룹 분리 검사에 포함하지 않았다.
- 그룹 정의를 JSON에서 읽는 Editor 전용 ContentGroupLayout은 직렬화·검증·기존 GUID 보존을 명시하기 위해 350자 규칙 예외다. Android 빌드 요청은 타깃 전환/재컴파일이 끝난 뒤 Editor update에서 이어서 실행한다.

### 초기 단일 원격 그룹 검증 기록

- Unity 6000.6.7f1 컴파일 오류 0개 확인.
- Windows Addressables 빌드 성공. ContentServer/Content/StandaloneWindows64의 catalog/hash/bundle과 HTTP 200 확인.
- Use Existing Build Play Mode에서 필수 크기 8,847,719 bytes 확인. 다운로드 버튼으로 전송 후 남은 용량 0 및 Persistent 로비 진입 확인. 해당 구간 Console 오류/경고 0개.
- GameManager를 통한 원격 InGame additive 로드 및 언로드 확인.
- ContentServer .NET 10 빌드 오류/경고 0개 확인.
- Android 타깃 Addressables 빌드 성공(BuildError 없음). Content/Android에 카탈로그·해시 및 10,159,134-byte 번들 생성. Android 카탈로그 HTTP 200 확인. 작업 후 활성 타깃/프로필은 Windows/CoinGame_Profile로 복원했다.
- 서버를 중단한 상태에서 실패 안내·재시도 버튼을 확인했다. 서버 복구 후 같은 Play Mode에서 재시도 → 용량 확인 → 다운로드 → Persistent 진입을 확인했다. 의도한 장애 구간의 Addressables 오류 로그는 예상 결과다. 제한 시간 타이머는 작업 완료 시 함께 Dispose하여 이후 CancellationTokenSource 오류를 방지한다.
- 실제 Relay Host 세션에서 Play Mode 종료 후 제보한 disposed lobby/never started/Called after dispose 경고는 재발하지 않았다. 다만 SDK Wire의 `The header part of a frame could not be read` 종료 로그가 1회 관찰되어 전체 SDK 종료 로그 0개로 보장하지 않는다.
- 최종 재검증은 실제 Relay Host 연결·원격 씬 로드/언로드·45초 실행 후 Play Mode 종료까지 Console 오류/경고 0개였다. 종료 후 에디터가 정상 Edit Mode이고 Bootstrap 씬이 열린 상태임을 확인했다. 검증용 ContentServer 프로세스는 종료했다.
- 기존 EditMode 검색 결과 3/3 통과. 검색된 테스트는 패키지 예제 테스트로 이번 다운로드/네트워크 회귀를 직접 검증하지 않는다. 해당 기능은 위 Play Mode 검사로 확인했다.
- PC/Android/Web 실제 Player 및 원격 Host+Client는 아직 미실행이다. Android 번들 생성은 Android 실기기 다운로드 검증과 구분한다. Toon Shaders Pro의 기존 `_FORWARD_PLUS` 폐기 예정 키워드 경고가 빌드 중 관찰됐으며 패키지 원본은 수정하지 않았다.

새 Content 컴포넌트와 Editor 도구는 직렬화된 필드와 비동기 수명주기를 명시하기 위해 350자 규칙 예외로 기록한다. 설정, UI, 다운로드 흐름, 씬 핸들, 그룹/씬 구성, Android 빌드, Editor 종료를 별도 파일로 분리했다. 기존 긴 통합 컴포넌트는 GUID와 기존 동작을 보존하면서 필요한 부분만 수정했다.

### 모바일 컨트롤 추가 후 콘텐츠 갱신

- InGame의 MobileControls에 자유형 조이스틱과 행동 버튼을 연결하고 Android Addressables를 ContentServer/Content/Android에 재생성했다. 새 런타임 입력/UI 타입이 추가되었으므로 번들만 갱신하는 것으로는 부족하며 APK도 함께 설치해야 한다.
- 2026-11-17 23:52 KST Android 프로필 APK 재빌드는 Succeeded, 오류 0개·경고 4개, 약 149초다. 현재 Build/Android/CoinGame.apk는 69,111,633 bytes이며 경고는 Diagnostics Debug Symbols 및 TMP IL2CPP 분할 컴파일 안내다. 기존 EditMode 7/7 및 Editor 로컬 Host의 이동·공격·무기·부활·레이아웃 복원은 확인했다.
- 실제 휴대폰의 기존 원격 다운로드·로비 진입 성공과 이번 새 터치 UI 검증은 구분한다. 새 APK의 실제 멀티터치·노치·앱 중단/복귀 및 원격 Host+Client 전체 경기는 미확인이다. 확인 이미지는 Captures/mobile-controls-landscape.png와 mobile-controls-floating.png다.
### 2026-11-18 그림자·스포너 보정 반영

인게임 조명의 그림자 bias override를 Android 원격 씬 번들에 반영했다. BaseSpawner의 초기화 동시 대기 보정은 런타임 C# 변경이므로 APK도 재빌드했다. 최신 APK는 69112945 bytes, 빌드 Succeeded(오류 0·경고 4, 약 56초)이며 상세 검증과 SDK Wire 종료 경고는 GDD의 2026-11-18 기록을 참고한다.

### 2026-11-18 입력창·대기 조이스틱·상자 보정 반영

Persistent 입력창 Viewport 참조, RoomWaiting 이동 전용 조이스틱과 Crate_Default의 열림 축을 수정했다. Android 콘텐츠와 APK를 다시 빌드했으며 APK는 69114493 bytes, Succeeded(오류 0·경고 5, 약 144초)다. 대기 씬/런타임 스크립트가 포함되므로 새 APK를 설치해야 한다. Editor 드래그 8/8·로컬 Host 이동/열림·EditMode 7/7 검증은 GDD의 관련 기록을 참고한다.

### Web Player의 StreamingAssets/aa 누락 복구

WebGL은 Android/Windows와 별도의 Addressables 콘텐츠가 필요하다. 현재 Web Build Profile로 Player를 빌드하면 WebAddressablesBuild가 해당 타깃 콘텐츠를 자동 생성하고, Addressables 기본 빌드 처리기가 StreamingAssets/aa에 포함한다. 기존 Android 설정과 수동 콘텐츠 빌드 메뉴는 유지한다. Web 4개 프로필에는 ENABLE_JSON_CATALOG가 적용돼 있다.

2026-11-18 Web Desktop Development 빌드는 Succeeded(오류 0·경고 12, 약 112초)다. 산출물은 Build/Web이며 StreamingAssets/aa/settings.json·catalog.json과 WebGL 로컬 번들이 들어 있다. 원격 콘텐츠는 ContentServer/Content/WebGL이다. settings.json과 번들 8개의 HTTP 200 및 Wasm MIME을 확인했다. 실제 브라우저 화면/게임플레이 검증은 URL 확인이 불가능한 Computer Use 정책 제한으로 중단됐다.

현재 로컬 확인 주소는 http://localhost:58135/ 이며 기존 ContentServer를 활용한 루프백 정적 서버가 Build/Web만 제공한다. 같은 포트에 서버가 이미 실행 중이면 다시 실행하지 않는다. 꺼진 경우 프로젝트 루트 PowerShell에서 다음 명령을 사용할 수 있다.

```powershell
& '..\ContentServer\bin\Debug\net10.0\ContentServer.exe' --FilesRoot "$PWD\Build\Web" --urls 'http://localhost:58135'
```

원격 콘텐츠를 받으려면 기존 5250 ContentServer도 실행 중이어야 한다. 이전 오류 페이지는 Ctrl+F5로 새로고침한다. 다른 PC나 HTTPS 배포에서는 localhost 개발 URL 대신 해당 클라이언트에서 접근 가능한 콘텐츠 URL과 맞는 스킴을 사용한다.
### Web 다운로드 완료 후 로비 판정

WebGL의 GetDownloadSizeAsync는 Unity 번들 캐시 지원이 없으면 다운로드 후에도 원격 번들 크기를 반환한다. Bootstrap에서 확인한 이번 실행의 준비 완료 라벨을 로비에 전달해 방 생성/참가를 허용하도록 보완했다. 새 확인·실패·앱 시작 때 초기화하며 영구 캐시 플래그는 저장하지 않는다. 5250은 콘텐츠 서버, 58135는 로컬 Web 페이지 서버이므로 포트가 달라도 정상이다. Relay 기본 WSS는 유지했다. 로비/대기 Status의 한글 폰트도 연결했다.
