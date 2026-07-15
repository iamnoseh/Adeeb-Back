export function formatDushanbeDate(value?: string | null, locale = "tg-TJ") {
  if (!value) return "—";

  return new Intl.DateTimeFormat(locale, {
    dateStyle: "medium",
    timeStyle: "short",
    timeZone: "Asia/Dushanbe",
  }).format(new Date(value));
}
