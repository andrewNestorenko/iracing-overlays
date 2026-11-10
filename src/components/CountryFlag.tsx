import React from 'react';
import { FLAG_SPECS } from '../utils/flags';

interface CountryFlagProps {
  countryCode: string;
  clubName?: string;
}

export const CountryFlag: React.FC<CountryFlagProps> = ({ countryCode, clubName }) => {
  const code = (countryCode || 'UN').toUpperCase();
  const spec = FLAG_SPECS[code];
  const title = clubName ? `${clubName} (${code})` : code;

  if (!spec) {
    return (
      <span className="flag-badge flag-fallback" title={title}>
        {code}
      </span>
    );
  }

  const renderSvgContent = () => {
    switch (spec.direction) {
      case 'vertical':
        return (
          <>
            <rect width="8" height="16" x="0" y="0" fill={spec.bands[0]} />
            <rect width="8" height="16" x="8" y="0" fill={spec.bands[1]} />
            <rect width="8" height="16" x="16" y="0" fill={spec.bands[2]} />
          </>
        );
      case 'cross':
        return (
          <>
            <rect width="24" height="16" fill={spec.bands[0]} />
            <rect width="24" height="3.6" y="6.2" fill={spec.bands[1]} />
            <rect width="3.6" height="16" x="7" fill={spec.bands[1]} />
          </>
        );
      case 'jp':
        return (
          <>
            <rect width="24" height="16" fill="#FFFFFF" />
            <circle cx="12" cy="8" r="4.6" fill="#BC002D" />
          </>
        );
      case 'br':
        return (
          <>
            <rect width="24" height="16" fill="#009739" />
            <polygon points="12,2 21.5,8 12,14 2.5,8" fill="#FEDD00" />
            <circle cx="12" cy="8" r="3.2" fill="#012169" />
          </>
        );
      case 'uk':
        return (
          <>
            <rect width="24" height="16" fill="#012169" />
            <path d="M0,0 L24,16 M24,0 L0,16" stroke="#FFFFFF" strokeWidth="3.2" />
            <path d="M0,0 L24,16 M24,0 L0,16" stroke="#C8102E" strokeWidth="1.6" />
            <rect width="24" height="4.8" y="5.6" fill="#FFFFFF" />
            <rect width="4.8" height="16" x="9.6" fill="#FFFFFF" />
            <rect width="24" height="2.8" y="6.6" fill="#C8102E" />
            <rect width="2.8" height="16" x="10.6" fill="#C8102E" />
          </>
        );
      case 'us':
        return (
          <>
            <rect width="24" height="16" fill="#FFFFFF" />
            <rect width="24" height="2.3" y="0" fill="#B22234" />
            <rect width="24" height="2.3" y="4.6" fill="#B22234" />
            <rect width="24" height="2.3" y="9.2" fill="#B22234" />
            <rect width="24" height="2.3" y="13.7" fill="#B22234" />
            <rect width="11" height="8.8" x="0" y="0" fill="#3C3B6E" />
          </>
        );
      case 'ca':
        return (
          <>
            <rect width="6" height="16" x="0" y="0" fill="#FF0000" />
            <rect width="12" height="16" x="6" y="0" fill="#FFFFFF" />
            <rect width="6" height="16" x="18" y="0" fill="#FF0000" />
            <polygon points="12,3.5 14.8,8.5 13,8.5 13,12.5 11,12.5 11,8.5 9.2,8.5" fill="#FF0000" />
          </>
        );
      case 'au':
        return (
          <>
            <rect width="24" height="16" fill={spec.bands[0]} />
            <rect width="11" height="8" x="0" y="0" fill="#012169" />
            <rect width="11" height="2" y="3" fill="#FFFFFF" />
            <rect width="2" height="8" x="4.5" fill="#FFFFFF" />
            <rect width="11" height="1.2" y="3.4" fill="#C8102E" />
            <rect width="1.2" height="8" x="4.9" fill="#C8102E" />
            <circle cx="18" cy="5" r="1.1" fill="#FFFFFF" />
            <circle cx="16" cy="11" r="1.2" fill="#FFFFFF" />
            <circle cx="20" cy="10" r="1.1" fill="#FFFFFF" />
          </>
        );
      case 'horizontal':
      default:
        return (
          <>
            <rect width="24" height="5.34" x="0" y="0" fill={spec.bands[0]} />
            <rect width="24" height="5.34" x="0" y="5.33" fill={spec.bands[1]} />
            <rect width="24" height="5.34" x="0" y="10.66" fill={spec.bands[2]} />
          </>
        );
    }
  };

  return (
    <span className="flag-badge" title={title}>
      <svg
        width="20"
        height="13"
        viewBox="0 0 24 16"
        className="flag-svg"
        aria-label={title}
      >
        {renderSvgContent()}
        <rect
          width="23"
          height="15"
          x="0.5"
          y="0.5"
          fill="none"
          stroke="rgba(255,255,255,0.22)"
          strokeWidth="1"
        />
      </svg>
    </span>
  );
};
