export type LicenseClass = 'R' | 'D' | 'C' | 'B' | 'A' | 'P';

export type TelemetrySourceMode = 'auto' | 'live' | 'mock';

export type LapTimeStatus = 'session-best' | 'personal-best' | 'normal' | 'invalid';

export interface DriverStanding {
  carIdx: number;
  position: number;
  classPosition: number;
  carNumber: string;
  userName: string;
  userId: number;
  countryCode: string; // ISO 3166-1 alpha-2 e.g. 'US', 'NL', 'GB', 'DE', 'BR'
  clubName: string;
  iRating: number;
  iRatingDelta: number; // Projected iRating gain/loss based on current position & SOF
  licenseClass: LicenseClass;
  safetyRating: string; // e.g. "4.42"
  licenseString: string; // e.g. "A 4.42"
  licenseColorHex: string; // e.g. "#0153db"
  carClassShortName: string;
  carClassColorHex: string;
  carName: string;
  lastLapTimeSec: number;
  bestLapTimeSec: number;
  lastLapStatus: LapTimeStatus;
  gapToLeaderSec: number;
  intervalAheadSec: number;
  lap: number;
  lapDistPct: number;
  onPitRoad: boolean;
  isPlayer: boolean;
  incidentCount: number;
}

export interface SessionInfoSummary {
  trackDisplayName: string;
  trackConfigName: string;
  sessionType: 'Practice' | 'Qualify' | 'Race' | 'Warmup';
  strengthOfField: number;
  currentLap: number;
  totalLaps: number;
  sessionTimeRemainSec: number;
  isTimedSession: boolean;
  flagState: 'GREEN' | 'YELLOW' | 'WHITE' | 'CHECKERED';
  airTempC: number;
  trackTempC: number;
}

export interface TelemetryPayload {
  connected: boolean;
  source: 'iracing-live' | 'mock-simulator';
  timestamp: number;
  session: SessionInfoSummary;
  standings: DriverStanding[];
}

export interface OverlayState {
  locked: boolean; // When true: click-through enabled (mouse passes to iRacing)
  mode: TelemetrySourceMode;
  opacity: number;
  maxRows: number;
}

export interface IracingOverlayBridge {
  onTelemetryUpdate: (callback: (payload: TelemetryPayload) => void) => () => void;
  onOverlayStateChange: (callback: (state: OverlayState) => void) => () => void;
  setOverlayLocked: (locked: boolean) => Promise<OverlayState>;
  setTelemetryMode: (mode: TelemetrySourceMode) => Promise<OverlayState>;
  getInitialState: () => Promise<OverlayState>;
}

declare global {
  interface Window {
    iracingOverlay?: IracingOverlayBridge;
  }
}
