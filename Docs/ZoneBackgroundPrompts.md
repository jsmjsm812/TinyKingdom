# 배경 일러스트 프롬프트 (타이틀 화면 · 로비(홈) 화면 · Zone2 · Zone3)

## 로비(홈) 화면 배경 (Home / Lobby Screen)

`Home.unity` — 타이틀에서 넘어오면 처음 보는 화면. `ZoneButtonView` 카드 3장(구역별 출전 버튼)이
세로로 가운데 쌓여서 뜨므로, **화면 중앙 세로 스트립은 카드가 덮을 걸 감안해서 저밀도로** 비워야 함
(필드 배경의 "중앙 완전 클리어" 규칙보다는 느슨해도 됨 — 카드가 불투명해서 뒤에 살짝 디테일이 있어도
괜찮지만, 카드 사이 틈에서 텍스트처럼 읽히는 디테일은 피할 것). GDD 세계관상 왕이 용병 주점에서
용사를 고용해 성벽을 지키는 설정이라, 이 화면은 **왕이 전황을 살피며 어느 전선에 용사를 보낼지
정하는 "작전 회의실/지도방"** 컨셉으로 잡음 — 카드로 고르는 3개 구역(초원/폐허/마왕의 영지)을
배경 속 지도에 은근히 암시해서 선택의 맥락을 시각적으로 연결.

```
STRICT ART STYLE — read this before anything else in the prompt:
ABSOLUTELY NO TEXT OR LETTERING ANYWHERE IN THE IMAGE — not on banners,
books, the map, signs, or any surface. Any spot that would normally carry
writing (banner cloth, book spines, the map) must be blank flat color or
a simple icon/symbol instead. This is a hard rule, not a suggestion.
2D mobile GAME MENU SCREEN ILLUSTRATION in PIXEL ART style, like Stardew
Valley or Octopath Traveler's HD-2D interiors — NOT a painting, NOT
concept art, NOT a realistic or semi-realistic render, NOT digital
painting with soft brushstrokes, NO dramatic cinematic lighting, NO soft
realistic shadows or ambient occlusion, NO painterly texture.
Instead: made of visible flat pixel blocks, FLAT cel-shaded color fills
with only 2-3 shading bands per surface (light/mid/shadow, hard edges
between them, no gradients), every object outlined with a clean thick
dark/black pixel outline, simple chibi-proportioned props (furniture,
banners, books, maps all read as cute simplified game icons, not detailed
realistic objects), bright flat readable colors, sharp pixel edges
throughout, no blur, no soft glow bloom except small flat-colored "glow"
pixel clusters (candlelight, torch flame) drawn as shapes, not soft light
bleed.

Composition: portrait canvas 1080x1920px, interior of a small stone
castle war-room seen straight-on and slightly from above (a cozy readable
angle, not a dramatic perspective),
a round wooden table sits in the lower-middle area with a hand-drawn
parchment MAP OF THE KINGDOM laid on it — the map shows three small
regions in a row, distinguished ONLY by their painted terrain color and a
tiny icon pin (a green meadow icon, a grey ruined tower icon, a small
purple spike/volcano icon) — DO NOT write any name, caption, or label of
any kind under, beside, or on top of these regions, the icons alone must
carry all the meaning, exactly like blank unlabeled markers on an old
treasure map,
stone walls on either side lined with framing decoration: a modest wooden
throne/chair pushed to one side (implying "the king's seat", simple and
not overly grand), a weapon rack with a couple of chibi swords/spears,
a bookshelf, one or two hanging banners in the kingdom's warm gold-and-
blue heraldic colors, a lit torch or two mounted on the walls (small flat
glow clusters),
a tall arched window in the upper-middle-back wall showing a small glimpse
of the green hills and distant castle wall outside (ties back to the
title screen / field background), soft warm daylight coming through it,
KEEP A WIDE VERTICAL STRIP DOWN THE CENTER OF THE CANVAS (roughly the
middle 60% width, from just below the window to the bottom edge) visually
calm and low-detail — open floor and the plain center of the table map —
this area will be covered by three stacked UI cards, avoid placing small
readable details (books, labels, patterns) directly behind this strip,
keep the interesting framing detail pushed toward the left/right thirds
and the window/upper area instead,
KEEP THE TOP 12% OF THE CANVAS mostly open wall/window — this area will
be covered by a title bar and currency HUD.
Lighting: warm indoor torch-and-window daylight mix, cozy and inviting,
flat even lighting with no dark corners or vignette — this is the
player's safe home base, it should feel warm and welcoming, not tense.
Palette: warm stone-grey walls + rich wood-brown furniture + gold and
royal-blue heraldic banner accents + warm candlelight-orange glow pops —
same overall warmth as the title screen, a calmer "indoors" version of
it.
Seamless single illustration (not a tileset), no readable text anywhere
— the map regions are distinguished by color and icon alone, with
nothing written under or near them — no character sprites, no UI
elements baked in.
```

