"use client";

import React, { useEffect, useState } from 'react';
import { Button } from '@/components/ui/button';
import { MOCK_DEVICE_INFO, withLatency } from '@/lib/mock-data';
import { Download, Monitor, Server, Cpu, Database, Activity, HardDrive, Network } from 'lucide-react';

const InfoItem = ({ label, value }: { label: string; value: string | number }) => (
  <div className="flex justify-between items-center py-2.5 border-b border-zinc-800/50 last:border-0 group hover:bg-zinc-800/20 px-2 -mx-2 rounded transition-colors">
    <span className="text-zinc-400 text-sm font-medium">{label}</span>
    <span className="text-zinc-100 text-sm text-right font-medium">{value}</span>
  </div>
);

const SectionCard = ({ title, icon: Icon, children }: { title: string; icon: any; children: React.ReactNode }) => (
  <div className="bg-zinc-950/40 border border-zinc-800 rounded-xl overflow-hidden shadow-sm shadow-black/20">
    <div className="flex items-center gap-3 px-5 py-4 border-b border-zinc-800 bg-zinc-900/50">
      <div className="p-2 bg-emerald-500/10 rounded-lg text-emerald-400">
        <Icon className="w-5 h-5" />
      </div>
      <h3 className="text-zinc-100 font-semibold tracking-wide">{title}</h3>
    </div>
    <div className="p-5">
      {children}
    </div>
  </div>
);

const SkeletonCard = () => (
  <div className="bg-zinc-950/40 border border-zinc-800 rounded-xl overflow-hidden animate-pulse">
    <div className="flex items-center gap-3 px-5 py-4 border-b border-zinc-800 bg-zinc-900/50">
      <div className="w-9 h-9 bg-zinc-800 rounded-lg"></div>
      <div className="h-5 bg-zinc-800 rounded w-1/3"></div>
    </div>
    <div className="p-5 space-y-4">
      {[1, 2, 3, 4, 5].map((i) => (
        <div key={i} className="flex justify-between py-1">
          <div className="h-4 bg-zinc-800 rounded w-1/4"></div>
          <div className="h-4 bg-zinc-800 rounded w-1/3"></div>
        </div>
      ))}
    </div>
  </div>
);

