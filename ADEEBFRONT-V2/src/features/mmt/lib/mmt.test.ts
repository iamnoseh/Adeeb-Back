import { describe, expect, it } from "vitest";
import { enumLabel, mmtAdmissionYear, mmtPage, queryString } from "@/features/mmt/lib/mmt";
import { mmtRu, mmtTg } from "@/shared/i18n/locales/mmt";

describe("MMT contract helpers", () => {
  it("serializes only defined query values", () => {
    expect(
      queryString({
        page: 2,
        search: "law",
        isActive: false,
        clusterId: undefined,
      }),
    ).toBe("?page=2&search=law&isActive=false");
  });

  it("keeps unknown enum values visible for administrators", () => {
    expect(enumLabel(["Буҷавӣ", "Шартномавӣ"], 9, "Номаълум")).toBe(
      "Номаълум (9)",
    );
  });

  it("normalizes invalid pages and admission years", () => {
    expect(mmtPage("3")).toBe(3);
    expect(mmtPage("abc")).toBe(1);
    expect(mmtPage("0")).toBe(1);
    expect(mmtAdmissionYear("2026")).toBe(2026);
    expect(mmtAdmissionYear("1999")).toBeUndefined();
    expect(mmtAdmissionYear("2026.5")).toBeUndefined();
  });

  it("keeps Tajik and Russian translation dictionaries structurally aligned", () => {
    expect(translationKeys(mmtTg)).toEqual(translationKeys(mmtRu));
  });
});

function translationKeys(
  value: Record<string, unknown>,
  prefix = "",
): string[] {
  return Object.entries(value)
    .flatMap(([key, nestedValue]) => {
      const path = prefix ? `${prefix}.${key}` : key;
      return nestedValue && typeof nestedValue === "object"
        ? translationKeys(nestedValue as Record<string, unknown>, path)
        : [path];
    })
    .sort();
}
