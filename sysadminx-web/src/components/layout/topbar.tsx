"use client";

import { useAppStore } from "@/store/useAppStore";
import { ThemeToggle } from "@/components/theme-toggle";
import { Button } from "@/components/ui/button";
import { RefreshCw } from "lucide-react";
import { toast } from "sonner";
import { useState } from "react";
import { delay } from "@/lib/mock-data";

export function Topbar() {
  const { activePanel } = useAppStore();
  const [isRefreshing, setIsRefreshing] = useState(false);

  const handleRefresh = async () => {
    setIsRefreshing(true);
    await delay(800);
    setIsRefreshing(false);
    toast.success(`${activePanel} data refreshed`);
  };

  return (
    <header className="flex h-14 items-center justify-between border-b px-4 lg:px-6 bg-card/30 backdrop-blur-md sticky top-0 z-10">
      <h1 className="text-xl font-semibold tracking-tight">{activePanel}</h1>
      <div className="flex items-center gap-2">
        <Button variant="outline" size="sm" onClick={handleRefresh} disabled={isRefreshing}>
          <RefreshCw className={`h-4 w-4 mr-2 ${isRefreshing ? "animate-spin" : ""}`} />
          Refresh
        </Button>
        <ThemeToggle />
      </div>
    </header>
  );
}
