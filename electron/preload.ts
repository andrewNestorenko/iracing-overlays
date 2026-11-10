import { contextBridge, ipcRenderer } from 'electron';
import {
  IracingOverlayBridge,
  OverlayState,
  TelemetryPayload,
  TelemetrySourceMode,
} from '../src/types/telemetry';

const bridge: IracingOverlayBridge = {
  onTelemetryUpdate: (callback: (payload: TelemetryPayload) => void) => {
    const listener = (_event: Electron.IpcRendererEvent, payload: TelemetryPayload) => {
      callback(payload);
    };
    ipcRenderer.on('telemetry:update', listener);
    return () => {
      ipcRenderer.removeListener('telemetry:update', listener);
    };
  },

  onOverlayStateChange: (callback: (state: OverlayState) => void) => {
    const listener = (_event: Electron.IpcRendererEvent, state: OverlayState) => {
      callback(state);
    };
    ipcRenderer.on('overlay:state', listener);
    return () => {
      ipcRenderer.removeListener('overlay:state', listener);
    };
  },

  setOverlayLocked: (locked: boolean) => {
    return ipcRenderer.invoke('overlay:set-locked', locked);
  },

  setTelemetryMode: (mode: TelemetrySourceMode) => {
    return ipcRenderer.invoke('overlay:set-mode', mode);
  },

  getInitialState: () => {
    return ipcRenderer.invoke('overlay:get-state');
  },
};

contextBridge.exposeInMainWorld('iracingOverlay', bridge);
