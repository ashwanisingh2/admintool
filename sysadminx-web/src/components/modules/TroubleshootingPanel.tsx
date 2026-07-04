import React, { useState, useEffect } from 'react';
import {
  ShieldAlert, Activity, Globe, Wifi, Network,
  Trash2, RefreshCw, HardDrive, Printer, DownloadCloud,
  Package, Server, FileText, AlertTriangle, Play,
  CheckCircle2, XCircle, Loader2
} from 'lucide-react';
import { MOCK_DEVICE_INFO, MOCK_DRIVERS, MOCK_MISSING_UPDATES, MOCK_UPDATE_HISTORY, withLatency } from "@/lib/mock-data";

interface ActionItem {
  id: string;
  title: string;
  description: string;
  icon: React.ElementType;
}

const ACTIONS: ActionItem[] = [
  { id: 'sfc', title: 'SFC Scan', description: 'System File Checker scan', icon: ShieldAlert },
  { id: 'dism', title: 'DISM Restore', description: 'Repair Windows image', icon: Activity },
  { id: 'dns', title: 'DNS Flush', description: 'ipconfig /flushdns', icon: Globe },
  { id: 'winsock', title: 'Winsock Reset', description: 'netsh winsock reset', icon: Wifi },
  { id: 'network', title: 'Network Reset', description: 'Reset network adapters', icon: Network },
  { id: 'temp', title: 'Clear Temp', description: 'Clear temporary files', icon: Trash2 },
  { id: 'explorer', title: 'Restart Explorer', description: 'Restart Windows Explorer', icon: RefreshCw },
  { id: 'chkdsk', title: 'Check Disk', description: 'chkdsk /f /r', icon: HardDrive },
  { id: 'spooler', title: 'Clear Print Spooler', description: 'Reset print queue', icon: Printer },
  { id: 'update', title: 'Reset Windows Update', description: 'Clear update cache', icon: DownloadCloud },
  { id: 'store', title: 'Re-register Store Apps', description: 'Fix Windows Store apps', icon: Package },
  { id: 'arp', title: 'Flush ARP Cache', description: 'arp -d *', icon: Server },
  { id: 'hosts', title: 'Reset Hosts File', description: 'Restore default hosts file', icon: FileText },
];

