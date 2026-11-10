import { TelemetryPayload, TelemetrySourceMode } from '../../src/types/telemetry';
import { MockTelemetrySimulator } from '../../src/utils/mockSimulator';
import { IracingSdkReader } from './iracingReader';
import { WindowsMemMapReader } from './windowsMemMapReader';

export class TelemetryManager {
  private mode: TelemetrySourceMode = 'auto';
  private liveReader = new IracingSdkReader();
  private winMemMapReader = new WindowsMemMapReader();
  private mockSimulator = new MockTelemetrySimulator();
  private timer: NodeJS.Timeout | null = null;
  private onUpdate: (payload: TelemetryPayload) => void;

  constructor(onUpdate: (payload: TelemetryPayload) => void) {
    this.onUpdate = onUpdate;
  }

  public setMode(mode: TelemetrySourceMode): void {
    this.mode = mode;
    this.pollOnce();
  }

  public getMode(): TelemetrySourceMode {
    return this.mode;
  }

  public start(intervalMs = 250): void {
    this.stop();
    this.winMemMapReader.startIfWindows();
    this.pollOnce();
    this.timer = setInterval(() => this.pollOnce(), intervalMs);
  }

  public stop(): void {
    if (this.timer) {
      clearInterval(this.timer);
      this.timer = null;
    }
    this.winMemMapReader.stop();
  }

  private pollOnce(): void {
    if (this.mode === 'live' || this.mode === 'auto') {
      const livePayload =
        this.liveReader.pollLiveTelemetry() ??
        this.winMemMapReader.getLatestPayload();
      if (livePayload) {
        this.onUpdate(livePayload);
        return;
      }
      if (this.mode === 'live') {
        // Waiting for iRacing simulator to launch
        this.onUpdate({
          connected: false,
          source: 'iracing-live',
          timestamp: Date.now(),
          session: {
            trackDisplayName: 'Waiting for iRacing Simulator...',
            trackConfigName: '',
            sessionType: 'Race',
            strengthOfField: 0,
            currentLap: 0,
            totalLaps: 0,
            sessionTimeRemainSec: 0,
            isTimedSession: false,
            flagState: 'GREEN',
            airTempC: 0,
            trackTempC: 0,
          },
          standings: [],
        });
        return;
      }
    }

    // Either 'mock' mode or 'auto' fallback when iRacing isn't running
    const mockPayload = this.mockSimulator.tick(0.25);
    this.onUpdate(mockPayload);
  }
}