## 타이틀 화면 배경 (Title Screen)

필드 배경(`FieldBackground.png` 등)과 달리 게임플레이 중 UI 슬롯이 위에 올라가지 않으므로 중앙을
비울 필요는 없음. 대신 로고와 시작 버튼이 들어갈 **상단·하단 안전 여백**을 남겨야 함. 톤은 Zone1과
같은 밝고 아기자기한 느낌을 유지하되, "몬스터 군단이 몰려오지만 왕국은 아직 지켜낼 수 있다"는
설렘 섞인 위기감을 배경 스토리텔링으로 담음(GDD 세계관: 왕이 용병을 고용해 성벽을 지키는 이야기).

```
STRICT ART STYLE — read this before anything else in the prompt:
ABSOLUTELY NO TEXT OR LETTERING ANYWHERE IN THE IMAGE — not on banners,
towers, signs, or any surface. Any spot that would normally carry
writing must be blank flat color or a simple icon/symbol instead. This
is a hard rule, not a suggestion.
2D mobile GAME TITLE SCREEN ILLUSTRATION in PIXEL ART style, like Stardew
Valley or Octopath Traveler's HD-2D key art — NOT a painting, NOT concept
art, NOT a realistic or semi-realistic render, NOT digital painting with
soft brushstrokes, NO dramatic cinematic lighting, NO soft realistic
shadows or ambient occlusion, NO painterly texture.
Instead: made of visible flat pixel blocks, FLAT cel-shaded color fills
with only 2-3 shading bands per surface (light/mid/shadow, hard edges
between them, no gradients), every object outlined with a clean thick
dark/black pixel outline, simple chibi-proportioned characters and props
(soldiers, banners, towers all read as cute simplified game icons, not
detailed realistic figures), bright flat readable colors, sharp pixel
edges throughout, no blur, no soft glow bloom except small flat-colored
"glow" pixel clusters (drawn as shapes, not soft light bleed).

Composition: portrait canvas 1080x1920px, wide panoramic establishing
shot of a small medieval kingdom seen from just outside its walls,
a sturdy stone castle with a raised drawbridge and banner-topped towers
sits in the upper-middle third against a warm sky (sunset gold-to-blue
gradient, but rendered as flat pixel color BANDS, not a soft gradient
blur),
a low defensive wall with a handful of small chibi guardian-soldier
silhouettes standing atop it facing outward (spear + shield poses,
simple flat-colored, reads as "the last line of defense" without being
grim),
the winding dirt path from the battle field leads from the wall down
toward the bottom of the frame,
on the horizon at the edges (left and right, not blocking the castle),
a distant silhouette of an approaching monster horde — goblins and
skeletons as small flat dark-silhouette shapes with a couple of glowing
red eye-dots, readable as "coming soon" not "already here", kept small
and pushed to the far background so they read as narrative texture, not
a threat in your face,
healthy green hills and a few trees flank the wall on both sides (same
grass-tuft icon style as the Zone1 meadow reference),
KEEP THE BOTTOM 25% OF THE CANVAS visually calm and low-detail (soft
open ground/path, no busy silhouettes) — this area will be covered by
UI buttons,
KEEP THE TOP 15% OF THE CANVAS mostly open sky — this area will be
covered by the game logo,
Lighting: warm late-afternoon flat lighting, golden highlight from the
upper-left, still bright and inviting overall — this must read as
"exciting adventure begins," not "dark ominous siege."
Palette: warm gold/amber sky + healthy grass-green hills + warm stone-tan
castle walls + small pops of banner-red and torch-orange — same overall
warmth and cheerfulness as the Zone1 meadow reference, the distant horde
silhouettes are the only cool/dark note in the whole image.
Seamless single illustration (not a tileset), no readable text, no UI
elements baked in.
```

## ⚠️ 1차 시도 실패 원인

1차 프롬프트로 나온 결과물이 픽셀아트가 아니라 **어둡고 회화적인(painterly) 반사실적 다크판타지
컨셉아트**로 나왔음 — 부드러운 그라데이션 명암, 사실적 조명/그림자, 붓터치 느낌. 기존
`FieldBackground.png`(TinyKingdom 픽셀아트 — 또렷한 셀셰이딩, 평평한 색면, 두꺼운 검은
테두리, 귀여운 chibi 톤)와 그림체 자체가 완전히 다름.

