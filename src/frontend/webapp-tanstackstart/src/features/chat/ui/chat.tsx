import { fetchServerSentEvents, useChat } from "@tanstack/ai-react";
import { ArrowDownIcon, ChevronRightIcon, SendIcon } from "lucide-react";
import { type KeyboardEvent, type SubmitEventHandler, useState } from "react";
import { ModeToggle } from "#/components/mode-toggle";
import { Badge } from "#/components/ui/badge.tsx";
import { Button } from "#/components/ui/button.tsx";
import {
	Card,
	CardContent,
	CardHeader,
	CardTitle,
} from "#/components/ui/card.tsx";
import {
	MessageScroller,
	MessageScrollerButton,
	MessageScrollerContent,
	MessageScrollerProvider,
	MessageScrollerViewport,
} from "#/components/ui/message-scroller.tsx";
import { Textarea } from "#/components/ui/textarea.tsx";
import { cn } from "#/lib/utils.ts";

const backendBaseUrl = import.meta.env.VITE_BACKEND_API_BASE_URL;
const agentStreamUrl = new URL("/api/agent", backendBaseUrl).toString();

export function Chat() {
	const [input, setInput] = useState("");
	const { messages, sendMessage, isLoading } = useChat({
		connection: fetchServerSentEvents(agentStreamUrl),
	});

	const handleSubmit: SubmitEventHandler<HTMLFormElement> = async (event) => {
		event.preventDefault();
		const trimmed = input.trim();
		if (!trimmed || isLoading) {
			return;
		}

		await sendMessage(trimmed);
		setInput("");
	};

	const handleKeyDown = async (event: KeyboardEvent<HTMLTextAreaElement>) => {
		if (
			event.key === "Enter" &&
			!event.shiftKey &&
			!event.ctrlKey &&
			!event.altKey &&
			!event.metaKey
		) {
			event.preventDefault();
			const trimmed = input.trim();
			if (!trimmed || isLoading) return;
			await sendMessage(trimmed);
			setInput("");
		}
	};

	return (
		<Card className="shadow-sm">
			<CardHeader className="gap-3">
				<div className="flex items-center justify-between">
					<Badge className="w-fit" variant="secondary">
						AI Assistant
					</Badge>
					<ModeToggle />
				</div>
				<CardTitle className="text-3xl font-semibold tracking-tight md:text-4xl">
					This is Mem
				</CardTitle>
				<p className="max-w-2xl text-sm text-muted-foreground md:text-base">
					Ask Mem anything about the rules, and it will help you find the right
					answer fast.
				</p>
			</CardHeader>
			<CardContent className="grid gap-4">
				<MessageScrollerProvider>
					<MessageScroller className="h-[64vh] min-h-96">
						<MessageScrollerViewport>
							<MessageScrollerContent className="pb-1">
								{messages.length === 0 ? (
									<div className="rounded-xl border border-dashed border-border bg-muted/40 p-4 text-sm text-muted-foreground">
										Start with a question like: “What's the minimum down
										payment?”, “Can this be backdated?”, or “What stops
										underwriting?”.
									</div>
								) : null}

								{messages.map((item, msgIndex) => {
									const stableMessageId = item.id ?? `msg-${msgIndex}`;
									return (
										<div
											key={stableMessageId}
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
														item.role === "assistant" ? "secondary" : "outline"
													}
												>
													{item.role === "assistant" ? "Mem" : "You"}
												</Badge>
											</div>

											<p className="m-0 whitespace-pre-wrap wrap-anywhere text-sm leading-relaxed">
												{item.parts.map((part, partIndex) => {
													const partKey = `${stableMessageId}-part-${partIndex}-${part.type}`;

													if (part.type === "text") {
														return <span key={partKey}>{part.content}</span>;
													}

													if (part.type === "thinking") {
														return (
															<details
																key={partKey}
																className="group my-2 rounded-lg border border-border/70 bg-muted/30 px-3 py-2 text-muted-foreground"
																open={isLoading}
															>
																<summary className="cursor-pointer list-none text-sm font-medium italic text-muted-foreground outline-none">
																	<span className="inline-flex items-center gap-1">
																		<span>
																			{isLoading ? "Thinking" : "Reasoning"}
																		</span>
																		<ChevronRightIcon className="h-3 w-3 transition-transform duration-200 group-open:rotate-90" />
																	</span>
																</summary>
																<span className="mt-2 block whitespace-pre-wrap wrap-anywhere text-sm leading-relaxed">
																	{part.content}
																</span>
															</details>
														);
													}

													return null;
												})}
											</p>
										</div>
									);
								})}
							</MessageScrollerContent>
						</MessageScrollerViewport>

						<form
							className="mt-auto flex items-center gap-3 border-t border-border bg-background/95 p-4 backdrop-blur"
							onSubmit={handleSubmit}
						>
							<Textarea
								className="min-h-16 flex-1 resize-none"
								value={input}
								onChange={(event) => setInput(event.target.value)}
								onKeyDown={handleKeyDown}
								placeholder="Ask a business-rules question..."
								rows={2}
								disabled={isLoading}
							/>
							<Button
								className="shrink-0"
								disabled={isLoading || input.trim().length === 0}
								type="submit"
							>
								<SendIcon data-icon="inline-start" />
								{isLoading ? "Sending" : "Send"}
							</Button>
						</form>

						<MessageScrollerButton className="mb-24" size="icon-sm">
							<ArrowDownIcon />
						</MessageScrollerButton>
					</MessageScroller>
				</MessageScrollerProvider>
			</CardContent>
		</Card>
	);
}
