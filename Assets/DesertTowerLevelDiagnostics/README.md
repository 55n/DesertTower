# Optional Level Diagnostics — 0.2.0

Desert Tower 레벨 에디터의 선택적 이동 검증 모듈입니다. 실제 EnemyAI나 게임 이동 코드가 아닙니다.

기본 에디터와 Input System이 설치되어 있으면 **저장 후 Play 테스트**에 자동 연결됩니다. Input System이 없으면 version define 조건으로 진단 어셈블리의 컴파일을 제외합니다. 전체 진단 폴더를 제외해도 기본 에디터는 컴파일되고 동작합니다.

각 기능은 별도 컴포넌트로 구성됩니다. 세션 구성은 LevelPlaytest, 테스트 출현은 DiagnosticWaveRunner, 모형 이동은 DiagnosticNavigator, 사용자 이동은 DiagnosticWalkthrough, 카메라는 DiagnosticCamera가 맡습니다. 속도 등 테스트 값은 DiagnosticSettings에 있으며 게임의 요소 정의를 변경하지 않습니다.

컴포넌트 어셈블리는 UNITY_EDITOR 컴파일 조건을, 런처 어셈블리는 Editor 플랫폼 제한을 사용하므로 게임 빌드에 들어가지 않습니다. 실제 게임에서 참조할 것은 기본 패키지의 `DesertTower.Levels.Runtime`입니다.

내보내기: `Tools > Desert Tower > Export Optional Diagnostics Package…`. 먼저 기본 에디터를 가져온 프로젝트에 설치합니다. UPM 형태로 배포할 때는 기본 패키지를 먼저 로컬 경로 또는 레지스트리로 해결해야 합니다.
