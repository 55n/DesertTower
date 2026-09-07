# SandGuard — Tripo 입력용 A포즈 캐릭터 시트 프롬프트 v0.1

작성일: 2026-09-08
용도: Tripo Studio 이미지→3D(멀티뷰) 입력용 캐릭터 턴어라운드 시트 생성
대상 생성기: Tripo 내장 이미지 생성(Nano Banana / GPT Image), 또는 외부 이미지 생성기
콘셉트 원본: `SandGuard_Origin_of_Djinn_게임기획서_v0.5` 슬라이드 3(플레이어), 9(방어시설), 11(적), 12(보스)

---

## 0. 사용 규칙 (반드시 읽기)

1. 프롬프트 구조 = **[공통 스타일 블록] + [캐릭터 블록] + [네거티브 블록]**. 공통·네거티브는 모든 캐릭터에 동일하게 붙인다.
2. 기존 콘셉트 이미지를 **레퍼런스 이미지로 첨부**하고 생성한다(image-to-image). 텍스트만으로 만들면 캐릭터마다 스타일이 흔들린다.
3. 결과물은 **가로로 긴 한 장(정면 · 좌측면 · 후면)** 이다. 이걸 세 장의 정사각형으로 잘라 Tripo Multi-view에 front / left / back 으로 넣는다.
   - 세 장 모두 같은 픽셀 크기, 캐릭터가 중앙, 위아래 여백 10% 정도로 맞춘다.
4. 시트 생성이 잘 안 되면 **정면 한 장만** 깨끗하게 만들고 Tripo Pro의 **Single-view to Multi-view** 기능으로 측면·후면을 뽑는 쪽이 더 빠르다. 정면 프롬프트는 아래 캐릭터 블록에서 "three views" 문장만 "single front view"로 바꾸면 된다.
5. 생성 후 체크리스트(§4)를 통과한 이미지만 Tripo에 넣는다. 통과 못 하면 재생성이 크레딧을 아끼는 길이다.

---

## 1. 공통 스타일 블록 (모든 캐릭터 앞에 붙임)

```
Character turnaround reference sheet for 3D modeling.
Three full-body views of the SAME character in one horizontal row, left to right: front view, left side view, back view.
Strict A-pose: arms straight and lowered about 40 degrees from the shoulders, palms facing the thighs, fingers together, legs straight and shoulder-width apart, feet flat and pointing forward, head facing forward, neutral expression, mouth closed.
Orthographic camera at chest height, no perspective distortion. All three views have identical scale and height and stand on the same invisible ground line, evenly spaced with clear gaps between them.
Plain flat light gray background, no floor, no cast shadow, no props on the ground, no text, no labels, no arrows, no frame, no grid.
Soft even studio lighting, no rim light, no dramatic shadows.
Art style: chunky blocky low-poly stylized 3D game character, voxel-inspired faceted cube-like shapes, hand-painted flat textures with small pixel-block patches, saturated desert palette (sand beige, terracotta brown, faded red cloth, brass gold, teal crystal accents).
The whole body including clothes and held items is ONE connected solid mesh: no floating detached cubes, no particles, no glow, no transparency.
```

## 2. 네거티브 블록 (모든 캐릭터 뒤에 붙임)

```
Negative: dynamic pose, action pose, fighting stance, T-pose, crossed arms, hands on hips, bent knees, twisted torso, weapon held in front of the body, weapon crossing the body, limbs overlapping the torso, cape covering the legs, cape between the legs, perspective view, low angle, dramatic lighting, ground shadow, background scenery, desert landscape, text, watermark, logo, multiple different characters, mismatched views, mirrored duplicate, floating cubes, particles, glow, motion blur, extra limbs, extra fingers, realistic skin, photorealistic.
```

---

## 3. 캐릭터 블록

### 3.1 플레이어 — 사막 노동자 (슬라이드 3 레퍼런스 첨부)

```
Character: young adult male desert laborer, average athletic build, tan skin, short messy dark brown hair, light stubble, neutral expression.
Outfit: faded red cloth headscarf worn loosely around the neck, off-white short-sleeve tunic with torn hem and gray stitched patches, brown baggy knee-length trousers with patches, leather sandals, cloth wrist wraps on both forearms, brown leather belt and diagonal chest strap, small brass genie oil lamp hanging from the belt on the left hip, small leather coin pouch on the right hip, small sheathed knife at the back of the belt.
Hands are empty. No pickaxe, no weapon.
```

- 곡괭이는 인게임 미사용이므로 생략. 필요하면 `Hands are empty...` 문장을 아래로 교체:
  `Right hand holds a short wooden pickaxe pointing straight down along the arm, away from the body.`

### 3.2 일반 도적 — WAVE 1 (슬라이드 11 맨 왼쪽 인물 크롭 첨부)

