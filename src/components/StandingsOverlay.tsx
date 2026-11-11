import React, { useMemo, useState } from 'react';
import {
  DriverStanding,
  OverlayState,
  TelemetryPayload,
  TelemetrySourceMode,
} from '../types/telemetry';
import {
  formatGap,
  formatIRating,
  formatLapTime,
  LICENSE_COLORS,
} from '../utils/formatters';
import { CountryFlag } from './CountryFlag';

interface StandingsOverlayProps {
  payload: TelemetryPayload;
  overlayState: OverlayState;
  onToggleLock: () => void;
  onChangeMode: (mode: TelemetrySourceMode) => void;
}

function formatSessionTimer(seconds: number): string {
  if (!seconds || seconds <= 0) return '00:00';
  const hrs = Math.floor(seconds / 3600);
  const mins = Math.floor((seconds % 3600) / 60);
  const secs = Math.floor(seconds % 60);
  if (hrs > 0) {
    return `${hrs}:${String(mins).padStart(2, '0')}:${String(secs).padStart(2, '0')}`;
  }
  return `${String(mins).padStart(2, '0')}:${String(secs).padStart(2, '0')}`;
}

export const StandingsOverlay: React.FC<StandingsOverlayProps> = ({
  payload,
  overlayState,
  onToggleLock,
  onChangeMode,
}) => {
  const [compactIR] = useState(false);
  const [showDeltaIR] = useState(true);
  const [showCarNumber, setShowCarNumber] = useState(true);
  const [gapMode, setGapMode] = useState<'gap' | 'interval'>('gap');
  const [cornerMode, setCornerMode] = useState<'resize' | 'font'>('resize');
  const [widgetSize, setWidgetSize] = useState({ width: 640, height: 592 });
  const [fontScale, setFontScale] = useState(1.0);

  const { session, standings, source, connected } = payload;

  const maxFitRows = Math.max(
    1,
    Math.floor((widgetSize.height - 116 * fontScale) / (29 * fontScale))
  );

  const visibleStandings = useMemo<DriverStanding[]>(() => {
    if (standings.length <= maxFitRows) {
      return standings;
    }
    const playerIdx = Math.max(
      0,
      standings.findIndex((d) => d.isPlayer)
    );
    const halfBefore = Math.floor(maxFitRows / 2);
    let startIdx = playerIdx - halfBefore;
    if (startIdx < 0) startIdx = 0;
    if (startIdx + maxFitRows > standings.length) {
      startIdx = standings.length - maxFitRows;
    }
    return standings.slice(startIdx, startIdx + maxFitRows);
  }, [standings, maxFitRows]);

  const startCornerDrag = (
    e: React.MouseEvent,
    corner: 'tl' | 'tr' | 'bl' | 'br'
  ) => {
    if (overlayState.locked) return;
    e.preventDefault();
    e.stopPropagation();
    const startX = e.clientX;
    const startY = e.clientY;
    const startW = widgetSize.width;
    const startH = widgetSize.height;
    const startFont = fontScale;

    const onMove = (moveEvent: MouseEvent) => {
      const dx = moveEvent.clientX - startX;
      const dy = moveEvent.clientY - startY;

      if (cornerMode === 'font') {
        let outward = dx + dy;
        if (corner === 'bl') outward = -dx + dy;
        else if (corner === 'tr') outward = dx - dy;
        else if (corner === 'tl') outward = -dx - dy;
        const nextScale = Math.max(
          0.7,
          Math.min(1.65, Number((startFont + outward * 0.0025).toFixed(2)))
        );
        setFontScale(nextScale);
        const ratio = nextScale / startFont;
        setWidgetSize({
          width: Math.max(460, Math.round(startW * ratio)),
          height: Math.max(210, Math.round(startH * ratio)),
        });
      } else {
        const signX = corner === 'br' || corner === 'tr' ? 1 : -1;
        const signY = corner === 'br' || corner === 'bl' ? 1 : -1;
        setWidgetSize({
          width: Math.max(460, startW + dx * signX),
          height: Math.max(210, startH + dy * signY),
        });
      }
    };

    const onUp = () => {
      window.removeEventListener('mousemove', onMove);
      window.removeEventListener('mouseup', onUp);
    };

    window.addEventListener('mousemove', onMove);
    window.addEventListener('mouseup', onUp);
  };

  return (
    <div
      className={`overlay-container ${overlayState.locked ? 'is-locked' : 'is-unlocked'} ${!showCarNumber ? 'no-car-num' : ''}`}
      style={
        {
          '--widget-width': `${widgetSize.width}px`,
          '--widget-height': `${widgetSize.height}px`,
          '--font-scale': fontScale,
          '--active-corner-accent':
            cornerMode === 'resize' ? '#38bdf8' : '#c084fc',
        } as React.CSSProperties
      }
    >
      {/* SimLab-style Corner Drag Handles (appear on hover when unlocked) */}
      {!overlayState.locked && (
        <>
          <div
            className="corner-grip corner-tl"
            onMouseDown={(e) => startCornerDrag(e, 'tl')}
          />
          <div
            className="corner-grip corner-tr"
            onMouseDown={(e) => startCornerDrag(e, 'tr')}
          />
          <div
            className="corner-grip corner-bl"
            onMouseDown={(e) => startCornerDrag(e, 'bl')}
          />
          <div
            className="corner-grip corner-br"
            onMouseDown={(e) => startCornerDrag(e, 'br')}
          />
        </>
      )}

      {/* Top Drag & Control Toolbar (Interactive when unlocked) */}
      <div className="overlay-toolbar">
        <div className="drag-region" title="Drag to reposition overlay window">
          <span className="brand-dot" />
          <span className="brand-title">FREE OVERLAY</span>
          <span
            className={`source-pill ${
              source === 'iracing-live' && connected ? 'source-live' : 'source-sim'
            }`}
          >
            {source === 'iracing-live'
              ? connected
                ? 'LIVE SDK'
                : 'WAITING FOR SIM'
              : 'MOCK SIMULATOR'}
          </span>
        </div>

        <div className="toolbar-actions">
          {!overlayState.locked && (
            <div className="hover-only-actions">
              <div className="segmented-group" title="Telemetry Source (Hotkey: F7)">
                {(['auto', 'mock', 'live'] as TelemetrySourceMode[]).map((m) => (
                  <button
                    key={m}
                    type="button"
                    className={`seg-btn ${overlayState.mode === m ? 'active' : ''}`}
                    onClick={() => onChangeMode(m)}
                  >
                    {m.toUpperCase()}
                  </button>
                ))}
              </div>

              <button
                type="button"
                className="ctrl-btn"
                onClick={() => setShowCarNumber((prev) => !prev)}
                title="Show or Hide Car Number (#) Column"
              >
                {showCarNumber ? '# ON' : '# OFF'}
              </button>

              <button
                type="button"
                className="ctrl-btn"
                onClick={() => setGapMode((prev) => (prev === 'gap' ? 'interval' : 'gap'))}
                title="Toggle Gap to Leader vs Interval Ahead"
              >
                {gapMode === 'gap' ? 'GAP' : 'INT'}
              </button>

              <button
                type="button"
                className={`simlab-mode-btn ${
                  cornerMode === 'resize' ? 'active-resize' : 'active-font'
                }`}
                onClick={() =>
                  setCornerMode((prev) => (prev === 'resize' ? 'font' : 'resize'))
                }
                title={
                  cornerMode === 'resize'
                    ? 'Mode: Resize Window (Click to switch to Font Size)'
                    : 'Mode: Font Size (Click to switch to Resize Window)'
                }
              >
                {cornerMode === 'resize' ? (
                  <svg width="13" height="13" viewBox="0 0 16 16" fill="none">
                    <path
                      d="M3 13L13 3M13 3H8.5M13 3V7.5M3 13H7.5M3 13V8.5"
                      stroke="currentColor"
                      strokeWidth="2"
                      strokeLinecap="round"
                      strokeLinejoin="round"
                    />
                  </svg>
                ) : (
                  <span>Aa</span>
                )}
              </button>
            </div>
          )}

          <button
            type="button"
            className={`lock-btn ${overlayState.locked ? 'locked' : 'unlocked'}`}
            onClick={onToggleLock}
            title="Lock/Unlock Click-Through HUD Mode (Global Hotkey: F6)"
          >
            {overlayState.locked ? 'LOCKED (F6)' : 'UNLOCKED (F6)'}
          </button>
        </div>
      </div>

      {/* Session Info Header Bar */}
      <div className="session-header">
        <div className="session-left">
          <span className="class-tag">GT3</span>
          <span className="session-type">{session.sessionType.toUpperCase()}</span>
          <span className="track-name">{session.trackDisplayName}</span>
        </div>
        <div className="session-right">
          <span className="meta-pill">
            <span className="meta-label">LAP</span>
            <span className="meta-value">
              {session.currentLap}
              {session.totalLaps > 0 ? `/${session.totalLaps}` : ''}
            </span>
          </span>
          {session.isTimedSession && (
            <span className="meta-pill">
              <span className="meta-label">REM</span>
              <span className="meta-value">{formatSessionTimer(session.sessionTimeRemainSec)}</span>
            </span>
          )}
          <span className="meta-pill sof-pill" title="Strength of Field">
            <span className="meta-label">SOF</span>
            <span className="meta-value">{session.strengthOfField.toLocaleString('en-US')}</span>
          </span>
          <span className="meta-pill temp-pill" title="Track Temperature">
            <span className="meta-label">TRK</span>
            <span className="meta-value">{session.trackTempC.toFixed(0)}°C</span>
          </span>
        </div>
      </div>

      {/* Column Headers */}
      <div className="standings-table-header">
        <span className="col-pos">P</span>
        {showCarNumber && <span className="col-num">#</span>}
        <span className="col-flag">FLG</span>
        <span className="col-driver">DRIVER</span>
        <span className="col-sr">SR</span>
        <span className="col-ir">iRATING</span>
        <span className="col-gap">{gapMode === 'gap' ? 'GAP' : 'INT'}</span>
        <span className="col-lap">LAST LAP</span>
      </div>

      {/* Standings Rows */}
      <div className="standings-body">
        {visibleStandings.length === 0 ? (
          <div className="empty-standings">
            Waiting for iRacing session telemetry... (Press F7 or select MOCK to preview)
          </div>
        ) : (
          visibleStandings.map((driver) => {
            const licStyle = LICENSE_COLORS[driver.licenseClass] ?? LICENSE_COLORS.R;
            const gapValue =
              gapMode === 'gap' ? driver.gapToLeaderSec : driver.intervalAheadSec;

            return (
              <div
                key={driver.carIdx}
                className={`standing-row ${driver.isPlayer ? 'is-player' : ''} ${
                  driver.onPitRoad ? 'in-pits' : ''
                }`}
              >
                {/* Position */}
                <div className="col-pos">
                  <span
                    className={`pos-badge ${
                      driver.position === 1
                        ? 'pos-p1'
                        : driver.position === 2
                          ? 'pos-p2'
                          : driver.position === 3
                            ? 'pos-p3'
                            : ''
                    }`}
                  >
                    {driver.position}
                  </span>
                </div>

                {/* Car Number */}
                {showCarNumber && (
                  <div className="col-num">
                    <span
                      className="car-number-badge"
                      style={{ borderLeftColor: driver.carClassColorHex }}
                    >
                      #{driver.carNumber}
                    </span>
                  </div>
                )}

                {/* Country Flag */}
                <div className="col-flag">
                  <CountryFlag
                    countryCode={driver.countryCode}
                    clubName={driver.clubName}
                  />
                </div>

                {/* Driver Name */}
                <div className="col-driver">
                  <span className="driver-name">{driver.userName}</span>
                  {driver.isPlayer && <span className="you-tag">YOU</span>}
                  {driver.onPitRoad && <span className="pit-tag">PIT</span>}
                </div>

                {/* Safety Rating & License Class Badge */}
                <div className="col-sr">
                  <span
                    className="sr-badge"
                    style={{
                      backgroundColor: licStyle.bg,
                      color: licStyle.text,
                      borderColor: licStyle.border,
                    }}
                    title={`License Class ${driver.licenseClass} • Safety Rating ${driver.safetyRating}`}
                  >
                    <strong className="sr-class">{driver.licenseClass}</strong>
                    <span className="sr-val">{driver.safetyRating}</span>
                  </span>
                </div>

                {/* Current iRating + Live Projected Delta */}
                <div className="col-ir">
                  <span className="ir-badge">
                    <span className="ir-value">
                      {formatIRating(driver.iRating, compactIR)}
                    </span>
                    {showDeltaIR && (
                      <span
                        className={`ir-delta ${
                          driver.iRatingDelta > 0
                            ? 'delta-pos'
                            : driver.iRatingDelta < 0
                              ? 'delta-neg'
                              : 'delta-zero'
                        }`}
                      >
                        {driver.iRatingDelta > 0
                          ? `+${driver.iRatingDelta}`
                          : driver.iRatingDelta}
                      </span>
                    )}
                  </span>
                </div>

                {/* Gap / Interval */}
                <div className="col-gap">
                  {driver.onPitRoad ? (
                    <span className="gap-pit">IN PIT</span>
                  ) : (
                    <span className="gap-text">
                      {formatGap(gapValue, driver.position === 1)}
                    </span>
                  )}
                </div>

                {/* Last Lap Time */}
                <div className="col-lap">
                  <span className={`lap-time lap-${driver.lastLapStatus}`}>
                    {formatLapTime(driver.lastLapTimeSec)}
                  </span>
                </div>
              </div>
            );
          })
        )}
      </div>

      {/* Footer Legend */}
      <div className="overlay-footer">
        <div className="legend-items">
          <span className="legend-item">
            <span className="dot dot-purple" /> Session Best
          </span>
          <span className="legend-item">
            <span className="dot dot-green" /> Personal Best
          </span>
        </div>
        <div className="hotkey-hint">
          <span>F6: Lock/Click-Through</span>
          <span>•</span>
          <span>F7: Live/Sim Mode</span>
        </div>
      </div>
    </div>
  );
};
