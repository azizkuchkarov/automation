import { cn } from "@/lib/utils";
import { InputHTMLAttributes, forwardRef } from "react";

export const Input = forwardRef<HTMLInputElement, InputHTMLAttributes<HTMLInputElement>>(
  ({ className, ...props }, ref) => (
    <input
      ref={ref}
      className={cn(
        "h-9 w-full rounded-md border border-border bg-surface px-3 text-sm outline-none focus:ring-2 focus:ring-atg-blue/50",
        className
      )}
      {...props}
    />
  )
);
Input.displayName = "Input";
