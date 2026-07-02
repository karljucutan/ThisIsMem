import { chatParamsFromRequestBody } from "@tanstack/ai";
import { createServerFn } from "@tanstack/react-start";

// This server function is not being used.
export const chatStreamFn = createServerFn({ method: "POST" }).handler(
	async ({ data }) => {
		const params = await chatParamsFromRequestBody(data);
		const backendBaseUrl = process.env.BACKEND_API_BASE_URL;
		const backendUrl = new URL("/api/agent", backendBaseUrl);

		const response = await fetch(backendUrl, {
			method: "POST",
			headers: {
				"Content-Type": "application/json",
			},
			body: JSON.stringify(params),
		});

		if (!response.body) {
			throw new Error("Failed to get stream from .NET backend");
		}

		return new Response(response.body, {
			headers: {
				"Content-Type": "text/event-stream",
				"Cache-Control": "no-cache",
				Connection: "keep-alive",
			},
		});
	},
);
