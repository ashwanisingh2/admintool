"use client";

import { useEffect, useState, useMemo } from "react";
import { MOCK_DRIVERS, withLatency } from "@/lib/mock-data";
import { Button } from "@/components/ui/button";
import { Search, Download, RefreshCw, MoreVertical, Check, AlertCircle, HelpCircle, Loader2, ChevronLeft, ChevronRight } from "lucide-react";
import { toast } from "sonner";
import { cn } from "@/lib/utils";

type Driver = {
  id: number;
  name: string;
  version: string;
  provider: string;
  date: string;
  status: string;
};

function StatusBadge({ status }: { status: string }) {
  if (status === "OK") {
    return (
      <span className="inline-flex items-center gap-1.5 rounded-full px-2 py-0.5 text-xs font-medium bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border border-emerald-500/20">
        <Check className="h-3 w-3" /> OK
      </span>
    );
  }
  if (status === "Error") {
    return (
      <span className="inline-flex items-center gap-1.5 rounded-full px-2 py-0.5 text-xs font-medium bg-rose-500/10 text-rose-600 dark:text-rose-400 border border-rose-500/20">
        <AlertCircle className="h-3 w-3" /> Error
      </span>
    );
  }
  return (
    <span className="inline-flex items-center gap-1.5 rounded-full px-2 py-0.5 text-xs font-medium bg-amber-500/10 text-amber-600 dark:text-amber-400 border border-amber-500/20">
      <HelpCircle className="h-3 w-3" /> Unknown
    </span>
  );
}

function ActionsDropdown({ onUpdate, onRollback, onProperties }: any) {
  const [open, setOpen] = useState(false);
  
  useEffect(() => {
    if (!open) return;
    const handleDocumentClick = () => setOpen(false);
    document.addEventListener("click", handleDocumentClick);
    return () => document.removeEventListener("click", handleDocumentClick);
  }, [open]);

  return (
    <div className="relative" onClick={(e) => e.stopPropagation()}>
      <Button variant="ghost" size="icon" onClick={() => setOpen(!open)}>
        <MoreVertical className="h-4 w-4" />
      </Button>
      {open && (
        <div className="absolute right-0 top-full mt-1 z-50 w-36 rounded-md border bg-popover p-1 text-popover-foreground shadow-md outline-none">
          <button 
            onClick={() => { setOpen(false); onUpdate(); }} 
            className="w-full flex items-center px-2 py-1.5 text-sm rounded-sm hover:bg-accent hover:text-accent-foreground transition-colors"
          >
            Update Driver
          </button>
          <button 
            onClick={() => { setOpen(false); onRollback(); }} 
            className="w-full flex items-center px-2 py-1.5 text-sm rounded-sm hover:bg-accent hover:text-accent-foreground transition-colors"
          >
            Rollback
          </button>
          <div className="h-px bg-border my-1 mx-1" />
          <button 
            onClick={() => { setOpen(false); onProperties(); }} 
            className="w-full flex items-center px-2 py-1.5 text-sm rounded-sm hover:bg-accent hover:text-accent-foreground transition-colors"
          >
            Properties
          </button>
        </div>
      )}
    </div>
  );
}