export function TroubleshootingPanel() {
  const [loading, setLoading] = useState(true);
  const [actions, setActions] = useState<ActionItem[]>([]);
  const [runningAction, setRunningAction] = useState<string | null>(null);
  const [actionStatuses, setActionStatuses] = useState<Record<string, 'idle' | 'running' | 'success' | 'error'>>({});
  const [progress, setProgress] = useState(0);
  const [toastMessage, setToastMessage] = useState<{message: string, type: 'success' | 'error'} | null>(null);

  useEffect(() => {
    let mounted = true;
    const fetchActions = async () => {
      try {
        const data = await withLatency(ACTIONS, 1000);
        if (mounted) {
          setActions(data);
          const initialStatuses: Record<string, 'idle'> = {};
          data.forEach(a => { initialStatuses[a.id] = 'idle'; });
          setActionStatuses(initialStatuses);
          setLoading(false);
        }
      } catch (error) {
        if (mounted) setLoading(false);
      }
    };
    fetchActions();
    return () => { mounted = false; };
  }, []);

  const showToast = (message: string, type: 'success' | 'error') => {
    setToastMessage({ message, type });
    setTimeout(() => setToastMessage(null), 3000);
  };

  const handleRun = async (id: string) => {
    if (runningAction) return;

    setRunningAction(id);
    setActionStatuses(prev => ({ ...prev, [id]: 'running' }));
    setProgress(0);

    // Simulate progress 2-3 seconds
    const duration = 2000 + Math.random() * 1000;
    const interval = 100;
    const steps = duration / interval;
    let currentStep = 0;

    const timer = setInterval(() => {
      currentStep++;
      setProgress(Math.min((currentStep / steps) * 100, 100));
    }, interval);

    await new Promise(resolve => setTimeout(resolve, duration));
    clearInterval(timer);
    
    const isSuccess = Math.random() < 0.9; // 90% success rate
    setActionStatuses(prev => ({ ...prev, [id]: isSuccess ? 'success' : 'error' }));
    setRunningAction(null);
    setProgress(0);

    showToast(isSuccess ? "Action completed successfully" : "Action failed", isSuccess ? 'success' : 'error');
  };

  if (loading) {
    return (
      <div className="space-y-6 animate-pulse">
        <div className="h-16 bg-zinc-900/50 rounded-lg w-full border border-zinc-800" />
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          {Array.from({ length: 13 }).map((_, i) => (
            <div key={i} className="h-[140px] bg-zinc-900/50 rounded-xl border border-zinc-800" />
          ))}
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6 relative">
      {toastMessage && (
        <div className={`fixed bottom-4 right-4 px-4 py-3 rounded-lg shadow-lg border flex items-center gap-3 z-50 animate-in fade-in slide-in-from-bottom-5 ${
          toastMessage.type === 'success' ? 'bg-emerald-950/90 border-emerald-900/50 text-emerald-400' : 'bg-red-950/90 border-red-900/50 text-red-400'
        }`}>
          {toastMessage.type === 'success' ? <CheckCircle2 className="w-5 h-5" /> : <XCircle className="w-5 h-5" />}
          <span className="font-medium">{toastMessage.message}</span>
        </div>
      )}

      {/* Info Banner */}
      <div className="flex items-start gap-3 bg-amber-500/10 border border-amber-500/20 rounded-lg p-4 text-amber-400/90">
        <AlertTriangle className="w-5 h-5 shrink-0 mt-0.5 text-amber-500" />
        <div className="text-sm">
          <p className="font-semibold text-amber-500 mb-1">Elevated Permissions Required</p>
          <p>These actions run with elevated permissions. Always review before running.</p>
        </div>
      </div>

      {/* Grid */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        {actions.map((action) => {
          const Icon = action.icon;
          const status = actionStatuses[action.id];
          const isRunning = status === 'running';
          const isDisabled = runningAction !== null && runningAction !== action.id;

          return (
            <div 
              key={action.id} 
              className={`bg-zinc-900/50 border rounded-xl p-5 flex flex-col gap-4 transition-all duration-200 ${
                isDisabled ? 'opacity-50 pointer-events-none border-zinc-800' : 'hover:bg-zinc-900/80 hover:border-emerald-500/30 border-zinc-800/80'
              }`}
            >
              <div className="flex items-start justify-between">
                <div className="flex items-center gap-3">
                  <div className={`p-2 rounded-lg ${
                    status === 'success' ? 'bg-emerald-500/10 text-emerald-500' :
                    status === 'error' ? 'bg-red-500/10 text-red-500' :
                    'bg-zinc-800 text-emerald-400'
                  }`}>
                    <Icon className="w-5 h-5" />
                  </div>
                  <div>
                    <h3 className="font-medium text-zinc-100">{action.title}</h3>
                    <p className="text-xs text-zinc-400 mt-0.5">{action.description}</p>
                  </div>
                </div>
                
                {status === 'success' && <CheckCircle2 className="w-5 h-5 text-emerald-500" />}
                {status === 'error' && <XCircle className="w-5 h-5 text-red-500" />}
              </div>

              <div className="mt-auto pt-2">
                {isRunning ? (
                  <div className="space-y-3">
                    <div className="flex items-center justify-between text-xs font-medium text-emerald-400">
                      <span className="flex items-center gap-2">
                        <Loader2 className="w-3.5 h-3.5 animate-spin" />
                        Running...
                      </span>
                      <span>{Math.round(progress)}%</span>
                    </div>
                    <div className="h-1.5 w-full bg-zinc-800 rounded-full overflow-hidden">
                      <div 
                        className="h-full bg-emerald-500 transition-all duration-100 ease-linear rounded-full"
                        style={{ width: `${progress}%` }}
                      />
                    </div>
                  </div>
                ) : (
                  <button
                    onClick={() => handleRun(action.id)}
                    disabled={isDisabled || isRunning}
                    className={`w-full py-2 px-3 rounded-md text-sm font-medium flex items-center justify-center gap-2 transition-colors ${
                      status === 'success' ? 'bg-zinc-800 text-zinc-300 hover:bg-zinc-700' :
                      status === 'error' ? 'bg-zinc-800 text-zinc-300 hover:bg-zinc-700' :
                      'bg-emerald-600/10 text-emerald-500 hover:bg-emerald-600/20'
                    }`}
                  >
                    {status === 'idle' && <Play className="w-4 h-4" />}
                    {status === 'success' ? 'Run Again' : status === 'error' ? 'Retry' : 'Run Action'}
                  </button>
                )}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}
