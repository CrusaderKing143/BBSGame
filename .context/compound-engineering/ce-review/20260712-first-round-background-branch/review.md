# First Round background branch review

## Scope

- `Assets/Scripts/Story/`
- `Assets/Scripts/Selection/SelectionPanelController.cs`
- `Assets/Scenes/SampleScene.unity`
- `Assets/Tests/EditMode/StoryProgressTests.cs`
- `Assets/Tests/PlayMode/StoryFlowControllerTests.cs`
- `Assets/Tests/PlayMode/SelectionPanelControllerTests.cs`

Unrelated animation, image, scene-layout, and asset-import changes already present in the working tree were preserved and excluded from review findings.

## Intent

Gate the first-round picture selection behind three normal posts, commit a `jiuBa` or `feiji` background choice, unlock one shared result-post button, route it to the matching content, and advance only after the result post is read. Keep historical mail visible and readable without allowing it to advance current progress.

## Review team

- Correctness
- Testing
- Maintainability
- Project standards
- API contract
- Agent-native applicability
- Institutional learnings
- Adversarial state-sequence review

## Findings and applied fixes

1. The initial implementation transitioned directly from result-post completion to the controller without recording an explicit completed phase.
   - Added `StoryRoundPhase.RoundCompleted` for both ordinary and selection-result round completion.
   - Added EditMode assertions for ordinary and branched completion states.

## Requirements verification

- Picture selection remains locked until all three normal first-round posts are completed.
- Background is the only required category; Character and Props remain browseable.
- Background previews toggle the `jiuBa` and `feiji` child objects and restore committed state on cancel.
- Submission locks the selection and unlocks `PostButton_04`.
- `jiuBa` resolves to `PostContent_04`; `feiji` resolves to `PostContent_05`.
- Returning from the selected result post advances to the next round.
- Current and historical mail stay visible; historical mail does not mutate current progress.

## Verification

- `dotnet build BBSGame.Story.EditModeTests.csproj --no-restore`: passed with 0 warnings and 0 errors.
- `dotnet build BBSGame.Story.PlayModeTests.csproj --no-restore`: passed with 0 warnings and 0 errors.
- The running Unity editor imported `SampleScene.unity` without a scene import error.
- SampleScene serialization contains the expected result-post, preview-object, required-category, and item-ID references.
- Unity Test Runner execution remains pending because the project is currently open in another Unity editor process and cannot be opened by a batch test process simultaneously.

## Advisory notes

- Agent-native parity is not applicable to this offline Unity game UI.
- No `docs/solutions/` institutional learning directory is present.
- `git diff --check` still reports pre-existing trailing whitespace across the heavily modified SampleScene serialization; none of the newly added flow lines contain trailing whitespace.

## Verdict

Code and serialized references are ready. Run the EditMode and PlayMode suites from the already-open Unity editor for final runtime confirmation.
