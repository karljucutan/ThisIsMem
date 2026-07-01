import { createFileRoute } from "@tanstack/react-router";
import { AlertCircleIcon, RefreshCwIcon, SendIcon } from "lucide-react";
import * as React from "react";

import {
	Accordion,
	AccordionContent,
	AccordionItem,
	AccordionTrigger,
} from "#/components/ui/accordion.tsx";
import { Alert, AlertDescription, AlertTitle } from "#/components/ui/alert.tsx";
import { Badge } from "#/components/ui/badge.tsx";
import { Button } from "#/components/ui/button.tsx";
import {
	Card,
	CardContent,
	CardDescription,
	CardHeader,
	CardTitle,
} from "#/components/ui/card.tsx";
import { Input } from "#/components/ui/input.tsx";
import {
	MessageScroller,
	MessageScrollerButton,
	MessageScrollerContent,
	MessageScrollerProvider,
	MessageScrollerViewport,
} from "#/components/ui/message-scroller.tsx";
import { Separator } from "#/components/ui/separator.tsx";
import { Spinner } from "#/components/ui/spinner.tsx";
import { Textarea } from "#/components/ui/textarea.tsx";
import { sendChatMessage } from "#/features/chat/chat.ts";
import type {
	NormalizedChatResponse,
	SendChatInput,
} from "#/features/chat/types.ts";
import { cn } from "#/lib/utils.ts";

export const Route = createFileRoute("/")({ component: Home });

type UiMessage = {
	id: string;
	role: "user" | "assistant";
	content: string;
	response?: NormalizedChatResponse;
};

const createId = () =>
	typeof crypto !== "undefined" && "randomUUID" in crypto
		? crypto.randomUUID()
		: `${Date.now()}-${Math.random().toString(16).slice(2)}`;

