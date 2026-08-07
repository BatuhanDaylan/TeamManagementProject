import * as React from "react";
import { cn } from "../utils/cn";

export interface BadgeProps extends React.HTMLAttributes<HTMLDivElement> {
  variant?: "default" | "success" | "warning" | "danger" | "info";
}

function Badge({ className, variant = "default", ...props }: BadgeProps) {
  return (
    <div
      className={cn(
        "inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-semibold transition-colors focus:outline-none",
        {
          "border-transparent bg-gray-100 text-gray-900 dark:bg-gray-800 dark:text-gray-100":
            variant === "default",
          "border-transparent bg-green-100 text-green-800 dark:bg-green-900/40 dark:text-green-300":
            variant === "success",
          "border-transparent bg-yellow-100 text-yellow-800 dark:bg-yellow-900/40 dark:text-yellow-300":
            variant === "warning",
          "border-transparent bg-red-100 text-red-800 dark:bg-red-900/40 dark:text-red-300":
            variant === "danger",
          "border-transparent bg-blue-100 text-blue-800 dark:bg-blue-900/40 dark:text-blue-300":
            variant === "info",
        },
        className,
      )}
      {...props}
    />
  );
}

export { Badge };
