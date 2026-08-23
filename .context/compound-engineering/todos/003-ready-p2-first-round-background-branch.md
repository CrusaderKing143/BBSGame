---
status: ready
priority: p2
issue_id: "003"
tags: [unity, story-flow, ugui, selection]
dependencies: []
---

# First Round background branch flow

## Problem Statement

The first story round currently advances immediately after its third post, while the picture selection is available too early and does not determine a final branch post.

## Findings

- The first round contains three normal posts; `PostButton_04`, `PostContent_04`, and `PostContent_05` already exist in `SampleScene` but are not wired into story data.
- Background selection currently replaces an Image sprite instead of toggling the `jiuBa` and `feiji` children under `BackgroundPreview`.
- Mail visibility is restricted to the current round, preventing historical mail review.

## Proposed Solutions

- Add an optional selection-gated result post to round data and explicit progress phases.
- Make Background the only required category while retaining Character and Props browsing.
- Resolve the committed background item ID to the appropriate final post content.
- Keep current and historical mail visible and readable without allowing old mail to advance progress.

## Recommended Action

Implement the approved plan across story state, selection preview behavior, scene serialization, and EditMode/PlayMode coverage.

## Acceptance Criteria

- [x] PictureIconButton unlocks only after the three first-round posts are completed.
- [x] `jiuBa` submits to `PostContent_04`; `feiji` submits to `PostContent_05`.
- [x] The first round advances only after returning from the selected result post.
- [x] Background preview children are mutually exclusive and cancel restores committed state.
- [x] Only Background is required for submission; Character and Props remain browseable.
- [x] Historical mail remains visible and readable without mutating current progress.
- [ ] Relevant EditMode and PlayMode tests pass.

## Work Log

- 2026-07-12: Approved implementation started from the conversation plan.
- 2026-07-12: Implementation, scene wiring, tests, static builds, and code review completed. Unity Test Runner execution is pending because the project is open in the Unity editor.