```
Character: adult male desert bandit, lean build, tan skin, short dark hair, short beard, red cloth bandana tied on the head, red neckerchief.
Outfit: off-white torn short-sleeve tunic, brown baggy trousers, leather sandals, brown leather belt with a small pouch, cloth wrist wraps.
Right hand holds a short curved dagger pointing straight down along the arm, blade clearly separated from the leg. A small round wooden buckler shield is strapped to the outside of the left forearm, facing outward, not covering the torso.
```

### 3.3 빠른 도적 — WAVE 2 (슬라이드 11 왼쪽에서 두 번째 인물 크롭 첨부)

```
Character: young male desert bandit, slim agile build, tan skin, dark hair, red cloth headband with a long tail, red face mask covering nose and mouth.
Outfit: sleeveless dark brown vest over bare arms, red waist sash, dark baggy trousers cropped at the shin, wrapped ankles, leather sandals, small pouches on the belt.
Both hands hold a short straight dagger, each pointing straight down along the arm, blades clearly separated from the legs. No shield.
```

### 3.4 방패 도적 — WAVE 3–4 (슬라이드 11 가운데 인물 크롭 첨부)

```
Character: adult male desert bandit, stocky heavy build, tan skin, red cloth wrapped around the head and lower face, only the eyes visible.
Outfit: beige padded sleeveless tunic with gray metal plate patches, brown trousers with knee plates, leather boots, wide leather belt.
A tall rectangular wooden tower shield with gray metal bands and a red cloth strip is strapped to the outside of the left forearm, held at the side of the body facing outward, not covering the torso or legs. Right hand holds a short pickaxe pointing straight down along the arm.
```

### 3.5 철거꾼 도적 — WAVE 3–4 (슬라이드 11 오른쪽에서 두 번째 인물 크롭 첨부)

```
Character: adult male desert bandit, bulky muscular build, tan skin, thick black beard, red cloth bandana on the head, red neckerchief.
Outfit: beige short-sleeve tunic with leather shoulder straps, brown trousers with gray knee plates, leather boots, wide leather belt with several red cloth-wrapped bomb pouches and a coil of rope.
Right hand holds a large two-handed stone sledgehammer by the end of the handle, hammer head pointing straight down beside the leg, clearly separated from the body. Left hand is empty.
```

- 철거 폭탄은 별도 프리팹(큐브)으로 만들므로 손에 들리지 않게 한다.

### 3.6 보스 — 붉은 독수리단장 자히르 (슬라이드 12 첨부)

```
Character: adult male desert warlord, tall broad imposing build, tan skin, thick black beard, stern brow. Red cloth turban with a gold crown band and a small gold crest at the front, red cloth draped around the shoulders.
Outfit: gold-plated shoulder armor on both shoulders, beige tunic with a red-and-gold sash, wide leather belt with a gold buckle, brown trousers with gold-trimmed knee plates, gold-trimmed leather boots, teal crystal pendant on the chest, small pouch with teal crystals on the belt, coiled rope with a three-pronged iron grappling hook hanging at the left hip.
Short red cloth cape hanging straight down the back from the shoulders, ending at mid-thigh, hanging clear of the legs and arms, not touching the ground.
Right hand holds a large curved gold-hilted scimitar pointing straight down along the arm, blade clearly separated from the leg. A large rectangular wooden shield with gold metal trim and a red eagle emblem is strapped to the outside of the left forearm, held at the side of the body facing outward, not covering the torso.
```

- 망토는 **반드시 허벅지 중간까지만**. 콘셉트의 종아리 길이 망토는 리깅 시 다리 사이에 끼므로 금지.
- 후면 뷰에서 망토가 등 전체를 덮어도 되지만 팔과 다리는 보여야 한다.

### 3.7 해골 소환수 — 망자의 마법진 (슬라이드 9 가운데 크롭 첨부)

```
Character: humanoid skeleton warrior, small stature (about three quarters of an adult human height), ivory bone body with blocky ribcage, spine and limb bones, round skull with dark eye sockets painted flat teal (no glow), no flesh, no clothing except a tattered dark brown cloth loincloth.
Right hand holds a short rusty iron sword pointing straight down along the arm. A small round wooden buckler with a metal cross is strapped to the outside of the left forearm, facing outward.
```

### 3.8 아누비스 수호자 — 아누비스 마법진 (슬라이드 9 오른쪽 크롭 첨부)

```
Character: Anubis jackal-headed guardian, tall broad humanoid build (about one and a quarter adult human height), dark charcoal-black body with a jackal head, tall pointed ears, flat teal painted eyes (no glow).
Outfit: wide gold Egyptian collar with teal inlay, gold armbands and bracers, gold-trimmed sand-colored kilt ending above the knee, gold shin guards, bare feet with claws, gold headdress stripes.
Right hand holds a curved khopesh sword with a gold hilt pointing straight down along the arm, blade clearly separated from the leg. A tall rectangular shield with gold trim and a teal ankh emblem is strapped to the outside of the left forearm, held at the side facing outward, not covering the torso.
```

