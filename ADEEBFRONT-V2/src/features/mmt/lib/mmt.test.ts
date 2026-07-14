import { describe, expect, it } from "vitest";
import { enumLabel, queryString } from "@/features/mmt/lib/mmt";
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
