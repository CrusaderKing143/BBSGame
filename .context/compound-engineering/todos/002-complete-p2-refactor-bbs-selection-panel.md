---
status: complete
priority: p2
issue_id: "002"
tags: [unity, ugui, selection-panel, story-integration]
dependencies: []
---

# Refactor BBSChoose Selection Panel

## Problem Statement

`SelectionPanelController` no longer matches the serialized scene data, and the current Story migration removed the legacy `BBSChoose` hierarchy. The picture icon therefore has no working customization panel.

## Findings

- `Assets/Scripts/SelectionPanelController.cs` contains a partial rewrite that is incompatible with the legacy scene fields.
- The current `SampleScene.unity` intentionally removed the old `StorySystem`; that migration must remain intact.
- `StoryFlowController` lives in `BBSGame.Story`, so the selection runtime needs its own named assembly for a direct typed reference.
- Existing assets provide two characters and five props; the two background entries need placeholders.

## Proposed Solutions

### Option 1: Typed runtime module and restored panel

**Approach:** Create `BBSGame.Selection`, rebuild `BBSChoose`, add draft/committed state, integrate it with `StoryFlowController`, and cover the behavior with PlayMode tests.

**Pros:** Clear ownership, testable state transitions, no legacy listener conflicts.

**Cons:** Requires coordinated script, asmdef, prefab, scene, and test changes.

**Effort:** Medium

**Risk:** Medium

## Recommended Action

Implement Option 1 while preserving all current Story migration work and using placeholder background items until final art is supplied.

## Technical Details

**Affected areas:**
- Selection runtime and Item view
- Story picture-button integration
- `SampleScene` BBSChoose hierarchy and serialized configuration
- PlayMode tests and assembly references

## Acceptance Criteria

- [x] PictureIconButton opens BBSChoose from the main UI.
- [x] Character, Background, and Props show 2, 2, and 5 items.
- [x] Each category supports exactly one draft selection.
- [x] Post is disabled until all three categories are selected.
- [x] Post commits and Back restores the previous committed selection.
- [x] Reopening the panel restores committed selections and previews.
- [x] Invalid configuration disables Post without throwing exceptions.
- [x] Story migration changes remain preserved.
- [x] Relevant Unity tests and compile checks pass.
- [x] Code review is completed.

## Work Log

### 2026-07-12 - Implementation started

**By:** Codex

**Actions:**
- Inspected the dirty worktree, current Story migration, legacy BBSChoose data, and available art.
- Confirmed implementation decisions with the user and prepared the execution plan.

**Learnings:**
- The panel must be restored without bringing back the deleted legacy StorySystem.
- A named selection assembly is required for typed integration with BBSGame.Story.

### 2026-07-12 - Implementation completed

**By:** Codex

**Actions:**
- Rewrote the selection runtime under `Assets/Scripts/Selection/` while preserving the original script GUID.
- Added the reusable `SelectionItemView`, selection assembly, Item prefab, draft/committed state, layered previews, validation, and public query/submit APIs.
- Restored the `BBSChoose` hierarchy and configured 2 Character, 2 Background, and 5 Props entries in `SampleScene`.
- Integrated PictureIconButton and ResetStory behavior with the current Story migration.
- Added and updated PlayMode coverage, then ran EditMode and PlayMode suites successfully.
- Completed focused code review, applied two safe correctness fixes, and verified scene/prefab references and diff formatting.

**Learnings:**
- Keeping committed and draft indices separate makes cancel/reopen behavior deterministic without introducing persistence.
- Validating the full Inspector contract at the Story entry point prevents opening a panel that can never submit.

## Notes

- Selection persistence is runtime-only.
- Post does not advance story progression.
- Background entries temporarily use `blank.png` thumbnails.
