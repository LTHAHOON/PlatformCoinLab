# Codex 프로젝트 설정 안내

이 프로젝트의 Codex 설치 버전은 `codex-cli 0.164.0-alpha.11.1`이다. 현재 CLI 도움말과 `--strict-config` 동작에서 프로젝트별 `.codex/config.toml` 및 `.codex/agents/*.toml` 커스텀 Subagent 등록 스키마가 노출되지 않아, 지원되지 않는 TOML을 추측해 추가하지 않았다.

역할 분배와 Unity 안전 규칙은 저장소 루트의 `AGENTS.md`에 정의되어 있다. `agents/*.toml`에는 각 역할의 이름, 범위, 읽기 전용 여부와 한국어 지침을 선언해 두었다. 현재 CLI는 이 파일을 자동 등록하는 공식 스키마를 제공하지 않으므로, 실제 Subagent 위임은 Codex IDE 세션의 멀티 에이전트 기능에서 이 manifest를 참고해 수행한다. 세션이 지원하면 Main Agent가 `gameplay`, `ui`, `vfx`, `server`, `reviewer`, `qa`를 독립 작업으로 병렬 호출한다. 현재 실행 환경에서 동시 슬롯은 메인 포함 4개이며, 실제 호출 결과는 작업 완료 보고에 기록한다.

`server` 역할은 Unity Netcode/Multiplayer Services, 세션·로비·룸, Addressables 원격 콘텐츠, ASP.NET Core ContentServer, 보안·데이터 무결성·운영 검증을 담당한다. 프로젝트별 `.codex/agents/*.toml` 등록 스키마가 공식 지원되지 않으므로 Server Agent 정의는 `AGENTS.md`에 보관하고 세션 위임으로 호출한다.

전역 Codex 설정의 Unity MCP 연결은 `C:\Users\82108\.codex\config.toml`의 `[mcp_servers.unityMCP]` 항목을 통해 유지된다. 이 프로젝트 파일은 전역 설정을 복제하거나 덮어쓰지 않는다.
