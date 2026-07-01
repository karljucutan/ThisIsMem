export type Confidence = "Low" | "Medium" | "High" | "Unknown";

export type SourceReference = {
	ruleId: string;
	title: string;
	domain: string;
	filePath: string;
};

export type SupportingMatch = {
	quote: string;
	relevanceScore: number;
	sourcePath: string;
	heading: string;
	section: string;
};

export type NormalizedChatResponse = {
	answer: string;
	confidence: Confidence;
	topSources: SourceReference[];
	rationale?: string;
	supportingMatches: SupportingMatch[];
	relatedRuleIds: string[];
	transport: "agui" | "rules-search";
	latencyMs: number;
	rawResponse?: unknown;
};

export type SendChatInput = {
	message: string;
	domain?: string;
	topResults?: number;
	threadId?: string;
	runId?: string;
};