export function DeviceDetailsPanel() {
  const [data, setData] = useState<typeof MOCK_DEVICE_INFO | null>(null);

  useEffect(() => {
    let mounted = true;
    withLatency(MOCK_DEVICE_INFO).then((res) => {
      if (mounted) setData(res);
    });
    return () => {
      mounted = false;
    };
  }, []);

  const handleExport = () => {
    if (!data) return;
    const jsonString = JSON.stringify(data, null, 2);
    const blob = new Blob([jsonString], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `device-details-${new Date().toISOString().split('T')[0]}.json`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  };

  return (
    <div className="space-y-6 animate-in fade-in slide-in-from-bottom-4 duration-500">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h2 className="text-2xl font-bold text-zinc-100 tracking-tight">Device Details</h2>
          <p className="text-zinc-400 mt-1">Comprehensive hardware and software specifications.</p>
        </div>
        <Button 
          onClick={handleExport} 
          disabled={!data}
          className="bg-emerald-600 hover:bg-emerald-700 text-white gap-2 shadow-emerald-900/20 shadow-lg transition-all"
        >
          <Download className="w-4 h-4" />
          Export to JSON
        </Button>
      </div>

      {!data ? (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {[1, 2, 3, 4, 5, 6, 7, 8].map((i) => (
            <SkeletonCard key={i} />
          ))}
        </div>
      ) : (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {/* Card 1 - Operating System */}
          <SectionCard title="Operating System" icon={Monitor}>
            <InfoItem label="Name" value={data.os.name} />
            <InfoItem label="Version" value={data.os.version} />
            <InfoItem label="OS Build" value={data.os.build} />
            <InfoItem label="Architecture" value={data.os.architecture} />
            <InfoItem label="Install Date" value={data.os.installDate} />
            <InfoItem label="Last Boot Time" value={data.os.lastBootTime} />
          </SectionCard>

          {/* Card 2 - Hardware */}
          <SectionCard title="Hardware" icon={Server}>
            <InfoItem label="Hostname" value={data.hardware.hostname} />
            <InfoItem label="Manufacturer" value={data.hardware.manufacturer} />
            <InfoItem label="Model" value={data.hardware.model} />
            <InfoItem label="Serial Number" value={data.hardware.serialNumber} />
            <InfoItem label="Chassis Type" value={data.hardware.chassisType} />
          </SectionCard>

          {/* Card 3 - CPU */}
          <SectionCard title="Processor" icon={Cpu}>
            <InfoItem label="Model" value={data.cpu.model} />
            <InfoItem label="Cores" value={data.cpu.cores} />
            <InfoItem label="Threads" value={data.cpu.threads} />
            <InfoItem label="Base Clock" value={data.cpu.baseClock} />
            <InfoItem label="Max Clock" value={data.cpu.maxClock} />
            <InfoItem label="L3 Cache" value={data.cpu.l3Cache} />
          </SectionCard>

          {/* Card 4 - Memory */}
          <SectionCard title="Memory" icon={Database}>
            <InfoItem label="Total RAM" value={data.memory.total} />
            <InfoItem label="Available" value={data.memory.available} />
            <InfoItem label="Slots Used" value={data.memory.slotsUsed} />
            <InfoItem label="Speed" value={data.memory.speed} />
            <InfoItem label="Type" value={data.memory.type} />
          </SectionCard>

          {/* Card 5 - Motherboard */}
          <SectionCard title="Motherboard" icon={Activity}>
            <InfoItem label="Vendor" value={data.motherboard.vendor} />
            <InfoItem label="Product" value={data.motherboard.product} />
            <InfoItem label="Version" value={data.motherboard.version} />
            <InfoItem label="BIOS Version" value={data.motherboard.biosVersion} />
            <InfoItem label="BIOS Date" value={data.motherboard.biosDate} />
          </SectionCard>

          {/* Card 6 - Graphics */}
          <SectionCard title="Graphics" icon={Monitor}>
            <InfoItem label="GPU Name" value={data.graphics.gpuName} />
            <InfoItem label="VRAM" value={data.graphics.vram} />
            <InfoItem label="Driver Version" value={data.graphics.driverVersion} />
            <InfoItem label="Resolution" value={data.graphics.resolution} />
          </SectionCard>

          {/* Card 8 - Network */}
          <SectionCard title="Network" icon={Network}>
            <InfoItem label="Adapter" value={data.network.primaryAdapter} />
            <InfoItem label="MAC Address" value={data.network.mac} />
            <InfoItem label="IP Address" value={data.network.ipv4} />
            <InfoItem label="Gateway" value={data.network.gateway} />
            <InfoItem label="DNS Servers" value={data.network.dns} />
            <InfoItem label="Connection Speed" value={data.network.connectionSpeed} />
          </SectionCard>

          {/* Card 7 - Storage (Spans or adapts to remaining space) */}
          <div className="lg:col-span-1">
            <SectionCard title="Storage" icon={HardDrive}>
              <div className="space-y-4">
                {data.storage.map((disk, idx) => (
                  <div key={idx} className="bg-zinc-900/80 rounded-lg p-4 border border-zinc-800/80 hover:border-zinc-700 transition-colors">
                    <div className="flex justify-between items-center mb-3">
                      <span className="font-medium text-emerald-400 text-sm flex items-center gap-2">
                        <HardDrive className="w-4 h-4" /> {disk.name}
                      </span>
                      <span className={`text-[10px] uppercase font-bold tracking-wider px-2 py-0.5 rounded-full ${
                        disk.health === 'OK' 
                          ? 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/20' 
                          : 'bg-red-500/10 text-red-400 border border-red-500/20'
                      }`}>
                        {disk.health}
                      </span>
                    </div>
                    <div className="grid grid-cols-3 gap-3">
                      <div className="space-y-1">
                        <div className="text-[11px] text-zinc-500 uppercase tracking-wider font-semibold">Type</div>
                        <div className="text-zinc-200 text-sm font-medium">{disk.type}</div>
                      </div>
                      <div className="space-y-1">
                        <div className="text-[11px] text-zinc-500 uppercase tracking-wider font-semibold">Capacity</div>
                        <div className="text-zinc-200 text-sm font-medium">{disk.capacity}</div>
                      </div>
                      <div className="space-y-1">
                        <div className="text-[11px] text-zinc-500 uppercase tracking-wider font-semibold">Free</div>
                        <div className="text-zinc-200 text-sm font-medium">{disk.freeSpace}</div>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            </SectionCard>
          </div>
        </div>
      )}
    </div>
  );
}
