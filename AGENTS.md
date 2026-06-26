# AGENTS Guide

## Project overview

This repository builds a knowledge assistant: a search and retrieval companion that helps teammates quickly ask questions when they forget business rules.

Primary goal (phase 1):

- Support one knowledge source/tool that reads markdown files from the knowledge base.
- Focus on reliable retrieval and clear answers before adding more sources.

High-level architecture:

- `src/frontend`: TanStack Start fullstack app (acts as Backend for Frontend).
- `src/backend`: ASP.NET Core API.
- `src/knowledgebase`: markdown files containing business rules and domain knowledge.

AI/LLM direction:

- Integrate TanStack AI on frontend and Microsoft Agent Framework on backend.
- LLM provider can be added later (for example Ollama or Azure OpenAI).
- Keep provider integrations pluggable, but avoid premature abstraction until a second provider is actively needed.

## Build and test commands

Use latest stable versions unless a specific version is pinned in repository config.

Frontend (`src/frontend`):

- Install deps: `pnpm install`
- Run dev app: `pnpm dev`
- Build: `pnpm build`
- Lint/format (Biome): `pnpm biome check .`

Backend (`src/backend`):

- Restore: `dotnet restore`
- Run API: `dotnet run`
- Build: `dotnet build`

## Code style guidelines

Follow these architectural and design rules across frontend and backend:

Architecture and patterns:

- Use CQRS for application behavior separation.
- Use Vertical Slice Architecture for feature organization.
- In TanStack Start, keep BFF behavior in server functions that orchestrate calls to backend API endpoints.

Design principles:

- Follow SOLID.
- Follow DRY, but do not over-generalize.
- Follow KISS and YAGNI.
- Favor composition over inheritance.
- Prefer simplicity over complexity.
- Do not abstract unless there is a real, present need.

Implementation guidance:

- Keep slices cohesive: request, handler, validation, mapping, and response should be easy to trace.
- Minimize cross-slice coupling.
- Keep domain/business rules explicit and readable.
- Prefer small, focused functions and clear naming over deep class hierarchies.
- Use progressive disclosure when presenting business rules from many files or long text bodies: show concise summaries first, then reveal deeper details, source excerpts, and full context on demand.

Backend guidance:

- C# classes should be sealed by default favoring composition over inheritance.

Frontend guidance:

- Use TypeScript with strict typing.
- Use Biome as formatter/linter.
- Use shadcn defaults with blue theme for UI baseline consistency.

Progressive disclosure:

- Default response should prioritize scanability: key answer, confidence, and top supporting rules.
- Add optional layers users can expand: rationale, matched excerpts, related rules, and raw source path.
- Preserve traceability at every layer by showing where the rule came from.
- Avoid overloading first view with full document dumps unless explicitly requested.
- Follow implementation plan in `src/PROGRESSIVE_DISCLOSURE.md`.

## Security considerations

Baseline security requirements:

- Never commit secrets to the repository.
- Never push API keys, tokens, passwords, certificates, connection strings, or private credentials.
- Store secrets in environment variables or secure secret stores.
- Use `.env` files only for local development and keep them ignored by git.
- Keep `.env.example` sanitized and free of real values.
- Validate and sanitize all external input.
- Enforce least privilege for service accounts and access tokens.
- Avoid logging sensitive data (credentials, tokens, personal data).
- Use HTTPS/TLS for all network communication.
- Keep dependencies updated to latest secure releases.
