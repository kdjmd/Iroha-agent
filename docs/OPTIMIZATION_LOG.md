# Optimization Log

## 2026-07-08 First Continuous Pass

Saved latest desktop package:

```text
Desktop\IrohaAgent-Latest.zip
Desktop\IrohaAgent-Source-Latest.zip
```

Completed:

- Added fixed save script: `tools\save-latest-desktop.ps1`
- Rewrote README for Git sharing
- Expanded `.gitignore` for toolchains, build outputs, archives and temporary files
- Added long-term local memory: `%APPDATA%\IrohaLocalAgent\memory.json`
- Added memory toggle in settings
- Added local prompt optimization toggle: `省 token`
- Added chat save and current conversation clear actions
- Expanded character system prompt with a safer high-level Iroha-style companion persona
- Added new avatar moods: `shy`, `surprised`, `cheer`, `focus`
- Generated 32 extra desktop animation frames
- Reduced desktop animation frame storage from about 27.7 MB to 5.46 MB
- Reduced latest Windows package to about 5.6 MB
- Added a custom hero header panel for a more character-focused first impression
- Verified latest desktop package starts without new crash logs

## 2026-07-09 Second Pass

Saved latest desktop package:

```text
Desktop\IrohaAgent-Latest.zip
Desktop\IrohaAgent-Source-Latest.zip
```

Completed:

- Added `记忆管理` window with view, add, delete, clear and save actions
- Added selected-memory rewrite support in `记忆管理`
- Added four quick companion scenarios: `陪我聊`, `做计划`, `找灵感`, `复盘`
- Added main-screen memory count and token optimization status hint
- Desktop runtime package now includes `README.md` and `OPTIMIZATION_LOG.md`
- Updated persona prompt with explicit mood selection rules for richer animation triggers
- Rebuilt and verified latest desktop package startup without new crash logs
- Latest Windows package remains about 5.6 MB with 70 animation frames

Next candidates:

- Add a more polished custom chat bubble renderer instead of `RichTextBox`
- Add a more polished visual style for memory and settings dialogs
- Add quick action chips for entertainment companionship scenarios
- Add more body-motion animation variants if package size remains acceptable
- Consider Android source/package refresh after desktop experience stabilizes

## 2026-07-09 Reference UI Pass

Completed:

- Reworked the desktop shell toward the provided reference image:
  - top product/status bar
  - left conversation sidebar
  - central visual-novel stage
  - floating memory/compression/service cards
  - bottom dialogue and quick action area
  - right-side vertical tool strip
- Added a lightweight illustrated room background for the central stage
- Switched avatar display to immersive mode so the standing portrait blends into the scene
- Fixed rounded-panel black corner artifacts
- Rebuilt and verified the app starts without new crash logs

## 2026-07-10 Engineering Acceptance Pass

Completed:

- Added the 25-page `彩叶_Iroha_Agent_工程验收与交接手册.docx` and its maintainable Markdown source.
- Documented Windows and Android scope separately; Windows is recommended for conditional acceptance, while Android 0.1.0 remains a prototype.
- Added executable acceptance cases, fault-recovery checks, security and copyright risks, technical debt, expansion interfaces, handover checklist, deviation log and signature page.
- Added three project-local evidence screenshots under `docs\evidence` for standalone, compact-window and settings verification.
- Fixed `desktop\build.ps1` so the standalone package includes the top-level high-resolution `assets\character\iroha-portrait.png`, not only animation frames.
- Rebuilt Windows and verified the package from a source-independent temporary directory.
- Rebuilt Android `:app:assembleDebug` successfully with 31 tasks up-to-date; mobile parity remains explicitly out of scope for this baseline.
- Verified the engineering handbook at 25 rendered pages with no clipping or overlap.
- Passed document accessibility audit with 0 high, 0 medium and 0 low findings.
- Passed exact geometry audit for all 27 Word tables and scanned the handbook for credential-shaped strings with 0 findings.
- Recorded baseline SHA-256 values in the handbook:
  - `IrohaAgent.exe`: `D17B32BC57E20C66EF3B820D95E4F4DBB9E8C49A44536EE1B2CDFD2C63CD6624`
  - `app-debug.apk`: `3DDFE85BE6DFE789F901A22EAEC73DFE2E490DE559AD8D4AC1ED2EA12D00CA68`

## 2026-07-10 Final VN Polish Pass

Completed:

- Re-captured the current standalone build before editing and compared it region-by-region with the visual-novel reference.
- Replaced the three-capsule brand mark with a five-petal crystalline leaf drawn at runtime and corrected the title spacing.
- Removed persistent circles from settings, minimize, maximize and close; the title bar now draws clean line icons with hover/press feedback.
- Preserved real settings, minimize, maximize and close hit areas; all four actions passed click regression.
- Added a functional search glyph inside the conversation search field.
- Added restrained sakura petals, crystal guide lines and soft sparkles to the VN dialogue panel without covering text.
- Re-verified the main screen at 1280 x 720, compact layout at 980 x 552 and the settings drawer.
- Updated all three project-local evidence screenshots and refreshed the Windows executable baseline hash.