function DriverRow({ driver, onUpdateDone }: { driver: Driver, onUpdateDone: (id: number, newVersion: string) => void }) {
  const [isUpdating, setIsUpdating] = useState(false);
  
  const handleUpdate = async () => {
    setIsUpdating(true);
    await new Promise(resolve => setTimeout(resolve, 2000));
    setIsUpdating(false);
    toast.success("Driver updated");
    
    const parts = driver.version.split('.');
    if (parts.length > 0) {
      parts[parts.length - 1] = (parseInt(parts[parts.length - 1] || "0") + 1).toString();
    }
    const newVersion = parts.join('.');
    onUpdateDone(driver.id, newVersion);
  };
  
  return (
    <tr className="border-b transition-colors hover:bg-muted/50 dark:even:bg-muted/20">
      <td className="p-4 align-middle">
        <div className="flex items-center gap-2">
          {isUpdating && <Loader2 className="h-4 w-4 animate-spin text-emerald-500" />}
          <span className="font-medium text-foreground">{driver.name}</span>
        </div>
      </td>
      <td className="p-4 align-middle text-muted-foreground">{driver.version}</td>
      <td className="p-4 align-middle text-muted-foreground">{driver.provider}</td>
      <td className="p-4 align-middle text-muted-foreground">{driver.date}</td>
      <td className="p-4 align-middle"><StatusBadge status={driver.status} /></td>
      <td className="p-4 align-middle">
        <ActionsDropdown 
          onUpdate={handleUpdate} 
          onRollback={() => toast.info("Rollback not implemented in mock")}
          onProperties={() => toast.info("Properties view not implemented")}
        />
      </td>
    </tr>
  );
}