### 3.9 화염 코브라 타워 — 비휴머노이드 (슬라이드 9 왼쪽 크롭 첨부)

A포즈 개념이 없으므로 공통 블록의 A-pose 문장을 아래로 교체하고, 나머지 공통·네거티브는 그대로 쓴다.

```
Three views of the SAME statue in one horizontal row: front view, left side view, back view.
Subject: sandstone cobra statue rising straight up from a small square stepped stone base, hood fully spread and facing forward, mouth closed, flat teal painted eyes, body in a simple vertical S-curve, symmetrical, no flame, no fire, no smoke, no torches, no floating blocks. Base is part of the same solid mesh.
```

- 화염은 Unity 파티클로 처리하므로 이미지에 넣지 않는다. 입은 닫힌 상태로 만들고 필요하면 Unity 애니메이션으로 벌린다.

### 3.10 마석코어 — 정상 코어 오브젝트 (콘셉트 없음, 텍스트 생성)

공통 블록의 A-pose 문장을 아래로 교체하고, 나머지 공통·네거티브는 그대로 쓴다.

```
Three views of the SAME object in one horizontal row: front view, left side view, back view.
Subject: large teal magic crystal cluster, one tall central hexagonal crystal with four smaller crystals around it, set into a square stepped sandstone pedestal with gold trim and carved hieroglyph bands, symmetrical, crystal painted flat teal with lighter facets (no glow, no transparency), pedestal is part of the same solid mesh, no floating fragments, no floor, no torches.
```

- 발광·안정도 변화는 Unity 이미시브 머티리얼과 파티클로 처리한다.

### 3.11 생성 대상 정리

| 구분 | 에셋 | 방법 |
|---|---|---|
| Tripo | 플레이어, 도적 4종, 자히르, 해골, 아누비스, 화염 코브라, 마석코어 (총 10) | 본 문서 프롬프트 |
| Tripo (조건부) | 모래시계 오벨리스크 | 스킬트리에만 있고 MVP 시설 3종에 없음. 스코프 확정 후 생성 |
| MagicaVoxel | 마법진 받침대, 피라미드 모듈(벽·바닥·계단·기둥·횃불·입구·지름길 턱) | 그레이박스 위에 덮기 |
| 데칼/프리미티브 | 마법진 원, 슬롯 링, 철거 폭탄, 갈고리 로프(Line Renderer) | Unity 내 제작 |
| 머티리얼 변형 | 시설 강화 상태 3종 | 금 테두리·이미시브·스케일 1.1 |
| 2D | 램프 HUD 아이콘, 스킬트리 노드, 시설 아이콘, 층별 위험도 | UI |
| 파티클 | 마나탄·관통·모래 폭발·대시·화염·소환·코어 흡수 | Shuriken |

---

## 4. Tripo 투입 전 체크리스트

- [ ] 세 뷰의 키·스케일이 같고 같은 바닥선에 서 있다
- [ ] 팔이 몸통에서 떨어져 있고(A포즈), 다리가 어깨너비로 벌어져 있다
- [ ] 무기가 팔 방향으로 아래를 향하고 다리와 겹치지 않는다
- [ ] 방패가 몸 옆에서 바깥을 향하고 몸통을 가리지 않는다
- [ ] 망토가 허벅지 위에서 끝나고 다리 사이에 없다
- [ ] 배경이 단색이고 바닥 그림자·텍스트·화살표·프레임이 없다
- [ ] 떠 있는 큐브, 파티클, 발광 효과가 없다
- [ ] 캐릭터 실루엣이 덩어리진 단일 메시로 보인다(낱개 복셀 아님)
- [ ] 세 장으로 자른 뒤 픽셀 크기가 동일하고 캐릭터가 중앙에 있다

## 5. 실패 패턴별 처방

| 증상 | 프롬프트 수정 |
|---|---|
| 뷰마다 옷·색이 달라짐 | 레퍼런스 이미지 첨부 강도 상향, `the SAME character` 를 문두에 한 번 더 반복 |
| 측면 뷰가 45도 반측면으로 나옴 | `left side view` → `exact 90-degree left profile view, nose pointing left` |
| 후면 뷰가 정면을 복사함 | `back view` → `back view showing the back of the head and the shoulder blades, face not visible` |
| 무기가 몸 앞으로 옴 | `held at the side, pointing straight down, parallel to the leg, with a visible gap from the leg` 추가 |
| 낱개 복셀로 부서짐 | `voxel` 단어 삭제, `chunky faceted low-poly, smooth connected surfaces` 로 대체 |
| 그림자·바닥 생김 | `floating on a flat gray background with no ground plane` 추가 |
| 텍스트·라벨 생김 | 네거티브에 `annotations, captions, view labels` 추가 |
