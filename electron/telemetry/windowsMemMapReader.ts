import { ChildProcessWithoutNullStreams, spawn } from 'node:child_process';
import { DriverStanding, LapTimeStatus, TelemetryPayload } from '../../src/types/telemetry';
import { estimateIRatingDelta, LICENSE_COLORS, parseLicenseString } from '../../src/utils/formatters';
import { resolveCountryCodeFromClub } from '../../src/utils/flags';

/**
 * Zero-dependency Windows Shared Memory reader for iRacing ("Local\\IRSDKMemMapFileName").
 * Uses Windows built-in .NET MemoryMappedFile via PowerShell C# compilation in memory,
 * allowing cross-compiled Windows Electron builds (built on macOS/Linux) to read live
 * 60Hz/4Hz iRacing telemetry on Windows without needing node-gyp or C++ build tools.
 */
const CSHARP_IRSDK_READER = `
using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Threading;
using System.Collections.Generic;

public class IracingMemReader {
  private static string ReadCString(byte[] buf, int offset, int maxLen) {
    int end = offset;
    while (end < offset + maxLen && end < buf.Length && buf[end] != 0) end++;
    return Encoding.ASCII.GetString(buf, offset, end - offset);
  }

  public static void Main() {
    Console.OutputEncoding = Encoding.UTF8;
    while (true) {
      try {
        using (var mmf = MemoryMappedFile.OpenExisting("Local\\\\IRSDKMemMapFileName", MemoryMappedFileRights.Read)) {
          using (var acc = mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read)) {
            int status = acc.ReadInt32(4);
            if ((status & 1) == 0) {
              Console.WriteLine("{\\"connected\\":false}");
              Thread.Sleep(500);
              continue;
            }
            int sessionInfoLen = acc.ReadInt32(16);
            int sessionInfoOffset = acc.ReadInt32(20);
            int numVars = acc.ReadInt32(24);
            int varHeaderOffset = acc.ReadInt32(28);
            int numBuf = acc.ReadInt32(32);
            int bufLen = acc.ReadInt32(36);

            int bestTick = -1;
            int bestBufOffset = 0;
            for (int i = 0; i < numBuf && i < 4; i++) {
              int bOff = 48 + i * 16;
              int tick = acc.ReadInt32(bOff);
              int off = acc.ReadInt32(bOff + 4);
              if (tick > bestTick) {
                bestTick = tick;
                bestBufOffset = off;
              }
            }

            byte[] yamlBytes = new byte[Math.Max(0, sessionInfoLen)];
            if (sessionInfoLen > 0) {
              acc.ReadArray(sessionInfoOffset, yamlBytes, 0, sessionInfoLen);
            }
            string yamlStr = ReadCString(yamlBytes, 0, sessionInfoLen);

            byte[] varHeaders = new byte[numVars * 144];
            acc.ReadArray(varHeaderOffset, varHeaders, 0, varHeaders.Length);

            byte[] dataBuf = new byte[bufLen];
            acc.ReadArray(bestBufOffset, dataBuf, 0, bufLen);

            int playerCarIdx = 0;
            int sessionNum = 0;
            double sessionTimeRemain = 0;
            int sessionLapsRemain = 0;
            float airTemp = 22f;
            float trackTemp = 32f;
            float lapBestLapTime = 0f;
            float lapLastLapTime = 0f;

            int[] pos = new int[64];
            int[] classPos = new int[64];
            int[] lap = new int[64];
            float[] lastLap = new float[64];
            float[] bestLap = new float[64];
            float[] lapDist = new float[64];
            float[] f2Time = new float[64];
            bool[] onPit = new bool[64];

            for (int v = 0; v < numVars; v++) {
              int hOff = v * 144;
              int vType = BitConverter.ToInt32(varHeaders, hOff);
              int vOffset = BitConverter.ToInt32(varHeaders, hOff + 4);
              int vCount = BitConverter.ToInt32(varHeaders, hOff + 8);
              string vName = ReadCString(varHeaders, hOff + 16, 32);

              if (vName == "PlayerCarIdx") playerCarIdx = BitConverter.ToInt32(dataBuf, vOffset);
              else if (vName == "SessionNum") sessionNum = BitConverter.ToInt32(dataBuf, vOffset);
              else if (vName == "SessionTimeRemain") sessionTimeRemain = BitConverter.ToDouble(dataBuf, vOffset);
              else if (vName == "SessionLapsRemainEx") sessionLapsRemain = BitConverter.ToInt32(dataBuf, vOffset);
              else if (vName == "AirTemp") airTemp = BitConverter.ToSingle(dataBuf, vOffset);
              else if (vName == "TrackTempCrew") trackTemp = BitConverter.ToSingle(dataBuf, vOffset);
              else if (vName == "LapBestLapTime") lapBestLapTime = BitConverter.ToSingle(dataBuf, vOffset);
              else if (vName == "LapLastLapTime") lapLastLapTime = BitConverter.ToSingle(dataBuf, vOffset);
              else if (vName == "CarIdxPosition") {
                for (int c = 0; c < Math.Min(64, vCount); c++) pos[c] = BitConverter.ToInt32(dataBuf, vOffset + c * 4);
              } else if (vName == "CarIdxClassPosition") {
                for (int c = 0; c < Math.Min(64, vCount); c++) classPos[c] = BitConverter.ToInt32(dataBuf, vOffset + c * 4);
              } else if (vName == "CarIdxLap") {
                for (int c = 0; c < Math.Min(64, vCount); c++) lap[c] = BitConverter.ToInt32(dataBuf, vOffset + c * 4);
              } else if (vName == "CarIdxLastLapTime") {
                for (int c = 0; c < Math.Min(64, vCount); c++) lastLap[c] = BitConverter.ToSingle(dataBuf, vOffset + c * 4);
              } else if (vName == "CarIdxBestLapTime") {
                for (int c = 0; c < Math.Min(64, vCount); c++) bestLap[c] = BitConverter.ToSingle(dataBuf, vOffset + c * 4);
              } else if (vName == "CarIdxLapDistPct") {
                for (int c = 0; c < Math.Min(64, vCount); c++) lapDist[c] = BitConverter.ToSingle(dataBuf, vOffset + c * 4);
              } else if (vName == "CarIdxF2Time") {
                for (int c = 0; c < Math.Min(64, vCount); c++) f2Time[c] = BitConverter.ToSingle(dataBuf, vOffset + c * 4);
              } else if (vName == "CarIdxOnPitRoad") {
                for (int c = 0; c < Math.Min(64, vCount); c++) onPit[c] = dataBuf[vOffset + c] != 0;
              }
            }

            string b64Yaml = Convert.ToBase64String(Encoding.UTF8.GetBytes(yamlStr));
            StringBuilder sb = new StringBuilder();
            sb.Append("{\\"connected\\":true,");
            sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,
              "\\"playerCarIdx\\":{0},\\"sessionNum\\":{1},\\"sessionTimeRemain\\":{2:F2},\\"sessionLapsRemain\\":{3},\\"airTemp\\":{4:F1},\\"trackTemp\\":{5:F1},\\"lapBestLapTime\\":{6:F4},\\"lapLastLapTime\\":{7:F4},",
              playerCarIdx, sessionNum, sessionTimeRemain, sessionLapsRemain, airTemp, trackTemp, lapBestLapTime, lapLastLapTime);
            sb.AppendFormat("\\"pos\\":[{0}],", string.Join(",", pos));
            sb.AppendFormat("\\"classPos\\":[{0}],", string.Join(",", classPos));
            sb.AppendFormat("\\"lap\\":[{0}],", string.Join(",", lap));
            sb.AppendFormat("\\"onPit\\":[{0}],", string.Join(",", Array.ConvertAll(onPit, b => b ? "1" : "0")));
            sb.AppendFormat("\\"lastLap\\":[{0}],", string.Join(",", Array.ConvertAll(lastLap, f => f.ToString("F4", System.Globalization.CultureInfo.InvariantCulture))));
            sb.AppendFormat("\\"bestLap\\":[{0}],", string.Join(",", Array.ConvertAll(bestLap, f => f.ToString("F4", System.Globalization.CultureInfo.InvariantCulture))));
            sb.AppendFormat("\\"lapDist\\":[{0}],", string.Join(",", Array.ConvertAll(lapDist, f => f.ToString("F4", System.Globalization.CultureInfo.InvariantCulture))));
            sb.AppendFormat("\\"f2Time\\":[{0}],", string.Join(",", Array.ConvertAll(f2Time, f => f.ToString("F3", System.Globalization.CultureInfo.InvariantCulture))));
            sb.AppendFormat("\\"yamlB64\\":\\"{0}\\"}", b64Yaml);
            Console.WriteLine(sb.ToString());
          }
        }
      } catch {
        Console.WriteLine("{\\"connected\\":false}");
      }
      Thread.Sleep(250);
    }
  }
}
`;

