import {
  AlertCircle,
  CheckCircle2,
  ChevronLeft,
  ChevronRight,
  X,
} from "lucide-react";
import { useEffect, type ReactNode } from "react";
import { useTranslation } from "react-i18next";
import type { MmtNotice } from "@/features/mmt/model/useMmtToast";
import { Badge } from "@/shared/ui/Badge";
import { Button } from "@/shared/ui/Button";

export function BooleanBadge({
  value,
  positive,
  negative,
}: {
  value: boolean;
  positive?: string;
  negative?: string;
}) {
  const { t } = useTranslation();
  return (
    <Badge tone={value ? "success" : "neutral"}>
      {value ? (positive ?? t("mmt.active")) : (negative ?? t("mmt.inactive"))}
    </Badge>
  );
}

export function Metric({
  label,
  value,
  warning = false,
}: {
  label: string;
  value: ReactNode;
  warning?: boolean;
}) {
  return (
    <div className="border-l-2 border-[var(--border)] px-4 py-1 first:border-l-0">
      <p className="text-xs font-bold uppercase text-[var(--muted)]">{label}</p>
      <p
        className={
          warning
            ? "mt-1 text-2xl font-black text-[var(--warning)]"
            : "mt-1 text-2xl font-black"
        }
      >
        {value}
      </p>
    </div>
  );
}

export function Pagination({
  page,
  pageSize,
  total,
  onPage,
}: {
  page: number;
  pageSize: number;
  total: number;
  onPage: (page: number) => void;
}) {
  const { t } = useTranslation();
  const pages = Math.max(1, Math.ceil(total / pageSize));
  return (
    <div className="flex items-center justify-between gap-3 border-t border-[var(--border)] px-4 py-3 text-sm">
      <span className="text-[var(--muted)]">
        {total} {t("mmt.records")} · {t("mmt.page")} {page} {t("mmt.of")}{" "}
        {pages}
      </span>
      <div className="flex gap-2">
        <Button
          type="button"
          variant="secondary"
          className="h-10 min-h-10 px-3"
          disabled={page <= 1}
          onClick={() => onPage(page - 1)}
          aria-label={t("mmt.previousPage")}
        >
          <ChevronLeft className="h-4 w-4" />
        </Button>
        <Button
          type="button"
          variant="secondary"
          className="h-10 min-h-10 px-3"
          disabled={page >= pages}
          onClick={() => onPage(page + 1)}
          aria-label={t("mmt.nextPage")}
        >
          <ChevronRight className="h-4 w-4" />
        </Button>
      </div>
    </div>
  );
}

export function Modal({
  title,
  children,
  onClose,
}: {
  title: string;
  children: ReactNode;
  onClose: () => void;
}) {
  const { t } = useTranslation();
  useEffect(() => {
    const close = (event: KeyboardEvent) => {
      if (event.key === "Escape") onClose();
    };
    document.addEventListener("keydown", close);
    return () => document.removeEventListener("keydown", close);
  }, [onClose]);
  return (
    <div
      className="fixed inset-0 z-50 grid place-items-center bg-black/35 p-4"
      role="dialog"
      aria-modal="true"
      aria-label={title}
      onMouseDown={(event) => {
        if (event.target === event.currentTarget) onClose();
      }}
    >
      <div className="custom-scrollbar max-h-[90vh] w-full max-w-2xl overflow-y-auto rounded-[1.5rem] border border-white/70 bg-white shadow-2xl">
        <header className="sticky top-0 z-10 flex items-center justify-between border-b border-[var(--border)] bg-white px-5 py-4">
          <h2 className="text-lg font-black">{title}</h2>
          <Button
            type="button"
            variant="ghost"
            className="h-10 min-h-10 px-3"
            onClick={onClose}
            aria-label={t("mmt.close")}
          >
            <X className="h-5 w-5" />
          </Button>
        </header>
        <div className="p-5">{children}</div>
      </div>
    </div>
  );
}

export function MmtToast({
  notice,
  onClose,
}: {
  notice: MmtNotice;
  onClose: () => void;
}) {
  const { t } = useTranslation();
  if (!notice) return null;
  const Icon = notice.tone === "success" ? CheckCircle2 : AlertCircle;
  return (
    <div
      className="fixed bottom-5 right-5 z-[60] flex max-w-sm items-start gap-3 rounded-2xl border border-[var(--border)] bg-white p-4 shadow-2xl"
      role="status"
    >
      <Icon
        className={
          notice.tone === "success"
            ? "mt-0.5 h-5 w-5 text-[var(--success)]"
            : "mt-0.5 h-5 w-5 text-[var(--danger)]"
        }
      />
      <p className="flex-1 text-sm font-semibold">{notice.message}</p>
      <button type="button" onClick={onClose} aria-label={t("mmt.dismiss")}>
        <X className="h-4 w-4 text-[var(--muted)]" />
      </button>
    </div>
  );
}
