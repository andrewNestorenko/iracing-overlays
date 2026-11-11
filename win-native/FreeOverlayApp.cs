using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace FreeOverlay
{
    public enum TelemetryMode
    {
        Auto = 0,
        Mock = 1,
        Live = 2
    }

    public enum LapStatus
    {
        Normal = 0,
        PersonalBest = 1,
        SessionBest = 2
    }

    public enum CornerDragMode
    {
        ResizeWindow = 0,
        ChangeFontSize = 1
    }

    public enum DragCorner
    {
        None = 0,
        TopLeft = 1,
        TopRight = 2,
        BottomLeft = 3,
        BottomRight = 4
    }

    public enum DeltaReferenceMode
    {
        SessionBest = 0,
        SessionOptimal = 1,
        PersonalBest = 2,
        LastLap = 3
    }

    public enum Garage61LayoutStyle
    {
        OptionA_LookaheadTraceAndPedals = 0,
        OptionB_CompactBrakeCoach = 1,
        OptionC_ProBoxWithCornerAudit = 2
    }

    public class OverlaySettings
    {
        public bool StandingsEnabled = true;
        public bool DeltaBarEnabled = true;
        public bool Garage61Enabled = true;
        public bool StandingsAutoOpen = true;
        public bool DeltaBarAutoOpen = true;
        public bool Garage61AutoOpen = true;
        public bool Locked = false;
        public TelemetryMode Mode = TelemetryMode.Auto;

        // Standings Settings
        public bool ShowCarNumber = true;
        public bool ShowInterval = false;
        public bool CompactIR = false;
        public bool ShowDeltaIR = true;
        public float StandingsFontScale = 1.0f;
        public float StandingsOpacity = 0.95f;
        public int StandingsX = -1;
        public int StandingsY = -1;
        public int StandingsW = 0;
        public int StandingsH = 0;

        // Delta Bar (Option C) Settings
        public DeltaReferenceMode DeltaRefMode = DeltaReferenceMode.SessionBest;
        public float DeltaBarRangeSec = 2.0f;
        public bool ShowSectorSplits = true;
        public float DeltaFontScale = 1.0f;
        public float DeltaOpacity = 0.95f;
        public int DeltaX = -1;
        public int DeltaY = -1;
        public int DeltaW = 0;
        public int DeltaH = 0;

        // Garage61 Telemetry & Braking Coach Settings (Options A / B / C)
        public Garage61LayoutStyle Garage61Style = Garage61LayoutStyle.OptionA_LookaheadTraceAndPedals;
        public bool Garage61AudioEnabled = true;
        public int Garage61LookaheadMeters = 120;
        public float Garage61FontScale = 1.0f;
        public float Garage61Opacity = 0.95f;
        public int Garage61X = -1;
        public int Garage61Y = -1;
        public int Garage61W = 0;
        public int Garage61H = 0;
        public string Garage61CsvPath = "";

        public event EventHandler Changed;

        public void NotifyChanged()
        {
            Save();
            if (Changed != null)
                Changed(this, EventArgs.Empty);
        }

        private static string GetConfigPath()
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FreeOverlay"
            );
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return Path.Combine(dir, "settings.cfg");
        }

        public void Save()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("StandingsEnabled=" + StandingsEnabled);
                sb.AppendLine("DeltaBarEnabled=" + DeltaBarEnabled);
                sb.AppendLine("Garage61Enabled=" + Garage61Enabled);
                sb.AppendLine("StandingsAutoOpen=" + StandingsAutoOpen);
                sb.AppendLine("DeltaBarAutoOpen=" + DeltaBarAutoOpen);
                sb.AppendLine("Garage61AutoOpen=" + Garage61AutoOpen);
                sb.AppendLine("Mode=" + (int)Mode);
                sb.AppendLine("ShowCarNumber=" + ShowCarNumber);
                sb.AppendLine("ShowInterval=" + ShowInterval);
                sb.AppendLine("CompactIR=" + CompactIR);
                sb.AppendLine("ShowDeltaIR=" + ShowDeltaIR);
                sb.AppendLine("StandingsFontScale=" + StandingsFontScale.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("StandingsOpacity=" + StandingsOpacity.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("StandingsX=" + StandingsX);
                sb.AppendLine("StandingsY=" + StandingsY);
                sb.AppendLine("StandingsW=" + StandingsW);
                sb.AppendLine("StandingsH=" + StandingsH);
                sb.AppendLine("DeltaRefMode=" + (int)DeltaRefMode);
                sb.AppendLine("DeltaBarRangeSec=" + DeltaBarRangeSec.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("ShowSectorSplits=" + ShowSectorSplits);
                sb.AppendLine("DeltaFontScale=" + DeltaFontScale.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("DeltaOpacity=" + DeltaOpacity.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("DeltaX=" + DeltaX);
                sb.AppendLine("DeltaY=" + DeltaY);
                sb.AppendLine("DeltaW=" + DeltaW);
                sb.AppendLine("DeltaH=" + DeltaH);
                sb.AppendLine("Garage61Style=" + (int)Garage61Style);
                sb.AppendLine("Garage61AudioEnabled=" + Garage61AudioEnabled);
                sb.AppendLine("Garage61LookaheadMeters=" + Garage61LookaheadMeters);
                sb.AppendLine("Garage61FontScale=" + Garage61FontScale.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("Garage61Opacity=" + Garage61Opacity.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("Garage61X=" + Garage61X);
                sb.AppendLine("Garage61Y=" + Garage61Y);
                sb.AppendLine("Garage61W=" + Garage61W);
                sb.AppendLine("Garage61H=" + Garage61H);
                sb.AppendLine("Garage61CsvPath=" + (Garage61CsvPath ?? ""));
                File.WriteAllText(GetConfigPath(), sb.ToString(), Encoding.UTF8);
            }
            catch
            {
            }
        }

        public void Load()
        {
            try
            {
                string path = GetConfigPath();
                if (!File.Exists(path)) return;
                bool hasStandingsAuto = false;
                bool hasDeltaAuto = false;
                bool hasG61Auto = false;
                string[] lines = File.ReadAllLines(path, Encoding.UTF8);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    string k = line.Substring(0, eq).Trim();
                    string v = line.Substring(eq + 1).Trim();
                    int iv;
                    float fv;
                    bool bv;
                    if (k == "StandingsEnabled" && bool.TryParse(v, out bv)) StandingsEnabled = bv;
                    else if (k == "DeltaBarEnabled" && bool.TryParse(v, out bv)) DeltaBarEnabled = bv;
                    else if (k == "Garage61Enabled" && bool.TryParse(v, out bv)) Garage61Enabled = bv;
                    else if (k == "StandingsAutoOpen" && bool.TryParse(v, out bv)) { StandingsAutoOpen = bv; hasStandingsAuto = true; }
                    else if (k == "DeltaBarAutoOpen" && bool.TryParse(v, out bv)) { DeltaBarAutoOpen = bv; hasDeltaAuto = true; }
                    else if (k == "Garage61AutoOpen" && bool.TryParse(v, out bv)) { Garage61AutoOpen = bv; hasG61Auto = true; }
                    else if (k == "Mode" && int.TryParse(v, out iv)) Mode = (TelemetryMode)(iv % 3);
                    else if (k == "ShowCarNumber" && bool.TryParse(v, out bv)) ShowCarNumber = bv;
                    else if (k == "ShowInterval" && bool.TryParse(v, out bv)) ShowInterval = bv;
                    else if (k == "CompactIR" && bool.TryParse(v, out bv)) CompactIR = bv;
                    else if (k == "ShowDeltaIR" && bool.TryParse(v, out bv)) ShowDeltaIR = bv;
                    else if (k == "StandingsFontScale" && float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out fv)) StandingsFontScale = Math.Max(0.65f, Math.Min(1.75f, fv));
                    else if (k == "StandingsOpacity" && float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out fv)) StandingsOpacity = Math.Max(0.45f, Math.Min(1.0f, fv));
                    else if (k == "StandingsX" && int.TryParse(v, out iv)) StandingsX = iv;
                    else if (k == "StandingsY" && int.TryParse(v, out iv)) StandingsY = iv;
                    else if (k == "StandingsW" && int.TryParse(v, out iv)) StandingsW = iv;
                    else if (k == "StandingsH" && int.TryParse(v, out iv)) StandingsH = iv;
                    else if (k == "DeltaRefMode" && int.TryParse(v, out iv)) DeltaRefMode = (DeltaReferenceMode)(iv % 4);
                    else if (k == "DeltaBarRangeSec" && float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out fv)) DeltaBarRangeSec = Math.Max(0.5f, Math.Min(10.0f, fv));
                    else if (k == "ShowSectorSplits" && bool.TryParse(v, out bv)) ShowSectorSplits = bv;
                    else if (k == "DeltaFontScale" && float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out fv)) DeltaFontScale = Math.Max(0.65f, Math.Min(1.75f, fv));
                    else if (k == "DeltaOpacity" && float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out fv)) DeltaOpacity = Math.Max(0.45f, Math.Min(1.0f, fv));
                    else if (k == "DeltaX" && int.TryParse(v, out iv)) DeltaX = iv;
                    else if (k == "DeltaY" && int.TryParse(v, out iv)) DeltaY = iv;
                    else if (k == "DeltaW" && int.TryParse(v, out iv)) DeltaW = iv;
                    else if (k == "DeltaH" && int.TryParse(v, out iv)) DeltaH = iv;
                    else if (k == "Garage61Style" && int.TryParse(v, out iv)) Garage61Style = (Garage61LayoutStyle)(iv % 3);
                    else if (k == "Garage61AudioEnabled" && bool.TryParse(v, out bv)) Garage61AudioEnabled = bv;
                    else if (k == "Garage61LookaheadMeters" && int.TryParse(v, out iv)) Garage61LookaheadMeters = Math.Max(40, Math.Min(350, iv));
                    else if (k == "Garage61FontScale" && float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out fv)) Garage61FontScale = Math.Max(0.65f, Math.Min(1.75f, fv));
                    else if (k == "Garage61Opacity" && float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out fv)) Garage61Opacity = Math.Max(0.45f, Math.Min(1.0f, fv));
                    else if (k == "Garage61X" && int.TryParse(v, out iv)) Garage61X = iv;
                    else if (k == "Garage61Y" && int.TryParse(v, out iv)) Garage61Y = iv;
                    else if (k == "Garage61W" && int.TryParse(v, out iv)) Garage61W = iv;
                    else if (k == "Garage61H" && int.TryParse(v, out iv)) Garage61H = iv;
                    else if (k == "Garage61CsvPath") Garage61CsvPath = v;
                }
                if (!hasStandingsAuto) StandingsAutoOpen = StandingsEnabled;
                if (!hasDeltaAuto) DeltaBarAutoOpen = DeltaBarEnabled;
                if (!hasG61Auto) Garage61AutoOpen = Garage61Enabled;
                StandingsEnabled = StandingsAutoOpen;
                DeltaBarEnabled = DeltaBarAutoOpen;
                Garage61Enabled = Garage61AutoOpen;
            }
            catch
            {
            }
        }
    }

    public class DriverRow
    {
        public int CarIdx;
        public int Position;
        public string CarNumber;
        public string UserName;
        public string ClubName;
        public string CountryCode;
        public int IRating;
        public int IRatingDelta;
        public string LicClass;
        public string SafetyRating;
        public float LastLapSec;
        public float BestLapSec;
        public LapStatus LastLapStatus;
        public float GapToLeaderSec;
        public float IntervalAheadSec;
        public int Lap;
        public float LapDistPct;
        public float BasePaceSec;
        public bool OnPitRoad;
        public bool IsPlayer;
    }

    public class DeltaTelemetryData
    {
        public float DeltaSessionBestSec;
        public float DeltaSessionBestRate;
        public float SessionBestLapSec;

        public float DeltaSessionOptimalSec;
        public float DeltaSessionOptimalRate;
        public float SessionOptimalLapSec;

        public float DeltaPersonalBestSec;
        public float DeltaPersonalBestRate;
        public float PersonalBestLapSec;

        public float DeltaLastLapSec;
        public float DeltaLastLapRate;
        public float LastLapSec;

        // Official track sectors pulled from iRacing SDK YAML (SplitTimeInfo -> Sectors -> SectorStartPct)
        public float[] SectorStartPcts = new float[] { 0.0f, 0.318452f, 0.674190f };
        public int ActiveSectorIndex = 0; // 0-based index into SectorStartPcts
        public float[] SectorDeltasSessionBest = new float[] { 0f, 0f, 0f };
        public float[] SectorDeltasOptimal = new float[] { 0f, 0f, 0f };
        public float[] SectorDeltasPersonalBest = new float[] { 0f, 0f, 0f };
        public float[] SectorDeltasLastLap = new float[] { 0f, 0f, 0f };
        public bool[] SectorDone = new bool[] { false, false, false };
        public bool OffTrack;

        public float[] GetSectorDeltas(DeltaReferenceMode mode)
        {
            if (mode == DeltaReferenceMode.SessionOptimal && SectorDeltasOptimal != null)
                return SectorDeltasOptimal;
            if (mode == DeltaReferenceMode.PersonalBest && SectorDeltasPersonalBest != null)
                return SectorDeltasPersonalBest;
            if (mode == DeltaReferenceMode.LastLap && SectorDeltasLastLap != null)
                return SectorDeltasLastLap;
            return SectorDeltasSessionBest ?? new float[] { 0f, 0f, 0f };
        }
    }

    public class SessionSnapshot
    {
        public bool Connected;
        public bool IsLiveSource;
        public string TrackName;
        public string SessionType;
        public int StrengthOfField;
        public int CurrentLap;
        public int TotalLaps;
        public double SessionTimeRemainSec;
        public float TrackTempC;
        public List<DriverRow> Drivers;
        public DeltaTelemetryData Delta;

        // Live 60Hz Player Pedal & Car Telemetry for Garage61 Braking Coach
        public float PlayerLapDistPct;
        public float TrackLengthMeters;
        public float PlayerBrakePct;
        public float PlayerThrottlePct;
        public float PlayerSpeedKph;
        public int PlayerGear;

        public SessionSnapshot()
        {
            TrackName = "Circuit de Spa-Francorchamps";
            SessionType = "RACE";
            StrengthOfField = 3451;
            CurrentLap = 8;
            TotalLaps = 20;
            SessionTimeRemainSec = 1425.0;
            TrackTempC = 32.0f;
            Drivers = new List<DriverRow>();
            Delta = new DeltaTelemetryData();
            PlayerLapDistPct = 0.0f;
            TrackLengthMeters = 7004.0f;
            PlayerBrakePct = 0.0f;
            PlayerThrottlePct = 100.0f;
            PlayerSpeedKph = 245.0f;
            PlayerGear = 5;
        }
    }

    public class BrakingZoneInfo
    {
        public int ZoneNumber;
        public string Name;
        public float StartPct;
        public float PeakPct;
        public float EndPct;
        public float PeakBrake;
        public float MinSpeedKph;
        public int MinGear;
    }

    public class CornerAuditItem
    {
        public string CornerLabel;
        public float BrakeSpotDeltaMeters;
        public float PlayerPeakBrake;
        public float RefPeakBrake;
        public bool Completed;
    }

    /// <summary>
    /// Zero-latency in-memory synthesized motorsport braking audio cue system.
    /// Plays a 3-beat preparatory countdown (1.05s -> 0.70s -> 0.35s) followed by
    /// a distinct dual-harmonic "BRAKE NOW!" cue at the exact reference braking point (0.00s).
    /// </summary>
    public static class BrakingAudioCoach
    {
        [DllImport("winmm.dll", SetLastError = true)]
        private static extern bool PlaySound(IntPtr pszSound, IntPtr hmod, uint fdwSound);

        private const uint SND_ASYNC = 0x0001;
        private const uint SND_NODEFAULT = 0x0002;
        private const uint SND_MEMORY = 0x0004;
        private const int SAMPLE_RATE = 44100;

        private static bool _initialized = false;
        private static GCHandle _pinPrep1;
        private static GCHandle _pinPrep2;
        private static GCHandle _pinPrep3;
        private static GCHandle _pinBrakeHeavy;
        private static GCHandle _pinBrakeLight;
        private static GCHandle _pinTestCadence;

        public static void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;
            try
            {
                // 3-step rising preparatory pips (G5 -> B5 -> D6)
                byte[] wavPrep1 = BuildSingleToneWav(784.0, 48, 0.48, 0.25);
                byte[] wavPrep2 = BuildSingleToneWav(988.0, 48, 0.58, 0.28);
                byte[] wavPrep3 = BuildSingleToneWav(1175.0, 52, 0.70, 0.32);

                // Exact Brake Point cues (Heavy corner double-chirp vs Medium/Light single punch)
                byte[] wavBrakeHeavy = BuildBrakeNowWav(true);
                byte[] wavBrakeLight = BuildBrakeNowWav(false);

                // Full 3-2-1-BRAKE test sequence for Hub / toggle preview
                byte[] wavTest = BuildTestSequenceWav();

                _pinPrep1 = GCHandle.Alloc(wavPrep1, GCHandleType.Pinned);
                _pinPrep2 = GCHandle.Alloc(wavPrep2, GCHandleType.Pinned);
                _pinPrep3 = GCHandle.Alloc(wavPrep3, GCHandleType.Pinned);
                _pinBrakeHeavy = GCHandle.Alloc(wavBrakeHeavy, GCHandleType.Pinned);
                _pinBrakeLight = GCHandle.Alloc(wavBrakeLight, GCHandleType.Pinned);
                _pinTestCadence = GCHandle.Alloc(wavTest, GCHandleType.Pinned);
            }
            catch
            {
            }
        }

        public static void PlayPrepBeep(int stage)
        {
            EnsureInitialized();
            try
            {
                IntPtr ptr = IntPtr.Zero;
                if (stage == 1 && _pinPrep1.IsAllocated) ptr = _pinPrep1.AddrOfPinnedObject();
                else if (stage == 2 && _pinPrep2.IsAllocated) ptr = _pinPrep2.AddrOfPinnedObject();
                else if (stage == 3 && _pinPrep3.IsAllocated) ptr = _pinPrep3.AddrOfPinnedObject();

                if (ptr != IntPtr.Zero)
                {
                    PlaySound(ptr, IntPtr.Zero, SND_MEMORY | SND_ASYNC | SND_NODEFAULT);
                }
            }
            catch
            {
            }
        }

        public static void PlayBrakeNow(bool heavyBraking)
        {
            EnsureInitialized();
            try
            {
                IntPtr ptr = heavyBraking
                    ? (_pinBrakeHeavy.IsAllocated ? _pinBrakeHeavy.AddrOfPinnedObject() : IntPtr.Zero)
                    : (_pinBrakeLight.IsAllocated ? _pinBrakeLight.AddrOfPinnedObject() : IntPtr.Zero);

                if (ptr != IntPtr.Zero)
                {
                    PlaySound(ptr, IntPtr.Zero, SND_MEMORY | SND_ASYNC | SND_NODEFAULT);
                }
            }
            catch
            {
            }
        }

        public static void PlayTestSequence()
        {
            EnsureInitialized();
            try
            {
                if (_pinTestCadence.IsAllocated)
                {
                    PlaySound(_pinTestCadence.AddrOfPinnedObject(), IntPtr.Zero, SND_MEMORY | SND_ASYNC | SND_NODEFAULT);
                }
            }
            catch
            {
            }
        }

        private static byte[] BuildSingleToneWav(double freqHz, int durationMs, double volume, double harmonicMix)
        {
            int numSamples = (SAMPLE_RATE * durationMs) / 1000;
            short[] pcm = new short[numSamples];
            SynthesizeToneInto(pcm, 0, numSamples, freqHz, volume, harmonicMix);
            return EncodePcmToWavBytes(pcm);
        }

        private static byte[] BuildBrakeNowWav(bool heavy)
        {
            if (heavy)
            {
                // Urgent F1/GT3 double-pulse at exact braking spot (52ms G6 + 10ms gap + 78ms A6/G6)
                int totalMs = 140;
                int totalSamples = (SAMPLE_RATE * totalMs) / 1000;
                short[] pcm = new short[totalSamples];

                int p1Samples = (SAMPLE_RATE * 52) / 1000;
                int p2Start = (SAMPLE_RATE * 60) / 1000;
                int p2Samples = totalSamples - p2Start;

                SynthesizeToneInto(pcm, 0, p1Samples, 1568.0, 0.88, 0.42);
                SynthesizeToneInto(pcm, p2Start, p2Samples, 1760.0, 0.94, 0.45);
                return EncodePcmToWavBytes(pcm);
            }
            else
            {
                int totalSamples = (SAMPLE_RATE * 100) / 1000;
                short[] pcm = new short[totalSamples];
                SynthesizeToneInto(pcm, 0, totalSamples, 1568.0, 0.82, 0.38);
                return EncodePcmToWavBytes(pcm);
            }
        }

        private static byte[] BuildTestSequenceWav()
        {
            // Full 3-2-1-BRAKE cadence at 350ms intervals (0ms, 350ms, 700ms, 1050ms)
            int totalMs = 1220;
            int totalSamples = (SAMPLE_RATE * totalMs) / 1000;
            short[] pcm = new short[totalSamples];

            int pipLen = (SAMPLE_RATE * 48) / 1000;
            SynthesizeToneInto(pcm, 0, pipLen, 784.0, 0.48, 0.25);
            SynthesizeToneInto(pcm, (SAMPLE_RATE * 350) / 1000, pipLen, 988.0, 0.58, 0.28);
            SynthesizeToneInto(pcm, (SAMPLE_RATE * 700) / 1000, (SAMPLE_RATE * 52) / 1000, 1175.0, 0.70, 0.32);

            int brkStart = (SAMPLE_RATE * 1050) / 1000;
            int p1Samples = (SAMPLE_RATE * 52) / 1000;
            int p2Start = brkStart + (SAMPLE_RATE * 60) / 1000;
            int p2Samples = Math.Max(0, totalSamples - p2Start);
            SynthesizeToneInto(pcm, brkStart, p1Samples, 1568.0, 0.88, 0.42);
            if (p2Samples > 0)
            {
                SynthesizeToneInto(pcm, p2Start, p2Samples, 1760.0, 0.94, 0.45);
            }

            return EncodePcmToWavBytes(pcm);
        }

        private static void SynthesizeToneInto(short[] buffer, int offset, int length, double freqHz, double volume, double harmonicMix)
        {
            int attackSamples = Math.Max(1, (SAMPLE_RATE * 4) / 1000);   // 4ms click-free cosine fade-in
            int releaseSamples = Math.Max(1, (SAMPLE_RATE * 10) / 1000); // 10ms cosine fade-out

            for (int i = 0; i < length && (offset + i) < buffer.Length; i++)
            {
                double t = (double)i / SAMPLE_RATE;
                double env = 1.0;
                if (i < attackSamples)
                {
                    env = 0.5 * (1.0 - Math.Cos(Math.PI * i / attackSamples));
                }
                else if (i > length - releaseSamples)
                {
                    int rem = length - i;
                    env = 0.5 * (1.0 - Math.Cos(Math.PI * Math.Max(0, rem) / releaseSamples));
                }

                // Slight natural exponential punch
                double punch = Math.Exp(-1.6 * ((double)i / length));

                // Fundamental + octave sub-reinforcement + 2nd harmonic for cockpit clarity
                double w = 2.0 * Math.PI * freqHz * t;
                double wave = Math.Sin(w)
                            + harmonicMix * Math.Sin(w * 2.0)
                            + (harmonicMix * 0.65) * Math.Sin(w * 0.5);
                double norm = 1.0 + harmonicMix * 1.65;

                double sampleVal = (wave / norm) * env * (0.45 + 0.55 * punch) * volume;
                if (sampleVal > 1.0) sampleVal = 1.0;
                else if (sampleVal < -1.0) sampleVal = -1.0;

                buffer[offset + i] = (short)Math.Round(sampleVal * 31000.0);
            }
        }

        private static byte[] EncodePcmToWavBytes(short[] pcm)
        {
            int dataBytes = pcm.Length * 2;
            byte[] wav = new byte[44 + dataBytes];

            // RIFF header
            wav[0] = (byte)'R'; wav[1] = (byte)'I'; wav[2] = (byte)'F'; wav[3] = (byte)'F';
            WriteInt32LE(wav, 4, 36 + dataBytes);
            wav[8] = (byte)'W'; wav[9] = (byte)'A'; wav[10] = (byte)'V'; wav[11] = (byte)'E';

            // fmt subchunk
            wav[12] = (byte)'f'; wav[13] = (byte)'m'; wav[14] = (byte)'t'; wav[15] = (byte)' ';
            WriteInt32LE(wav, 16, 16);             // Subchunk1Size (16 for PCM)
            WriteInt16LE(wav, 20, 1);              // AudioFormat (1 = PCM)
            WriteInt16LE(wav, 22, 1);              // NumChannels (1 = mono)
            WriteInt32LE(wav, 24, SAMPLE_RATE);    // SampleRate
            WriteInt32LE(wav, 28, SAMPLE_RATE * 2);// ByteRate
            WriteInt16LE(wav, 32, 2);              // BlockAlign
            WriteInt16LE(wav, 34, 16);             // BitsPerSample

            // data subchunk
            wav[36] = (byte)'d'; wav[37] = (byte)'a'; wav[38] = (byte)'t'; wav[39] = (byte)'a';
            WriteInt32LE(wav, 40, dataBytes);

            Buffer.BlockCopy(pcm, 0, wav, 44, dataBytes);
            return wav;
        }

        private static void WriteInt32LE(byte[] buf, int offset, int value)
        {
            buf[offset] = (byte)(value & 0xFF);
            buf[offset + 1] = (byte)((value >> 8) & 0xFF);
            buf[offset + 2] = (byte)((value >> 16) & 0xFF);
            buf[offset + 3] = (byte)((value >> 24) & 0xFF);
        }

        private static void WriteInt16LE(byte[] buf, int offset, short value)
        {
            buf[offset] = (byte)(value & 0xFF);
            buf[offset + 1] = (byte)((value >> 8) & 0xFF);
        }
    }

    public class Garage61LapManager
    {
        public const int BUCKETS = 2000;

        public string DriverName = "M. Van Der Berg";
        public float RefLapTimeSec = 136.412f;
        public string SourceLabel = "Built-in Spa GT3 Pro Lap";

        public readonly float[] RefBrake = new float[BUCKETS];
        public readonly float[] RefThrottle = new float[BUCKETS];
        public readonly float[] RefSpeedKph = new float[BUCKETS];
        public readonly int[] RefGear = new int[BUCKETS];

        public readonly float[] PlayerBrakeTrace = new float[BUCKETS];
        public readonly float[] PlayerThrottleTrace = new float[BUCKETS];

        public readonly List<BrakingZoneInfo> Zones = new List<BrakingZoneInfo>();
        public readonly List<CornerAuditItem> Audits = new List<CornerAuditItem>();

        private int _lastBucket = -1;
        private float _lastPlayerBrake = 0f;
        private float _lastPlayerThrottle = 0f;
        private bool _playerBrakingInZone = false;
        private int _activeZoneIdx = -1;
        public float CurrentBrakeSpotDeltaMeters = 0f;
        public bool HasCurrentBrakeSpotDelta = false;
        public float CurrentZonePlayerPeakBrake = 0f;

        // Braking Audio Coach state (0 = idle, 1 = Prep1, 2 = Prep2, 3 = Prep3, 4 = BRAKE NOW!)
        private int _audioZoneNumber = -1;
        private int _audioStageFired = 0;
        public int CurrentAudioStage = 0;
        public float CurrentTimeToBrakeSec = 99f;

        public Garage61LapManager()
        {
            BuildDefaultSpaReferenceLap();
        }

        public void BuildDefaultSpaReferenceLap()
        {
            DriverName = "M. Van Der Berg";
            RefLapTimeSec = 136.412f;
            SourceLabel = "G61: M. Van Der Berg (2:16.412)";
            Zones.Clear();
            Audits.Clear();

            for (int i = 0; i < BUCKETS; i++)
            {
                RefBrake[i] = 0f;
                RefThrottle[i] = 100f;
                RefSpeedKph[i] = 252f;
                RefGear[i] = 5;
            }

            // 7 Authentic Spa-Francorchamps GT3 Braking Zones
            AddSyntheticZone(1, "T1 La Source", 0.045f, 0.056f, 0.092f, 89f, 68f, 1);
            AddSyntheticZone(2, "T5 Les Combes", 0.312f, 0.324f, 0.368f, 84f, 118f, 3);
            AddSyntheticZone(3, "T8 Bruxelles", 0.428f, 0.440f, 0.486f, 76f, 96f, 2);
            AddSyntheticZone(4, "T10 Pouhon", 0.562f, 0.569f, 0.596f, 48f, 182f, 4);
            AddSyntheticZone(5, "T12 Fagnes", 0.668f, 0.678f, 0.715f, 68f, 132f, 3);
            AddSyntheticZone(6, "T14 Stavelot", 0.752f, 0.760f, 0.788f, 54f, 156f, 3);
            AddSyntheticZone(7, "T18 Bus Stop", 0.912f, 0.926f, 0.968f, 92f, 64f, 1);

            for (int z = 0; z < Zones.Count; z++)
            {
                BrakingZoneInfo bz = Zones[z];
                CornerAuditItem ca = new CornerAuditItem();
                ca.CornerLabel = bz.Name;
                ca.BrakeSpotDeltaMeters = (z % 2 == 0) ? -1.4f : 3.2f;
                ca.PlayerPeakBrake = bz.PeakBrake - 4f;
                ca.RefPeakBrake = bz.PeakBrake;
                ca.Completed = true;
                Audits.Add(ca);
            }
        }

        private void AddSyntheticZone(int num, string name, float startPct, float peakPct, float endPct, float peakBrk, float minSpd, int minGear)
        {
            BrakingZoneInfo z = new BrakingZoneInfo();
            z.ZoneNumber = num;
            z.Name = name;
            z.StartPct = startPct;
            z.PeakPct = peakPct;
            z.EndPct = endPct;
            z.PeakBrake = peakBrk;
            z.MinSpeedKph = minSpd;
            z.MinGear = minGear;
            Zones.Add(z);

            int sIdx = Math.Max(0, (int)Math.Round(startPct * BUCKETS));
            int pIdx = Math.Max(sIdx + 1, (int)Math.Round(peakPct * BUCKETS));
            int eIdx = Math.Min(BUCKETS - 1, (int)Math.Round(endPct * BUCKETS));
            int throtEndIdx = Math.Min(BUCKETS - 1, eIdx + 18);

            for (int i = sIdx; i <= throtEndIdx; i++)
            {
                float brk = 0f;
                if (i <= pIdx)
                {
                    float t = (float)(i - sIdx) / Math.Max(1, pIdx - sIdx);
                    brk = peakBrk * (float)Math.Pow(t, 0.65);
                }
                else if (i <= eIdx)
                {
                    float t = (float)(i - pIdx) / Math.Max(1, eIdx - pIdx);
                    brk = peakBrk * (float)Math.Pow(1.0f - t, 1.35);
                }

                float thr = 0f;
                if (i > eIdx - 4)
                {
                    float t = (float)(i - (eIdx - 4)) / Math.Max(1, throtEndIdx - (eIdx - 4));
                    thr = Math.Max(0f, Math.Min(100f, t * 100f));
                }

                RefBrake[i] = Math.Max(0f, Math.Min(100f, brk));
                RefThrottle[i] = thr;
                RefSpeedKph[i] = minSpd + (1.0f - (RefBrake[i] / 100f)) * (250f - minSpd);
                RefGear[i] = i >= pIdx ? minGear : Math.Min(6, minGear + 2);
            }
        }

        public bool LoadFromCsvFile(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return false;
                string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);
                if (lines.Length < 10) return false;

                int headerRow = -1;
                int colDist = -1, colBrake = -1, colThrottle = -1, colSpeed = -1, colGear = -1;

                for (int r = 0; r < Math.Min(25, lines.Length); r++)
                {
                    string[] cols = lines[r].Split(',');
                    if (cols.Length < 3) continue;
                    for (int c = 0; c < cols.Length; c++)
                    {
                        string cn = cols[c].Trim().Trim('"').ToLowerInvariant();
                        if (cn == "lapdistpct" || cn == "lap_dist_pct" || cn == "distpct") colDist = c;
                        else if (cn == "brake" || cn == "brakepct" || cn == "brake_pct") colBrake = c;
                        else if (cn == "throttle" || cn == "throttlepct" || cn == "throttle_pct") colThrottle = c;
                        else if (cn == "speed" || cn == "speedkph" || cn == "velocity") colSpeed = c;
                        else if (cn == "gear") colGear = c;
                    }
                    if (colDist >= 0 && colBrake >= 0)
                    {
                        headerRow = r;
                        break;
                    }
                }

                if (headerRow < 0) return false;

                float[] sumBrake = new float[BUCKETS];
                float[] sumThrot = new float[BUCKETS];
                float[] sumSpd = new float[BUCKETS];
                int[] counts = new int[BUCKETS];
                float maxBrakeSeen = 0f;
                float maxThrotSeen = 0f;

                for (int r = headerRow + 1; r < lines.Length; r++)
                {
                    string[] cols = lines[r].Split(',');
                    if (cols.Length <= Math.Max(colDist, colBrake)) continue;

                    float dist, brk;
                    if (!float.TryParse(cols[colDist].Trim().Trim('"'), NumberStyles.Float, CultureInfo.InvariantCulture, out dist)) continue;
                    if (!float.TryParse(cols[colBrake].Trim().Trim('"'), NumberStyles.Float, CultureInfo.InvariantCulture, out brk)) continue;

                    if (dist > 1.0f && dist <= 100.0f) dist /= 100.0f;
                    if (dist < 0f || dist >= 1.0f) continue;

                    float thr = 0f;
                    if (colThrottle >= 0 && colThrottle < cols.Length)
                        float.TryParse(cols[colThrottle].Trim().Trim('"'), NumberStyles.Float, CultureInfo.InvariantCulture, out thr);

                    float spd = 160f;
                    if (colSpeed >= 0 && colSpeed < cols.Length)
                        float.TryParse(cols[colSpeed].Trim().Trim('"'), NumberStyles.Float, CultureInfo.InvariantCulture, out spd);

                    int gVal = 3;
                    if (colGear >= 0 && colGear < cols.Length)
                        int.TryParse(cols[colGear].Trim().Trim('"'), out gVal);

                    if (brk > maxBrakeSeen) maxBrakeSeen = brk;
                    if (thr > maxThrotSeen) maxThrotSeen = thr;

                    int b = Math.Max(0, Math.Min(BUCKETS - 1, (int)(dist * BUCKETS)));
                    sumBrake[b] += brk;
                    sumThrot[b] += thr;
                    sumSpd[b] += spd;
                    RefGear[b] = gVal;
                    counts[b]++;
                }

                float brkScale = (maxBrakeSeen <= 1.05f) ? 100.0f : 1.0f;
                float thrScale = (maxThrotSeen <= 1.05f) ? 100.0f : 1.0f;

                float lastB = 0f, lastT = 100f, lastS = 180f;
                for (int i = 0; i < BUCKETS; i++)
                {
                    if (counts[i] > 0)
                    {
                        lastB = Math.Max(0f, Math.Min(100f, (sumBrake[i] / counts[i]) * brkScale));
                        lastT = Math.Max(0f, Math.Min(100f, (sumThrot[i] / counts[i]) * thrScale));
                        lastS = sumSpd[i] / counts[i];
                        if (lastS < 90f) lastS *= 3.6f; // m/s to kph if needed
                    }
                    RefBrake[i] = lastB;
                    RefThrottle[i] = lastT;
                    RefSpeedKph[i] = lastS;
                }

                RebuildZonesFromRefTrace();
                DriverName = Path.GetFileNameWithoutExtension(filePath);
                SourceLabel = "G61 CSV: " + DriverName;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void RebuildZonesFromRefTrace()
        {
            Zones.Clear();
            Audits.Clear();
            bool inZone = false;
            int startIdx = 0;
            int peakIdx = 0;
            float peakBrk = 0f;
            float minSpd = 999f;
            int minG = 6;

            for (int i = 0; i < BUCKETS; i++)
            {
                float b = RefBrake[i];
                if (!inZone && b >= 8.0f)
                {
                    inZone = true;
                    startIdx = i;
                    peakIdx = i;
                    peakBrk = b;
                    minSpd = RefSpeedKph[i];
                    minG = RefGear[i];
                }
                else if (inZone)
                {
                    if (b > peakBrk)
                    {
                        peakBrk = b;
                        peakIdx = i;
                    }
                    if (RefSpeedKph[i] < minSpd) minSpd = RefSpeedKph[i];
                    if (RefGear[i] > 0 && RefGear[i] < minG) minG = RefGear[i];

                    if (b < 3.0f || i == BUCKETS - 1)
                    {
                        inZone = false;
                        if (i - startIdx >= 6 && peakBrk >= 15.0f)
                        {
                            BrakingZoneInfo z = new BrakingZoneInfo();
                            z.ZoneNumber = Zones.Count + 1;
                            z.Name = "Corner " + z.ZoneNumber;
                            z.StartPct = (float)startIdx / BUCKETS;
                            z.PeakPct = (float)peakIdx / BUCKETS;
                            z.EndPct = (float)i / BUCKETS;
                            z.PeakBrake = peakBrk;
                            z.MinSpeedKph = minSpd;
                            z.MinGear = minG;
                            Zones.Add(z);

                            CornerAuditItem ca = new CornerAuditItem();
                            ca.CornerLabel = z.Name;
                            ca.BrakeSpotDeltaMeters = 0f;
                            ca.PlayerPeakBrake = peakBrk;
                            ca.RefPeakBrake = peakBrk;
                            ca.Completed = false;
                            Audits.Add(ca);
                        }
                    }
                }
            }
        }

        private static float SampleCircularLerp(float[] arr, float distPct)
        {
            float wrapped = distPct - (float)Math.Floor(distPct);
            float exactPos = wrapped * BUCKETS;
            int idx0 = (int)exactPos;
            if (idx0 < 0) idx0 = 0;
            else if (idx0 >= BUCKETS) idx0 = BUCKETS - 1;
            int idx1 = (idx0 + 1) % BUCKETS;
            float frac = exactPos - idx0;
            return arr[idx0] + (arr[idx1] - arr[idx0]) * frac;
        }

        public float GetRefBrakeAtPct(float distPct)
        {
            return SampleCircularLerp(RefBrake, distPct);
        }

        public float GetRefThrottleAtPct(float distPct)
        {
            return SampleCircularLerp(RefThrottle, distPct);
        }

        public float GetRefSpeedAtPct(float distPct)
        {
            return SampleCircularLerp(RefSpeedKph, distPct);
        }

        public int GetRefGearAtPct(float distPct)
        {
            float wrapped = distPct - (float)Math.Floor(distPct);
            int idx = Math.Max(0, Math.Min(BUCKETS - 1, (int)(wrapped * BUCKETS)));
            return RefGear[idx];
        }

        public float GetPlayerBrakeAtPct(float distPct)
        {
            return SampleCircularLerp(PlayerBrakeTrace, distPct);
        }

        public float GetPlayerThrottleAtPct(float distPct)
        {
            return SampleCircularLerp(PlayerThrottleTrace, distPct);
        }

        public void UpdatePlayerTelemetry(float distPct, float playerBrake, float playerThrottle, float trackLengthMeters)
        {
            UpdatePlayerTelemetry(distPct, playerBrake, playerThrottle, trackLengthMeters, 200f, false);
        }

        public void UpdatePlayerTelemetry(float distPct, float playerBrake, float playerThrottle, float trackLengthMeters, float playerSpeedKph, bool playAudioCues)
        {
            float wrapped = distPct - (float)Math.Floor(distPct);
            int b = Math.Max(0, Math.Min(BUCKETS - 1, (int)(wrapped * BUCKETS)));

            if (_lastBucket >= 0 && b != _lastBucket)
            {
                int fwdDiff = (b - _lastBucket + BUCKETS) % BUCKETS;
                if (fwdDiff >= 1 && fwdDiff <= 60)
                {
                    for (int s = 1; s <= fwdDiff; s++)
                    {
                        int step = (_lastBucket + s) % BUCKETS;
                        float t = (float)s / fwdDiff;
                        PlayerBrakeTrace[step] = _lastPlayerBrake + (playerBrake - _lastPlayerBrake) * t;
                        PlayerThrottleTrace[step] = _lastPlayerThrottle + (playerThrottle - _lastPlayerThrottle) * t;
                    }
                }
                else
                {
                    PlayerBrakeTrace[b] = playerBrake;
                    PlayerThrottleTrace[b] = playerThrottle;
                }
            }
            else
            {
                PlayerBrakeTrace[b] = playerBrake;
                PlayerThrottleTrace[b] = playerThrottle;
            }
            _lastBucket = b;
            _lastPlayerBrake = playerBrake;
            _lastPlayerThrottle = playerThrottle;

            float safeTrackLen = Math.Max(1000f, trackLengthMeters);

            // Evaluate approaching braking zone for Audio & Visual Countdown Cues
            float distToStartMeters;
            BrakingZoneInfo relZone = GetRelevantZone(distPct, out distToStartMeters, safeTrackLen);
            if (relZone != null)
            {
                float speedMps = Math.Max(18.0f, playerSpeedKph / 3.6f);
                float timeToStartSec = distToStartMeters / speedMps;
                CurrentTimeToBrakeSec = timeToStartSec;

                if (relZone.ZoneNumber != _audioZoneNumber)
                {
                    _audioZoneNumber = relZone.ZoneNumber;
                    _audioStageFired = 0;
                }
                else if (distToStartMeters > 150.0f && _audioStageFired >= 4)
                {
                    _audioStageFired = 0;
                }

                // Visual countdown stage (1 = 1.05s, 2 = 0.70s, 3 = 0.35s, 4 = BRAKE NOW!)
                if (distToStartMeters <= 3.0f && distToStartMeters >= -45.0f)
                {
                    CurrentAudioStage = 4;
                }
                else if (distToStartMeters > 3.0f && timeToStartSec <= 0.36f)
                {
                    CurrentAudioStage = 3;
                }
                else if (distToStartMeters > 3.0f && timeToStartSec <= 0.72f)
                {
                    CurrentAudioStage = 2;
                }
                else if (distToStartMeters > 3.0f && timeToStartSec <= 1.08f)
                {
                    CurrentAudioStage = 1;
                }
                else
                {
                    CurrentAudioStage = 0;
                }

                // Fire rhythmic 3-2-1-BRAKE audio cues (350ms beat spacing leading into exact brake spot)
                if (playAudioCues && playerSpeedKph >= 35.0f)
                {
                    if ((distToStartMeters <= 2.2f || timeToStartSec <= 0.035f) && distToStartMeters >= -16.0f)
                    {
                        if (_audioStageFired < 4)
                        {
                            _audioStageFired = 4;
                            BrakingAudioCoach.PlayBrakeNow(relZone.PeakBrake >= 55.0f);
                        }
                    }
                    else if (distToStartMeters > 2.2f && timeToStartSec <= 0.36f && timeToStartSec > 0.07f)
                    {
                        if (_audioStageFired < 3)
                        {
                            _audioStageFired = 3;
                            BrakingAudioCoach.PlayPrepBeep(3);
                        }
                    }
                    else if (distToStartMeters > 2.2f && timeToStartSec <= 0.72f && timeToStartSec > 0.40f)
                    {
                        if (_audioStageFired < 2)
                        {
                            _audioStageFired = 2;
                            BrakingAudioCoach.PlayPrepBeep(2);
                        }
                    }
                    else if (distToStartMeters > 2.2f && timeToStartSec <= 1.08f && timeToStartSec > 0.75f)
                    {
                        if (_audioStageFired < 1)
                        {
                            _audioStageFired = 1;
                            BrakingAudioCoach.PlayPrepBeep(1);
                        }
                    }
                }
            }
            else
            {
                CurrentAudioStage = 0;
                CurrentTimeToBrakeSec = 99f;
            }

            // Find current or approaching braking zone
            int foundZone = -1;
            for (int i = 0; i < Zones.Count; i++)
            {
                BrakingZoneInfo z = Zones[i];
                if (wrapped >= z.StartPct - 0.025f && wrapped <= z.EndPct + 0.010f)
                {
                    foundZone = i;
                    break;
                }
            }

            if (foundZone >= 0)
            {
                BrakingZoneInfo z = Zones[foundZone];
                if (_activeZoneIdx != foundZone)
                {
                    _activeZoneIdx = foundZone;
                    _playerBrakingInZone = false;
                    CurrentZonePlayerPeakBrake = 0f;
                }

                if (playerBrake > CurrentZonePlayerPeakBrake)
                    CurrentZonePlayerPeakBrake = playerBrake;

                if (!_playerBrakingInZone && playerBrake >= 6.0f)
                {
                    _playerBrakingInZone = true;
                    CurrentBrakeSpotDeltaMeters = (wrapped - z.StartPct) * safeTrackLen;
                    HasCurrentBrakeSpotDelta = true;
                }

                if (_playerBrakingInZone && foundZone < Audits.Count)
                {
                    Audits[foundZone].BrakeSpotDeltaMeters = CurrentBrakeSpotDeltaMeters;
                    Audits[foundZone].PlayerPeakBrake = Math.Max(Audits[foundZone].PlayerPeakBrake, CurrentZonePlayerPeakBrake);
                    Audits[foundZone].RefPeakBrake = z.PeakBrake;
                    Audits[foundZone].Completed = true;
                }
            }
            else
            {
                _playerBrakingInZone = false;
            }
        }

        public BrakingZoneInfo GetRelevantZone(float distPct, out float distToStartMeters, float trackLenMeters)
        {
            distToStartMeters = 0f;
            if (Zones.Count == 0) return null;
            float wrapped = distPct - (float)Math.Floor(distPct);

            for (int i = 0; i < Zones.Count; i++)
            {
                BrakingZoneInfo z = Zones[i];
                if (wrapped >= z.StartPct && wrapped <= z.EndPct + 0.008f)
                {
                    distToStartMeters = (z.StartPct - wrapped) * trackLenMeters; // negative = inside zone
                    return z;
                }
                if (z.StartPct > wrapped)
                {
                    distToStartMeters = (z.StartPct - wrapped) * trackLenMeters;
                    return z;
                }
            }

            // Wrap around to Zone 1 on next lap
            BrakingZoneInfo first = Zones[0];
            distToStartMeters = ((1.0f - wrapped) + first.StartPct) * trackLenMeters;
            return first;
        }
    }

    public static class DpiHelper
    {
        [DllImport("user32.dll")]
        private static extern bool SetProcessDpiAwarenessContext(IntPtr dpiContext);

        [DllImport("shcore.dll")]
        private static extern int SetProcessDpiAwareness(int awareness);

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(IntPtr hwnd);

        public static void EnablePerMonitorHighDpi()
        {
            try
            {
                // DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = -4
                if (SetProcessDpiAwarenessContext(new IntPtr(-4)))
                    return;
            }
            catch
            {
            }

            try
            {
                // PROCESS_PER_MONITOR_DPI_AWARE = 2
                if (SetProcessDpiAwareness(2) == 0)
                    return;
            }
            catch
            {
            }

            try
            {
                SetProcessDPIAware();
            }
            catch
            {
            }
        }

        public static float GetScale(IntPtr hwnd)
        {
            try
            {
                if (hwnd != IntPtr.Zero)
                {
                    uint dpi = GetDpiForWindow(hwnd);
                    if (dpi >= 72 && dpi <= 480)
                        return dpi / 96.0f;
                }
            }
            catch
            {
            }

            try
            {
                using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
                {
                    if (g.DpiX >= 72f)
                        return g.DpiX / 96.0f;
                }
            }
            catch
            {
            }
            return 1.0f;
        }

        public static string FormatLapTime(float seconds)
        {
            if (seconds <= 0f || float.IsNaN(seconds) || float.IsInfinity(seconds))
                return "-:--.---";
            int mins = (int)(seconds / 60f);
            float rem = seconds - mins * 60f;
            int whole = (int)rem;
            int ms = Math.Min(999, (int)Math.Round((rem - whole) * 1000f));
            return string.Format(CultureInfo.InvariantCulture, "{0}:{1:00}.{2:000}", mins, whole, ms);
        }

        public static void DrawCornerModeIconBtn(Graphics g, Font iconFont, Rectangle rect, bool isResize, float s)
        {
            Color bg = isResize ? Color.FromArgb(225, 2, 132, 199) : Color.FromArgb(225, 147, 51, 234);
            Color border = isResize ? Color.FromArgb(125, 211, 252) : Color.FromArgb(216, 180, 254);

            using (SolidBrush b = new SolidBrush(bg))
            {
                g.FillRectangle(b, rect);
            }
            using (Pen bp = new Pen(border, Math.Max(1.2f, 1.2f * s)))
            {
                g.DrawRectangle(bp, rect);
            }

            if (isResize)
            {
                float pad = Math.Max(4f, 5.0f * s);
                float x1 = rect.Left + pad;
                float y1 = rect.Bottom - pad;
                float x2 = rect.Right - pad;
                float y2 = rect.Top + pad;
                float head = Math.Max(3.5f, 4.5f * s);

                using (Pen arrowPen = new Pen(Color.White, Math.Max(1.6f, 1.8f * s)))
                {
                    arrowPen.StartCap = LineCap.Round;
                    arrowPen.EndCap = LineCap.Round;
                    g.DrawLine(arrowPen, x1, y1, x2, y2);
                    g.DrawLine(arrowPen, x2 - head, y2, x2, y2);
                    g.DrawLine(arrowPen, x2, y2, x2, y2 + head);
                    g.DrawLine(arrowPen, x1, y1 - head, x1, y1);
                    g.DrawLine(arrowPen, x1, y1, x1 + head, y1);
                }
            }
            else
            {
                StringFormat sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString("Aa", iconFont, Brushes.White, rect, sf);
            }
        }

        public static void DrawCornerGrips(Graphics g, int width, int height, Color gripColor, float s)
        {
            int len = Math.Max(12, (int)Math.Round(14 * s));
            float thick = Math.Max(2.5f, 2.8f * s);
            int m = Math.Max(2, (int)Math.Round(2 * s));

            using (Pen p = new Pen(gripColor, thick))
            {
                g.DrawLine(p, m, m, m + len, m);
                g.DrawLine(p, m, m, m, m + len);

                g.DrawLine(p, width - m - 1 - len, m, width - m - 1, m);
                g.DrawLine(p, width - m - 1, m, width - m - 1, m + len);

                g.DrawLine(p, m, height - m - 1 - len, m, height - m - 1);
                g.DrawLine(p, m, height - m - 1, m + len, height - m - 1);

                g.DrawLine(p, width - m - 1 - len, height - m - 1, width - m - 1, height - m - 1);
                g.DrawLine(p, width - m - 1, height - m - 1 - len, width - m - 1, height - m - 1);
            }
        }
    }

    public static class CountryResolver
    {
        private static readonly Dictionary<string, string> ClubMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "california", "US" }, { "texas", "US" }, { "florida", "US" }, { "new england", "US" },
            { "new york", "US" }, { "midwest", "US" }, { "mid-south", "US" }, { "atlantic", "US" },
            { "virginias", "US" }, { "carolinas", "US" }, { "georgia", "US" }, { "ohio", "US" },
            { "michigan", "US" }, { "illinois", "US" }, { "west", "US" }, { "northwest", "US" },
            { "plains", "US" }, { "usa", "US" }, { "united states", "US" },
            { "canada", "CA" }, { "mexico", "MX" },
            { "benelux", "NL" }, { "netherlands", "NL" }, { "belgium", "BE" },
            { "uk and i", "GB" }, { "united kingdom", "GB" }, { "ireland", "IE" },
            { "de-at-ch", "DE" }, { "germany", "DE" }, { "austria", "AT" }, { "switzerland", "CH" },
            { "france", "FR" }, { "iberia", "ES" }, { "spain", "ES" }, { "portugal", "PT" },
            { "italy", "IT" }, { "finland", "FI" }, { "scandinavia", "SE" }, { "sweden", "SE" },
            { "norway", "NO" }, { "denmark", "DK" }, { "central-eastern europe", "PL" }, { "poland", "PL" },
            { "brazil", "BR" }, { "hispanoamerica", "AR" }, { "argentina", "AR" },
            { "australia/nz", "AU" }, { "australia", "AU" }, { "new zealand", "NZ" },
            { "japan", "JP" }
        };

        public static string Resolve(string clubName, string fallbackCode)
        {
            if (!string.IsNullOrEmpty(fallbackCode) && fallbackCode.Length == 2)
                return fallbackCode.ToUpperInvariant();
            if (string.IsNullOrEmpty(clubName))
                return "US";
            string clean = clubName.Trim();
            string code;
            if (ClubMap.TryGetValue(clean, out code))
                return code;
            foreach (KeyValuePair<string, string> kv in ClubMap)
            {
                if (clean.IndexOf(kv.Key, StringComparison.OrdinalIgnoreCase) >= 0)
                    return kv.Value;
            }
            return "US";
        }

        public static int EstimateIRatingDelta(int pos, int fieldSize, int driverIR, int sof)
        {
            if (fieldSize <= 1 || pos <= 0) return 0;
            double expected = 1.0 / (1.0 + Math.Pow(10.0, (sof - driverIR) / 1600.0));
            double actual = (double)(fieldSize - pos) / (double)(fieldSize - 1);
            double k = 1800.0 / Math.Sqrt(fieldSize);
            return (int)Math.Round((actual - expected) * k);
        }
    }

    public class MockSimulator
    {
        private readonly List<DriverRow> _drivers = new List<DriverRow>();
        private float _sessionBestLap = 137.142f;
        private double _sessionRemainSec = 1425.0;
        private int _sof = 3451;
        private double _simPhase = 0.0;
        private readonly Random _rng = new Random(42);

        public MockSimulator()
        {
            AddSeed(1, "1", "Maxime Van Der Berg", "Benelux", "NL", 5420, "A", "4.99", 137.185f, false, false);
            AddSeed(2, "14", "Lukas Schneider", "DE-AT-CH", "DE", 4890, "A", "4.42", 137.310f, false, false);
            AddSeed(3, "23", "Callum Sterling", "UK and I", "GB", 4510, "A", "3.85", 137.440f, false, false);
            AddSeed(4, "88", "Mikko Virtanen", "Finland", "FI", 4210, "A", "3.64", 137.540f, false, false);
            AddSeed(5, "44", "Gabriel Oliveira", "Brazil", "BR", 3980, "B", "3.92", 137.650f, false, false);
            AddSeed(6, "19", "Antoine Moreau", "France", "FR", 3810, "A", "2.74", 137.740f, false, false);
            AddSeed(7, "63", "Marco Rossini", "Italy", "IT", 3650, "B", "3.45", 137.820f, false, false);
            AddSeed(8, "7", "Alex Nestorenko", "California", "US", 3525, "A", "4.18", 137.895f, true, false);
            AddSeed(9, "27", "Liam O'Connor", "Australia/NZ", "AU", 3380, "A", "4.05", 138.015f, false, false);
            AddSeed(10, "91", "Carlos Mendoza", "Iberia", "ES", 3195, "B", "2.58", 138.140f, false, false);
            AddSeed(11, "52", "Tyler Vance", "New England", "US", 2940, "B", "3.12", 138.310f, false, false);
            AddSeed(12, "33", "Henrik Lindqvist", "Scandinavia", "SE", 2785, "A", "2.31", 138.450f, false, false);
            AddSeed(13, "12", "Kenji Takahashi", "Japan", "JP", 2610, "B", "4.40", 138.620f, false, false);
            AddSeed(14, "77", "Connor MacLeod", "Canada", "CA", 2450, "C", "3.76", 138.790f, false, false);
            AddSeed(15, "99", "Ruben Peeters", "Benelux", "BE", 2290, "C", "2.91", 138.960f, false, true);
            AddSeed(16, "48", "Piotr Kowalski", "Central-Eastern Europe", "PL", 2110, "B", "1.88", 139.180f, false, false);
            AddSeed(17, "5", "Sebastien Chevalier", "France", "FR", 1980, "B", "2.44", 139.350f, false, false);
            AddSeed(18, "61", "Matteo Bianchi", "Italy", "IT", 1890, "C", "3.15", 139.520f, false, false);
            AddSeed(19, "81", "Noah Lindholm", "Scandinavia", "SE", 1760, "C", "2.62", 139.710f, false, false);
            AddSeed(20, "30", "Evan Richardson", "Texas", "US", 1640, "D", "3.40", 139.890f, false, false);

            int sum = 0;
            for (int i = 0; i < _drivers.Count; i++) sum += _drivers[i].IRating;
            _sof = sum / _drivers.Count;
        }

        private void AddSeed(int idx, string num, string name, string club, string cc, int ir, string lic, string sr, float pace, bool player, bool pit)
        {
            int pos = _drivers.Count + 1;
            DriverRow d = new DriverRow();
            d.CarIdx = idx;
            d.Position = pos;
            d.CarNumber = num;
            d.UserName = name;
            d.ClubName = club;
            d.CountryCode = cc;
            d.IRating = ir;
            d.LicClass = lic;
            d.SafetyRating = sr;
            d.BasePaceSec = pace;
            d.LastLapSec = pos == 1 ? _sessionBestLap : pace + (float)(Math.Sin(pos * 1.7) * 0.18);
            d.BestLapSec = pos == 1 ? _sessionBestLap : pace - 0.09f;
            d.LastLapStatus = pos == 1 ? LapStatus.SessionBest : ((player || pos == 6) ? LapStatus.PersonalBest : LapStatus.Normal);
            d.IntervalAheadSec = pos == 1 ? 0f : 1.15f + (pos % 3) * 0.32f;
            d.Lap = 8;
            d.LapDistPct = 0.88f - pos * 0.014f;
            d.OnPitRoad = pit;
            d.IsPlayer = player;
            _drivers.Add(d);
        }

        public SessionSnapshot Tick(double dtSec)
        {
            _sessionRemainSec = Math.Max(0.0, _sessionRemainSec - dtSec);
            _simPhase += dtSec * 0.9;
            float runningGap = 0f;
            DriverRow playerRow = _drivers[7];

            for (int i = 0; i < _drivers.Count; i++)
            {
                DriverRow d = _drivers[i];
                if (d.IsPlayer) playerRow = d;

                float prevDist = d.LapDistPct;
                float nextDist = (prevDist + (float)(dtSec / d.BasePaceSec)) % 1.0f;
                if (nextDist < prevDist)
                {
                    d.Lap += 1;
                    float jitter = (float)((_rng.NextDouble() - 0.48) * 0.48);
                    float lapTime = d.BasePaceSec + jitter;
                    d.LastLapSec = lapTime;
                    if (lapTime < _sessionBestLap)
                    {
                        _sessionBestLap = lapTime;
                        for (int k = 0; k < _drivers.Count; k++)
                        {
                            if (_drivers[k].LastLapStatus == LapStatus.SessionBest)
                                _drivers[k].LastLapStatus = LapStatus.PersonalBest;
                        }
                        d.BestLapSec = lapTime;
                        d.LastLapStatus = LapStatus.SessionBest;
                    }
                    else if (lapTime <= d.BestLapSec)
                    {
                        d.BestLapSec = lapTime;
                        d.LastLapStatus = LapStatus.PersonalBest;
                    }
                    else
                    {
                        d.LastLapStatus = LapStatus.Normal;
                    }
                }
                d.LapDistPct = nextDist;

                if (i == 0)
                {
                    d.IntervalAheadSec = 0f;
                    d.GapToLeaderSec = 0f;
                }
                else
                {
                    float delta = (float)((_rng.NextDouble() - 0.5) * 0.015);
                    d.IntervalAheadSec = Math.Max(0.2f, d.IntervalAheadSec + delta);
                    runningGap += d.IntervalAheadSec;
                    d.GapToLeaderSec = runningGap;
                }
                d.IRatingDelta = CountryResolver.EstimateIRatingDelta(d.Position, _drivers.Count, d.IRating, _sof);
            }

            // Simulate realistic 60Hz Delta Bar wave + Sector Splits for Player
            float wave = (float)(Math.Sin(_simPhase) * 0.68 - 0.18 + Math.Sin(_simPhase * 2.3) * 0.14);
            float rate = (float)(Math.Cos(_simPhase) * 0.62);

            DeltaTelemetryData dt = new DeltaTelemetryData();
            dt.SessionBestLapSec = playerRow.BestLapSec;
            dt.DeltaSessionBestSec = wave;
            dt.DeltaSessionBestRate = rate;

            dt.SessionOptimalLapSec = playerRow.BestLapSec - 0.24f;
            dt.DeltaSessionOptimalSec = wave + 0.21f;
            dt.DeltaSessionOptimalRate = rate;

            dt.PersonalBestLapSec = playerRow.BestLapSec - 0.08f;
            dt.DeltaPersonalBestSec = wave + 0.06f;
            dt.DeltaPersonalBestRate = rate;

            dt.LastLapSec = playerRow.LastLapSec;
            dt.DeltaLastLapSec = wave - 0.12f;
            dt.DeltaLastLapRate = rate;

            // Official Spa-Francorchamps SplitTimeInfo SectorStartPct boundaries
            dt.SectorStartPcts = new float[] { 0.000000f, 0.318452f, 0.674190f };
            float pct = playerRow.LapDistPct;
            if (pct < dt.SectorStartPcts[1])
            {
                dt.ActiveSectorIndex = 0;
                dt.SectorDeltasSessionBest = new float[] { wave, -0.14f, -0.09f };
                dt.SectorDeltasOptimal = new float[] { wave + 0.07f, -0.08f, -0.04f };
                dt.SectorDeltasPersonalBest = new float[] { wave + 0.02f, -0.11f, -0.07f };
                dt.SectorDeltasLastLap = new float[] { wave - 0.04f, -0.16f, -0.12f };
                dt.SectorDone = new bool[] { false, false, false };
            }
            else if (pct < dt.SectorStartPcts[2])
            {
                dt.ActiveSectorIndex = 1;
                dt.SectorDeltasSessionBest = new float[] { -0.18f, wave + 0.18f, -0.09f };
                dt.SectorDeltasOptimal = new float[] { -0.11f, wave + 0.25f, -0.04f };
                dt.SectorDeltasPersonalBest = new float[] { -0.15f, wave + 0.21f, -0.07f };
                dt.SectorDeltasLastLap = new float[] { -0.22f, wave + 0.14f, -0.12f };
                dt.SectorDone = new bool[] { true, false, false };
            }
            else
            {
                dt.ActiveSectorIndex = 2;
                dt.SectorDeltasSessionBest = new float[] { -0.18f, -0.11f, wave + 0.29f };
                dt.SectorDeltasOptimal = new float[] { -0.11f, -0.05f, wave + 0.37f };
                dt.SectorDeltasPersonalBest = new float[] { -0.15f, -0.08f, wave + 0.31f };
                dt.SectorDeltasLastLap = new float[] { -0.22f, -0.14f, wave + 0.24f };
                dt.SectorDone = new bool[] { true, true, false };
            }

            SessionSnapshot snap = new SessionSnapshot();
            snap.Connected = true;
            snap.IsLiveSource = false;
            snap.TrackName = "Circuit de Spa-Francorchamps";
            snap.SessionType = "RACE";
            snap.StrengthOfField = _sof;
            snap.CurrentLap = _drivers[0].Lap;
            snap.TotalLaps = 20;
            snap.SessionTimeRemainSec = _sessionRemainSec;
            snap.TrackTempC = 32.0f;
            snap.Drivers = _drivers;
            snap.Delta = dt;

            // Simulate realistic Player Brake / Throttle / Speed / Gear slightly offset from G61 reference
            snap.PlayerLapDistPct = pct;
            snap.TrackLengthMeters = 7004.0f;
            float simBrk = 0f;
            float simThr = 100f;
            float simSpd = 250f;
            int simGear = 5;

            // Match the 7 Spa braking zones with slight realistic human offset (+0.0005 pct late brake, -4% peak)
            float[,] spaZones = new float[,]
            {
                { 0.0456f, 0.0565f, 0.091f, 84f, 67f, 1f },
                { 0.3128f, 0.3248f, 0.366f, 79f, 115f, 3f },
                { 0.4285f, 0.4405f, 0.484f, 72f, 94f, 2f },
                { 0.5624f, 0.5695f, 0.595f, 44f, 179f, 4f },
                { 0.6686f, 0.6785f, 0.713f, 65f, 130f, 3f },
                { 0.7524f, 0.7605f, 0.786f, 51f, 154f, 3f },
                { 0.9127f, 0.9265f, 0.966f, 88f, 62f, 1f }
            };
            for (int z = 0; z < 7; z++)
            {
                float zs = spaZones[z, 0];
                float zp = spaZones[z, 1];
                float ze = spaZones[z, 2];
                float pk = spaZones[z, 3];
                float ms = spaZones[z, 4];
                int mg = (int)spaZones[z, 5];

                if (pct >= zs && pct <= ze + 0.015f)
                {
                    if (pct <= zp)
                    {
                        float t = (pct - zs) / Math.Max(0.001f, zp - zs);
                        simBrk = pk * (float)Math.Pow(Math.Max(0f, t), 0.7);
                    }
                    else if (pct <= ze)
                    {
                        float t = (pct - zp) / Math.Max(0.001f, ze - zp);
                        simBrk = pk * (float)Math.Pow(Math.Max(0f, 1.0f - t), 1.45);
                    }
                    simThr = pct < ze - 0.003f ? 0f : Math.Min(100f, ((pct - (ze - 0.003f)) / 0.018f) * 100f);
                    simSpd = ms + (1.0f - (simBrk / 100f)) * (248f - ms);
                    simGear = pct >= zp ? mg : Math.Min(6, mg + 2);
                    break;
                }
            }

            snap.PlayerBrakePct = Math.Max(0f, Math.Min(100f, simBrk));
            snap.PlayerThrottlePct = Math.Max(0f, Math.Min(100f, simThr));
            snap.PlayerSpeedKph = simSpd;
            snap.PlayerGear = simGear;
            return snap;
        }
    }

    public class LiveIracingReader
    {
        private class CachedYamlDriver
        {
            public int CarIdx;
            public string CarNumber;
            public string UserName;
            public string ClubName;
            public string CountryCode;
            public int IRating;
            public string LicClass;
            public string SafetyRating;
        }

        private MemoryMappedFile _mmf;
        private MemoryMappedViewAccessor _acc;
        private long _nextOpenAttemptTicks = 0;

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr OpenFileMapping(uint dwDesiredAccess, bool bInheritHandle, string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        private const uint FILE_MAP_READ = 0x0004;

        private int _cachedSessionInfoUpdate = -1;
        private int _cachedSessionInfoLen = -1;
        private int _cachedSessionInfoOffset = -1;
        private int _cachedSessionNum = -1;
        private string _cachedYaml = string.Empty;
        private string _cachedTrackName = "iRacing Circuit";
        private string _cachedSessionType = "RACE";
        private float _cachedTrackLenMeters = 5000f;
        private float[] _cachedSectorPcts = new float[] { 0.0f, 0.333333f, 0.666667f };
        private readonly List<CachedYamlDriver> _cachedYamlDrivers = new List<CachedYamlDriver>();
        private List<DriverRow> _cachedSortedDrivers = new List<DriverRow>();
        private int _cachedSof = 1500;
        private int _cachedMaxLap = 1;
        private float _cachedSessionBestLap = float.MaxValue;

        // Cached VarHeader offsets (refreshed only when numVars or varHeaderOffset changes)
        private int _cachedNumVars = -1;
        private int _cachedVarHeaderOffset = -1;
        private int _offPlayerCarIdx = -1;
        private int _offPlayerTrackSurface = -1;
        private int _offSessionNum = -1;
        private int _offSessionTimeRemain = -1;
        private int _offSessionLapsRemainEx = -1;
        private int _offTrackTempCrew = -1;
        private int _offDeltaSessionBest = -1;
        private int _offDeltaSessionBestDD = -1;
        private int _offLapBestLapTime = -1;
        private int _offDeltaSessionOpt = -1;
        private int _offDeltaSessionOptDD = -1;
        private int _offDeltaBest = -1;
        private int _offDeltaBestDD = -1;
        private int _offDeltaLast = -1;
        private int _offDeltaLastDD = -1;
        private int _offLapLastLapTime = -1;
        private int _offBrake = -1;
        private int _offThrottle = -1;
        private int _offSpeed = -1;
        private int _offGear = -1;
        private int _offLapDistPct = -1;
        private int _offCarIdxPos = -1, _cntCarIdxPos = 0;
        private int _offCarIdxLap = -1, _cntCarIdxLap = 0;
        private int _offCarIdxLastLap = -1, _cntCarIdxLastLap = 0;
        private int _offCarIdxBestLap = -1, _cntCarIdxBestLap = 0;
        private int _offCarIdxLapDist = -1, _cntCarIdxLapDist = 0;
        private int _offCarIdxF2Time = -1, _cntCarIdxF2Time = 0;
        private int _offCarIdxOnPit = -1, _cntCarIdxOnPit = 0;

        private byte[] _varHeadersBuf;
        private byte[] _dataBuf;
        private readonly int[] _pos = new int[64];
        private readonly int[] _lap = new int[64];
        private readonly float[] _lastLap = new float[64];
        private readonly float[] _bestLap = new float[64];
        private readonly float[] _lapDist = new float[64];
        private readonly float[] _f2Time = new float[64];
        private readonly bool[] _onPit = new bool[64];

        private int _lastSectorIdx = 0;
        private int _cachedSectorCount = 0;
        private float[] _secStartBest = new float[0];
        private float[] _secStartOpt = new float[0];
        private float[] _secStartPers = new float[0];
        private float[] _secStartLast = new float[0];

        private float[] _secSplitBest = new float[0];
        private float[] _secSplitOpt = new float[0];
        private float[] _secSplitPers = new float[0];
        private float[] _secSplitLast = new float[0];
        private bool[] _secDone = new bool[0];

        private float _prevDeltaBest = 0f;
        private float _prevDeltaOpt = 0f;
        private float _prevDeltaPers = 0f;
        private float _prevDeltaLast = 0f;

        private void CloseMapping()
        {
            if (_acc != null)
            {
                try { _acc.Dispose(); } catch { }
                _acc = null;
            }
            if (_mmf != null)
            {
                try { _mmf.Dispose(); } catch { }
                _mmf = null;
            }
            _cachedSessionInfoUpdate = -1;
            _cachedNumVars = -1;
            _cachedVarHeaderOffset = -1;
        }

        private static string ReadCString(byte[] buf, int offset, int maxLen)
        {
            int end = offset;
            int limit = Math.Min(buf.Length, offset + maxLen);
            while (end < limit && buf[end] != 0) end++;
            if (end <= offset) return string.Empty;
            return Encoding.ASCII.GetString(buf, offset, end - offset);
        }

        private static string ExtractYamlField(string chunk, string key, string defVal)
        {
            Match m = Regex.Match(chunk, @"^[ \t]*" + Regex.Escape(key) + @":[ \t]*([^\r\n]*)$", RegexOptions.Multiline);
            if (!m.Success) return defVal;
            string val = m.Groups[1].Value.Trim().Trim('"', '\'');
            if (string.IsNullOrEmpty(val) || string.Equals(val, "-none-", StringComparison.OrdinalIgnoreCase))
                return defVal;
            return val;
        }

        private static string ExtractTopLevelYamlSection(string yaml, string sectionName)
        {
            Match startMatch = Regex.Match(yaml, @"(?:^|\r?\n)" + Regex.Escape(sectionName) + @":[ \t]*\r?\n");
            if (!startMatch.Success) return string.Empty;
            int contentStart = startMatch.Index + startMatch.Length;
            string tail = yaml.Substring(contentStart);
            Match nextTop = Regex.Match(tail, @"\r?\n(?:[A-Za-z0-9_]+:|\.\.\.)");
            return nextTop.Success ? tail.Substring(0, nextTop.Index) : tail;
        }

        private static string ExtractSessionTypeFromYaml(string yaml, int sessionNum)
        {
            string sessionInfo = ExtractTopLevelYamlSection(yaml, "SessionInfo");
            if (string.IsNullOrEmpty(sessionInfo)) return "RACE";

            string[] sessionBlocks = Regex.Split(sessionInfo, @"(?:^|\r?\n)[ \t]*-[ \t]*SessionNum:");
            string activeChunk = string.Empty;
            for (int i = 1; i < sessionBlocks.Length; i++)
            {
                string chunk = "SessionNum:" + sessionBlocks[i];
                int sNum;
                if (int.TryParse(ExtractYamlField(chunk, "SessionNum", "-1"), out sNum) && sNum == sessionNum)
                {
                    activeChunk = chunk;
                    break;
                }
                activeChunk = chunk;
            }

            string rawType = ExtractYamlField(activeChunk, "SessionType", "Race");
            if (rawType.IndexOf("Prac", StringComparison.OrdinalIgnoreCase) >= 0 ||
                rawType.IndexOf("Test", StringComparison.OrdinalIgnoreCase) >= 0)
                return "PRAC";
            if (rawType.IndexOf("Qual", StringComparison.OrdinalIgnoreCase) >= 0)
                return "QUAL";
            if (rawType.IndexOf("Warm", StringComparison.OrdinalIgnoreCase) >= 0)
                return "WARM";
            return "RACE";
        }

        private static float[] ExtractTrackSectorsFromYaml(string yaml)
        {
            int splitIdx = yaml.IndexOf("SplitTimeInfo:");
            if (splitIdx >= 0)
            {
                string tail = yaml.Substring(splitIdx);
                Match nextTop = Regex.Match(tail.Substring(14), @"\r?\n[A-Za-z0-9_]+:");
                string splitSection = nextTop.Success ? tail.Substring(0, 14 + nextTop.Index) : tail;

                MatchCollection matches = Regex.Matches(splitSection, @"SectorStartPct:\s*([0-9]+(?:\.[0-9]+)?)");
                if (matches.Count >= 2)
                {
                    List<float> pcts = new List<float>();
                    for (int i = 0; i < matches.Count; i++)
                    {
                        float val;
                        if (float.TryParse(matches[i].Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out val))
                        {
                            if (val >= 0.0f && val < 0.999f)
                            {
                                pcts.Add(val);
                            }
                        }
                    }
                    if (pcts.Count >= 2)
                    {
                        pcts.Sort();
                        if (pcts[0] > 0.01f) pcts.Insert(0, 0.0f);
                        else pcts[0] = 0.0f;
                        return pcts.ToArray();
                    }
                }
            }
            return new float[] { 0.0f, 0.333333f, 0.666667f };
        }

        private void RefreshYamlCache(string yaml, int sessionNum)
        {
            _cachedYaml = yaml;
            _cachedSessionNum = sessionNum;
            _cachedTrackName = ExtractYamlField(yaml, "TrackDisplayName", "iRacing Circuit");
            _cachedSessionType = ExtractSessionTypeFromYaml(yaml, sessionNum);
            _cachedSectorPcts = ExtractTrackSectorsFromYaml(yaml);

            float trackLenMeters = 5000f;
            string rawLen = ExtractYamlField(yaml, "TrackLength", "5.00 km");
            Match lenMatch = Regex.Match(rawLen, @"([0-9]+(?:\.[0-9]+)?)\s*(km|mi)?", RegexOptions.IgnoreCase);
            if (lenMatch.Success)
            {
                float parsedLen;
                if (float.TryParse(lenMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsedLen))
                {
                    string unit = lenMatch.Groups[2].Value.ToLowerInvariant();
                    if (unit == "mi") trackLenMeters = parsedLen * 1609.34f;
                    else trackLenMeters = parsedLen * 1000.0f;
                }
            }
            _cachedTrackLenMeters = Math.Max(500f, trackLenMeters);

            _cachedYamlDrivers.Clear();
            string driverInfoSection = ExtractTopLevelYamlSection(yaml, "DriverInfo");
            if (string.IsNullOrEmpty(driverInfoSection)) return;

            int paceCarIdx = -1;
            int.TryParse(ExtractYamlField(driverInfoSection, "PaceCarIdx", "-1"), out paceCarIdx);

            Match driversHeaderMatch = Regex.Match(driverInfoSection, @"(?:^|\r?\n)([ \t]+)Drivers:[ \t]*\r?\n");
            if (!driversHeaderMatch.Success) return;

            string driversIndent = driversHeaderMatch.Groups[1].Value;
            string driversTail = driverInfoSection.Substring(driversHeaderMatch.Index + driversHeaderMatch.Length);
            Match nextSiblingKey = Regex.Match(driversTail, @"\r?\n" + Regex.Escape(driversIndent) + @"[A-Za-z0-9_]+:");
            string driversSection = "\n" + (nextSiblingKey.Success ? driversTail.Substring(0, nextSiblingKey.Index) : driversTail);

            string[] blocks = Regex.Split(driversSection, @"\r?\n[ \t]*-[ \t]*CarIdx:");
            HashSet<int> seenCarIdx = new HashSet<int>();

            for (int i = 1; i < blocks.Length; i++)
            {
                string chunk = "CarIdx:" + blocks[i];
                if (ExtractYamlField(chunk, "CarIsPaceCar", "0") == "1") continue;
                if (ExtractYamlField(chunk, "IsSpectator", "0") == "1") continue;

                int cIdx;
                if (!int.TryParse(ExtractYamlField(chunk, "CarIdx", "-1"), out cIdx)) continue;
                if (cIdx < 0 || cIdx >= 64) continue;
                if (cIdx == paceCarIdx || seenCarIdx.Contains(cIdx)) continue;

                string userName = ExtractYamlField(chunk, "UserName", string.Empty);
                if (string.IsNullOrEmpty(userName) ||
                    string.Equals(userName, "Pace Car", StringComparison.OrdinalIgnoreCase))
                    continue;

                seenCarIdx.Add(cIdx);

                int ir;
                if (!int.TryParse(ExtractYamlField(chunk, "IRating", "1350"), out ir)) ir = 1350;

                string licRaw = ExtractYamlField(chunk, "LicString", "R 2.50");
                string licClass = "R";
                string srVal = "2.50";
                Match lm = Regex.Match(licRaw, @"^([RDCBAP])\s*([0-9]+\.[0-9]{2})", RegexOptions.IgnoreCase);
                if (lm.Success)
                {
                    licClass = lm.Groups[1].Value.ToUpperInvariant();
                    srVal = lm.Groups[2].Value;
                }

                string club = ExtractYamlField(chunk, "ClubName", ExtractYamlField(chunk, "FlairName", "International"));

                CachedYamlDriver cd = new CachedYamlDriver();
                cd.CarIdx = cIdx;
                cd.CarNumber = ExtractYamlField(chunk, "CarNumber", cIdx.ToString());
                cd.UserName = userName;
                cd.ClubName = club;
                cd.CountryCode = CountryResolver.Resolve(club, null);
                cd.IRating = ir;
                cd.LicClass = licClass;
                cd.SafetyRating = srVal;
                _cachedYamlDrivers.Add(cd);
            }
        }

        private void RefreshVarOffsets(int numVars, int varHeaderOffset)
        {
            int headersLen = numVars * 144;
            if (_varHeadersBuf == null || _varHeadersBuf.Length != headersLen)
                _varHeadersBuf = new byte[headersLen];
            _acc.ReadArray(varHeaderOffset, _varHeadersBuf, 0, headersLen);

            _offPlayerCarIdx = _offPlayerTrackSurface = _offSessionNum = -1;
            _offSessionTimeRemain = _offSessionLapsRemainEx = _offTrackTempCrew = -1;
            _offDeltaSessionBest = _offDeltaSessionBestDD = _offLapBestLapTime = -1;
            _offDeltaSessionOpt = _offDeltaSessionOptDD = -1;
            _offDeltaBest = _offDeltaBestDD = -1;
            _offDeltaLast = _offDeltaLastDD = _offLapLastLapTime = -1;
            _offBrake = _offThrottle = _offSpeed = _offGear = _offLapDistPct = -1;
            _offCarIdxPos = _offCarIdxLap = _offCarIdxLastLap = _offCarIdxBestLap = -1;
            _offCarIdxLapDist = _offCarIdxF2Time = _offCarIdxOnPit = -1;

            for (int v = 0; v < numVars; v++)
            {
                int hOff = v * 144;
                int vOffset = BitConverter.ToInt32(_varHeadersBuf, hOff + 4);
                int vCount = BitConverter.ToInt32(_varHeadersBuf, hOff + 8);
                string vName = ReadCString(_varHeadersBuf, hOff + 16, 32);

                if (vName == "PlayerCarIdx") _offPlayerCarIdx = vOffset;
                else if (vName == "PlayerTrackSurface") _offPlayerTrackSurface = vOffset;
                else if (vName == "SessionNum") _offSessionNum = vOffset;
                else if (vName == "SessionTimeRemain") _offSessionTimeRemain = vOffset;
                else if (vName == "SessionLapsRemainEx") _offSessionLapsRemainEx = vOffset;
                else if (vName == "TrackTempCrew") _offTrackTempCrew = vOffset;
                else if (vName == "LapDeltaToSessionBestLap") _offDeltaSessionBest = vOffset;
                else if (vName == "LapDeltaToSessionBestLap_DD") _offDeltaSessionBestDD = vOffset;
                else if (vName == "LapBestLapTime") _offLapBestLapTime = vOffset;
                else if (vName == "LapDeltaToSessionOptimalLap") _offDeltaSessionOpt = vOffset;
                else if (vName == "LapDeltaToSessionOptimalLap_DD") _offDeltaSessionOptDD = vOffset;
                else if (vName == "LapDeltaToBestLap") _offDeltaBest = vOffset;
                else if (vName == "LapDeltaToBestLap_DD") _offDeltaBestDD = vOffset;
                else if (vName == "LapDeltaToSessionLastLap") _offDeltaLast = vOffset;
                else if (vName == "LapDeltaToSessionLastLap_DD") _offDeltaLastDD = vOffset;
                else if (vName == "LapLastLapTime") _offLapLastLapTime = vOffset;
                else if (vName == "Brake") _offBrake = vOffset;
                else if (vName == "Throttle") _offThrottle = vOffset;
                else if (vName == "Speed") _offSpeed = vOffset;
                else if (vName == "Gear") _offGear = vOffset;
                else if (vName == "LapDistPct") _offLapDistPct = vOffset;
                else if (vName == "CarIdxPosition") { _offCarIdxPos = vOffset; _cntCarIdxPos = Math.Min(64, vCount); }
                else if (vName == "CarIdxLap") { _offCarIdxLap = vOffset; _cntCarIdxLap = Math.Min(64, vCount); }
                else if (vName == "CarIdxLastLapTime") { _offCarIdxLastLap = vOffset; _cntCarIdxLastLap = Math.Min(64, vCount); }
                else if (vName == "CarIdxBestLapTime") { _offCarIdxBestLap = vOffset; _cntCarIdxBestLap = Math.Min(64, vCount); }
                else if (vName == "CarIdxLapDistPct") { _offCarIdxLapDist = vOffset; _cntCarIdxLapDist = Math.Min(64, vCount); }
                else if (vName == "CarIdxF2Time") { _offCarIdxF2Time = vOffset; _cntCarIdxF2Time = Math.Min(64, vCount); }
                else if (vName == "CarIdxOnPitRoad") { _offCarIdxOnPit = vOffset; _cntCarIdxOnPit = Math.Min(64, vCount); }
            }

            _cachedNumVars = numVars;
            _cachedVarHeaderOffset = varHeaderOffset;
        }

        public SessionSnapshot TryReadLive()
        {
            return TryReadLive(true);
        }

        public SessionSnapshot TryReadLive(bool rebuildStandings)
        {
            try
            {
                if (_mmf == null || _acc == null)
                {
                    long nowTicks = Environment.TickCount;
                    if (nowTicks - _nextOpenAttemptTicks < 0)
                        return null;
                    _nextOpenAttemptTicks = nowTicks + 1000; // Probe at most once per second when disconnected

                    IntPtr hMap = OpenFileMapping(FILE_MAP_READ, false, "Local\\IRSDKMemMapFileName");
                    if (hMap == IntPtr.Zero)
                        return null;
                    CloseHandle(hMap);

                    CloseMapping();
                    _mmf = MemoryMappedFile.OpenExisting("Local\\IRSDKMemMapFileName", MemoryMappedFileRights.Read);
                    _acc = _mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
                }

                int status = _acc.ReadInt32(4);
                if ((status & 1) == 0)
                {
                    return null;
                }

                int sessionInfoUpdate = _acc.ReadInt32(12);
                int sessionInfoLen = _acc.ReadInt32(16);
                int sessionInfoOffset = _acc.ReadInt32(20);
                int numVars = _acc.ReadInt32(24);
                int varHeaderOffset = _acc.ReadInt32(28);
                int numBuf = _acc.ReadInt32(32);
                int bufLen = _acc.ReadInt32(36);

                if (numVars <= 0 || bufLen <= 0 || sessionInfoLen <= 0) return null;

                if (numVars != _cachedNumVars || varHeaderOffset != _cachedVarHeaderOffset)
                {
                    RefreshVarOffsets(numVars, varHeaderOffset);
                }

                int bestTick = -1;
                int bestBufOffset = 0;
                for (int i = 0; i < numBuf && i < 4; i++)
                {
                    int bOff = 48 + i * 16;
                    int tick = _acc.ReadInt32(bOff);
                    int off = _acc.ReadInt32(bOff + 4);
                    if (tick > bestTick)
                    {
                        bestTick = tick;
                        bestBufOffset = off;
                    }
                }

                if (_dataBuf == null || _dataBuf.Length != bufLen)
                    _dataBuf = new byte[bufLen];
                _acc.ReadArray(bestBufOffset, _dataBuf, 0, bufLen);

                int playerCarIdx = _offPlayerCarIdx >= 0 ? BitConverter.ToInt32(_dataBuf, _offPlayerCarIdx) : 0;
                int playerTrackSurface = _offPlayerTrackSurface >= 0 ? BitConverter.ToInt32(_dataBuf, _offPlayerTrackSurface) : 3;
                int sessionNum = _offSessionNum >= 0 ? BitConverter.ToInt32(_dataBuf, _offSessionNum) : 0;
                double sessionTimeRemain = _offSessionTimeRemain >= 0 ? BitConverter.ToDouble(_dataBuf, _offSessionTimeRemain) : 0.0;
                int sessionLapsRemain = _offSessionLapsRemainEx >= 0 ? BitConverter.ToInt32(_dataBuf, _offSessionLapsRemainEx) : 0;
                float trackTemp = _offTrackTempCrew >= 0 ? BitConverter.ToSingle(_dataBuf, _offTrackTempCrew) : 32f;

                // Native 60Hz Delta Bar variables from iRacing SDK
                float deltaSessionBest = _offDeltaSessionBest >= 0 ? BitConverter.ToSingle(_dataBuf, _offDeltaSessionBest) : 0f;
                float deltaSessionBestDD = _offDeltaSessionBestDD >= 0 ? BitConverter.ToSingle(_dataBuf, _offDeltaSessionBestDD) : 0f;
                float lapBestLapTime = _offLapBestLapTime >= 0 ? BitConverter.ToSingle(_dataBuf, _offLapBestLapTime) : 0f;

                float deltaSessionOpt = _offDeltaSessionOpt >= 0 ? BitConverter.ToSingle(_dataBuf, _offDeltaSessionOpt) : 0f;
                float deltaSessionOptDD = _offDeltaSessionOptDD >= 0 ? BitConverter.ToSingle(_dataBuf, _offDeltaSessionOptDD) : 0f;

                float deltaBest = _offDeltaBest >= 0 ? BitConverter.ToSingle(_dataBuf, _offDeltaBest) : 0f;
                float deltaBestDD = _offDeltaBestDD >= 0 ? BitConverter.ToSingle(_dataBuf, _offDeltaBestDD) : 0f;

                float deltaLast = _offDeltaLast >= 0 ? BitConverter.ToSingle(_dataBuf, _offDeltaLast) : 0f;
                float deltaLastDD = _offDeltaLastDD >= 0 ? BitConverter.ToSingle(_dataBuf, _offDeltaLastDD) : 0f;
                float lapLastLapTime = _offLapLastLapTime >= 0 ? BitConverter.ToSingle(_dataBuf, _offLapLastLapTime) : 0f;

                // Player live pedal, speed, gear & distance telemetry for Garage61 Braking Coach
                float playerBrakeRaw = _offBrake >= 0 ? BitConverter.ToSingle(_dataBuf, _offBrake) : 0f;
                float playerThrottleRaw = _offThrottle >= 0 ? BitConverter.ToSingle(_dataBuf, _offThrottle) : 0f;
                float playerSpeedMs = _offSpeed >= 0 ? BitConverter.ToSingle(_dataBuf, _offSpeed) : 0f;
                int playerGearVal = _offGear >= 0 ? BitConverter.ToInt32(_dataBuf, _offGear) : 0;
                float scalarLapDistPct = _offLapDistPct >= 0 ? BitConverter.ToSingle(_dataBuf, _offLapDistPct) : -1f;

                float playerPct = scalarLapDistPct;
                if (playerPct < 0f && _offCarIdxLapDist >= 0 && playerCarIdx >= 0 && playerCarIdx < _cntCarIdxLapDist)
                {
                    playerPct = BitConverter.ToSingle(_dataBuf, _offCarIdxLapDist + playerCarIdx * 4);
                }
                if (playerPct < 0f) playerPct = 0f;

                // Only read and regex-parse the 100KB+ SessionInfo YAML when iRacing increments sessionInfoUpdate
                bool yamlUpdated = false;
                if (_cachedYamlDrivers.Count == 0 ||
                    sessionInfoUpdate != _cachedSessionInfoUpdate ||
                    sessionInfoLen != _cachedSessionInfoLen ||
                    sessionInfoOffset != _cachedSessionInfoOffset)
                {
                    byte[] yamlBytes = new byte[sessionInfoLen];
                    _acc.ReadArray(sessionInfoOffset, yamlBytes, 0, sessionInfoLen);
                    string yaml = ReadCString(yamlBytes, 0, sessionInfoLen);
                    RefreshYamlCache(yaml, sessionNum);
                    _cachedSessionInfoUpdate = sessionInfoUpdate;
                    _cachedSessionInfoLen = sessionInfoLen;
                    _cachedSessionInfoOffset = sessionInfoOffset;
                    yamlUpdated = true;
                }
                else if (sessionNum != _cachedSessionNum && !string.IsNullOrEmpty(_cachedYaml))
                {
                    _cachedSessionNum = sessionNum;
                    _cachedSessionType = ExtractSessionTypeFromYaml(_cachedYaml, sessionNum);
                    yamlUpdated = true;
                }

                if (_cachedYamlDrivers.Count == 0) return null;

                string trackName = _cachedTrackName;
                string sessionType = _cachedSessionType;
                bool isRaceSession = (sessionType == "RACE");

                if (rebuildStandings || yamlUpdated || _cachedSortedDrivers.Count == 0)
                {
                    Array.Clear(_pos, 0, 64);
                    Array.Clear(_lap, 0, 64);
                    Array.Clear(_lastLap, 0, 64);
                    Array.Clear(_bestLap, 0, 64);
                    Array.Clear(_lapDist, 0, 64);
                    Array.Clear(_f2Time, 0, 64);
                    Array.Clear(_onPit, 0, 64);

                    if (_offCarIdxPos >= 0)
                        for (int c = 0; c < _cntCarIdxPos; c++) _pos[c] = BitConverter.ToInt32(_dataBuf, _offCarIdxPos + c * 4);
                    if (_offCarIdxLap >= 0)
                        for (int c = 0; c < _cntCarIdxLap; c++) _lap[c] = BitConverter.ToInt32(_dataBuf, _offCarIdxLap + c * 4);
                    if (_offCarIdxLastLap >= 0)
                        for (int c = 0; c < _cntCarIdxLastLap; c++) _lastLap[c] = BitConverter.ToSingle(_dataBuf, _offCarIdxLastLap + c * 4);
                    if (_offCarIdxBestLap >= 0)
                        for (int c = 0; c < _cntCarIdxBestLap; c++) _bestLap[c] = BitConverter.ToSingle(_dataBuf, _offCarIdxBestLap + c * 4);
                    if (_offCarIdxLapDist >= 0)
                        for (int c = 0; c < _cntCarIdxLapDist; c++) _lapDist[c] = BitConverter.ToSingle(_dataBuf, _offCarIdxLapDist + c * 4);
                    if (_offCarIdxF2Time >= 0)
                        for (int c = 0; c < _cntCarIdxF2Time; c++) _f2Time[c] = BitConverter.ToSingle(_dataBuf, _offCarIdxF2Time + c * 4);
                    if (_offCarIdxOnPit >= 0)
                        for (int c = 0; c < _cntCarIdxOnPit; c++) _onPit[c] = _dataBuf[_offCarIdxOnPit + c] != 0;

                    List<DriverRow> rows = new List<DriverRow>(_cachedYamlDrivers.Count);
                    float sessionBest = float.MaxValue;

                    for (int i = 0; i < _cachedYamlDrivers.Count; i++)
                    {
                        CachedYamlDriver cd = _cachedYamlDrivers[i];
                        int cIdx = cd.CarIdx;
                        float bLap = _bestLap[cIdx];
                        float lLap = _lastLap[cIdx];
                        if (cIdx == playerCarIdx)
                        {
                            if (bLap <= 0f && lapBestLapTime > 0f) bLap = lapBestLapTime;
                            if (lLap <= 0f && lapLastLapTime > 0f) lLap = lapLastLapTime;
                        }
                        if (bLap > 0f && bLap < sessionBest) sessionBest = bLap;

                        DriverRow row = new DriverRow();
                        row.CarIdx = cIdx;
                        row.Position = _pos[cIdx];
                        row.CarNumber = cd.CarNumber;
                        row.UserName = cd.UserName;
                        row.ClubName = cd.ClubName;
                        row.CountryCode = cd.CountryCode;
                        row.IRating = cd.IRating;
                        row.LicClass = cd.LicClass;
                        row.SafetyRating = cd.SafetyRating;
                        row.LastLapSec = lLap > 0f ? lLap : 0f;
                        row.BestLapSec = bLap > 0f ? bLap : 0f;
                        row.GapToLeaderSec = Math.Max(0f, _f2Time[cIdx]);
                        row.Lap = _lap[cIdx];
                        row.LapDistPct = _lapDist[cIdx];
                        row.OnPitRoad = _onPit[cIdx];
                        row.IsPlayer = (cIdx == playerCarIdx);
                        rows.Add(row);
                    }

                    rows.Sort(delegate(DriverRow a, DriverRow b)
                    {
                        if (a.Position > 0 && b.Position > 0) return a.Position.CompareTo(b.Position);
                        if (a.Position > 0) return -1;
                        if (b.Position > 0) return 1;
                        if (!isRaceSession)
                        {
                            if (a.BestLapSec > 0f && b.BestLapSec > 0f) return a.BestLapSec.CompareTo(b.BestLapSec);
                            if (a.BestLapSec > 0f) return -1;
                            if (b.BestLapSec > 0f) return 1;
                        }
                        float progA = a.Lap + a.LapDistPct;
                        float progB = b.Lap + b.LapDistPct;
                        if (Math.Abs(progA - progB) > 0.001f) return progB.CompareTo(progA);
                        return b.IRating.CompareTo(a.IRating);
                    });

                    int irSum = 0;
                    for (int i = 0; i < rows.Count; i++) irSum += rows[i].IRating;
                    _cachedSof = irSum / rows.Count;

                    int maxLap = 1;
                    float leaderBestLap = rows[0].BestLapSec;
                    for (int i = 0; i < rows.Count; i++)
                    {
                        DriverRow d = rows[i];
                        d.Position = i + 1;
                        if (d.Lap > maxLap) maxLap = d.Lap;
                        if (i == 0)
                        {
                            d.GapToLeaderSec = 0f;
                            d.IntervalAheadSec = 0f;
                        }
                        else
                        {
                            if (!isRaceSession)
                            {
                                d.GapToLeaderSec = (d.BestLapSec > 0f && leaderBestLap > 0f)
                                    ? Math.Max(0f, d.BestLapSec - leaderBestLap)
                                    : 0f;
                            }
                            d.IntervalAheadSec = Math.Max(0f, d.GapToLeaderSec - rows[i - 1].GapToLeaderSec);
                        }

                        if (d.LastLapSec > 0f && Math.Abs(d.LastLapSec - sessionBest) < 0.003f)
                            d.LastLapStatus = LapStatus.SessionBest;
                        else if (d.LastLapSec > 0f && d.BestLapSec > 0f && Math.Abs(d.LastLapSec - d.BestLapSec) < 0.003f)
                            d.LastLapStatus = LapStatus.PersonalBest;
                        else
                            d.LastLapStatus = LapStatus.Normal;

                        d.IRatingDelta = CountryResolver.EstimateIRatingDelta(d.Position, rows.Count, d.IRating, _cachedSof);
                    }

                    _cachedSortedDrivers = rows;
                    _cachedMaxLap = maxLap;
                    _cachedSessionBestLap = sessionBest;
                }

                // Official track sectors cached from iRacing SDK YAML (SplitTimeInfo -> Sectors)
                float[] sectorPcts = _cachedSectorPcts;
                int secCount = sectorPcts.Length;
                if (secCount != _cachedSectorCount)
                {
                    _cachedSectorCount = secCount;
                    _secStartBest = new float[secCount];
                    _secStartOpt = new float[secCount];
                    _secStartPers = new float[secCount];
                    _secStartLast = new float[secCount];
                    _secSplitBest = new float[secCount];
                    _secSplitOpt = new float[secCount];
                    _secSplitPers = new float[secCount];
                    _secSplitLast = new float[secCount];
                    _secDone = new bool[secCount];
                    _lastSectorIdx = 0;
                }

                int curSecIdx = 0;
                for (int sIdx = secCount - 1; sIdx >= 0; sIdx--)
                {
                    if (playerPct >= sectorPcts[sIdx])
                    {
                        curSecIdx = sIdx;
                        break;
                    }
                }

                if (curSecIdx != _lastSectorIdx)
                {
                    if (curSecIdx > _lastSectorIdx)
                    {
                        // Completed _lastSectorIdx on track -> lock its split and record start boundary for curSecIdx
                        _secSplitBest[_lastSectorIdx] = deltaSessionBest - _secStartBest[_lastSectorIdx];
                        _secSplitOpt[_lastSectorIdx] = deltaSessionOpt - _secStartOpt[_lastSectorIdx];
                        _secSplitPers[_lastSectorIdx] = deltaBest - _secStartPers[_lastSectorIdx];
                        _secSplitLast[_lastSectorIdx] = deltaLast - _secStartLast[_lastSectorIdx];
                        _secDone[_lastSectorIdx] = true;

                        _secStartBest[curSecIdx] = deltaSessionBest;
                        _secStartOpt[curSecIdx] = deltaSessionOpt;
                        _secStartPers[curSecIdx] = deltaBest;
                        _secStartLast[curSecIdx] = deltaLast;
                    }
                    else if (curSecIdx == 0)
                    {
                        // Crossed Start/Finish into Sector 1 -> lock final sector from just before S/F reset
                        int finalIdx = secCount - 1;
                        _secSplitBest[finalIdx] = _prevDeltaBest - _secStartBest[finalIdx];
                        _secSplitOpt[finalIdx] = _prevDeltaOpt - _secStartOpt[finalIdx];
                        _secSplitPers[finalIdx] = _prevDeltaPers - _secStartPers[finalIdx];
                        _secSplitLast[finalIdx] = _prevDeltaLast - _secStartLast[finalIdx];
                        _secDone[finalIdx] = true;

                        for (int k = 0; k < secCount - 1; k++)
                        {
                            _secDone[k] = false;
                            _secStartBest[k] = 0f;
                            _secStartOpt[k] = 0f;
                            _secStartPers[k] = 0f;
                            _secStartLast[k] = 0f;
                        }
                    }
                    _lastSectorIdx = curSecIdx;
                }

                // Continuously update active sector's live split at 60Hz
                _secSplitBest[curSecIdx] = deltaSessionBest - _secStartBest[curSecIdx];
                _secSplitOpt[curSecIdx] = deltaSessionOpt - _secStartOpt[curSecIdx];
                _secSplitPers[curSecIdx] = deltaBest - _secStartPers[curSecIdx];
                _secSplitLast[curSecIdx] = deltaLast - _secStartLast[curSecIdx];

                _prevDeltaBest = deltaSessionBest;
                _prevDeltaOpt = deltaSessionOpt;
                _prevDeltaPers = deltaBest;
                _prevDeltaLast = deltaLast;

                DeltaTelemetryData dt = new DeltaTelemetryData();
                dt.SessionBestLapSec = lapBestLapTime > 0f ? lapBestLapTime : (_cachedSessionBestLap < float.MaxValue ? _cachedSessionBestLap : 0f);
                dt.DeltaSessionBestSec = deltaSessionBest;
                dt.DeltaSessionBestRate = deltaSessionBestDD;

                dt.SessionOptimalLapSec = dt.SessionBestLapSec > 0f ? Math.Max(0f, dt.SessionBestLapSec - (deltaSessionOpt - deltaSessionBest)) : 0f;
                dt.DeltaSessionOptimalSec = deltaSessionOpt;
                dt.DeltaSessionOptimalRate = deltaSessionOptDD;

                dt.PersonalBestLapSec = dt.SessionBestLapSec;
                dt.DeltaPersonalBestSec = deltaBest;
                dt.DeltaPersonalBestRate = deltaBestDD;

                dt.LastLapSec = lapLastLapTime;
                dt.DeltaLastLapSec = deltaLast;
                dt.DeltaLastLapRate = deltaLastDD;

                dt.SectorStartPcts = sectorPcts;
                dt.ActiveSectorIndex = curSecIdx;
                dt.SectorDeltasSessionBest = _secSplitBest;
                dt.SectorDeltasOptimal = _secSplitOpt;
                dt.SectorDeltasPersonalBest = _secSplitPers;
                dt.SectorDeltasLastLap = _secSplitLast;
                dt.SectorDone = _secDone;
                dt.OffTrack = (playerTrackSurface == 0);

                SessionSnapshot snap = new SessionSnapshot();
                snap.Connected = true;
                snap.IsLiveSource = true;
                snap.TrackName = trackName;
                snap.SessionType = sessionType;
                snap.StrengthOfField = _cachedSof;
                snap.CurrentLap = _cachedMaxLap;
                snap.TotalLaps = (sessionLapsRemain > 0 && sessionLapsRemain < 32767) ? sessionLapsRemain : 0;
                snap.SessionTimeRemainSec = Math.Max(0.0, sessionTimeRemain);
                snap.TrackTempC = trackTemp;
                snap.Drivers = _cachedSortedDrivers;
                snap.Delta = dt;
                snap.PlayerLapDistPct = playerPct;
                snap.TrackLengthMeters = _cachedTrackLenMeters;
                snap.PlayerBrakePct = Math.Max(0f, Math.Min(100f, playerBrakeRaw * 100.0f));
                snap.PlayerThrottlePct = Math.Max(0f, Math.Min(100f, playerThrottleRaw * 100.0f));
                snap.PlayerSpeedKph = Math.Max(0f, playerSpeedMs * 3.6f);
                snap.PlayerGear = playerGearVal;
                return snap;
            }
            catch
            {
                CloseMapping();
                return null;
            }
        }
    }

    /// <summary>
    /// Option C Delta Bar Overlay Window:
    /// - Top row: Reference Mode & Ref Lap Time | Center Big Delta Readout (colored by _DD rate-of-change) | Predicted Lap Time
    /// - Center row: Bi-directional Center-Zero Progress Bar (Green/Purple left for faster, Red right for slower)
    /// - Bottom row: S1 / S2 / S3 Sector Splits (toggleable)
    /// - Same SimLab-style single top-right icon toggle button (Resize <-> Aa Font) + 4 corner grips
    /// </summary>
    public class DeltaBarForm : Form
    {
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int WS_EX_TOPMOST = 0x00000008;
        private const int WM_DPICHANGED = 0x02E0;
        private const int WM_NCLBUTTONDOWN = 0x00A1;
        private const int HT_CAPTION = 0x02;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private readonly OverlaySettings _settings;
        private SessionSnapshot _snapshot;
        private float _dpiScale = 1.0f;

        private bool _isHovered = false;
        private CornerDragMode _cornerMode = CornerDragMode.ResizeWindow;
        private DragCorner _activeDragCorner = DragCorner.None;
        private Point _dragStartScreenPt;
        private Rectangle _dragStartBounds;
        private float _dragStartFontScale = 1.0f;

        private Rectangle _rectCornerModeBtn;
        private Rectangle _rectRefCycleBtn;

        private float _cachedFontScale = -1f;
        private Font _fLabel;
        private Font _fSubMono;
        private Font _fBigDelta;
        private Font _fIconAa;
        private readonly StringFormat _centerFmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        private readonly StringFormat _leftFmt = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
        private readonly StringFormat _rightFmt = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };

        private void EnsureFonts(float s)
        {
            if (Math.Abs(_cachedFontScale - s) < 0.001f && _fLabel != null) return;
            if (_fLabel != null) _fLabel.Dispose();
            if (_fSubMono != null) _fSubMono.Dispose();
            if (_fBigDelta != null) _fBigDelta.Dispose();
            if (_fIconAa != null) _fIconAa.Dispose();

            _fLabel = new Font("Segoe UI", 10.5f * s, FontStyle.Bold, GraphicsUnit.Pixel);
            _fSubMono = new Font("Consolas", 12.0f * s, FontStyle.Bold, GraphicsUnit.Pixel);
            _fBigDelta = new Font("Consolas", 21.0f * s, FontStyle.Bold, GraphicsUnit.Pixel);
            _fIconAa = new Font("Segoe UI", 11.5f * s, FontStyle.Bold, GraphicsUnit.Pixel);
            _cachedFontScale = s;
        }

        public DeltaBarForm(OverlaySettings settings, SessionSnapshot initialSnapshot)
        {
            _settings = settings;
            _snapshot = initialSnapshot;

            Text = "Free Overlay - Delta Bar";
            AutoScaleMode = AutoScaleMode.None;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            DoubleBuffered = true;
            BackColor = Color.FromArgb(13, 16, 23);
            Opacity = _settings.DeltaOpacity;

            _dpiScale = DpiHelper.GetScale(IntPtr.Zero);
            int savedX = _settings.DeltaX;
            int savedY = _settings.DeltaY;
            int savedW = _settings.DeltaW;
            int savedH = _settings.DeltaH;

            ResetPositionAndSize(false);

            if (savedW > 100 && savedH > 50)
            {
                Width = savedW;
                Height = savedH;
            }
            if (savedX != -1 || savedY != -1)
            {
                Location = new Point(savedX, savedY);
            }
            SaveGeometry();
        }

        private float EffectiveScale
        {
            get { return _dpiScale * _settings.DeltaFontScale; }
        }

        public void ResetPositionAndSize(bool notify)
        {
            float s = EffectiveScale;
            Width = (int)Math.Round(460 * s);
            Height = (int)Math.Round((_settings.ShowSectorSplits ? 124 : 92) * s);
            Rectangle workArea = Screen.PrimaryScreen.WorkingArea;
            Location = new Point((workArea.Width - Width) / 2, (int)Math.Round(36 * _dpiScale));
            SaveGeometry();
            if (notify) _settings.NotifyChanged();
            Invalidate();
        }

        public void SaveGeometry()
        {
            _settings.DeltaX = Left;
            _settings.DeltaY = Top;
            _settings.DeltaW = Width;
            _settings.DeltaH = Height;
        }

        protected override void OnMove(EventArgs e)
        {
            base.OnMove(e);
            if (IsHandleCreated) SaveGeometry();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (IsHandleCreated) SaveGeometry();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            _dpiScale = DpiHelper.GetScale(Handle);
            ApplySettingsState();
        }

        public void ApplySettingsState()
        {
            if (!IsHandleCreated) return;
            Opacity = _settings.DeltaOpacity;
            int exStyle = GetWindowLong(Handle, GWL_EXSTYLE);
            if (_settings.Locked)
            {
                SetWindowLong(Handle, GWL_EXSTYLE, exStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOPMOST);
                _isHovered = false;
            }
            else
            {
                SetWindowLong(Handle, GWL_EXSTYLE, (exStyle | WS_EX_LAYERED | WS_EX_TOPMOST) & ~WS_EX_TRANSPARENT);
            }
            Invalidate();
        }

        public void UpdateSnapshot(SessionSnapshot snap)
        {
            _snapshot = snap;
            if (!_settings.Locked && _activeDragCorner == DragCorner.None)
            {
                bool inside = Bounds.Contains(Cursor.Position);
                if (inside != _isHovered) _isHovered = inside;
            }
            Invalidate();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_DPICHANGED)
            {
                int newDpi = m.WParam.ToInt32() & 0xFFFF;
                if (newDpi >= 72 && newDpi <= 480)
                {
                    float nextDpi = newDpi / 96.0f;
                    float ratio = nextDpi / _dpiScale;
                    _dpiScale = nextDpi;
                    Width = (int)Math.Round(Width * ratio);
                    Height = (int)Math.Round(Height * ratio);
                    Invalidate();
                }
            }
            base.WndProc(ref m);
        }

        private int GetMinWidth()
        {
            return Math.Max((int)Math.Round(300 * _dpiScale), (int)Math.Round(340 * EffectiveScale));
        }

        private int GetMinHeight()
        {
            int baseH = _settings.ShowSectorSplits ? 108 : 78;
            return Math.Max((int)Math.Round(70 * _dpiScale), (int)Math.Round(baseH * EffectiveScale));
        }

        private DragCorner HitTestCorner(Point pt)
        {
            int grip = Math.Max(16, (int)Math.Round(20 * EffectiveScale));
            bool left = pt.X <= grip;
            bool right = pt.X >= Width - grip;
            bool top = pt.Y <= grip;
            bool bottom = pt.Y >= Height - grip;

            if (top && right)
            {
                if (_rectCornerModeBtn.Contains(pt)) return DragCorner.None;
                int edge = Math.Max(8, (int)Math.Round(10 * EffectiveScale));
                if (pt.X >= Width - edge && pt.Y <= edge) return DragCorner.TopRight;
                return DragCorner.None;
            }
            if (bottom && right) return DragCorner.BottomRight;
            if (bottom && left) return DragCorner.BottomLeft;
            if (top && left) return DragCorner.TopLeft;
            return DragCorner.None;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (_settings.Locked || e.Button != MouseButtons.Left) return;

            Point pt = e.Location;
            if (_isHovered)
            {
                if (_rectCornerModeBtn.Contains(pt))
                {
                    _cornerMode = (_cornerMode == CornerDragMode.ResizeWindow)
                        ? CornerDragMode.ChangeFontSize
                        : CornerDragMode.ResizeWindow;
                    Invalidate();
                    return;
                }
                if (_rectRefCycleBtn.Contains(pt))
                {
                    _settings.DeltaRefMode = (DeltaReferenceMode)(((int)_settings.DeltaRefMode + 1) % 4);
                    _settings.NotifyChanged();
                    Invalidate();
                    return;
                }
            }

            DragCorner corner = HitTestCorner(pt);
            if (corner != DragCorner.None)
            {
                _activeDragCorner = corner;
                _dragStartScreenPt = Cursor.Position;
                _dragStartBounds = Bounds;
                _dragStartFontScale = _settings.DeltaFontScale;
                Capture = true;
                return;
            }

            ReleaseCapture();
            SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            SaveGeometry();
            _settings.Save();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_settings.Locked) return;

            if (!_isHovered)
            {
                _isHovered = true;
                Invalidate();
            }

            if (_activeDragCorner != DragCorner.None)
            {
                Point curScreen = Cursor.Position;
                int dx = curScreen.X - _dragStartScreenPt.X;
                int dy = curScreen.Y - _dragStartScreenPt.Y;

                if (_cornerMode == CornerDragMode.ChangeFontSize)
                {
                    int outward = 0;
                    if (_activeDragCorner == DragCorner.BottomRight) outward = dx + dy;
                    else if (_activeDragCorner == DragCorner.BottomLeft) outward = -dx + dy;
                    else if (_activeDragCorner == DragCorner.TopRight) outward = dx - dy;
                    else if (_activeDragCorner == DragCorner.TopLeft) outward = -dx - dy;

                    float sens = 0.0022f / Math.Max(1.0f, _dpiScale * 0.75f);
                    float nextScale = Math.Max(0.65f, Math.Min(1.75f, _dragStartFontScale + outward * sens));
                    _settings.DeltaFontScale = nextScale;

                    float ratio = nextScale / _dragStartFontScale;
                    int newW = Math.Max(GetMinWidth(), (int)Math.Round(_dragStartBounds.Width * ratio));
                    int newH = Math.Max(GetMinHeight(), (int)Math.Round(_dragStartBounds.Height * ratio));
                    Size = new Size(newW, newH);
                    SaveGeometry();
                    Invalidate();
                }
                else
                {
                    int minW = GetMinWidth();
                    int minH = GetMinHeight();
                    int newX = _dragStartBounds.X;
                    int newY = _dragStartBounds.Y;
                    int newW = _dragStartBounds.Width;
                    int newH = _dragStartBounds.Height;

                    if (_activeDragCorner == DragCorner.BottomRight || _activeDragCorner == DragCorner.TopRight)
                        newW = Math.Max(minW, _dragStartBounds.Width + dx);
                    else if (_activeDragCorner == DragCorner.BottomLeft || _activeDragCorner == DragCorner.TopLeft)
                    {
                        newW = Math.Max(minW, _dragStartBounds.Width - dx);
                        newX = _dragStartBounds.Right - newW;
                    }

                    if (_activeDragCorner == DragCorner.BottomRight || _activeDragCorner == DragCorner.BottomLeft)
                        newH = Math.Max(minH, _dragStartBounds.Height + dy);
                    else if (_activeDragCorner == DragCorner.TopRight || _activeDragCorner == DragCorner.TopLeft)
                    {
                        newH = Math.Max(minH, _dragStartBounds.Height - dy);
                        newY = _dragStartBounds.Bottom - newH;
                    }

                    Bounds = new Rectangle(newX, newY, newW, newH);
                    SaveGeometry();
                    Invalidate();
                }
                return;
            }

            DragCorner hc = HitTestCorner(e.Location);
            if (hc == DragCorner.TopLeft || hc == DragCorner.BottomRight) Cursor = Cursors.SizeNWSE;
            else if (hc == DragCorner.TopRight || hc == DragCorner.BottomLeft) Cursor = Cursors.SizeNESW;
            else if (_rectCornerModeBtn.Contains(e.Location) || _rectRefCycleBtn.Contains(e.Location)) Cursor = Cursors.Hand;
            else Cursor = Cursors.SizeAll;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_activeDragCorner != DragCorner.None)
            {
                _activeDragCorner = DragCorner.None;
                Capture = false;
                SaveGeometry();
                _settings.NotifyChanged();
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            float s = EffectiveScale;
            DeltaTelemetryData dt = (_snapshot != null && _snapshot.Delta != null) ? _snapshot.Delta : new DeltaTelemetryData();

            float deltaSec = dt.DeltaSessionBestSec;
            float deltaRate = dt.DeltaSessionBestRate;
            float refLapSec = dt.SessionBestLapSec;
            string refTitle = "SESSION BEST";

            if (_settings.DeltaRefMode == DeltaReferenceMode.SessionOptimal)
            {
                deltaSec = dt.DeltaSessionOptimalSec;
                deltaRate = dt.DeltaSessionOptimalRate;
                refLapSec = dt.SessionOptimalLapSec;
                refTitle = "OPTIMAL LAP";
            }
            else if (_settings.DeltaRefMode == DeltaReferenceMode.PersonalBest)
            {
                deltaSec = dt.DeltaPersonalBestSec;
                deltaRate = dt.DeltaPersonalBestRate;
                refLapSec = dt.PersonalBestLapSec;
                refTitle = "PERSONAL BEST";
            }
            else if (_settings.DeltaRefMode == DeltaReferenceMode.LastLap)
            {
                deltaSec = dt.DeltaLastLapSec;
                deltaRate = dt.DeltaLastLapRate;
                refLapSec = dt.LastLapSec;
                refTitle = "LAST LAP";
            }

            float maxRange = Math.Max(0.5f, _settings.DeltaBarRangeSec);
            float clamped = Math.Max(-maxRange, Math.Min(maxRange, deltaSec));
            float normPct = Math.Abs(clamped) / maxRange; // 0.0 .. 1.0 of half-bar
            bool isFaster = deltaSec <= 0f;
            bool isPurple = deltaSec < -0.55f && _settings.DeltaRefMode == DeltaReferenceMode.SessionBest;

            Color fillCol = isPurple
                ? Color.FromArgb(192, 132, 252)
                : (isFaster ? Color.FromArgb(34, 197, 94) : Color.FromArgb(239, 68, 68));

            Color trendCol = deltaRate <= 0f
                ? Color.FromArgb(74, 222, 128)
                : Color.FromArgb(248, 113, 113);

            EnsureFonts(s);
            Font fLabel = _fLabel;
            Font fSubMono = _fSubMono;
            Font fBigDelta = _fBigDelta;
            Font fIconAa = _fIconAa;
            {
                StringFormat centerFmt = _centerFmt;
                StringFormat leftFmt = _leftFmt;
                StringFormat rightFmt = _rightFmt;

                int padX = (int)Math.Round(12 * s);
                int topY = (int)Math.Round(8 * s);
                int topRowH = (int)Math.Round(38 * s);

                // Left: Reference Mode + Ref Lap Time (clickable on hover to cycle ref mode)
                _rectRefCycleBtn = new Rectangle(padX, topY, (int)Math.Round(125 * s), topRowH);
                using (SolidBrush cyanB = new SolidBrush(Color.FromArgb(56, 189, 248)))
                {
                    g.DrawString(refTitle, fLabel, cyanB, new RectangleF(padX, topY, 150 * s, 16 * s), leftFmt);
                }
                using (SolidBrush refB = new SolidBrush(Color.FromArgb(209, 213, 219)))
                {
                    g.DrawString("REF " + DpiHelper.FormatLapTime(refLapSec), fSubMono, refB,
                        new RectangleF(padX, topY + 17 * s, 150 * s, 18 * s), leftFmt);
                }

                // Center: Big Delta Pill (colored by instantaneous _DD speed trend)
                int pillW = (int)Math.Round(132 * s);
                int pillH = (int)Math.Round(34 * s);
                Rectangle pillRect = new Rectangle((Width - pillW) / 2, topY + (topRowH - pillH) / 2, pillW, pillH);
                using (SolidBrush pBg = new SolidBrush(Color.FromArgb(20, 25, 36)))
                {
                    g.FillRectangle(pBg, pillRect);
                }
                using (Pen pPen = new Pen(trendCol, Math.Max(1.5f, 1.8f * s)))
                {
                    g.DrawRectangle(pPen, pillRect);
                }

                string sign = deltaSec > 0f ? "+" : "-";
                string deltaStr = string.Format(CultureInfo.InvariantCulture, "{0}{1:0.00}s", sign, Math.Abs(deltaSec));
                using (SolidBrush tb = new SolidBrush(trendCol))
                {
                    g.DrawString(deltaStr, fBigDelta, tb, pillRect, centerFmt);
                }

                // Right: Predicted Lap Time
                int rightBlockW = (int)Math.Round(145 * s);
                int rightOffset = (_isHovered && !_settings.Locked) ? (int)Math.Round(28 * s) : 0;
                int rightX = Width - padX - rightBlockW - rightOffset;
                float predLap = refLapSec > 0f ? (refLapSec + deltaSec) : 0f;

                using (SolidBrush lblB = new SolidBrush(Color.FromArgb(156, 163, 175)))
                {
                    g.DrawString(dt.OffTrack ? "OFF-TRACK (1x)" : "PREDICTED", fLabel,
                        dt.OffTrack ? Brushes.Orange : lblB,
                        new RectangleF(rightX, topY, rightBlockW, 16 * s), rightFmt);
                }
                using (SolidBrush predB = new SolidBrush(isFaster ? Color.FromArgb(74, 222, 128) : Color.FromArgb(243, 244, 246)))
                {
                    g.DrawString(DpiHelper.FormatLapTime(predLap), fSubMono, predB,
                        new RectangleF(rightX, topY + 17 * s, rightBlockW, 18 * s), rightFmt);
                }

                // Middle: Bi-Directional Center-Zero Progress Bar
                int barY = topY + topRowH + (int)Math.Round(6 * s);
                int availH = Height - barY - (int)Math.Round(10 * s);
                int barH = _settings.ShowSectorSplits
                    ? Math.Max((int)Math.Round(16 * s), availH - (int)Math.Round(30 * s))
                    : Math.Max((int)Math.Round(18 * s), availH);

                Rectangle barRect = new Rectangle(padX, barY, Width - padX * 2, barH);
                using (SolidBrush barBg = new SolidBrush(Color.FromArgb(24, 30, 42)))
                {
                    g.FillRectangle(barBg, barRect);
                }

                int centerX = barRect.Left + barRect.Width / 2;
                int halfW = barRect.Width / 2;
                int fillW = (int)Math.Round(halfW * normPct);
                if (fillW > 1)
                {
                    Rectangle fillRect = isFaster
                        ? new Rectangle(centerX - fillW, barRect.Top + 2, fillW, Math.Max(2, barRect.Height - 4))
                        : new Rectangle(centerX, barRect.Top + 2, fillW, Math.Max(2, barRect.Height - 4));
                    using (SolidBrush fb = new SolidBrush(fillCol))
                    {
                        g.FillRectangle(fb, fillRect);
                    }
                }

                // Quarter tick lines & Center zero line
                using (Pen tickPen = new Pen(Color.FromArgb(45, 255, 255, 255), Math.Max(1f, 1.0f * s)))
                {
                    g.DrawLine(tickPen, barRect.Left + barRect.Width / 4, barRect.Top, barRect.Left + barRect.Width / 4, barRect.Bottom);
                    g.DrawLine(tickPen, barRect.Left + (barRect.Width * 3) / 4, barRect.Top, barRect.Left + (barRect.Width * 3) / 4, barRect.Bottom);
                }
                using (Pen zeroPen = new Pen(Color.White, Math.Max(2f, 2.2f * s)))
                {
                    g.DrawLine(zeroPen, centerX, barRect.Top, centerX, barRect.Bottom);
                }
                using (Pen barBorder = new Pen(Color.FromArgb(55, 255, 255, 255), Math.Max(1f, 1.0f * s)))
                {
                    g.DrawRectangle(barBorder, barRect);
                }

                // Bottom: Official SDK Sector Splits from SplitTimeInfo (S1 .. Sn)
                if (_settings.ShowSectorSplits)
                {
                    float[] sVals = dt.GetSectorDeltas(_settings.DeltaRefMode);
                    int totalSectors = (sVals != null && sVals.Length > 0) ? sVals.Length : 3;

                    // Show up to 5 sectors at once; if a long track (e.g. Nordschleife) has >5 sectors, center around ActiveSectorIndex
                    int maxVis = Math.Min(totalSectors, 5);
                    int startSec = 0;
                    if (totalSectors > maxVis)
                    {
                        startSec = dt.ActiveSectorIndex - (maxVis / 2);
                        if (startSec < 0) startSec = 0;
                        if (startSec + maxVis > totalSectors) startSec = totalSectors - maxVis;
                    }

                    int secY = barRect.Bottom + (int)Math.Round(6 * s);
                    int secH = Math.Max((int)Math.Round(20 * s), Height - secY - (int)Math.Round(6 * s));
                    int gap = (int)Math.Round(6 * s);
                    int secW = (barRect.Width - gap * (maxVis - 1)) / maxVis;

                    for (int visIdx = 0; visIdx < maxVis; visIdx++)
                    {
                        int secIdx = startSec + visIdx;
                        int sx = barRect.Left + visIdx * (secW + gap);
                        int w = (visIdx == maxVis - 1) ? (barRect.Right - sx) : secW;
                        Rectangle sRect = new Rectangle(sx, secY, w, secH);

                        bool isAct = (dt.ActiveSectorIndex == secIdx);
                        float sv = (sVals != null && secIdx < sVals.Length) ? sVals[secIdx] : 0f;
                        bool fasterSec = sv <= 0f;

                        Color sBg = isAct
                            ? Color.FromArgb(35, 56, 189, 248)
                            : (fasterSec ? Color.FromArgb(38, 34, 197, 94) : Color.FromArgb(38, 239, 68, 68));
                        Color sBorder = isAct
                            ? Color.FromArgb(140, 56, 189, 248)
                            : (fasterSec ? Color.FromArgb(120, 74, 222, 128) : Color.FromArgb(120, 248, 113, 113));
                        Color sText = fasterSec ? Color.FromArgb(134, 239, 172) : Color.FromArgb(252, 165, 165);

                        using (SolidBrush sb = new SolidBrush(sBg)) g.FillRectangle(sb, sRect);
                        using (Pen sp = new Pen(sBorder, Math.Max(1f, 1.0f * s))) g.DrawRectangle(sp, sRect);

                        string sName = (maxVis <= 3 && isAct) ? ("S" + (secIdx + 1) + " LIVE") : ("S" + (secIdx + 1));
                        using (SolidBrush lb = new SolidBrush(isAct ? Color.FromArgb(125, 211, 252) : Color.FromArgb(156, 163, 175)))
                        {
                            g.DrawString(sName, fLabel, lb, new RectangleF(sRect.Left + 6 * s, sRect.Top, sRect.Width * 0.55f, sRect.Height), leftFmt);
                        }

                        string sValStr = string.Format(CultureInfo.InvariantCulture, "{0}{1:0.00}s", sv > 0f ? "+" : "-", Math.Abs(sv));
                        using (SolidBrush vb = new SolidBrush(sText))
                        {
                            g.DrawString(sValStr, fSubMono, vb, new RectangleF(sRect.Left, sRect.Top, sRect.Width - 6 * s, sRect.Height), rightFmt);
                        }
                    }
                }

                // SimLab-style Single Top-Right Icon Toggle Button + Corner Grips on Hover
                if (_isHovered && !_settings.Locked)
                {
                    int iconSz = Math.Max(20, (int)Math.Round(24 * s));
                    _rectCornerModeBtn = new Rectangle(
                        Width - iconSz - (int)Math.Round(6 * s),
                        (int)Math.Round(6 * s),
                        iconSz,
                        iconSz
                    );
                    DpiHelper.DrawCornerModeIconBtn(
                        g,
                        fIconAa,
                        _rectCornerModeBtn,
                        _cornerMode == CornerDragMode.ResizeWindow,
                        s
                    );

                    Color activeAccent = _cornerMode == CornerDragMode.ResizeWindow
                        ? Color.FromArgb(215, 56, 189, 248)
                        : Color.FromArgb(215, 192, 132, 252);

                    using (Pen bp = new Pen(activeAccent, Math.Max(1.5f, 1.5f * s)))
                    {
                        g.DrawRectangle(bp, 1, 1, Width - 2, Height - 2);
                    }
                    DpiHelper.DrawCornerGrips(g, Width, Height, activeAccent, s);
                }
                else
                {
                    _rectCornerModeBtn = Rectangle.Empty;
                    Color borderCol = _settings.Locked ? Color.FromArgb(45, 255, 255, 255) : Color.FromArgb(80, 255, 255, 255);
                    using (Pen bp = new Pen(borderCol, 1.0f))
                    {
                        g.DrawRectangle(bp, 0, 0, Width - 1, Height - 1);
                    }
                }
            }
        }
    }

    public class OverlayForm : Form
    {
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int WS_EX_TOPMOST = 0x00000008;
        private const int WM_DPICHANGED = 0x02E0;
        private const int WM_NCLBUTTONDOWN = 0x00A1;
        private const int HT_CAPTION = 0x02;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private readonly OverlaySettings _settings;
        private SessionSnapshot _snapshot;
        private float _dpiScale = 1.0f;

        private bool _isHovered = false;
        private CornerDragMode _cornerMode = CornerDragMode.ResizeWindow;
        private DragCorner _activeDragCorner = DragCorner.None;
        private Point _dragStartScreenPt;
        private Rectangle _dragStartBounds;
        private float _dragStartFontScale = 1.0f;

        private Rectangle _rectCornerModeBtn;
        private Rectangle _rectModeCycleBtn;
        private Rectangle _rectGapBtn;
        private Rectangle _rectCarNumBtn;
        private Rectangle _rectLockBtn;

        private float _cachedFontScale = -1f;
        private Font _fBrand;
        private Font _fSmallBold;
        private Font _fIconAa;
        private Font _fHeader;
        private Font _fRow;
        private Font _fRowBold;
        private Font _fMono;
        private readonly StringFormat _centerFmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        private readonly StringFormat _rightFmt = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
        private readonly StringFormat _leftFmt = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
        private readonly StringFormat _trackFmt = new StringFormat
        {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap
        };

        private void EnsureFonts(float s)
        {
            if (Math.Abs(_cachedFontScale - s) < 0.001f && _fBrand != null) return;
            if (_fBrand != null) _fBrand.Dispose();
            if (_fSmallBold != null) _fSmallBold.Dispose();
            if (_fIconAa != null) _fIconAa.Dispose();
            if (_fHeader != null) _fHeader.Dispose();
            if (_fRow != null) _fRow.Dispose();
            if (_fRowBold != null) _fRowBold.Dispose();
            if (_fMono != null) _fMono.Dispose();

            _fBrand = new Font("Segoe UI", 11.5f * s, FontStyle.Bold, GraphicsUnit.Pixel);
            _fSmallBold = new Font("Segoe UI", 10.2f * s, FontStyle.Bold, GraphicsUnit.Pixel);
            _fIconAa = new Font("Segoe UI", 11.5f * s, FontStyle.Bold, GraphicsUnit.Pixel);
            _fHeader = new Font("Segoe UI", 11.5f * s, FontStyle.Bold, GraphicsUnit.Pixel);
            _fRow = new Font("Segoe UI", 12.2f * s, FontStyle.Regular, GraphicsUnit.Pixel);
            _fRowBold = new Font("Segoe UI", 12.2f * s, FontStyle.Bold, GraphicsUnit.Pixel);
            _fMono = new Font("Consolas", 12.6f * s, FontStyle.Bold, GraphicsUnit.Pixel);
            _cachedFontScale = s;
        }

        public OverlayForm(OverlaySettings settings, SessionSnapshot initialSnapshot)
        {
            _settings = settings;
            _snapshot = initialSnapshot;

            Text = "Free Overlay - Standings";
            AutoScaleMode = AutoScaleMode.None;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            DoubleBuffered = true;
            BackColor = Color.FromArgb(13, 16, 23);
            Opacity = _settings.StandingsOpacity;

            _dpiScale = DpiHelper.GetScale(IntPtr.Zero);
            int savedX = _settings.StandingsX;
            int savedY = _settings.StandingsY;
            int savedW = _settings.StandingsW;
            int savedH = _settings.StandingsH;

            ResetPositionAndSize(false);

            if (savedW > 200 && savedH > 120)
            {
                Width = savedW;
                Height = savedH;
            }
            if (savedX != -1 || savedY != -1)
            {
                Location = new Point(savedX, savedY);
            }
            SaveGeometry();
        }

        private float EffectiveScale
        {
            get { return _dpiScale * _settings.StandingsFontScale; }
        }

        public void ResetPositionAndSize(bool notify)
        {
            float s = EffectiveScale;
            Width = (int)Math.Round(640 * s);
            Height = (int)Math.Round(534 * s);
            Rectangle workArea = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(Math.Max(24, workArea.Width - Width - (int)(36 * _dpiScale)), (int)(48 * _dpiScale));
            SaveGeometry();
            if (notify) _settings.NotifyChanged();
            Invalidate();
        }

        public void SaveGeometry()
        {
            _settings.StandingsX = Left;
            _settings.StandingsY = Top;
            _settings.StandingsW = Width;
            _settings.StandingsH = Height;
        }

        protected override void OnMove(EventArgs e)
        {
            base.OnMove(e);
            if (IsHandleCreated) SaveGeometry();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (IsHandleCreated) SaveGeometry();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            _dpiScale = DpiHelper.GetScale(Handle);
            ApplySettingsState();
        }

        public void ApplySettingsState()
        {
            if (!IsHandleCreated) return;
            Opacity = _settings.StandingsOpacity;
            int exStyle = GetWindowLong(Handle, GWL_EXSTYLE);
            if (_settings.Locked)
            {
                SetWindowLong(Handle, GWL_EXSTYLE, exStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOPMOST);
                _isHovered = false;
            }
            else
            {
                SetWindowLong(Handle, GWL_EXSTYLE, (exStyle | WS_EX_LAYERED | WS_EX_TOPMOST) & ~WS_EX_TRANSPARENT);
            }
            Invalidate();
        }

        public void UpdateSnapshot(SessionSnapshot snap)
        {
            _snapshot = snap;
            if (!_settings.Locked && _activeDragCorner == DragCorner.None)
            {
                bool inside = Bounds.Contains(Cursor.Position);
                if (inside != _isHovered) _isHovered = inside;
            }
            Invalidate();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_DPICHANGED)
            {
                int newDpi = m.WParam.ToInt32() & 0xFFFF;
                if (newDpi >= 72 && newDpi <= 480)
                {
                    float nextDpiScale = newDpi / 96.0f;
                    float ratio = nextDpiScale / _dpiScale;
                    _dpiScale = nextDpiScale;
                    Width = (int)Math.Round(Width * ratio);
                    Height = (int)Math.Round(Height * ratio);
                    Invalidate();
                }
            }
            base.WndProc(ref m);
        }

        private DragCorner HitTestCorner(Point pt)
        {
            int grip = Math.Max(18, (int)Math.Round(22 * EffectiveScale));
            bool left = pt.X <= grip;
            bool right = pt.X >= Width - grip;
            bool top = pt.Y <= grip;
            bool bottom = pt.Y >= Height - grip;

            if (top && right)
            {
                if (_rectCornerModeBtn.Contains(pt)) return DragCorner.None;
                int topEdge = Math.Max(10, (int)Math.Round(12 * EffectiveScale));
                if (pt.X >= Width - topEdge && pt.Y <= topEdge)
                    return DragCorner.TopRight;
                return DragCorner.None;
            }
            if (bottom && right) return DragCorner.BottomRight;
            if (bottom && left) return DragCorner.BottomLeft;
            if (top && left) return DragCorner.TopLeft;
            return DragCorner.None;
        }

        private int GetMinWindowWidth()
        {
            return Math.Max((int)Math.Round(380 * _dpiScale), (int)Math.Round(470 * EffectiveScale));
        }

        private int GetMinWindowHeight()
        {
            return Math.Max((int)Math.Round(160 * _dpiScale), (int)Math.Round(190 * EffectiveScale));
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (_settings.Locked || e.Button != MouseButtons.Left) return;

            Point pt = e.Location;

            if (_isHovered)
            {
                if (_rectCornerModeBtn.Contains(pt))
                {
                    _cornerMode = (_cornerMode == CornerDragMode.ResizeWindow)
                        ? CornerDragMode.ChangeFontSize
                        : CornerDragMode.ResizeWindow;
                    Invalidate();
                    return;
                }
                if (_rectLockBtn.Contains(pt))
                {
                    _settings.Locked = !_settings.Locked;
                    _settings.NotifyChanged();
                    return;
                }
                if (_rectModeCycleBtn.Contains(pt))
                {
                    _settings.Mode = (TelemetryMode)(((int)_settings.Mode + 1) % 3);
                    _settings.NotifyChanged();
                    return;
                }
                if (_rectGapBtn.Contains(pt))
                {
                    _settings.ShowInterval = !_settings.ShowInterval;
                    _settings.NotifyChanged();
                    return;
                }
                if (_rectCarNumBtn.Contains(pt))
                {
                    _settings.ShowCarNumber = !_settings.ShowCarNumber;
                    _settings.NotifyChanged();
                    return;
                }
            }

            DragCorner corner = HitTestCorner(pt);
            if (corner != DragCorner.None)
            {
                _activeDragCorner = corner;
                _dragStartScreenPt = Cursor.Position;
                _dragStartBounds = Bounds;
                _dragStartFontScale = _settings.StandingsFontScale;
                Capture = true;
                return;
            }

            int headerDragH = (int)Math.Round(66 * EffectiveScale);
            if (pt.Y < headerDragH)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                SaveGeometry();
                _settings.Save();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_settings.Locked) return;

            if (!_isHovered)
            {
                _isHovered = true;
                Invalidate();
            }

            if (_activeDragCorner != DragCorner.None)
            {
                Point curScreen = Cursor.Position;
                int dx = curScreen.X - _dragStartScreenPt.X;
                int dy = curScreen.Y - _dragStartScreenPt.Y;

                if (_cornerMode == CornerDragMode.ChangeFontSize)
                {
                    int outwardDelta = 0;
                    if (_activeDragCorner == DragCorner.BottomRight) outwardDelta = dx + dy;
                    else if (_activeDragCorner == DragCorner.BottomLeft) outwardDelta = -dx + dy;
                    else if (_activeDragCorner == DragCorner.TopRight) outwardDelta = dx - dy;
                    else if (_activeDragCorner == DragCorner.TopLeft) outwardDelta = -dx - dy;

                    float sensitivity = 0.0022f / Math.Max(1.0f, _dpiScale * 0.75f);
                    float nextScale = Math.Max(0.65f, Math.Min(1.75f, _dragStartFontScale + outwardDelta * sensitivity));
                    _settings.StandingsFontScale = nextScale;

                    float ratio = _settings.StandingsFontScale / _dragStartFontScale;
                    int newW = Math.Max(GetMinWindowWidth(), (int)Math.Round(_dragStartBounds.Width * ratio));
                    int newH = Math.Max(GetMinWindowHeight(), (int)Math.Round(_dragStartBounds.Height * ratio));
                    Size = new Size(newW, newH);
                    SaveGeometry();
                    Invalidate();
                }
                else
                {
                    int minW = GetMinWindowWidth();
                    int minH = GetMinWindowHeight();
                    int newX = _dragStartBounds.X;
                    int newY = _dragStartBounds.Y;
                    int newW = _dragStartBounds.Width;
                    int newH = _dragStartBounds.Height;

                    if (_activeDragCorner == DragCorner.BottomRight || _activeDragCorner == DragCorner.TopRight)
                        newW = Math.Max(minW, _dragStartBounds.Width + dx);
                    else if (_activeDragCorner == DragCorner.BottomLeft || _activeDragCorner == DragCorner.TopLeft)
                    {
                        newW = Math.Max(minW, _dragStartBounds.Width - dx);
                        newX = _dragStartBounds.Right - newW;
                    }

                    if (_activeDragCorner == DragCorner.BottomRight || _activeDragCorner == DragCorner.BottomLeft)
                        newH = Math.Max(minH, _dragStartBounds.Height + dy);
                    else if (_activeDragCorner == DragCorner.TopRight || _activeDragCorner == DragCorner.TopLeft)
                    {
                        newH = Math.Max(minH, _dragStartBounds.Height - dy);
                        newY = _dragStartBounds.Bottom - newH;
                    }

                    Bounds = new Rectangle(newX, newY, newW, newH);
                    SaveGeometry();
                    Invalidate();
                }
                return;
            }

            DragCorner hoverCorner = HitTestCorner(e.Location);
            if (hoverCorner == DragCorner.TopLeft || hoverCorner == DragCorner.BottomRight)
                Cursor = Cursors.SizeNWSE;
            else if (hoverCorner == DragCorner.TopRight || hoverCorner == DragCorner.BottomLeft)
                Cursor = Cursors.SizeNESW;
            else if (_rectCornerModeBtn.Contains(e.Location) || _rectLockBtn.Contains(e.Location) ||
                     _rectModeCycleBtn.Contains(e.Location) || _rectGapBtn.Contains(e.Location) ||
                     _rectCarNumBtn.Contains(e.Location))
                Cursor = Cursors.Hand;
            else
                Cursor = Cursors.Default;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_activeDragCorner != DragCorner.None)
            {
                _activeDragCorner = DragCorner.None;
                Capture = false;
                SaveGeometry();
                _settings.NotifyChanged();
                Invalidate();
            }
        }

        private static string FormatTimer(double seconds)
        {
            if (seconds <= 0) return "00:00";
            int mins = (int)(seconds / 60.0);
            int secs = (int)(seconds % 60.0);
            return string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}", mins, secs);
        }

        private static Color GetLicenseBg(string lic)
        {
            switch (lic)
            {
                case "A": return Color.FromArgb(1, 83, 219);
                case "B": return Color.FromArgb(46, 125, 50);
                case "C": return Color.FromArgb(249, 168, 37);
                case "D": return Color.FromArgb(239, 108, 0);
                case "P": return Color.FromArgb(30, 34, 45);
                default: return Color.FromArgb(211, 47, 47);
            }
        }

        public static void DrawCountryFlag(Graphics g, int x, int y, int w, int h, string code, float s)
        {
            Rectangle r = new Rectangle(x, y, w, h);
            using (SolidBrush bg = new SolidBrush(Color.FromArgb(30, 41, 59)))
            {
                g.FillRectangle(bg, r);
            }

            if (code == "NL")
            {
                g.FillRectangle(new SolidBrush(Color.FromArgb(174, 28, 40)), x, y, w, h / 3);
                g.FillRectangle(Brushes.White, x, y + h / 3, w, h / 3);
                g.FillRectangle(new SolidBrush(Color.FromArgb(33, 70, 139)), x, y + 2 * (h / 3), w, h - 2 * (h / 3));
            }
            else if (code == "DE")
            {
                g.FillRectangle(Brushes.Black, x, y, w, h / 3);
                g.FillRectangle(new SolidBrush(Color.FromArgb(221, 0, 0)), x, y + h / 3, w, h / 3);
                g.FillRectangle(new SolidBrush(Color.FromArgb(255, 206, 0)), x, y + 2 * (h / 3), w, h - 2 * (h / 3));
            }
            else if (code == "FR")
            {
                g.FillRectangle(new SolidBrush(Color.FromArgb(0, 35, 149)), x, y, w / 3, h);
                g.FillRectangle(Brushes.White, x + w / 3, y, w / 3, h);
                g.FillRectangle(new SolidBrush(Color.FromArgb(237, 41, 57)), x + 2 * (w / 3), y, w - 2 * (w / 3), h);
            }
            else if (code == "IT")
            {
                g.FillRectangle(new SolidBrush(Color.FromArgb(0, 146, 70)), x, y, w / 3, h);
                g.FillRectangle(Brushes.White, x + w / 3, y, w / 3, h);
                g.FillRectangle(new SolidBrush(Color.FromArgb(206, 43, 55)), x + 2 * (w / 3), y, w - 2 * (w / 3), h);
            }
            else if (code == "BE")
            {
                g.FillRectangle(Brushes.Black, x, y, w / 3, h);
                g.FillRectangle(new SolidBrush(Color.FromArgb(253, 218, 36)), x + w / 3, y, w / 3, h);
                g.FillRectangle(new SolidBrush(Color.FromArgb(239, 51, 64)), x + 2 * (w / 3), y, w - 2 * (w / 3), h);
            }
            else if (code == "FI")
            {
                int crossW = Math.Max(2, h / 3);
                g.FillRectangle(Brushes.White, r);
                using (SolidBrush b = new SolidBrush(Color.FromArgb(0, 53, 128)))
                {
                    g.FillRectangle(b, x, y + (h - crossW) / 2, w, crossW);
                    g.FillRectangle(b, x + w / 4, y, crossW, h);
                }
            }
            else if (code == "SE")
            {
                int crossW = Math.Max(2, h / 3);
                g.FillRectangle(new SolidBrush(Color.FromArgb(0, 106, 167)), r);
                using (SolidBrush b = new SolidBrush(Color.FromArgb(254, 204, 2)))
                {
                    g.FillRectangle(b, x, y + (h - crossW) / 2, w, crossW);
                    g.FillRectangle(b, x + w / 4, y, crossW, h);
                }
            }
            else if (code == "GB" || code == "AU" || code == "NZ")
            {
                int cw = Math.Max(2, h / 3);
                g.FillRectangle(new SolidBrush(Color.FromArgb(1, 33, 105)), r);
                g.FillRectangle(Brushes.White, x, y + (h - cw) / 2, w, cw);
                g.FillRectangle(Brushes.White, x + (w - cw) / 2, y, cw, h);
                using (SolidBrush red = new SolidBrush(Color.FromArgb(200, 16, 46)))
                {
                    int rw = Math.Max(1, cw / 2);
                    g.FillRectangle(red, x, y + (h - rw) / 2, w, rw);
                    g.FillRectangle(red, x + (w - rw) / 2, y, rw, h);
                }
            }
            else if (code == "BR")
            {
                g.FillRectangle(new SolidBrush(Color.FromArgb(0, 151, 57)), r);
                Point[] diamond = new Point[]
                {
                    new Point(x + w / 2, y + 2),
                    new Point(x + w - 2, y + h / 2),
                    new Point(x + w / 2, y + h - 2),
                    new Point(x + 2, y + h / 2)
                };
                g.FillPolygon(new SolidBrush(Color.FromArgb(254, 221, 0)), diamond);
                int rad = Math.Max(2, h / 4);
                g.FillEllipse(new SolidBrush(Color.FromArgb(1, 33, 105)), x + w / 2 - rad, y + h / 2 - rad, rad * 2, rad * 2);
            }
            else if (code == "ES")
            {
                g.FillRectangle(new SolidBrush(Color.FromArgb(170, 21, 27)), r);
                g.FillRectangle(new SolidBrush(Color.FromArgb(241, 191, 0)), x, y + h / 4, w, h / 2);
            }
            else if (code == "JP")
            {
                g.FillRectangle(Brushes.White, r);
                int rad = Math.Max(3, h / 3);
                g.FillEllipse(new SolidBrush(Color.FromArgb(188, 0, 45)), x + w / 2 - rad, y + h / 2 - rad, rad * 2, rad * 2);
            }
            else if (code == "CA")
            {
                g.FillRectangle(Brushes.Red, r);
                g.FillRectangle(Brushes.White, x + w / 4, y, w / 2, h);
                int rad = Math.Max(2, h / 4);
                g.FillEllipse(Brushes.Red, x + w / 2 - rad, y + h / 2 - rad, rad * 2, rad * 2);
            }
            else if (code == "PL")
            {
                g.FillRectangle(Brushes.White, x, y, w, h / 2);
                g.FillRectangle(new SolidBrush(Color.FromArgb(220, 20, 60)), x, y + h / 2, w, h - h / 2);
            }
            else
            {
                g.FillRectangle(Brushes.White, r);
                int stripeH = Math.Max(1, h / 6);
                using (SolidBrush red = new SolidBrush(Color.FromArgb(178, 34, 52)))
                {
                    for (int sy = 0; sy < h; sy += stripeH * 2)
                        g.FillRectangle(red, x, y + sy, w, stripeH);
                }
                g.FillRectangle(new SolidBrush(Color.FromArgb(60, 59, 110)), x, y, w / 2, h / 2 + 1);
            }

            using (Pen borderPen = new Pen(Color.FromArgb(80, 255, 255, 255), Math.Max(1f, 0.8f * s)))
            {
                g.DrawRectangle(borderPen, r);
            }
        }

        private static Rectangle DrawPillButton(Graphics g, Font font, string text, int rightX, int y, int h, float s, Color bg, Color fg, Color border)
        {
            SizeF sz = g.MeasureString(text, font);
            int padX = (int)Math.Round(10 * s);
            int w = (int)Math.Ceiling(sz.Width) + padX;
            int x = rightX - w;
            Rectangle rect = new Rectangle(x, y, w, h);
            using (SolidBrush b = new SolidBrush(bg))
            {
                g.FillRectangle(b, rect);
            }
            using (Pen p = new Pen(border, Math.Max(1f, 1.1f * s)))
            {
                g.DrawRectangle(p, rect);
            }
            StringFormat sf = new StringFormat();
            sf.Alignment = StringAlignment.Center;
            sf.LineAlignment = StringAlignment.Center;
            using (SolidBrush tb = new SolidBrush(fg))
            {
                g.DrawString(text, font, tb, rect, sf);
            }
            return rect;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            float s = EffectiveScale;
            int toolbarH = (int)Math.Round(32 * s);
            int sessionH = (int)Math.Round(32 * s);
            int colHeaderH = (int)Math.Round(24 * s);
            int footerH = (int)Math.Round(26 * s);
            int rowH = (int)Math.Round(29 * s);

            EnsureFonts(s);
            Font fBrand = _fBrand;
            Font fSmallBold = _fSmallBold;
            Font fIconAa = _fIconAa;
            Font fHeader = _fHeader;
            Font fRow = _fRow;
            Font fRowBold = _fRowBold;
            Font fMono = _fMono;
            {
                StringFormat centerFmt = _centerFmt;
                StringFormat rightFmt = _rightFmt;
                StringFormat leftFmt = _leftFmt;

                using (SolidBrush tbBg = new SolidBrush(Color.FromArgb(9, 11, 17)))
                {
                    g.FillRectangle(tbBg, 0, 0, Width, toolbarH);
                }

                int dotSz = Math.Max(6, (int)Math.Round(8 * s));
                using (SolidBrush dotBrush = new SolidBrush(Color.FromArgb(239, 68, 68)))
                {
                    g.FillEllipse(dotBrush, (int)(10 * s), (toolbarH - dotSz) / 2, dotSz, dotSz);
                }

                int brandX = (int)(22 * s);
                g.DrawString("FREE OVERLAY", fBrand, Brushes.WhiteSmoke, new RectangleF(brandX, 0, 130 * s, toolbarH), leftFmt);

                bool isLive = _snapshot != null && _snapshot.IsLiveSource;
                bool isConn = _snapshot != null && _snapshot.Connected;
                string srcText = isLive ? (isConn ? "LIVE SDK" : "WAITING") : "SIMULATOR";
                Color srcBg = (isLive && isConn) ? Color.FromArgb(40, 34, 197, 94) : Color.FromArgb(45, 56, 189, 248);
                Color srcFg = (isLive && isConn) ? Color.FromArgb(74, 222, 128) : Color.FromArgb(56, 189, 248);
                int pillH = Math.Max(16, (int)Math.Round(19 * s));
                int pillY = (toolbarH - pillH) / 2;
                Rectangle srcRect = new Rectangle((int)(120 * s), pillY, (int)(78 * s), pillH);
                using (SolidBrush sb = new SolidBrush(srcBg)) g.FillRectangle(sb, srcRect);
                using (Pen sp = new Pen(srcFg, Math.Max(1f, 1.0f * s))) g.DrawRectangle(sp, srcRect);
                using (SolidBrush sfb = new SolidBrush(srcFg)) g.DrawString(srcText, fSmallBold, sfb, srcRect, centerFmt);

                if (_isHovered && !_settings.Locked)
                {
                    int curRight = Width - (int)Math.Round(8 * s);
                    int btnH = Math.Max(18, (int)Math.Round(22 * s));
                    int btnY = (toolbarH - btnH) / 2;

                    int iconBtnSz = Math.Max(20, (int)Math.Round(24 * s));
                    int iconBtnY = (toolbarH - iconBtnSz) / 2;
                    _rectCornerModeBtn = new Rectangle(curRight - iconBtnSz, iconBtnY, iconBtnSz, iconBtnSz);
                    DpiHelper.DrawCornerModeIconBtn(
                        g,
                        fIconAa,
                        _rectCornerModeBtn,
                        _cornerMode == CornerDragMode.ResizeWindow,
                        s
                    );
                    curRight = _rectCornerModeBtn.Left - (int)Math.Round(6 * s);

                    if (curRight > srcRect.Right + (int)(100 * s))
                    {
                        _rectLockBtn = DrawPillButton(g, fSmallBold, "LOCK (F6)", curRight, btnY, btnH, s,
                            Color.FromArgb(45, 245, 158, 11), Color.FromArgb(251, 191, 36), Color.FromArgb(120, 245, 158, 11));
                        curRight = _rectLockBtn.Left - (int)Math.Round(5 * s);
                    }
                    else _rectLockBtn = Rectangle.Empty;

                    if (curRight > srcRect.Right + (int)(75 * s))
                    {
                        _rectGapBtn = DrawPillButton(g, fSmallBold, _settings.ShowInterval ? "INT" : "GAP", curRight, btnY, btnH, s,
                            Color.FromArgb(28, 255, 255, 255), Color.LightGray, Color.FromArgb(60, 255, 255, 255));
                        curRight = _rectGapBtn.Left - (int)Math.Round(5 * s);
                    }
                    else _rectGapBtn = Rectangle.Empty;

                    if (curRight > srcRect.Right + (int)(70 * s))
                    {
                        _rectCarNumBtn = DrawPillButton(g, fSmallBold, _settings.ShowCarNumber ? "# ON" : "# OFF", curRight, btnY, btnH, s,
                            _settings.ShowCarNumber ? Color.FromArgb(40, 56, 189, 248) : Color.FromArgb(28, 255, 255, 255),
                            _settings.ShowCarNumber ? Color.FromArgb(125, 211, 252) : Color.LightGray,
                            _settings.ShowCarNumber ? Color.FromArgb(110, 56, 189, 248) : Color.FromArgb(60, 255, 255, 255));
                        curRight = _rectCarNumBtn.Left - (int)Math.Round(5 * s);
                    }
                    else _rectCarNumBtn = Rectangle.Empty;

                    if (curRight > srcRect.Right + (int)(70 * s))
                    {
                        string modeTxt = _settings.Mode == TelemetryMode.Auto ? "AUTO" : (_settings.Mode == TelemetryMode.Mock ? "MOCK" : "LIVE");
                        _rectModeCycleBtn = DrawPillButton(g, fSmallBold, modeTxt, curRight, btnY, btnH, s,
                            Color.FromArgb(28, 255, 255, 255), Color.LightGray, Color.FromArgb(60, 255, 255, 255));
                    }
                    else _rectModeCycleBtn = Rectangle.Empty;
                }
                else
                {
                    _rectCornerModeBtn = Rectangle.Empty;
                    _rectLockBtn = Rectangle.Empty;
                    _rectGapBtn = Rectangle.Empty;
                    _rectCarNumBtn = Rectangle.Empty;
                    _rectModeCycleBtn = Rectangle.Empty;

                    string hint = _settings.Locked ? "LOCKED (F6)" : "F5: HUB  •  F6: LOCK";
                    using (SolidBrush hb = new SolidBrush(Color.FromArgb(110, 156, 163, 175)))
                    {
                        g.DrawString(hint, fSmallBold, hb, new RectangleF(Width - 160 * s, 0, 150 * s, toolbarH), rightFmt);
                    }
                }

                int sessY = toolbarH;
                using (SolidBrush sbBg = new SolidBrush(Color.FromArgb(20, 25, 35)))
                {
                    g.FillRectangle(sbBg, 0, sessY, Width, sessionH);
                }

                int gt3W = (int)Math.Round(34 * s);
                int gt3H = Math.Max(16, (int)Math.Round(18 * s));
                Rectangle gt3Rect = new Rectangle((int)(10 * s), sessY + (sessionH - gt3H) / 2, gt3W, gt3H);
                using (SolidBrush gt3B = new SolidBrush(Color.FromArgb(255, 82, 82))) g.FillRectangle(gt3B, gt3Rect);
                g.DrawString("GT3", fSmallBold, Brushes.White, gt3Rect, centerFmt);

                int sessTypeX = gt3Rect.Right + (int)Math.Round(8 * s);
                using (SolidBrush cyanB = new SolidBrush(Color.FromArgb(56, 189, 248)))
                {
                    g.DrawString(_snapshot.SessionType, fHeader, cyanB, new RectangleF(sessTypeX, sessY, 55 * s, sessionH), leftFmt);
                }

                int trackX = sessTypeX + (int)Math.Round(44 * s);
                string lapPart = (_snapshot.TotalLaps > 0 && _snapshot.TotalLaps < 32767)
                    ? string.Format(CultureInfo.InvariantCulture, "LAP {0}/{1}", _snapshot.CurrentLap, _snapshot.TotalLaps)
                    : string.Format(CultureInfo.InvariantCulture, "LAP {0}", _snapshot.CurrentLap);
                string remPart = (_snapshot.SessionTimeRemainSec > 0 && _snapshot.SessionTimeRemainSec < 86000)
                    ? ("  •  REM " + FormatTimer(_snapshot.SessionTimeRemainSec))
                    : string.Empty;
                string metaStr = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}{1}  •  SOF {2:N0}  •  {3:0}C",
                    lapPart,
                    remPart,
                    _snapshot.StrengthOfField,
                    _snapshot.TrackTempC
                );
                SizeF metaSz = g.MeasureString(metaStr, fSmallBold);
                int metaW = (int)Math.Ceiling(metaSz.Width) + (int)(12 * s);
                int availTrackW = Math.Max(50, Width - trackX - metaW - (int)(10 * s));

                StringFormat trackFmt = _trackFmt;
                g.DrawString(_snapshot.TrackName, fHeader, Brushes.Gainsboro, new RectangleF(trackX, sessY, availTrackW, sessionH), trackFmt);

                using (SolidBrush goldB = new SolidBrush(Color.FromArgb(253, 224, 71)))
                {
                    g.DrawString(metaStr, fSmallBold, goldB, new RectangleF(Width - metaW - (int)(8 * s), sessY, metaW, sessionH), rightFmt);
                }

                int colLapW = (int)Math.Round(84 * s);
                int colLapX = Width - colLapW - (int)Math.Round(10 * s);

                int colGapW = (int)Math.Round(56 * s);
                int colGapX = colLapX - colGapW - (int)Math.Round(6 * s);

                int colIrW = (int)Math.Round(88 * s);
                int colIrX = colGapX - colIrW - (int)Math.Round(6 * s);

                int colSrW = (int)Math.Round(56 * s);
                int colSrX = colIrX - colSrW - (int)Math.Round(8 * s);

                int colPosX = (int)Math.Round(10 * s);
                int colPosW = (int)Math.Round(25 * s);

                int colNumX = colPosX + colPosW + (int)Math.Round(6 * s);
                int colNumW = _settings.ShowCarNumber ? (int)Math.Round(42 * s) : 0;

                int colFlagX = _settings.ShowCarNumber
                    ? (colNumX + colNumW + (int)Math.Round(4 * s))
                    : (colPosX + colPosW + (int)Math.Round(6 * s));
                int colFlagW = (int)Math.Round(24 * s);

                int colDriverX = colFlagX + colFlagW + (int)Math.Round(8 * s);
                int colDriverW = Math.Max((int)Math.Round(60 * s), colSrX - colDriverX - (int)Math.Round(6 * s));

                int colHeadY = sessY + sessionH;
                using (SolidBrush thBg = new SolidBrush(Color.FromArgb(12, 15, 22)))
                {
                    g.FillRectangle(thBg, 0, colHeadY, Width, colHeaderH);
                }
                using (SolidBrush mutedB = new SolidBrush(Color.FromArgb(156, 163, 175)))
                {
                    g.DrawString("P", fSmallBold, mutedB, new RectangleF(colPosX, colHeadY, colPosW, colHeaderH), centerFmt);
                    if (_settings.ShowCarNumber)
                    {
                        g.DrawString("#", fSmallBold, mutedB, new RectangleF(colNumX, colHeadY, colNumW, colHeaderH), leftFmt);
                    }
                    g.DrawString("FLG", fSmallBold, mutedB, new RectangleF(colFlagX, colHeadY, colFlagW, colHeaderH), centerFmt);
                    g.DrawString("DRIVER", fSmallBold, mutedB, new RectangleF(colDriverX, colHeadY, colDriverW, colHeaderH), leftFmt);
                    g.DrawString("SR", fSmallBold, mutedB, new RectangleF(colSrX, colHeadY, colSrW, colHeaderH), centerFmt);
                    g.DrawString("iRATING", fSmallBold, mutedB, new RectangleF(colIrX, colHeadY, colIrW, colHeaderH), rightFmt);
                    g.DrawString(_settings.ShowInterval ? "INT" : "GAP", fSmallBold, mutedB, new RectangleF(colGapX, colHeadY, colGapW, colHeaderH), rightFmt);
                    g.DrawString("LAST LAP", fSmallBold, mutedB, new RectangleF(colLapX, colHeadY, colLapW, colHeaderH), rightFmt);
                }

                int rowY = colHeadY + colHeaderH;
                int footerY = Height - footerH;
                int availRowsH = Math.Max(0, footerY - rowY);
                int maxRowsFit = Math.Max(1, availRowsH / rowH);

                List<DriverRow> drivers = _snapshot.Drivers;
                int totalDrivers = drivers.Count;
                int startIdx = 0;
                int visibleCount = Math.Min(maxRowsFit, totalDrivers);

                if (totalDrivers > maxRowsFit)
                {
                    int playerIdx = 0;
                    for (int i = 0; i < totalDrivers; i++)
                    {
                        if (drivers[i].IsPlayer)
                        {
                            playerIdx = i;
                            break;
                        }
                    }
                    int halfBefore = maxRowsFit / 2;
                    startIdx = playerIdx - halfBefore;
                    if (startIdx < 0) startIdx = 0;
                    if (startIdx + maxRowsFit > totalDrivers) startIdx = totalDrivers - maxRowsFit;
                }

                for (int r = 0; r < visibleCount; r++)
                {
                    DriverRow d = drivers[startIdx + r];
                    Color rowBg = d.IsPlayer
                        ? Color.FromArgb(95, 14, 116, 144)
                        : (r % 2 == 0 ? Color.FromArgb(18, 22, 32) : Color.FromArgb(23, 28, 40));

                    using (SolidBrush rb = new SolidBrush(rowBg))
                    {
                        g.FillRectangle(rb, 0, rowY, Width, rowH);
                    }

                    if (d.IsPlayer)
                    {
                        using (SolidBrush cyanAccent = new SolidBrush(Color.FromArgb(34, 211, 238)))
                        {
                            g.FillRectangle(cyanAccent, 0, rowY, Math.Max(3, (int)Math.Round(3.5f * s)), rowH);
                        }
                    }

                    int badgeH = Math.Max(15, (int)Math.Round(19 * s));
                    int badgeY = rowY + (rowH - badgeH) / 2;

                    Rectangle posRect = new Rectangle(colPosX, badgeY, colPosW, badgeH);
                    Color posBg = d.Position == 1
                        ? Color.FromArgb(250, 204, 21)
                        : (d.Position == 2 ? Color.FromArgb(209, 213, 219) : (d.Position == 3 ? Color.FromArgb(217, 119, 6) : Color.FromArgb(35, 42, 56)));
                    Color posFg = d.Position <= 2 ? Color.FromArgb(17, 24, 39) : Color.White;
                    using (SolidBrush pb = new SolidBrush(posBg)) g.FillRectangle(pb, posRect);
                    using (SolidBrush pf = new SolidBrush(posFg)) g.DrawString(d.Position.ToString(), fSmallBold, pf, posRect, centerFmt);

                    if (_settings.ShowCarNumber)
                    {
                        int barW = Math.Max(2, (int)Math.Round(3 * s));
                        using (SolidBrush classBar = new SolidBrush(Color.FromArgb(255, 82, 82)))
                        {
                            g.FillRectangle(classBar, colNumX, badgeY + 1, barW, badgeH - 2);
                        }
                        g.DrawString("#" + d.CarNumber, fRowBold, Brushes.WhiteSmoke,
                            new RectangleF(colNumX + barW + 3 * s, rowY, colNumW - barW, rowH), leftFmt);
                    }

                    int flagW = Math.Max(16, (int)Math.Round(21 * s));
                    int flagH = Math.Max(10, (int)Math.Round(13 * s));
                    DrawCountryFlag(g, colFlagX + (colFlagW - flagW) / 2, rowY + (rowH - flagH) / 2, flagW, flagH, d.CountryCode, s);

                    int tagW = (d.IsPlayer || d.OnPitRoad) ? (int)Math.Round(34 * s) : 0;
                    int nameAvailW = Math.Max(30, colDriverW - tagW - (int)(4 * s));
                    Color nameColor = d.IsPlayer ? Color.FromArgb(56, 189, 248) : Color.FromArgb(243, 244, 246);
                    using (SolidBrush nb = new SolidBrush(nameColor))
                    {
                        g.DrawString(d.UserName, d.IsPlayer ? fRowBold : fRow, nb,
                            new RectangleF(colDriverX, rowY, nameAvailW, rowH), trackFmt);
                    }

                    if (d.IsPlayer)
                    {
                        Rectangle youRect = new Rectangle(colDriverX + colDriverW - tagW, badgeY + 1, tagW - 2, badgeH - 2);
                        using (SolidBrush yb = new SolidBrush(Color.FromArgb(55, 56, 189, 248))) g.FillRectangle(yb, youRect);
                        using (SolidBrush yf = new SolidBrush(Color.FromArgb(56, 189, 248))) g.DrawString("YOU", fSmallBold, yf, youRect, centerFmt);
                    }
                    else if (d.OnPitRoad)
                    {
                        Rectangle pitRect = new Rectangle(colDriverX + colDriverW - tagW, badgeY + 1, tagW - 2, badgeH - 2);
                        using (SolidBrush pb = new SolidBrush(Color.FromArgb(65, 245, 158, 11))) g.FillRectangle(pb, pitRect);
                        using (SolidBrush pf = new SolidBrush(Color.FromArgb(251, 191, 36))) g.DrawString("PIT", fSmallBold, pf, pitRect, centerFmt);
                    }

                    Rectangle srRect = new Rectangle(colSrX, badgeY, colSrW, badgeH);
                    Color licBg = GetLicenseBg(d.LicClass);
                    Color licFg = d.LicClass == "C" ? Color.FromArgb(17, 24, 39) : Color.White;
                    using (SolidBrush lb = new SolidBrush(licBg)) g.FillRectangle(lb, srRect);
                    using (SolidBrush lf = new SolidBrush(licFg))
                    {
                        g.DrawString(d.LicClass + " " + d.SafetyRating, fSmallBold, lf, srRect, centerFmt);
                    }

                    Rectangle irRect = new Rectangle(colIrX, badgeY, colIrW, badgeH);
                    using (SolidBrush irBg = new SolidBrush(Color.FromArgb(26, 32, 44))) g.FillRectangle(irBg, irRect);
                    string irStr = _settings.CompactIR
                        ? string.Format(CultureInfo.InvariantCulture, "{0:0.0}k", d.IRating / 1000.0)
                        : d.IRating.ToString("N0", CultureInfo.InvariantCulture);
                    g.DrawString(irStr, fSmallBold, Brushes.WhiteSmoke,
                        new RectangleF(irRect.Left + 5 * s, badgeY, irRect.Width * 0.62f, badgeH), leftFmt);

                    if (_settings.ShowDeltaIR)
                    {
                        string dStr = d.IRatingDelta >= 0 ? ("+" + d.IRatingDelta) : d.IRatingDelta.ToString();
                        Color dCol = d.IRatingDelta >= 0 ? Color.FromArgb(74, 222, 128) : Color.FromArgb(248, 113, 113);
                        using (SolidBrush db = new SolidBrush(dCol))
                        {
                            g.DrawString(dStr, fSmallBold, db,
                                new RectangleF(irRect.Left, badgeY, irRect.Width - 4 * s, badgeH), rightFmt);
                        }
                    }

                    RectangleF gapRect = new RectangleF(colGapX, rowY, colGapW, rowH);
                    if (d.OnPitRoad)
                    {
                        using (SolidBrush pitB = new SolidBrush(Color.FromArgb(251, 191, 36)))
                            g.DrawString("IN PIT", fSmallBold, pitB, gapRect, rightFmt);
                    }
                    else
                    {
                        float gapVal = _settings.ShowInterval ? d.IntervalAheadSec : d.GapToLeaderSec;
                        string gapTxt = d.Position == 1 ? "LDR" : string.Format(CultureInfo.InvariantCulture, "+{0:0.0}s", gapVal);
                        using (SolidBrush gb = new SolidBrush(Color.FromArgb(209, 213, 219)))
                            g.DrawString(gapTxt, fSmallBold, gb, gapRect, rightFmt);
                    }

                    RectangleF lapRect = new RectangleF(colLapX, rowY, colLapW, rowH);
                    Color lapCol = d.LastLapStatus == LapStatus.SessionBest
                        ? Color.FromArgb(192, 132, 252)
                        : (d.LastLapStatus == LapStatus.PersonalBest ? Color.FromArgb(74, 222, 128) : Color.FromArgb(243, 244, 246));
                    using (SolidBrush lb = new SolidBrush(lapCol))
                    {
                        g.DrawString(DpiHelper.FormatLapTime(d.LastLapSec), fMono, lb, lapRect, rightFmt);
                    }

                    using (Pen divPen = new Pen(Color.FromArgb(14, 255, 255, 255)))
                    {
                        g.DrawLine(divPen, 0, rowY + rowH - 1, Width, rowY + rowH - 1);
                    }

                    rowY += rowH;
                }

                using (SolidBrush fbBg = new SolidBrush(Color.FromArgb(9, 11, 17)))
                {
                    g.FillRectangle(fbBg, 0, footerY, Width, footerH);
                }

                int fDotSz = Math.Max(5, (int)Math.Round(7 * s));
                int fDotY = footerY + (footerH - fDotSz) / 2;
                using (SolidBrush pDot = new SolidBrush(Color.FromArgb(192, 132, 252))) g.FillEllipse(pDot, (int)(12 * s), fDotY, fDotSz, fDotSz);
                g.DrawString("Session Best", fSmallBold, Brushes.Gray, new RectangleF(22 * s, footerY, 90 * s, footerH), leftFmt);

                using (SolidBrush gDot = new SolidBrush(Color.FromArgb(74, 222, 128))) g.FillEllipse(gDot, (int)(110 * s), fDotY, fDotSz, fDotSz);
                g.DrawString("Personal Best", fSmallBold, Brushes.Gray, new RectangleF(120 * s, footerY, 95 * s, footerH), leftFmt);

                string modeHint = _isHovered && !_settings.Locked
                    ? (_cornerMode == CornerDragMode.ResizeWindow
                        ? "Corner Drag: RESIZE  •  F5: Hub  •  F6: Lock"
                        : string.Format(CultureInfo.InvariantCulture, "Corner Drag: FONT ({0}%)  •  F5: Hub  •  F6: Lock", (int)Math.Round(_settings.StandingsFontScale * 100)))
                    : "F5: Hub  •  F6: Lock  •  F7: Mode  •  F8: Quit";

                Brush hintBrush = (_isHovered && !_settings.Locked)
                    ? (_cornerMode == CornerDragMode.ResizeWindow
                        ? (Brush)new SolidBrush(Color.FromArgb(56, 189, 248))
                        : new SolidBrush(Color.FromArgb(216, 180, 254)))
                    : Brushes.Gray;

                g.DrawString(modeHint, fSmallBold, hintBrush,
                    new RectangleF(215 * s, footerY, Width - 225 * s, footerH), rightFmt);

                if (_isHovered && !_settings.Locked && hintBrush != Brushes.Gray)
                {
                    hintBrush.Dispose();
                }

                if (_isHovered && !_settings.Locked)
                {
                    Color activeAccent = _cornerMode == CornerDragMode.ResizeWindow
                        ? Color.FromArgb(210, 56, 189, 248)
                        : Color.FromArgb(210, 192, 132, 252);

                    using (Pen borderPen = new Pen(activeAccent, Math.Max(1.5f, 1.5f * s)))
                    {
                        g.DrawRectangle(borderPen, 1, 1, Width - 2, Height - 2);
                    }
                    DpiHelper.DrawCornerGrips(g, Width, Height, activeAccent, s);
                }
                else
                {
                    Color borderCol = _settings.Locked ? Color.FromArgb(45, 255, 255, 255) : Color.FromArgb(80, 255, 255, 255);
                    using (Pen borderPen = new Pen(borderCol, 1.0f))
                    {
                        g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
                    }
                }
            }
        }
    }


    /// <summary>
    /// Garage61 Reference Telemetry & Braking Coach Overlay Window.
    /// Supports all 3 selectable layout designs:
    /// - OptionA_LookaheadTraceAndPedals: Left live Brake/Throttle/Speed/Gear matcher + Right spatial distance-synced graph with +Lookahead
    /// - OptionB_CompactBrakeCoach: Compact horizontal Braking Spot countdown & full-width Brake Pressure/Peak matcher bar
    /// - OptionC_ProBoxWithCornerAudit: Spatial lookahead graph + live brake readouts + 3-corner audit strip at bottom
    /// Includes SimLab-style single top-right icon toggle button (Resize <-> Aa Font) + 4 corner grips.
    /// </summary>
    public class Garage61OverlayForm : Form
    {
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int WS_EX_TOPMOST = 0x00000008;
        private const int WM_DPICHANGED = 0x02E0;
        private const int WM_NCLBUTTONDOWN = 0x00A1;
        private const int HT_CAPTION = 0x02;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private readonly OverlaySettings _settings;
        private readonly Garage61LapManager _g61;
        private SessionSnapshot _snapshot;
        private float _dpiScale = 1.0f;

        private bool _isHovered = false;
        private CornerDragMode _cornerMode = CornerDragMode.ResizeWindow;
        private DragCorner _activeDragCorner = DragCorner.None;
        private Point _dragStartScreenPt;
        private Rectangle _dragStartBounds;
        private float _dragStartFontScale = 1.0f;

        private Rectangle _rectCornerModeBtn;
        private Rectangle _rectStyleCycleBtn;
        private Rectangle _rectAudioToggleBtn;

        private const int GRAPH_SAMPLES = 72;
        private readonly PointF[] _g61BrakeBuf = new PointF[GRAPH_SAMPLES + 1];
        private readonly PointF[] _g61ThrotBuf = new PointF[GRAPH_SAMPLES + 1];
        private PointF[] _youBrakeBuf = new PointF[0];
        private PointF[] _youThrotBuf = new PointF[0];

        private float _cachedFontScale = -1f;
        private Font _fHeader;
        private Font _fSmall;
        private Font _fMono;
        private Font _fBigMono;
        private Font _fIconAa;
        private readonly StringFormat _leftFmt = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
        private readonly StringFormat _rightFmt = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
        private readonly StringFormat _centerFmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        private void EnsureFonts(float s)
        {
            if (Math.Abs(_cachedFontScale - s) < 0.001f && _fHeader != null) return;
            if (_fHeader != null) _fHeader.Dispose();
            if (_fSmall != null) _fSmall.Dispose();
            if (_fMono != null) _fMono.Dispose();
            if (_fBigMono != null) _fBigMono.Dispose();
            if (_fIconAa != null) _fIconAa.Dispose();

            _fHeader = new Font("Segoe UI", 10.5f * s, FontStyle.Bold, GraphicsUnit.Pixel);
            _fSmall = new Font("Segoe UI", 9.5f * s, FontStyle.Bold, GraphicsUnit.Pixel);
            _fMono = new Font("Consolas", 11.5f * s, FontStyle.Bold, GraphicsUnit.Pixel);
            _fBigMono = new Font("Consolas", 15.0f * s, FontStyle.Bold, GraphicsUnit.Pixel);
            _fIconAa = new Font("Segoe UI", 11.5f * s, FontStyle.Bold, GraphicsUnit.Pixel);
            _cachedFontScale = s;
        }

        public Garage61OverlayForm(OverlaySettings settings, Garage61LapManager g61, SessionSnapshot initialSnapshot)
        {
            _settings = settings;
            _g61 = g61;
            _snapshot = initialSnapshot;

            Text = "Free Overlay - Garage61 Coach";
            AutoScaleMode = AutoScaleMode.None;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            DoubleBuffered = true;
            BackColor = Color.FromArgb(13, 16, 23);
            Opacity = _settings.Garage61Opacity;

            _dpiScale = DpiHelper.GetScale(IntPtr.Zero);
            int savedX = _settings.Garage61X;
            int savedY = _settings.Garage61Y;
            int savedW = _settings.Garage61W;
            int savedH = _settings.Garage61H;

            ResetPositionAndSize(false);

            if (savedW > 200 && savedH > 70)
            {
                Width = savedW;
                Height = savedH;
            }
            if (savedX != -1 || savedY != -1)
            {
                Location = new Point(savedX, savedY);
            }
            SaveGeometry();
        }

        private float EffectiveScale
        {
            get { return _dpiScale * _settings.Garage61FontScale; }
        }

        public void ApplyDefaultSizeForCurrentStyle(bool notify)
        {
            float s = EffectiveScale;
            if (_settings.Garage61Style == Garage61LayoutStyle.OptionB_CompactBrakeCoach)
            {
                Width = (int)Math.Round(520 * s);
                Height = (int)Math.Round(122 * s);
            }
            else if (_settings.Garage61Style == Garage61LayoutStyle.OptionC_ProBoxWithCornerAudit)
            {
                Width = (int)Math.Round(620 * s);
                Height = (int)Math.Round(236 * s);
            }
            else
            {
                Width = (int)Math.Round(600 * s);
                Height = (int)Math.Round(208 * s);
            }
            SaveGeometry();
            if (notify) _settings.NotifyChanged();
            Invalidate();
        }

        public void ResetPositionAndSize(bool notify)
        {
            ApplyDefaultSizeForCurrentStyle(false);
            Rectangle workArea = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(
                (workArea.Width - Width) / 2,
                Math.Max(100, workArea.Height - Height - (int)Math.Round(48 * _dpiScale))
            );
            SaveGeometry();
            if (notify) _settings.NotifyChanged();
            Invalidate();
        }

        public void SaveGeometry()
        {
            _settings.Garage61X = Left;
            _settings.Garage61Y = Top;
            _settings.Garage61W = Width;
            _settings.Garage61H = Height;
        }

        protected override void OnMove(EventArgs e)
        {
            base.OnMove(e);
            if (IsHandleCreated) SaveGeometry();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (IsHandleCreated) SaveGeometry();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            _dpiScale = DpiHelper.GetScale(Handle);
            ApplySettingsState();
        }

        public void ApplySettingsState()
        {
            if (!IsHandleCreated) return;
            Opacity = _settings.Garage61Opacity;
            int exStyle = GetWindowLong(Handle, GWL_EXSTYLE);
            if (_settings.Locked)
            {
                SetWindowLong(Handle, GWL_EXSTYLE, exStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOPMOST);
                _isHovered = false;
            }
            else
            {
                SetWindowLong(Handle, GWL_EXSTYLE, (exStyle | WS_EX_LAYERED | WS_EX_TOPMOST) & ~WS_EX_TRANSPARENT);
            }
            Invalidate();
        }

        public void UpdateSnapshot(SessionSnapshot snap)
        {
            _snapshot = snap;
            if (!_settings.Locked && _activeDragCorner == DragCorner.None)
            {
                bool inside = Bounds.Contains(Cursor.Position);
                if (inside != _isHovered) _isHovered = inside;
            }
            Invalidate();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_DPICHANGED)
            {
                int newDpi = m.WParam.ToInt32() & 0xFFFF;
                if (newDpi >= 72 && newDpi <= 480)
                {
                    float nextDpi = newDpi / 96.0f;
                    float ratio = nextDpi / _dpiScale;
                    _dpiScale = nextDpi;
                    Width = (int)Math.Round(Width * ratio);
                    Height = (int)Math.Round(Height * ratio);
                    Invalidate();
                }
            }
            base.WndProc(ref m);
        }

        private int GetMinWidth()
        {
            return Math.Max((int)Math.Round(340 * _dpiScale), (int)Math.Round(380 * EffectiveScale));
        }

        private int GetMinHeight()
        {
            int baseH = _settings.Garage61Style == Garage61LayoutStyle.OptionB_CompactBrakeCoach ? 98 : 165;
            return Math.Max((int)Math.Round(80 * _dpiScale), (int)Math.Round(baseH * EffectiveScale));
        }

        private DragCorner HitTestCorner(Point pt)
        {
            int grip = Math.Max(16, (int)Math.Round(20 * EffectiveScale));
            bool left = pt.X <= grip;
            bool right = pt.X >= Width - grip;
            bool top = pt.Y <= grip;
            bool bottom = pt.Y >= Height - grip;

            if (top && right)
            {
                if (_rectCornerModeBtn.Contains(pt)) return DragCorner.None;
                int edge = Math.Max(8, (int)Math.Round(10 * EffectiveScale));
                if (pt.X >= Width - edge && pt.Y <= edge) return DragCorner.TopRight;
                return DragCorner.None;
            }
            if (bottom && right) return DragCorner.BottomRight;
            if (bottom && left) return DragCorner.BottomLeft;
            if (top && left) return DragCorner.TopLeft;
            return DragCorner.None;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (_settings.Locked || e.Button != MouseButtons.Left) return;

            Point pt = e.Location;
            if (_isHovered)
            {
                if (_rectCornerModeBtn.Contains(pt))
                {
                    _cornerMode = (_cornerMode == CornerDragMode.ResizeWindow)
                        ? CornerDragMode.ChangeFontSize
                        : CornerDragMode.ResizeWindow;
                    Invalidate();
                    return;
                }
                if (_rectStyleCycleBtn.Contains(pt))
                {
                    _settings.Garage61Style = (Garage61LayoutStyle)(((int)_settings.Garage61Style + 1) % 3);
                    ApplyDefaultSizeForCurrentStyle(true);
                    return;
                }
                if (_rectAudioToggleBtn.Contains(pt))
                {
                    _settings.Garage61AudioEnabled = !_settings.Garage61AudioEnabled;
                    _settings.NotifyChanged();
                    if (_settings.Garage61AudioEnabled)
                    {
                        BrakingAudioCoach.PlayTestSequence();
                    }
                    Invalidate();
                    return;
                }
            }

            DragCorner corner = HitTestCorner(pt);
            if (corner != DragCorner.None)
            {
                _activeDragCorner = corner;
                _dragStartScreenPt = Cursor.Position;
                _dragStartBounds = Bounds;
                _dragStartFontScale = _settings.Garage61FontScale;
                Capture = true;
                return;
            }

            ReleaseCapture();
            SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            SaveGeometry();
            _settings.Save();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_settings.Locked) return;

            if (!_isHovered)
            {
                _isHovered = true;
                Invalidate();
            }

            if (_activeDragCorner != DragCorner.None)
            {
                Point curScreen = Cursor.Position;
                int dx = curScreen.X - _dragStartScreenPt.X;
                int dy = curScreen.Y - _dragStartScreenPt.Y;

                if (_cornerMode == CornerDragMode.ChangeFontSize)
                {
                    int outward = 0;
                    if (_activeDragCorner == DragCorner.BottomRight) outward = dx + dy;
                    else if (_activeDragCorner == DragCorner.BottomLeft) outward = -dx + dy;
                    else if (_activeDragCorner == DragCorner.TopRight) outward = dx - dy;
                    else if (_activeDragCorner == DragCorner.TopLeft) outward = -dx - dy;

                    float sens = 0.0022f / Math.Max(1.0f, _dpiScale * 0.75f);
                    float nextScale = Math.Max(0.65f, Math.Min(1.75f, _dragStartFontScale + outward * sens));
                    _settings.Garage61FontScale = nextScale;

                    float ratio = nextScale / _dragStartFontScale;
                    int newW = Math.Max(GetMinWidth(), (int)Math.Round(_dragStartBounds.Width * ratio));
                    int newH = Math.Max(GetMinHeight(), (int)Math.Round(_dragStartBounds.Height * ratio));
                    Size = new Size(newW, newH);
                    SaveGeometry();
                    Invalidate();
                }
                else
                {
                    int minW = GetMinWidth();
                    int minH = GetMinHeight();
                    int newX = _dragStartBounds.X;
                    int newY = _dragStartBounds.Y;
                    int newW = _dragStartBounds.Width;
                    int newH = _dragStartBounds.Height;

                    if (_activeDragCorner == DragCorner.BottomRight || _activeDragCorner == DragCorner.TopRight)
                        newW = Math.Max(minW, _dragStartBounds.Width + dx);
                    else if (_activeDragCorner == DragCorner.BottomLeft || _activeDragCorner == DragCorner.TopLeft)
                    {
                        newW = Math.Max(minW, _dragStartBounds.Width - dx);
                        newX = _dragStartBounds.Right - newW;
                    }

                    if (_activeDragCorner == DragCorner.BottomRight || _activeDragCorner == DragCorner.BottomLeft)
                        newH = Math.Max(minH, _dragStartBounds.Height + dy);
                    else if (_activeDragCorner == DragCorner.TopRight || _activeDragCorner == DragCorner.TopLeft)
                    {
                        newH = Math.Max(minH, _dragStartBounds.Height - dy);
                        newY = _dragStartBounds.Bottom - newH;
                    }

                    Bounds = new Rectangle(newX, newY, newW, newH);
                    SaveGeometry();
                    Invalidate();
                }
                return;
            }

            DragCorner hc = HitTestCorner(e.Location);
            if (hc == DragCorner.TopLeft || hc == DragCorner.BottomRight)
                Cursor = Cursors.SizeNWSE;
            else if (hc == DragCorner.TopRight || hc == DragCorner.BottomLeft)
                Cursor = Cursors.SizeNESW;
            else if (_rectCornerModeBtn.Contains(e.Location) || _rectStyleCycleBtn.Contains(e.Location) || _rectAudioToggleBtn.Contains(e.Location))
                Cursor = Cursors.Hand;
            else
                Cursor = Cursors.Default;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_activeDragCorner != DragCorner.None)
            {
                _activeDragCorner = DragCorner.None;
                Capture = false;
                SaveGeometry();
                _settings.NotifyChanged();
                Invalidate();
            }
        }

        private void DrawBrakingBanner(Graphics g, Rectangle rect, float nowPct, float youBrk, float g61Brk, float trackLen, Font fBold, Font fMono, float s)
        {
            float distToStart;
            BrakingZoneInfo z = _g61.GetRelevantZone(nowPct, out distToStart, trackLen);
            float peakTarget = z != null ? z.PeakBrake : 84f;
            string zName = z != null ? z.Name : "Corner";

            StringFormat leftFmt = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
            StringFormat rightFmt = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
            StringFormat centerFmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

            if (distToStart > 3.0f && distToStart <= 260.0f)
            {
                // Approaching Braking Zone -> Countdown bar, 3-2-1-BRAKE lights & distance
                using (SolidBrush bg = new SolidBrush(Color.FromArgb(20, 26, 38))) g.FillRectangle(bg, rect);
                using (Pen bp = new Pen(Color.FromArgb(90, 56, 189, 248), 1f)) g.DrawRectangle(bp, rect);

                string leftTxt = string.Format(CultureInfo.InvariantCulture, "NEXT BRAKE ({0}): IN {1:0}m", zName, distToStart);
                using (SolidBrush sb = new SolidBrush(Color.FromArgb(125, 211, 252)))
                {
                    g.DrawString(leftTxt, fBold, sb, new RectangleF(rect.Left + 8 * s, rect.Top, rect.Width * 0.42f, rect.Height), leftFmt);
                }

                // 4-Stage F1-style Countdown Lights (Prep 1 -> Prep 2 -> Prep 3 -> BRAKE)
                int stage = _g61.CurrentAudioStage;
                int lightsAreaW = (int)Math.Round(rect.Width * 0.25f);
                int lightsLeft = rect.Left + (rect.Width - lightsAreaW) / 2;
                int lightGap = Math.Max(2, (int)Math.Round(4 * s));
                int lightW = (lightsAreaW - lightGap * 3) / 4;
                int lightH = Math.Max(10, (int)Math.Round(14 * s));
                int lightY = rect.Top + (rect.Height - lightH) / 2;

                for (int li = 0; li < 4; li++)
                {
                    int lx = lightsLeft + li * (lightW + lightGap);
                    Rectangle lr = new Rectangle(lx, lightY, lightW, lightH);
                    bool lit = stage >= (li + 1);
                    Color activeCol = li == 0
                        ? Color.FromArgb(56, 189, 248)
                        : (li == 1 ? Color.FromArgb(250, 204, 21) : (li == 2 ? Color.FromArgb(249, 115, 22) : Color.FromArgb(244, 63, 94)));
                    Color fillC = lit ? activeCol : Color.FromArgb(28, 38, 54);
                    Color bordC = lit ? Color.White : Color.FromArgb(70, 255, 255, 255);
                    using (SolidBrush lb = new SolidBrush(fillC)) g.FillRectangle(lb, lr);
                    using (Pen lp = new Pen(bordC, 1f)) g.DrawRectangle(lp, lr);
                }

                string rightTxt = string.Format(CultureInfo.InvariantCulture, "TARGET PEAK: {0:0}%", peakTarget);
                using (SolidBrush rb = new SolidBrush(Color.FromArgb(251, 113, 133)))
                {
                    g.DrawString(rightTxt, fMono, rb, new RectangleF(rect.Right - rect.Width * 0.32f, rect.Top, rect.Width * 0.32f - 8 * s, rect.Height), rightFmt);
                }
            }
            else if (distToStart <= 3.0f && z != null)
            {
                // Actively inside Braking Zone -> Flash BRAKE NOW! at entry or show Brake Spot Delta + Live Pressure Diff
                bool justEntered = distToStart >= -18.0f && youBrk < 12.0f;
                Color bannerBg = justEntered ? Color.FromArgb(185, 225, 29, 72) : Color.FromArgb(48, 136, 19, 55);
                using (SolidBrush bg = new SolidBrush(bannerBg)) g.FillRectangle(bg, rect);
                using (Pen bp = new Pen(Color.FromArgb(220, 244, 63, 94), 1.2f)) g.DrawRectangle(bp, rect);

                float spotDelta = _g61.HasCurrentBrakeSpotDelta ? _g61.CurrentBrakeSpotDeltaMeters : 2.4f;
                string spotTxt = justEntered
                    ? string.Format(CultureInfo.InvariantCulture, ">>> BRAKE NOW! ({0}) <<<", zName)
                    : string.Format(
                        CultureInfo.InvariantCulture,
                        "BRAKE PT ({0}): {1:0.0}m {2}",
                        zName,
                        Math.Abs(spotDelta),
                        spotDelta >= 0f ? "LATE" : "EARLY"
                    );
                using (SolidBrush ab = new SolidBrush(justEntered ? Color.White : Color.FromArgb(253, 224, 71)))
                {
                    g.DrawString(spotTxt, fBold, ab, new RectangleF(rect.Left + 8 * s, rect.Top, rect.Width * 0.48f, rect.Height), leftFmt);
                }

                float diff = youBrk - g61Brk;
                string diffWord = Math.Abs(diff) <= 4f ? "MATCHED" : (diff > 0f ? string.Format(CultureInfo.InvariantCulture, "+{0:0}% OVER", diff) : string.Format(CultureInfo.InvariantCulture, "{0:0}% UNDER", diff));
                string pressTxt = string.Format(CultureInfo.InvariantCulture, "PRESSURE: {0:0}% vs {1:0}% ({2})", youBrk, g61Brk, diffWord);
                Color pCol = Math.Abs(diff) <= 5f ? Color.FromArgb(74, 222, 128) : Color.FromArgb(253, 224, 71);
                using (SolidBrush pb = new SolidBrush(pCol))
                {
                    g.DrawString(pressTxt, fMono, pb, new RectangleF(rect.Left + rect.Width * 0.45f, rect.Top, rect.Width * 0.55f - 8 * s, rect.Height), rightFmt);
                }
            }
            else
            {
                // Straightaway / Cruise -> Show distance to next corner and last corner summary
                using (SolidBrush bg = new SolidBrush(Color.FromArgb(18, 28, 34))) g.FillRectangle(bg, rect);
                using (Pen bp = new Pen(Color.FromArgb(90, 52, 211, 153), 1f)) g.DrawRectangle(bp, rect);

                float lastSpot = _g61.HasCurrentBrakeSpotDelta ? _g61.CurrentBrakeSpotDeltaMeters : -1.4f;
                string sumTxt = string.Format(
                    CultureInfo.InvariantCulture,
                    "LAST BRAKE PT: {0:0.0}m {1}  •  PEAK: {2:0}% (TARGET {3:0}%)",
                    Math.Abs(lastSpot),
                    lastSpot >= 0f ? "LATE" : "EARLY",
                    _g61.CurrentZonePlayerPeakBrake > 5f ? _g61.CurrentZonePlayerPeakBrake : (peakTarget - 4f),
                    peakTarget
                );
                using (SolidBrush eb = new SolidBrush(Color.FromArgb(110, 231, 183)))
                {
                    g.DrawString(sumTxt, fBold, eb, new RectangleF(rect.Left + 8 * s, rect.Top, rect.Width * 0.65f, rect.Height), leftFmt);
                }

                string nextTxt = string.Format(CultureInfo.InvariantCulture, "NEXT: {0} ({1:0}m)", zName, Math.Max(0f, distToStart));
                using (SolidBrush sb = new SolidBrush(Color.FromArgb(125, 211, 252)))
                {
                    g.DrawString(nextTxt, fMono, sb, new RectangleF(rect.Right - rect.Width * 0.35f, rect.Top, rect.Width * 0.35f - 8 * s, rect.Height), rightFmt);
                }
            }
        }

        private void DrawSpatialLookaheadGraph(Graphics g, Rectangle graphRect, float nowPct, float trackLen, float peakTarget, Font fSmall, float s)
        {
            using (SolidBrush bg = new SolidBrush(Color.FromArgb(10, 13, 20))) g.FillRectangle(bg, graphRect);
            using (Pen border = new Pen(Color.FromArgb(45, 255, 255, 255), 1f)) g.DrawRectangle(border, graphRect);

            if (graphRect.Width < 40 || graphRect.Height < 30) return;

            // 62% of width = History (-180m), 38% of width = Lookahead (+Garage61LookaheadMeters)
            float lookaheadMeters = Math.Max(50f, _settings.Garage61LookaheadMeters);
            float historyMeters = lookaheadMeters * 1.55f;
            float winPastPct = historyMeters / Math.Max(1000f, trackLen);
            float winAheadPct = lookaheadMeters / Math.Max(1000f, trackLen);
            float totalWinPct = winPastPct + winAheadPct;

            int nowX = graphRect.Left + (int)Math.Round((winPastPct / totalWinPct) * graphRect.Width);

            // Shaded Lookahead region to the right of NOW line
            Rectangle lookRect = new Rectangle(nowX, graphRect.Top + 1, graphRect.Right - nowX - 1, graphRect.Height - 2);
            using (SolidBrush lb = new SolidBrush(Color.FromArgb(16, 56, 189, 248)))
            {
                g.FillRectangle(lb, lookRect);
            }

            int topPad = (int)Math.Round(12 * s);
            int botPad = (int)Math.Round(6 * s);
            int plotH = Math.Max(10, graphRect.Height - topPad - botPad);

            // Peak Brake Target horizontal dashed line
            int peakY = graphRect.Bottom - botPad - (int)Math.Round((peakTarget / 100.0f) * plotH);
            using (Pen peakPen = new Pen(Color.FromArgb(95, 251, 113, 133), 1f))
            {
                peakPen.DashStyle = DashStyle.Dash;
                g.DrawLine(peakPen, graphRect.Left + 2, peakY, graphRect.Right - 2, peakY);
            }

            int numSamples = GRAPH_SAMPLES;
            int youCount = 0;
            for (int i = 0; i <= numSamples; i++)
            {
                float frac = (float)i / numSamples;
                float dp = -winPastPct + frac * totalWinPct;
                if (dp < -0.0002f) youCount++;
            }
            int totalYouPts = youCount + 1;
            if (_youBrakeBuf.Length != totalYouPts)
            {
                _youBrakeBuf = new PointF[totalYouPts];
                _youThrotBuf = new PointF[totalYouPts];
            }

            int yIdx = 0;
            for (int i = 0; i <= numSamples; i++)
            {
                float frac = (float)i / numSamples;
                float dp = -winPastPct + frac * totalWinPct;
                float samplePct = nowPct + dp;
                float x = graphRect.Left + frac * graphRect.Width;

                float rb = _g61.GetRefBrakeAtPct(samplePct);
                float rt = _g61.GetRefThrottleAtPct(samplePct);
                float yb = graphRect.Bottom - botPad - (rb / 100.0f) * plotH;
                float yt = graphRect.Bottom - botPad - (rt / 100.0f) * plotH;
                _g61BrakeBuf[i] = new PointF(x, yb);
                _g61ThrotBuf[i] = new PointF(x, yt);

                if (dp < -0.0002f && yIdx < youCount)
                {
                    float pb = _g61.GetPlayerBrakeAtPct(samplePct);
                    float pt = _g61.GetPlayerThrottleAtPct(samplePct);
                    float pyb = graphRect.Bottom - botPad - (pb / 100.0f) * plotH;
                    float pyt = graphRect.Bottom - botPad - (pt / 100.0f) * plotH;
                    _youBrakeBuf[yIdx] = new PointF(x, pyb);
                    _youThrotBuf[yIdx] = new PointF(x, pyt);
                    yIdx++;
                }
            }

            // Anchor the live tip of the player trace squarely on NOW (dp = 0, x = nowX)
            float nowYb = graphRect.Bottom - botPad - (_snapshot.PlayerBrakePct / 100.0f) * plotH;
            float nowYt = graphRect.Bottom - botPad - (_snapshot.PlayerThrottlePct / 100.0f) * plotH;
            _youBrakeBuf[totalYouPts - 1] = new PointF(nowX, nowYb);
            _youThrotBuf[totalYouPts - 1] = new PointF(nowX, nowYt);

            // Draw Throttle traces (G61 reference translucent green, Player solid green)
            using (Pen g61ThrPen = new Pen(Color.FromArgb(115, 52, 211, 153), Math.Max(1.2f, 1.4f * s)))
            {
                g.DrawLines(g61ThrPen, _g61ThrotBuf);
            }
            using (Pen youThrPen = new Pen(Color.FromArgb(220, 16, 185, 129), Math.Max(1.6f, 1.9f * s)))
            {
                if (totalYouPts > 1) g.DrawLines(youThrPen, _youThrotBuf);
            }

            // Draw Brake traces (G61 reference cyan extending into Lookahead, Player solid bright red up to NOW)
            using (Pen g61BrkPen = new Pen(Color.FromArgb(215, 56, 189, 248), Math.Max(1.8f, 2.1f * s)))
            {
                g.DrawLines(g61BrkPen, _g61BrakeBuf);
            }
            using (Pen youBrkPen = new Pen(Color.FromArgb(255, 244, 63, 94), Math.Max(2.2f, 2.8f * s)))
            {
                if (totalYouPts > 1) g.DrawLines(youBrkPen, _youBrakeBuf);
            }

            // Highlight any G61 Braking Point marker (and 3-2-1 prep ticks) inside the visible window
            float wrappedNow = nowPct - (float)Math.Floor(nowPct);
            float speedMps = Math.Max(18.0f, _snapshot.PlayerSpeedKph / 3.6f);
            for (int z = 0; z < _g61.Zones.Count; z++)
            {
                float dp = _g61.Zones[z].StartPct - wrappedNow;
                if (dp < -0.5f) dp += 1.0f;
                if (dp > 0.5f) dp -= 1.0f;

                // Draw 3 preparatory audio cue ticks ahead of the exact brake spot (1.05s, 0.70s, 0.35s)
                if (_settings.Garage61AudioEnabled && dp > 0f)
                {
                    float[] prepSecs = new float[] { 1.05f, 0.70f, 0.35f };
                    for (int p = 0; p < prepSecs.Length; p++)
                    {
                        float prepPctOffset = (prepSecs[p] * speedMps) / Math.Max(1000f, trackLen);
                        float tickDp = dp - prepPctOffset;
                        if (tickDp >= -winPastPct && tickDp <= winAheadPct)
                        {
                            int tx = graphRect.Left + (int)Math.Round(((tickDp + winPastPct) / totalWinPct) * graphRect.Width);
                            Color tickCol = p == 0
                                ? Color.FromArgb(110, 56, 189, 248)
                                : (p == 1 ? Color.FromArgb(130, 250, 204, 21) : Color.FromArgb(150, 249, 115, 22));
                            using (Pen tp = new Pen(tickCol, Math.Max(1f, 1.1f * s)))
                            {
                                tp.DashStyle = DashStyle.Dot;
                                g.DrawLine(tp, tx, graphRect.Top + 14 * s, tx, graphRect.Bottom - 2);
                            }
                        }
                    }
                }

                if (dp >= -winPastPct && dp <= winAheadPct)
                {
                    int bx = graphRect.Left + (int)Math.Round(((dp + winPastPct) / totalWinPct) * graphRect.Width);
                    bool atBrakeSpot = Math.Abs(dp * trackLen) <= 14.0f;
                    Color lineCol = atBrakeSpot ? Color.FromArgb(255, 244, 63, 94) : Color.FromArgb(220, 56, 189, 248);
                    using (Pen mPen = new Pen(lineCol, Math.Max(1.4f, (atBrakeSpot ? 2.2f : 1.5f) * s)))
                    {
                        if (!atBrakeSpot) mPen.DashStyle = DashStyle.Dot;
                        g.DrawLine(mPen, bx, graphRect.Top + 2, bx, graphRect.Bottom - 2);
                    }
                    int tagW = (int)Math.Round((atBrakeSpot ? 62 : 56) * s);
                    int tagH = (int)Math.Round(13 * s);
                    Rectangle tagR = new Rectangle(Math.Max(graphRect.Left + 2, Math.Min(graphRect.Right - tagW - 2, bx - tagW / 2)), graphRect.Top + 2, tagW, tagH);
                    Color tagBg = atBrakeSpot ? Color.FromArgb(240, 225, 29, 72) : Color.FromArgb(220, 2, 132, 199);
                    using (SolidBrush tb = new SolidBrush(tagBg)) g.FillRectangle(tb, tagR);
                    StringFormat cf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString(atBrakeSpot ? "BRAKE!" : "G61 BRK", fSmall, Brushes.White, tagR, cf);
                }
            }

            // Vertical NOW Crosshair
            using (Pen nowPen = new Pen(Color.White, Math.Max(1.6f, 2.0f * s)))
            {
                g.DrawLine(nowPen, nowX, graphRect.Top + 1, nowX, graphRect.Bottom - 1);
            }
            int nw = (int)Math.Round(34 * s);
            int nh = (int)Math.Round(13 * s);
            Rectangle nowTag = new Rectangle(nowX - nw / 2, graphRect.Bottom - nh - 2, nw, nh);
            using (SolidBrush wb = new SolidBrush(Color.White)) g.FillRectangle(wb, nowTag);
            StringFormat cFmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            using (SolidBrush db = new SolidBrush(Color.FromArgb(9, 13, 22))) g.DrawString("NOW", fSmall, db, nowTag, cFmt);

            string lookLbl = string.Format(CultureInfo.InvariantCulture, "LOOKAHEAD (+{0}m) ->", _settings.Garage61LookaheadMeters);
            using (SolidBrush lBrush = new SolidBrush(Color.FromArgb(125, 211, 252)))
            {
                g.DrawString(lookLbl, fSmall, lBrush, new PointF(nowX + 6 * s, graphRect.Top + 2 * s));
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            float s = EffectiveScale;
            if (_snapshot == null) _snapshot = new SessionSnapshot();

            float nowPct = _snapshot.PlayerLapDistPct;
            float trackLen = Math.Max(1000f, _snapshot.TrackLengthMeters);
            float youBrk = _snapshot.PlayerBrakePct;
            float youThr = _snapshot.PlayerThrottlePct;
            float g61Brk = _g61.GetRefBrakeAtPct(nowPct);
            float g61Thr = _g61.GetRefThrottleAtPct(nowPct);

            float distToZone;
            BrakingZoneInfo relZone = _g61.GetRelevantZone(nowPct, out distToZone, trackLen);
            float peakTarget = relZone != null ? relZone.PeakBrake : 84f;

            EnsureFonts(s);
            Font fHeader = _fHeader;
            Font fSmall = _fSmall;
            Font fMono = _fMono;
            Font fBigMono = _fBigMono;
            Font fIconAa = _fIconAa;
            {
                StringFormat leftFmt = _leftFmt;
                StringFormat rightFmt = _rightFmt;
                StringFormat centerFmt = _centerFmt;

                int pad = (int)Math.Round(10 * s);
                int topY = (int)Math.Round(6 * s);
                int topBarH = (int)Math.Round(20 * s);

                // Top Header Strip: G61 Source + Clickable Design Style Pill (Option A / B / C)
                string styleName = _settings.Garage61Style == Garage61LayoutStyle.OptionA_LookaheadTraceAndPedals
                    ? "STYLE A: TRACE+PEDALS"
                    : (_settings.Garage61Style == Garage61LayoutStyle.OptionB_CompactBrakeCoach ? "STYLE B: COMPACT BAR" : "STYLE C: PRO+AUDIT");

                int stylePillW = (int)Math.Round(136 * s);
                _rectStyleCycleBtn = new Rectangle(pad, topY, stylePillW, topBarH);
                using (SolidBrush sb = new SolidBrush(Color.FromArgb(40, 56, 189, 248))) g.FillRectangle(sb, _rectStyleCycleBtn);
                using (Pen sp = new Pen(Color.FromArgb(140, 56, 189, 248), 1f)) g.DrawRectangle(sp, _rectStyleCycleBtn);
                using (SolidBrush sf = new SolidBrush(Color.FromArgb(125, 211, 252))) g.DrawString(styleName, fSmall, sf, _rectStyleCycleBtn, centerFmt);

                int audioPillW = (int)Math.Round(78 * s);
                _rectAudioToggleBtn = new Rectangle(_rectStyleCycleBtn.Right + (int)Math.Round(6 * s), topY, audioPillW, topBarH);
                Color audBg = _settings.Garage61AudioEnabled ? Color.FromArgb(45, 34, 197, 94) : Color.FromArgb(30, 107, 114, 128);
                Color audBorder = _settings.Garage61AudioEnabled ? Color.FromArgb(150, 74, 222, 128) : Color.FromArgb(80, 156, 163, 175);
                Color audFg = _settings.Garage61AudioEnabled ? Color.FromArgb(134, 239, 172) : Color.FromArgb(156, 163, 175);
                using (SolidBrush ab = new SolidBrush(audBg)) g.FillRectangle(ab, _rectAudioToggleBtn);
                using (Pen ap = new Pen(audBorder, 1f)) g.DrawRectangle(ap, _rectAudioToggleBtn);
                using (SolidBrush af = new SolidBrush(audFg))
                {
                    g.DrawString(_settings.Garage61AudioEnabled ? "AUDIO: ON" : "AUDIO: OFF", fSmall, af, _rectAudioToggleBtn, centerFmt);
                }

                g.DrawString(_g61.SourceLabel, fHeader, Brushes.Gainsboro,
                    new RectangleF(_rectAudioToggleBtn.Right + 8 * s, topY, Width * 0.40f, topBarH), leftFmt);

                int rightReserved = (_isHovered && !_settings.Locked) ? (int)Math.Round(30 * s) : 0;
                using (SolidBrush legB = new SolidBrush(Color.FromArgb(251, 113, 133)))
                {
                    g.DrawString("YOU (RED) vs G61 (CYAN)", fSmall, legB,
                        new RectangleF(Width - 200 * s - rightReserved, topY, 190 * s, topBarH), rightFmt);
                }

                // Braking Spot Countdown / Delta Banner
                int bannerY = topY + topBarH + (int)Math.Round(4 * s);
                int bannerH = (int)Math.Round(24 * s);
                Rectangle bannerRect = new Rectangle(pad, bannerY, Width - pad * 2, bannerH);
                DrawBrakingBanner(g, bannerRect, nowPct, youBrk, g61Brk, trackLen, fHeader, fMono, s);

                int bodyY = bannerRect.Bottom + (int)Math.Round(6 * s);
                int bodyH = Math.Max(30, Height - bodyY - (int)Math.Round(8 * s));

                if (_settings.Garage61Style == Garage61LayoutStyle.OptionB_CompactBrakeCoach)
                {
                    // ===============================================================
                    // OPTION B: COMPACT BRAKE COACH BAR (Full-Width Brake Matcher)
                    // ===============================================================
                    int lblH = (int)Math.Round(18 * s);
                    string leftB = string.Format(CultureInfo.InvariantCulture, "LIVE BRAKE: {0:0}%", youBrk);
                    string rightB = string.Format(CultureInfo.InvariantCulture, "G61 TARGET NOW: {0:0}%  •  CORNER PEAK: {1:0}%", g61Brk, peakTarget);
                    using (SolidBrush rb = new SolidBrush(Color.FromArgb(251, 113, 133)))
                        g.DrawString(leftB, fMono, rb, new RectangleF(pad, bodyY, Width * 0.4f, lblH), leftFmt);
                    using (SolidBrush cb = new SolidBrush(Color.FromArgb(125, 211, 252)))
                        g.DrawString(rightB, fMono, cb, new RectangleF(Width * 0.35f, bodyY, Width * 0.65f - pad, lblH), rightFmt);

                    int barY = bodyY + lblH + (int)Math.Round(3 * s);
                    int barH = Math.Max((int)Math.Round(18 * s), Height - barY - (int)Math.Round(8 * s));
                    Rectangle barR = new Rectangle(pad, barY, Width - pad * 2, barH);
                    using (SolidBrush bbg = new SolidBrush(Color.FromArgb(20, 25, 37))) g.FillRectangle(bbg, barR);

                    int youFillW = (int)Math.Round((youBrk / 100.0f) * barR.Width);
                    if (youFillW > 1)
                    {
                        using (SolidBrush yfb = new SolidBrush(Color.FromArgb(225, 225, 29, 72)))
                            g.FillRectangle(yfb, barR.Left + 1, barR.Top + 1, youFillW, barR.Height - 2);
                    }

                    // G61 Live Target Cursor (Thick Cyan Vertical Marker)
                    int g61CurX = barR.Left + (int)Math.Round((g61Brk / 100.0f) * barR.Width);
                    using (SolidBrush cCur = new SolidBrush(Color.FromArgb(125, 211, 252)))
                    {
                        g.FillRectangle(cCur, Math.Max(barR.Left, g61CurX - 2), barR.Top, Math.Max(3, (int)Math.Round(4 * s)), barR.Height);
                    }

                    // Corner Peak Target Line (Amber)
                    int peakX = barR.Left + (int)Math.Round((peakTarget / 100.0f) * barR.Width);
                    using (Pen pkPen = new Pen(Color.FromArgb(253, 224, 71), Math.Max(1.5f, 2.0f * s)))
                    {
                        g.DrawLine(pkPen, peakX, barR.Top, peakX, barR.Bottom);
                    }

                    using (Pen bPen = new Pen(Color.FromArgb(65, 255, 255, 255), 1f)) g.DrawRectangle(bPen, barR);
                }
                else if (_settings.Garage61Style == Garage61LayoutStyle.OptionA_LookaheadTraceAndPedals)
                {
                    // ===============================================================
                    // OPTION A: LOOKAHEAD TRACE + LIVE PEDAL BARS
                    // ===============================================================
                    int leftW = (int)Math.Round(Width * 0.34f);
                    int gapX = (int)Math.Round(8 * s);
                    Rectangle leftBox = new Rectangle(pad, bodyY, leftW, bodyH);
                    Rectangle graphBox = new Rectangle(leftBox.Right + gapX, bodyY, Width - pad - (leftBox.Right + gapX), bodyH);

                    using (SolidBrush lbg = new SolidBrush(Color.FromArgb(17, 22, 33))) g.FillRectangle(lbg, leftBox);
                    using (Pen lbp = new Pen(Color.FromArgb(45, 255, 255, 255), 1f)) g.DrawRectangle(lbp, leftBox);

                    int inPad = (int)Math.Round(8 * s);
                    int curY = leftBox.Top + inPad;
                    int innerW = leftBox.Width - inPad * 2;

                    // 1. Brake Pressure Bar
                    using (SolidBrush brkLblB = new SolidBrush(Color.FromArgb(251, 113, 133)))
                        g.DrawString("BRAKE", fSmall, brkLblB, new RectangleF(leftBox.Left + inPad, curY, innerW * 0.5f, 15 * s), leftFmt);
                    string bValTxt = string.Format(CultureInfo.InvariantCulture, "{0:0}% / {1:0}%", youBrk, g61Brk);
                    g.DrawString(bValTxt, fMono, Brushes.White, new RectangleF(leftBox.Left + inPad, curY, innerW, 15 * s), rightFmt);
                    curY += (int)Math.Round(16 * s);

                    int brkBarH = (int)Math.Round(18 * s);
                    Rectangle brkBarR = new Rectangle(leftBox.Left + inPad, curY, innerW, brkBarH);
                    using (SolidBrush bbg = new SolidBrush(Color.FromArgb(10, 13, 20))) g.FillRectangle(bbg, brkBarR);
                    int yfW = (int)Math.Round((youBrk / 100.0f) * brkBarR.Width);
                    if (yfW > 1)
                    {
                        using (SolidBrush rb = new SolidBrush(Color.FromArgb(225, 225, 29, 72)))
                            g.FillRectangle(rb, brkBarR.Left + 1, brkBarR.Top + 1, yfW, brkBarR.Height - 2);
                    }
                    int gCurX = brkBarR.Left + (int)Math.Round((g61Brk / 100.0f) * brkBarR.Width);
                    using (SolidBrush cb = new SolidBrush(Color.FromArgb(56, 189, 248)))
                        g.FillRectangle(cb, Math.Max(brkBarR.Left, gCurX - 1), brkBarR.Top, Math.Max(2, (int)Math.Round(3 * s)), brkBarR.Height);
                    int pkX = brkBarR.Left + (int)Math.Round((peakTarget / 100.0f) * brkBarR.Width);
                    using (Pen pp = new Pen(Color.FromArgb(253, 224, 71), Math.Max(1.2f, 1.6f * s)))
                        g.DrawLine(pp, pkX, brkBarR.Top, pkX, brkBarR.Bottom);
                    using (Pen bp = new Pen(Color.FromArgb(55, 255, 255, 255), 1f)) g.DrawRectangle(bp, brkBarR);
                    curY += brkBarH + (int)Math.Round(8 * s);

                    // 2. Throttle Bar
                    using (SolidBrush thrLblB = new SolidBrush(Color.FromArgb(52, 211, 153)))
                        g.DrawString("THROTTLE", fSmall, thrLblB, new RectangleF(leftBox.Left + inPad, curY, innerW * 0.55f, 15 * s), leftFmt);
                    string tValTxt = string.Format(CultureInfo.InvariantCulture, "{0:0}% / {1:0}%", youThr, g61Thr);
                    g.DrawString(tValTxt, fMono, Brushes.White, new RectangleF(leftBox.Left + inPad, curY, innerW, 15 * s), rightFmt);
                    curY += (int)Math.Round(16 * s);

                    int thrBarH = (int)Math.Round(14 * s);
                    Rectangle thrBarR = new Rectangle(leftBox.Left + inPad, curY, innerW, thrBarH);
                    using (SolidBrush tbg = new SolidBrush(Color.FromArgb(10, 13, 20))) g.FillRectangle(tbg, thrBarR);
                    int tfW = (int)Math.Round((youThr / 100.0f) * thrBarR.Width);
                    if (tfW > 1)
                    {
                        using (SolidBrush eb = new SolidBrush(Color.FromArgb(215, 16, 185, 129)))
                            g.FillRectangle(eb, thrBarR.Left + 1, thrBarR.Top + 1, tfW, thrBarR.Height - 2);
                    }
                    int gtCurX = thrBarR.Left + (int)Math.Round((g61Thr / 100.0f) * thrBarR.Width);
                    using (SolidBrush cb = new SolidBrush(Color.FromArgb(56, 189, 248)))
                        g.FillRectangle(cb, Math.Max(thrBarR.Left, gtCurX - 1), thrBarR.Top, Math.Max(2, (int)Math.Round(3 * s)), thrBarR.Height);
                    using (Pen tp = new Pen(Color.FromArgb(55, 255, 255, 255), 1f)) g.DrawRectangle(tp, thrBarR);
                    curY += thrBarH + (int)Math.Round(8 * s);

                    // 3. Speed & Gear row at bottom of Left Box
                    float refSpd = relZone != null ? relZone.MinSpeedKph : _g61.GetRefSpeedAtPct(nowPct);
                    int refGear = relZone != null ? relZone.MinGear : _g61.GetRefGearAtPct(nowPct);
                    string spdStr = string.Format(CultureInfo.InvariantCulture, "SPD {0:0}/{1:0} kph", _snapshot.PlayerSpeedKph, refSpd);
                    string gearStr = string.Format(CultureInfo.InvariantCulture, "G{0} (Ref G{1})", _snapshot.PlayerGear, refGear);
                    g.DrawString(spdStr, fSmall, Brushes.Gainsboro, new RectangleF(leftBox.Left + inPad, leftBox.Bottom - 20 * s, innerW * 0.65f, 18 * s), leftFmt);
                    using (SolidBrush gb = new SolidBrush(Color.FromArgb(253, 224, 71)))
                        g.DrawString(gearStr, fSmall, gb, new RectangleF(leftBox.Left + inPad, leftBox.Bottom - 20 * s, innerW, 18 * s), rightFmt);

                    // Draw Right Spatial Lookahead Graph
                    DrawSpatialLookaheadGraph(g, graphBox, nowPct, trackLen, peakTarget, fSmall, s);
                }
                else
                {
                    // ===============================================================
                    // OPTION C: PRO BOX + CORNER-BY-CORNER AUDIT STRIP
                    // ===============================================================
                    int auditH = (int)Math.Round(26 * s);
                    int midH = Math.Max(40, bodyH - auditH - (int)Math.Round(6 * s));
                    int leftW = (int)Math.Round(Width * 0.27f);
                    int gapX = (int)Math.Round(8 * s);

                    Rectangle leftBox = new Rectangle(pad, bodyY, leftW, midH);
                    Rectangle graphBox = new Rectangle(leftBox.Right + gapX, bodyY, Width - pad - (leftBox.Right + gapX), midH);

                    using (SolidBrush lbg = new SolidBrush(Color.FromArgb(17, 22, 33))) g.FillRectangle(lbg, leftBox);
                    using (Pen lbp = new Pen(Color.FromArgb(45, 255, 255, 255), 1f)) g.DrawRectangle(lbp, leftBox);

                    int inPad = (int)Math.Round(8 * s);
                    int rowStep = midH / 3;
                    g.DrawString("BRAKE (YOU / G61)", fSmall, Brushes.Gray, new PointF(leftBox.Left + inPad, leftBox.Top + 3 * s));
                    string brkReadout = string.Format(CultureInfo.InvariantCulture, "{0:0}% / {1:0}%", youBrk, g61Brk);
                    using (SolidBrush rb = new SolidBrush(Color.FromArgb(251, 113, 133)))
                        g.DrawString(brkReadout, fBigMono, rb, new PointF(leftBox.Left + inPad, leftBox.Top + 15 * s));

                    float spotD = _g61.HasCurrentBrakeSpotDelta ? _g61.CurrentBrakeSpotDeltaMeters : -1.4f;
                    g.DrawString("BRAKE SPOT DELTA", fSmall, Brushes.Gray, new PointF(leftBox.Left + inPad, leftBox.Top + rowStep + 2 * s));
                    string spotReadout = string.Format(CultureInfo.InvariantCulture, "{0}{1:0.0}m {2}", spotD >= 0f ? "+" : "-", Math.Abs(spotD), spotD >= 0f ? "LATE" : "EARLY");
                    using (SolidBrush ab = new SolidBrush(Color.FromArgb(253, 224, 71)))
                        g.DrawString(spotReadout, fMono, ab, new PointF(leftBox.Left + inPad, leftBox.Top + rowStep + 15 * s));

                    g.DrawString("CORNER PEAK TARGET", fSmall, Brushes.Gray, new PointF(leftBox.Left + inPad, leftBox.Top + rowStep * 2 + 2 * s));
                    string pkReadout = string.Format(CultureInfo.InvariantCulture, "Target {0:0}%", peakTarget);
                    using (SolidBrush eb = new SolidBrush(Color.FromArgb(74, 222, 128)))
                        g.DrawString(pkReadout, fMono, eb, new PointF(leftBox.Left + inPad, leftBox.Top + rowStep * 2 + 15 * s));

                    DrawSpatialLookaheadGraph(g, graphBox, nowPct, trackLen, peakTarget, fSmall, s);

                    // Bottom 3-Corner Audit Strip
                    int auditY = bodyY + midH + (int)Math.Round(6 * s);
                    int colGap = (int)Math.Round(6 * s);
                    int colW = (Width - pad * 2 - colGap * 2) / 3;
                    int auditCount = _g61.Audits.Count;

                    for (int c = 0; c < 3; c++)
                    {
                        int cx = pad + c * (colW + colGap);
                        int w = (c == 2) ? (Width - pad - cx) : colW;
                        Rectangle cRect = new Rectangle(cx, auditY, w, auditH);

                        using (SolidBrush cbg = new SolidBrush(Color.FromArgb(18, 24, 36))) g.FillRectangle(cbg, cRect);
                        using (Pen cp = new Pen(Color.FromArgb(55, 255, 255, 255), 1f)) g.DrawRectangle(cp, cRect);

                        if (auditCount > 0)
                        {
                            CornerAuditItem item = _g61.Audits[c % auditCount];
                            g.DrawString(item.CornerLabel, fSmall, Brushes.Gainsboro,
                                new RectangleF(cRect.Left + 6 * s, cRect.Top, cRect.Width * 0.45f, cRect.Height), leftFmt);

                            string score = string.Format(
                                CultureInfo.InvariantCulture,
                                "Pt {0}{1:0.0}m • Pk {2:0}/{3:0}%",
                                item.BrakeSpotDeltaMeters >= 0f ? "+" : "-",
                                Math.Abs(item.BrakeSpotDeltaMeters),
                                item.PlayerPeakBrake,
                                item.RefPeakBrake
                            );
                            Color scCol = Math.Abs(item.BrakeSpotDeltaMeters) <= 2.5f ? Color.FromArgb(74, 222, 128) : Color.FromArgb(253, 224, 71);
                            using (SolidBrush sb = new SolidBrush(scCol))
                            {
                                g.DrawString(score, fSmall, sb,
                                    new RectangleF(cRect.Left + cRect.Width * 0.40f, cRect.Top, cRect.Width * 0.60f - 6 * s, cRect.Height), rightFmt);
                            }
                        }
                    }
                }

                // SimLab-style Single Top-Right Icon Toggle Button + Corner Grips on Hover
                if (_isHovered && !_settings.Locked)
                {
                    int iconSz = Math.Max(20, (int)Math.Round(24 * s));
                    _rectCornerModeBtn = new Rectangle(
                        Width - iconSz - (int)Math.Round(6 * s),
                        (int)Math.Round(5 * s),
                        iconSz,
                        iconSz
                    );
                    DpiHelper.DrawCornerModeIconBtn(
                        g,
                        fIconAa,
                        _rectCornerModeBtn,
                        _cornerMode == CornerDragMode.ResizeWindow,
                        s
                    );

                    Color activeAccent = _cornerMode == CornerDragMode.ResizeWindow
                        ? Color.FromArgb(215, 56, 189, 248)
                        : Color.FromArgb(215, 192, 132, 252);

                    using (Pen bp = new Pen(activeAccent, Math.Max(1.5f, 1.5f * s)))
                    {
                        g.DrawRectangle(bp, 1, 1, Width - 2, Height - 2);
                    }
                    DpiHelper.DrawCornerGrips(g, Width, Height, activeAccent, s);
                }
                else
                {
                    _rectCornerModeBtn = Rectangle.Empty;
                    Color borderCol = _settings.Locked ? Color.FromArgb(45, 255, 255, 255) : Color.FromArgb(80, 255, 255, 255);
                    using (Pen bp = new Pen(borderCol, 1.0f))
                    {
                        g.DrawRectangle(bp, 0, 0, Width - 1, Height - 1);
                    }
                }
            }
        }
    }

    public class ControlCenterForm : Form
    {
        private const int WM_HOTKEY = 0x0312;
        private const int WM_DPICHANGED = 0x02E0;
        private const int WM_NCLBUTTONDOWN = 0x00A1;
        private const int HT_CAPTION = 0x02;

        private const int HOTKEY_HUB = 9101;   // F5
        private const int HOTKEY_LOCK = 9102;  // F6
        private const int HOTKEY_MODE = 9103;  // F7
        private const int HOTKEY_QUIT = 9104;  // F8

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("winmm.dll")]
        private static extern uint timeBeginPeriod(uint uPeriod);

        [DllImport("winmm.dll")]
        private static extern uint timeEndPeriod(uint uPeriod);

        private readonly OverlaySettings _settings;
        private readonly MockSimulator _mockSim;
        private readonly LiveIracingReader _liveReader;
        private readonly Garage61LapManager _g61Manager;
        private readonly OverlayForm _standingsForm;
        private readonly DeltaBarForm _deltaBarForm;
        private readonly Garage61OverlayForm _garage61Form;
        private readonly Timer _timer;
        private readonly Stopwatch _frameStopwatch;
        private readonly NotifyIcon _trayIcon;

        private SessionSnapshot _lastSnapshot;
        private float _dpiScale = 1.0f;
        private int _tickCounter = 0;
        private bool _lastHubConnected = false;
        private bool _lastHubIsLive = false;
        private int _activeTab = 0; // 0 = Standings, 1 = Delta Bar, 2 = Garage61 Coach

        // Interactive Hub hit rectangles
        private Rectangle _btnLockAll;
        private Rectangle _btnCycleMode;
        private Rectangle _btnHideHub;
        private Rectangle _btnQuitApp;

        // Top Widget Tabs
        private Rectangle _tabStandings;
        private Rectangle _tabDeltaBar;
        private Rectangle _tabGarage61;

        // Standings Tab controls
        private Rectangle _btnStandingsToggle;
        private Rectangle _btnStandingsAutoOpen;
        private Rectangle _btnStandingsCarNum;
        private Rectangle _btnStandingsGap;
        private Rectangle _btnStandingsCompactIr;
        private Rectangle _btnStandingsDeltaIr;
        private Rectangle _btnStandingsFontMinus;
        private Rectangle _btnStandingsFontPlus;
        private Rectangle _btnStandingsOpacMinus;
        private Rectangle _btnStandingsOpacPlus;
        private Rectangle _btnStandingsReset;

        // Delta Bar Tab controls
        private Rectangle _btnDeltaToggle;
        private Rectangle _btnDeltaAutoOpen;
        private Rectangle _btnDeltaRefBest;
        private Rectangle _btnDeltaRefOpt;
        private Rectangle _btnDeltaRefPers;
        private Rectangle _btnDeltaRefLast;
        private Rectangle _btnDeltaRange1;
        private Rectangle _btnDeltaRange2;
        private Rectangle _btnDeltaRange5;
        private Rectangle _btnDeltaSectors;
        private Rectangle _btnDeltaFontMinus;
        private Rectangle _btnDeltaFontPlus;
        private Rectangle _btnDeltaOpacMinus;
        private Rectangle _btnDeltaOpacPlus;
        private Rectangle _btnDeltaReset;

        // Garage61 Tab controls
        private Rectangle _btnG61Toggle;
        private Rectangle _btnG61AutoOpen;
        private Rectangle _btnG61StyleA;
        private Rectangle _btnG61StyleB;
        private Rectangle _btnG61StyleC;
        private Rectangle _btnG61AudioToggle;
        private Rectangle _btnG61AudioTest;
        private Rectangle _btnG61Look80;
        private Rectangle _btnG61Look120;
        private Rectangle _btnG61Look180;
        private Rectangle _btnG61LoadCsv;
        private Rectangle _btnG61DefaultLap;
        private Rectangle _btnG61FontMinus;
        private Rectangle _btnG61FontPlus;
        private Rectangle _btnG61OpacMinus;
        private Rectangle _btnG61OpacPlus;
        private Rectangle _btnG61Reset;

        public ControlCenterForm()
        {
            _settings = new OverlaySettings();
            _settings.Load();

            _mockSim = new MockSimulator();
            _liveReader = new LiveIracingReader();
            _g61Manager = new Garage61LapManager();
            if (!string.IsNullOrEmpty(_settings.Garage61CsvPath) && File.Exists(_settings.Garage61CsvPath))
            {
                _g61Manager.LoadFromCsvFile(_settings.Garage61CsvPath);
            }

            _lastSnapshot = _mockSim.Tick(0.05);
            _g61Manager.UpdatePlayerTelemetry(_lastSnapshot.PlayerLapDistPct, _lastSnapshot.PlayerBrakePct, _lastSnapshot.PlayerThrottlePct, _lastSnapshot.TrackLengthMeters);

            _standingsForm = new OverlayForm(_settings, _lastSnapshot);
            _deltaBarForm = new DeltaBarForm(_settings, _lastSnapshot);
            _garage61Form = new Garage61OverlayForm(_settings, _g61Manager, _lastSnapshot);

            _settings.Changed += OnSettingsChanged;

            Text = "Free Overlay Hub";
            AutoScaleMode = AutoScaleMode.None;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = true;
            DoubleBuffered = true;
            BackColor = Color.FromArgb(11, 14, 21);

            _dpiScale = DpiHelper.GetScale(IntPtr.Zero);
            Width = (int)Math.Round(1180 * _dpiScale);
            Height = (int)Math.Round(590 * _dpiScale);

            ContextMenu trayMenu = new ContextMenu();
            trayMenu.MenuItems.Add("Open Free Overlay Hub (F5)", delegate(object s, EventArgs e) { ToggleHubVisibility(); });
            trayMenu.MenuItems.Add("Toggle Standings Overlay", delegate(object s, EventArgs e)
            {
                _settings.StandingsEnabled = !_settings.StandingsEnabled;
                _settings.StandingsAutoOpen = _settings.StandingsEnabled;
                _settings.NotifyChanged();
            });
            trayMenu.MenuItems.Add("Toggle Delta Bar Overlay", delegate(object s, EventArgs e)
            {
                _settings.DeltaBarEnabled = !_settings.DeltaBarEnabled;
                _settings.DeltaBarAutoOpen = _settings.DeltaBarEnabled;
                _settings.NotifyChanged();
            });
            trayMenu.MenuItems.Add("Toggle Garage61 Coach Overlay", delegate(object s, EventArgs e)
            {
                _settings.Garage61Enabled = !_settings.Garage61Enabled;
                _settings.Garage61AutoOpen = _settings.Garage61Enabled;
                _settings.NotifyChanged();
            });
            trayMenu.MenuItems.Add("Toggle Braking Audio Cues", delegate(object s, EventArgs e)
            {
                _settings.Garage61AudioEnabled = !_settings.Garage61AudioEnabled;
                _settings.NotifyChanged();
                if (_settings.Garage61AudioEnabled) BrakingAudioCoach.PlayTestSequence();
            });
            trayMenu.MenuItems.Add("Lock / Unlock Overlays (F6)", delegate(object s, EventArgs e)
            {
                _settings.Locked = !_settings.Locked;
                _settings.NotifyChanged();
            });
            trayMenu.MenuItems.Add("-");
            trayMenu.MenuItems.Add("Quit Free Overlay (F8)", delegate(object s, EventArgs e) { ExitAll(); });

            _trayIcon = new NotifyIcon();
            _trayIcon.Text = "Free Overlay Hub (F5: Hub, F6: Lock)";
            _trayIcon.Icon = SystemIcons.Application;
            _trayIcon.ContextMenu = trayMenu;
            _trayIcon.Visible = true;
            _trayIcon.DoubleClick += delegate(object s, EventArgs e)
            {
                Show();
                WindowState = FormWindowState.Normal;
                Activate();
            };

            timeBeginPeriod(1);
            _frameStopwatch = Stopwatch.StartNew();

            _timer = new Timer();
            _timer.Interval = 16; // ~60 FPS high-rate update matching iRacing 60 Hz telemetry
            _timer.Tick += OnTimerTick;
            _timer.Start();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            _dpiScale = DpiHelper.GetScale(Handle);
            Width = (int)Math.Round(1180 * _dpiScale);
            Height = (int)Math.Round(590 * _dpiScale);

            RegisterHotKey(Handle, HOTKEY_HUB, 0, (uint)Keys.F5);
            RegisterHotKey(Handle, HOTKEY_LOCK, 0, (uint)Keys.F6);
            RegisterHotKey(Handle, HOTKEY_MODE, 0, (uint)Keys.F7);
            RegisterHotKey(Handle, HOTKEY_QUIT, 0, (uint)Keys.F8);

            SyncOverlaysVisibilityAndState();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_standingsForm != null) _standingsForm.SaveGeometry();
            if (_deltaBarForm != null) _deltaBarForm.SaveGeometry();
            if (_garage61Form != null) _garage61Form.SaveGeometry();
            _settings.Save();
            timeEndPeriod(1);
            UnregisterHotKey(Handle, HOTKEY_HUB);
            UnregisterHotKey(Handle, HOTKEY_LOCK);
            UnregisterHotKey(Handle, HOTKEY_MODE);
            UnregisterHotKey(Handle, HOTKEY_QUIT);
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
            }
            base.OnFormClosing(e);
        }

        private void ExitAll()
        {
            if (_standingsForm != null) _standingsForm.SaveGeometry();
            if (_deltaBarForm != null) _deltaBarForm.SaveGeometry();
            if (_garage61Form != null) _garage61Form.SaveGeometry();
            _settings.Save();
            _timer.Stop();
            timeEndPeriod(1);
            if (_trayIcon != null) _trayIcon.Visible = false;
            Application.Exit();
        }

        private void ToggleHubVisibility()
        {
            if (Visible && WindowState != FormWindowState.Minimized)
            {
                Hide();
            }
            else
            {
                Show();
                WindowState = FormWindowState.Normal;
                Activate();
                Invalidate();
            }
        }

        private void OnSettingsChanged(object sender, EventArgs e)
        {
            SyncOverlaysVisibilityAndState();
            if (Visible) Invalidate();
        }

        private void SyncOverlaysVisibilityAndState()
        {
            if (_settings.StandingsEnabled)
            {
                if (!_standingsForm.Visible) _standingsForm.Show();
                _standingsForm.ApplySettingsState();
            }
            else
            {
                if (_standingsForm.Visible) _standingsForm.Hide();
            }

            if (_settings.DeltaBarEnabled)
            {
                if (!_deltaBarForm.Visible) _deltaBarForm.Show();
                _deltaBarForm.ApplySettingsState();
            }
            else
            {
                if (_deltaBarForm.Visible) _deltaBarForm.Hide();
            }

            if (_settings.Garage61Enabled)
            {
                if (!_garage61Form.Visible) _garage61Form.Show();
                _garage61Form.ApplySettingsState();
            }
            else
            {
                if (_garage61Form.Visible) _garage61Form.Hide();
            }
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            _tickCounter++;
            double dtSec = _frameStopwatch.Elapsed.TotalSeconds;
            _frameStopwatch.Restart();
            if (dtSec <= 0.001 || dtSec > 0.25) dtSec = 0.01667;

            bool anyOverlayEnabled = _settings.StandingsEnabled || _settings.DeltaBarEnabled || _settings.Garage61Enabled;
            int targetInterval = anyOverlayEnabled ? 16 : 200;
            if (_timer.Interval != targetInterval)
            {
                _timer.Interval = targetInterval;
            }

            if (!anyOverlayEnabled && !Visible)
            {
                return;
            }

            bool needStandingsRebuild = _settings.StandingsEnabled && (_tickCounter % 12 == 0);
            SessionSnapshot snap = null;

            if (_settings.Mode == TelemetryMode.Live || _settings.Mode == TelemetryMode.Auto)
            {
                snap = _liveReader.TryReadLive(needStandingsRebuild);
            }

            if (snap == null)
            {
                if (_settings.Mode == TelemetryMode.Live)
                {
                    snap = new SessionSnapshot();
                    snap.Connected = false;
                    snap.IsLiveSource = true;
                    snap.TrackName = "Waiting for iRacing Simulation...";
                    snap.SessionType = "WAIT";
                }
                else
                {
                    snap = _mockSim.Tick(dtSec);
                }
            }

            _lastSnapshot = snap;

            if (_settings.Garage61Enabled)
            {
                bool allowAudio = _settings.Garage61AudioEnabled && snap.Connected;
                _g61Manager.UpdatePlayerTelemetry(snap.PlayerLapDistPct, snap.PlayerBrakePct, snap.PlayerThrottlePct, snap.TrackLengthMeters, snap.PlayerSpeedKph, allowAudio);
            }

            // Repaint Delta Bar & Garage61 at 30 FPS (every 2nd tick = 32ms) while sampling telemetry & audio at 60 Hz
            if (_tickCounter % 2 == 0)
            {
                if (_settings.DeltaBarEnabled && _deltaBarForm.Visible)
                {
                    _deltaBarForm.UpdateSnapshot(snap);
                }

                if (_settings.Garage61Enabled && _garage61Form.Visible)
                {
                    _garage61Form.UpdateSnapshot(snap);
                }
            }

            // Update Standings at ~5 Hz (every 12 ticks at 60 FPS = ~200ms)
            if (_settings.StandingsEnabled && _standingsForm.Visible && (_tickCounter % 12 == 0))
            {
                _standingsForm.UpdateSnapshot(snap);
            }

            // Only repaint ControlCenterForm when telemetry connection/source state changes
            if (Visible && (snap.Connected != _lastHubConnected || snap.IsLiveSource != _lastHubIsLive))
            {
                _lastHubConnected = snap.Connected;
                _lastHubIsLive = snap.IsLiveSource;
                Invalidate();
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                if (id == HOTKEY_HUB)
                {
                    ToggleHubVisibility();
                }
                else if (id == HOTKEY_LOCK)
                {
                    _settings.Locked = !_settings.Locked;
                    _settings.NotifyChanged();
                }
                else if (id == HOTKEY_MODE)
                {
                    _settings.Mode = (TelemetryMode)(((int)_settings.Mode + 1) % 3);
                    _settings.NotifyChanged();
                }
                else if (id == HOTKEY_QUIT)
                {
                    ExitAll();
                }
            }
            else if (m.Msg == WM_DPICHANGED)
            {
                int newDpi = m.WParam.ToInt32() & 0xFFFF;
                if (newDpi >= 72 && newDpi <= 480)
                {
                    _dpiScale = newDpi / 96.0f;
                    Width = (int)Math.Round(1180 * _dpiScale);
                    Height = (int)Math.Round(590 * _dpiScale);
                    Invalidate();
                }
            }
            base.WndProc(ref m);
        }

        private void PromptLoadGarage61Csv()
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Select Garage61 Exported Telemetry CSV";
                ofd.Filter = "CSV Telemetry Files (*.csv)|*.csv|All Files (*.*)|*.*";
                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    if (_g61Manager.LoadFromCsvFile(ofd.FileName))
                    {
                        _settings.Garage61CsvPath = ofd.FileName;
                        _settings.NotifyChanged();
                    }
                    else
                    {
                        MessageBox.Show(
                            this,
                            "Could not parse LapDistPct / Brake columns from the selected CSV file.\nMake sure it is a Garage61 or iRacing telemetry CSV export.",
                            "Free Overlay - Garage61 CSV Loader",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );
                    }
                }
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;

            Point pt = e.Location;

            // Header buttons
            if (_btnLockAll.Contains(pt))
            {
                _settings.Locked = !_settings.Locked;
                _settings.NotifyChanged();
                return;
            }
            if (_btnCycleMode.Contains(pt))
            {
                _settings.Mode = (TelemetryMode)(((int)_settings.Mode + 1) % 3);
                _settings.NotifyChanged();
                return;
            }
            if (_btnHideHub.Contains(pt))
            {
                Hide();
                return;
            }
            if (_btnQuitApp.Contains(pt))
            {
                ExitAll();
                return;
            }

            // Tab switching
            if (_tabStandings.Contains(pt))
            {
                _activeTab = 0;
                Invalidate();
                return;
            }
            if (_tabDeltaBar.Contains(pt))
            {
                _activeTab = 1;
                Invalidate();
                return;
            }
            if (_tabGarage61.Contains(pt))
            {
                _activeTab = 2;
                Invalidate();
                return;
            }

            // Active Tab Controls
            if (_activeTab == 0)
            {
                if (_btnStandingsToggle.Contains(pt))
                {
                    _settings.StandingsEnabled = !_settings.StandingsEnabled;
                    _settings.StandingsAutoOpen = _settings.StandingsEnabled;
                    _settings.NotifyChanged();
                    return;
                }
                if (_btnStandingsAutoOpen.Contains(pt))
                {
                    _settings.StandingsAutoOpen = !_settings.StandingsAutoOpen;
                    _settings.NotifyChanged();
                    return;
                }
                if (_btnStandingsCarNum.Contains(pt))
                {
                    _settings.ShowCarNumber = !_settings.ShowCarNumber;
                    _settings.NotifyChanged();
                    return;
                }
                if (_btnStandingsGap.Contains(pt))
                {
                    _settings.ShowInterval = !_settings.ShowInterval;
                    _settings.NotifyChanged();
                    return;
                }
                if (_btnStandingsCompactIr.Contains(pt))
                {
                    _settings.CompactIR = !_settings.CompactIR;
                    _settings.NotifyChanged();
                    return;
                }
                if (_btnStandingsDeltaIr.Contains(pt))
                {
                    _settings.ShowDeltaIR = !_settings.ShowDeltaIR;
                    _settings.NotifyChanged();
                    return;
                }
                if (_btnStandingsFontMinus.Contains(pt))
                {
                    _settings.StandingsFontScale = Math.Max(0.65f, (float)Math.Round(_settings.StandingsFontScale - 0.05f, 2));
                    _settings.NotifyChanged();
                    return;
                }
                if (_btnStandingsFontPlus.Contains(pt))
                {
                    _settings.StandingsFontScale = Math.Min(1.75f, (float)Math.Round(_settings.StandingsFontScale + 0.05f, 2));
                    _settings.NotifyChanged();
                    return;
                }
                if (_btnStandingsOpacMinus.Contains(pt))
                {
                    _settings.StandingsOpacity = Math.Max(0.45f, (float)Math.Round(_settings.StandingsOpacity - 0.05f, 2));
                    _settings.NotifyChanged();
                    return;
                }
                if (_btnStandingsOpacPlus.Contains(pt))
                {
                    _settings.StandingsOpacity = Math.Min(1.0f, (float)Math.Round(_settings.StandingsOpacity + 0.05f, 2));
                    _settings.NotifyChanged();
                    return;
                }
                if (_btnStandingsReset.Contains(pt))
                {
                    _settings.StandingsFontScale = 1.0f;
                    _standingsForm.ResetPositionAndSize(true);
                    return;
                }
            }
            else if (_activeTab == 1)
            {
                if (_btnDeltaToggle.Contains(pt))
                {
                    _settings.DeltaBarEnabled = !_settings.DeltaBarEnabled;
                    _settings.DeltaBarAutoOpen = _settings.DeltaBarEnabled;
                    _settings.NotifyChanged();
                    return;
                }
                if (_btnDeltaAutoOpen.Contains(pt))
                {
                    _settings.DeltaBarAutoOpen = !_settings.DeltaBarAutoOpen;
                    _settings.NotifyChanged();
                    return;
                }
                if (_btnDeltaRefBest.Contains(pt))
                {
                    _settings.DeltaRefMode = DeltaReferenceMode.SessionBest;
                    _settings.NotifyChanged();
                    return;
                }
                if (_btnDeltaRefOpt.Contains(pt))
                {
                    _settings.DeltaRefMode = DeltaReferenceMode.SessionOptimal;
                    _settings.NotifyChanged();
                    return;
                }
                if (_btnDeltaRefPers.Contains(pt))
                {
                    _settings.DeltaRefMode = DeltaReferenceMode.PersonalBest;
                    _settings.NotifyChanged();
                    return;
                }
                if (_btnDeltaRefLast.Contains(pt))
                {
                    _settings.DeltaRefMode = DeltaReferenceMode.LastLap;
                    _settings.NotifyChanged();
                    return;
                }
                if (_btnDeltaRange1.Contains(pt))
                {
                    _settings.DeltaBarRangeSec = 1.0f;
                    _settings.NotifyChanged();
                    return;
                }
                if (_btnDeltaRange2.Contains(pt))
                {
                    _settings.DeltaBarRangeSec = 2.0f;
                    _settings.NotifyChanged();
                    return;
                }
                if (_btnDeltaRange5.Contains(pt))
                {
                    _settings.DeltaBarRangeSec = 5.0f;
                    _settings.NotifyChanged();
                    return;
                }
                if (_btnDeltaSectors.Contains(pt))
                {
                    _settings.ShowSectorSplits = !_settings.ShowSectorSplits;
                    _deltaBarForm.ResetPositionAndSize(true);
                    return;
                }
                if (_btnDeltaFontMinus.Contains(pt))
                {
                    _settings.DeltaFontScale = Math.Max(0.65f, (float)Math.Round(_settings.DeltaFontScale - 0.05f, 2));
                    _settings.NotifyChanged();
                    return;
                }
                if (_btnDeltaFontPlus.Contains(pt))
                {
                    _settings.DeltaFontScale = Math.Min(1.75f, (float)Math.Round(_settings.DeltaFontScale + 0.05f, 2));
                    _settings.NotifyChanged();
                    return;
                }
                if (_btnDeltaOpacMinus.Contains(pt))
                {
                    _settings.DeltaOpacity = Math.Max(0.45f, (float)Math.Round(_settings.DeltaOpacity - 0.05f, 2));
                    _settings.NotifyChanged();
                    return;
                }
                if (_btnDeltaOpacPlus.Contains(pt))
                {
                    _settings.DeltaOpacity = Math.Min(1.0f, (float)Math.Round(_settings.DeltaOpacity + 0.05f, 2));
                    _settings.NotifyChanged();
                    return;
                }
                if (_btnDeltaReset.Contains(pt))
                {
                    _settings.DeltaFontScale = 1.0f;
                    _deltaBarForm.ResetPositionAndSize(true);
                    return;
                }
            }
            else if (_activeTab == 2)
            {
                if (_btnG61Toggle.Contains(pt))
                {
                    _settings.Garage61Enabled = !_settings.Garage61Enabled;
                    _settings.Garage61AutoOpen = _settings.Garage61Enabled;
                    _settings.NotifyChanged();
                    return;
                }
                if (_btnG61AutoOpen.Contains(pt))
                {
                    _settings.Garage61AutoOpen = !_settings.Garage61AutoOpen;
                    _settings.NotifyChanged();
                    return;
                }
                if (_btnG61StyleA.Contains(pt))
                {
                    _settings.Garage61Style = Garage61LayoutStyle.OptionA_LookaheadTraceAndPedals;
                    _garage61Form.ApplyDefaultSizeForCurrentStyle(true);
                    return;
                }
                if (_btnG61StyleB.Contains(pt))
                {
                    _settings.Garage61Style = Garage61LayoutStyle.OptionB_CompactBrakeCoach;
                    _garage61Form.ApplyDefaultSizeForCurrentStyle(true);
                    return;
                }
                if (_btnG61StyleC.Contains(pt))
                {
                    _settings.Garage61Style = Garage61LayoutStyle.OptionC_ProBoxWithCornerAudit;
                    _garage61Form.ApplyDefaultSizeForCurrentStyle(true);
                    return;
                }
                if (_btnG61AudioToggle.Contains(pt))
                {
                    _settings.Garage61AudioEnabled = !_settings.Garage61AudioEnabled;
                    _settings.NotifyChanged();
                    if (_settings.Garage61AudioEnabled)
                    {
                        BrakingAudioCoach.PlayTestSequence();
                    }
                    return;
                }
                if (_btnG61AudioTest.Contains(pt))
                {
                    BrakingAudioCoach.PlayTestSequence();
                    return;
                }
                if (_btnG61Look80.Contains(pt))
                {
                    _settings.Garage61LookaheadMeters = 80;
                    _settings.NotifyChanged();
                    return;
                }
                if (_btnG61Look120.Contains(pt))
                {
                    _settings.Garage61LookaheadMeters = 120;
                    _settings.NotifyChanged();
                    return;
                }
                if (_btnG61Look180.Contains(pt))
                {
                    _settings.Garage61LookaheadMeters = 180;
                    _settings.NotifyChanged();
                    return;
                }
                if (_btnG61LoadCsv.Contains(pt))
                {
                    PromptLoadGarage61Csv();
                    return;
                }
                if (_btnG61DefaultLap.Contains(pt))
                {
                    _settings.Garage61CsvPath = "";
                    _g61Manager.BuildDefaultSpaReferenceLap();
                    _settings.NotifyChanged();
                    return;
                }
                if (_btnG61FontMinus.Contains(pt))
                {
                    _settings.Garage61FontScale = Math.Max(0.65f, (float)Math.Round(_settings.Garage61FontScale - 0.05f, 2));
                    _settings.NotifyChanged();
                    return;
                }
                if (_btnG61FontPlus.Contains(pt))
                {
                    _settings.Garage61FontScale = Math.Min(1.75f, (float)Math.Round(_settings.Garage61FontScale + 0.05f, 2));
                    _settings.NotifyChanged();
                    return;
                }
                if (_btnG61OpacMinus.Contains(pt))
                {
                    _settings.Garage61Opacity = Math.Max(0.45f, (float)Math.Round(_settings.Garage61Opacity - 0.05f, 2));
                    _settings.NotifyChanged();
                    return;
                }
                if (_btnG61OpacPlus.Contains(pt))
                {
                    _settings.Garage61Opacity = Math.Min(1.0f, (float)Math.Round(_settings.Garage61Opacity + 0.05f, 2));
                    _settings.NotifyChanged();
                    return;
                }
                if (_btnG61Reset.Contains(pt))
                {
                    _settings.Garage61FontScale = 1.0f;
                    _garage61Form.ResetPositionAndSize(true);
                    return;
                }
            }

            // Drag Hub window by top header bar
            if (pt.Y < (int)Math.Round(56 * _dpiScale))
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private static Rectangle DrawButton(Graphics g, Font f, string text, int x, int y, int w, int h, bool active, Color activeColor, float s)
        {
            Rectangle r = new Rectangle(x, y, w, h);
            Color bg = active
                ? Color.FromArgb(55, activeColor.R, activeColor.G, activeColor.B)
                : Color.FromArgb(26, 32, 46);
            Color border = active
                ? Color.FromArgb(190, activeColor.R, activeColor.G, activeColor.B)
                : Color.FromArgb(65, 255, 255, 255);
            Color fg = active ? Color.White : Color.FromArgb(209, 213, 219);

            using (SolidBrush b = new SolidBrush(bg)) g.FillRectangle(b, r);
            using (Pen p = new Pen(border, Math.Max(1f, 1.2f * s))) g.DrawRectangle(p, r);

            StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            using (SolidBrush tb = new SolidBrush(fg)) g.DrawString(text, f, tb, r, sf);
            return r;
        }

        private static Rectangle DrawTabButton(Graphics g, Font fTab, Font fBadge, string title, bool isOpenNow, bool isAutoOpen, int x, int y, int w, int h, bool selected, float s)
        {
            Rectangle r = new Rectangle(x, y, w, h);
            Color bg = selected ? Color.FromArgb(24, 32, 48) : Color.FromArgb(13, 17, 26);
            Color border = selected ? Color.FromArgb(210, 56, 189, 248) : Color.FromArgb(38, 255, 255, 255);

            using (SolidBrush b = new SolidBrush(bg)) g.FillRectangle(b, r);
            using (Pen p = new Pen(border, 1.0f)) g.DrawRectangle(p, r);

            if (selected)
            {
                using (SolidBrush accent = new SolidBrush(Color.FromArgb(56, 189, 248)))
                {
                    g.FillRectangle(accent, x, y, (int)Math.Max(3, Math.Round(4 * s)), h);
                }
            }

            StringFormat leftFmt = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
            StringFormat centerFmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

            int padX = (int)Math.Round(12 * s);
            Color titleCol = selected ? Color.White : Color.FromArgb(170, 180, 195);
            using (SolidBrush tb = new SolidBrush(titleCol))
            {
                g.DrawString(title, fTab, tb, new RectangleF(x + padX, y + (int)(5 * s), w - padX * 2, (int)(22 * s)), leftFmt);
            }

            // Status pills on bottom row of vertical tab item
            int badgeH = (int)Math.Round(18 * s);
            int badgeY = y + h - badgeH - (int)Math.Round(7 * s);
            int openW = (int)Math.Round(52 * s);
            int autoW = (int)Math.Round(68 * s);

            Rectangle openRect = new Rectangle(x + padX, badgeY, openW, badgeH);
            Color openBg = isOpenNow ? Color.FromArgb(45, 34, 197, 94) : Color.FromArgb(35, 107, 114, 128);
            Color openFg = isOpenNow ? Color.FromArgb(74, 222, 128) : Color.FromArgb(156, 163, 175);
            using (SolidBrush ob = new SolidBrush(openBg)) g.FillRectangle(ob, openRect);
            using (Pen op = new Pen(Color.FromArgb(120, openFg.R, openFg.G, openFg.B), 1f)) g.DrawRectangle(op, openRect);
            using (SolidBrush otb = new SolidBrush(openFg))
            {
                g.DrawString(isOpenNow ? "OPEN" : "OFF", fBadge, otb, openRect, centerFmt);
            }

            Rectangle autoRect = new Rectangle(openRect.Right + (int)Math.Round(6 * s), badgeY, autoW, badgeH);
            Color autoBg = isAutoOpen ? Color.FromArgb(40, 56, 189, 248) : Color.FromArgb(30, 107, 114, 128);
            Color autoFg = isAutoOpen ? Color.FromArgb(125, 211, 252) : Color.FromArgb(156, 163, 175);
            using (SolidBrush ab = new SolidBrush(autoBg)) g.FillRectangle(ab, autoRect);
            using (Pen ap = new Pen(Color.FromArgb(110, autoFg.R, autoFg.G, autoFg.B), 1f)) g.DrawRectangle(ap, autoRect);
            using (SolidBrush atb = new SolidBrush(autoFg))
            {
                g.DrawString(isAutoOpen ? "AUTO: ON" : "AUTO: OFF", fBadge, atb, autoRect, centerFmt);
            }

            return r;
        }

        private void DrawSimulatedViewportFrame(Graphics g, Rectangle vp, string captionTitle, string captionSub, float s)
        {
            using (LinearGradientBrush bg = new LinearGradientBrush(
                vp,
                Color.FromArgb(12, 17, 27),
                Color.FromArgb(18, 25, 38),
                LinearGradientMode.Vertical))
            {
                g.FillRectangle(bg, vp);
            }

            // Subtle simulated cockpit / asphalt grid lines in background
            int horizonY = vp.Top + (int)(vp.Height * 0.36f);
            using (Pen gridPen = new Pen(Color.FromArgb(18, 148, 163, 184), 1f))
            {
                g.DrawLine(gridPen, vp.Left, horizonY, vp.Right, horizonY);
                for (int i = 1; i <= 5; i++)
                {
                    int gy = horizonY + (int)((vp.Bottom - horizonY) * (i / 5.0f));
                    g.DrawLine(gridPen, vp.Left, gy, vp.Right, gy);
                }
                int cx = vp.Left + vp.Width / 2;
                g.DrawLine(gridPen, cx - (int)(70 * s), horizonY, vp.Left + (int)(20 * s), vp.Bottom);
                g.DrawLine(gridPen, cx + (int)(70 * s), horizonY, vp.Right - (int)(20 * s), vp.Bottom);
            }

            using (Pen borderPen = new Pen(Color.FromArgb(65, 56, 189, 248), 1f))
            {
                g.DrawRectangle(borderPen, vp);
            }

            // Bottom description strip inside preview viewport
            int capH = (int)Math.Round(46 * s);
            Rectangle capRect = new Rectangle(vp.Left, vp.Bottom - capH, vp.Width, capH);
            using (SolidBrush cb = new SolidBrush(Color.FromArgb(225, 11, 15, 24)))
            {
                g.FillRectangle(cb, capRect);
            }
            using (Pen cp = new Pen(Color.FromArgb(45, 255, 255, 255), 1f))
            {
                g.DrawLine(cp, capRect.Left, capRect.Top, capRect.Right, capRect.Top);
            }

            using (Font fCapTitle = new Font("Segoe UI", 10.2f * s, FontStyle.Bold, GraphicsUnit.Pixel))
            using (Font fCapSub = new Font("Segoe UI", 9.5f * s, FontStyle.Regular, GraphicsUnit.Pixel))
            using (SolidBrush sbCol = new SolidBrush(Color.FromArgb(175, 184, 198)))
            {
                StringFormat lf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
                g.DrawString(captionTitle, fCapTitle, Brushes.White,
                    new RectangleF(capRect.Left + 12 * s, capRect.Top + 3 * s, capRect.Width - 24 * s, 20 * s), lf);
                g.DrawString(captionSub, fCapSub, sbCol,
                    new RectangleF(capRect.Left + 12 * s, capRect.Top + 22 * s, capRect.Width - 24 * s, 20 * s), lf);
            }
        }

        private void DrawStandingsPreview(Graphics g, Rectangle vp, float s)
        {
            DrawSimulatedViewportFrame(
                g, vp,
                "STANDINGS OVERLAY SCREENSHOT PREVIEW",
                "Player-centered live standings with country flags, License/SR badge, iRating +/- delta, and lap colors.",
                s
            );

            int w = Math.Min(vp.Width - (int)Math.Round(24 * s), (int)Math.Round(485 * s));
            int rowH = (int)Math.Round(24 * s);
            int hdrH = (int)Math.Round(28 * s);
            int colH = (int)Math.Round(20 * s);
            int rows = 6;
            int h = hdrH + colH + rows * rowH + (int)Math.Round(8 * s);

            int x = vp.Left + (vp.Width - w) / 2;
            int y = vp.Top + (vp.Height - (int)Math.Round(46 * s) - h) / 2;

            Rectangle box = new Rectangle(x, y, w, h);
            using (SolidBrush bg = new SolidBrush(Color.FromArgb(242, 11, 14, 21))) g.FillRectangle(bg, box);
            using (Pen bp = new Pen(Color.FromArgb(95, 255, 255, 255), 1f)) g.DrawRectangle(bp, box);

            // Top header bar
            using (SolidBrush hb = new SolidBrush(Color.FromArgb(18, 24, 36)))
            {
                g.FillRectangle(hb, x, y, w, hdrH);
            }
            using (SolidBrush accent = new SolidBrush(Color.FromArgb(239, 68, 68)))
            {
                g.FillRectangle(accent, x, y, (int)Math.Round(4 * s), hdrH);
            }

            using (Font fHdr = new Font("Segoe UI", 9.8f * s, FontStyle.Bold, GraphicsUnit.Pixel))
            using (Font fCol = new Font("Segoe UI", 8.2f * s, FontStyle.Bold, GraphicsUnit.Pixel))
            using (Font fRow = new Font("Segoe UI", 9.2f * s, FontStyle.Bold, GraphicsUnit.Pixel))
            using (Font fMono = new Font("Consolas", 9.2f * s, FontStyle.Bold, GraphicsUnit.Pixel))
            {
                StringFormat lf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
                StringFormat rf = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
                StringFormat cf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

                g.DrawString("RACE  •  Spa-Francorchamps", fHdr, Brushes.White, new RectangleF(x + 12 * s, y, w * 0.55f, hdrH), lf);
                using (SolidBrush cb = new SolidBrush(Color.FromArgb(56, 189, 248)))
                {
                    g.DrawString("SOF 3,451   LAP 8/20", fMono, cb, new RectangleF(x + w * 0.45f, y, w * 0.55f - 10 * s, hdrH), rf);
                }

                // Column header row
                int cy = y + hdrH;
                using (SolidBrush chb = new SolidBrush(Color.FromArgb(14, 18, 28)))
                {
                    g.FillRectangle(chb, x, cy, w, colH);
                }

                int pxPos = x + (int)Math.Round(6 * s);
                int pxNum = pxPos + (int)Math.Round(20 * s);
                int pxFlg = _settings.ShowCarNumber ? (pxNum + (int)Math.Round(26 * s)) : pxNum;
                int pxDrv = pxFlg + (int)Math.Round(22 * s);
                int pxSr = x + w - (int)Math.Round(204 * s);
                int pxIr = x + w - (int)Math.Round(158 * s);
                int pxGap = x + w - (int)Math.Round(98 * s);
                int pxLap = x + w - (int)Math.Round(54 * s);

                using (SolidBrush mut = new SolidBrush(Color.FromArgb(148, 163, 184)))
                {
                    g.DrawString("P", fCol, mut, new RectangleF(pxPos, cy, 18 * s, colH), cf);
                    if (_settings.ShowCarNumber)
                        g.DrawString("#", fCol, mut, new RectangleF(pxNum, cy, 24 * s, colH), cf);
                    g.DrawString("FLG", fCol, mut, new RectangleF(pxFlg, cy, 20 * s, colH), cf);
                    g.DrawString("DRIVER", fCol, mut, new RectangleF(pxDrv, cy, 110 * s, colH), lf);
                    g.DrawString("SR", fCol, mut, new RectangleF(pxSr, cy, 42 * s, colH), cf);
                    g.DrawString("iRATING", fCol, mut, new RectangleF(pxIr, cy, 58 * s, colH), cf);
                    g.DrawString(_settings.ShowInterval ? "INT" : "GAP", fCol, mut, new RectangleF(pxGap, cy, 42 * s, colH), rf);
                    g.DrawString("LAST LAP", fCol, mut, new RectangleF(pxLap, cy, 48 * s, colH), rf);
                }

                string[] pos = { "1", "2", "3", "4", "5", "6" };
                string[] nums = { "1", "4", "16", "23", "81", "63" };
                string[] flags = { "NL", "GB", "IT", "US", "AU", "DE" };
                string[] names = { "M. Verstappen", "L. Norris", "C. Leclerc", "You", "O. Piastri", "G. Russell" };
                string[] srs = { "A 4.99", "A 4.42", "A 3.85", "A 4.18", "B 3.64", "A 3.12" };
                string[] irs = _settings.CompactIR
                    ? new string[] { "7.2k", "5.8k", "5.1k", "4.1k", "3.9k", "3.7k" }
                    : new string[] { "7,240", "5,810", "5,120", "4,125", "3,940", "3,710" };
                string[] dIrs = { "+34", "+22", "+11", "+18", "-9", "-18" };
                string[] gaps = _settings.ShowInterval
                    ? new string[] { "LEADER", "+1.4s", "+0.8s", "+0.5s", "+0.9s", "+1.2s" }
                    : new string[] { "LEADER", "+1.4s", "+2.2s", "+2.7s", "+3.6s", "+4.8s" };
                string[] laps = { "2:16.482", "2:16.710", "2:16.895", "2:16.604", "2:17.112", "2:17.290" };

                int ry = cy + colH;
                for (int i = 0; i < rows; i++)
                {
                    bool isYou = (i == 3);
                    Rectangle rr = new Rectangle(x + 1, ry, w - 2, rowH);
                    Color rBg = isYou
                        ? Color.FromArgb(48, 56, 189, 248)
                        : (i % 2 == 0 ? Color.FromArgb(16, 21, 32) : Color.FromArgb(12, 16, 25));
                    using (SolidBrush rb = new SolidBrush(rBg)) g.FillRectangle(rb, rr);

                    if (isYou)
                    {
                        using (SolidBrush bar = new SolidBrush(Color.FromArgb(56, 189, 248)))
                            g.FillRectangle(bar, x + 1, ry, (int)Math.Round(3 * s), rowH);
                    }

                    // Position badge
                    int pBadgeH = (int)Math.Round(16 * s);
                    Rectangle pRect = new Rectangle(pxPos + 1, ry + (rowH - pBadgeH) / 2, (int)(18 * s), pBadgeH);
                    Color pBg = i == 0 ? Color.FromArgb(250, 204, 21) : (i == 1 ? Color.FromArgb(209, 213, 219) : (i == 2 ? Color.FromArgb(217, 119, 6) : Color.FromArgb(35, 42, 56)));
                    Color pFg = i <= 1 ? Color.FromArgb(17, 24, 39) : Color.White;
                    using (SolidBrush pb = new SolidBrush(pBg)) g.FillRectangle(pb, pRect);
                    using (SolidBrush pf = new SolidBrush(pFg)) g.DrawString(pos[i], fCol, pf, pRect, cf);

                    if (_settings.ShowCarNumber)
                    {
                        using (SolidBrush nb = new SolidBrush(Color.FromArgb(251, 191, 36)))
                            g.DrawString("#" + nums[i], fMono, nb, new RectangleF(pxNum, ry, 24 * s, rowH), cf);
                    }

                    // Country flag
                    int fw = Math.Max(13, (int)Math.Round(16 * s));
                    int fh = Math.Max(9, (int)Math.Round(11 * s));
                    OverlayForm.DrawCountryFlag(g, pxFlg + ((int)(20 * s) - fw) / 2, ry + (rowH - fh) / 2, fw, fh, flags[i], s);

                    g.DrawString(names[i], fRow, isYou ? Brushes.Cyan : Brushes.White, new RectangleF(pxDrv, ry, pxSr - pxDrv - 4 * s, rowH), lf);
                    if (isYou)
                    {
                        Rectangle youBadge = new Rectangle(pxSr - (int)(30 * s), ry + (rowH - pBadgeH) / 2, (int)(26 * s), pBadgeH);
                        using (SolidBrush yb = new SolidBrush(Color.FromArgb(55, 56, 189, 248))) g.FillRectangle(yb, youBadge);
                        using (SolidBrush yf = new SolidBrush(Color.FromArgb(56, 189, 248))) g.DrawString("YOU", fCol, yf, youBadge, cf);
                    }

                    // License pill
                    Rectangle srRect = new Rectangle(pxSr + (int)(2 * s), ry + (int)(4 * s), (int)(39 * s), rowH - (int)(8 * s));
                    Color srCol = i == 4 ? Color.FromArgb(34, 197, 94) : Color.FromArgb(59, 130, 246);
                    using (SolidBrush sb = new SolidBrush(Color.FromArgb(50, srCol.R, srCol.G, srCol.B))) g.FillRectangle(sb, srRect);
                    using (Pen sp = new Pen(srCol, 1f)) g.DrawRectangle(sp, srRect);
                    g.DrawString(srs[i], fCol, Brushes.White, srRect, cf);

                    string irText = _settings.ShowDeltaIR ? (irs[i] + " " + dIrs[i]) : irs[i];
                    g.DrawString(irText, fMono, Brushes.Gainsboro, new RectangleF(pxIr - 4 * s, ry, 64 * s, rowH), cf);
                    g.DrawString(gaps[i], fMono, Brushes.LightGray, new RectangleF(pxGap, ry, 42 * s, rowH), rf);

                    Color lapCol = i == 0 ? Color.FromArgb(192, 132, 252) : (isYou ? Color.FromArgb(74, 222, 128) : Color.White);
                    using (SolidBrush lb = new SolidBrush(lapCol))
                        g.DrawString(laps[i], fMono, lb, new RectangleF(pxLap, ry, 48 * s, rowH), rf);

                    ry += rowH;
                }
            }
        }

        private void DrawDeltaBarPreview(Graphics g, Rectangle vp, float s)
        {
            DrawSimulatedViewportFrame(
                g, vp,
                "DELTA BAR (OPTION C) SCREENSHOT PREVIEW",
                "Center-zero delta bar colored by live speed trend, reference lap selector, predicted lap, and official sectors.",
                s
            );

            int w = Math.Min(vp.Width - (int)Math.Round(28 * s), (int)Math.Round(436 * s));
            int h = _settings.ShowSectorSplits ? (int)Math.Round(108 * s) : (int)Math.Round(80 * s);
            int x = vp.Left + (vp.Width - w) / 2;
            int y = vp.Top + (vp.Height - (int)Math.Round(46 * s) - h) / 2;

            Rectangle box = new Rectangle(x, y, w, h);
            using (SolidBrush bg = new SolidBrush(Color.FromArgb(242, 11, 14, 21))) g.FillRectangle(bg, box);
            using (Pen bp = new Pen(Color.FromArgb(100, 255, 255, 255), 1f)) g.DrawRectangle(bp, box);

            using (Font fTiny = new Font("Segoe UI", 8.5f * s, FontStyle.Bold, GraphicsUnit.Pixel))
            using (Font fRef = new Font("Consolas", 11.2f * s, FontStyle.Bold, GraphicsUnit.Pixel))
            using (Font fBig = new Font("Consolas", 18.0f * s, FontStyle.Bold, GraphicsUnit.Pixel))
            {
                StringFormat lf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
                StringFormat rf = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
                StringFormat cf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

                int pad = (int)Math.Round(12 * s);
                int topY = y + (int)Math.Round(8 * s);
                int topH = (int)Math.Round(38 * s);

                string modeLabel = "SESSION BEST";
                if (_settings.DeltaRefMode == DeltaReferenceMode.SessionOptimal) modeLabel = "OPTIMAL LAP";
                else if (_settings.DeltaRefMode == DeltaReferenceMode.PersonalBest) modeLabel = "PERSONAL BEST";
                else if (_settings.DeltaRefMode == DeltaReferenceMode.LastLap) modeLabel = "LAST LAP";

                using (SolidBrush cyan = new SolidBrush(Color.FromArgb(56, 189, 248)))
                    g.DrawString(modeLabel, fTiny, cyan, new RectangleF(x + pad, topY, 130 * s, 15 * s), lf);
                g.DrawString("2:16.482", fRef, Brushes.White, new RectangleF(x + pad, topY + 15 * s, 130 * s, 20 * s), lf);

                // Center Big Delta Pill
                int pillW = (int)Math.Round(118 * s);
                Rectangle pill = new Rectangle(x + (w - pillW) / 2, topY, pillW, topH);
                using (SolidBrush pb = new SolidBrush(Color.FromArgb(40, 34, 197, 94))) g.FillRectangle(pb, pill);
                using (Pen pp = new Pen(Color.FromArgb(210, 74, 222, 128), 1.5f * s)) g.DrawRectangle(pp, pill);
                using (SolidBrush gb = new SolidBrush(Color.FromArgb(74, 222, 128)))
                    g.DrawString("-0.28s", fBig, gb, pill, cf);

                // Right Predicted Lap
                using (SolidBrush mut = new SolidBrush(Color.FromArgb(156, 163, 175)))
                    g.DrawString("PREDICTED LAP", fTiny, mut, new RectangleF(x + w - pad - 130 * s, topY, 130 * s, 15 * s), rf);
                g.DrawString("2:16.202", fRef, Brushes.White, new RectangleF(x + w - pad - 130 * s, topY + 15 * s, 130 * s, 20 * s), rf);

                // Progress bar
                int barY = topY + topH + (int)Math.Round(8 * s);
                int barH = (int)Math.Round(14 * s);
                Rectangle track = new Rectangle(x + pad, barY, w - pad * 2, barH);
                using (SolidBrush tb = new SolidBrush(Color.FromArgb(24, 30, 44))) g.FillRectangle(tb, track);
                using (Pen tp = new Pen(Color.FromArgb(60, 255, 255, 255), 1f)) g.DrawRectangle(tp, track);

                int midX = track.Left + track.Width / 2;
                int fillW = (int)(track.Width * 0.24f);
                Rectangle fill = new Rectangle(midX - fillW, barY + 1, fillW, barH - 2);
                using (SolidBrush fb = new SolidBrush(Color.FromArgb(210, 34, 197, 94))) g.FillRectangle(fb, fill);
                using (Pen mp = new Pen(Color.White, 2f)) g.DrawLine(mp, midX, barY - 2, midX, barY + barH + 2);

                if (_settings.ShowSectorSplits)
                {
                    int secY = barY + barH + (int)Math.Round(7 * s);
                    int secH = (int)Math.Round(22 * s);
                    int gap = (int)Math.Round(6 * s);
                    int secW = (track.Width - gap * 2) / 3;
                    string[] sLabels = { "S1  -0.14s", "S2  -0.14s", "S3  +0.00s" };
                    for (int i = 0; i < 3; i++)
                    {
                        Rectangle sr = new Rectangle(track.Left + i * (secW + gap), secY, secW, secH);
                        bool done = i < 2;
                        Color sCol = done ? Color.FromArgb(34, 197, 94) : Color.FromArgb(56, 189, 248);
                        using (SolidBrush sb = new SolidBrush(Color.FromArgb(35, sCol.R, sCol.G, sCol.B))) g.FillRectangle(sb, sr);
                        using (Pen sp = new Pen(Color.FromArgb(140, sCol.R, sCol.G, sCol.B), 1f)) g.DrawRectangle(sp, sr);
                        g.DrawString(sLabels[i], fTiny, Brushes.White, sr, cf);
                    }
                }
            }
        }

        private void DrawGarage61Preview(Graphics g, Rectangle vp, float s)
        {
            string styleTitle = "GARAGE61 COACH — OPTION A (LOOKAHEAD TRACE + PEDALS) PREVIEW";
            string styleSub = "Scrolling spatial brake/throttle trace (-60m to +" + _settings.Garage61LookaheadMeters + "m) + live vs ghost pedal pressure matcher.";
            if (_settings.Garage61Style == Garage61LayoutStyle.OptionB_CompactBrakeCoach)
            {
                styleTitle = "GARAGE61 COACH — OPTION B (COMPACT SIGHTLINE BAR) PREVIEW";
                styleSub = "Minimalist horizontal brake countdown, live vs target peak brake pressure bar, and braking spot delta.";
            }
            else if (_settings.Garage61Style == Garage61LayoutStyle.OptionC_ProBoxWithCornerAudit)
            {
                styleTitle = "GARAGE61 COACH — OPTION C (PRO BOX + 3-CORNER AUDIT) PREVIEW";
                styleSub = "Full lookahead trace, pedal matcher, speed/gear delta box, and 3-corner braking accuracy scorecard.";
            }

            DrawSimulatedViewportFrame(g, vp, styleTitle, styleSub, s);

            using (Font fHdr = new Font("Segoe UI", 9.2f * s, FontStyle.Bold, GraphicsUnit.Pixel))
            using (Font fTiny = new Font("Segoe UI", 8.2f * s, FontStyle.Bold, GraphicsUnit.Pixel))
            using (Font fMono = new Font("Consolas", 10.5f * s, FontStyle.Bold, GraphicsUnit.Pixel))
            using (Font fBig = new Font("Consolas", 14.5f * s, FontStyle.Bold, GraphicsUnit.Pixel))
            {
                StringFormat lf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
                StringFormat rf = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
                StringFormat cf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

                if (_settings.Garage61Style == Garage61LayoutStyle.OptionB_CompactBrakeCoach)
                {
                    int w = Math.Min(vp.Width - (int)Math.Round(28 * s), (int)Math.Round(436 * s));
                    int h = (int)Math.Round(66 * s);
                    int x = vp.Left + (vp.Width - w) / 2;
                    int y = vp.Top + (vp.Height - (int)Math.Round(46 * s) - h) / 2;

                    Rectangle box = new Rectangle(x, y, w, h);
                    using (SolidBrush bg = new SolidBrush(Color.FromArgb(242, 11, 14, 21))) g.FillRectangle(bg, box);
                    using (Pen bp = new Pen(Color.FromArgb(110, 244, 63, 94), 1.2f * s)) g.DrawRectangle(bp, box);

                    int pad = (int)Math.Round(8 * s);
                    Rectangle leftBox = new Rectangle(x + pad, y + pad, (int)(110 * s), h - pad * 2);
                    using (SolidBrush lb = new SolidBrush(Color.FromArgb(40, 245, 158, 11))) g.FillRectangle(lb, leftBox);
                    using (Pen lp = new Pen(Color.FromArgb(245, 158, 11), 1f)) g.DrawRectangle(lp, leftBox);
                    g.DrawString("T5 Les Combes", fTiny, Brushes.LightGray, new RectangleF(leftBox.Left + 5 * s, leftBox.Top + 3 * s, leftBox.Width - 10 * s, 16 * s), lf);
                    using (SolidBrush yb = new SolidBrush(Color.FromArgb(253, 224, 71)))
                        g.DrawString("BRK IN 28 m", fMono, yb, new RectangleF(leftBox.Left + 5 * s, leftBox.Top + 20 * s, leftBox.Width - 10 * s, 24 * s), lf);

                    Rectangle rightBox = new Rectangle(x + w - pad - (int)(108 * s), y + pad, (int)(108 * s), h - pad * 2);
                    using (SolidBrush rb = new SolidBrush(Color.FromArgb(40, 34, 197, 94))) g.FillRectangle(rb, rightBox);
                    using (Pen rp = new Pen(Color.FromArgb(34, 197, 94), 1f)) g.DrawRectangle(rp, rightBox);
                    g.DrawString("BRAKE SPOT", fTiny, Brushes.LightGray, new RectangleF(rightBox.Left, rightBox.Top + 3 * s, rightBox.Width, 16 * s), cf);
                    using (SolidBrush gb = new SolidBrush(Color.FromArgb(74, 222, 128)))
                        g.DrawString("0.8m ON TARGET", fTiny, gb, new RectangleF(rightBox.Left, rightBox.Top + 22 * s, rightBox.Width, 22 * s), cf);

                    Rectangle midBox = new Rectangle(leftBox.Right + pad, y + pad, rightBox.Left - leftBox.Right - pad * 2, h - pad * 2);
                    g.DrawString("BRAKE   YOU 84% / G61 88%", fTiny, Brushes.White,
                        new RectangleF(midBox.Left, midBox.Top + 2 * s, midBox.Width, 16 * s), lf);
                    Rectangle barTrack = new Rectangle(midBox.Left, midBox.Top + (int)(22 * s), midBox.Width, (int)(20 * s));
                    using (SolidBrush tb = new SolidBrush(Color.FromArgb(24, 30, 44))) g.FillRectangle(tb, barTrack);
                    using (SolidBrush fb = new SolidBrush(Color.FromArgb(220, 239, 68, 68)))
                        g.FillRectangle(fb, barTrack.Left + 1, barTrack.Top + 1, (int)(barTrack.Width * 0.84f), barTrack.Height - 2);
                    int pkX = barTrack.Left + (int)(barTrack.Width * 0.92f);
                    using (Pen pkPen = new Pen(Color.FromArgb(250, 204, 21), 2f * s))
                        g.DrawLine(pkPen, pkX, barTrack.Top - 2, pkX, barTrack.Bottom + 2);
                    return;
                }

                // Option A or Option C preview box
                bool showAudit = (_settings.Garage61Style == Garage61LayoutStyle.OptionC_ProBoxWithCornerAudit);
                int boxW = Math.Min(vp.Width - (int)Math.Round(28 * s), (int)Math.Round(436 * s));
                int boxH = showAudit ? (int)Math.Round(232 * s) : (int)Math.Round(182 * s);
                int bx = vp.Left + (vp.Width - boxW) / 2;
                int by = vp.Top + (vp.Height - (int)Math.Round(46 * s) - boxH) / 2;

                Rectangle mainBox = new Rectangle(bx, by, boxW, boxH);
                using (SolidBrush bg = new SolidBrush(Color.FromArgb(242, 11, 14, 21))) g.FillRectangle(bg, mainBox);
                using (Pen bp = new Pen(Color.FromArgb(95, 56, 189, 248), 1f)) g.DrawRectangle(bp, mainBox);

                // Top banner
                int banH = (int)Math.Round(30 * s);
                Rectangle ban = new Rectangle(bx + (int)(8 * s), by + (int)(8 * s), boxW - (int)(16 * s), banH);
                using (SolidBrush bb = new SolidBrush(Color.FromArgb(35, 245, 158, 11))) g.FillRectangle(bb, ban);
                using (Pen bPen = new Pen(Color.FromArgb(160, 245, 158, 11), 1f)) g.DrawRectangle(bPen, ban);
                using (SolidBrush yb = new SolidBrush(Color.FromArgb(253, 224, 71)))
                    g.DrawString("T5 Les Combes • BRK IN 32m (G61 92%)", fHdr, yb, new RectangleF(ban.Left + 8 * s, ban.Top, ban.Width * 0.65f, ban.Height), lf);
                using (SolidBrush gb = new SolidBrush(Color.FromArgb(74, 222, 128)))
                    g.DrawString("0.9m ON TARGET", fTiny, gb, new RectangleF(ban.Left + ban.Width * 0.62f, ban.Top, ban.Width * 0.38f - 8 * s, ban.Height), rf);

                // Trace graph area + right pedal bars
                int graphTop = ban.Bottom + (int)Math.Round(8 * s);
                int graphH = (int)Math.Round(128 * s);
                int pedalsW = (int)Math.Round(96 * s);
                Rectangle graphRect = new Rectangle(ban.Left, graphTop, ban.Width - pedalsW - (int)(8 * s), graphH);
                Rectangle pedalsRect = new Rectangle(graphRect.Right + (int)(8 * s), graphTop, pedalsW, graphH);

                using (SolidBrush gb = new SolidBrush(Color.FromArgb(16, 22, 34))) g.FillRectangle(gb, graphRect);
                using (Pen gp = new Pen(Color.FromArgb(50, 255, 255, 255), 1f)) g.DrawRectangle(gp, graphRect);

                // NOW vertical crosshair at 33%
                int nowX = graphRect.Left + (int)(graphRect.Width * 0.33f);
                using (Pen nowPen = new Pen(Color.FromArgb(180, 56, 189, 248), 1.5f * s))
                    g.DrawLine(nowPen, nowX, graphRect.Top + 2, nowX, graphRect.Bottom - 2);
                g.DrawString("NOW", fTiny, Brushes.Cyan, new RectangleF(nowX + 3 * s, graphRect.Top + 2 * s, 40 * s, 14 * s), lf);

                // Simulated Ghost & Live Brake/Throttle curves
                PointF[] ghostBrk = new PointF[]
                {
                    new PointF(graphRect.Left + graphRect.Width * 0.05f, graphRect.Bottom - 8 * s),
                    new PointF(graphRect.Left + graphRect.Width * 0.44f, graphRect.Bottom - 8 * s),
                    new PointF(graphRect.Left + graphRect.Width * 0.50f, graphRect.Top + 14 * s),
                    new PointF(graphRect.Left + graphRect.Width * 0.66f, graphRect.Top + 24 * s),
                    new PointF(graphRect.Left + graphRect.Width * 0.88f, graphRect.Bottom - 8 * s)
                };
                PointF[] ghostThr = new PointF[]
                {
                    new PointF(graphRect.Left + graphRect.Width * 0.05f, graphRect.Top + 12 * s),
                    new PointF(graphRect.Left + graphRect.Width * 0.42f, graphRect.Top + 12 * s),
                    new PointF(graphRect.Left + graphRect.Width * 0.48f, graphRect.Bottom - 8 * s),
                    new PointF(graphRect.Left + graphRect.Width * 0.72f, graphRect.Bottom - 8 * s),
                    new PointF(graphRect.Left + graphRect.Width * 0.92f, graphRect.Top + 18 * s)
                };

                using (Pen gThrPen = new Pen(Color.FromArgb(140, 34, 197, 94), 1.8f * s) { DashStyle = DashStyle.Dash })
                using (Pen gBrkPen = new Pen(Color.FromArgb(210, 244, 63, 94), 2.2f * s) { DashStyle = DashStyle.Dash })
                {
                    g.DrawLines(gThrPen, ghostThr);
                    g.DrawLines(gBrkPen, ghostBrk);
                }

                int brkSpotX = graphRect.Left + (int)(graphRect.Width * 0.44f);
                using (Pen flagPen = new Pen(Color.FromArgb(244, 63, 94), 1.5f * s))
                    g.DrawLine(flagPen, brkSpotX, graphRect.Top + 4, brkSpotX, graphRect.Bottom - 4);
                using (SolidBrush rb = new SolidBrush(Color.FromArgb(244, 63, 94)))
                    g.DrawString("G61 BRK", fTiny, rb, new RectangleF(brkSpotX + 3 * s, graphRect.Top + 2 * s, 60 * s, 14 * s), lf);

                // Vertical Brake & Throttle bars inside pedalsRect
                int bBarW = (pedalsRect.Width - (int)(12 * s)) / 2;
                Rectangle brkBar = new Rectangle(pedalsRect.Left, pedalsRect.Top + (int)(16 * s), bBarW, pedalsRect.Height - (int)(32 * s));
                Rectangle thrBar = new Rectangle(brkBar.Right + (int)(12 * s), brkBar.Top, bBarW, brkBar.Height);

                g.DrawString("BRK", fTiny, Brushes.White, new RectangleF(brkBar.Left, pedalsRect.Top, brkBar.Width, 14 * s), cf);
                g.DrawString("THR", fTiny, Brushes.White, new RectangleF(thrBar.Left, pedalsRect.Top, thrBar.Width, 14 * s), cf);

                using (SolidBrush tb = new SolidBrush(Color.FromArgb(22, 28, 42)))
                {
                    g.FillRectangle(tb, brkBar);
                    g.FillRectangle(tb, thrBar);
                }
                int brkFillH = (int)(brkBar.Height * 0.84f);
                using (SolidBrush bf = new SolidBrush(Color.FromArgb(220, 239, 68, 68)))
                    g.FillRectangle(bf, brkBar.Left + 2, brkBar.Bottom - brkFillH, brkBar.Width - 4, brkFillH);

                int pkY = brkBar.Bottom - (int)(brkBar.Height * 0.92f);
                using (Pen pkPen = new Pen(Color.FromArgb(250, 204, 21), 2f * s))
                    g.DrawLine(pkPen, brkBar.Left - 2, pkY, brkBar.Right + 2, pkY);

                int thrFillH = (int)(thrBar.Height * 0.18f);
                using (SolidBrush tf = new SolidBrush(Color.FromArgb(210, 34, 197, 94)))
                    g.FillRectangle(tf, thrBar.Left + 2, thrBar.Bottom - thrFillH, thrBar.Width - 4, thrFillH);

                g.DrawString("84%", fTiny, Brushes.White, new RectangleF(brkBar.Left - 4 * s, brkBar.Bottom + 2 * s, brkBar.Width + 8 * s, 14 * s), cf);
                g.DrawString("18%", fTiny, Brushes.White, new RectangleF(thrBar.Left - 4 * s, thrBar.Bottom + 2 * s, thrBar.Width + 8 * s, 14 * s), cf);

                if (showAudit)
                {
                    int audY = graphRect.Bottom + (int)Math.Round(8 * s);
                    int audH = (int)Math.Round(42 * s);
                    int gap = (int)Math.Round(6 * s);
                    int cardW = (ban.Width - gap * 2) / 3;
                    string[] cNames = { "T1 La Source", "T5 Les Combes", "T8 Bruxelles" };
                    string[] cSpots = { "0.8m TARGET", "2.1m LATE", "3.4m EARLY" };
                    string[] cSubs = { "PK 93/94% • 69kph", "PK 88/92% • 134kph", "PK 79/86% • 108kph" };
                    Color[] cCols = { Color.FromArgb(34, 197, 94), Color.FromArgb(244, 63, 94), Color.FromArgb(245, 158, 11) };

                    for (int i = 0; i < 3; i++)
                    {
                        Rectangle cr = new Rectangle(ban.Left + i * (cardW + gap), audY, cardW, audH);
                        using (SolidBrush cb = new SolidBrush(Color.FromArgb(28, cCols[i].R, cCols[i].G, cCols[i].B))) g.FillRectangle(cb, cr);
                        using (Pen cp = new Pen(Color.FromArgb(140, cCols[i].R, cCols[i].G, cCols[i].B), 1f)) g.DrawRectangle(cp, cr);
                        g.DrawString(cNames[i], fTiny, Brushes.White, new RectangleF(cr.Left + 4 * s, cr.Top + 3 * s, cr.Width * 0.56f, 16 * s), lf);
                        using (SolidBrush sb = new SolidBrush(cCols[i]))
                            g.DrawString(cSpots[i], fTiny, sb, new RectangleF(cr.Left + cr.Width * 0.44f, cr.Top + 3 * s, cr.Width * 0.56f - 4 * s, 16 * s), rf);
                        g.DrawString(cSubs[i], fTiny, Brushes.LightGray, new RectangleF(cr.Left + 4 * s, cr.Top + 21 * s, cr.Width - 8 * s, 16 * s), lf);
                    }
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            float s = _dpiScale;
            int headerH = (int)Math.Round(54 * s);
            int tabBarH = (int)Math.Round(46 * s);
            int footerH = (int)Math.Round(36 * s);
            int pad = (int)Math.Round(16 * s);

            using (Font fTitle = new Font("Segoe UI", 16.0f * s, FontStyle.Bold, GraphicsUnit.Pixel))
            using (Font fTab = new Font("Segoe UI", 11.8f * s, FontStyle.Bold, GraphicsUnit.Pixel))
            using (Font fBadge = new Font("Segoe UI", 8.8f * s, FontStyle.Bold, GraphicsUnit.Pixel))
            using (Font fCardTitle = new Font("Segoe UI", 13.0f * s, FontStyle.Bold, GraphicsUnit.Pixel))
            using (Font fLabel = new Font("Segoe UI", 11.4f * s, FontStyle.Regular, GraphicsUnit.Pixel))
            using (Font fBtn = new Font("Segoe UI", 10.6f * s, FontStyle.Bold, GraphicsUnit.Pixel))
            using (Font fMono = new Font("Consolas", 11.5f * s, FontStyle.Bold, GraphicsUnit.Pixel))
            {
                StringFormat leftFmt = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
                StringFormat rightFmt = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
                StringFormat centerFmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

                // 1. Top Header Bar
                using (SolidBrush hb = new SolidBrush(Color.FromArgb(16, 20, 30)))
                {
                    g.FillRectangle(hb, 0, 0, Width, headerH);
                }

                int dotSz = (int)Math.Round(10 * s);
                using (SolidBrush redDot = new SolidBrush(Color.FromArgb(239, 68, 68)))
                {
                    g.FillEllipse(redDot, pad, (headerH - dotSz) / 2, dotSz, dotSz);
                }
                g.DrawString("FREE OVERLAY HUB", fTitle, Brushes.White,
                    new RectangleF(pad + dotSz + 10 * s, 0, 260 * s, headerH), leftFmt);

                int hBtnH = (int)Math.Round(30 * s);
                int hBtnY = (headerH - hBtnH) / 2;
                int rightX = Width - pad;

                int quitW = (int)Math.Round(84 * s);
                _btnQuitApp = DrawButton(g, fBtn, "QUIT (F8)", rightX - quitW, hBtnY, quitW, hBtnH, true, Color.FromArgb(239, 68, 68), s);
                rightX = _btnQuitApp.Left - (int)Math.Round(8 * s);

                int hideW = (int)Math.Round(110 * s);
                _btnHideHub = DrawButton(g, fBtn, "HIDE HUB (F5)", rightX - hideW, hBtnY, hideW, hBtnH, false, Color.FromArgb(56, 189, 248), s);
                rightX = _btnHideHub.Left - (int)Math.Round(8 * s);

                string modeStr = _settings.Mode == TelemetryMode.Auto ? "SRC: AUTO (F7)" : (_settings.Mode == TelemetryMode.Mock ? "SRC: SIM (F7)" : "SRC: LIVE (F7)");
                int modeW = (int)Math.Round(118 * s);
                _btnCycleMode = DrawButton(g, fBtn, modeStr, rightX - modeW, hBtnY, modeW, hBtnH, true, Color.FromArgb(56, 189, 248), s);
                rightX = _btnCycleMode.Left - (int)Math.Round(8 * s);

                string lockStr = _settings.Locked ? "UNLOCK ALL (F6)" : "LOCK ALL (F6)";
                int lockW = (int)Math.Round(126 * s);
                _btnLockAll = DrawButton(g, fBtn, lockStr, rightX - lockW, hBtnY, lockW, hBtnH, _settings.Locked, Color.FromArgb(245, 158, 11), s);

                // 2. Three-Column Layout: Left Sidebar (Widgets Tabs) | Center (Settings) | Right (Widget Preview)
                int contentTop = headerH + (int)Math.Round(12 * s);
                int contentH = Height - contentTop - footerH - (int)Math.Round(12 * s);
                int sidebarW = (int)Math.Round(226 * s);
                int settingsW = (int)Math.Round(394 * s);
                int previewW = Width - pad * 4 - sidebarW - settingsW;

                Rectangle sidebarPanel = new Rectangle(pad, contentTop, sidebarW, contentH);
                Rectangle settingsPanel = new Rectangle(sidebarPanel.Right + pad, contentTop, settingsW, contentH);
                Rectangle previewPanel = new Rectangle(settingsPanel.Right + pad, contentTop, previewW, contentH);

                using (SolidBrush sbBg = new SolidBrush(Color.FromArgb(14, 18, 28)))
                {
                    g.FillRectangle(sbBg, sidebarPanel);
                }
                using (SolidBrush cb = new SolidBrush(Color.FromArgb(17, 22, 33)))
                {
                    g.FillRectangle(cb, settingsPanel);
                    g.FillRectangle(cb, previewPanel);
                }
                using (Pen cp = new Pen(Color.FromArgb(45, 255, 255, 255), 1.0f))
                {
                    g.DrawRectangle(cp, sidebarPanel);
                    g.DrawRectangle(cp, settingsPanel);
                    g.DrawRectangle(cp, previewPanel);
                }

                // Left Sidebar: Widgets Navigation Rail
                int sbPad = (int)Math.Round(12 * s);
                int sbInnerW = sidebarPanel.Width - sbPad * 2;
                int sbCurY = sidebarPanel.Top + (int)Math.Round(12 * s);
                using (SolidBrush mutHdr = new SolidBrush(Color.FromArgb(148, 163, 184)))
                {
                    g.DrawString("WIDGETS", fBadge, mutHdr, new RectangleF(sidebarPanel.Left + sbPad, sbCurY, sbInnerW, 18 * s), leftFmt);
                }
                sbCurY += (int)Math.Round(22 * s);

                int vTabH = (int)Math.Round(56 * s);
                int vTabGap = (int)Math.Round(8 * s);

                _tabStandings = DrawTabButton(
                    g, fTab, fBadge, "1. STANDINGS",
                    _settings.StandingsEnabled, _settings.StandingsAutoOpen,
                    sidebarPanel.Left + sbPad, sbCurY, sbInnerW, vTabH, _activeTab == 0, s
                );
                sbCurY += vTabH + vTabGap;

                _tabDeltaBar = DrawTabButton(
                    g, fTab, fBadge, "2. DELTA BAR",
                    _settings.DeltaBarEnabled, _settings.DeltaBarAutoOpen,
                    sidebarPanel.Left + sbPad, sbCurY, sbInnerW, vTabH, _activeTab == 1, s
                );
                sbCurY += vTabH + vTabGap;

                _tabGarage61 = DrawTabButton(
                    g, fTab, fBadge, "3. GARAGE61 COACH",
                    _settings.Garage61Enabled, _settings.Garage61AutoOpen,
                    sidebarPanel.Left + sbPad, sbCurY, sbInnerW, vTabH, _activeTab == 2, s
                );

                int sx = settingsPanel.Left + (int)Math.Round(16 * s);
                int sw = settingsPanel.Width - (int)Math.Round(32 * s);
                int curY = settingsPanel.Top + (int)Math.Round(14 * s);
                int rowH = (int)Math.Round(30 * s);
                int gapY = (int)Math.Round(10 * s);
                int optW = (int)Math.Round(186 * s);
                int rStart = sx + sw - optW;
                int pmW = (int)Math.Round(36 * s);
                int valW = optW - pmW * 2;
                int resetY = settingsPanel.Bottom - rowH - (int)Math.Round(14 * s);

                // Right Preview Panel Header + Viewport
                int px = previewPanel.Left + (int)Math.Round(16 * s);
                int pw = previewPanel.Width - (int)Math.Round(32 * s);
                int py = previewPanel.Top + (int)Math.Round(12 * s);
                g.DrawString("WIDGET PREVIEW", fCardTitle, Brushes.White, new RectangleF(px, py, pw * 0.6f, 24 * s), leftFmt);
                using (SolidBrush cyanBrush = new SolidBrush(Color.FromArgb(56, 189, 248)))
                {
                    g.DrawString("LIVE SCREENSHOT PREVIEW", fBadge, cyanBrush, new RectangleF(px + pw * 0.5f, py, pw * 0.5f, 24 * s), rightFmt);
                }
                Rectangle viewportRect = new Rectangle(px, py + (int)Math.Round(28 * s), pw, previewPanel.Height - (int)Math.Round(44 * s));

                if (_activeTab == 0)
                {
                    // =====================================================
                    // TAB 1: STANDINGS SETTINGS + PREVIEW
                    // =====================================================
                    g.DrawString("STANDINGS OVERLAY SETTINGS", fCardTitle, Brushes.White,
                        new RectangleF(sx, curY, sw, rowH), leftFmt);
                    curY += rowH + (int)Math.Round(6 * s);

                    g.DrawString("Widget Status (Now)", fLabel, Brushes.Gainsboro, new RectangleF(sx, curY, sw - optW, rowH), leftFmt);
                    _btnStandingsToggle = DrawButton(
                        g, fBtn,
                        _settings.StandingsEnabled ? "OPEN (VISIBLE)" : "CLOSED (HIDDEN)",
                        rStart, curY, optW, rowH,
                        _settings.StandingsEnabled,
                        _settings.StandingsEnabled ? Color.FromArgb(34, 197, 94) : Color.FromArgb(107, 114, 128),
                        s
                    );
                    curY += rowH + gapY;

                    g.DrawString("Open Automatically on Launch", fLabel, Brushes.Gainsboro, new RectangleF(sx, curY, sw - optW, rowH), leftFmt);
                    _btnStandingsAutoOpen = DrawButton(
                        g, fBtn,
                        _settings.StandingsAutoOpen ? "YES (AUTO-OPEN)" : "NO (MANUAL)",
                        rStart, curY, optW, rowH,
                        _settings.StandingsAutoOpen,
                        _settings.StandingsAutoOpen ? Color.FromArgb(56, 189, 248) : Color.FromArgb(107, 114, 128),
                        s
                    );
                    curY += rowH + gapY;

                    using (Pen divPen = new Pen(Color.FromArgb(35, 255, 255, 255), 1f))
                        g.DrawLine(divPen, sx, curY - (int)(4 * s), sx + sw, curY - (int)(4 * s));

                    g.DrawString("Car Number (#)", fLabel, Brushes.Gainsboro, new RectangleF(sx, curY, sw - optW, rowH), leftFmt);
                    _btnStandingsCarNum = DrawButton(
                        g, fBtn,
                        _settings.ShowCarNumber ? "SHOW (#)" : "HIDDEN",
                        rStart, curY, optW, rowH,
                        _settings.ShowCarNumber, Color.FromArgb(34, 197, 94), s
                    );
                    curY += rowH + gapY;

                    g.DrawString("Timing Column", fLabel, Brushes.Gainsboro, new RectangleF(sx, curY, sw - optW, rowH), leftFmt);
                    _btnStandingsGap = DrawButton(
                        g, fBtn,
                        _settings.ShowInterval ? "INTERVAL AHEAD" : "GAP TO LEADER",
                        rStart, curY, optW, rowH,
                        true, Color.FromArgb(56, 189, 248), s
                    );
                    curY += rowH + gapY;

                    g.DrawString("iRating Display", fLabel, Brushes.Gainsboro, new RectangleF(sx, curY, sw - optW, rowH), leftFmt);
                    _btnStandingsCompactIr = DrawButton(
                        g, fBtn,
                        _settings.CompactIR ? "COMPACT (4.1k)" : "FULL (4,125)",
                        rStart, curY, optW, rowH,
                        _settings.CompactIR, Color.FromArgb(56, 189, 248), s
                    );
                    curY += rowH + gapY;

                    g.DrawString("Projected +/- iR", fLabel, Brushes.Gainsboro, new RectangleF(sx, curY, sw - optW, rowH), leftFmt);
                    _btnStandingsDeltaIr = DrawButton(
                        g, fBtn,
                        _settings.ShowDeltaIR ? "SHOW (+/- iR)" : "HIDDEN",
                        rStart, curY, optW, rowH,
                        _settings.ShowDeltaIR, Color.FromArgb(34, 197, 94), s
                    );
                    curY += rowH + gapY;

                    g.DrawString("Font Scale", fLabel, Brushes.Gainsboro, new RectangleF(sx, curY, sw - optW, rowH), leftFmt);
                    _btnStandingsFontMinus = DrawButton(g, fBtn, "-", rStart, curY, pmW, rowH, false, Color.Gray, s);
                    g.DrawString((int)Math.Round(_settings.StandingsFontScale * 100) + "%", fMono, Brushes.White,
                        new RectangleF(rStart + pmW, curY, valW, rowH), centerFmt);
                    _btnStandingsFontPlus = DrawButton(g, fBtn, "+", rStart + pmW + valW, curY, pmW, rowH, false, Color.Gray, s);
                    curY += rowH + gapY;

                    g.DrawString("Window Opacity", fLabel, Brushes.Gainsboro, new RectangleF(sx, curY, sw - optW, rowH), leftFmt);
                    _btnStandingsOpacMinus = DrawButton(g, fBtn, "-", rStart, curY, pmW, rowH, false, Color.Gray, s);
                    g.DrawString((int)Math.Round(_settings.StandingsOpacity * 100) + "%", fMono, Brushes.White,
                        new RectangleF(rStart + pmW, curY, valW, rowH), centerFmt);
                    _btnStandingsOpacPlus = DrawButton(g, fBtn, "+", rStart + pmW + valW, curY, pmW, rowH, false, Color.Gray, s);

                    _btnStandingsReset = DrawButton(g, fBtn, "RESET POSITION & SIZE", sx, resetY, sw, rowH, false, Color.FromArgb(56, 189, 248), s);

                    DrawStandingsPreview(g, viewportRect, s);
                }
                else if (_activeTab == 1)
                {
                    // =====================================================
                    // TAB 2: DELTA BAR SETTINGS + PREVIEW
                    // =====================================================
                    g.DrawString("DELTA BAR OVERLAY SETTINGS", fCardTitle, Brushes.White,
                        new RectangleF(sx, curY, sw, rowH), leftFmt);
                    curY += rowH + (int)Math.Round(6 * s);

                    g.DrawString("Widget Status (Now)", fLabel, Brushes.Gainsboro, new RectangleF(sx, curY, sw - optW, rowH), leftFmt);
                    _btnDeltaToggle = DrawButton(
                        g, fBtn,
                        _settings.DeltaBarEnabled ? "OPEN (VISIBLE)" : "CLOSED (HIDDEN)",
                        rStart, curY, optW, rowH,
                        _settings.DeltaBarEnabled,
                        _settings.DeltaBarEnabled ? Color.FromArgb(34, 197, 94) : Color.FromArgb(107, 114, 128),
                        s
                    );
                    curY += rowH + gapY;

                    g.DrawString("Open Automatically on Launch", fLabel, Brushes.Gainsboro, new RectangleF(sx, curY, sw - optW, rowH), leftFmt);
                    _btnDeltaAutoOpen = DrawButton(
                        g, fBtn,
                        _settings.DeltaBarAutoOpen ? "YES (AUTO-OPEN)" : "NO (MANUAL)",
                        rStart, curY, optW, rowH,
                        _settings.DeltaBarAutoOpen,
                        _settings.DeltaBarAutoOpen ? Color.FromArgb(56, 189, 248) : Color.FromArgb(107, 114, 128),
                        s
                    );
                    curY += rowH + gapY;

                    using (Pen divPen = new Pen(Color.FromArgb(35, 255, 255, 255), 1f))
                        g.DrawLine(divPen, sx, curY - (int)(4 * s), sx + sw, curY - (int)(4 * s));

                    g.DrawString("Reference Lap Mode", fLabel, Brushes.Gainsboro, new RectangleF(sx, curY, sw, (int)(18 * s)), leftFmt);
                    curY += (int)Math.Round(20 * s);
                    int refGap = (int)Math.Round(6 * s);
                    int refW = (sw - refGap * 3) / 4;
                    int smallBtnH = (int)Math.Round(28 * s);
                    _btnDeltaRefBest = DrawButton(g, fBtn, "BEST", sx, curY, refW, smallBtnH,
                        _settings.DeltaRefMode == DeltaReferenceMode.SessionBest, Color.FromArgb(56, 189, 248), s);
                    _btnDeltaRefOpt = DrawButton(g, fBtn, "OPTIMAL", sx + (refW + refGap), curY, refW, smallBtnH,
                        _settings.DeltaRefMode == DeltaReferenceMode.SessionOptimal, Color.FromArgb(192, 132, 252), s);
                    _btnDeltaRefPers = DrawButton(g, fBtn, "PERSONAL", sx + (refW + refGap) * 2, curY, refW, smallBtnH,
                        _settings.DeltaRefMode == DeltaReferenceMode.PersonalBest, Color.FromArgb(34, 197, 94), s);
                    _btnDeltaRefLast = DrawButton(g, fBtn, "LAST LAP", sx + (refW + refGap) * 3, curY, sw - (refW + refGap) * 3, smallBtnH,
                        _settings.DeltaRefMode == DeltaReferenceMode.LastLap, Color.FromArgb(245, 158, 11), s);
                    curY += smallBtnH + gapY;

                    int rngW = (optW - 8) / 3;
                    g.DrawString("Bar Scale Range", fLabel, Brushes.Gainsboro, new RectangleF(sx, curY, sw - optW, rowH), leftFmt);
                    _btnDeltaRange1 = DrawButton(g, fBtn, "±1.0s", rStart, curY, rngW, rowH,
                        Math.Abs(_settings.DeltaBarRangeSec - 1.0f) < 0.1f, Color.FromArgb(56, 189, 248), s);
                    _btnDeltaRange2 = DrawButton(g, fBtn, "±2.0s", rStart + rngW + 4, curY, rngW, rowH,
                        Math.Abs(_settings.DeltaBarRangeSec - 2.0f) < 0.1f, Color.FromArgb(56, 189, 248), s);
                    _btnDeltaRange5 = DrawButton(g, fBtn, "±5.0s", rStart + (rngW + 4) * 2, curY, optW - (rngW + 4) * 2, rowH,
                        Math.Abs(_settings.DeltaBarRangeSec - 5.0f) < 0.1f, Color.FromArgb(56, 189, 248), s);
                    curY += rowH + gapY;

                    g.DrawString("Official Sectors", fLabel, Brushes.Gainsboro, new RectangleF(sx, curY, sw - optW, rowH), leftFmt);
                    _btnDeltaSectors = DrawButton(
                        g, fBtn,
                        _settings.ShowSectorSplits ? "SHOW SPLITS" : "HIDDEN",
                        rStart, curY, optW, rowH,
                        _settings.ShowSectorSplits, Color.FromArgb(34, 197, 94), s
                    );
                    curY += rowH + gapY;

                    g.DrawString("Font Scale", fLabel, Brushes.Gainsboro, new RectangleF(sx, curY, sw - optW, rowH), leftFmt);
                    _btnDeltaFontMinus = DrawButton(g, fBtn, "-", rStart, curY, pmW, rowH, false, Color.Gray, s);
                    g.DrawString((int)Math.Round(_settings.DeltaFontScale * 100) + "%", fMono, Brushes.White,
                        new RectangleF(rStart + pmW, curY, valW, rowH), centerFmt);
                    _btnDeltaFontPlus = DrawButton(g, fBtn, "+", rStart + pmW + valW, curY, pmW, rowH, false, Color.Gray, s);
                    curY += rowH + gapY;

                    g.DrawString("Window Opacity", fLabel, Brushes.Gainsboro, new RectangleF(sx, curY, sw - optW, rowH), leftFmt);
                    _btnDeltaOpacMinus = DrawButton(g, fBtn, "-", rStart, curY, pmW, rowH, false, Color.Gray, s);
                    g.DrawString((int)Math.Round(_settings.DeltaOpacity * 100) + "%", fMono, Brushes.White,
                        new RectangleF(rStart + pmW, curY, valW, rowH), centerFmt);
                    _btnDeltaOpacPlus = DrawButton(g, fBtn, "+", rStart + pmW + valW, curY, pmW, rowH, false, Color.Gray, s);

                    _btnDeltaReset = DrawButton(g, fBtn, "RESET POSITION & SIZE", sx, resetY, sw, rowH, false, Color.FromArgb(56, 189, 248), s);

                    DrawDeltaBarPreview(g, viewportRect, s);
                }
                else
                {
                    // =====================================================
                    // TAB 3: GARAGE61 COACH SETTINGS + PREVIEW
                    // =====================================================
                    g.DrawString("GARAGE61 COACH SETTINGS", fCardTitle, Brushes.White,
                        new RectangleF(sx, curY, sw, rowH), leftFmt);
                    curY += rowH + (int)Math.Round(4 * s);

                    int gRowH = (int)Math.Round(27 * s);
                    int gGapY = (int)Math.Round(8 * s);

                    g.DrawString("Widget Status (Now)", fLabel, Brushes.Gainsboro, new RectangleF(sx, curY, sw - optW, gRowH), leftFmt);
                    _btnG61Toggle = DrawButton(
                        g, fBtn,
                        _settings.Garage61Enabled ? "OPEN (VISIBLE)" : "CLOSED (HIDDEN)",
                        rStart, curY, optW, gRowH,
                        _settings.Garage61Enabled,
                        _settings.Garage61Enabled ? Color.FromArgb(34, 197, 94) : Color.FromArgb(107, 114, 128),
                        s
                    );
                    curY += gRowH + gGapY;

                    g.DrawString("Open Automatically on Launch", fLabel, Brushes.Gainsboro, new RectangleF(sx, curY, sw - optW, gRowH), leftFmt);
                    _btnG61AutoOpen = DrawButton(
                        g, fBtn,
                        _settings.Garage61AutoOpen ? "YES (AUTO-OPEN)" : "NO (MANUAL)",
                        rStart, curY, optW, gRowH,
                        _settings.Garage61AutoOpen,
                        _settings.Garage61AutoOpen ? Color.FromArgb(56, 189, 248) : Color.FromArgb(107, 114, 128),
                        s
                    );
                    curY += gRowH + gGapY;

                    int refGap = (int)Math.Round(6 * s);
                    int stW = (sw - refGap * 2) / 3;
                    _btnG61StyleA = DrawButton(g, fBtn, "OPT A: TRACE", sx, curY, stW, gRowH,
                        _settings.Garage61Style == Garage61LayoutStyle.OptionA_LookaheadTraceAndPedals, Color.FromArgb(56, 189, 248), s);
                    _btnG61StyleB = DrawButton(g, fBtn, "OPT B: BAR", sx + stW + refGap, curY, stW, gRowH,
                        _settings.Garage61Style == Garage61LayoutStyle.OptionB_CompactBrakeCoach, Color.FromArgb(244, 63, 94), s);
                    _btnG61StyleC = DrawButton(g, fBtn, "OPT C: AUDIT", sx + (stW + refGap) * 2, curY, sw - (stW + refGap) * 2, gRowH,
                        _settings.Garage61Style == Garage61LayoutStyle.OptionC_ProBoxWithCornerAudit, Color.FromArgb(192, 132, 252), s);
                    curY += gRowH + gGapY;

                    g.DrawString("Braking Audio Cues", fLabel, Brushes.Gainsboro, new RectangleF(sx, curY, sw - optW, gRowH), leftFmt);
                    int audW = (int)Math.Round(optW * 0.65f);
                    int tstW = optW - audW - 4;
                    _btnG61AudioToggle = DrawButton(
                        g, fBtn,
                        _settings.Garage61AudioEnabled ? "BEEPS ON" : "MUTED",
                        rStart, curY, audW, gRowH,
                        _settings.Garage61AudioEnabled, Color.FromArgb(34, 197, 94), s
                    );
                    _btnG61AudioTest = DrawButton(g, fBtn, "TEST", rStart + audW + 4, curY, tstW, gRowH, false, Color.FromArgb(56, 189, 248), s);
                    curY += gRowH + gGapY;

                    int rngW = (optW - 8) / 3;
                    g.DrawString("Lookahead Window", fLabel, Brushes.Gainsboro, new RectangleF(sx, curY, sw - optW, gRowH), leftFmt);
                    _btnG61Look80 = DrawButton(g, fBtn, "+80m", rStart, curY, rngW, gRowH,
                        _settings.Garage61LookaheadMeters == 80, Color.FromArgb(56, 189, 248), s);
                    _btnG61Look120 = DrawButton(g, fBtn, "+120m", rStart + rngW + 4, curY, rngW, gRowH,
                        _settings.Garage61LookaheadMeters == 120, Color.FromArgb(56, 189, 248), s);
                    _btnG61Look180 = DrawButton(g, fBtn, "+180m", rStart + (rngW + 4) * 2, curY, optW - (rngW + 4) * 2, gRowH,
                        _settings.Garage61LookaheadMeters == 180, Color.FromArgb(56, 189, 248), s);
                    curY += gRowH + gGapY;

                    g.DrawString("Reference Lap CSV", fLabel, Brushes.Gainsboro, new RectangleF(sx, curY, sw - optW, gRowH), leftFmt);
                    int csvW = (int)Math.Round(optW * 0.65f);
                    int defW = optW - csvW - 4;
                    _btnG61LoadCsv = DrawButton(g, fBtn, "LOAD CSV...", rStart, curY, csvW, gRowH, true, Color.FromArgb(34, 197, 94), s);
                    _btnG61DefaultLap = DrawButton(g, fBtn, "SPA", rStart + csvW + 4, curY, defW, gRowH, string.IsNullOrEmpty(_settings.Garage61CsvPath), Color.FromArgb(56, 189, 248), s);
                    curY += gRowH + gGapY;

                    g.DrawString("Font Scale", fLabel, Brushes.Gainsboro, new RectangleF(sx, curY, sw - optW, gRowH), leftFmt);
                    _btnG61FontMinus = DrawButton(g, fBtn, "-", rStart, curY, pmW, gRowH, false, Color.Gray, s);
                    g.DrawString((int)Math.Round(_settings.Garage61FontScale * 100) + "%", fMono, Brushes.White,
                        new RectangleF(rStart + pmW, curY, valW, gRowH), centerFmt);
                    _btnG61FontPlus = DrawButton(g, fBtn, "+", rStart + pmW + valW, curY, pmW, gRowH, false, Color.Gray, s);
                    curY += gRowH + gGapY;

                    g.DrawString("Window Opacity", fLabel, Brushes.Gainsboro, new RectangleF(sx, curY, sw - optW, gRowH), leftFmt);
                    _btnG61OpacMinus = DrawButton(g, fBtn, "-", rStart, curY, pmW, gRowH, false, Color.Gray, s);
                    g.DrawString((int)Math.Round(_settings.Garage61Opacity * 100) + "%", fMono, Brushes.White,
                        new RectangleF(rStart + pmW, curY, valW, gRowH), centerFmt);
                    _btnG61OpacPlus = DrawButton(g, fBtn, "+", rStart + pmW + valW, curY, pmW, gRowH, false, Color.Gray, s);

                    _btnG61Reset = DrawButton(g, fBtn, "RESET POSITION & SIZE", sx, resetY, sw, rowH, false, Color.FromArgb(56, 189, 248), s);

                    DrawGarage61Preview(g, viewportRect, s);
                }

                // 4. Footer Status Bar
                int footY = Height - footerH;
                using (SolidBrush fb = new SolidBrush(Color.FromArgb(15, 19, 28)))
                {
                    g.FillRectangle(fb, 0, footY, Width, footerH);
                }

                string statusTxt = (_lastSnapshot != null && _lastSnapshot.IsLiveSource)
                    ? (_lastSnapshot.Connected ? "STATUS: LIVE iRACING SDK CONNECTED" : "STATUS: WAITING FOR iRACING SIMULATOR...")
                    : "STATUS: BUILT-IN 20-CAR + 60Hz DELTA & GARAGE61 SIMULATOR ACTIVE";
                Color statusCol = (_lastSnapshot != null && _lastSnapshot.IsLiveSource && _lastSnapshot.Connected)
                    ? Color.FromArgb(74, 222, 128)
                    : Color.FromArgb(56, 189, 248);

                using (SolidBrush sb = new SolidBrush(statusCol))
                {
                    g.DrawString(statusTxt, fBtn, sb, new RectangleF(pad, footY, Width * 0.55f, footerH), leftFmt);
                }

                using (SolidBrush hb = new SolidBrush(Color.FromArgb(156, 163, 175)))
                {
                    g.DrawString("Hover any unlocked overlay -> top-right icon toggles Resize (⤢) vs Font Size (Aa)", fLabel, hb,
                        new RectangleF(Width * 0.42f, footY, Width * 0.58f - pad, footerH), rightFmt);
                }

                using (Pen borderPen = new Pen(Color.FromArgb(80, 56, 189, 248), 1.5f))
                {
                    g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
                }
            }
        }
    }

    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            DpiHelper.EnablePerMonitorHighDpi();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ControlCenterForm());
        }
    }
}
