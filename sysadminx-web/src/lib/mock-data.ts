export const MOCK_DEVICE_INFO = {
  os: {
    name: "Windows 11 Pro",
    version: "23H2",
    build: "22631.4317",
    architecture: "x64",
    installDate: "2023-11-05",
    lastBootTime: "2024-05-18 08:32:11",
  },
  hardware: {
    hostname: "DESKTOP-SYSX",
    manufacturer: "Dell Inc.",
    model: "OptiPlex 7090",
    serialNumber: "D3LL01X",
    chassisType: "Desktop",
  },
  cpu: {
    model: "Intel Core i7-11700",
    cores: 8,
    threads: 16,
    baseClock: "2.5 GHz",
    maxClock: "4.9 GHz",
    l3Cache: "16 MB",
  },
  memory: {
    total: "32 GB",
    available: "18.4 GB",
    slotsUsed: "2/4",
    speed: "3200 MHz",
    type: "DDR4",
  },
  motherboard: {
    vendor: "Dell Inc.",
    product: "0X9Y12",
    version: "A00",
    biosVersion: "1.14.0",
    biosDate: "2023-09-12",
  },
  graphics: {
    gpuName: "NVIDIA RTX 3060",
    vram: "12 GB",
    driverVersion: "551.23",
    resolution: "1920x1080",
  },
  storage: [
    { name: "C: (OS)", type: "SSD", capacity: "1024 GB", freeSpace: "450 GB", health: "OK" },
    { name: "D: (Data)", type: "HDD", capacity: "4096 GB", freeSpace: "1200 GB", health: "OK" },
    { name: "E: (Backup)", type: "HDD", capacity: "2048 GB", freeSpace: "50 GB", health: "Pred Fail" },
  ],
  network: {
    primaryAdapter: "Intel(R) Ethernet Connection (14) I219-LM",
    mac: "00:1A:2B:3C:4D:5E",
    ipv4: "192.168.1.105",
    ipv6: "fe80::1234:5678:9abc:def0",
    gateway: "192.168.1.1",
    dns: "8.8.8.8, 1.1.1.1",
    connectionSpeed: "1000 Mbps",
  },
};

export const MOCK_DRIVERS = [
  { id: 1, name: "Intel(R) UHD Graphics 750", version: "31.0.101.2115", provider: "Intel Corporation", date: "2022-10-14", status: "OK" },
  { id: 2, name: "NVIDIA GeForce RTX 3060", version: "31.0.15.5123", provider: "NVIDIA", date: "2024-01-18", status: "OK" },
  { id: 3, name: "Realtek High Definition Audio", version: "6.0.9231.1", provider: "Realtek", date: "2021-08-10", status: "OK" },
  { id: 4, name: "Intel(R) Ethernet Connection I219-LM", version: "12.19.2.45", provider: "Intel Corporation", date: "2023-05-12", status: "Error" },
  { id: 5, name: "Bluetooth Device (Personal Area Network)", version: "10.0.22621.1", provider: "Microsoft", date: "2006-06-21", status: "Unknown" },
  { id: 6, name: "Synaptics Pointing Device", version: "19.5.31.19", provider: "Synaptics", date: "2020-03-05", status: "OK" },
  { id: 7, name: "Intel(R) Wi-Fi 6 AX201 160MHz", version: "22.180.0.4", provider: "Intel Corporation", date: "2022-11-20", status: "OK" },
  { id: 8, name: "Generic PnP Monitor", version: "10.0.22621.1", provider: "Microsoft", date: "2006-06-21", status: "OK" },
  { id: 9, name: "Logitech USB Input Device", version: "1.0.0.1", provider: "Logitech", date: "2018-09-01", status: "OK" },
  { id: 10, name: "USB Mass Storage Device", version: "10.0.22621.1", provider: "Microsoft", date: "2006-06-21", status: "Error" },
  { id: 11, name: "AMD Ryzen Master Device", version: "2.10.1.2287", provider: "Advanced Micro Devices", date: "2023-02-15", status: "Unknown" },
  { id: 12, name: "HD WebCam", version: "10.0.22621.1", provider: "Microsoft", date: "2006-06-21", status: "OK" },
  { id: 13, name: "Intel(R) Management Engine Interface", version: "2234.3.26.0", provider: "Intel Corporation", date: "2022-09-10", status: "OK" },
  { id: 14, name: "Standard NVM Express Controller", version: "10.0.22621.1", provider: "Microsoft", date: "2006-06-21", status: "OK" },
  { id: 15, name: "HID-compliant consumer control device", version: "10.0.22621.1", provider: "Microsoft", date: "2006-06-21", status: "OK" },
];

