import { useEffect, useState } from "react";

export type MmtNotice = { tone: "success" | "error"; message: string } | null;

export function useMmtToast() {
  const [notice, setNotice] = useState<MmtNotice>(null);
  useEffect(() => {
    if (!notice) return;
    const timeout = window.setTimeout(() => setNotice(null), 4500);
    return () => window.clearTimeout(timeout);
  }, [notice]);
  return {
    notice,
    success: (message: string) => setNotice({ tone: "success", message }),
    error: (message: string) => setNotice({ tone: "error", message }),
    clear: () => setNotice(null),
  };
}
