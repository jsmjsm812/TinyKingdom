# 캐릭터 스프라이트 시트 프롬프트 (수호자 10 + 침입자 8)

초상화(상점/명부용, 이미 있음)와 **같은 아트 컨셉**으로 실제 필드에서 쓰일 배틀 스프라이트를 만든다.
(이전 버전은 배경 타일셋 톤에 맞춘 단순 레트로 픽셀아트로 잘못 설계되어 있었음 — 전면 수정)

## ⚠️ 실제 초상화를 직접 확인하고 수정한 내용

`Lumi.png`, `WarpedCrow.png` 두 장을 직접 열어서 확인한 결과, 실제 아트 스타일은:

- **단순/블록형 레트로 픽셀아트가 아니라**, 고디테일 애니메이션(anime) 톤의 "모바일 가챠 게임" 픽셀아트에 가까움
- 부드러운 셀셰이딩 + 그라데이션, 잔잔한 픽셀 그레인(선명한 8x8 블록이 아님), 깔끔하지만 섬세한 다크 아웃라인
- 마법/이펙트 부분에 은은한 글로우·블룸이 들어감 (WarpedCrow의 보라색 균열 발광 등)
- 배경은 완전 투명

기존 프롬프트 파일의 "공통 스펙"과 "루미" 항목이 이 실제 스타일과 맞지 않아서 (예: 루미 실제 헤어는 하늘색 트윈테일인데 기존 프롬프트엔 "금발 단발"로 잘못 적혀 있었음) 전면 재작성함.

## 🔴 애니메이션 구성 변경: walk 삭제

기존: 침입자 = walk(4)+death(4). 변경 후: **모든 캐릭터가 idle + 액션 2가지 상태만 사용.**

- **수호자 (10종)**: idle(4) + **attack**(4) — 필드에 고정 배치되어 공격만 하므로 기존과 동일
- **침입자 (8종)**: idle(4) + **death**(4) — 이동은 코드에서 위치 보간(position lerp)으로 처리되고 다리를 움직이는 걷기 모션이 필요 없으므로, "걷기"가 아니라 "제자리에서 살짝 흔들리는/부유하는 idle 루프"를 이동 중 계속 재생. 맞아 죽을 때만 death 재생.
  - 침입자는 실제로 "공격" 모션이 필요 없음(수호물에 닿으면 코드상 즉시 피해 판정, 별도 타격 모션 없음) → attack 대신 death가 맞다고 판단해서 이렇게 정리함. **만약 침입자도 도착 시 별도의 돌진/타격 연출을 원하면 알려주면 attack 프레임을 추가로 설계할게.**

## 공통 스펙 (모든 프롬프트 뒤에 그대로 붙여 쓸 것)

```
2D character sprite, transparent background, full-body dynamic pose (not a bust/portrait crop),
detailed anime-style pixel art, modern mobile gacha-game aesthetic —
NOT simple/blocky retro pixel art, NOT flat 8-bit style,
fine pixel-grain texture with soft cel-shaded coloring, smooth gradient shading,
clean but delicate dark outline, soft rim lighting from upper-left,
subtle glow/bloom on any magical or corrupted-energy effects,
same exact hairstyle / outfit / palette / color scheme as the character's existing portrait artwork,
frame canvas 512x512px, frames arranged in an even grid with consistent character scale and ground line across all frames,
no baked-in drop shadow (added separately in-engine)
```

**강력 권장**: 텍스트만으로 생성하면 이번처럼 헤어색/의상이 실제 초상화와 어긋날 위험이 크다.
가능하면 `Assets/03_Art/Sprites/Guardians/이름.png` / `Invaders/이름.png` 파일을 **레퍼런스 이미지로 함께 첨부**해서
"이 캐릭터와 동일한 디자인으로 전신 포즈를 그려줘" 식으로 이미지 참조 생성을 쓸 것. 텍스트 설명은 보조 수단으로만 사용.

## 등급별 시각 규칙 (수호자 전용, 기존과 동일)

- **★1**: 단색 위주, 이펙트 없음, 실루엣이 단순함
- **★2**: 주 색상 + 보조 강조색 1개, 공격 시 옅은 발광/파티클 1~2개
- **★3**: 화려한 팔레트(주색+보조색+금색 트림), 공격 시 뚜렷한 발광 이펙트, 망토/리본 등 흩날리는 디테일 추가

## 시트 레이아웃

### 수호자 (10종)
```
Sheet size: 2048x1024px (4 columns x 2 rows, 512x512 per cell)
Row 1 (idle, 4 frames): gentle breathing/floating loop, weapon at rest
Row 2 (attack, 4 frames): wind-up -> release -> follow-through -> recover
```