## 2026-07-10 Dynamic Model And Fidelity Pass

Completed:

- Re-captured the packaged application before editing and compared the 1280 x 720 result against `彩叶视觉小说聊天界面.png` zone by zone.
- Replaced the active conversation portrait with an exact runtime crop of the official real-world Iroha character art; other conversations continue to rotate official work characters.
- Replaced the title-bar text bullet with a semantic green online indicator and compact chevron.
- Made the model badge reflect the configured DeepSeek model: `deepseek-v4-flash` renders `Flash`, while `deepseek-v4-pro` renders `Pro`.
- Added a Flash/Pro model selector to advanced settings and removed the old behavior that always reset saved settings to Flash.
- Replaced the send glyph with a dedicated owner-drawn paper plane, expanded the VN dialogue panel's lower breathing room, and moved decorative guide lines outside the primary text area.
- Added a subtle horizontal depth wash and top highlight to the VN dialogue glass while retaining crisp foreground text.
- Verified Flash and Pro badges independently, verified the advanced selector saves `deepseek-v4-pro`, and restored the user's original Flash setting after the test.
- Re-verified the standard view, compact view, settings drawer, advanced settings render, executable liveness and crash-log timestamp.

## 2026-07-10 Interior Rhythm And Interaction Pass

Completed:

- Captured a new runtime baseline and normalized the 1672 x 941 reference to the application's 1280 x 720 acceptance viewport.
- Added full-view and focused side-by-side comparison evidence for the left information zone and bottom interaction zone.
- Removed nested white pills from inactive conversations while preserving distinct active, hover and pressed animation states.
- Corrected conversation avatar scale, explicit left alignment and text spacing to match the reference sidebar rhythm.
- Rebalanced the three information cards: tighter icon/title spacing, wider body copy, source-matched heights and a less compressed service status row.
- Narrowed the VN nameplate, corrected dialogue typography and composer inset, and kept all decorative lines outside readable text.
- Removed the enclosing quick-action strip and restored the reference's two-line labels without changing click behavior.
- Reflowed the text-only voice dock into the space formerly reserved for microphone input, eliminating the unexplained bottom gap.
- Increased sidebar and information-card glass opacity slightly so sharp background shelves no longer compete with copy.
- Matched the owner-drawn outer bounds of the sidebar, search field, new-chat control and three information cards after accounting for the glass renderer's five-pixel inset.
- Re-captured and verified the main 1280 x 720 view, compact 980 x 552 view and real runtime settings drawer.
- Verified quick-action click filling, Flash/Pro badge mapping, settings visibility, conversation editing handlers and unchanged crash-log timestamp.

## 2026-07-11 Companion Card And Avatar Cohesion Pass

Completed:

- Captured a fresh 1280 x 720 runtime baseline before editing and compared it with the normalized visual-novel reference in full, left-zone and bottom-zone views.
- Confirmed the conversation rotation uses character thumbnails from the work's official site rather than unrelated placeholder portraits.
- Unified conversation-avatar sampling, inner highlight and cyan ring treatment while preserving the active real-world Iroha crop and official character artwork.
- Rebuilt the lower-left companion card as an integrated glass illustration panel with a smaller Q-version portrait, caption band, subtle shadow and speaker feedback.
- Added bounded hover animation to the companion card; portrait scale and speaker glow animate without resizing the control or moving adjacent copy.
- Iterated through three captured passes to correct the initial over-cropped face and match the reference card's character scale and breathing room.
- Re-captured the final 1280 x 720 view, compact 980 x 552 layout, settings drawer and companion hover state with no text clipping or control overlap.
- Refreshed the Windows executable baseline to `337,408` bytes with SHA-256 `C40309324E827CC0403FC3F5913CE45E8871CD575DD65B6129BFBAB72F503CDF`.

## 2026-07-11 Natural Expression Animation Pass

Completed:

- Re-captured the compiled application and compared its character stage with the normalized visual-novel reference before changing animation behavior.
- Enlarged and repositioned the high-resolution portrait so the head, raised hand and Galgame-style lower crop align more closely with the reference.
- Replaced the rejected geometric eye and mouth experiment with four natural 887 x 1774 expression sources: half blink, closed blink, small speaking mouth and open speaking mouth.
- Added `tools/build-expression-frames.py`, which composites only feathered eye or mouth regions onto the exact original portrait. Hair, clothing, hands, bag and silhouette remain pixel-stable between frames.
- Added randomized blink timing and a 100 ms half-close/close/half-close sequence, plus a restrained closed/small/open speaking loop.
- Removed the redundant cyan speech arcs from the character stage; the natural mouth animation and functional voice waveform now carry speech feedback.
- Updated `desktop/build.ps1` so all four expression frames are included in standalone runtime packages.
- Added deterministic runtime capture tooling and verified idle, half-blink, closed-blink, speaking-small, speaking-open, compact-window and settings-drawer states.
- Refreshed the Windows executable baseline to `338,432` bytes with SHA-256 `2834E203934AD7CE14E4C7EED086196D8CA99F016AB06ECDBF479850BF3C94B0`.

