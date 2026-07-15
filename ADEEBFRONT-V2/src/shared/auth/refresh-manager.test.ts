import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  configureRefreshManager,
  refreshOnce,
} from "@/shared/auth/refresh-manager";
import { tokenStore } from "@/shared/auth/token-store";

describe("refresh manager", () => {
  beforeEach(() => {
    const values = new Map<string, string>();
    vi.stubGlobal("window", {
      sessionStorage: {
        getItem: (key: string) => values.get(key) ?? null,
        setItem: (key: string, value: string) => values.set(key, value),
        removeItem: (key: string) => values.delete(key),
      },
    });
    tokenStore.clear();
    tokenStore.setRefreshToken("refresh-1");
  });

  it("serializes concurrent refresh attempts", async () => {
    const refresh = vi.fn(async () => ({
      accessToken: "access-2",
      refreshToken: "refresh-2",
    }));
    configureRefreshManager({ refresh, onRefreshFailed: vi.fn() });

    const [first, second] = await Promise.all([refreshOnce(), refreshOnce()]);

    expect(refresh).toHaveBeenCalledOnce();
    expect(first).toEqual(second);
    expect(tokenStore.getAccessToken()).toBe("access-2");
    expect(tokenStore.getRefreshToken()).toBe("refresh-2");
  });
});
