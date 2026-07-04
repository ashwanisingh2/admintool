"use client";

import { useAppStore } from "@/store/useAppStore";
import { LayoutDashboard, Monitor, HardDrive, ShieldCheck, Wrench, Menu, PanelLeftClose, PanelLeft } from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { useEffect, useState } from "react";

const NAV_ITEMS = [
  { name: "Dashboard", icon: LayoutDashboard },
  { name: "Device Details", icon: Monitor },
  { name: "Driver Manager", icon: HardDrive },
  { name: "Patch Manager", icon: ShieldCheck },
  { name: "Troubleshooting", icon: Wrench },
] as const;

export function Sidebar() {
  const { activePanel, setActivePanel, isSidebarCollapsed, toggleSidebar } = useAppStore();
  const [mounted, setMounted] = useState(false);

  useEffect(() => {
    setMounted(true);
  }, []);

  if (!mounted) return <div className="w-64 border-r bg-card/50" />; // Skeleton

  return (
    <aside
      className={cn(
        "flex flex-col border-r bg-card/50 backdrop-blur-sm transition-all duration-300",
        isSidebarCollapsed ? "w-16" : "w-64"
      )}
    >
      <div className="flex h-14 items-center justify-between px-4 border-b">
        {!isSidebarCollapsed && (
          <div className="flex items-center gap-2 font-bold text-lg text-emerald-600 dark:text-emerald-500">
            <Monitor className="h-5 w-5" />
            <span>SysAdminX</span>
          </div>
        )}
        <Button variant="ghost" size="icon" onClick={toggleSidebar} className={cn(isSidebarCollapsed && "mx-auto")}>
          {isSidebarCollapsed ? <PanelLeft className="h-5 w-5" /> : <PanelLeftClose className="h-5 w-5" />}
        </Button>
      </div>
      
      <nav className="flex-1 space-y-1 p-2">
        {NAV_ITEMS.map((item) => (
          <Button
            key={item.name}
            variant={activePanel === item.name ? "secondary" : "ghost"}
            className={cn(
              "w-full justify-start transition-colors",
              isSidebarCollapsed ? "px-2 justify-center" : "px-4",
              activePanel === item.name && "border-l-4 border-emerald-500 bg-emerald-500/10 hover:bg-emerald-500/20"
            )}
            onClick={() => setActivePanel(item.name)}
            title={isSidebarCollapsed ? item.name : undefined}
          >
            <item.icon className={cn("h-5 w-5", !isSidebarCollapsed && "mr-3", activePanel === item.name && "text-emerald-500")} />
            {!isSidebarCollapsed && <span>{item.name}</span>}
          </Button>
        ))}
      </nav>
    </aside>
  );
}