function Home() {
	const [message, setMessage] = React.useState("");
	const [domain, setDomain] = React.useState("");
	const [messages, setMessages] = React.useState<UiMessage[]>([]);
	const [isLoading, setIsLoading] = React.useState(false);
	const [error, setError] = React.useState<string | null>(null);
	const [lastRequest, setLastRequest] = React.useState<SendChatInput | null>(
		null,
	);
	const [threadId] = React.useState(() => createId().replace(/-/g, ""));

	const runSend = React.useCallback(
		async (input: SendChatInput) => {
			setIsLoading(true);
			setError(null);
			setLastRequest(input);

			const userMessageId = createId();
			setMessages((current) => [
				...current,
				{
					id: userMessageId,
					role: "user",
					content: input.message,
				},
			]);

			try {
				const response = (await sendChatMessage({
					data: {
						...input,
						threadId,
						runId: createId().replace(/-/g, ""),
					},
				})) as NormalizedChatResponse;

				setMessages((current) => [
					...current,
					{
						id: createId(),
						role: "assistant",
						content: response.answer,
						response,
					},
				]);
				setMessage("");
			} catch (caught) {
				setError(
					caught instanceof Error
						? caught.message
						: "Failed to send message. Please retry.",
				);
			} finally {
				setIsLoading(false);
			}
		},
		[threadId],
	);

	const handleSubmit: React.SubmitEventHandler<HTMLFormElement> = async (
		event,
	) => {
		event.preventDefault();
		const trimmed = message.trim();
		if (!trimmed || isLoading) {
			return;
		}

		await runSend({
			message: trimmed,
			domain: domain.trim() || undefined,
			topResults: 5,
		});
	};

	const handleRetry = async () => {
		if (!lastRequest || isLoading) {
			return;
		}

		await runSend(lastRequest);
	};

	// TODO: move this to chat feature component. in VSA feature slice, add folder for layerered architecture: ui, server
	return (
		<div className="mx-auto w-full max-w-5xl px-4 py-8 md:py-12">
			<div className="grid gap-6">
				<Card className="shadow-sm">
					<CardHeader className="gap-3">
						<CardTitle className="text-3xl font-semibold tracking-tight md:text-4xl">
							Rules Assistant
						</CardTitle>
						<CardDescription className="text-sm text-muted-foreground md:text-base">
							TanStack Start BFF calling the backend AGUI agent endpoint, with
							progressive disclosure for rule traceability.
						</CardDescription>
					</CardHeader>
					<CardContent className="grid gap-4">
						<form className="grid gap-3" onSubmit={handleSubmit}>
							<Textarea
								value={message}
								onChange={(event) => setMessage(event.target.value)}
								placeholder="Ask a business-rules question..."
								rows={4}
								disabled={isLoading}
							/>
							<div className="grid gap-3 md:grid-cols-[1fr_auto]">
								<Input
									value={domain}
									onChange={(event) => setDomain(event.target.value)}
									placeholder="Optional domain filter (e.g. Billing)"
									disabled={isLoading}
								/>
								<Button
									disabled={isLoading || message.trim().length === 0}
									type="submit"
								>
									{isLoading ? (
										<>
											<Spinner data-icon="inline-start" />
											Thinking
										</>
									) : (
										<>
											<SendIcon data-icon="inline-start" />
											Send
										</>
									)}
								</Button>
							</div>
						</form>

						{error ? (
							<Alert variant="destructive">
								<AlertCircleIcon />
								<AlertTitle>Request failed</AlertTitle>
								<AlertDescription>
									<p>{error}</p>
									<div className="pt-2">
										<Button
											onClick={handleRetry}
											size="sm"
											type="button"
											variant="outline"
										>
											<RefreshCwIcon data-icon="inline-start" />
											Retry last message
										</Button>
									</div>
								</AlertDescription>
							</Alert>
						) : null}

						<MessageScrollerProvider>
							<MessageScroller className="h-[52vh] min-h-80">
								<MessageScrollerViewport>
									<MessageScrollerContent>
										{messages.length === 0 ? (
											<div className="rounded-xl border border-dashed border-border bg-muted/40 p-4 text-sm text-muted-foreground">
												Start by asking a specific business-rule question such
												as eligibility, down payment, underwriting, or billing
												timelines.
											</div>
										) : null}

										{messages.map((item) => (
											<div
												key={item.id}
												className={cn(
													"max-w-3xl rounded-xl border p-4 shadow-sm",
													item.role === "user"
														? "ml-auto border-border bg-muted"
														: "mr-auto border-border bg-card",
												)}
											>
												<div className="mb-2 flex items-center gap-2">
													<Badge
														variant={
															item.role === "assistant"
																? "secondary"
																: "outline"
														}
													>
														{item.role === "assistant" ? "Assistant" : "You"}
													</Badge>
													{item.response ? (
														<Badge variant="outline">
															{item.response.confidence} confidence
														</Badge>
													) : null}
												</div>

												<p className="m-0 whitespace-pre-wrap text-sm leading-relaxed">
													{item.content}
												</p>

												{item.response ? (
													<>
														<Separator className="my-4" />
														<Accordion collapsible type="single">
															<AccordionItem value="sources">
																<AccordionTrigger>
																	Top Supporting Rules (
																	{item.response.topSources.length})
																</AccordionTrigger>
																<AccordionContent>
																	<div className="flex flex-col gap-3">
																		{item.response.topSources.length === 0 ? (
																			<p className="m-0 text-sm text-muted-foreground">
																				No structured sources were returned.
																			</p>
																		) : (
																			item.response.topSources.map((source) => (
																				<div
																					key={`${item.id}-${source.ruleId}-${source.filePath}`}
																					className="rounded-md border border-(--line) p-3"
																				>
																					<p className="m-0 text-sm font-semibold">
																						{source.title || source.ruleId}
																					</p>
																					<p className="m-0 text-xs text-muted-foreground">
																						{source.domain || "Unknown domain"}{" "}
																						•{" "}
																						{source.filePath ||
																							"Unknown source path"}
																					</p>
																				</div>
																			))
																		)}
																	</div>
																</AccordionContent>
															</AccordionItem>

															<AccordionItem value="rationale">
																<AccordionTrigger>
																	Rationale & matched excerpts
																</AccordionTrigger>
																<AccordionContent>
																	<div className="flex flex-col gap-3">
																		<p className="m-0 text-sm text-muted-foreground">
																			{item.response.rationale ||
																				"No rationale text returned."}
																		</p>
																		{item.response.supportingMatches.map(
																			(match) => (
																				<div
																					key={`${item.id}-${match.sourcePath}-${match.section}-${match.heading}-${match.quote}`}
																					className="rounded-md border border-(--line) p-3"
																				>
																					<p className="m-0 text-sm">
																						{match.quote}
																					</p>
																					<p className="m-0 pt-1 text-xs text-muted-foreground">
																						{match.heading} • {match.section} •
																						relevance{" "}
																						{match.relevanceScore.toFixed(2)}
																					</p>
																				</div>
																			),
																		)}
																	</div>
																</AccordionContent>
															</AccordionItem>

															<AccordionItem value="meta">
																<AccordionTrigger>
																	Request metadata
																</AccordionTrigger>
																<AccordionContent>
																	<div className="flex flex-col gap-2 text-xs text-muted-foreground">
																		<p className="m-0">
																			Transport: {item.response.transport} •
																			latency: {item.response.latencyMs}ms
																		</p>
																		<p className="m-0">
																			Related rules:{" "}
																			{item.response.relatedRuleIds.join(
																				", ",
																			) || "None"}
																		</p>
																	</div>
																</AccordionContent>
															</AccordionItem>
														</Accordion>
													</>
												) : null}
											</div>
										))}
									</MessageScrollerContent>
								</MessageScrollerViewport>
								<MessageScrollerButton />
							</MessageScroller>
						</MessageScrollerProvider>
					</CardContent>
				</Card>
			</div>
		</div>
	);
}
