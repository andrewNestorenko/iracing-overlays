/**
 * Maps iRacing ClubName / FlairName values to ISO 3166-1 alpha-2 country codes,
 * and provides crisp inline SVG flags so flags render reliably on Windows 10/11
 * (where Unicode flag emojis are not supported by default).
 */

const CLUB_TO_COUNTRY_MAP: Record<string, string> = {
  // North America
  'california': 'US',
  'texas': 'US',
  'florida': 'US',
  'new england': 'US',
  'new york': 'US',
  'midwest': 'US',
  'mid-south': 'US',
  'atlantic': 'US',
  'virginias': 'US',
  'carolinas': 'US',
  'georgia': 'US',
  'ohio': 'US',
  'michigan': 'US',
  'illinois': 'US',
  'indiana': 'US',
  'pennsylvania': 'US',
  'west': 'US',
  'northwest': 'US',
  'great plains': 'US',
  'usa': 'US',
  'united states': 'US',
  'canada': 'CA',
  'mexico': 'MX',

  // Europe
  'benelux': 'NL',
  'netherlands': 'NL',
  'belgium': 'BE',
  'uk and i': 'GB',
  'united kingdom': 'GB',
  'ireland': 'IE',
  'de-at-ch': 'DE',
  'germany': 'DE',
  'austria': 'AT',
  'switzerland': 'CH',
  'france': 'FR',
  'iberia': 'ES',
  'spain': 'ES',
  'portugal': 'PT',
  'italy': 'IT',
  'finland': 'FI',
  'scandinavia': 'SE',
  'sweden': 'SE',
  'norway': 'NO',
  'denmark': 'DK',
  'central-eastern europe': 'PL',
  'poland': 'PL',
  'czechia': 'CZ',

  // South America
  'brazil': 'BR',
  'hispanoamerica': 'AR',
  'argentina': 'AR',

  // Asia / Oceania / Rest of World
  'australia/nz': 'AU',
  'australia': 'AU',
  'new zealand': 'NZ',
  'japan': 'JP',
  'asia': 'SG',
  'south africa': 'ZA',
  'international': 'UN',
};

export function resolveCountryCodeFromClub(clubName?: string, explicitCode?: string): string {
  if (explicitCode && explicitCode.length === 2) {
    return explicitCode.toUpperCase();
  }
  if (!clubName) return 'UN';
  const normalized = clubName.trim().toLowerCase();
  if (CLUB_TO_COUNTRY_MAP[normalized]) {
    return CLUB_TO_COUNTRY_MAP[normalized];
  }
  for (const [key, code] of Object.entries(CLUB_TO_COUNTRY_MAP)) {
    if (normalized.includes(key)) {
      return code;
    }
  }
  return 'US';
}

export interface FlagSpec {
  code: string;
  label: string;
  bands: string[]; // 3-band or custom representation for crisp SVG rendering
  direction?: 'horizontal' | 'vertical' | 'cross' | 'uk' | 'us' | 'br' | 'jp' | 'ca' | 'au';
}

export const FLAG_SPECS: Record<string, FlagSpec> = {
  NL: { code: 'NL', label: 'Netherlands', bands: ['#AE1C28', '#FFFFFF', '#21468B'], direction: 'horizontal' },
  DE: { code: 'DE', label: 'Germany', bands: ['#000000', '#DD0000', '#FFCE00'], direction: 'horizontal' },
  BE: { code: 'BE', label: 'Belgium', bands: ['#000000', '#FDDA24', '#EF3340'], direction: 'vertical' },
  FR: { code: 'FR', label: 'France', bands: ['#002395', '#FFFFFF', '#ED2939'], direction: 'vertical' },
  IT: { code: 'IT', label: 'Italy', bands: ['#009246', '#FFFFFF', '#CE2B37'], direction: 'vertical' },
  ES: { code: 'ES', label: 'Spain', bands: ['#AA151B', '#F1BF00', '#AA151B'], direction: 'horizontal' },
  PT: { code: 'PT', label: 'Portugal', bands: ['#006600', '#FF0000', '#FF0000'], direction: 'vertical' },
  AT: { code: 'AT', label: 'Austria', bands: ['#ED2939', '#FFFFFF', '#ED2939'], direction: 'horizontal' },
  PL: { code: 'PL', label: 'Poland', bands: ['#FFFFFF', '#DC143C', '#DC143C'], direction: 'horizontal' },
  IE: { code: 'IE', label: 'Ireland', bands: ['#169B62', '#FFFFFF', '#FF883E'], direction: 'vertical' },
  MX: { code: 'MX', label: 'Mexico', bands: ['#006847', '#FFFFFF', '#CE1126'], direction: 'vertical' },
  AR: { code: 'AR', label: 'Argentina', bands: ['#74ACDF', '#FFFFFF', '#74ACDF'], direction: 'horizontal' },
  FI: { code: 'FI', label: 'Finland', bands: ['#FFFFFF', '#003580'], direction: 'cross' },
  SE: { code: 'SE', label: 'Sweden', bands: ['#006AA7', '#FECC02'], direction: 'cross' },
  NO: { code: 'NO', label: 'Norway', bands: ['#BA0C2F', '#00205B'], direction: 'cross' },
  DK: { code: 'DK', label: 'Denmark', bands: ['#C8102E', '#FFFFFF'], direction: 'cross' },
  GB: { code: 'GB', label: 'United Kingdom', bands: ['#012169', '#FFFFFF', '#C8102E'], direction: 'uk' },
  US: { code: 'US', label: 'United States', bands: ['#B22234', '#FFFFFF', '#3C3B6E'], direction: 'us' },
  BR: { code: 'BR', label: 'Brazil', bands: ['#009739', '#FEDD00', '#012169'], direction: 'br' },
  JP: { code: 'JP', label: 'Japan', bands: ['#FFFFFF', '#BC002D'], direction: 'jp' },
  CA: { code: 'CA', label: 'Canada', bands: ['#FF0000', '#FFFFFF', '#FF0000'], direction: 'ca' },
  AU: { code: 'AU', label: 'Australia', bands: ['#00008B', '#FFFFFF', '#FF0000'], direction: 'au' },
  NZ: { code: 'NZ', label: 'New Zealand', bands: ['#00247D', '#FFFFFF', '#CC142B'], direction: 'au' },
};
