import { create } from 'zustand';
import { persist } from 'zustand/middleware';

type Panel = 'Dashboard' | 'Device Details' | 'Driver Manager' | 'Patch Manager' | 'Troubleshooting';

interface AppState {
  activePanel: Panel;
  setActivePanel: (panel: Panel) => void;
  isSidebarCollapsed: boolean;
  toggleSidebar: () => void;
}

export const useAppStore = create<AppState>()(
  persist(
    (set) => ({
      activePanel: 'Dashboard',
      setActivePanel: (panel) => set({ activePanel: panel }),
      isSidebarCollapsed: false,
      toggleSidebar: () => set((state) => ({ isSidebarCollapsed: !state.isSidebarCollapsed })),
    }),
    {
      name: 'sysadminx-storage',
    }
  )
);