## 2026-07-11 Full-Bleed Companion Card Pass

Completed:

- Captured a fresh baseline from the desktop ZIP and produced full, left, companion-card, bottom, voice-dock and character-stage comparisons against the same-size reference.
- Replaced the lower-left circular medallion with a new 1448 x 1086 full-bleed rectangular Iroha chibi bust that preserves the character's violet braid, cyan accents, turquoise eyes and sailor uniform.
- Rebuilt the card artwork layer, translucent caption band, frame, shadow and speaker feedback so the illustration reads as part of the glass surface instead of a pasted avatar.
- Preserved bounded hover zoom and click-to-test-voice behavior without resizing the card or moving neighboring controls.
- Added compact-height artwork positioning so the caption does not cover the character's eyes at 980 x 552.
- Aligned the VN nameplate to the dialogue panel's left edge and connected its speaker mark to the real voice-test workflow.
- Reduced the voice dock's opaque white fill while preserving contrast, live waveform feedback and every existing hit target.
- Verified standard, compact, settings, hover and nameplate-click states using the compiled application.
- Refreshed the Windows executable baseline to `338,432` bytes with SHA-256 `2FCF74710CF2E41BFFAFE33DAD1E724B0CF12AC9A2D0B576A17647A6F5618D3D`.

## 2026-07-11 Background Depth And Glass Hierarchy Pass

Completed:

- Captured a new baseline from the V1.5 desktop ZIP and compared full, background, left-room and window regions against the normalized reference.
- Measured luminance, variation and local edge contrast in clean room regions instead of judging softness from screenshots alone.
- Kept the original 1672 x 941 room bitmap and added a background-only horizontal light field: cool gray depth on the shelf/desk side and restrained white window light on the right.
- Avoided bitmap blur, downsampling and character overlays; the portrait, text and controls continue to render afterward at full sharpness.
- Reduced sampled left-room luminance from 198.20 to 190.72 and local edge contrast from 3.60 to 3.34 while moving the window brightness toward the reference.
- Verified the final depth pass at 1280 x 720, 980 x 552 and with the settings drawer open; no text, card or interaction geometry changed.
- Refreshed the Windows executable baseline to `338,944` bytes with SHA-256 `240A3630AB83F63B212A7E7B87EF655757609EA55EF8DC05E42CB079BA3B67A8`.

## 2026-07-11 Dialogue Typography And Natural Animation Runtime Pass

Completed:

- Launched the V1.6 desktop ZIP from an independent directory, captured a fresh baseline and compared the full view, left column, dialogue area and character stage against the same-size reference.
- Recalibrated the visual-novel dialogue hierarchy to a restrained 11.8 pt headline, 8.7 pt body and 9 pt nameplate so the text density and emphasis more closely match the reference.
- Raised the character animation timer to 66 ms, approximately 15 FPS, while preserving a randomized 3.8-6.3 second natural blink interval and half-close/close/half-close sequence.
- Synchronized the title-bar portrait slice to the main stage through `AvatarControl.AnimationFrameChanged`, removing the independent overlay timer and preventing mismatched facial frames.
- Added an exact-window-size, full-resolution background cache. It uses high-quality bicubic sampling without blur or downsampling and keeps the character and owner-drawn controls fully sharp.
- Limited invalidation to the character-stage region and suppressed redundant idle frames unless integer motion or blink state changes.
- Reduced compiled idle use from 90.3% to 4.1% of one CPU core while retaining the active 66 ms expression cadence; measured working set was 151.5 MB.
- Captured a real runtime blink contact sequence and GIF proving idle, half-close, closed, half-close and idle states occur in order without portrait displacement.
- Updated `desktop/build.ps1` to fail immediately when `csc.exe` returns a non-zero exit code; verified the guard with a controlled compiler failure before rebuilding successfully.
- Re-verified 1280 x 720, 980 x 552, settings drawer, nameplate voice feedback and all five static expression states with the compiled application.
- Refreshed the Windows executable baseline to `340,480` bytes with SHA-256 `EBB4DB8D80B666EADDDB0106B27E81A5584649FCE5FE75BA0F2D90E46DCF4A1D`.

## 2026-07-15 V1.8 Natural Motion And Stability Pass

Completed:

