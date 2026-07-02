import { createFileRoute } from "@tanstack/react-router";

import { Chat } from "#/features/chat/ui/chat";

export const Route = createFileRoute("/")({ component: App });

function App() {
	return (
		<div className="mx-auto w-full max-w-5xl px-4 py-10">
			<div className="grid gap-6">
				<Chat />
			</div>
		</div>
	);
}
