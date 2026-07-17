// @vitest-environment jsdom
import "@testing-library/jest-dom/vitest";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { OverflowMarquee } from "./OverflowMarquee";

describe("OverflowMarquee", () => {
  let contentWidth = 220;
  beforeEach(() => {
    contentWidth = 220;
    Object.defineProperty(HTMLElement.prototype, "clientWidth", { configurable: true, get: () => 100 });
    Object.defineProperty(HTMLElement.prototype, "scrollWidth", {
      configurable: true,
      get() { return this.parentElement?.classList.contains("overflow-marquee") ? contentWidth : 100; },
    });
    vi.stubGlobal("ResizeObserver", class {
      private readonly callback: ResizeObserverCallback;
      constructor(callback: ResizeObserverCallback) { this.callback = callback; }
      observe() { this.callback([], this as unknown as ResizeObserver); }
      disconnect() {}
      unobserve() {}
    });
  });
  afterEach(() => { cleanup(); vi.unstubAllGlobals(); });

  it("animates and exposes a tooltip only when the text overflows", async () => {
    render(<OverflowMarquee text="A long university name" />);
    const container = screen.getByTitle("A long university name");
    await waitFor(() => expect(container).toHaveClass("overflow-marquee--active"));
    expect(container).toHaveAttribute("tabindex", "0");
  });

  it("keeps fitting text static and without a tooltip", () => {
    contentWidth = 80;
    const { container } = render(<OverflowMarquee text="Short" />);
    const marquee = container.firstElementChild;
    expect(marquee).not.toHaveClass("overflow-marquee--active");
    expect(marquee).not.toHaveAttribute("title");
  });
});