### 침입자 (8종)
```
Sheet size: 2048x1024px (4 columns x 2 rows, 512x512 per cell)
Row 1 (idle, 4 frames): subtle in-place sway/bob/breathing loop (used continuously while it
  slides along the path in-engine — no leg-walk-cycle needed)
Row 2 (death, 4 frames): stagger -> collapse -> dissolve into dark/purple particles -> gone
```

---

## 수호자 (10종)

### 1. 루미 (Lumi) — ★1, 단일 원거리, 빛 속성
```
[공통 스펙] + [★1 규칙]
A small academy witch girl guardian spirit, light cyan/pale-blue hair styled in
loose wavy twin low pigtails, black pointed witch hat with a blue ribbon bow on the brim,
dark navy academy blazer with a light-blue necktie, white collar, and a small silver
shield-crest pin on the lapel, sparkly dark-blue eyes, holding a tiny glowing wand,
soft warm-golden-to-cool-blue light motif, cheerful beginner-mage design
(match exact look of Lumi.png portrait)
```

### 2. 노아 (Noa) — ★1, 단일 원거리 + 치명타, 정밀함
```
[공통 스펙] + [★1 규칙]
A small academy witch girl guardian spirit, dark green hair in a long braid,
forest-green archer-style academy uniform with a small quiver, holding a compact bow,
focused sharpshooter design, sharp narrowed eyes
(match exact look of Noa.png portrait)
```

### 3. 미라 (Mira) — ★1, 다중(2체) 공격, 쌍둥이/거울 속성
```
[공통 스펙] + [★1 규칙]
A small academy witch girl guardian spirit with a symmetrical twin-tails hairstyle,
pale lavender academy uniform, dual-wielding two small matching wands,
mirror/twin motif, both hands active and balanced pose
(match exact look of Mira.png portrait)
```

### 4. 도리 (Dori) — ★1, 단일 근접 + 기절, 우직함
```
[공통 스펙] + [★1 규칙]
A small sturdy academy guardian girl with short orange hair,
simple brown work-uniform, wielding a small round-headed mallet,
stout cheerful bruiser design, wide stable stance
(match exact look of Dori.png portrait)
```

### 5. 세라 (Sera) — ★2, 범위 + 슬로우, 얼음 속성
```
[공통 스펙] + [★2 규칙]
An academy witch girl guardian spirit, teal hair, pale-blue and white uniform
trimmed with icy-cyan accents, holding a small frost staff with a snowflake tip,
faint cyan glow around her feet, cold-mist particle motif
(match exact look of Sera.png portrait)
```

### 6. 이레네 (Irene) — ★2, 관통(직선), 창/화살 속성
```
[공통 스펙] + [★2 규칙]
An academy witch girl guardian spirit, long navy wavy hair,
navy-and-silver uniform, holding a slender glowing spear held level for a thrust,
straight-line piercing light motif along the spear
(match exact look of Irene.png portrait)
```

### 7. 클로에 (Chloe) — ★2, 단일 + 도트, 독/가시 속성
```
[공통 스펙] + [★2 규칙]
An academy witch girl guardian spirit, short purple hair,
dark-green uniform with thorny vine trim, holding a small vial of glowing green poison,
faint sickly-green particle motif, mischievous expression
(match exact look of Chloe.png portrait)
```

### 8. 아스텔 (Astel) — ★3, 범위(광역) + 속박, 대마법사 속성
```
[공통 스펙] + [★3 규칙]
A tall elegant academy witch guardian spirit wearing a purple pointed witch hat,
deep-purple and gold uniform with a flowing cape, holding an ornate long staff
with a glowing amethyst orb, arcane binding-rune motif swirling at her feet
(match exact look of Astel.png portrait)
```

### 9. 셀레네 (Selene) — ★3, 관통+슬로우, 달빛 속성
```
[공통 스펙] + [★3 규칙]
An elegant academy witch guardian spirit, long silver hair,
midnight-blue uniform with silver crescent-moon trim and a flowing cape,
holding a crescent-bladed staff, pale moonlight glow motif, serene expression
(match exact look of Selene.png portrait)
```

### 10. 오필리아 (Ophelia) — ★3, 버프, 축복/온기 속성
```
[공통 스펙] + [★3 규칙]
An elegant academy witch guardian spirit, warm auburn hair in a bun,
cream-and-gold uniform with a flowing cape, holding a small glowing lantern staff,
warm golden aura motif radiating outward (buff/support feel), gentle smile
(match exact look of Ophelia.png portrait)
```