원인: "detailed anime-tinged pixel art" 정도의 지시로는 약했다 — "ruined," "volcanic,"
"obsidian," "lava," "corruption" 같은 다크판타지 소재 단어들이 대부분의 이미지 생성 모델에서
회화체 컨셉아트 쪽으로 강하게 끌어당기는 경향이 있어서, 픽셀아트 지정을 훨씬 더 명시적이고
반복적으로 박아야 함. 아래는 그렇게 다시 짠 버전.

**이미지 첨부 가능한 도구라면 `FieldBackground.png`를 레퍼런스로 반드시 첨부할 것** — 텍스트만으론
또 빗나갈 위험이 큼. 이미지 첨부가 안 되는 도구라면 아래처럼 그림체를 훨씬 구체적으로 못박은
텍스트만으로 시도.

## 공통 스펙 (두 프롬프트 뒤에 그대로 붙여 쓸 것)

```
STRICT ART STYLE — read this before anything else in the prompt:
ABSOLUTELY NO TEXT OR LETTERING ANYWHERE IN THE IMAGE — not on banners,
statues, crates, or any surface. Any spot that would normally carry
writing must be blank flat color or a simple icon/symbol instead. This
is a hard rule, not a suggestion.
2D top-down mobile GAME ASSET in PIXEL ART style, like Stardew Valley or
Octopath Traveler's HD-2D top-down maps — NOT a painting, NOT concept art,
NOT a realistic or semi-realistic render, NOT digital painting with soft
brushstrokes, NO dramatic cinematic lighting, NO soft realistic shadows or
ambient occlusion, NO painterly texture.
Instead: made of visible flat pixel blocks, FLAT cel-shaded color fills
with only 2-3 shading bands per surface (light/mid/shadow, hard edges
between them, no gradients), every object outlined with a clean thick
dark/black pixel outline, simple chibi-proportioned props (crates, barrels,
banners, statues all read as cute simplified game icons, not detailed
sculptures), bright flat readable colors even in dark scenes (moody through
COLOR CHOICE and PALETTE, never through realistic shadow rendering or
gradient blur), sharp pixel edges throughout, no blur, no soft glow bloom
except small flat-colored "glow" pixel clusters (drawn as shapes, not as
soft light bleed).

Composition: portrait canvas 1080x1920px, symmetrical vertical layout,
a large open clear central area (flat ground, completely empty of large
objects — this is where UI slots and a winding path get overlaid
in-engine, keep the center uncluttered and readable, no cast shadows or
vignette darkening the center),
dense decorative framing along all four edges only (pushing inward from
the border, tapering off well before the center),
a small pair of matching banner-post decorations on the left and right
edges (mirrored placement),
a couple of crate/barrel clusters tucked into corners,
one small distinct landmark structure anchored at the bottom-left corner,
one hanging lantern near the bottom-right edge,
scattered small rocks/detail sprites drifting into the open center for
texture without blocking readability,
flat even lighting across the whole scene (a hint of warm light from
upper-left is fine, but the center must stay just as bright/readable as
the edges — no vignette, no darkening toward center or corners),
seamless single illustration (not a tileset), no character sprites, no UI
```

## 존2 — 폐허 성채 (Ruined Citadel)

존1(밝은 초원)과 존3(가장 어두운 최종 구역) 사이 중간 톤 — 폐허지만 대낮이고 생기가 남아있음.

```
[공통 스펙]
Theme: a sunlit ruined citadel courtyard being gently reclaimed by nature.
Ground: warm sandy-grey flat-shaded stone tiles, cracked but intact, with
healthy flat-green grass-tuft pixel clusters and moss growing through the
cracks (same simple grass-tuft icon style as the reference meadow, just
sparser and poking through stone instead of covering it fully).
Border: weathered stone walls and columns MIXED with some surviving green
trees and ivy (not pure grey rubble — keep it visibly alive and green),
a couple of mossy statues in the corners (simplified chibi-statue icons,
weathered but still standing, not shattered),
banner posts faded but upright (muted teal-grey flat-colored banner cloth),
bottom-left landmark: a partially-collapsed stone watchtower, still mostly
standing (roof half-caved, walls intact) — same simple building-icon style
as the reference windmill cottage.
Lighting: bright flat daytime, same warm upper-left highlight as the
reference — this must read as clearly DAYLIT, not dusk or gloomy.
Palette: warm stone-tan + moss-green, a notch more muted than Zone1's
vivid grass but still bright and cheerful-adjacent — a "quiet overgrown
ruin," not a "grim dead ruin." No fog, no haze, no darkened corners.
```

