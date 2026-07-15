import { Check, ChevronDown, X } from "lucide-react";
import { useEffect, useId, useRef, useState } from "react";
import { cn } from "@/shared/lib/cn";
import type { SelectFieldOption } from "@/shared/ui/SelectField";

type MultiSelectFieldProps = {
  values: string[];
  options: SelectFieldOption[];
  onValuesChange: (values: string[]) => void;
  placeholder?: string;
  disabled?: boolean;
};

export function MultiSelectField({
  values,
  options,
  onValuesChange,
  placeholder,
  disabled = false,
}: MultiSelectFieldProps) {
  const id = useId();
  const rootRef = useRef<HTMLDivElement>(null);
  const [open, setOpen] = useState(false);
  const selected = options.filter((option) => values.includes(option.value));

  useEffect(() => {
    function close(event: MouseEvent) {
      if (!rootRef.current?.contains(event.target as Node)) setOpen(false);
    }
    document.addEventListener("mousedown", close);
    return () => document.removeEventListener("mousedown", close);
  }, []);

  function toggle(value: string) {
    onValuesChange(
      values.includes(value)
        ? values.filter((item) => item !== value)
        : [...values, value],
    );
  }

  return (
    <div
      ref={rootRef}
      className={cn("relative min-w-0 max-w-full", open ? "z-30" : "")}
    >
      <button
        type="button"
        aria-expanded={open}
        aria-haspopup="listbox"
        aria-controls={id}
        disabled={disabled}
        className={cn(
          "flex min-h-14 min-w-0 w-full max-w-full items-center justify-between gap-3 overflow-hidden rounded-[1.55rem] border-2 border-[var(--border)] bg-white px-4 py-3 text-left text-sm font-semibold text-[var(--text)] shadow-[0_8px_22px_rgb(24_49_45/0.06)] transition sm:px-5 sm:text-base",
          "hover:border-[color-mix(in_srgb,var(--primary)_45%,var(--border))] focus:border-[var(--primary)] focus:outline-none",
          disabled ? "cursor-not-allowed opacity-60" : "",
        )}
        onClick={() => setOpen((current) => !current)}
      >
        <span
          className={cn(
            "min-w-0 flex-1 truncate",
            selected.length ? "" : "text-[var(--muted)]",
          )}
        >
          {selected.length
            ? selected.map((option) => option.label).join(", ")
            : placeholder}
        </span>
        <ChevronDown
          className={cn(
            "h-4 w-4 shrink-0 transition",
            open ? "rotate-180" : "",
          )}
          aria-hidden
        />
      </button>
      {selected.length ? (
        <div className="mt-2 flex min-w-0 max-w-full flex-wrap gap-2 overflow-hidden">
          {selected.map((option) => (
            <span
              key={option.value}
              className="inline-flex min-w-0 max-w-full items-center gap-1 rounded-full bg-[var(--surface-muted)] px-3 py-1 text-sm font-semibold"
            >
              <span className="min-w-0 truncate">{option.label}</span>
              <button
                type="button"
                aria-label={option.label}
                onClick={() => toggle(option.value)}
              >
                <X className="h-3.5 w-3.5" aria-hidden />
              </button>
            </span>
          ))}
        </div>
      ) : null}
      {open ? (
        <div
          id={id}
          role="listbox"
          aria-multiselectable="true"
          className="absolute left-0 right-0 top-[calc(100%+0.45rem)] min-w-0 max-w-full max-h-72 overflow-y-auto overflow-x-hidden rounded-[1.25rem] border border-[var(--border)] bg-white p-1.5 shadow-[0_22px_55px_rgb(24_49_45/0.18)]"
        >
          {options.map((option) => {
            const checked = values.includes(option.value);
            return (
              <button
                key={option.value}
                type="button"
                role="option"
                aria-selected={checked}
                disabled={option.disabled}
                className="flex min-h-11 min-w-0 w-full items-center gap-3 rounded-2xl px-4 py-2.5 text-left text-sm font-semibold hover:bg-[var(--surface-muted)]"
                onClick={() => toggle(option.value)}
              >
                <span
                  className={cn(
                    "grid h-5 w-5 shrink-0 place-items-center rounded border",
                    checked
                      ? "border-[var(--primary)] bg-[var(--primary)] text-white"
                      : "border-[var(--border)]",
                  )}
                >
                  {checked ? (
                    <Check className="h-3.5 w-3.5" aria-hidden />
                  ) : null}
                </span>
                <span className="min-w-0 break-words">{option.label}</span>
              </button>
            );
          })}
        </div>
      ) : null}
    </div>
  );
}
