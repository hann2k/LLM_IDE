# LLM IDE

**A research prototype for a Natural-Language IDE**  
**자연어 IDE 연구용 프로토타입**

LLM IDE is an experimental workspace for managing long-running LLM work as a form of **manual natural-language programming**.

LLM IDE는 장기 LLM 작업을 **수동 자연어 프로그래밍**으로 보고, 이를 관리하기 위한 실험적 작업환경입니다.

## 0. 

AI는 영리합니다.  
AI is smart.

다만 당신의 일을, 바로 이해하진 못하죠.
But it doesn’t understand your work right away.

당신이 일하는 법을 알려주면, 더 잘 잘할거에요.
Teach it how you work, and it will do better.

---

## 1. What is this?

LLM IDE is not just a chat wrapper.

It explores the idea that working with an LLM over a long session is not merely “prompting,” but an iterative process of:

- defining a task goal,
- injecting criteria,
- setting constraints,
- generating outputs,
- detecting violations,
- revising instructions,
- saving operation logs,
- preserving task state,
- and handing off work to another session.

LLM IDE는 단순한 채팅 래퍼가 아닙니다.

이 프로젝트는 장기 LLM 작업을 다음과 같은 반복 과정으로 봅니다.

- 작업 목표 설정
- 기준 구조 주입
- 제약 조건 지정
- 산출물 생성
- 기준 위반 탐지
- 지시 수정
- 운영 로그 저장
- 작업 상태 보존
- 새 세션으로 핸드오프

In this sense, the user is not only asking questions.  
The user is operating the LLM runtime through natural-language specifications.

이 관점에서 사용자는 단순히 질문하는 사람이 아닙니다.  
사용자는 자연어 명세를 통해 LLM 런타임을 운용합니다.

---

## 2. Core idea

Traditional software development has:

```text
source code
→ runtime / compiler
→ execution result
→ debugging
→ versioning
````

LLM-based long-running work needs something similar:

```text
natural-language source state
→ LLM runtime
→ artifact / document / code / decision output
→ CSO/CVO verification
→ criterion revision
→ session handoff
```

기존 소프트웨어 개발에는 소스코드, 런타임, 실행 결과, 디버깅, 버전 관리가 있습니다.

장기 LLM 작업에도 이에 대응하는 구조가 필요합니다.

```text
자연어 소스 상태
→ LLM 런타임
→ 문서 / 코드 / 판단 / 산출물
→ CSO/CVO 검증
→ 기준 수정
→ 세션 핸드오프
```

LLM IDE is an early attempt to build that environment.

LLM IDE는 이러한 환경을 만들기 위한 초기 시도입니다.

---

## 3. Key concepts

### Natural-Language Programming

Natural-language programming is the process of operating an LLM runtime by specifying task goals, criteria, constraints, output formats, current state, and verification rules in natural language.

자연어 프로그래밍이란 사용자가 자연어로 작업 목표, 기준, 제약 조건, 출력 형식, 현재 상태, 검증 조건을 구성하여 LLM 런타임이 특정 산출물을 생성하도록 운용하는 과정입니다.

It is not the same as deterministic programming in Python, C, or JavaScript.
Instead, it is a probabilistic, criterion-conditioned interaction process.

이는 Python, C, JavaScript 같은 결정론적 프로그래밍과 동일하지 않습니다.
대신 기준 구조에 의해 조건화되는 확률적 상호작용 과정입니다.

---

### Natural-Language Source State

A natural-language source state is the reusable working state required to continue an LLM project.

자연어 소스 상태는 LLM 작업을 이어가기 위해 필요한 재활성화 가능한 작업 상태입니다.

It may include:

* task narrative,
* active criteria,
* constraints,
* decisions,
* current state,
* artifact list,
* boundary conditions,
* verification rules,
* handoff instructions,
* authority policies.

포함될 수 있는 요소는 다음과 같습니다.

* 작업 서사
* 활성 기준
* 제약 조건
* 결정 로그
* 현재 작업 상태
* 산출물 목록
* 주제 경계
* 검증 기준
* 핸드오프 지시
* 권한 정책

---

### CSO / CVO

LLM IDE uses the concepts of CSO and CVO.

LLM IDE는 CSO와 CVO 개념을 사용합니다.

```text
CSO = Criterion Satisfied Output
CVO = Criterion Violation Output
```

```text
CSO = 기준만족출력
CVO = 기준위반출력
```

A single LLM output may satisfy one criterion while violating another.

하나의 LLM 출력은 어떤 기준에서는 만족 상태일 수 있고, 다른 기준에서는 위반 상태일 수 있습니다.

Example:

```text
Format criterion: CSO
Factuality criterion: CVO
Scope-control criterion: partial CVO
User-intent criterion: partial CSO
```

This allows LLM outputs to be analyzed not as simple success or failure, but as composite criterion-specific output states.

이를 통해 LLM 출력은 단순 성공/실패가 아니라, 기준별 만족·위반이 결합된 출력 상태로 분석될 수 있습니다.

---

## 4. Current prototype

The current implementation is a WPF-based local prototype.

현재 구현은 WPF 기반 로컬 프로토타입입니다.

Main components:

* WebView2-based LLM web runtime wrapper
* project-level natural-language context editor
* criterion injection through `[SYSTEM_CONTEXT_INIT]`
* project setting synchronization through `[SYSTEM_UPDATE]`
* operation log recording
* incremental log export
* streaming response capture
* HTML-to-Markdown reconstruction
* session list scanning
* experimental session handoff
* file-drop preview area
* placeholder for CSO/CVO verification

주요 구성 요소는 다음과 같습니다.

* WebView2 기반 LLM 웹 런타임 래퍼
* 프로젝트 단위 자연어 컨텍스트 편집기
* `[SYSTEM_CONTEXT_INIT]` 기반 기준 주입
* `[SYSTEM_UPDATE]` 기반 프로젝트 설정 동기화
* 운영 로그 기록
* 증분 로그 저장
* 스트리밍 응답 수집
* HTML → Markdown 복원
* 세션 목록 스캔
* 실험적 세션 핸드오프
* 파일 드래그앤드롭 프리뷰
* CSO/CVO 검증 계층 자리

---

## 5. Prototype architecture

```text
User
  ↓