## 존3 — 마왕의 영지 (Demon Lord's Domain)

1차 시도가 "어둡게"를 너무 문자 그대로 받아들여서 전체 화면이 거의 안 보일 정도로 캄캄하게
나왔음(시인성 실패 — 게임 UI가 이 위에 슬롯/캐릭터를 올려야 하는데 배경이 안 보이면 안 됨).
**어두운 "느낌"은 팔레트(보라/진홍 vs 초록)로만 내고, 실제 밝기(명도)는 존1과 비슷한 수준으로
유지**하도록 명도 하한선을 명시적으로 못박는다.

```
[공통 스펙]
Theme: a corrupted volcanic wasteland at the demon lord's domain — still
must read as clean FLAT-SHADED PIXEL ART, with a dark-purple/crimson
palette instead of green. IMPORTANT — READABILITY OVER DARKNESS: the
"evil" mood must come entirely from HUE CHOICE (purple/crimson vs green),
NOT from making the image actually dark or low-contrast. The overall
brightness/value level of this image must be roughly comparable to the
Zone1 meadow reference (a mid-to-bright value range) — imagine dusk
twilight or a lit volcanic cavern, never pitch-black or underexposed.
Every element must stay clearly legible at a glance, especially the open
center area which needs to read as clearly as the Zone1 grass does.
Ground: a medium-value dark plum-purple stone (NOT near-black, NOT deep
grey-black — think "dark eggplant purple," clearly a colored surface, not
absence of light), flat-shaded with visible tile texture, veined with
bright glowing magenta-pink crack lines (drawn as crisp bright flat-color
line shapes, high contrast against the plum ground so they pop clearly).
Border: jagged rock spike silhouettes and bare dead tree silhouettes in a
medium-dark warm charcoal-purple (not solid black silhouettes — keep some
readable form/color in them) with thick clean outlines, small flat-shaded
bright orange-red lava-crack accents in the corners for warm contrast pops,
banner posts of dark iron with a flat bright crimson-red banner (saturated
red, not muddy dark red, so it reads clearly),
bottom-left landmark: a compact dark stone altar/throne fragment with a
few steps, a clearly-glowing bright purple rune accent on it (simple
building-icon style, same scale/simplicity as the reference windmill
cottage),
the lantern replaced with a small bright flat-colored floating purple
flame icon (glows clearly, reads instantly against the ground).
Lighting: flat and even across the whole scene, NO dramatic spotlighting,
NO vignette, NO dark corners or dark center — every corner of the canvas
should be as easy to read as the middle.
Palette: dark plum-purple base (not black) + bright magenta-pink crack
accents + bright crimson-red banner/warm accents + orange-red ember pops —
high contrast between base and accent colors so the whole thing reads
instantly even though the hue is "evil." Darkest of the three zones by
HUE/mood only, not by actual brightness — should NOT look dimmer or
harder to read than Zone1 or Zone2 at a glance.
```

## 적용 순서 (참고 — 존1과 동일한 절차, 코드에 zone별 배경 스위치가 아직 없음에 주의)

1. 생성된 두 장을 각각 `Assets/03_Art/Sprites/Zone2Background.png`, `Zone3Background.png`로 저장
   (해상도는 `FieldBackground.png`와 맞추기 — 대략 1080x1920 비율)
2. **현재 `ShopRosterUIBootstrap.AddFieldBackground()`는 `FieldBackgroundIllustrationPath` 상수 하나만
   보고 항상 같은 배경을 씀 — 구역별로 자동으로 안 바뀐다.** 실제로 3구역이 각자 다른 배경으로
   보이게 하려면 별도 작업이 필요함:
   - `ZoneData`에 `public Sprite fieldBackground;` 필드 추가
   - 배틀 씬 로드 시(예: `WaveSpawner.Awake` 근처, `RunSession.SelectedZone` 읽는 곳) 그 구역의
     `fieldBackground`를 `FieldBackground` Image에 런타임으로 대입하는 코드 추가
   - 세 구역 에셋(`Zone1_FrontGate`/`Zone2_Library`/`Zone3_Greenhouse` — 참고: 폴더명은 아직 예전
     구역명 그대로라 실제 테마명과 안 맞음, 확인 필요)에 각각 이미지 연결

이 스위치 로직은 이번 세션에서 아직 안 만들었으니, 그림 두 장 받으시면 말씀해주세요 — 바로 붙여드릴게요.
