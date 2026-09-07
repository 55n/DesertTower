# DESERT TOWER 공통 전투 기반 — IDamageable

버전: 0.1
작성일: 2026-09-06
기준 문서: Desert_Tower_상세_게임기획서_v0.2 (11.1 "공통 체력·피해·사망 처리와 진영 구분")
문서 성격: 구현 전 설계 기준. 코드 없음.

---

## 1. 정의

공통 전투 기반은 인터페이스 **IDamageable** 하나로 정의된다.

IDamageable은 "피해를 받을 수 있는 것"의 계약이다. 피해를 주는 쪽은 상대가 IDamageable이라는 것만 알고 피해를 넘긴다. 피해를 받은 결과를 알고 싶은 쪽은 IDamageable의 이벤트만 구독한다.

플레이어, 적, 하수인, 벽, 공격 타워, 소환 타워, 코어는 모두 IDamageable을 구현한다.

---

## 2. IDamageable 명세

### 2.1 읽기 속성

| 이름 | 형 | 의미 |
|---|---|---|
| MaxHealth | 수 | 체력 상한 |
| CurrentHealth | 수 | 현재 체력. 0 이≤ CurrentHealth ≤ MaxHealth |
| IsAlive | 참/거짓 | 피해를 받아 상태가 바뀔 수 있는가. 거짓이면 이후 모든 피해는 거부된다 |
| Faction | 진영 식별자 | 이 개체의 진영 |
| HitPoint | 월드 위치 | 이 개체를 향한 거리·시야 판정의 기준 위치 |
| Transform | 변환 | 개체의 씬 위치. 위치 갱신·연출용 |

### 2.2 메서드

| 이름 | 입력 | 출력 | 의미 |
|---|---|---|---|
| TakeDamage | DamageInfo | DamageResult | 피해 요청을 접수해 3장의 규칙으로 처리한다 |

### 2.3 이벤트

| 이름 | 실리는 데이터 | 시점 |
|---|---|---|
| HealthChanged | 이전 체력, 현재 체력, 최대 체력 | CurrentHealth 값이 바뀔 때마다 |
| Damaged | DamageInfo, 실제 적용 피해량 | TakeDamage가 거부되지 않고 적용된 직후 |
| Died | DamageInfo (마지막 피해) | IsAlive가 참에서 거짓으로 바뀐 직후 |

이벤트를 내는 쪽은 구독자의 존재, 수, 순서를 알지 못한다. 구독자 사이의 처리 순서는 보장하지 않는다.

---

## 3. DamageInfo와 DamageResult

### 3.1 DamageInfo

| 항목 | 필수 | 의미 |
|---|---|---|
| Amount | 예 | 피해량. 0 이상 |
| SourceFaction | 예 | 가해 진영. 가해 개체가 없어도 반드시 있음 |
| Source | 아니오 | 가해 개체 참조 |
| DamageType | 아니오 | 피해 종류 식별자. IDamageable은 해석하지 않고 이벤트에 그대로 싣는다 |
| HitPosition | 아니오 | 적중 위치 |
| HitDirection | 아니오 | 적중 방향 |

### 3.2 DamageResult

| 항목 | 의미 |
|---|---|
| Applied | 피해가 적용됐는가 |
| RejectReason | 거부됐다면 이유: 사망 상태 / 비적대 / 무적 |
| AppliedAmount | 실제 깎인 체력 |
| Killed | 이 피해로 IsAlive가 거짓이 됐는가 |

---

## 4. TakeDamage 처리 규칙

1. IsAlive가 거짓이면 거부한다. 이유: 사망 상태.
2. SourceFaction이 Faction에 적대가 아니면 거부한다. 이유: 비적대. 적대 여부는 FactionProfile 자산에 묻는다.
3. 구현체가 무적 상태(부활 보호 등)이면 거부한다. 이유: 무적.
4. CurrentHealth에서 Amount를 뺀다. 0 미만은 0.
5. HealthChanged, Damaged를 순서대로 낸다.
6. CurrentHealth가 0이면 IsAlive를 거짓으로 만든 뒤 Died를 낸다.

보장 사항:

- 거부는 오류가 아니다. 예외를 던지지 않고 DamageResult로만 돌려준다.
- IsAlive의 참 → 거짓 전이와 Died는 한 번의 TakeDamage에서만 일어난다. 같은 프레임에 여러 TakeDamage가 들어와도 첫 번째로 체력을 0으로 만든 호출만 Died를 낸다.
- Died를 내기 전에 IsAlive는 이미 거짓이다. Died 구독자가 같은 개체에 TakeDamage를 호출하면 거부된다.
- 구현체는 Died를 내기 전에 자기 충돌체와 공격 기능을 꺼야 한다. Died 구독자가 개체를 조회했을 때 "더 이상 전장에 참여하지 않는 상태"를 봐야 한다.

---

## 5. 구현체

IDamageable은 계약이고, 구현은 개체 종류마다 다를 수 있다. 초기 버전은 공통 구현체 하나로 시작한다.

