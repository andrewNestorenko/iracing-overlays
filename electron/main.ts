import { app, BrowserWindow, globalShortcut, ipcMain, screen } from 'electron';
import path from 'node:path';
import { OverlayState, TelemetryPayload, TelemetrySourceMode } from '../src/types/telemetry';
import { TelemetryManager } from './telemetry/TelemetryManager';

let mainWindow: BrowserWindow | null = null;
let telemetryManager: TelemetryManager | null = null;

const overlayState: OverlayState = {
  locked: false, // Start unlocked so user can position the overlay or click controls; press F6 to lock click-through
  mode: 'auto',
  opacity: 0.94,
  maxRows: 16,
};

function applyWindowLockState(): void {
  if (!mainWindow || mainWindow.isDestroyed()) return;
  if (overlayState.locked) {
    // Pass all mouse clicks directly through to iRacing beneath the overlay
    mainWindow.setIgnoreMouseEvents(true, { forward: true });
  } else {
    mainWindow.setIgnoreMouseEvents(false);
  }
  mainWindow.webContents.send('overlay:state', overlayState);
}

function createOverlayWindow(): void {
  const primaryDisplay = screen.getPrimaryDisplay();
  const { width: screenWidth } = primaryDisplay.workAreaSize;

  const windowWidth = 620;
  const windowHeight = 640;

  mainWindow = new BrowserWindow({
    width: windowWidth,
    height: windowHeight,
    x: Math.max(24, screenWidth - windowWidth - 36),
    y: 48,
    frame: false,
    transparent: true,
    resizable: true,
    hasShadow: false,
    skipTaskbar: false,
    alwaysOnTop: true,
    backgroundColor: '#00000000',
    webPreferences: {
      preload: path.join(__dirname, 'preload.js'),
      contextIsolation: true,
      nodeIntegration: false,
    },
  });

  // Ensure overlay renders above borderless windowed iRacing on Windows
  mainWindow.setAlwaysOnTop(true, 'screen-saver');
  mainWindow.setVisibleOnAllWorkspaces(true, { visibleOnFullScreen: true });

  if (process.env.VITE_DEV_SERVER_URL) {
    mainWindow.loadURL(process.env.VITE_DEV_SERVER_URL);
  } else {
    mainWindow.loadFile(path.join(__dirname, '../dist/index.html'));
  }

  mainWindow.webContents.on('did-finish-load', () => {
    applyWindowLockState();
  });

  mainWindow.on('closed', () => {
    mainWindow = null;
  });
}

function registerShortcuts(): void {
  // F6 or Ctrl+Shift+L toggles Click-Through Lock
  const toggleLock = () => {
    overlayState.locked = !overlayState.locked;
    applyWindowLockState();
  };

  globalShortcut.register('F6', toggleLock);
  globalShortcut.register('CommandOrControl+Shift+L', toggleLock);

  // F7 or Ctrl+Shift+M cycles telemetry source mode (auto -> mock -> live)
  const cycleMode = () => {
    const order: TelemetrySourceMode[] = ['auto', 'mock', 'live'];
    const nextIdx = (order.indexOf(overlayState.mode) + 1) % order.length;
    overlayState.mode = order[nextIdx];
    telemetryManager?.setMode(overlayState.mode);
    mainWindow?.webContents.send('overlay:state', overlayState);
  };

  globalShortcut.register('F7', cycleMode);
  globalShortcut.register('CommandOrControl+Shift+M', cycleMode);
}

app.whenReady().then(() => {
  createOverlayWindow();
  registerShortcuts();

  telemetryManager = new TelemetryManager((payload: TelemetryPayload) => {
    if (mainWindow && !mainWindow.isDestroyed()) {
      mainWindow.webContents.send('telemetry:update', payload);
    }
  });
  telemetryManager.start(250);

  ipcMain.handle('overlay:get-state', () => overlayState);

  ipcMain.handle('overlay:set-locked', (_event, locked: boolean) => {
    overlayState.locked = Boolean(locked);
    applyWindowLockState();
    return overlayState;
  });

  ipcMain.handle('overlay:set-mode', (_event, mode: TelemetrySourceMode) => {
    overlayState.mode = mode;
    telemetryManager?.setMode(mode);
    mainWindow?.webContents.send('overlay:state', overlayState);
    return overlayState;
  });
});

app.on('will-quit', () => {
  globalShortcut.unregisterAll();
  telemetryManager?.stop();
});

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    app.quit();
  }
});
