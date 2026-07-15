import type { ReactNode } from "react";

type FormFieldProps = {
  label: string;
  error?: string | undefined;
  children: ReactNode;
};

export function FormField({ label, error, children }: FormFieldProps) {
  return (
    <div className="min-w-0 grid gap-2 text-sm font-semibold text-[var(--text)]">
      <span className="px-1">{label}</span>
      {children}
      {error ? (
        <span className="text-xs font-semibold text-[var(--danger)]">
          {error}
        </span>
      ) : null}
    </div>
  );
}
