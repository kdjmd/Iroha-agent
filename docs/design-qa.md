# Design QA

source visual truth: external reference image (not redistributed in this repository)
implementation screenshot path: docs\evidence\round-2026-07-16-v20-rail-service-standard.png
packaged runtime screenshot path: docs\evidence\round-2026-07-16-v20-packaged-final.png
full-view comparison evidence: docs\evidence\round-2026-07-16-v20-comparison-full.png
focused region comparison evidence:
- docs\evidence\round-2026-07-16-v20-rail-service-comparison.png
- docs\evidence\round-2026-07-16-v20-packaged-voice-speaking.png
- docs\evidence\round-2026-07-16-v20-functional-qa.txt
- docs\evidence\round-2026-07-16-v20-voice-qa.txt
- docs\evidence\round-2026-07-16-v20-voice-ui-qa.txt
- docs\evidence\round-2026-07-16-v19-voice-comparison-bottom.png
- docs\evidence\round-2026-07-16-v19-voice-speaking.png
- docs\evidence\round-2026-07-16-v19-voice-qa.txt
- docs\evidence\round-2026-07-15-v18-natural-blink-contact.png
- docs\evidence\round-2026-07-15-v18-natural-speech-contact.png
responsive evidence: docs\evidence\round-2026-07-16-v20-rail-service-compact.png
settings evidence: docs\evidence\round-2026-07-16-v20-rail-settings.png
viewport: 1280 x 720 desktop window; secondary check at 980 x 552
state: idle, automatic voice warmup, real TTS playback, six-stage blink, eighteen-phase speaking loop, happy/thinking/shy/surprised, settings and compact runtime states; API key masked

## Findings

