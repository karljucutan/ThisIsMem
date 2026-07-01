import { createServerFn } from "@tanstack/react-start";
import { z } from "zod";

import type {
	Confidence,
	NormalizedChatResponse,
	SendChatInput,
	SourceReference,
	SupportingMatch,
} from "#/features/chat/types.ts";

const sendChatInputSchema = z.object({
	message: z.string().trim().min(1, "Message is required"),
	domain: z.string().trim().min(1).optional(),
	topResults: z.number().int().min(1).max(20).default(5),
	threadId: z.string().trim().min(1).optional(),
	runId: z.string().trim().min(1).optional(),
});

const timeoutMs = 20_000;

const safeJson = async (response: Response): Promise<unknown> => {
	try {
		return await response.json();
	} catch {
		return null;
	}
};

const normalizeConfidence = (value: unknown): Confidence => {
	if (value === "Low" || value === "Medium" || value === "High") {
		return value;
	}

	if (typeof value === "number") {
		if (value <= 1) {
			return "Low";
		}

		if (value === 2) {
			return "Medium";
		}

		if (value >= 3) {
			return "High";
		}
	}

	return "Unknown";
};

const normalizeTopSources = (value: unknown): SourceReference[] => {
	if (!Array.isArray(value)) {
		return [];
	}

	return value
		.map((item) => {
			if (typeof item !== "object" || item === null) {
				return null;
			}

			const candidate = item as Record<string, unknown>;
			return {
				ruleId:
					typeof candidate.RuleId === "string"
						? candidate.RuleId
						: typeof candidate.ruleId === "string"
							? candidate.ruleId
							: "",
				title:
					typeof candidate.Title === "string"
						? candidate.Title
						: typeof candidate.title === "string"
							? candidate.title
							: "",
				domain:
					typeof candidate.Domain === "string"
						? candidate.Domain
						: typeof candidate.domain === "string"
							? candidate.domain
							: "",
				filePath:
					typeof candidate.FilePath === "string"
						? candidate.FilePath
						: typeof candidate.filePath === "string"
							? candidate.filePath
							: "",
			};
		})
		.filter((item): item is SourceReference => item !== null);
};

const normalizeSupportingMatches = (value: unknown): SupportingMatch[] => {
	if (!Array.isArray(value)) {
		return [];
	}

	return value
		.map((item) => {
			if (typeof item !== "object" || item === null) {
				return null;
			}

			const candidate = item as Record<string, unknown>;
			return {
				quote:
					typeof candidate.Quote === "string"
						? candidate.Quote
						: typeof candidate.quote === "string"
							? candidate.quote
							: "",
				relevanceScore:
					typeof candidate.RelevanceScore === "number"
						? candidate.RelevanceScore
						: typeof candidate.relevanceScore === "number"
							? candidate.relevanceScore
							: 0,
				sourcePath:
					typeof candidate.SourcePath === "string"
						? candidate.SourcePath
						: typeof candidate.sourcePath === "string"
							? candidate.sourcePath
							: "",
				heading:
					typeof candidate.Heading === "string"
						? candidate.Heading
						: typeof candidate.heading === "string"
							? candidate.heading
							: "",
				section:
					typeof candidate.Section === "string"
						? candidate.Section
						: typeof candidate.section === "string"
							? candidate.section
							: "",
			};
		})
		.filter((item): item is SupportingMatch => item !== null);
};

const parseAssistantText = (value: unknown): string => {
	if (typeof value === "string") {
		return value;
	}

	if (!Array.isArray(value)) {
		return "";
	}

	const textParts = value
		.map((item) => {
			if (typeof item === "string") {
				return item;
			}

			if (typeof item !== "object" || item === null) {
				return "";
			}

			const record = item as Record<string, unknown>;
			if (typeof record.text === "string") {
				return record.text;
			}

			if (typeof record.content === "string") {
				return record.content;
			}

			return "";
		})
		.filter((part) => part.length > 0);

	return textParts.join("\n").trim();
};

const normalizeFromRulesSearch = (
	json: unknown,
	latencyMs: number,
): NormalizedChatResponse | null => {
	if (!Array.isArray(json) || json.length === 0) {
		return null;
	}

	const first = json[0];
	if (typeof first !== "object" || first === null) {
		return null;
	}

	const record = first as Record<string, unknown>;
	const answer =
		typeof record.AnswerSummary === "string"
			? record.AnswerSummary
			: typeof record.answerSummary === "string"
				? record.answerSummary
				: "No matching rule summary returned.";

	const related = Array.isArray(record.RelatedRuleIds)
		? record.RelatedRuleIds
		: Array.isArray(record.relatedRuleIds)
			? record.relatedRuleIds
			: [];

	return {
		answer,
		confidence: normalizeConfidence(record.Confidence ?? record.confidence),
		topSources: normalizeTopSources(record.TopSources ?? record.topSources),
		rationale:
			typeof record.Rationale === "string"
				? record.Rationale
				: typeof record.rationale === "string"
					? record.rationale
					: undefined,
		supportingMatches: normalizeSupportingMatches(
			record.SupportingMatches ?? record.supportingMatches,
		),
		relatedRuleIds: related.filter(
			(item): item is string => typeof item === "string",
		),
		transport: "rules-search",
		latencyMs,
		rawResponse: json,
	};
};