---

## 침입자 (8종)

공통 컨셉: "정체불명 괴물이 아니라, 원래 학교를 지키던 수호수가 마력에 잠식된 모습" —
평범한 동물 실루엣은 그대로 유지하고, 균열/보라빛 발광/일그러짐으로 "잠식"을 표현.

### 1. 일그러진 까마귀 (Warped Crow) — 일반
```
[공통 스펙, 침입자 레이아웃]
A corrupted crow, dark slate-black feathers with glowing purple crack veins across
its wings and body, one eye a glowing magenta orb, slightly twisted/asymmetric
feather tufts, broad hunched wing silhouette
(match exact look of WarpedCrow.png portrait)
```

### 2. 일그러진 쥐 (Warped Rat) — 일반
```
[공통 스펙, 침입자 레이아웃]
A corrupted rat, dark grey fur with thin glowing purple cracks along its back,
faint magenta glow in its eyes, small hunched fast-scurrying silhouette
(match exact look of WarpedRat.png portrait)
```

### 3. 일그러진 뱀 (Warped Snake) — 일반
```
[공통 스펙, 침입자 레이아웃]
A corrupted snake, dark green scales with glowing purple crack patterns running
down its length, faint magenta glowing eyes, coiled side-view pose
(idle = slow slither/coil sway instead of legs)
(match exact look of WarpedSnake.png portrait)
```

### 4. 일그러진 멧돼지 (Warped Boar) — 일반(탱커)
```
[공통 스펙, 침입자 레이아웃]
A corrupted wild boar, bulky dark-brown body with glowing purple cracks across
its hide, cracked tusks glowing faint magenta, heavy sturdy tank silhouette
(match exact look of WarpedBoar.png portrait)
```

### 5. 미쳐버린 늑대 (Rabid Wolf) — 정예
```
[공통 스펙, 침입자 레이아웃]
A corrupted wolf, dark grey fur with more pronounced glowing purple cracks and
faint dark-purple smoke wisps trailing off its back, wild glowing magenta eyes,
sleek fast aggressive silhouette (elite tier — slightly more detailed than normal tier)
(match exact look of RabidWolf.png portrait)
```

### 6. 태고의 매 (Primordial Hawk) — 1구역 보스
```
[공통 스펙, 침입자 레이아웃, 보스급 — 더 크고 디테일 많게]
An ancient corrupted hawk spirit, large wingspan, dark bronze-brown feathers
with heavy glowing purple crack patterns and small drifting magenta embers,
fierce glowing eyes, majestic but clearly corrupted, larger and more imposing
silhouette than normal-tier invaders
(match exact look of PrimordialHawk.png portrait)
```

### 7. 태고의 곰 (Primordial Bear) — 2구역 보스
```
[공통 스펙, 침입자 레이아웃, 보스급 — 더 크고 디테일 많게]
An ancient corrupted bear spirit, massive dark-brown body with deep glowing
purple crack patterns across its chest and claws, heavy slow imposing stance,
faint dark-purple mist pooling around its feet, large intimidating silhouette
(match exact look of PrimordialBear.png portrait)
```

### 8. 태고의 산군 (Primordial Tiger) — 3구역 보스
```
[공통 스펙, 침입자 레이아웃, 보스급 — 더 크고 디테일 많게]
An ancient corrupted tiger spirit, dark orange-and-black striped fur overlaid
with intense glowing purple crack patterns, glowing magenta eyes, sleek powerful
predator stance, faint purple flame-like aura along its back — the most
elaborate and threatening of the three bosses
(match exact look of PrimordialTiger.png portrait)
```

---

## Unity 적용 순서 (참고)

1. 생성된 시트 PNG를 `Assets/03_Art/Sprites/Guardians(또는 Invaders)/Battle/이름_Sheet.png`로 저장
2. Texture Type = Sprite (2D and UI), Sprite Mode = Multiple, Filter Mode = Point (또는 Bilinear — 초상화가 부드러운 셰이딩이라 Bilinear가 더 잘 맞을 수 있음), Pixels Per Unit은 512로
3. Sprite Editor → Slice → Grid By Cell Size (512x512)로 8프레임 분할 (idle 4 + attack/death 4)
4. `FireBoltProjectile.cs`와 같은 패턴으로 프레임 배열을 순환시키는 가벼운 컴포넌트를 새로 만들어서
   `GuardianUnit`(idle↔attack 전환)과 `InvaderUnit`(idle↔death 전환)에 연결
