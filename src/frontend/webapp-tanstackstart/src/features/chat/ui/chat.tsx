import { fetchServerSentEvents, useChat } from "@tanstack/ai-react";
import { SendIcon } from "lucide-react";
import { type SubmitEventHandler, useState } from "react";
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
import { chatStreamFn } from "#/features/chat/server/send-chat-message.ts";
import { cn } from "#/lib/utils.ts";

export function Chat() {
	const [input, setInput] = useState("");
	const { messages, sendMessage, isLoading } = useChat({
		connection: fetchServerSentEvents(chatStreamFn.url),
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

	return (
		<Card className="shadow-sm">
			<CardHeader className="gap-3">
				<CardTitle className="text-3xl font-semibold tracking-tight md:text-4xl">
					Rules Assistant
				</CardTitle>
			</CardHeader>
			<CardContent className="grid gap-4">
				<MessageScrollerProvider>
					<div className="grid gap-4">
						<MessageScroller className="h-[52vh] min-h-80">
							<MessageScrollerViewport>
								<MessageScrollerContent>
									{messages.length === 0 ? (
										<div className="rounded-xl border border-dashed border-border bg-muted/40 p-4 text-sm text-muted-foreground">
											Start by asking a specific business-rule question such as
											eligibility, down payment, underwriting, or billing
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
														item.role === "assistant" ? "secondary" : "outline"
													}
												>
													{item.role === "assistant" ? "Assistant" : "You"}
												</Badge>
											</div>

											<p className="m-0 whitespace-pre-wrap text-sm leading-relaxed">
												{item.parts.map((part) => {
													const content =
														part.type === "text" || part.type === "thinking"
															? part.content
															: "";

													if (part.type === "text") {
														return (
															<span key={`${item.id}-${part.type}-${content}`}>
																{part.content}
															</span>
														);
													}

													if (part.type === "thinking") {
														return (
															<span
																key={`${item.id}-${part.type}-${content}`}
																className="block italic text-muted-foreground"
															>
																Thinking: {part.content}
															</span>
														);
													}

													return null;
												})}
											</p>
										</div>
									))}
								</MessageScrollerContent>
							</MessageScrollerViewport>
							<MessageScrollerButton />
						</MessageScroller>

						<form className="grid gap-3" onSubmit={handleSubmit}>
							<Textarea
								value={input}
								onChange={(event) => setInput(event.target.value)}
								placeholder="Ask a business-rules question..."
								rows={4}
								disabled={isLoading}
							/>
							<Button
								disabled={isLoading || input.trim().length === 0}
								type="submit"
							>
								<SendIcon data-icon="inline-start" />
								{isLoading ? "Sending" : "Send"}
							</Button>
						</form>
					</div>
				</MessageScrollerProvider>
			</CardContent>
		</Card>
	);
}