- No actionable P0/P1/P2 mismatches remain in this pass.
- The implementation now uses the room illustration, high-resolution portrait, owner-drawn glass surfaces, live text controls, and live buttons as separate runtime layers. The previous baked-shell duplicates, ghost labels, and mismatched hit areas are gone.
- The 1280 x 720 composition aligns the reference's major zones: sidebar, three memory cards, large right-side character stage, VN dialogue panel, four quick actions, text composer, voice dock, and right tool rail.
- The compact 980 x 552 layout no longer crushes the information cards into the sidebar. It intentionally hides the three secondary cards, retains the main chat path, and preserves readable dialogue, conversation history, typing, voice playback, and tool access.
- P3: the current room is sharper and brighter than the deliberately softened reference background. This is acceptable because the user explicitly prioritized a high-definition result.
- The service card now uses two compact status pills for DeepSeek and GPT-SoVITS, matching the reference hierarchy while preserving live state text.
- The service card reserves explicit bottom breathing room and derives pill geometry from its usable client area, so neither status pill nor secondary line is clipped at 1280 x 720.
- The right tool rail is now one owner-drawn interactive surface instead of three child-button rectangles. Its glass shell, selected layer, icons, labels, hover and pressed feedback share one coordinate system, eliminating the seams and hard boxes visible in the reported crop.
- `CharacterTopOverlayControl` draws the portrait head across the lower title-bar edge so the stage depth matches the reference without blocking window controls. It now follows `AvatarControl.AnimationFrameChanged`, keeping both portrait slices on the same animation frame.
- The stage now swaps four 887 x 1774 locally composited expression frames. Only the eye or mouth region changes; hair, body, hand, clothing and bag pixels stay on the original portrait, preventing redraw shimmer.
- Blink cadence is randomized between roughly 3.4 and 5.9 seconds and now uses six eased stages from open to half-close, closed and open. Speaking uses an eighteen-phase rest/small/open pattern on a 50 ms timer, approximately 20 FPS, with gradual opacity transitions instead of hard frame swaps.
- Happy, thinking, focus, shy, surprised and error states now reuse the same pixel-stable facial assets with restrained blends. The former diamond, petal and exclamation overlays are no longer drawn on the character stage.
- Stage mode no longer preloads the legacy full-body effect frames. It starts from the 887 x 1774 portrait and four localized expression layers, avoiding the rough halo and geometric overlays even during startup.
- The VN text hierarchy was recalibrated against a same-size reference crop: 11.8 pt bold headline, 8.7 pt regular body and 9 pt bold nameplate. The final dialogue block is quieter, denser and closer to the reference while remaining readable at 980 x 552.
- Intentional difference: the top `模式：视觉小说` chip is omitted because the user explicitly requested removing that bar.
- Intentional difference: the reference microphone input control is omitted because the user explicitly requested text-only input.
- The title badge is now live application state rather than a decorative `Pro` label: the current `deepseek-v4-flash` configuration renders `Flash`; `deepseek-v4-pro` renders `Pro`.
- The active conversation uses an exact circular crop of the official real-world Iroha artwork; the remaining records rotate official work character thumbnails.
- The official work thumbnails now share high-quality bicubic sampling, an inset white ring and a restrained cyan outer ring, so different source palettes read as one conversation system without hiding the original character art.
- The companion card now uses a purpose-built 1448 x 1086 full-bleed rectangular chibi bust instead of a circular avatar medallion. Its integrated caption band, restrained outline and bounded hover zoom match the reference composition without changing layout dimensions.
- Compact-card artwork shifts upward only below 160 px card height, keeping the character's eyes clear of the caption band at the declared 980 x 552 minimum window.
- The dialogue nameplate is aligned to the panel's left edge like the reference and its visible speaker mark is now functional: clicking the nameplate invokes the same voice-test workflow as the companion card and voice dock.
- The room remains the original 1672 x 941 high-resolution asset, but a background-only horizontal depth field now lowers left-side room contrast and adds restrained window light on the right. It is cached once at the exact window size with high-quality bicubic sampling; no bitmap blur or low-resolution intermediate is applied.
- Animation redraws remain limited to the character-stage region, and idle frames are invalidated only when integer breathing displacement or blink state changes. Active speaking uses the new 50 ms cadence while the exact-size background cache prevents full-room resampling.
- In clean background samples, left-room luminance moved from 198.20 to 190.72 and edge contrast from 3.60 to 3.34; the right window moved toward the reference brightness without washing out plants or curtain folds.
- Inactive conversation rows now sit directly on the sidebar glass instead of forming nested white pills; only the active and hover states receive a filled surface.
- Information-card title, icon, body and action-button spacing now follows the source at the normalized 1280 x 720 viewport. The service card was shortened after focused comparison so its pills match the source density.
- Quick actions now render the source's two-line hierarchy (`陪我聊 / 随便聊聊吧`, etc.) without a second enclosing card, and the text-only voice dock has been reflowed into the space left by the intentionally removed microphone control.
- V1.9 validates the real voice chain instead of treating `/docs` as sufficient evidence: the formal EXE starts GPT-SoVITS from a stopped state, creates a non-silent RIFF/WAVE file, enters the speaking animation, activates the waveform and restores the controls after playback.
- Generated PCM16 speech is now peak-normalized with a bounded gain before playback. The measured sample moved from roughly `-9.7 dBFS` peak to `-1.1 dBFS`, while silent or non-WAV responses are rejected.
- Voice startup and playback failures no longer escape into the whole chat operation. Chinese text remains available, the status reports a voice-only degradation, and temporary WAV files are removed.

## Required Fidelity Surfaces

- Fonts and typography: Microsoft YaHei UI is used consistently for Chinese UI copy; the VN headline/body/nameplate sizes are calibrated to 11.8/8.7/9 pt, and left-aligned conversation rows, card labels, quick-action subtitles and truncation were checked at both viewports. No overlapping or clipped primary text remains.
- Spacing and layout rhythm: the final dialogue/input/voice zones align closely with the reference at 1280 x 720. Sidebar conversations, save/clear actions, the three card heights, nameplate width, composer inset and tool rail use stable dimensions and responsive repositioning.
- Colors and visual tokens: the interface is cyan-white frosted glass with navy text, mint status accents, soft borders, and no purple rectangular buttons or hard black/gray panel borders.
- Image quality and asset fidelity: the runtime uses `vn-room-bg.png`, the 887 x 1774 transparent character portrait and four same-resolution expression frames. The character is stage-cropped rather than shown as a small full-body card, and scaling uses high-quality bicubic rendering.
- Copy and content: visible actions are functional and task-specific: `陪我聊`, `做计划`, `找灵感`, `复盘`, `保存对话`, `清空对话`, `记忆`, `设定`, and `外观`. Voice input is absent as requested.
- Icons: top window controls use Segoe Fluent Icons; quick actions and tool icons map to their actual functions; the memory tool uses a dedicated brain mark.
- States and interactions: settings opens as a floating drawer; the conversation menu exposes rename, pin/unpin, and delete; prompt shortcuts populate the text composer; voice warmup reaches `语音已准备好`; Enter sends and Shift+Enter inserts a newline.

