---
status: complete
priority: p2
issue_id: "001"
tags: [unity, ugui, story]
dependencies: []
---

# Replace StorySystem with a simple linear story flow

## Problem Statement

`Assets/Scripts/StorySystem.cs` mixed serialized story data, runtime progress, button binding, panel switching, forum onboarding, post unlocking, and an unused choice system in one MonoBehaviour.

## Findings

- `SampleScene` serialized one mail and three posts into `StorySystem`.
- Two additional mail buttons and content GameObjects existed but were not connected.
- The choice system was outside the required linear mail-to-forum flow.
- `SelectionPanelController.cs` contained unrelated user changes and was preserved.

## Proposed Solutions

### Option 1: Split data, progress, views, and flow

**Approach:** Use serializable round data, a plain runtime progress class, two panel controllers, and one small flow coordinator.

**Pros:** Beginner-readable, Inspector-driven, and easy to extend with more rounds.

**Cons:** Adds several focused files.

**Effort:** Medium

**Risk:** Low

## Recommended Action

Option 1 was implemented and the existing scene references were migrated without changing unrelated UI systems.

## Technical Details

**Affected files:**
- `Assets/Scripts/Story/`
- `Assets/Scenes/SampleScene.unity`
- `Assets/Scripts/StorySystem.cs`
- `Assets/Tests/`

## Acceptance Criteria

- [x] Initial state allows mail but not forum interaction.
- [x] Reading mail unlocks the first forum post.
- [x] Posts unlock one at a time when returning from the latest post.
- [x] Completing a round returns to the main page and selects the next mail.
- [x] Forum welcome appears only once per full reset.
- [x] Choice UI and `StorySystem` references are removed.
- [x] Tests and Unity compilation pass.

## Work Log

### 2026-07-12 - Implementation completed

**By:** Codex

**Actions:**
- Added story round data, runtime progress, mail panel, forum panel, and flow coordinator.
- Migrated `SampleScene` to three mail rounds and removed the choice hierarchy.
- Added EditMode and PlayMode tests.
- Fixed re-reading mail progress loss and unsafe post indexing during review.

**Learnings:**
- The scene already contained three mail entries, while only the first round currently has forum post content.
- Keeping panel display separate from progress logic makes the flow easier to read without requiring ScriptableObjects or event buses.

## Notes

- Rounds two and three intentionally have empty post arrays and keep the forum disabled until content is assigned.
