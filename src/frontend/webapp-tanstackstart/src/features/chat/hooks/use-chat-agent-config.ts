import { useMemo, useState } from "react";

export type ChatAgentId = "thisismem" | "nursejoy";

export interface ChatAgentConfig {
	id: ChatAgentId;
	label: string;
	title: string;
	description: string;
	starterPrompt: string;
	placeholder: string;
	assistantLabel: string;
	streamUrl: string;
}

const backendBaseUrl = import.meta.env.VITE_BACKEND_API_BASE_URL;

const agentConfigs: Record<ChatAgentId, Omit<ChatAgentConfig, "id">> = {
	thisismem: {
		label: "ThisIsMem",
		title: "ThisIsMem",
		description:
			"Ask Mem anything about the rules, and it will help you find the right answer fast.",
		starterPrompt: `Start with a question like: "What's the minimum down payment?", "Can this be backdated?", or "What stops underwriting?".`,
		placeholder: "Ask a business-rules question...",
		assistantLabel: "Mem",
		streamUrl: new URL("/api/agent", backendBaseUrl).toString(),
	},
	nursejoy: {
		label: "NurseJoy",
		title: "NurseJoy",
		description:
			"Access clinical guidelines and protocols to support your healthcare practice. Search protocols, procedures, and best practices across critical care, medications, and more.",
		starterPrompt: `Start with a question like: "What are the indications for peripheral noradrenaline?", "When do we escalate care?", or "What are the dosing guidelines for this medication?".`,
		placeholder: "Search clinical guidelines and protocols...",
		assistantLabel: "NurseJoy",
		streamUrl: new URL("/api/rag/agent/nursejoy", backendBaseUrl).toString(),
	},
};

export function useChatAgentConfig(defaultAgent: ChatAgentId = "thisismem") {
	const [selectedAgentId, setSelectedAgentId] =
		useState<ChatAgentId>(defaultAgent);

	const selectedAgent = useMemo<ChatAgentConfig>(
		() => ({
			id: selectedAgentId,
			...agentConfigs[selectedAgentId],
		}),
		[selectedAgentId],
	);

	const agentOptions = useMemo(
		() =>
			(Object.keys(agentConfigs) as ChatAgentId[]).map((id) => ({
				id,
				label: agentConfigs[id].label,
			})),
		[],
	);

	return {
		agentOptions,
		selectedAgent,
		selectedAgentId,
		setSelectedAgentId,
	};
}
