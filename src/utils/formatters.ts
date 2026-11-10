import { LicenseClass } from '../types/telemetry';

/**
 * Formats seconds into M:SS.mmm (e.g., 137.842 -> "2:17.842")
 */
export function formatLapTime(seconds: number): string {
  if (!seconds || seconds <= 0 || !Number.isFinite(seconds)) {
    return '—:——.———';
  }
  const mins = Math.floor(seconds / 60);
  const remSec = seconds - mins * 60;
  const wholeSec = Math.floor(remSec);
  const millis = Math.round((remSec - wholeSec) * 1000);

  const secStr = String(wholeSec).padStart(2, '0');
  const msStr = String(Math.min(999, millis)).padStart(3, '0');

  return mins > 0 ? `${mins}:${secStr}.${msStr}` : `${wholeSec}.${msStr}`;
}

/**
 * Formats gap or interval in seconds (e.g., +1.42s or +1L)
 */
export function formatGap(seconds: number, isLeader: boolean): string {
  if (isLeader) return 'LDR';
  if (!seconds || seconds <= 0) return '—';
  if (seconds >= 60) {
    const mins = Math.floor(seconds / 60);
    const rem = (seconds - mins * 60).toFixed(1).padStart(4, '0');
    return `+${mins}:${rem}`;
  }
  return `+${seconds.toFixed(1)}s`;
}

/**
 * Formats iRating in either compact (e.g. "3.4k") or full ("3,420") format
 */
export function formatIRating(irating: number, compact = false): string {
  if (!irating || irating <= 0) return '—';
  if (compact && irating >= 1000) {
    return `${(irating / 1000).toFixed(1)}k`;
  }
  return irating.toLocaleString('en-US');
}

/**
 * Authentic iRacing License class colors
 */
export const LICENSE_COLORS: Record<LicenseClass, { bg: string; text: string; border: string }> = {
  R: { bg: '#d32f2f', text: '#ffffff', border: '#ef5350' }, // Rookie Red
  D: { bg: '#ef6c00', text: '#ffffff', border: '#ffa726' }, // Class D Orange
  C: { bg: '#f9a825', text: '#111318', border: '#ffee58' }, // Class C Yellow
  B: { bg: '#2e7d32', text: '#ffffff', border: '#66bb6a' }, // Class B Green
  A: { bg: '#0153db', text: '#ffffff', border: '#42a5f5' }, // Class A Blue
  P: { bg: '#1e222d', text: '#ffffff', border: '#90a4ae' }, // Pro / WC Black-Silver
};

/**
 * Parses iRacing LicString (e.g. "A 4.42" or "R 2.50") into structured fields
 */
export function parseLicenseString(licString?: string): {
  licenseClass: LicenseClass;
  safetyRating: string;
  licenseString: string;
  licenseColorHex: string;
} {
  if (!licString) {
    return {
      licenseClass: 'R',
      safetyRating: '2.50',
      licenseString: 'R 2.50',
      licenseColorHex: LICENSE_COLORS.R.bg,
    };
  }
  const clean = licString.trim();
  const match = clean.match(/^([RDCBAP])\s*([0-9]+\.[0-9]{2})/i);
  if (match) {
    const cls = match[1].toUpperCase() as LicenseClass;
    const sr = match[2];
    return {
      licenseClass: cls,
      safetyRating: sr,
      licenseString: `${cls} ${sr}`,
      licenseColorHex: LICENSE_COLORS[cls]?.bg ?? LICENSE_COLORS.R.bg,
    };
  }
  return {
    licenseClass: 'C',
    safetyRating: '3.00',
    licenseString: clean,
    licenseColorHex: LICENSE_COLORS.C.bg,
  };
}

/**
 * Estimates iRating gain/loss based on position, field size, driver iRating, and SOF
 */
export function estimateIRatingDelta(
  position: number,
  fieldSize: number,
  driverIRating: number,
  sof: number
): number {
  if (fieldSize <= 1 || position <= 0) return 0;
  const expectedNormalized = 1 / (1 + Math.pow(10, (sof - driverIRating) / 1600));
  const actualNormalized = (fieldSize - position) / (fieldSize - 1);
  const kFactor = Math.round(1800 / Math.sqrt(fieldSize));
  return Math.round((actualNormalized - expectedNormalized) * kFactor);
}