- Launched the V1.7 desktop ZIP from an independent temporary directory and normalized the reference and runtime to the same 1280 x 720 viewport before editing.
- Replaced the three-step 66 ms blink switch with six eased 50 ms stages and replaced the regular mouth loop with an eighteen-phase rest/small/open cadence.
- Kept every facial state on the same 887 x 1774 portrait geometry and blended only the localized eye or mouth source, eliminating circular face patches and hard expression jumps.
- Added restrained happy, thinking, focus, shy, surprised and error reactions from the same natural face layers and removed the stage's geometric diamonds, petals and exclamation mark.
- Stopped stage mode from preloading the legacy rough effect frames; fallback frames remain available only when the primary portrait is unavailable.
- Rebalanced the portrait crop slightly downward and inward, added slower breathing motion, refined the VN dialogue panel into a left-clear/right-cyan layered glass surface and reduced companion-card crowding.
- Captured all six blink stages, all eighteen speech phases, standard 1280 x 720, compact 980 x 552 and settings states from the compiled application.
- Added a deterministic functional QA harness covering the primary composer, four quick actions, dialogue, settings drawer, model state, speaking state, compact layout and conversation pin/reorder behavior.
- Fixed a real `ContextMenuStrip` disposal race by deferring cleanup until the current UI message completes; open/click/close regression passed with an unchanged crash-log hash.
- Refreshed the Windows executable baseline to `342,528` bytes with SHA-256 `AD4D1DFE7F3843044407436067C9EEF25B2618C56D7E1BFE7E8FE1AD199E26AF`.

## 2026-07-16 V1.9 Voice Output Reliability Pass

Completed:

- Reproduced the complete local GPT-SoVITS chain with the user's current reference audio, prompt text and v2ProPlus CPU configuration instead of relying on a health endpoint alone.
- Verified cold startup from a stopped service: the formal EXE launched the configured runtime, reached `/docs` in 16.35 seconds and did not change the crash-log hash.
- Tightened TTS response validation to require a real RIFF/WAVE payload; long JSON or HTML error bodies can no longer be mistaken for audio.
- Added bounded PCM16 peak normalization before playback. The measured voice sample reached `-1.1 dBFS` peak and approximately `-18.7 dBFS` RMS without clipping.
- Added near-silence rejection and explicit temporary-file cleanup for rejected, completed and failed playback paths.
- Loaded WAV data before `PlaySync` and isolated playback exceptions so a voice-device failure cannot turn an otherwise valid Chinese reply into a failed chat operation.
- Protected asynchronous voice warmup and bundled-process startup from unhandled launch errors while preserving automatic startup and text-only degradation.
- Added deterministic voice and voice-UI harnesses covering service readiness, synthesis timing, duration, loudness, silence, playback, cleanup, invalid payload rejection, speaking state, waveform state and control recovery.
- Captured the formal application in automatic-ready and real-speaking states, then compared the full 1280 x 720 view and bottom voice region beside the normalized reference.
- Refreshed the Windows executable baseline to `344,576` bytes with SHA-256 `E9F80273170CD3021094B1686C19D22FA628C3B8D67891E22B16B5B5D6CD44A0`.

## 2026-07-16 V2.0 Owner-Drawn Tool Rail And Service Card Pass

Completed:

- Captured the reported right-rail and service-card defects from the compiled application and compared both regions beside the normalized 1280 x 720 reference before editing.
- Replaced the three independently composited tool buttons with one owner-drawn `ToolRailPanel`, removing rectangular child backgrounds, dotted seams and inconsistent glass edges.
- Preserved functional memory, settings and appearance actions through proxy commands while adding coherent selected, hover, pressed and disabled states to the visible rail.
- Rebuilt the memory glyph as a restrained brain outline with internal sulci so it reads as memory rather than mirrored leaves.
- Increased the service-card content allowance and derived both status pills from the usable client rectangle, eliminating lower-edge clipping and text overflow.
- Re-captured standard 1280 x 720, compact 1080 x 680, settings-open and same-size comparison evidence after the owner-drawn changes.
- Re-ran the full interaction harness: conversations, pinning, menu lifecycle, settings selection, appearance feedback, quick actions, Flash/Pro badge, speaking state and compact layout all passed.
- Re-ran GPT-SoVITS synthesis and playback: the 4.70-second PCM16 sample generated in 5.36 seconds, reached `-1.1 dBFS` peak / `-19.6 dBFS` RMS, played and cleaned successfully.
- Extracted the formal desktop ZIP outside the source tree, verified its executable hash and forbidden-file audit, then used a real screen-space pointer hit to drive `准备中 -> 说话中 -> 试听彩叶` in the packaged EXE.
- Refreshed the Windows executable baseline to `350,720` bytes with SHA-256 `F2C67DCF449D0F2BB36758649EF6D806147332EC2D16ED42EB883AD87868AD59`.