Natural-language input
  ↓
Project context / criteria
  ↓
LLM IDE wrapper
  ↓
WebView2 LLM runtime
  ↓
LLM output
  ↓
Response capture
  ↓
Operation log
  ↓
Verification / handoff / artifact preservation
```

한국어로 표현하면 다음과 같습니다.

```text
사용자
  ↓
자연어 입력
  ↓
프로젝트 컨텍스트 / 기준 구조
  ↓
LLM IDE 래퍼
  ↓
WebView2 기반 LLM 런타임
  ↓
LLM 출력
  ↓
응답 수집
  ↓
운영 로그
  ↓
검증 / 핸드오프 / 산출물 보존
```

---

## 6. Project context injection

The prototype supports a project-level context editor.

프로토타입은 프로젝트 단위 컨텍스트 편집기를 지원합니다.

On the first message, the project context is injected as:

```text
[SYSTEM_CONTEXT_INIT]
...project settings...

[USER_INPUT]
...user input...
```

After the initial injection, later messages are sent as:

```text
[USER_INPUT]
...user input...
```

When project settings are saved, the prototype sends:

```text
[SYSTEM_UPDATE]
...updated settings...
```

This is an early implementation of criterion/state synchronization.

이는 기준 구조와 작업 상태 동기화의 초기 구현입니다.

---

## 7. Operation logging

LLM IDE records interaction logs with:

* sequence number,
* timestamp,
* user input,
* LLM answer,
* response status.

LLM IDE는 다음 정보를 포함해 상호작용 로그를 기록합니다.

* 순번
* 타임스탬프
* 사용자 입력
* LLM 응답
* 응답 상태

Exported logs are intended to support later analysis of:

* criterion injection,
* model drift,
* correction,
* regeneration,
* CSO/CVO judgment,
* session continuity.

저장된 로그는 이후 다음 분석에 활용될 수 있습니다.

* 기준 주입
* 모델 이탈
* 사용자 교정
* 재생성
* CSO/CVO 판정
* 세션 연속성

---

## 8. Session handoff

The prototype includes an experimental handoff flow.

프로토타입에는 실험적 세션 핸드오프 흐름이 포함되어 있습니다.

The handoff process is roughly:

```text
1. Ask the current session to generate a backup command set.
2. Capture the generated command set.
3. Move to a new LLM session.
4. Inject the command set into the new session.
5. Continue the work.
```

한국어로는 다음과 같습니다.

```text
1. 현재 세션에 백업 명령셋 생성을 요청한다.
2. 생성된 명령셋을 수집한다.
3. 새 LLM 세션으로 이동한다.
4. 명령셋을 새 세션에 투입한다.
5. 작업을 이어간다.
```

The goal is not to reproduce identical text.
The goal is to reactivate a similar task state.

목표는 동일한 문자열을 재현하는 것이 아닙니다.
목표는 유사한 작업 상태를 재활성화하는 것입니다.

---

## 9. Research background

This prototype is connected to the following research ideas:

* LLMs as probabilistic natural-language interpreter runtimes
* criterion-based execution
* natural-language source state
* manual natural-language programming
* CSO/CVO-based output verification
* session continuity
* natural-language IDEs
* user-guided constraint formation

이 프로토타입은 다음 연구 개념들과 연결됩니다.

* LLM을 확률적 자연어 인터프리터 런타임으로 보는 관점
* 기준 기반 실행
* 자연어 소스 상태
* 수동 자연어 프로그래밍
* CSO/CVO 기반 출력 검증
* 세션 연속성
* 자연어 IDE
* 사용자 주도 기준 형성

---

## 10. What this is not

This project is not:

* a commercial product,
* an official client for any LLM service,
* a production-ready automation tool,
* a deterministic natural-language programming language,
* a claim that LLMs execute natural language like formal code,
* a replacement for official APIs,
* a tool for bypassing service policies.

이 프로젝트는 다음이 아닙니다.

* 상용 제품
* 특정 LLM 서비스의 공식 클라이언트
* 운영 환경용 자동화 도구
* 결정론적 자연어 프로그래밍 언어
* LLM이 자연어를 형식 코드처럼 실행한다는 주장
* 공식 API의 대체물
* 서비스 정책 우회를 위한 도구

---

## 11. Important disclaimer

This repository is a research prototype and proof-of-concept.

이 저장소는 연구용 프로토타입이며 개념검증용 코드입니다.

The current implementation may rely on WebView-based observation and interaction with an LLM web interface. Such behavior can be fragile because web UI structures may change.

현재 구현은 WebView 기반으로 LLM 웹 인터페이스를 관찰하고 조작하는 방식을 포함할 수 있습니다. 웹 UI 구조는 언제든 바뀔 수 있으므로 이 방식은 안정적이지 않을 수 있습니다.

Users are responsible for checking and following the terms, policies, and usage rules of any LLM service they access.

사용자는 자신이 접근하는 LLM 서비스의 약관, 정책, 사용 규칙을 직접 확인하고 준수해야 합니다.

This project is intended for research, local experimentation, and conceptual exploration.

본 프로젝트는 연구, 로컬 실험, 개념 탐색을 목적으로 합니다.

---

## 12. Roadmap

Possible next steps:

* structured natural-language source state file
* criterion list editor
* artifact tracker
* CSO/CVO verification panel
* session handoff generator
* operation-log dataset export
* prompt/criterion diff view
* model drift detection
* scope-overrun warning
* official API-based runtime adapter
* multi-model runtime support

향후 가능한 작업은 다음과 같습니다.

* 구조화된 자연어 소스 상태 파일
* 기준 목록 편집기
* 산출물 추적기
* CSO/CVO 검증 패널
* 세션 핸드오프 생성기
* 운영 로그 데이터셋 export
* 프롬프트/기준 diff view
* 모델 drift 탐지
* 범위 과확장 경고
* 공식 API 기반 런타임 어댑터
* 다중 모델 런타임 지원

---

## 13. Repository status

Current status:

```text
Research prototype
Proof-of-concept
Experimental
Not production-ready
```

현재 상태:

```text
연구용 프로토타입
개념검증 단계
실험적 구현
운영 환경용 완성품 아님
```

---

## 14. Suggested citation

If you use or discuss this project, please cite the related papers and records when available.

이 프로젝트를 사용하거나 논의할 경우, 관련 논문 및 공개 기록을 함께 인용해 주세요.

Suggested description:

```text
LLM IDE is a research prototype for managing long-running LLM work as manual natural-language programming. It explores natural-language source state, criterion injection, operation logging, CSO/CVO verification, and session handoff in an LLM workspace.
```

한국어 설명:

```text
LLM IDE는 장기 LLM 작업을 수동 자연어 프로그래밍으로 관리하기 위한 연구용 프로토타입이다. 자연어 소스 상태, 기준 주입, 운영 로그, CSO/CVO 검증, 세션 핸드오프를 LLM 작업환경 안에서 실험한다.
```

---

## 15. License

License information will be provided by the repository owner.

라이선스 정보는 저장소 소유자가 별도로 지정합니다.

---

## 16. Author

Hochul Lee
Independent Researcher

---

## 17. Short summary

LLM IDE is an experimental Natural-Language IDE.

It treats long-running LLM work as a process of maintaining task state, criteria, constraints, artifacts, verification rules, and session handoff information.

It is not just a better prompt editor.
It is an early attempt to build a development environment for natural-language work with LLMs.

LLM IDE는 실험적 자연어 IDE입니다.

장기 LLM 작업을 작업 상태, 기준 구조, 제약 조건, 산출물, 검증 규칙, 세션 핸드오프 정보를 유지하는 과정으로 봅니다.

이는 단순한 프롬프트 편집기가 아닙니다.
LLM과 함께 수행하는 자연어 작업을 위한 개발환경을 만들기 위한 초기 시도입니다.