interface RawWinSnapshot {
  connected: boolean;
  playerCarIdx?: number;
  sessionNum?: number;
  sessionTimeRemain?: number;
  sessionLapsRemain?: number;
  airTemp?: number;
  trackTemp?: number;
  lapBestLapTime?: number;
  lapLastLapTime?: number;
  pos?: number[];
  classPos?: number[];
  lap?: number[];
  onPit?: number[];
  lastLap?: number[];
  bestLap?: number[];
  lapDist?: number[];
  f2Time?: number[];
  yamlB64?: string;
}

interface ParsedYamlDriver {
  carIdx: number;
  userName: string;
  userId: number;
  carNumber: string;
  iRating: number;
  licString: string;
  clubName: string;
  carClassShortName: string;
  carClassColor: string;
  carScreenNameShort: string;
  isPaceCar: boolean;
  isSpectator: boolean;
  incidentCount: number;
}

function extractTopLevelYamlSection(yaml: string, sectionName: string): string {
  const startMatch = yaml.match(new RegExp(`(?:^|\\r?\\n)${sectionName}:[ \\t]*\\r?\\n`));
  if (!startMatch || startMatch.index === undefined) return '';
  const tail = yaml.slice(startMatch.index + startMatch[0].length);
  const nextTop = tail.match(/\r?\n(?:[A-Za-z0-9_]+:|\.\.\.)/);
  return nextTop && nextTop.index !== undefined ? tail.slice(0, nextTop.index) : tail;
}

