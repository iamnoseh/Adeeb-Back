import { describe, expect, it } from "vitest";
import type {
  MmtClusterDto,
  UniversityDto,
} from "@/features/mmt/model/mmt.types";
import { catalogSubjectIds } from "@/features/mmt/lib/mmt";

describe("catalogSubjectIds", () => {
  it("does not read cluster subjects from a university opened by deep link", () => {
    const university = { id: "university-1" } as UniversityDto;

    expect(catalogSubjectIds("universities", university)).toEqual([]);
  });

  it("returns selected subjects for a cluster", () => {
    const cluster = {
      id: "cluster-1",
      subjects: [
        { id: "subject-1", code: "01", name: "Mathematics" },
        { id: "subject-2", code: "02", name: "Physics" },
      ],
    } as MmtClusterDto;

    expect(catalogSubjectIds("clusters", cluster)).toEqual([
      "subject-1",
      "subject-2",
    ]);
  });
});
