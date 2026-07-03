import { MoonStarIcon, SunMediumIcon } from "lucide-react";
import { useTheme } from "#/components/theme-provider.tsx";
import { Button } from "#/components/ui/button.tsx";

export function ModeToggle() {
	const { theme, setTheme } = useTheme();
	const isDark = theme === "dark";
	const nextTheme = isDark ? "light" : "dark";
	const ThemeIcon = isDark ? SunMediumIcon : MoonStarIcon;

	return (
		<Button
			aria-label={`Switch to ${nextTheme} mode`}
			aria-pressed={isDark}
			onClick={() => setTheme(nextTheme)}
			size="icon"
			variant="outline"
			title={`Switch to ${nextTheme} mode`}
		>
			<ThemeIcon data-icon="inline-start" />
			<span className="sr-only">Toggle theme</span>
		</Button>
	);
}