## Comparison History

- Iteration 1 evidence: `iroha-current-20260710-pass1.png` showed a static shell carrying baked controls. P1 issues were duplicated UI, ghost text, non-interactive visual chrome, and mismatched component proportions.
- Iteration 1 fix: switched the main surface to the clean room asset, enabled the live stage character, and converted sidebar, cards, dialogue, composer, quick actions, voice dock, top controls, and tool rail to owner-drawn runtime controls.
- Iteration 2 evidence: `iroha-current-20260710-pass6.png` showed the stage switching to a smaller low-resolution frame after voice warmup. The fifth conversation also competed with footer actions.
- Iteration 2 fix: kept the high-resolution portrait for every stage state, added state-specific motion, compacted conversation spacing, restored category labels, and added the memory footer and voice-engine label.
- Iteration 3 evidence: `iroha-current-980x552-pass1.png` showed P2 clipping and density collapse at the declared minimum size.
- Iteration 3 fix: added a dedicated compact layout that hides secondary cards, preserves the core conversation workflow, resizes conversation rows, and simplifies the voice dock.
- Interior-rhythm baseline: `docs\evidence\round-2026-07-10-comparison-full.png` exposed P2 drift from opaque inactive conversation pills, oversized card insets, an over-wide nameplate, a large composer inset and missing quick-action subtitles.
- Interior-rhythm pass 1-2: `round-2026-07-10-pass1.png` and `round-2026-07-10-pass2.png` removed the nested sidebar pills, aligned card content, narrowed the nameplate and restored the two-line quick actions. Focused comparison still showed centered conversation copy and compressed status-card heights.
- Interior-rhythm pass 3: `round-2026-07-10-pass3.png` explicitly left-aligned conversation copy, normalized avatar size and matched the three information-card proportions. Focused bottom comparison then exposed the empty microphone-sized gap left by the text-only input requirement.
- Interior-rhythm pass 4: `docs\evidence\round-2026-07-10-comparison-pass4-full.png`, `round-2026-07-10-comparison-pass4-left.png` and `round-2026-07-10-comparison-pass4-bottom.png` show the reflowed voice dock, corrected VN typography and quieter glass surfaces with no remaining P0/P1/P2 mismatch.
- Interior-rhythm pass 5-6: focused comparison exposed the five-pixel inset created by owner-drawn glass bounds. `round-2026-07-10-comparison-pass6-full.png` and `round-2026-07-10-comparison-pass6-left.png` align the sidebar, search field, new-chat control and three information-card outer edges without moving their already-correct internal text.
- Companion-card pass 1-3: `round-2026-07-11-baseline-current.png` first proved that the old full-width circular portrait read like a pasted avatar. `round-2026-07-11-pass1.png` removed the enclosing ring but over-cropped the face; `round-2026-07-11-pass2.png` restored the shoulders; the final `round-2026-07-11-pass3.png` uses a smaller centered Q-version composition that matches the reference card's visual weight at both declared window sizes.
- Post-fix evidence: `docs\evidence\windows-standalone-1280.png`, `docs\evidence\windows-compact-980.png` and `docs\evidence\windows-settings-1280.png` show no overlapping primary controls or clipped primary actions.
- Final polish evidence: `iroha-reference-vs-final-round.png` compares the reference and current 1280 x 720 build after replacing the brand mark, simplifying title-bar controls, adding the search glyph and introducing restrained dialogue-panel petals.
- Dynamic-model polish evidence: `iroha-reference-vs-final-dynamic.png` compares the final Flash configuration after expanding dialogue breathing room, adding the paper-plane send mark, correcting the active Iroha portrait and moving decorative lines away from body copy.
- Natural-expression pass: `round-2026-07-11-expression-final-contact.png` proves the source-level facial changes are localized; `round-2026-07-11-animation-runtime-contact.png` proves the five runtime states remain aligned; `round-2026-07-11-natural-expression-demo.gif` records the intended blink and speech cadence.
- Full-bleed companion-card pass: `round-2026-07-11-continuation-companion-comparison.png` exposed the circular-medallion mismatch. `round-2026-07-11-chibi-card-companion-comparison-pass2.png` shows the replacement rectangular artwork, integrated caption and quieter frame against the same reference crop.
- Compact and interaction follow-up: `round-2026-07-11-chibi-card-compact-980-pass2.png` verifies the responsive crop; `round-2026-07-11-nameplate-click.png` records the functional speaker/nameplate path entering the expected voice-unavailable feedback when voice is disabled by the QA harness.
- Background-depth pass: `round-2026-07-11-depth-full-comparison-pass2.png` and its focused background/window crops compare the reference, V1.5 baseline and V1.6 depth field side by side. Pass 2 keeps the room crisp while reducing the high-frequency prominence behind glass cards.
- V1.7 typography and runtime pass: `round-2026-07-11-v17-dialogue-comparison-pass4.png` compares the reference, V1.6 and the final calibrated dialogue hierarchy at the same scale. `round-2026-07-11-v17-animation-final-contact.png` verifies five aligned expression states, while `round-2026-07-11-v17-natural-blink-contact.png` records the real runtime idle/half/closed/half/idle sequence.
- V1.8 natural-motion pass: `round-2026-07-15-v18-natural-blink-contact.png` records all six blink transitions, and `round-2026-07-15-v18-natural-speech-contact.png` records the complete eighteen-phase mouth cadence. `round-2026-07-15-v18-final-comparison-full.png` and focused left/bottom/stage comparisons place the refined runtime beside the same normalized reference.
- V1.9 voice-output pass: `round-2026-07-16-v19-auto-voice-ready.png` records automatic GPT-SoVITS startup from a stopped state; `round-2026-07-16-v19-voice-speaking.png` records real playback with natural mouth motion and an active waveform; the full and bottom comparisons confirm that the voice fix preserves the reference-aligned VN composition.
- V2.0 rail/service pass: `round-2026-07-16-v20-rail-service-comparison.png` places the reported service card and right tool rail beside the same normalized reference crop. `round-2026-07-16-v20-packaged-voice-speaking.png` proves the rebuilt formal ZIP still reaches the speaking state after a real pointer hit on the packaged EXE.

