import { ApiError } from "@/shared/api/problem-details";

export const controlLink =
  "inline-flex min-h-10 items-center justify-center gap-2 rounded-2xl border border-[var(--border)] bg-white px-3 py-2 text-sm font-bold text-[var(--text)] no-underline shadow-sm transition hover:bg-[var(--surface-muted)]";

export const mmtDefaultPageSize = 10;

export function mmtPage(value: string | null) {
  const parsed = Number(value);
  return Number.isInteger(parsed) && parsed >= 1 ? parsed : 1;
}

export function mmtAdmissionYear(value: string | null) {
  const parsed = Number(value);
  return Number.isInteger(parsed) && parsed >= 2000 && parsed <= 2100
    ? parsed
    : undefined;
}

export function enumLabel(
  labels: readonly string[],
  value: number,
  unknownLabel: string,
) {
  return labels[value] ?? `${unknownLabel} (${value})`;
}

export function errorMessage(error: unknown, fallback: string) {
  return error instanceof ApiError
    ? (error.problem?.title ?? fallback)
    : fallback;
}

export function queryString(query: Record<string, unknown>) {
  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(query)) {
    if (value !== undefined && value !== null && value !== "")
      params.set(key, String(value));
  }
  const result = params.toString();
  return result ? `?${result}` : "";
}

export function compactId(value: string | null) {
  return value ? `${value.slice(0, 8)}...${value.slice(-4)}` : "—";
}

export function numberOrNull(value: FormDataEntryValue | null) {
  if (typeof value !== "string" || value.trim() === "") return null;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : null;
}