function extractYamlField(chunk: string, key: string, def = ''): string {
  const m = chunk.match(new RegExp(`^[ \\t]*${key}:[ \\t]*([^\\r\\n]*)$`, 'm'));
  if (!m) return def;
  const val = m[1].trim().replace(/^["']|["']$/g, '');
  if (!val || val.toLowerCase() === '-none-') return def;
  return val;
}

function parseIracingYamlDrivers(
  yaml: string,
  sessionNum = 0
): {
  trackDisplayName: string;
  sessionType: 'Practice' | 'Qualify' | 'Race' | 'Warmup';
  drivers: ParsedYamlDriver[];
} {
  const trackDisplayName = extractYamlField(yaml, 'TrackDisplayName', 'iRacing Circuit');

  let sessionType: 'Practice' | 'Qualify' | 'Race' | 'Warmup' = 'Race';
  const sessionInfoSection = extractTopLevelYamlSection(yaml, 'SessionInfo');
  if (sessionInfoSection) {
    const sessionBlocks = sessionInfoSection.split(/(?:^|\r?\n)[ \t]*-[ \t]*SessionNum:/);
    let activeChunk = '';
    for (let i = 1; i < sessionBlocks.length; i++) {
      const chunk = 'SessionNum:' + sessionBlocks[i];
      if (Number(extractYamlField(chunk, 'SessionNum', '-1')) === sessionNum) {
        activeChunk = chunk;
        break;
      }
      activeChunk = chunk;
    }
    const rawType = extractYamlField(activeChunk, 'SessionType', 'Race');
    if (/prac|test/i.test(rawType)) sessionType = 'Practice';
    else if (/qual/i.test(rawType)) sessionType = 'Qualify';
    else if (/warm/i.test(rawType)) sessionType = 'Warmup';
  }

  const driverInfoSection = extractTopLevelYamlSection(yaml, 'DriverInfo');
  if (!driverInfoSection) {
    return { trackDisplayName, sessionType, drivers: [] };
  }

  const paceCarIdx = Number(extractYamlField(driverInfoSection, 'PaceCarIdx', '-1'));
  const driversHeaderMatch = driverInfoSection.match(/(?:^|\r?\n)([ \t]+)Drivers:[ \t]*\r?\n/);
  if (!driversHeaderMatch || driversHeaderMatch.index === undefined) {
    return { trackDisplayName, sessionType, drivers: [] };
  }

  const driversIndent = driversHeaderMatch[1];
  const driversTail = driverInfoSection.slice(
    driversHeaderMatch.index + driversHeaderMatch[0].length
  );
  const nextSiblingKey = driversTail.match(
    new RegExp(`\\r?\\n${driversIndent}[A-Za-z0-9_]+:`)
  );
  const driversBlock =
    '\n' +
    (nextSiblingKey && nextSiblingKey.index !== undefined
      ? driversTail.slice(0, nextSiblingKey.index)
      : driversTail);

  const entries = driversBlock.split(/\r?\n[ \t]*-[ \t]*CarIdx:/);
  const drivers: ParsedYamlDriver[] = [];
  const seenCarIdx = new Set<number>();

  for (let i = 1; i < entries.length; i++) {
    const chunk = 'CarIdx:' + entries[i];
    const carIdx = Number(extractYamlField(chunk, 'CarIdx', '-1'));
    if (!Number.isInteger(carIdx) || carIdx < 0 || carIdx >= 64) continue;
    if (carIdx === paceCarIdx || seenCarIdx.has(carIdx)) continue;

    const isPaceCar = extractYamlField(chunk, 'CarIsPaceCar', '0') === '1';
    const isSpectator = extractYamlField(chunk, 'IsSpectator', '0') === '1';
    const userName = extractYamlField(chunk, 'UserName', '');
    if (!userName || userName.toLowerCase() === 'pace car') continue;

    seenCarIdx.add(carIdx);
    drivers.push({
      carIdx,
      userName,
      userId: Number(extractYamlField(chunk, 'UserID', String(carIdx))),
      carNumber: extractYamlField(chunk, 'CarNumber', String(carIdx)),
      iRating: Number(extractYamlField(chunk, 'IRating', '1350')),
      licString: extractYamlField(chunk, 'LicString', 'R 2.50'),
      clubName: extractYamlField(
        chunk,
        'ClubName',
        extractYamlField(chunk, 'FlairName', 'International')
      ),
      carClassShortName: extractYamlField(chunk, 'CarClassShortName', 'GT3'),
      carClassColor: extractYamlField(chunk, 'CarClassColor', '0xff5252'),
      carScreenNameShort: extractYamlField(chunk, 'CarScreenNameShort', 'Race Car'),
      isPaceCar,
      isSpectator,
      incidentCount: Number(extractYamlField(chunk, 'CurDriverIncidentCount', '0')),
    });
  }

  return { trackDisplayName, sessionType, drivers };
}

export class WindowsMemMapReader {
  private child: ChildProcessWithoutNullStreams | null = null;
  private latestPayload: TelemetryPayload | null = null;
  private buffer = '';

  public startIfWindows(): void {
    if (process.platform !== 'win32' || this.child) return;

    const psScript = `
$code = @'
${CSHARP_IRSDK_READER}
'@
Add-Type -TypeDefinition $code -Language CSharp
[IracingMemReader]::Main()
`;

    try {
      this.child = spawn(
        'powershell.exe',
        ['-NoProfile', '-NonInteractive', '-ExecutionPolicy', 'Bypass', '-Command', psScript],
        { windowsHide: true }
      );

      this.child.stdout.on('data', (chunk: Buffer) => {
        this.buffer += chunk.toString('utf8');
        let newlineIdx = this.buffer.indexOf('\n');
        while (newlineIdx !== -1) {
          const line = this.buffer.slice(0, newlineIdx).trim();
          this.buffer = this.buffer.slice(newlineIdx + 1);
          if (line.startsWith('{')) {
            this.handleSnapshotLine(line);
          }
          newlineIdx = this.buffer.indexOf('\n');
        }
      });

      this.child.on('exit', () => {
        this.child = null;
        this.latestPayload = null;
      });
    } catch {
      this.child = null;
    }
  }

  public stop(): void {
    if (this.child) {
      this.child.kill();
      this.child = null;
    }
  }

  public getLatestPayload(): TelemetryPayload | null {
    this.startIfWindows();
    return this.latestPayload;
  }

  private handleSnapshotLine(jsonLine: string): void {
    try {
      const snap = JSON.parse(jsonLine) as RawWinSnapshot;
      if (!snap.connected || !snap.yamlB64) {
        this.latestPayload = null;
        return;
      }

      const yaml = Buffer.from(snap.yamlB64, 'base64').toString('utf8');
      const { trackDisplayName, sessionType, drivers } = parseIracingYamlDrivers(
        yaml,
        Number(snap.sessionNum ?? 0)
      );
      const isRaceSession = sessionType === 'Race';
      const activeDrivers = drivers.filter((d) => !d.isPaceCar && !d.isSpectator);
      if (!activeDrivers.length) {
        this.latestPayload = null;
        return;
      }

      const sof = Math.round(
        activeDrivers.reduce((acc, d) => acc + d.iRating, 0) / activeDrivers.length
      );

      let sessionBestLapSec = Infinity;
      activeDrivers.forEach((d) => {
        let best = Number(snap.bestLap?.[d.carIdx] ?? -1);
        if (d.carIdx === snap.playerCarIdx && best <= 0 && (snap.lapBestLapTime ?? 0) > 0) {
          best = Number(snap.lapBestLapTime);
        }
        if (best > 0 && best < sessionBestLapSec) {
          sessionBestLapSec = best;
        }
      });

      const standings: DriverStanding[] = activeDrivers.map((d) => {
        const idx = d.carIdx;
        const rawPos = Number(snap.pos?.[idx] ?? 0);
        const rawClassPos = Number(snap.classPos?.[idx] ?? 0);
        let lastLap = Number(snap.lastLap?.[idx] ?? -1);
        let bestLap = Number(snap.bestLap?.[idx] ?? -1);
        if (idx === snap.playerCarIdx) {
          if (lastLap <= 0 && (snap.lapLastLapTime ?? 0) > 0) {
            lastLap = Number(snap.lapLastLapTime);
          }
          if (bestLap <= 0 && (snap.lapBestLapTime ?? 0) > 0) {
            bestLap = Number(snap.lapBestLapTime);
          }
        }
        const lap = Number(snap.lap?.[idx] ?? 0);
        const lapDistPct = Number(snap.lapDist?.[idx] ?? 0);
        const onPitRoad = Boolean(snap.onPit?.[idx]);
        const gapToLeader = Math.max(0, Number(snap.f2Time?.[idx] ?? 0));

        const lic = parseLicenseString(d.licString);
        let lastLapStatus: LapTimeStatus = 'normal';
        if (lastLap > 0 && Math.abs(lastLap - sessionBestLapSec) < 0.002) {
          lastLapStatus = 'session-best';
        } else if (lastLap > 0 && bestLap > 0 && Math.abs(lastLap - bestLap) < 0.002) {
          lastLapStatus = 'personal-best';
        }

        return {
          carIdx: idx,
          position: rawPos,
          classPosition: rawClassPos,
          carNumber: d.carNumber,
          userName: d.userName,
          userId: d.userId,
          countryCode: resolveCountryCodeFromClub(d.clubName),
          clubName: d.clubName,
          iRating: d.iRating,
          iRatingDelta: 0,
          licenseClass: lic.licenseClass,
          safetyRating: lic.safetyRating,
          licenseString: lic.licenseString,
          licenseColorHex: LICENSE_COLORS[lic.licenseClass].bg,
          carClassShortName: d.carClassShortName,
          carClassColorHex: d.carClassColor.replace(/^0x/i, '#'),
          carName: d.carScreenNameShort,
          lastLapTimeSec: lastLap > 0 ? lastLap : 0,
          bestLapTimeSec: bestLap > 0 ? bestLap : 0,
          lastLapStatus,
          gapToLeaderSec: gapToLeader,
          intervalAheadSec: 0,
          lap,
          lapDistPct,
          onPitRoad,
          isPlayer: idx === snap.playerCarIdx,
          incidentCount: d.incidentCount,
        };
      });

      standings.sort((a, b) => {
        if (a.position > 0 && b.position > 0) return a.position - b.position;
        if (a.position > 0) return -1;
        if (b.position > 0) return 1;
        if (!isRaceSession) {
          if (a.bestLapTimeSec > 0 && b.bestLapTimeSec > 0) {
            return a.bestLapTimeSec - b.bestLapTimeSec;
          }
          if (a.bestLapTimeSec > 0) return -1;
          if (b.bestLapTimeSec > 0) return 1;
        }
        const progA = a.lap + a.lapDistPct;
        const progB = b.lap + b.lapDistPct;
        if (Math.abs(progA - progB) > 0.001) return progB - progA;
        return b.iRating - a.iRating;
      });

      const leaderBestLap = standings[0]?.bestLapTimeSec ?? 0;

      standings.forEach((driver, index) => {
        driver.position = index + 1;
        if (driver.classPosition <= 0) driver.classPosition = index + 1;
        if (index === 0) {
          driver.gapToLeaderSec = 0;
          driver.intervalAheadSec = 0;
        } else {
          if (!isRaceSession) {
            driver.gapToLeaderSec =
              driver.bestLapTimeSec > 0 && leaderBestLap > 0
                ? Math.max(0, Number((driver.bestLapTimeSec - leaderBestLap).toFixed(3)))
                : 0;
          }
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

      const sessionTimeRemainSec = Math.max(0, Number(snap.sessionTimeRemain ?? 0));
      const rawTotalLaps = Number(snap.sessionLapsRemain ?? 0);

      this.latestPayload = {
        connected: true,
        source: 'iracing-live',
        timestamp: Date.now(),
        session: {
          trackDisplayName,
          trackConfigName: '',
          sessionType,
          strengthOfField: sof,
          currentLap: Math.max(1, ...standings.map((s) => s.lap)),
          totalLaps: rawTotalLaps > 0 && rawTotalLaps < 32767 ? rawTotalLaps : 0,
          sessionTimeRemainSec,
          isTimedSession: sessionTimeRemainSec > 0 && sessionTimeRemainSec < 86000,
          flagState: 'GREEN',
          airTempC: Number(snap.airTemp ?? 22),
          trackTempC: Number(snap.trackTemp ?? 32),
        },
        standings,
      };
    } catch {
      // Ignore malformed line
    }
  }
}
