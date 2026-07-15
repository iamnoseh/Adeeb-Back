import { useQuery } from "@tanstack/react-query";
import { Check, ChevronDown, ChevronLeft, ChevronRight, Search, X } from "lucide-react";
import { useEffect, useMemo, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import { mmtApi, mmtKeys } from "@/features/mmt/api/mmt.api";
import type { CatalogDto, CatalogKind } from "@/features/mmt/model/mmt.types";
import { Button } from "@/shared/ui/Button";
import { Input } from "@/shared/ui/Input";

type Props = {
  kind: CatalogKind;
  value: string;
  onValueChange: (value: string) => void;
  placeholder: string;
  allLabel?: string;
  activeOnly?: boolean;
  disabled?: boolean;
};

export function MmtReferenceSelect({
  kind,
  value,
  onValueChange,
  placeholder,
  allLabel,
  activeOnly = true,
  disabled = false,
}: Props) {
  const { t } = useTranslation();
  const rootRef = useRef<HTMLDivElement>(null);
  const [open, setOpen] = useState(false);
  const [input, setInput] = useState("");
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);

  useEffect(() => {
    const timeout = window.setTimeout(() => {
      setSearch(input.trim());
      setPage(1);
    }, 300);
    return () => window.clearTimeout(timeout);
  }, [input]);

  useEffect(() => {
    function close(event: MouseEvent) {
      if (!rootRef.current?.contains(event.target as Node)) setOpen(false);
    }
    function closeOnEscape(event: KeyboardEvent) {
      if (event.key === "Escape") setOpen(false);
    }
    document.addEventListener("mousedown", close);
    document.addEventListener("keydown", closeOnEscape);
    return () => {
      document.removeEventListener("mousedown", close);
      document.removeEventListener("keydown", closeOnEscape);
    };
  }, []);

  const queryParams = {
    search: search || undefined,
    isActive: activeOnly ? true : undefined,
    page,
    pageSize: 10,
  };
  const list = useQuery({
    queryKey: mmtKeys.catalog(kind, queryParams),
    queryFn: () => mmtApi.catalogList(kind, queryParams),
    enabled: open || Boolean(value),
  });
  const selected = useQuery({
    queryKey: mmtKeys.catalogDetail(kind, value),
    queryFn: () => mmtApi.catalogDetail(kind, value),
    enabled: Boolean(value) && !list.data?.items.some((item) => item.id === value),
  });
  const items = useMemo(() => {
    const result = [...(list.data?.items ?? [])];
    if (selected.data && !result.some((item) => item.id === selected.data?.id)) result.unshift(selected.data);
    return result;
  }, [list.data?.items, selected.data]);
  const selectedItem = items.find((item) => item.id === value) ?? selected.data;
  const pages = Math.max(1, Math.ceil((list.data?.totalCount ?? 0) / 10));

  function choose(next: string) {
    onValueChange(next);
    setOpen(false);
    setInput("");
  }

  return (
    <div ref={rootRef} className={`relative min-w-0 ${open ? "z-50" : ""}`}>
      <div className={`flex min-h-12 w-full items-stretch overflow-hidden rounded-xl border bg-white shadow-sm transition ${open ? "border-[var(--primary)] ring-4 ring-[rgb(47_125_115/0.12)]" : "border-[var(--border)] hover:border-[var(--primary)]"} ${disabled ? "bg-[var(--surface-muted)] opacity-70" : ""}`}>
        <button
          type="button"
          disabled={disabled}
          aria-expanded={open}
          aria-haspopup="listbox"
          onClick={() => setOpen((current) => !current)}
          className="flex min-w-0 flex-1 items-center gap-3 px-4 text-left text-sm font-semibold focus:outline-none disabled:cursor-not-allowed"
        >
          <Search className="h-4 w-4 shrink-0 text-[var(--muted)]" />
          <span className={`min-w-0 flex-1 truncate ${selectedItem ? "text-[var(--text)]" : "text-[var(--muted)]"}`}>
            {selectedItem ? referenceLabel(selectedItem) : allLabel ?? placeholder}
          </span>
          <ChevronDown className={`h-4 w-4 shrink-0 text-[var(--muted)] transition ${open ? "rotate-180" : ""}`} />
        </button>
        {value && !disabled ? (
          <button type="button" aria-label={t("mmt.clearSelection")} className="grid w-10 shrink-0 place-items-center border-l border-[var(--border)] text-[var(--muted)] hover:bg-[var(--surface-muted)] hover:text-[var(--text)]" onClick={() => choose("")}>
            <X className="h-4 w-4" />
          </button>
        ) : null}
      </div>

      {open ? (
        <div className="absolute left-0 right-0 top-[calc(100%+0.5rem)] min-w-[18rem] overflow-hidden rounded-xl border border-[var(--border)] bg-white shadow-[0_20px_50px_rgb(24_49_45/0.18)]">
          <div className="border-b border-[var(--border)] p-3">
            <div className="relative">
              <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-[var(--muted)]" />
              <Input autoFocus value={input} onChange={(event) => setInput(event.target.value)} className="min-h-10 pl-10" placeholder={t("mmt.searchReferences")} aria-label={t("mmt.searchReferences")} />
            </div>
          </div>
          <div role="listbox" className="max-h-64 overflow-y-auto p-1.5">
            {allLabel ? <Option label={allLabel} selected={!value} onClick={() => choose("")} /> : null}
            {list.isLoading ? <p className="px-3 py-4 text-sm text-[var(--muted)]">{t("mmt.loading")}</p> : null}
            {!list.isLoading && items.length === 0 ? <p className="px-3 py-4 text-sm text-[var(--muted)]">{t("mmt.noSearchResults")}</p> : null}
            {items.map((item) => <Option key={item.id} label={referenceLabel(item)} selected={item.id === value} onClick={() => choose(item.id)} />)}
          </div>
          {pages > 1 ? (
            <div className="flex items-center justify-between border-t border-[var(--border)] px-3 py-2 text-xs font-semibold text-[var(--muted)]">
              <span>{page} / {pages}</span>
              <div className="flex gap-1">
                <Button type="button" variant="ghost" className="h-8 min-h-8 px-2" disabled={page <= 1} onClick={() => setPage((current) => current - 1)} aria-label={t("mmt.previousPage")}><ChevronLeft className="h-4 w-4" /></Button>
                <Button type="button" variant="ghost" className="h-8 min-h-8 px-2" disabled={page >= pages} onClick={() => setPage((current) => current + 1)} aria-label={t("mmt.nextPage")}><ChevronRight className="h-4 w-4" /></Button>
              </div>
            </div>
          ) : null}
        </div>
      ) : null}
    </div>
  );
}

function Option({ label, selected, onClick }: { label: string; selected: boolean; onClick: () => void }) {
  return <button type="button" role="option" aria-selected={selected} onClick={onClick} className={`flex min-h-10 w-full items-center gap-3 rounded-lg px-3 py-2 text-left text-sm font-semibold transition ${selected ? "bg-[var(--surface-muted)] text-[var(--primary-strong)]" : "hover:bg-[var(--surface-soft)]"}`}><span className="min-w-0 flex-1 truncate">{label}</span>{selected ? <Check className="h-4 w-4 shrink-0 text-[var(--primary)]" /> : null}</button>;
}

function referenceLabel(item: CatalogDto) {
  if ("fullName" in item) return item.fullName;
  return `${item.code} - ${item.name}`;
}
