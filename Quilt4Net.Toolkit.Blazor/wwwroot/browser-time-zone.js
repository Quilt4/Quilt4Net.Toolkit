// Returns the browser's IANA timezone name (e.g. "Europe/Stockholm", "America/Los_Angeles").
// Falls back to an empty string when Intl is unavailable (very old runtimes) — the C# side
// then defaults to UTC.
export function getBrowserTimeZone() {
    try {
        return Intl.DateTimeFormat().resolvedOptions().timeZone || "";
    } catch {
        return "";
    }
}
