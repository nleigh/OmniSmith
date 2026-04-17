# Ticket 1.2: Unit Testing Infrastructure
**Goal**: Establish a robust unit testing environment for the OmniRhythm engine.

### Context
With the introduction of multiple domains (Piano, Guitar), architectural stability is paramount. Unit testing allows us to verify core logic (math, parsing, state) without launching the full ImGui application.

### Implementation Steps
1. **Project Setup**:
   - Create `src/OmniSmith.Tests` using the `xunit` template.
   - Reference `OmniSmith.csproj`.
   - Add `FluentAssertions` and `Moq` for expressive testing and interface mocking.
2. **Global Usings**:
   - Configure `Usings.cs` in the test project to include standard testing namespaces.
3. **Core Interface Tests**:
   - Create `Core/DomainTests.cs`.
   - Implement tests for the `IPlayableSong` interface lifecycle (ensure `Dispose` is called, default property values are handled).
4. **CI Integration readiness**:
   - Ensure `dotnet test` executes successfully from the solution root.



### Mandatory TDD Generation 🧪
- You **must** create parallel `[NEW] src/OmniSmith.Tests/` xUnit class tests for any new services or classes introduced here. You must write checks mapping edge bounds to ensure logic does not fail on edge inputs.

### Definition of Done
- `src/OmniSmith.Tests` is part of the solution.
- `dotnet test` runs and passes on the local environment.
- At least one unit test exists for the core interface layer.
