# SandGuard 나선형 피라미드

Unity에서 `Scenes/SandGuard_Spiral_Blockout.unity`를 열면 모델을 확인할 수 있습니다. 상단 메뉴 **DesertTower → Levels → Open Spiral Blockout**으로도 열 수 있습니다.

- **Scene 뷰:** 모델을 선택하고 F로 화면에 맞춥니다. Alt + 왼쪽 드래그로 회전하고 휠로 확대합니다.
- **Game 뷰:** 저장된 카메라에서 전체 모습을 봅니다.
- **주황색 선:** 적이 지상 입구부터 정상 코어까지 이동하는 나선형 경로입니다.
- **청록색 선:** 플레이어의 수직이동과 착지 위치를 설명하는 가이드입니다.
- Hierarchy의 `Inspection Guides` 오브젝트를 끄면 가이드를 숨길 수 있습니다.

`Models`에는 FBX 원본, `Materials`에는 URP 재질, `Prefabs`에는 재질과 지형 충돌체를 설정한 프리팹이 있습니다. 다른 씬에 모델을 배치하려면 프리팹을 사용하면 됩니다.

층고 3m, 길 폭 약 4m의 블록아웃입니다. 건설 슬롯은 총 8개이며 배치는 임시입니다. 내부 진입, 워프, 포탈은 없습니다. 이 씬은 모델 확인용으로, 적 AI나 플레이어 이동을 실행하는 게임 씬은 아닙니다.

Blender 원본은 프로젝트의 `Docs/LevelArt/Blender/SandGuard_Spiral_Blockout_v1.blend`입니다. 메뉴의 `Rebuild Spiral Blockout Preview`는 이 폴더의 프리뷰 씬과 프리팹을 다시 생성하므로, 직접 수정한 씬은 다른 이름으로 저장하세요.
