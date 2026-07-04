import { useState, useEffect } from "react";
import { 
  MOCK_MISSING_UPDATES, 
  MOCK_UPDATE_HISTORY, 
  withLatency 
} from "@/lib/mock-data";
import { 
  RefreshCw, 
  CheckCircle2, 
  XCircle, 
  Loader2, 
  Clock, 
  Calendar,
  AlertCircle,
  Download,
  Filter
} from "lucide-react";

type TabType = "missing" | "history";
type UpdateStatus = "Pending" | "Installing" | "Done" | "Failed";

type MissingUpdate = {
  kb: string;
  title: string;
  severity: string;
  size: string;
  status: UpdateStatus;
};

type UpdateHistory = {
  id: number;
  date: string;
  kb: string;
  title: string;
  category: string;
  result: string;
  duration: string;
};

export function PatchManagerPanel() {
  const [activeTab, setActiveTab] = useState<TabType>("missing");
  const [missingUpdates, setMissingUpdates] = useState<MissingUpdate[]>([]);
  const [historyUpdates, setHistoryUpdates] = useState<UpdateHistory[]>([]);
  
  const [isLoadingMissing, setIsLoadingMissing] = useState(true);
  const [isLoadingHistory, setIsLoadingHistory] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  
  const [isInstalling, setIsInstalling] = useState(false);
  const [progress, setProgress] = useState(0);
  const [showToast, setShowToast] = useState(false);

  const [historyDateFilter, setHistoryDateFilter] = useState("30");
  const [historyResultFilter, setHistoryResultFilter] = useState("All");

  const loadMissingUpdates = async () => {
    setIsLoadingMissing(true);
    try {
      const data = await withLatency(MOCK_MISSING_UPDATES);
      setMissingUpdates(data as MissingUpdate[]);
    } finally {
      setIsLoadingMissing(false);
    }
  };

  const loadHistoryUpdates = async () => {
    setIsLoadingHistory(true);
    try {
      const data = await withLatency(MOCK_UPDATE_HISTORY);
      setHistoryUpdates(data as UpdateHistory[]);
    } finally {
      setIsLoadingHistory(false);
    }
  };

  useEffect(() => {
    loadMissingUpdates();
    loadHistoryUpdates();
  }, []);

  const handleRefresh = async () => {
    setIsRefreshing(true);
    if (activeTab === "missing") {
      await loadMissingUpdates();
    } else {
      await loadHistoryUpdates();
    }
    setIsRefreshing(false);
  };

  const handleInstallAll = () => {
    if (isInstalling || missingUpdates.length === 0) return;
    setIsInstalling(true);
    setProgress(0);
  };

  useEffect(() => {
    if (isInstalling) {
      const interval = setInterval(() => {
        setProgress(p => {
          if (p >= 100) {
            clearInterval(interval);
            setShowToast(true);
            setTimeout(() => setShowToast(false), 3000);
            setIsInstalling(false);
            return 100;
          }
          return p + (100 / (6000 / 50)); 
        });
      }, 50);
      return () => clearInterval(interval);
    }
  }, [isInstalling]);

  useEffect(() => {
    if (isInstalling) {
      setMissingUpdates(prev => prev.map((u, i) => {
        const startPercent = (i / prev.length) * 80; 
        const endPercent = startPercent + 20;
        
        let newStatus: UpdateStatus = "Pending";
        if (progress >= endPercent) newStatus = "Done";
        else if (progress >= startPercent) newStatus = "Installing";
        else newStatus = "Pending";
        
        return { ...u, status: newStatus };
      }));
    } else if (progress >= 100) {
        setMissingUpdates(prev => prev.map(u => ({ ...u, status: "Done" })));
    }
  }, [progress, isInstalling]);

  const filteredHistory = historyUpdates.filter(u => {
    if (historyResultFilter !== "All" && u.result !== historyResultFilter) return false;
    
    const date = new Date(u.date);
    const now = new Date();
    const diffDays = Math.ceil((now.getTime() - date.getTime()) / (1000 * 3600 * 24));
    
    if (historyDateFilter !== "All" && diffDays > parseInt(historyDateFilter)) return false;
    
    return true;
  });

  return (
    <div className="flex flex-col h-full bg-slate-950 text-slate-200 rounded-xl border border-slate-800 shadow-xl overflow-hidden">
      {/* Header */}
      <div className="flex items-center justify-between p-6 border-b border-slate-800 bg-slate-900/50">
        <div className="flex bg-slate-900 p-1 rounded-lg">
          <button
            onClick={() => setActiveTab("missing")}
            className={`px-4 py-2 rounded-md text-sm font-medium transition-colors ${
              activeTab === "missing" 
                ? "bg-slate-800 text-emerald-400 shadow-sm" 
                : "text-slate-400 hover:text-slate-300 hover:bg-slate-800/50"
            }`}
          >
            Missing Updates
          </button>
          <button
            onClick={() => setActiveTab("history")}
            className={`px-4 py-2 rounded-md text-sm font-medium transition-colors ${
              activeTab === "history" 
                ? "bg-slate-800 text-emerald-400 shadow-sm" 
                : "text-slate-400 hover:text-slate-300 hover:bg-slate-800/50"
            }`}
          >
            Update History
          </button>
        </div>

        {activeTab === "missing" && (
          <div className="flex items-center gap-3">
            <span className="bg-slate-800 text-slate-300 px-3 py-1 rounded-full text-xs font-semibold border border-slate-700">
              {missingUpdates.length} Updates Found
            </span>
            <button
              onClick={handleRefresh}
              disabled={isRefreshing || isInstalling}
              className="p-2 bg-slate-800 hover:bg-slate-700 text-slate-300 rounded-md transition-colors disabled:opacity-50 border border-slate-700"
            >
              <RefreshCw size={16} className={isRefreshing ? "animate-spin" : ""} />
            </button>
            <button
              onClick={handleInstallAll}
              disabled={isInstalling || missingUpdates.length === 0}
              className="bg-emerald-600 hover:bg-emerald-500 text-white px-4 py-2 rounded-md text-sm font-medium transition-colors flex items-center gap-2 shadow-lg shadow-emerald-900/20 disabled:opacity-50"
            >
              <Download size={16} />
              Install All
            </button>
          </div>
        )}

        {activeTab === "history" && (
          <div className="flex items-center gap-3">
            <button
              onClick={handleRefresh}
              disabled={isRefreshing}
              className="p-2 bg-slate-800 hover:bg-slate-700 text-slate-300 rounded-md transition-colors disabled:opacity-50 border border-slate-700"
            >
              <RefreshCw size={16} className={isRefreshing ? "animate-spin" : ""} />
            </button>
          </div>
        )}
      </div>

      {/* Progress Bar (Missing Updates) */}
      {activeTab === "missing" && (isInstalling || progress === 100) && (
        <div className="px-6 pt-4">
          <div className="h-2 w-full bg-slate-800 rounded-full overflow-hidden">
            <div 
              className="h-full bg-emerald-500 transition-all duration-75 ease-linear"
              style={{ width: `${progress}%` }}
            />
          </div>
        </div>
      )}

      {/* Content */}
      <div className="flex-1 p-6 overflow-auto">
        {activeTab === "missing" && (
          isLoadingMissing ? (
            <div className="space-y-4">
              {[1, 2, 3, 4, 5].map(i => (
                <div key={i} className="h-16 bg-slate-800/50 rounded-lg animate-pulse" />
              ))}
            </div>
          ) : (
            <div className="rounded-lg border border-slate-800 bg-slate-900/30 overflow-hidden">
              <table className="w-full text-left border-collapse">
                <thead>
                  <tr className="bg-slate-900 border-b border-slate-800 text-xs font-semibold text-slate-400 uppercase tracking-wider">
                    <th className="px-6 py-4">KB Number</th>
                    <th className="px-6 py-4">Title</th>
                    <th className="px-6 py-4">Severity</th>
                    <th className="px-6 py-4">Size</th>
                    <th className="px-6 py-4">Status</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-800/50">
                  {missingUpdates.map((update, idx) => (
                    <tr key={`${update.kb}-${idx}`} className="hover:bg-slate-800/30 transition-colors">
                      <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-slate-300">
                        {update.kb}
                      </td>
                      <td className="px-6 py-4 text-sm text-slate-300">
                        {update.title}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap">
                        <span className={`px-2.5 py-1 rounded-md text-xs font-medium border ${
                          update.severity === 'Critical' ? 'text-rose-400 bg-rose-400/10 border-rose-400/20' :
                          update.severity === 'Important' ? 'text-amber-400 bg-amber-400/10 border-amber-400/20' :
                          'text-sky-400 bg-sky-400/10 border-sky-400/20'
                        }`}>
                          {update.severity}
                        </span>
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-slate-400">
                        {update.size}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap">
                        <div className={`flex items-center gap-2 text-sm font-medium ${
                          update.status === 'Pending' ? 'text-slate-400' :
                          update.status === 'Installing' ? 'text-emerald-400' :
                          update.status === 'Done' ? 'text-emerald-500' :
                          'text-rose-400'
                        }`}>
                          {update.status === 'Pending' && <Clock size={16} />}
                          {update.status === 'Installing' && <Loader2 size={16} className="animate-spin" />}
                          {update.status === 'Done' && <CheckCircle2 size={16} />}
                          {update.status === 'Failed' && <XCircle size={16} />}
                          {update.status}
                        </div>
                      </td>
                    </tr>
                  ))}
                  {missingUpdates.length === 0 && (
                    <tr>
                      <td colSpan={5} className="px-6 py-12 text-center text-slate-500">
                        No missing updates found. Your system is up to date!
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
          )
        )}

        {activeTab === "history" && (
          <div className="flex flex-col h-full space-y-4">
            <div className="flex items-center gap-4 bg-slate-900/50 p-4 rounded-lg border border-slate-800">
              <div className="flex items-center gap-2">
                <Filter size={16} className="text-slate-400" />
                <span className="text-sm text-slate-400 font-medium">Filters:</span>
              </div>
              <select 
                value={historyDateFilter} 
                onChange={e => setHistoryDateFilter(e.target.value)}
                className="bg-slate-950 border border-slate-700 text-sm rounded-md px-3 py-1.5 text-slate-300 focus:outline-none focus:border-emerald-500"
              >
                <option value="7">Last 7 days</option>
                <option value="30">Last 30 days</option>
                <option value="90">Last 90 days</option>
                <option value="All">All time</option>
              </select>
              <select 
                value={historyResultFilter} 
                onChange={e => setHistoryResultFilter(e.target.value)}
                className="bg-slate-950 border border-slate-700 text-sm rounded-md px-3 py-1.5 text-slate-300 focus:outline-none focus:border-emerald-500"
              >
                <option value="All">All Results</option>
                <option value="Success">Success</option>
                <option value="Failed">Failed</option>
              </select>
            </div>

            {isLoadingHistory ? (
              <div className="space-y-4">
                {[1, 2, 3, 4, 5].map(i => (
                  <div key={i} className="h-16 bg-slate-800/50 rounded-lg animate-pulse" />
                ))}
              </div>
            ) : (
              <div className="rounded-lg border border-slate-800 bg-slate-900/30 overflow-hidden flex-1">
                <table className="w-full text-left border-collapse">
                  <thead>
                    <tr className="bg-slate-900 border-b border-slate-800 text-xs font-semibold text-slate-400 uppercase tracking-wider">
                      <th className="px-6 py-4">Date</th>
                      <th className="px-6 py-4">KB Number</th>
                      <th className="px-6 py-4">Title</th>
                      <th className="px-6 py-4">Category</th>
                      <th className="px-6 py-4">Result</th>
                      <th className="px-6 py-4">Duration</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-800/50">
                    {filteredHistory.map((update) => (
                      <tr key={update.id} className="hover:bg-slate-800/30 transition-colors">
                        <td className="px-6 py-4 whitespace-nowrap text-sm text-slate-400 flex items-center gap-2">
                          <Calendar size={14} className="opacity-70" />
                          {update.date}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-slate-300">
                          {update.kb}
                        </td>
                        <td className="px-6 py-4 text-sm text-slate-300">
                          {update.title}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm text-slate-400">
                          {update.category}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap">
                          <div className={`flex items-center gap-1.5 text-sm font-medium ${
                            update.result === 'Success' ? 'text-emerald-400' : 'text-rose-400'
                          }`}>
                            {update.result === 'Success' ? <CheckCircle2 size={16} /> : <XCircle size={16} />}
                            {update.result}
                          </div>
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm text-slate-400">
                          {update.duration}
                        </td>
                      </tr>
                    ))}
                    {filteredHistory.length === 0 && (
                      <tr>
                        <td colSpan={6} className="px-6 py-12 text-center text-slate-500">
                          No history records found for the selected filters.
                        </td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        )}
      </div>

      {/* Simple Toast */}
      {showToast && (
        <div className="absolute bottom-6 right-6 bg-slate-800 border border-emerald-500/50 text-slate-200 px-4 py-3 rounded-lg shadow-2xl flex items-center gap-3 animate-in slide-in-from-bottom-5">
          <div className="bg-emerald-500/20 p-1 rounded-full">
            <CheckCircle2 size={20} className="text-emerald-400" />
          </div>
          <div>
            <p className="text-sm font-medium">Updates Installed</p>
            <p className="text-xs text-slate-400">All available updates have been successfully installed.</p>
          </div>
        </div>
      )}
    </div>
  );
}
