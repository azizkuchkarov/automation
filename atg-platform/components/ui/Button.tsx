import { cn } from "@/lib/utils";
import { ButtonHTMLAttributes, forwardRef } from "react";

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: "primary" | "secondary" | "ghost" | "danger";
  size?: "sm" | "md";
}

export const Button = forwardRef<HTMLButtonElement, ButtonProps>(
  ({ className, variant = "primary", size = "md", ...props }, ref) => (
    <button
      ref={ref}
      className={cn(
        "inline-flex items-center justify-center rounded-md font-medium transition-colors disabled:opacity-50",
        size === "sm" ? "h-8 px-3 text-sm" : "h-9 px-4 text-sm",
        variant === "primary" && "bg-atg-blue text-white hover:bg-blue-700",
        variant === "secondary" && "bg-surface border border-border hover:bg-border/30",
        variant === "ghost" && "hover:bg-border/30",
        variant === "danger" && "bg-red-600 text-white hover:bg-red-700",
        className
      )}
      {...props}
    />
  )
);
Button.displayName = "Button";
