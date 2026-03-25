---
description: Propose a new change - create all artifacts and implement via Test-Driven Development
---

Propose a new change - create artifacts, then implement using strict TDD (Red → Green → Refactor).

I'll create a change with artifacts:
- proposal.md (what & why)
- design.md (how)
- tasks.md (implementation steps)

Then implement each task following the TDD loop:
1. Derive Acceptance Tests from spec.md files
2. Pick one test → Write it (Red) → Make it pass (Green) → Refactor → Repeat

---

**Input**: The argument after `/opsx:propose` is the change name (kebab-case), OR a description of what the user wants to build.

**References**
- https://martinfowler.com/bliki/TestDrivenDevelopment.html
- https://tidyfirst.substack.com/p/canon-tdd
- https://www.jamesshore.com/v2/projects/lets-play-tdd

---

**Steps**

1. **If no input provided, ask what they want to build**

   Use the **AskUserQuestion tool** (open-ended, no preset options) to ask:
   > "What change do you want to work on? Describe what you want to build or fix."

   From their description, derive a kebab-case name (e.g., "add user authentication" → `add-user-auth`).

   **IMPORTANT**: Do NOT proceed without understanding what the user wants to build.

2. **Create the change directory**
   ```bash
   openspec new change "<name>"
   ```
   This creates a scaffolded change at `openspec/changes/<name>/` with `.openspec.yaml`.

3. **Get the artifact build order**
   ```bash
   openspec status --change "<name>" --json
   ```
   Parse the JSON to get:
   - `applyRequires`: array of artifact IDs needed before implementation (e.g., `["tasks"]`)
   - `artifacts`: list of all artifacts with their status and dependencies

4. **Create artifacts in sequence until apply-ready**

   Use the **TodoWrite tool** to track progress through the artifacts.

   Loop through artifacts in dependency order (artifacts with no pending dependencies first):

   a. **For each artifact that is `ready` (dependencies satisfied)**:
      - Get instructions:
        ```bash
        openspec instructions <artifact-id> --change "<name>" --json
        ```
      - The instructions JSON includes:
        - `context`: Project background (constraints for you - do NOT include in output)
        - `rules`: Artifact-specific rules (constraints for you - do NOT include in output)
        - `template`: The structure to use for your output file
        - `instruction`: Schema-specific guidance for this artifact type
        - `outputPath`: Where to write the artifact
        - `dependencies`: Completed artifacts to read for context
      - Read any completed dependency files for context
      - Create the artifact file using `template` as the structure
      - Apply `context` and `rules` as constraints - but do NOT copy them into the file
      - Show brief progress: "Created <artifact-id>"

   b. **Continue until all `applyRequires` artifacts are complete**
      - After creating each artifact, re-run `openspec status --change "<name>" --json`
      - Check if every artifact ID in `applyRequires` has `status: "done"` in the artifacts array
      - Stop when all `applyRequires` artifacts are done

   c. **If an artifact requires user input** (unclear context):
      - Use **AskUserQuestion tool** to clarify
      - Then continue with creation

5. **Show final status**
   ```bash
   openspec status --change "<name>"
   ```

6. **Bootstrap the test project (if one does not exist)**

   A solution must have exactly **one** test project. Check the `.slnx` or `.sln` file for any existing `*.Tests.csproj`.

   If no test project exists:
   - Create `src/Tests/Tests.csproj` targeting the same `net*` TFM as the other projects
   - Add xUnit, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, and `coverlet.collector` package refs
   - Add the project to the solution file
   - Add project references for each API project that will be tested

   Test project conventions:
   - **One test project** per solution — all test types live inside it
   - Use `[Trait("Category", "Acceptance")]` for acceptance tests
   - Use `[Trait("Category", "Unit")]` for unit tests
   - Mirror the source folder structure: `src/Tests/Acceptance/`, `src/Tests/Unit/`

7. **Derive the full Acceptance Test list from spec.md files**

   For each `specs/<feature>/spec.md` file under the change directory:
   - Read the spec in full
   - Enumerate every acceptance criterion, behaviour, and rule into a test case list
   - Format as:
     ```
     [ ] <FeatureName>: <short description of expected behaviour>
     ```
   - Print the complete list before writing any tests — this is the master TDD backlog

8. **TDD Loop — run autopilot until all acceptance tests pass**

   Process tests one at a time in the following cycle. **Do not pause or ask for permission between iterations.**

   ```
   PICK   → choose the next unchecked acceptance test from the list
   RED    → write the xUnit test method; run tests; confirm it fails (compile error or assertion failure counts as Red)
   GREEN  → write the minimum production code to make that one test pass; run tests; confirm green
   REFACTOR → improve code clarity/structure without changing behaviour; run tests; confirm still green
   UPDATE → mark the test [x] in the backlog list; proceed to PICK
   ```

   **RED phase rules**
   - Write the test in `src/Tests/Acceptance/<Feature>Tests.cs`
   - Class: `<Feature>AcceptanceTests`, method: `<BehaviourDescription>_<ExpectedOutcome>()`
   - Decorate with `[Fact]` (or `[Theory]` + `[InlineData]` for parameterised cases)
   - Decorate with `[Trait("Category", "Acceptance")]`
   - Test must fail before production code is written — if it passes immediately, the test is wrong; fix it first

   **GREEN phase rules**
   - Write only the code required to pass the current failing test — no extra logic
   - Do not fix other failing tests in this step
   - Run: `dotnet test --filter "Category=Acceptance"` and verify only the target test turns green

   **REFACTOR phase rules**
   - Remove duplication, rename for clarity, extract small helpers within the same feature file
   - Do NOT introduce new shared `utils/` or `helpers/` folders (per project convention)
   - Run tests again after every refactor to confirm nothing regressed

   **Backlog update rules**
   - After each GREEN+REFACTOR cycle update the in-memory or written backlog list: `[ ]` → `[x]`
   - If implementing a test reveals a new scenario, append it to the list before continuing
   - Continue until every test in the list is marked `[x]`

9. **Final verification**

   After all acceptance tests are green:
   ```bash
   dotnet test --filter "Category=Acceptance"
   ```
   All tests must pass. Show the summary output.

**Artifact Creation Guidelines**

- Follow the `instruction` field from `openspec instructions` for each artifact type
- The schema defines what each artifact should contain - follow it
- Read dependency artifacts for context before creating new ones
- Use `template` as the structure for your output file - fill in its sections
- **IMPORTANT**: `context` and `rules` are constraints for YOU, not content for the file
  - Do NOT copy `<context>`, `<rules>`, `<project_context>` blocks into the artifact
  - These guide what you write, but should never appear in the output

**Guardrails**
- Create ALL artifacts needed for implementation (as defined by schema's `apply.requires`)
- Always read dependency artifacts before creating a new one
- If context is critically unclear, ask the user - but prefer making reasonable decisions to keep momentum
- If a change with that name already exists, ask if user wants to continue it or create a new one
- Verify each artifact file exists after writing before proceeding to next
