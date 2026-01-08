# Code Generation and Review Guidelines

## Purpose

These guidelines help ensure generated code follows sound software engineering principles. When generating or reviewing code, apply these principles thoughtfully and consistently.

## Core Principles

### Be Consistent with Existing Code

When reviewing or extending existing code, maintain consistency with established patterns and style. Avoid suggesting wholesale refactoring unless specifically requested. When code conflicts with guidelines, only suggest changes to sections that need updating for other reasons (bug fixes, new features). Fixing code in isolation risks introducing new bugs without the context of active development.

### Code Must Be Easy to Change

Prioritize designing code that is easy to change and adapt. This is the most important property of a codebase—it enables adaptation to new requirements and prevents stagnation.

When considering abstractions, evaluate:

- How much code would truly unify under the abstraction?
- How frequently has this code pattern changed recently?
- Will the abstraction need modification soon after introduction?
- How many potential use-cases might the abstraction prevent?

Resist premature abstraction. Patterns should emerge clearly before being codified.

### Less is More

Code is a liability, not an asset. Smaller, simpler codebases are easier to understand, improve, and extend.

**Minimalistic Approach:**

- Implement the smallest solution that solves the current problem
- Avoid anticipating future needs—add functionality only when necessary
- Build advanced features from simple building blocks (arrays, hash tables)

**Pruning:**

- Remove unused code paths
- Refactor out unnecessary complexity
- Never leave commented-out code (rely on version history)

**Dependencies:**

- Avoid unnecessary abstraction layers
- Minimize external libraries; prefer minimalistic single-file libraries
- Consider implementing small functionalities rather than adding dependencies
- Remember that external libraries increase conceptual complexity and maintenance burden

### Keep It Simple

Simpler solutions are easier to understand, reason about, and modify.

"Simple" means:

- Fewer levels of abstraction
- Easier to understand completely (performance, threading, etc.)
- Easier to debug
- More straightforward logic
- Closer to the hardware

**Examples of simpler choices:**

- Immutable over mutable
- Non-generic over generic code
- Single-threaded over multi-threaded (when performance allows)
- Simple standards (JSON, WebSockets, UTF-8) over complex ones (XML, CORBA, UTF-16)

Apply complexity only when necessary (e.g., for performance on modern hardware).

### Explicit Is Better Than Implicit

Code should reveal its behavior rather than hide it. Avoid clever tricks with generics and macros. Prioritize code that's easy to understand and step through in a debugger.

### Design with Performance in Mind

Performance is part of design, not an afterthought. New systems should have clear performance goals (object capacity, update latency, etc.). A system that doesn't meet its performance goals is incomplete. Profile code to understand where time is spent.

### Unit Test Where It Makes Sense

Unit tests provide most value for complicated low-level building blocks (hash tables, memory allocators, algorithms). Cover foundational components with unit tests and property tests.

Higher up the abstraction stack, unit tests can become obstacles to change. For higher-level functionality, consider snapshot tests, input recording, and end-to-end testing methods that catch regressions quickly.

### Avoid Coupling

Minimize complicated dependencies between systems. Each system should be modifiable, optimizable, or replaceable independently.

Use abstract interfaces for shared services (logging, file systems, memory allocation) to enable replacement and mocking for tests.

### Follow Data-Oriented Design Principles

Design around data layouts and data flows first. Memory throughput often limits software performance—ensure data layouts and access patterns are cache-friendly.

Avoid object-oriented design principles that encourage:

- Heap-allocated individual objects
- Data hidden behind accessor functions
- Poor data access patterns

Instead: lay out data efficiently, then write functions to operate on that data.

Consider job-based parallelism the norm for high-throughput systems.

### Write for Readability

Code is read more often than written. Write code that's easy to read and understand for someone new to it (or yourself months later).

Code should be plain and straightforward. Avoid:

- Tricky constructs
- Unnecessary cleverness
- Showing off programming skills

Learn and apply common idioms for the language and domain. What seems like a "hack" to newcomers may be standard idiom to experienced developers (e.g., bit manipulation patterns).

### Consider Inlined Code

When functions grow long, consider whether splitting them actually improves clarity. Sometimes keeping related code together provides value:

- Easier to step through in a debugger
- Reads top-to-bottom without jumping between locations
- Clear logical flow

Use paragraph-sized comment blocks to visually separate functional sections within longer functions.

Split to separate functions when:

- Code needs to be called from multiple places
- The function is genuinely too complicated and long

### Commenting and Documentation

- Public API functions require documentation comments
- Internal code needs comments only as necessary
- Use clear code and sensible names to reduce comment needs
- In Rust: use `///` for documentation comments, `//` for other comments
- Match documentation style of the language's standard library
- Keep documentation in or generated from source code repositories

### Source Code Formatting

Use standard formatting tools (rustfmt, prettier, black, etc.) to avoid formatting discussions. Consistency matters more than perfection.

## Code Review Principles

### Shared Responsibility

When reviewing code:

- Look for opportunities to improve clarity, simplicity, and performance
- Consider whether bugs could be caught at compile time
- Suggest unit tests for detectable issues
- Recommend asserts to prevent misuse
- Evaluate if better naming or documentation would prevent confusion

### Leave Code Better

Every change should leave code in a better state: cleaner, simpler, faster, and easier to understand.

### Question Abstractions

When reviewing new abstractions, consider:

- Is the pattern sufficiently established?
- Does it prevent reasonable future use-cases?
- Will it need modification soon after introduction?

## Build and Integration

### Keep Builds Simple

Building should require minimal steps—ideally one command.

### Keep History Linear

Linear commit history makes it easier to:

- Understand project evolution
- Use tools like git bisect
- Manually search for error introduction points

Use git rebase and squashing appropriately to maintain clean history.

### Deliver Changes Quickly

Short paths from implementation to delivery enable:

- Earlier design validation
- Sooner bug discovery
- Better team synchronization

Use feature flags instead of long-lived branches to protect experimental features and enable quick rollback.

---

## Application Notes

These guidelines prioritize:

1. **Changeability** - Code must adapt to evolving requirements
2. **Simplicity** - Simple code is maintainable code
3. **Performance** - Design with performance goals from the start
4. **Clarity** - Code should be self-documenting where possible

Apply these principles with judgment, considering the specific context and constraints of each situation.