const normalizeFromAgui = (
	json: unknown,
	latencyMs: number,
): NormalizedChatResponse => {
	if (Array.isArray(json)) {
		const fromSearch = normalizeFromRulesSearch(json, latencyMs);
		if (fromSearch) {
			return fromSearch;
		}
	}

	if (typeof json !== "object" || json === null) {
		return {
			answer: "The assistant response was empty.",
			confidence: "Unknown",
			topSources: [],
			supportingMatches: [],
			relatedRuleIds: [],
			transport: "agui",
			latencyMs,
			rawResponse: json,
		};
	}

	const record = json as Record<string, unknown>;
	const messages = Array.isArray(record.messages) ? record.messages : [];
	const lastAssistant = [...messages].reverse().find((item) => {
		if (typeof item !== "object" || item === null) {
			return false;
		}

		const message = item as Record<string, unknown>;
		return message.role === "assistant";
	}) as Record<string, unknown> | undefined;

	const fallbackAnswer =
		typeof record.answer === "string"
			? record.answer
			: typeof record.outputText === "string"
				? record.outputText
				: "No answer returned by the agent.";

	const answer = parseAssistantText(lastAssistant?.content) || fallbackAnswer;

	return {
		answer,
		confidence: "Unknown",
		topSources: normalizeTopSources(record.topSources),
		rationale:
			typeof record.rationale === "string" ? record.rationale : undefined,
		supportingMatches: normalizeSupportingMatches(record.supportingMatches),
		relatedRuleIds: Array.isArray(record.relatedRuleIds)
			? record.relatedRuleIds.filter(
					(item): item is string => typeof item === "string",
				)
			: [],
		transport: "agui",
		latencyMs,
		rawResponse: json,
	};
};

const callRulesSearchFallback = async (
	baseUrl: string,
	input: SendChatInput,
): Promise<NormalizedChatResponse> => {
	const fallbackController = new AbortController();
	const timeoutHandle = setTimeout(() => fallbackController.abort(), timeoutMs);
	const startedAt = Date.now();

	try {
		const fallbackResponse = await fetch(`${baseUrl}/api/rules/search`, {
			method: "POST",
			headers: {
				"Content-Type": "application/json",
			},
			body: JSON.stringify({
				query: input.message,
				domain: input.domain,
				topResults: input.topResults,
			}),
			signal: fallbackController.signal,
		});

		const fallbackJson = await safeJson(fallbackResponse);
		if (!fallbackResponse.ok) {
			throw new Error("Search endpoint did not return success.");
		}

		const latencyMs = Date.now() - startedAt;
		const normalized = normalizeFromRulesSearch(fallbackJson, latencyMs);
		if (!normalized) {
			return {
				answer: "No business rules found matching the query.",
				confidence: "Unknown",
				topSources: [],
				supportingMatches: [],
				relatedRuleIds: [],
				transport: "rules-search",
				latencyMs,
				rawResponse: fallbackJson,
			};
		}

		return normalized;
	} finally {
		clearTimeout(timeoutHandle);
	}
};

export const sendChatMessage = createServerFn({ method: "POST" })
	.validator((data: SendChatInput) => sendChatInputSchema.parse(data))
	.handler(async ({ data }) => {
		const baseUrl = process.env.BACKEND_API_BASE_URL ?? "http://localhost:5234";
		const requestId = data.runId ?? crypto.randomUUID().replace(/-/g, "");
		const threadId = data.threadId ?? crypto.randomUUID().replace(/-/g, "");

		const controller = new AbortController();
		const timeoutHandle = setTimeout(() => controller.abort(), timeoutMs);
		const startedAt = Date.now();

		try {
			const response = await fetch(`${baseUrl}/api/agent`, {
				method: "POST",
				headers: {
					"Content-Type": "application/json",
				},
				body: JSON.stringify({
					threadId,
					runId: requestId,
					state: null,
					messages: [
						{
							role: "user",
							content: data.message,
						},
					],
					tools: [],
					context: [],
					forwardedProps: {
						domain: data.domain,
						topResults: data.topResults,
					},
				}),
				signal: controller.signal,
			});

			const json = await safeJson(response);
			if (!response.ok) {
				return await callRulesSearchFallback(baseUrl, data);
			}

			const latencyMs = Date.now() - startedAt;
			return normalizeFromAgui(json, latencyMs);
		} catch {
			return await callRulesSearchFallback(baseUrl, data);
		} finally {
			clearTimeout(timeoutHandle);
		}
	});
