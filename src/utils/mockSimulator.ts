import { DriverStanding, LapTimeStatus, TelemetryPayload } from '../types/telemetry';
import { estimateIRatingDelta, LICENSE_COLORS, parseLicenseString } from './formatters';
import { resolveCountryCodeFromClub } from './flags';

interface MockDriverSeed {
  carIdx: number;
  carNumber: string;
  userName: string;
  clubName: string;
  countryCode: string;
  iRating: number;
  licString: string;
  carName: string;
  basePaceSec: number;
  isPlayer?: boolean;
}

const MOCK_SEEDS: MockDriverSeed[] = [
  { carIdx: 1, carNumber: '1', userName: 'Maxime Van Der Berg', clubName: 'Benelux', countryCode: 'NL', iRating: 5420, licString: 'A 4.99', carName: 'Porsche 911 GT3 R (992)', basePaceSec: 137.185 },
  { carIdx: 2, carNumber: '14', userName: 'Lukas Schneider', clubName: 'DE-AT-CH', countryCode: 'DE', iRating: 4890, licString: 'A 4.42', carName: 'BMW M4 GT3', basePaceSec: 137.310 },
  { carIdx: 3, carNumber: '23', userName: 'Callum Sterling', clubName: 'UK and I', countryCode: 'GB', iRating: 4510, licString: 'A 3.85', carName: 'Ferrari 296 GT3', basePaceSec: 137.440 },
  { carIdx: 4, carNumber: '7', userName: 'Alex Nestorenko', clubName: 'California', countryCode: 'US', iRating: 4125, licString: 'A 4.18', carName: 'Porsche 911 GT3 R (992)', basePaceSec: 137.515, isPlayer: true },
  { carIdx: 5, carNumber: '88', userName: 'Mikko Virtanen', clubName: 'Finland', countryCode: 'FI', iRating: 3980, licString: 'A 3.64', carName: 'Mercedes-AMG GT3 2020', basePaceSec: 137.620 },
  { carIdx: 6, carNumber: '44', userName: 'Gabriel Oliveira', clubName: 'Brazil', countryCode: 'BR', iRating: 3740, licString: 'B 3.92', carName: 'Ferrari 296 GT3', basePaceSec: 137.790 },
  { carIdx: 7, carNumber: '19', userName: 'Antoine Moreau', clubName: 'France', countryCode: 'FR', iRating: 3560, licString: 'A 2.74', carName: 'Audi R8 LMS EVO II GT3', basePaceSec: 137.880 },
  { carIdx: 8, carNumber: '63', userName: 'Marco Rossini', clubName: 'Italy', countryCode: 'IT', iRating: 3410, licString: 'B 3.45', carName: 'Ferrari 296 GT3', basePaceSec: 138.010 },
  { carIdx: 9, carNumber: '27', userName: 'Liam O\'Connor', clubName: 'Australia/NZ', countryCode: 'AU', iRating: 3280, licString: 'A 4.05', carName: 'McLaren 720S GT3 EVO', basePaceSec: 138.125 },
  { carIdx: 10, carNumber: '91', userName: 'Carlos Mendoza', clubName: 'Iberia', countryCode: 'ES', iRating: 3115, licString: 'B 2.58', carName: 'Porsche 911 GT3 R (992)', basePaceSec: 138.240 },
  { carIdx: 11, carNumber: '52', userName: 'Tyler Vance', clubName: 'New England', countryCode: 'US', iRating: 2940, licString: 'B 3.12', carName: 'Corvette Z06 GT3.R', basePaceSec: 138.390 },
  { carIdx: 12, carNumber: '33', userName: 'Henrik Lindqvist', clubName: 'Scandinavia', countryCode: 'SE', iRating: 2785, licString: 'A 2.31', carName: 'BMW M4 GT3', basePaceSec: 138.510 },
  { carIdx: 13, carNumber: '12', userName: 'Kenji Takahashi', clubName: 'Japan', countryCode: 'JP', iRating: 2610, licString: 'B 4.40', carName: 'Acura NSX GT3 EVO 22', basePaceSec: 138.680 },
  { carIdx: 14, carNumber: '77', userName: 'Connor MacLeod', clubName: 'Canada', countryCode: 'CA', iRating: 2450, licString: 'C 3.76', carName: 'Ford Mustang GT3', basePaceSec: 138.850 },
  { carIdx: 15, carNumber: '99', userName: 'Ruben Peeters', clubName: 'Benelux', countryCode: 'BE', iRating: 2290, licString: 'C 2.91', carName: 'Porsche 911 GT3 R (992)', basePaceSec: 139.020 },
  { carIdx: 16, carNumber: '48', userName: 'Piotr Kowalski', clubName: 'Central-Eastern Europe', countryCode: 'PL', iRating: 2110, licString: 'B 1.88', carName: 'BMW M4 GT3', basePaceSec: 139.240 },
];

export class MockTelemetrySimulator {
  private drivers: DriverStanding[] = [];
  private sessionBestLapSec = 137.142;
  private sessionTimeRemainSec = 1425; // ~23m45s left in a 40m GT3 Sprint race
  private sof = 3451;

  constructor() {
    this.initializeField();
  }

