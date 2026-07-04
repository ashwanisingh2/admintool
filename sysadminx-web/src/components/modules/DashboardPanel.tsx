"use client";

import React, { useState, useEffect, useMemo } from "react";
import { 
  Activity, Server, HardDrive, Clock, Cpu, 
  Network, Wifi, ArrowDown, ArrowUp 
} from "lucide-react";
import { 
  MOCK_DEVICE_INFO, MOCK_DRIVERS, MOCK_MISSING_UPDATES, 
  MOCK_UPDATE_HISTORY, withLatency 
} from "@/lib/mock-data";

const getPath = (data: number[], width: number, height: number) => {
  if (data.length === 0) return "";
  const max = 100;
  const dx = width / (data.length - 1 || 1);
  const dy = height / max;
  return data.map((val, i) => {
    const x = i * dx;
    const y = height - (val * dy);
    return `${i === 0 ? 'M' : 'L'} ${x} ${y}`;
  }).join(" ");
};

const GaugeCard = ({ value, label, icon: Icon }: { value: number, label: string, icon: any }) => {
  const color = value < 70 ? "text-emerald-500" : value < 90 ? "text-amber-500" : "text-rose-500";
  const strokeColor = value < 70 ? "#10b981" : value < 90 ? "#f59e0b" : "#f43f5e";
  const radius = 36;
  const circumference = 2 * Math.PI * radius;
  const strokeDashoffset = circumference - (value / 100) * circumference;

  return (
    <div className="bg-zinc-900/80 border border-zinc-800 rounded-xl p-5 shadow-sm flex items-center justify-between hover:border-zinc-700 transition-colors">
      <div>
        <div className="flex items-center gap-2 text-zinc-400 mb-2">
          <Icon className="w-4 h-4" />
          <span className="text-sm font-medium">{label}</span>
        </div>
        <div className={`text-3xl font-bold ${color}`}>{value.toFixed(1)}%</div>
      </div>
      <div className="relative w-20 h-20">
        <svg className="w-full h-full transform -rotate-90" viewBox="0 0 100 100">
          <circle cx="50" cy="50" r={radius} fill="transparent" stroke="#27272a" strokeWidth="8" />
          <circle 
            cx="50" cy="50" r={radius} 
            fill="transparent" 
            stroke={strokeColor} 
            strokeWidth="8" 
            strokeDasharray={circumference} 
            strokeDashoffset={strokeDashoffset} 
            strokeLinecap="round" 
            className="transition-all duration-1000 ease-in-out"
          />
        </svg>
      </div>
    </div>
  );
};

const UptimeCard = ({ value }: { value: string }) => {
  return (
    <div className="bg-zinc-900/80 border border-zinc-800 rounded-xl p-5 shadow-sm flex items-center justify-between hover:border-zinc-700 transition-colors">
      <div>
        <div className="flex items-center gap-2 text-zinc-400 mb-2">
          <Clock className="w-4 h-4" />
          <span className="text-sm font-medium">System Uptime</span>
        </div>
        <div className="text-2xl font-bold text-zinc-100 mt-1">{value}</div>
      </div>
      <div className="w-16 h-16 rounded-full bg-emerald-500/10 flex items-center justify-center text-emerald-500 shadow-inner">
        <Activity className="w-7 h-7" />
      </div>
    </div>
  );
};

const MiniStatCard = ({ title, icon: Icon, val1, label1, val2, label2 }: any) => {
  return (
    <div className="bg-zinc-900/80 border border-zinc-800 rounded-xl p-5 shadow-sm hover:border-zinc-700 transition-colors">
      <div className="flex items-center gap-2 text-zinc-400 mb-4">
        <Icon className="w-4 h-4" />
        <span className="text-sm font-medium">{title}</span>
      </div>
      <div className="grid grid-cols-2 gap-4">
        <div>
          <div className="text-xl font-bold text-zinc-100 flex items-center gap-1">
            <ArrowDown className="w-4 h-4 text-emerald-500" /> {val1}
          </div>
          <div className="text-xs text-zinc-500 uppercase ml-5 mt-1">{label1}</div>
        </div>
        <div>
          <div className="text-xl font-bold text-zinc-100 flex items-center gap-1">
            <ArrowUp className="w-4 h-4 text-sky-500" /> {val2}
          </div>
          <div className="text-xs text-zinc-500 uppercase ml-5 mt-1">{label2}</div>
        </div>
      </div>
    </div>
  );
};

