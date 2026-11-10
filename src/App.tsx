import React, { useEffect, useRef, useState } from 'react';
import { StandingsOverlay } from './components/StandingsOverlay';
import {
  OverlayState,
  TelemetryPayload,
  TelemetrySourceMode,
} from './types/telemetry';
import { MockTelemetrySimulator } from './utils/mockSimulator';

const initialSimulator = new MockTelemetrySimulator();

export const App: React.FC = () => {
  const [payload, setPayload] = useState<TelemetryPayload>(() =>
    initialSimulator.tick(0)
  );
  const [overlayState, setOverlayState] = useState<OverlayState>({
    locked: false,
    mode: 'auto',
    opacity: 0.94,
    maxRows: 16,
  });

  const browserSimRef = useRef<MockTelemetrySimulator>(initialSimulator);

  useEffect(() => {
    const bridge = window.iracingOverlay;
    if (bridge) {
      bridge.getInitialState().then((st) => setOverlayState(st));
      const unsubTelemetry = bridge.onTelemetryUpdate((nextPayload) => {
        setPayload(nextPayload);
      });
      const unsubState = bridge.onOverlayStateChange((nextState) => {
        setOverlayState(nextState);
      });
      return () => {
        unsubTelemetry();
        unsubState();
      };
    } else {
      // Browser fallback mode: run the built-in MockTelemetrySimulator at 4Hz
      const timer = setInterval(() => {
        setPayload(browserSimRef.current.tick(0.25));
      }, 250);
      return () => clearInterval(timer);
    }
  }, []);

  const handleToggleLock = async () => {
    const nextLocked = !overlayState.locked;
    if (window.iracingOverlay) {
      const updated = await window.iracingOverlay.setOverlayLocked(nextLocked);
      setOverlayState(updated);
    } else {
      setOverlayState((prev) => ({ ...prev, locked: nextLocked }));
    }
  };

  const handleChangeMode = async (mode: TelemetrySourceMode) => {
    if (window.iracingOverlay) {
      const updated = await window.iracingOverlay.setTelemetryMode(mode);
      setOverlayState(updated);
    } else {
      setOverlayState((prev) => ({ ...prev, mode }));
    }
  };

  return (
    <StandingsOverlay
      payload={payload}
      overlayState={overlayState}
      onToggleLock={handleToggleLock}
      onChangeMode={handleChangeMode}
    />
  );
};