export const MOCK_MISSING_UPDATES = [
  { kb: "KB5031455", title: "Cumulative Update for Windows 11 Version 23H2", severity: "Critical", size: "450 MB", status: "Pending" },
  { kb: "KB5031234", title: "Windows Malicious Software Removal Tool", severity: "Important", size: "35 MB", status: "Pending" },
  { kb: "KB5030999", title: "Security Update for Microsoft Defender Antivirus", severity: "Critical", size: "120 MB", status: "Pending" },
  { kb: "KB5029987", title: "Cumulative Update for .NET Framework 3.5 and 4.8.1", severity: "Important", size: "85 MB", status: "Pending" },
  { kb: "KB5030111", title: "Intel - System - 2314.5.12.0", severity: "Optional", size: "5 MB", status: "Pending" },
  { kb: "KB5030222", title: "Realtek - Audio - 6.0.9231.1", severity: "Optional", size: "15 MB", status: "Pending" },
  { kb: "KB5030333", title: "Dell - Firmware - 1.15.0", severity: "Critical", size: "22 MB", status: "Pending" },
  { kb: "KB5030444", title: "Windows 11 Insider Preview 25967.1000", severity: "Optional", size: "3.2 GB", status: "Pending" },
];

export const MOCK_UPDATE_HISTORY = [
  { id: 1, date: "2024-05-15", kb: "KB5031455", title: "Cumulative Update for Windows 11", category: "Quality Update", result: "Success", duration: "5m 20s" },
  { id: 2, date: "2024-05-14", kb: "KB5031234", title: "Windows Malicious Software Removal Tool", category: "Security", result: "Success", duration: "1m 10s" },
  { id: 3, date: "2024-05-10", kb: "KB5030999", title: "Security Update for Microsoft Defender", category: "Security", result: "Failed", duration: "0m 45s" },
  { id: 4, date: "2024-05-01", kb: "KB5029987", title: "Cumulative Update for .NET Framework", category: "Quality Update", result: "Success", duration: "3m 15s" },
  { id: 5, date: "2024-04-20", kb: "KB5028888", title: "Cumulative Update for Windows 11", category: "Quality Update", result: "Success", duration: "6m 05s" },
  { id: 6, date: "2024-04-18", kb: "KB5028777", title: "Security Intelligence Update", category: "Security", result: "Success", duration: "0m 30s" },
  { id: 7, date: "2024-04-10", kb: "KB5028666", title: "Cumulative Update for .NET Framework", category: "Quality Update", result: "Success", duration: "2m 50s" },
  { id: 8, date: "2024-03-25", kb: "KB5027777", title: "Cumulative Update for Windows 11", category: "Quality Update", result: "Success", duration: "4m 40s" },
  { id: 9, date: "2024-03-15", kb: "KB5027666", title: "Feature Update to Windows 11 23H2", category: "Feature Update", result: "Failed", duration: "25m 10s" },
  { id: 10, date: "2024-03-12", kb: "KB5027555", title: "Windows Malicious Software Removal Tool", category: "Security", result: "Success", duration: "1m 20s" },
];

export const delay = (ms: number) => new Promise(resolve => setTimeout(resolve, ms));
export const withLatency = async <T>(data: T): Promise<T> => {
  await delay(500 + Math.random() * 1000);
  return data;
};
