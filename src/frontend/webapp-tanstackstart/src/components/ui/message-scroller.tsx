import * as React from "react";

import { Button } from "#/components/ui/button.tsx";
import { cn } from "#/lib/utils.ts";

type MessageScrollerContextValue = {
	viewportRef: React.RefObject<HTMLDivElement | null>;
	isAtBottom: boolean;
	refreshBottomState: () => void;
	scrollToBottom: () => void;
};

const MessageScrollerContext =
	React.createContext<MessageScrollerContextValue | null>(null);

const useMessageScroller = () => {
	const context = React.useContext(MessageScrollerContext);
	if (!context) {
		throw new Error(
			"MessageScroller components must be used within MessageScrollerProvider.",
		);
	}

	return context;
};

function MessageScrollerProvider({
	children,
	autoStickToBottom = true,
}: {
	children: React.ReactNode;
	autoStickToBottom?: boolean;
}) {
	const viewportRef = React.useRef<HTMLDivElement>(null);
	const [isAtBottom, setIsAtBottom] = React.useState(true);

	const refreshBottomState = React.useCallback(() => {
		const viewport = viewportRef.current;
		if (!viewport) {
			return;
		}

		const distanceFromBottom =
			viewport.scrollHeight - viewport.scrollTop - viewport.clientHeight;
		setIsAtBottom(distanceFromBottom <= 8);
	}, []);

	const scrollToBottom = React.useCallback(() => {
		const viewport = viewportRef.current;
		if (!viewport) {
			return;
		}

		viewport.scrollTo({ top: viewport.scrollHeight, behavior: "smooth" });
	}, []);

	React.useEffect(() => {
		if (!autoStickToBottom) {
			return;
		}

		const viewport = viewportRef.current;
		if (!viewport) {
			return;
		}

		const observer = new MutationObserver(() => {
			if (isAtBottom) {
				scrollToBottom();
			}
		});

		observer.observe(viewport, {
			childList: true,
			subtree: true,
		});

		return () => {
			observer.disconnect();
		};
	}, [autoStickToBottom, isAtBottom, scrollToBottom]);

	const value = React.useMemo(
		() => ({ viewportRef, isAtBottom, refreshBottomState, scrollToBottom }),
		[isAtBottom, refreshBottomState, scrollToBottom],
	);

	return (
		<MessageScrollerContext.Provider value={value}>
			{children}
		</MessageScrollerContext.Provider>
	);
}

function MessageScroller({
	className,
	children,
}: React.ComponentProps<"section">) {
	return (
		<section
			className={cn(
				"relative flex min-h-0 flex-1 flex-col rounded-xl border bg-background/70",
				className,
			)}
		>
			{children}
		</section>
	);
}

function MessageScrollerViewport({
	className,
	children,
}: React.ComponentProps<"div">) {
	const { viewportRef, refreshBottomState } = useMessageScroller();

	return (
		<div
			ref={viewportRef}
			onScroll={refreshBottomState}
			className={cn("min-h-0 flex-1 overflow-y-auto p-4", className)}
		>
			{children}
		</div>
	);
}

function MessageScrollerContent({
	className,
	children,
}: React.ComponentProps<"div">) {
	return <div className={cn("flex flex-col gap-4", className)}>{children}</div>;
}

function MessageScrollerButton({
	className,
	children,
	...props
}: React.ComponentProps<typeof Button>) {
	const { isAtBottom, scrollToBottom } = useMessageScroller();

	if (isAtBottom) {
		return null;
	}

	return (
		<div className="pointer-events-none absolute right-4 bottom-4 z-10">
			<Button
				className={cn(
					"pointer-events-auto rounded-full shadow-lg shadow-black/10",
					className,
				)}
				size="sm"
				variant="secondary"
				onClick={scrollToBottom}
				{...props}
			>
				{children ?? "Jump to latest"}
			</Button>
		</div>
	);
}

export {
	MessageScroller,
	MessageScrollerButton,
	MessageScrollerContent,
	MessageScrollerProvider,
	MessageScrollerViewport,
};
