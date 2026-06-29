# Architecture Diagram

Full system flow: TanStack Start (BFF) → ASP.NET Core API → Microsoft Agent Framework → Knowledge Base, with progressive disclosure via `AgentFrameworkToolParser`.

```mermaid
flowchart TD
    subgraph FE["Frontend — TanStack Start (BFF)"]
        UI["Chat UI\n(React Component)"]
        SF["Server Function\n(BFF Orchestrator)"]
        TAI["TanStack AI\nuseChat / useCompletion"]
        UI -- "user message" --> TAI
        TAI -- "streams AI response" --> UI
        TAI -- "tool call / fetch" --> SF
    end

    subgraph BE["Backend — ASP.NET Core API"]
        direction TB

        subgraph MAF["Microsoft Agent Framework (MAF)"]
            AGENT["AI Agent\n(orchestrates LLM + tools)"]
            TOOL["Tool: SearchRules\n(registered via MAF tool manifest)"]
            LLM["LLM Provider\n(Ollama / Azure OpenAI — pluggable)"]
            AGENT -- "selects tool" --> TOOL
            AGENT <--> LLM
        end

        subgraph SLICE["Vertical Slice — Rules Feature"]
            SRH["SearchRulesHandler\n(CQRS Query Handler)"]
            SRQ["SearchRulesQuery\n(record: Query, Domain, TopResults)"]
            GRC["GetRuleCollectionHandler\n(CQRS Query Handler)"]
            GRCQ["GetRuleCollectionQuery\n(record: FilePath, DisclosureLevel)"]
            LRC["ListRuleCollectionsHandler\n(CQRS Query Handler)"]
        end

        subgraph INFRA["Infrastructure"]
            PARSER["AgentFrameworkToolParser\n(parses YAML frontmatter + body)"]
            KBOPT["KnowledgeBaseOptions\n(config: Path to /knowledgebase)"]
        end

        subgraph DOMAIN["Domain Models"]
            RCD["RuleCollectionDocument\n(Layer 1: metadata, tags, domain)"]
            RI["RuleItem\n(Layer 2: Summary, AcceptanceCriteria)"]
            RD["RuleDetails\n(Layer 3: Gherkin, Examples, Exceptions)"]
            RQR["RuleQueryResult\n(AnswerSummary, Confidence, TopSources)"]
            MF["MatchedFragment\n(Quote, RelevanceScore, SourcePath)"]
        end
    end

    subgraph KB["Knowledge Base"]
        MD["Markdown Files\n(YAML frontmatter + rule body)"]
    end

    SF -- "POST /rules/search\n{query, domain}" --> AGENT
    TOOL -- "SearchRulesQuery" --> SRH
    SRH -- "GetRuleCollectionQuery\n(Minimal)" --> GRC
    GRC -- "ParseRuleCollectionMinimal/Standard/Complete" --> PARSER
    LRC -- "ParseRuleCollectionMinimal" --> PARSER
    PARSER -- "reads" --> MD
    PARSER -- "produces" --> RCD
    RCD -- "contains" --> RI
    RI -- "lazy Layer 3" --> RD
    SRH -- "produces" --> RQR
    RQR -- "fragments" --> MF

    subgraph PD["Progressive Disclosure Layers"]
        L1["Layer 1 — Minimal\nfrontmatter: id, title, tags, domain\nParseRuleCollectionMinimal()"]
        L2["Layer 2 — Standard\n+ Summary, AcceptanceCriteria\nParseRuleCollectionStandard()"]
        L3["Layer 3 — Complete\n+ Gherkin, Examples, Exceptions, Notes\nParseRuleCollectionComplete()"]
        L1 --> L2 --> L3
    end

    PARSER -- "respects DisclosureLevel" --> PD
    AGENT -- "RuleQueryResult" --> SF
    SF -- "streams structured answer" --> TAI
```

## Flow Summary

1. **User types a question** → `Chat UI` → `TanStack AI` (`useChat`)
2. **TanStack AI calls** the BFF **Server Function**, which POSTs to the .NET API
3. **MAF Agent** receives the request, picks the `SearchRules` tool based on LLM reasoning
4. **Tool invokes** `SearchRulesHandler` with a `SearchRulesQuery` (CQRS record)
5. **Handler delegates** to `GetRuleCollectionHandler` / `AgentFrameworkToolParser` per file
6. **Parser applies the `DisclosureLevel`** — `Minimal` for fast scan, `Standard` for summaries, `Complete` for full details — reading the markdown KB files
7. **Domain models** (`RuleCollectionDocument` → `RuleItem` → `RuleDetails`) carry only what was requested
8. **`RuleQueryResult`** (with `AnswerSummary`, `Confidence`, `SupportingMatches`) flows back up to the Agent → API → Server Function → TanStack AI → streamed to the UI
