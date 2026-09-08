# VFX

코드로 생성하는 파티클 프리팹 모음. 근거 문서: `Docs/SandGuard_VFX_제작계획_v0.2.md`.
프리팹은 손으로 고치지 말고 `Editor/` 빌더를 수정한 뒤 다시 빌드한다(GUID 유지, 덮어쓰기 안전).

## 빌드

Unity 메뉴 `DesertTower > VFX > Build All`, 또는 배치:

```
Unity.exe -batchmode -projectPath . -executeMethod DesertTower.VFX.Editor.VfxBatch.BuildAll -quit
```

`BuildAllAndRender`로 바꾸면 `Temp/VfxPreview/`(또는 env `VFX_PREVIEW_DIR`)에 프레임 PNG를 함께 뽑는다. 렌더는 `-nographics` 없이 실행해야 한다.

## 눈으로 확인하기

- **에디터에서 바로**: 프리팹을 씬에 놓고 루트를 선택하면 Particle Effect 오버레이의 Play가 자식 레이어까지 재생한다(루트마다 빈 허브 ParticleSystem이 있다). 단, 스크립트가 움직이는 부분(경험치 흡수, 코어 안정도, 큐브 회전, 라이트 깜빡임, 시설 솟아오름)은 에디트 모드에서 안 돈다.
- **전부 한 번에**: `DesertTower > VFX > Showcase > Open Showcase Scene + Play`. `Scenes/VFX_Showcase.unity`에 8개 효과가 스테이션별로 놓여 있고 `VfxShowcaseStation`이 반복 재생한다 — 투사체는 날아가서 착탄하고, 적 사망은 매번 다른 색으로, 코어는 안정도 100→0→100을 오간다. 씬은 `Build All`이 다시 만든다.

## 프리팹

| 프리팹 | 계획서 | 원점 | 런타임 연결 |
|---|---|---|---|
| `VFX_ManaBolt_Projectile` | #1 | 투사체 위치, 로컬 +Z가 진행 방향 | 투사체에 자식으로 붙인다. 총구 플래시는 스폰 시 1회 자동 재생 |
| `VFX_ManaBolt_Impact` | #2 | 착탄점 | 없음 |
| `VFX_Enemy_Death` | #23 | 적 발밑 | `VfxTint.Apply(적 색)` · `VfxParticleAttractor.Target = 플레이어` |
| `VFX_Build_Complete` | #14 | 슬롯 중심(바닥) | 시설 오브젝트에 `VfxScaleIn`을 붙이면 0→1 솟아오름 |
| `VFX_Core_Ambient` | #9 | 코어 바닥 (크리스탈 y=1.2) | `CoreAmbientVfx.SetStability(0~100)` — 60/30 경계에서 틸→노랑→빨강+연기 |
| `VFX_FlameCobra_Breath` | #17 | 코브라 입, 로컬 +Z가 분사 방향 | 루프. 공격 시작/끝에 Play/Stop |
| `VFX_Fire_Impact` | #17 | 화염 착탄점 | 없음 |
| `VFX_Burning_Loop` | #17 | 불붙은 적 발밑 | 2초 원샷. 라이트는 남으니 3초 뒤 Destroy |
| `VFX_Pierce_Beam` | #3 | 시전자, 로컬 +Z가 발사 방향 | `VfxBeam.SetLength(사거리)`. 통과한 적마다 `VFX_ManaBolt_Impact` |
| `VFX_Sand_Burst` | #4 | 착탄점(바닥) | 없음 (셰이크·히트스톱은 피드백 스크립트 이후) |
| `VFX_LevelUp` | #7 | 플레이어 발밑 | 없음 |
| `VFX_Mana_Charge` | #10 | 코어 바닥 | 루프. `VfxParticleAttractor.Target = 램프`. 충전 완료 시 Stop 후 램프에 `VFX_Mana_Charge_Complete` |
| `VFX_Mana_Charge_Complete` | #10 | 램프 | 없음 |
| `VFX_Core_Damage_Enemy` | #11 | 도착한 적 발밑 | `VfxParticleAttractor.Target = 코어`. 코어에는 `VFX_Core_Damage_Flash` |
| `VFX_Core_Damage_Flash` | #11 | 코어 바닥 (플래시는 y=1.2) | 없음 |
| `VFX_Summon_Circle` | #18 | 마법진 바닥 | 루프 |
| `VFX_Summon_Pillar` | #19 | 소환 지점 바닥 | 0.6초 원샷. 소환수 디졸브 인은 캐릭터 메시 이후 |
| `VFX_Enemy_Spawn` | #22 | 입구 바닥 | 없음 |
| `VFX_Wave_Clear` | #27 | 코어 바닥 | 없음 |
| `VFX_Torch` | #36 | 횃불 머리 | 루프. 프리팹 1개 반복 배치 |
| `VFX_Floating_Dust` | #38 | 레벨 중심 | 루프. 20×6×20 상자 안에서 부유. 씬에 1개 |

임팩트형(플래시·코어·링·파편·불씨·먼지 + 라이트)은 전부 `ImpactVfxBuilder`의 `ImpactSpec` 하나로 정의된다.
새 임팩트가 필요하면 스펙을 하나 더 만들고 `ImpactVfxBuilder.All()`에 추가하면 `Build All`·프리뷰가 같이 돈다.
빔 계열은 Hovl 원기둥 + `VfxBeam`(머티리얼 프로퍼티 블록으로 텍스처 오프셋을 밀어 스크롤)이라 커스텀 셰이더가 없다.

## 빌려 쓰는 팩 에셋

빌더가 아래 에셋을 참조하므로 두 팩을 지우면 빌드가 실패한다(어떤 파일이 없는지 예외 메시지에 나온다).

| 에셋 | 용도 |
|---|---|
| Hovl `Models/Torus1.fbx` | 충격파 링(메시 파티클). bounds로 크기·축을 맞추므로 팩 단위와 무관 |
| Hovl `Models/Crystal1.fbx` | 코어 크리스탈. 높이 1유닛으로 정규화 |
| Hovl `Models/Cylinder.fbx` | 관통 빔·소환 빛기둥. 지름 1, 길이 1(z 0→1)로 정규화한 뒤 `VfxBeam`이 스케일 |
| Hovl `Textures/MagicCircle.png` | 소환진 룬 링 → `Textures/T_VFX_MagicCircle.png`로 변환 |
| CFXR `cfxr fire small anim.png` | 3×3 8프레임 화염 플립북 → `Textures/T_VFX_FireFlipbook_3x3.png`로 변환(밝기→알파, 포스터라이즈) |
| CFXR `cfxr flamme stepped.png` | 불꽃 스프라이트(1:2) → `Textures/T_VFX_Flame_Stepped.png` |
| CFXR `cfxr smoke cloud x4.png` | 코어 위험 단계 연기, 2×2 랜덤 타일. 원본을 그대로 참조 |

## 머티리얼 규칙

머티리얼은 흰색 HDR(`M_VFX_Cube_White` 2.5, `M_VFX_Glow_White` 4.0, `M_VFX_Fire_Flipbook` 3.0 …)이고 색은 파티클 Start Color × Color over Lifetime으로 정한다.
그래서 같은 머티리얼로 런타임 틴트(적 색, 코어 안정도)가 된다. `M_VFX_Cube_Teal`만 MeshRenderer용(투사체 코어) 고정 색.

## 아직 없는 것 (계획서 §5)

카메라 셰이크·히트스톱·비네트 스크립트, Shader Graph 4종(링·디졸브·빔·히트플래시), 오브젝트 풀 `VfxService`.