### 5.1 기본 구현체: Health 컴포넌트

| 항목 | 내용 |
|---|---|
| 데이터 | HealthDefinition 자산 참조 |
| 초기화 | 활성화 시 MaxHealth를 정의에서 읽고 CurrentHealth = MaxHealth, IsAlive = 참 |
| HitPoint | Transform 위치 + 정의의 오프셋 |
| Died 후 | 정의의 "사망 후 처리"에 따른다 |

### 5.2 HealthDefinition (편집 가능한 데이터)

| 항목 | 의미 |
|---|---|
| MaxHealth | |
| Faction | FactionProfile의 진영 식별자 |
| HitPointOffset | Transform 기준 피격 지점 오프셋 |
| DeathHandling | Destroy(연출 후 오브젝트 제거) / Remain(오브젝트 유지, 외부 회복 대기) |
| DeathDelay | Destroy일 때 Died 후 제거까지의 시간 |

이 자산에 넣지 않는 것: 이동 속도, 공격력, 사거리, 보상, 비용, 크기.

### 5.3 회복

IDamageable에는 회복이 없다. 회복이 필요한 개체는 별도 인터페이스를 추가로 구현한다.

| 인터페이스 | 메서드 | 대상 |
|---|---|---|
| IRepairable | Repair(양) — CurrentHealth에 더하고 MaxHealth로 상한. IsAlive가 참일 때만 | 벽, 공격 타워, 소환 타워 |
| IRevivable | Revive() — IsAlive를 참으로, CurrentHealth = MaxHealth, 무적 시간 시작 | 플레이어 |

Repair와 Revive는 HealthChanged를 낸다. Revive는 추가로 Revived 이벤트를 낸다. 허용 시점(웨이브 사이, 5초 경과)과 비용 판단은 호출자의 몫이다.

### 5.4 소속과 제거

하수인 소멸, 시설 철거, 재시작 정리처럼 피해 없이 개체를 없애는 것은 IDamageable을 거치지 않는다. 오브젝트를 직접 제거하며 Died는 나오지 않는다.

---

## 6. 호출자 규칙

TakeDamage를 호출하는 쪽(투사체, 근접 판정, 범위 마법, 타워 투사체)이 지킬 것:

- 대상을 IDamageable로만 다룬다. 구체 타입을 알지 않는다.
- 호출 전에 사거리·시야·실제 충돌을 스스로 확인한다. IDamageable은 이를 검사하지 않는다.
- SourceFaction을 반드시 채운다.
- 범위 피해는 대상마다 TakeDamage를 한 번씩 호출한다. IDamageable에 범위 개념은 없다.
- DamageResult의 거부를 오류로 다루지 않는다.

---

## 7. 어셈블리

```
DesertTower.Combat.Runtime
  ├─ IDamageable, DamageInfo, DamageResult
  ├─ IRepairable, IRevivable
  ├─ Health, HealthDefinition
  └─ FactionProfile
```

Unity 기본 타입만 참조한다. Levels.Runtime, AI Navigation, Input System을 참조하지 않는다. EditMode에서 씬 없이 검증 가능해야 한다.

---

## 8. 검증 기준

| 항목 | 기대 결과 |
|---|---|
| 체력 1인 개체에 같은 프레임 TakeDamage 3회 | Died 1회, Damaged 1회, 나머지 2회는 사망 상태로 거부 |
| 비적대 SourceFaction | 거부(비적대), 체력 불변, 이벤트 없음 |
| Amount가 체력보다 큼 | CurrentHealth 0, AppliedAmount = 이전 체력, Killed 참 |
| Died 구독자가 같은 개체에 TakeDamage | 거부(사망 상태) |
| Died 수신 시점에 개체 조회 | IsAlive 거짓, 충돌체·공격 기능 꺼짐 |
| Remain 개체 Died 후 Revive | IsAlive 참, 최대 체력, Revived 1회, 무적 시간 동안 거부(무적) |
| Destroy 개체 Died 후 DeathDelay 경과 | 오브젝트 제거 |
| Repair가 MaxHealth 초과 | MaxHealth로 상한 |
| IsAlive 거짓인 개체에 Repair | 무시 |
| DamageType·HitPosition이 있는 DamageInfo | Damaged 이벤트에 그대로 실림 |
| 직접 제거(철거·소멸) | Died 0회 |

---

## 9. 남은 결정

- 체력 자료형: 초기값은 내부 소수, 표시 정수 올림.
- DamageType 형식: 열거형 대신 자산 참조 또는 문자열로 두어 Combat 어셈블리 수정 없이 추가할 수 있게 한다.
- 무적 상태를 IDamageable 읽기 속성(IsInvulnerable)으로 노출할지. 초기값은 노출하지 않고 DamageResult의 거부 이유로만 알린다.
- Died 후 Destroy 지연 동안 HitPoint를 시야 장애물로 취급할지. 초기값은 아님.

문서 끝.
