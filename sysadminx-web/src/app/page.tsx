"use client";

import { useAppStore } from "@/store/useAppStore";
import { Sidebar } from "@/components/layout/sidebar";
import { Topbar } from "@/components/layout/topbar";
import { Globe } from "lucide-react";
import dynamic from "next/dynamic";

const DashboardPanel = dynamic(() => import("@/components/modules/DashboardPanel").then(mod => mod.DashboardPanel), { loading: () => <div className="p-8">Loading Dashboard...</div> });
const DeviceDetailsPanel = dynamic(() => import("@/components/modules/DeviceDetailsPanel").then(mod => mod.DeviceDetailsPanel), { loading: () => <div className="p-8">Loading Device Details...</div> });
const DriverManagerPanel = dynamic(() => import("@/components/modules/DriverManagerPanel").then(mod => mod.DriverManagerPanel), { loading: () => <div className="p-8">Loading Driver Manager...</div> });
const PatchManagerPanel = dynamic(() => import("@/components/modules/PatchManagerPanel").then(mod => mod.PatchManagerPanel), { loading: () => <div className="p-8">Loading Patch Manager...</div> });
const TroubleshootingPanel = dynamic(() => import("@/components/modules/TroubleshootingPanel").then(mod => mod.TroubleshootingPanel), { loading: () => <div className="p-8">Loading Troubleshooting...</div> });

export default function Home() {
  const { activePanel } = useAppStore();

  return (
    <div className="flex min-h-screen bg-background">
      <Sidebar />
      <div className="flex-1 flex flex-col min-w-0">
        <Topbar />
        
        <main className="flex-1 overflow-x-hidden p-6 relative">
          <div className="absolute inset-0 bg-[radial-gradient(ellipse_at_top,_var(--tw-gradient-stops))] from-emerald-900/10 via-background to-background pointer-events-none" />
          <div className="relative z-0 max-w-7xl mx-auto h-full">
            {activePanel === "Dashboard" && <DashboardPanel />}
            {activePanel === "Device Details" && <DeviceDetailsPanel />}
            {activePanel === "Driver Manager" && <DriverManagerPanel />}
            {activePanel === "Patch Manager" && <PatchManagerPanel />}
            {activePanel === "Troubleshooting" && <TroubleshootingPanel />}
          </div>
        </main>
        
        <footer className="h-12 border-t flex items-center justify-between px-6 text-sm text-muted-foreground z-10 bg-background/50 backdrop-blur-sm">
          <span>SysAdminX v2.0.0-web</span>
          <a href="https://github.com/sysadminx" target="_blank" rel="noreferrer" className="flex items-center hover:text-foreground transition-colors">
            <Globe className="h-4 w-4 mr-2" />
            GitHub
          </a>
        </footer>
      </div>
    </div>
  );
}