## Interaction Verification

- `desktop\build.ps1`: passed; it now throws when `csc.exe` returns a non-zero exit code, and the guard was proven with a controlled compile failure before the corrected build.
- Main EXE launch and 1280 x 720 render: passed.
- Source-independent package launch from `%TEMP%\IrohaStandaloneAcceptance`: passed; the high-resolution portrait remained present.
- Voice warmup status: passed (`语音已准备好`).
- Settings drawer invocation: passed.
- Conversation context menu: passed; rename, pin, and delete entries were present.
- Pin behavior: passed; the pinned conversation moved to the top.
- Quick-action handler: passed; the selected scene populated the composer.
- Minimum-window render at 980 x 552: passed.
- Top title-bar controls: passed; settings opened, maximize and minimize states were observed, and close terminated the tested process.
- Model badge mapping: passed; isolated screenshots confirmed `deepseek-v4-flash -> Flash` and `deepseek-v4-pro -> Pro`, then the user's original Flash setting was restored.
- Advanced model selector: passed; selecting v4 Pro and invoking save produced `Settings.Model = deepseek-v4-pro` with `DialogResult.OK`.
- Quick-action mouse click path: passed; the selected scenario populated the composer while retaining its full prompt in `Tag`.
- Settings runtime state: passed; the real owner-drawn title bar, masked API key, companion toggles and drawer actions were captured together through `PrintWindow`.
- Companion-card hover state: passed; the tested pointer state changes only portrait scale, shadow and speaker glow, with no text movement or control overlap.
- Conversation editing regression: passed; rename, pin/unpin and delete handlers remain available for all five initial conversations.
- Post-change stability: passed; repeated standard, compact and settings launches remained alive and did not change the last crash-log timestamp (`2026-07-10 10:04:05`).
- Natural expression state capture: passed; idle, half-blink, closed-blink, speaking-small and speaking-open were rendered from the compiled application with no face patch boundary or character-stage jump.
- Natural runtime blink sequence: passed; frames 79-85 of the real application show idle, idle, half, closed, half, idle, idle with no stage displacement.
- Animation performance: passed; compiled idle sampling fell from 90.3% to 4.1% of one CPU core after exact-size background caching, localized invalidation and redundant idle-frame suppression. Working set measured 151.5 MB.
- V1.7 layout regression: passed; standard 1280 x 720, compact 980 x 552, settings drawer and nameplate-click captures remain aligned and unclipped after the animation/runtime changes.
- Expression packaging: passed; all four high-resolution PNGs were copied to `desktop/dist/assets/character/expressions` by `desktop/build.ps1`.
- Companion-card packaging: passed; `iroha-chibi-card-v2.png` is present in `desktop/dist/assets/ui` and renders from a source-independent runtime directory.
- Nameplate voice interaction: passed; a deterministic compiled QA run invoked the visible nameplate speaker and reached the expected voice feedback without network or local TTS startup.
- Background-only depth field: passed; standard, compact and settings captures retain sharp character edges, readable text and unchanged control geometry.
- V1.8 natural animation: passed; all six blink stages and all eighteen speaking phases retain the same hair, face outline, body, hands, clothing and bag alignment with no circular patches or hard facial jumps.
- V1.8 interaction harness: passed; quick actions, composer, dialogue, settings drawer, Flash/Pro state, speaking state, compact layout and conversation pin/reorder paths all passed in the compiled build.
- Conversation-menu lifecycle regression: passed; menu disposal is deferred until the current UI message completes, and the open/click/close test leaves the crash-log hash unchanged.
- V1.8 executable baseline: `342,528` bytes, SHA-256 `AD4D1DFE7F3843044407436067C9EEF25B2618C56D7E1BFE7E8FE1AD199E26AF`.
- V1.9 voice service cold-start: passed; the formal EXE started the configured GPT-SoVITS runtime in `16.35` seconds and left the crash-log hash unchanged.
- V1.9 voice generation and playback: passed; the final 4.50-second sample generated in 4.67 seconds, reached `-1.1 dBFS` peak / `-18.7 dBFS` RMS, played through `SoundPlayer`, and cleaned its temporary file.
- V1.9 voice failure isolation: passed; missing playback files and non-WAV payloads degrade without throwing or blocking Chinese output.
- V1.9 speaking UI: passed; real playback activates the natural mouth animation, `说话中` label and waveform, then restores the voice button and idle status.
- V1.9 executable baseline: `344,576` bytes, SHA-256 `E9F80273170CD3021094B1686C19D22FA628C3B8D67891E22B16B5B5D6CD44A0`.
- V2.0 owner-drawn rail and service-card regression: passed; standard, compact and settings screenshots have no child-button seams, status-pill overflow or clipped labels.
- V2.0 functional regression: passed; settings selection, appearance feedback, conversations, quick actions, model badge, speaking state and both supported layouts remain interactive.
- V2.0 packaged voice click: passed; an actual screen-space pointer hit on the extracted formal EXE produced `准备中 -> 说话中 -> 试听彩叶`, active waveform and natural mouth motion, then restored the button.
- V2.0 executable baseline: `350,720` bytes, SHA-256 `F2C67DCF449D0F2BB36758649EF6D806147332EC2D16ED42EB883AD87868AD59`.

## Follow-up Polish

- Optional P3: add similarly localized shy, delighted and thinking portraits after licensed art review, using the same pixel-stable composition pipeline.
- Optional P3: after formal publication licensing, substitute a distribution-approved character illustration while preserving the now-correct full-bleed rectangular composition and existing interactions.

final result: passed