  private initializeField(): void {
    const totalIR = MOCK_SEEDS.reduce((acc, s) => acc + s.iRating, 0);
    this.sof = Math.round(totalIR / MOCK_SEEDS.length);

    this.drivers = MOCK_SEEDS.map((seed, idx) => {
      const position = idx + 1;
      const lic = parseLicenseString(seed.licString);
      const bestLap = Number((seed.basePaceSec - 0.12 + (idx === 0 ? -0.043 : 0.03 * (idx % 3))).toFixed(3));
      const lastLap = Number((seed.basePaceSec + (Math.sin(idx * 1.7) * 0.22)).toFixed(3));

      let lastLapStatus: LapTimeStatus = 'normal';
      if (idx === 0) {
        lastLapStatus = 'session-best';
      } else if (idx === 3 || idx === 6) {
        lastLapStatus = 'personal-best';
      }

      const gapToLeaderSec = idx === 0 ? 0 : Number((idx * 1.38 + Math.sin(idx) * 0.45).toFixed(2));
      const prevGap = idx <= 1 ? 0 : Number(((idx - 1) * 1.38 + Math.sin(idx - 1) * 0.45).toFixed(2));

      return {
        carIdx: seed.carIdx,
        position,
        classPosition: position,
        carNumber: seed.carNumber,
        userName: seed.userName,
        userId: 100000 + seed.carIdx * 421,
        countryCode: resolveCountryCodeFromClub(seed.clubName, seed.countryCode),
        clubName: seed.clubName,
        iRating: seed.iRating,
        iRatingDelta: estimateIRatingDelta(position, MOCK_SEEDS.length, seed.iRating, this.sof),
        licenseClass: lic.licenseClass,
        safetyRating: lic.safetyRating,
        licenseString: lic.licenseString,
        licenseColorHex: LICENSE_COLORS[lic.licenseClass].bg,
        carClassShortName: 'GT3',
        carClassColorHex: '#ff5252',
        carName: seed.carName,
        lastLapTimeSec: idx === 0 ? this.sessionBestLapSec : lastLap,
        bestLapTimeSec: idx === 0 ? this.sessionBestLapSec : Math.min(bestLap, lastLap),
        lastLapStatus,
        gapToLeaderSec,
        intervalAheadSec: idx === 0 ? 0 : Math.max(0.2, Number((gapToLeaderSec - prevGap).toFixed(2))),
        lap: 8,
        lapDistPct: Number((0.82 - idx * 0.011).toFixed(4)),
        onPitRoad: idx === 14,
        isPlayer: Boolean(seed.isPlayer),
        incidentCount: (idx * 2) % 7,
      };
    });
  }

  public tick(dtSec = 0.25): TelemetryPayload {
    this.sessionTimeRemainSec = Math.max(0, this.sessionTimeRemainSec - dtSec);

    this.drivers.forEach((driver, idx) => {
      const seed = MOCK_SEEDS.find((s) => s.carIdx === driver.carIdx) ?? MOCK_SEEDS[0];
      const lapSpeedPctPerSec = 1 / seed.basePaceSec;
      const jitter = (Math.random() - 0.495) * 0.0005;
      const prevDist = driver.lapDistPct;
      const nextDist = (prevDist + lapSpeedPctPerSec * dtSec + jitter) % 1;

      // Crossed start/finish line -> generate new last lap time
      if (nextDist < prevDist) {
        driver.lap += 1;
        const variance = (Math.random() - 0.47) * 0.55;
        const newLapTime = Number((seed.basePaceSec + variance).toFixed(3));
        driver.lastLapTimeSec = newLapTime;

        if (newLapTime < this.sessionBestLapSec) {
          this.sessionBestLapSec = newLapTime;
          // Clear previous session-best markers
          this.drivers.forEach((d) => {
            if (d.lastLapStatus === 'session-best') d.lastLapStatus = 'personal-best';
          });
          driver.bestLapTimeSec = newLapTime;
          driver.lastLapStatus = 'session-best';
        } else if (newLapTime <= driver.bestLapTimeSec) {
          driver.bestLapTimeSec = newLapTime;
          driver.lastLapStatus = 'personal-best';
        } else {
          driver.lastLapStatus = 'normal';
        }
      }

      driver.lapDistPct = Number(nextDist.toFixed(4));

      // Subtle dynamic interval changes
      if (idx > 0) {
        const deltaJitter = (Math.random() - 0.5) * 0.04;
        driver.intervalAheadSec = Number(
          Math.max(0.15, driver.intervalAheadSec + deltaJitter).toFixed(2)
        );
      }
    });

    // Recompute cumulative gaps to leader
    let runningGap = 0;
    this.drivers.forEach((driver, idx) => {
      driver.position = idx + 1;
      driver.classPosition = idx + 1;
      if (idx === 0) {
        driver.gapToLeaderSec = 0;
        driver.intervalAheadSec = 0;
      } else {
        runningGap += driver.intervalAheadSec;
        driver.gapToLeaderSec = Number(runningGap.toFixed(2));
      }
      driver.iRatingDelta = estimateIRatingDelta(
        driver.position,
        this.drivers.length,
        driver.iRating,
        this.sof
      );
    });

    return {
      connected: true,
      source: 'mock-simulator',
      timestamp: Date.now(),
      session: {
        trackDisplayName: 'Circuit de Spa-Francorchamps',
        trackConfigName: 'Grand Prix Pits',
        sessionType: 'Race',
        strengthOfField: this.sof,
        currentLap: Math.max(...this.drivers.map((d) => d.lap)),
        totalLaps: 20,
        sessionTimeRemainSec: this.sessionTimeRemainSec,
        isTimedSession: true,
        flagState: 'GREEN',
        airTempC: 21.4,
        trackTempC: 31.8,
      },
      standings: this.drivers.map((d) => ({ ...d })),
    };
  }
}
