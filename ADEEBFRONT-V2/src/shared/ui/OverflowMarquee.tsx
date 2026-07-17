import { useEffect, useRef, useState } from "react";
import { cn } from "@/shared/lib/cn";

export function OverflowMarquee({ text, className }: { text: string; className?: string }) {
  const containerRef = useRef<HTMLSpanElement>(null);
  const textRef = useRef<HTMLSpanElement>(null);
  const [overflow, setOverflow] = useState(false);
  const [distance, setDistance] = useState(0);

  useEffect(() => {
    const measure = () => {
      const container = containerRef.current;
      const content = textRef.current;
      if (!container || !content) return;
      const nextDistance = Math.max(0, content.scrollWidth - container.clientWidth);
      setDistance(nextDistance);
      setOverflow(nextDistance > 1);
    };
    measure();
    const observer = new ResizeObserver(measure);
    if (containerRef.current) observer.observe(containerRef.current);
    if (textRef.current) observer.observe(textRef.current);
    return () => observer.disconnect();
  }, [text]);

  return (
    <span
      ref={containerRef}
      className={cn("overflow-marquee", overflow && "overflow-marquee--active", className)}
      title={overflow ? text : undefined}
      tabIndex={overflow ? 0 : undefined}
      style={{ "--overflow-distance": `${distance}px` } as React.CSSProperties}
    >
      <span ref={textRef}>{text}</span>
    </span>
  );
}
