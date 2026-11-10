import { DriverStanding, LapTimeStatus, TelemetryPayload } from '../../src/types/telemetry';
import { estimateIRatingDelta, LICENSE_COLORS, parseLicenseString } from '../../src/utils/formatters';
import { resolveCountryCodeFromClub } from '../../src/utils/flags';

/**
 * Windows iRacing Shared Memory SDK Reader (via irsdk-node).
 * Dynamically imports irsdk-node at runtime on Windows so cross-platform development
 * on macOS or environments without iRacing installed works seamlessly.
 */
export class IracingSdkReader {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  private sdk: any = null;
  private initAttempted = false;

  public tryInit(): boolean {
    if (this.sdk) return true;
    if (process.platform !== 'win32' && this.initAttempted) {
      return false;
    }
    this.initAttempted = true;

    try {
      // Dynamic require so bundler/macOS doesn't fail if native Windows addon isn't present
      // eslint-disable-next-line @typescript-eslint/no-var-requires
      const irsdkModule = require('irsdk-node');
      const IRacingSDK = irsdkModule.IRacingSDK || irsdkModule.default || irsdkModule;
      this.sdk = new IRacingSDK();
      return true;
    } catch {
      this.sdk = null;
      return false;
    }
  }

  public pollLiveTelemetry(): TelemetryPayload | null {
    if (!this.tryInit() || !this.sdk) {
      return null;
    }

    try {
      const isRunning =
        typeof this.sdk.isSimRunning === 'function'
          ? this.sdk.isSimRunning()
          : Boolean(this.sdk.sessionStatusOK);

      if (!isRunning) {
        return null;
      }

      if (typeof this.sdk.waitForData === 'function') {
        this.sdk.waitForData(16);
      }

      const telemetry = this.sdk.getTelemetry?.() ?? {};
      const sessionData = this.sdk.getSessionData?.() ?? {};

      const driversRaw: Array<Record<string, unknown>> =
        sessionData?.DriverInfo?.Drivers ?? [];
      if (!driversRaw.length) {
        return null;
      }

      const playerCarIdx = Number(
        sessionData?.DriverInfo?.DriverCarIdx ?? telemetry?.PlayerCarIdx ?? -1
      );

      // Extract telemetry arrays indexed by CarIdx (0..63)
      const carIdxPosition: number[] = telemetry?.CarIdxPosition?.value ?? telemetry?.CarIdxPosition ?? [];
      const carIdxClassPosition: number[] = telemetry?.CarIdxClassPosition?.value ?? telemetry?.CarIdxClassPosition ?? [];
      const carIdxLastLapTime: number[] = telemetry?.CarIdxLastLapTime?.value ?? telemetry?.CarIdxLastLapTime ?? [];
      const carIdxBestLapTime: number[] = telemetry?.CarIdxBestLapTime?.value ?? telemetry?.CarIdxBestLapTime ?? [];
      const carIdxLap: number[] = telemetry?.CarIdxLap?.value ?? telemetry?.CarIdxLap ?? [];
      const carIdxLapDistPct: number[] = telemetry?.CarIdxLapDistPct?.value ?? telemetry?.CarIdxLapDistPct ?? [];
      const carIdxOnPitRoad: boolean[] = telemetry?.CarIdxOnPitRoad?.value ?? telemetry?.CarIdxOnPitRoad ?? [];
      const carIdxF2Time: number[] = telemetry?.CarIdxF2Time?.value ?? telemetry?.CarIdxF2Time ?? [];

      // Filter out pace car and spectators
      const activeDrivers = driversRaw.filter(
        (d) => Number(d.CarIsPaceCar ?? 0) === 0 && Number(d.IsSpectator ?? 0) === 0
      );

      if (!activeDrivers.length) {
        return null;
      }

      const sof = Math.round(
        activeDrivers.reduce((acc, d) => acc + Number(d.IRating ?? 1350), 0) /
          activeDrivers.length
      );

      // Find overall session best lap
      let sessionBestLapSec = Infinity;
      activeDrivers.forEach((d) => {
        const idx = Number(d.CarIdx ?? 0);
        const best = Number(carIdxBestLapTime[idx] ?? -1);
        if (best > 0 && best < sessionBestLapSec) {
          sessionBestLapSec = best;
        }
      });

      const standings: DriverStanding[] = activeDrivers.map((d) => {
        const idx = Number(d.CarIdx ?? 0);
        const rawPos = Number(carIdxPosition[idx] ?? 0);
        const rawClassPos = Number(carIdxClassPosition[idx] ?? 0);
        const lastLap = Number(carIdxLastLapTime[idx] ?? -1);
        const bestLap = Number(carIdxBestLapTime[idx] ?? -1);
        const lap = Number(carIdxLap[idx] ?? 0);
        const lapDistPct = Number(carIdxLapDistPct[idx] ?? 0);
        const onPitRoad = Boolean(carIdxOnPitRoad[idx]);
        const gapToLeader = Math.max(0, Number(carIdxF2Time[idx] ?? 0));

        const lic = parseLicenseString(String(d.LicString ?? 'R 2.50'));
        const iRating = Number(d.IRating ?? 1350);
        const clubName = String(d.ClubName ?? d.FlairName ?? 'International');

        let lastLapStatus: LapTimeStatus = 'normal';
        if (lastLap > 0 && Math.abs(lastLap - sessionBestLapSec) < 0.002) {
          lastLapStatus = 'session-best';
        } else if (lastLap > 0 && bestLap > 0 && Math.abs(lastLap - bestLap) < 0.002) {
          lastLapStatus = 'personal-best';
        }

        const rawClassColor = String(d.CarClassColor ?? '0xff5252').replace(/^0x/i, '#');

        return {
          carIdx: idx,
          position: rawPos,
          classPosition: rawClassPos,
          carNumber: String(d.CarNumber ?? idx),
          userName: String(d.UserName ?? `Driver #${idx}`),
          userId: Number(d.UserID ?? idx),
          countryCode: resolveCountryCodeFromClub(clubName),
          clubName,
          iRating,
          iRatingDelta: 0,
          licenseClass: lic.licenseClass,
          safetyRating: lic.safetyRating,
          licenseString: lic.licenseString,
          licenseColorHex: LICENSE_COLORS[lic.licenseClass].bg,
          carClassShortName: String(d.CarClassShortName ?? 'GT3'),
          carClassColorHex: rawClassColor.startsWith('#') ? rawClassColor : '#ff5252',
          carName: String(d.CarScreenNameShort ?? d.CarScreenName ?? 'Race Car'),
          lastLapTimeSec: lastLap > 0 ? lastLap : 0,
          bestLapTimeSec: bestLap > 0 ? bestLap : 0,
          lastLapStatus,
          gapToLeaderSec: gapToLeader,
          intervalAheadSec: 0,
          lap,
          lapDistPct,
          onPitRoad,
          isPlayer: idx === playerCarIdx,
          incidentCount: Number(d.CurDriverIncidentCount ?? 0),
        };
      });

      // Sort by official position if present, otherwise by progress (lap + lapDistPct) or best lap
      standings.sort((a, b) => {
        if (a.position > 0 && b.position > 0) return a.position - b.position;
        if (a.position > 0) return -1;
        if (b.position > 0) return 1;
        const progA = a.lap + a.lapDistPct;
        const progB = b.lap + b.lapDistPct;
        if (Math.abs(progA - progB) > 0.001) return progB - progA;
        if (a.bestLapTimeSec > 0 && b.bestLapTimeSec > 0) {
          return a.bestLapTimeSec - b.bestLapTimeSec;
        }
        return b.iRating - a.iRating;
      });

      // Assign finalized positions, intervals, and iRating delta
      standings.forEach((driver, index) => {
        driver.position = index + 1;
        if (driver.classPosition <= 0) {
          driver.classPosition = index + 1;
        }
        if (index === 0) {
          driver.gapToLeaderSec = 0;
          driver.intervalAheadSec = 0;
        } else {
          const prev = standings[index - 1];
          driver.intervalAheadSec = Math.max(
            0,
            Number((driver.gapToLeaderSec - prev.gapToLeaderSec).toFixed(2))
          );
        }
        driver.iRatingDelta = estimateIRatingDelta(
          driver.position,
          standings.length,
          driver.iRating,
          sof
        );
      });

      const weekendInfo = sessionData?.WeekendInfo ?? {};
      const sessionsList: Array<Record<string, unknown>> =
        sessionData?.SessionInfo?.Sessions ?? [];
      const currentSessionNum = Number(
        telemetry?.SessionNum?.value ?? telemetry?.SessionNum ?? 0
      );
      const activeSession =
        sessionsList.find((s) => Number(s.SessionNum) === currentSessionNum) ??
        sessionsList[sessionsList.length - 1] ??
        {};

      const rawSessionType = String(activeSession.SessionType ?? 'Race');
      const sessionType: 'Practice' | 'Qualify' | 'Race' | 'Warmup' =
        rawSessionType.includes('Qual')
          ? 'Qualify'
          : rawSessionType.includes('Prac')
            ? 'Practice'
            : rawSessionType.includes('Warm')
              ? 'Warmup'
              : 'Race';

      const sessionTimeRemainSec = Math.max(
        0,
        Number(telemetry?.SessionTimeRemain?.value ?? telemetry?.SessionTimeRemain ?? 0)
      );

      return {
        connected: true,
        source: 'iracing-live',
        timestamp: Date.now(),
        session: {
          trackDisplayName: String(weekendInfo.TrackDisplayName ?? 'iRacing Circuit'),
          trackConfigName: String(weekendInfo.TrackConfigName ?? ''),
          sessionType,
          strengthOfField: sof,
          currentLap: Math.max(1, ...standings.map((s) => s.lap)),
          totalLaps: Number(
            telemetry?.SessionLapsRemainEx?.value ?? telemetry?.SessionLapsRemainEx ?? 0
          ),
          sessionTimeRemainSec,
          isTimedSession: sessionTimeRemainSec > 0 && sessionTimeRemainSec < 86000,
          flagState: 'GREEN',
          airTempC: Number(telemetry?.AirTemp?.value ?? telemetry?.AirTemp ?? 22),
          trackTempC: Number(telemetry?.TrackTempCrew?.value ?? telemetry?.TrackTempCrew ?? 32),
        },
        standings,
      };
    } catch {
      return null;
    }
  }
}
