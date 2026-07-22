import { createFileRoute } from "@tanstack/react-router";

import { Chat } from "#/features/chat/ui/chat";

export const Route = createFileRoute("/")({ component: App });

function App() {
	return (
		// TODO: Add a navigation to change AI Agent Chat from BRULS to procedures.
		// Still Mem but with a different AI Agent Chat for procedures.
		<div className="mx-auto w-full max-w-5xl px-4 py-10">
			<div className="grid gap-6">
				<Chat />
				<footer className="pt-6 text-center text-sm text-muted-foreground">
					<span>karljucutan</span>
				</footer>
			</div>
		</div>
	);
}