export function DashboardPanel() {
  const [loading, setLoading] = useState(true);
  
  const [cpuVal, setCpuVal] = useState(32);
  const [ramVal, setRamVal] = useState(65);
  const [diskVal, setDiskVal] = useState(48);
  const [uptime, setUptime] = useState("3d 14h 22m");

  const [history, setHistory] = useState<{cpu: number, ram: number}[]>(
    Array.from({ length: 30 }, () => ({ cpu: Math.random() * 20 + 20, ram: Math.random() * 10 + 60 }))
  );

  const [processes, setProcesses] = useState([
    { name: "chrome.exe", pid: 1424, cpu: 12.4, ram: 14.2 },
    { name: "node.exe", pid: 821, cpu: 8.1, ram: 5.6 },
    { name: "svchost.exe", pid: 312, cpu: 2.1, ram: 1.1 },
    { name: "explorer.exe", pid: 110, cpu: 1.5, ram: 3.4 },
    { name: "vscode.exe", pid: 9801, cpu: 0.8, ram: 8.9 },
  ]);

  const [netStats, setNetStats] = useState({ in: 1450, out: 820 });
  const [diskIo, setDiskIo] = useState({ read: 45, write: 12 });
  const [tcpConns, setTcpConns] = useState(142);

  useEffect(() => {
    let isMounted = true;
    
    // Simulate loading data initially
    withLatency(MOCK_DEVICE_INFO, 1200).then(() => {
      if (isMounted) setLoading(false);
    });

    const interval = setInterval(() => {
      if (!isMounted) return;

      let currentCpu = 0;
      let currentRam = 0;

      setCpuVal(prev => {
        currentCpu = Math.max(5, Math.min(95, prev + (Math.random() * 20 - 10)));
        return currentCpu;
      });
      setRamVal(prev => {
        currentRam = Math.max(10, Math.min(90, prev + (Math.random() * 4 - 2)));
        return currentRam;
      });
      setDiskVal(prev => Math.max(40, Math.min(95, prev + (Math.random() * 2 - 1))));
      
      setHistory(prev => [...prev.slice(1), { cpu: currentCpu || 32, ram: currentRam || 65 }]);

      // Update processes
      setProcesses(prev => {
        let newProcs = prev.map(p => ({
          ...p,
          cpu: Math.max(0.1, p.cpu + (Math.random() * 4 - 2)),
          ram: Math.max(0.1, p.ram + (Math.random() * 2 - 1)),
        }));
        newProcs.sort((a, b) => b.cpu - a.cpu);
        return newProcs;
      });

      // Update mini stats
      setNetStats(prev => ({
        in: Math.max(10, Math.floor(prev.in + (Math.random() * 400 - 200))),
        out: Math.max(5, Math.floor(prev.out + (Math.random() * 150 - 75)))
      }));
      setDiskIo(prev => ({
        read: Math.max(0, Math.floor(prev.read + (Math.random() * 20 - 10))),
        write: Math.max(0, Math.floor(prev.write + (Math.random() * 10 - 5)))
      }));
      setTcpConns(prev => Math.max(50, Math.floor(prev + (Math.random() * 10 - 5))));
    }, 2000);

    return () => {
      isMounted = false;
      clearInterval(interval);
    };
  }, []);

  const cpuPath = useMemo(() => getPath(history.map(h => h.cpu), 600, 200), [history]);
  const ramPath = useMemo(() => getPath(history.map(h => h.ram), 600, 200), [history]);

  if (loading) {
    return (
      <div className="p-6 space-y-6 animate-pulse">
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
          {[1,2,3,4].map(i => (
            <div key={i} className="h-32 bg-zinc-900/50 rounded-xl border border-zinc-800"></div>
          ))}
        </div>
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <div className="lg:col-span-2 h-[300px] bg-zinc-900/50 rounded-xl border border-zinc-800"></div>
          <div className="h-[300px] bg-zinc-900/50 rounded-xl border border-zinc-800"></div>
        </div>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          {[1,2,3].map(i => (
            <div key={i} className="h-32 bg-zinc-900/50 rounded-xl border border-zinc-800"></div>
          ))}
        </div>
      </div>
    );
  }

  return (
    <div className="p-6 space-y-6 text-zinc-100">
      
      {/* Top Cards Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        <GaugeCard value={cpuVal} label="CPU Usage" icon={Cpu} />
        <GaugeCard value={ramVal} label="RAM Usage" icon={Server} />
        <GaugeCard value={diskVal} label="Disk Usage" icon={HardDrive} />
        <UptimeCard value={uptime} />
      </div>

      {/* Middle Section */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Sparkline Chart */}
        <div className="lg:col-span-2 bg-zinc-900/80 border border-zinc-800 rounded-xl p-5 flex flex-col shadow-sm">
          <div className="flex justify-between items-center mb-6">
            <h3 className="text-lg font-semibold text-zinc-100">Resource Utilization (60s)</h3>
            <div className="flex gap-4">
              <div className="flex items-center gap-2 text-sm text-zinc-400 bg-zinc-950 px-3 py-1 rounded-full border border-zinc-800">
                <div className="w-2.5 h-2.5 rounded-full bg-emerald-500 shadow-[0_0_8px_rgba(16,185,129,0.6)]"></div> 
                CPU: <span className="font-mono text-emerald-400">{cpuVal.toFixed(1)}%</span>
              </div>
              <div className="flex items-center gap-2 text-sm text-zinc-400 bg-zinc-950 px-3 py-1 rounded-full border border-zinc-800">
                <div className="w-2.5 h-2.5 rounded-full bg-sky-500 shadow-[0_0_8px_rgba(14,165,233,0.6)]"></div> 
                RAM: <span className="font-mono text-sky-400">{ramVal.toFixed(1)}%</span>
              </div>
            </div>
          </div>
          
          <div className="flex-1 relative w-full h-[240px] mt-4">
            {/* Background Grid */}
            <div className="absolute inset-0 flex flex-col justify-between opacity-10 pointer-events-none">
              {[0, 1, 2, 3, 4].map((i) => (
                <div key={i} className="border-b border-zinc-100 w-full h-0"></div>
              ))}
            </div>
            
            <svg className="absolute inset-0 w-full h-full overflow-visible" preserveAspectRatio="none" viewBox="0 0 600 200">
              <path 
                d={cpuPath} 
                fill="none" 
                stroke="#10b981" 
                strokeWidth="3" 
                strokeLinejoin="round" 
                strokeLinecap="round" 
                className="drop-shadow-[0_4px_12px_rgba(16,185,129,0.3)] transition-all duration-1000 ease-linear" 
              />
              <path 
                d={ramPath} 
                fill="none" 
                stroke="#0ea5e9" 
                strokeWidth="3" 
                strokeLinejoin="round" 
                strokeLinecap="round" 
                className="drop-shadow-[0_4px_12px_rgba(14,165,233,0.3)] transition-all duration-1000 ease-linear" 
              />
            </svg>
          </div>
        </div>
        
        {/* Top 5 Processes Table */}
        <div className="bg-zinc-900/80 border border-zinc-800 rounded-xl flex flex-col overflow-hidden shadow-sm">
          <div className="p-5 border-b border-zinc-800 flex items-center justify-between">
            <h3 className="text-lg font-semibold text-zinc-100">Top 5 Processes</h3>
            <span className="text-xs font-medium text-emerald-500 bg-emerald-500/10 px-2 py-1 rounded">Live</span>
          </div>
          <div className="overflow-x-auto flex-1">
            <table className="w-full text-sm text-left">
              <thead className="text-xs text-zinc-400 uppercase bg-zinc-950/50">
                <tr>
                  <th className="px-5 py-4 font-medium">Name</th>
                  <th className="px-5 py-4 font-medium">PID</th>
                  <th className="px-5 py-4 font-medium text-right">CPU %</th>
                  <th className="px-5 py-4 font-medium text-right">RAM %</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-zinc-800/50">
                {processes.map((p, i) => (
                  <tr key={i} className="hover:bg-zinc-800/50 transition-colors">
                    <td className="px-5 py-3.5 font-medium text-zinc-200">{p.name}</td>
                    <td className="px-5 py-3.5 text-zinc-500">{p.pid}</td>
                    <td className="px-5 py-3.5 text-right font-mono text-emerald-400">{p.cpu.toFixed(1)}%</td>
                    <td className="px-5 py-3.5 text-right font-mono text-sky-400">{p.ram.toFixed(1)}%</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      </div>

      {/* Bottom Mini-Stats */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <MiniStatCard 
          title="Network I/O" 
          icon={Network} 
          val1={`${netStats.in} KB/s`} label1="In" 
          val2={`${netStats.out} KB/s`} label2="Out" 
        />
        <MiniStatCard 
          title="Disk I/O" 
          icon={HardDrive} 
          val1={`${diskIo.read} MB/s`} label1="Read" 
          val2={`${diskIo.write} MB/s`} label2="Write" 
        />
        <div className="bg-zinc-900/80 border border-zinc-800 rounded-xl p-5 flex items-center justify-between shadow-sm hover:border-zinc-700 transition-colors">
          <div>
            <div className="flex items-center gap-2 text-zinc-400 mb-2">
              <Wifi className="w-4 h-4" />
              <span className="text-sm font-medium">Active TCP Conns</span>
            </div>
            <div className="text-3xl font-bold text-zinc-100 mt-2">{tcpConns}</div>
          </div>
          <div className="w-14 h-14 rounded-full bg-blue-500/10 flex items-center justify-center text-blue-500 shadow-inner">
            <Network className="w-7 h-7" />
          </div>
        </div>
      </div>
      
    </div>
  );
}
