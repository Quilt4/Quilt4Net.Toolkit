// Returns the browser's IANA timezone name and current UTC offset in minutes (signed: positive
// for zones east of UTC, mirroring TimeZoneInfo conventions — note this is the negation of the
// raw Date.getTimezoneOffset() which uses positive=west). The server prefers the IANA name (DST-
// aware), falling back to the offset when the name can't be resolved on the host runtime.
export function getBrowserTimeZone() {
    try {
        const name = (Intl.DateTimeFormat().resolvedOptions().timeZone) || "";
        const offsetMinutes = -new Date().getTimezoneOffset();
        return { name, offsetMinutes };
    } catch {
        return { name: "", offsetMinutes: 0 };
    }
}