export function DriverManagerPanel() {
  const [drivers, setDrivers] = useState<Driver[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isScanning, setIsScanning] = useState(false);
  const [search, setSearch] = useState("");
  const [filter, setFilter] = useState("All");
  const [page, setPage] = useState(1);
  const itemsPerPage = 10;

  useEffect(() => {
    let mounted = true;
    withLatency(MOCK_DRIVERS).then(data => {
      if (mounted) {
        setDrivers(data);
        setIsLoading(false);
      }
    });
    return () => { mounted = false; };
  }, []);

  const handleScan = async () => {
    setIsScanning(true);
    await new Promise(resolve => setTimeout(resolve, 2000));
    setIsScanning(false);
    toast.success(`Scan complete: ${drivers.length} drivers found`);
  };

  const handleExport = () => {
    toast.info("CSV Export started...");
  };

  const handleUpdateDone = (id: number, newVersion: string) => {
    setDrivers(prev => prev.map(d => 
      d.id === id ? { ...d, version: newVersion, status: "OK" } : d
    ));
  };

  const filteredDrivers = useMemo(() => {
    return drivers.filter(d => {
      const matchesSearch = d.name.toLowerCase().includes(search.toLowerCase()) || 
                            d.provider.toLowerCase().includes(search.toLowerCase());
      const matchesFilter = filter === "All" || d.status === filter;
      return matchesSearch && matchesFilter;
    });
  }, [drivers, search, filter]);

  const totalPages = Math.ceil(filteredDrivers.length / itemsPerPage);
  const paginatedDrivers = filteredDrivers.slice((page - 1) * itemsPerPage, page * itemsPerPage);

  useEffect(() => {
    if (page > totalPages && totalPages > 0) {
      setPage(totalPages);
    }
  }, [totalPages, page]);

  return (
    <div className="rounded-xl border bg-card text-card-foreground shadow-sm overflow-hidden flex flex-col h-full min-h-[500px]">
      <div className="p-6 border-b flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h2 className="text-xl font-semibold tracking-tight">Driver Manager</h2>
          <p className="text-sm text-muted-foreground mt-1">Manage and update system drivers</p>
        </div>
        <div className="flex items-center gap-2">
          <Button variant="outline" size="sm" onClick={handleExport}>
            <Download className="mr-2 h-4 w-4" /> Export CSV
          </Button>
          <Button size="sm" onClick={handleScan} disabled={isScanning} className="bg-emerald-600 hover:bg-emerald-700 text-white">
            {isScanning ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : <RefreshCw className="mr-2 h-4 w-4" />}
            {isScanning ? "Scanning..." : "Scan for changes"}
          </Button>
        </div>
      </div>
      
      <div className="p-4 border-b bg-muted/20 flex flex-col sm:flex-row gap-4 justify-between items-center">
        <div className="flex gap-2 w-full sm:w-auto overflow-x-auto pb-2 sm:pb-0 scrollbar-none">
          {["All", "OK", "Error", "Unknown"].map(f => {
            const count = f === "All" ? drivers.length : drivers.filter(d => d.status === f).length;
            return (
              <button 
                key={f} 
                onClick={() => { setFilter(f); setPage(1); }} 
                className={cn(
                  "px-3 py-1.5 rounded-full text-xs font-medium border whitespace-nowrap transition-colors", 
                  filter === f 
                    ? "bg-primary text-primary-foreground border-primary" 
                    : "bg-background text-muted-foreground border-border hover:bg-muted"
                )}
              >
                {f} ({count})
              </button>
            );
          })}
        </div>
        <div className="relative w-full sm:w-72">
          <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
          <input 
            type="text" 
            placeholder="Search by name or provider..." 
            className="w-full pl-9 h-9 rounded-md border border-input bg-background px-3 py-1 text-sm shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
            value={search}
            onChange={e => { setSearch(e.target.value); setPage(1); }}
          />
        </div>
      </div>

      <div className="flex-1 overflow-auto">
        <table className="w-full caption-bottom text-sm text-left whitespace-nowrap">
          <thead className="[&_tr]:border-b sticky top-0 bg-card z-10 shadow-sm">
            <tr className="border-b transition-colors hover:bg-muted/50">
              <th className="h-10 px-4 align-middle font-medium text-muted-foreground">Device Name</th>
              <th className="h-10 px-4 align-middle font-medium text-muted-foreground">Version</th>
              <th className="h-10 px-4 align-middle font-medium text-muted-foreground">Provider</th>
              <th className="h-10 px-4 align-middle font-medium text-muted-foreground">Date</th>
              <th className="h-10 px-4 align-middle font-medium text-muted-foreground">Status</th>
              <th className="h-10 px-4 align-middle font-medium text-muted-foreground w-[100px]">Actions</th>
            </tr>
          </thead>
          <tbody className="[&_tr:last-child]:border-0">
            {isLoading ? (
              Array.from({ length: 5 }).map((_, i) => (
                <tr key={i} className="border-b">
                  <td className="p-4"><div className="h-5 w-48 bg-muted rounded animate-pulse" /></td>
                  <td className="p-4"><div className="h-5 w-24 bg-muted rounded animate-pulse" /></td>
                  <td className="p-4"><div className="h-5 w-32 bg-muted rounded animate-pulse" /></td>
                  <td className="p-4"><div className="h-5 w-24 bg-muted rounded animate-pulse" /></td>
                  <td className="p-4"><div className="h-5 w-16 bg-muted rounded animate-pulse" /></td>
                  <td className="p-4"><div className="h-8 w-8 bg-muted rounded animate-pulse" /></td>
                </tr>
              ))
            ) : paginatedDrivers.length > 0 ? (
              paginatedDrivers.map(driver => (
                <DriverRow key={driver.id} driver={driver} onUpdateDone={handleUpdateDone} />
              ))
            ) : (
              <tr>
                <td colSpan={6} className="p-8 text-center text-muted-foreground">No drivers found.</td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
      
      <div className="p-4 border-t flex justify-between items-center bg-muted/20">
        <div className="text-sm text-muted-foreground">
          Showing {filteredDrivers.length > 0 ? Math.min((page - 1) * itemsPerPage + 1, filteredDrivers.length) : 0} to {Math.min(page * itemsPerPage, filteredDrivers.length)} of {filteredDrivers.length} results
        </div>
        <div className="flex gap-1">
          <Button 
            variant="outline" 
            size="icon" 
            onClick={() => setPage(p => Math.max(1, p - 1))} 
            disabled={page === 1 || isLoading}
          >
            <ChevronLeft className="h-4 w-4" />
          </Button>
          <Button 
            variant="outline" 
            size="icon" 
            onClick={() => setPage(p => Math.min(totalPages, p + 1))} 
            disabled={page === totalPages || totalPages === 0 || isLoading}
          >
            <ChevronRight className="h-4 w-4" />
          </Button>
        </div>
      </div>
    </div>
  );
}
