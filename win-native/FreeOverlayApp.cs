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
        public bool Locked = false;
        public TelemetryMode Mode = TelemetryMode.Auto;

        // Standings Settings
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
                sb.AppendLine("Mode=" + (int)Mode);
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
                    else if (k == "Mode" && int.TryParse(v, out iv)) Mode = (TelemetryMode)(iv % 3);
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
                    else if (k == "Garage61LookaheadMeters" && int.TryParse(v, out iv)) Garage61LookaheadMeters = Math.Max(40, Math.Min(350, iv));
                    else if (k == "Garage61FontScale" && float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out fv)) Garage61FontScale = Math.Max(0.65f, Math.Min(1.75f, fv));
                    else if (k == "Garage61Opacity" && float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out fv)) Garage61Opacity = Math.Max(0.45f, Math.Min(1.0f, fv));
                    else if (k == "Garage61X" && int.TryParse(v, out iv)) Garage61X = iv;
                    else if (k == "Garage61Y" && int.TryParse(v, out iv)) Garage61Y = iv;
                    else if (k == "Garage61W" && int.TryParse(v, out iv)) Garage61W = iv;
                    else if (k == "Garage61H" && int.TryParse(v, out iv)) Garage61H = iv;
                    else if (k == "Garage61CsvPath") Garage61CsvPath = v;
                }
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

    public class Garage61LapManager
    {
        public const int BUCKETS = 1000;

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
        private bool _playerBrakingInZone = false;
        private int _activeZoneIdx = -1;
        public float CurrentBrakeSpotDeltaMeters = 0f;
        public bool HasCurrentBrakeSpotDelta = false;
        public float CurrentZonePlayerPeakBrake = 0f;

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

        public float GetRefBrakeAtPct(float distPct)
        {
            float wrapped = distPct - (float)Math.Floor(distPct);
            int idx = Math.Max(0, Math.Min(BUCKETS - 1, (int)(wrapped * BUCKETS)));
            return RefBrake[idx];
        }

        public float GetRefThrottleAtPct(float distPct)
        {
            float wrapped = distPct - (float)Math.Floor(distPct);
            int idx = Math.Max(0, Math.Min(BUCKETS - 1, (int)(wrapped * BUCKETS)));
            return RefThrottle[idx];
        }

        public float GetRefSpeedAtPct(float distPct)
        {
            float wrapped = distPct - (float)Math.Floor(distPct);
            int idx = Math.Max(0, Math.Min(BUCKETS - 1, (int)(wrapped * BUCKETS)));
            return RefSpeedKph[idx];
        }

        public int GetRefGearAtPct(float distPct)
        {
            float wrapped = distPct - (float)Math.Floor(distPct);
            int idx = Math.Max(0, Math.Min(BUCKETS - 1, (int)(wrapped * BUCKETS)));
            return RefGear[idx];
        }

        public float GetPlayerBrakeAtPct(float distPct)
        {
            float wrapped = distPct - (float)Math.Floor(distPct);
            int idx = Math.Max(0, Math.Min(BUCKETS - 1, (int)(wrapped * BUCKETS)));
            return PlayerBrakeTrace[idx];
        }

        public float GetPlayerThrottleAtPct(float distPct)
        {
            float wrapped = distPct - (float)Math.Floor(distPct);
            int idx = Math.Max(0, Math.Min(BUCKETS - 1, (int)(wrapped * BUCKETS)));
            return PlayerThrottleTrace[idx];
        }

        public void UpdatePlayerTelemetry(float distPct, float playerBrake, float playerThrottle, float trackLengthMeters)
        {
            float wrapped = distPct - (float)Math.Floor(distPct);
            int b = Math.Max(0, Math.Min(BUCKETS - 1, (int)(wrapped * BUCKETS)));

            if (_lastBucket >= 0 && b != _lastBucket)
            {
                int step = _lastBucket;
                int guard = 0;
                while (step != b && guard < 40)
                {
                    step = (step + 1) % BUCKETS;
                    PlayerBrakeTrace[step] = playerBrake;
                    PlayerThrottleTrace[step] = playerThrottle;
                    guard++;
                }
            }
            else
            {
                PlayerBrakeTrace[b] = playerBrake;
                PlayerThrottleTrace[b] = playerThrottle;
            }
            _lastBucket = b;

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
                    CurrentBrakeSpotDeltaMeters = (wrapped - z.StartPct) * Math.Max(1000f, trackLengthMeters);
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
            Match m = Regex.Match(chunk, @"^\s*" + key + @":\s*(.+)$", RegexOptions.Multiline);
            if (!m.Success) return defVal;
            return m.Groups[1].Value.Trim().Trim('"', '\'');
        }

        /// <summary>
        /// Parses the official track sectors from iRacing's SessionInfo YAML:
        /// SplitTimeInfo:
        ///  Sectors:
        ///  - SectorNum: 0
        ///    SectorStartPct: 0.000000
        ///  - SectorNum: 1
        ///    SectorStartPct: 0.318452
        ///  ...
        /// </summary>
        private static float[] ExtractTrackSectorsFromYaml(string yaml)
        {
            int splitIdx = yaml.IndexOf("SplitTimeInfo:");
            if (splitIdx >= 0)
            {
                string tail = yaml.Substring(splitIdx);
                // Stop at the next top-level YAML section (unindented key at start of line)
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

        public SessionSnapshot TryReadLive()
        {
            try
            {
                using (MemoryMappedFile mmf = MemoryMappedFile.OpenExisting("Local\\IRSDKMemMapFileName", MemoryMappedFileRights.Read))
                {
                    using (MemoryMappedViewAccessor acc = mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read))
                    {
                        int status = acc.ReadInt32(4);
                        if ((status & 1) == 0) return null;

                        int sessionInfoLen = acc.ReadInt32(16);
                        int sessionInfoOffset = acc.ReadInt32(20);
                        int numVars = acc.ReadInt32(24);
                        int varHeaderOffset = acc.ReadInt32(28);
                        int numBuf = acc.ReadInt32(32);
                        int bufLen = acc.ReadInt32(36);

                        if (numVars <= 0 || bufLen <= 0 || sessionInfoLen <= 0) return null;

                        int bestTick = -1;
                        int bestBufOffset = 0;
                        for (int i = 0; i < numBuf && i < 4; i++)
                        {
                            int bOff = 48 + i * 16;
                            int tick = acc.ReadInt32(bOff);
                            int off = acc.ReadInt32(bOff + 4);
                            if (tick > bestTick)
                            {
                                bestTick = tick;
                                bestBufOffset = off;
                            }
                        }

                        byte[] yamlBytes = new byte[sessionInfoLen];
                        acc.ReadArray(sessionInfoOffset, yamlBytes, 0, sessionInfoLen);
                        string yaml = ReadCString(yamlBytes, 0, sessionInfoLen);

                        byte[] varHeaders = new byte[numVars * 144];
                        acc.ReadArray(varHeaderOffset, varHeaders, 0, varHeaders.Length);

                        byte[] dataBuf = new byte[bufLen];
                        acc.ReadArray(bestBufOffset, dataBuf, 0, bufLen);

                        int playerCarIdx = 0;
                        int playerTrackSurface = 3;
                        double sessionTimeRemain = 0;
                        int sessionLapsRemain = 0;
                        float trackTemp = 32f;

                        // Native 60Hz Delta Bar variables from iRacing SDK
                        float deltaSessionBest = 0f;
                        float deltaSessionBestDD = 0f;
                        float lapBestLapTime = 0f;

                        float deltaSessionOpt = 0f;
                        float deltaSessionOptDD = 0f;

                        float deltaBest = 0f;
                        float deltaBestDD = 0f;

                        float deltaLast = 0f;
                        float deltaLastDD = 0f;
                        float lapLastLapTime = 0f;

                        // Player live pedal, speed, gear & distance telemetry for Garage61 Braking Coach
                        float playerBrakeRaw = 0f;
                        float playerThrottleRaw = 0f;
                        float playerSpeedMs = 0f;
                        int playerGearVal = 0;
                        float scalarLapDistPct = -1f;

                        int[] pos = new int[64];
                        int[] lap = new int[64];
                        float[] lastLap = new float[64];
                        float[] bestLap = new float[64];
                        float[] lapDist = new float[64];
                        float[] f2Time = new float[64];
                        bool[] onPit = new bool[64];

                        for (int v = 0; v < numVars; v++)
                        {
                            int hOff = v * 144;
                            int vOffset = BitConverter.ToInt32(varHeaders, hOff + 4);
                            int vCount = BitConverter.ToInt32(varHeaders, hOff + 8);
                            string vName = ReadCString(varHeaders, hOff + 16, 32);

                            if (vName == "PlayerCarIdx") playerCarIdx = BitConverter.ToInt32(dataBuf, vOffset);
                            else if (vName == "PlayerTrackSurface") playerTrackSurface = BitConverter.ToInt32(dataBuf, vOffset);
                            else if (vName == "SessionTimeRemain") sessionTimeRemain = BitConverter.ToDouble(dataBuf, vOffset);
                            else if (vName == "SessionLapsRemainEx") sessionLapsRemain = BitConverter.ToInt32(dataBuf, vOffset);
                            else if (vName == "TrackTempCrew") trackTemp = BitConverter.ToSingle(dataBuf, vOffset);
                            else if (vName == "LapDeltaToSessionBestLap") deltaSessionBest = BitConverter.ToSingle(dataBuf, vOffset);
                            else if (vName == "LapDeltaToSessionBestLap_DD") deltaSessionBestDD = BitConverter.ToSingle(dataBuf, vOffset);
                            else if (vName == "LapBestLapTime") lapBestLapTime = BitConverter.ToSingle(dataBuf, vOffset);
                            else if (vName == "LapDeltaToSessionOptimalLap") deltaSessionOpt = BitConverter.ToSingle(dataBuf, vOffset);
                            else if (vName == "LapDeltaToSessionOptimalLap_DD") deltaSessionOptDD = BitConverter.ToSingle(dataBuf, vOffset);
                            else if (vName == "LapDeltaToBestLap") deltaBest = BitConverter.ToSingle(dataBuf, vOffset);
                            else if (vName == "LapDeltaToBestLap_DD") deltaBestDD = BitConverter.ToSingle(dataBuf, vOffset);
                            else if (vName == "LapDeltaToSessionLastLap") deltaLast = BitConverter.ToSingle(dataBuf, vOffset);
                            else if (vName == "LapDeltaToSessionLastLap_DD") deltaLastDD = BitConverter.ToSingle(dataBuf, vOffset);
                            else if (vName == "LapLastLapTime") lapLastLapTime = BitConverter.ToSingle(dataBuf, vOffset);
                            else if (vName == "Brake") playerBrakeRaw = BitConverter.ToSingle(dataBuf, vOffset);
                            else if (vName == "Throttle") playerThrottleRaw = BitConverter.ToSingle(dataBuf, vOffset);
                            else if (vName == "Speed") playerSpeedMs = BitConverter.ToSingle(dataBuf, vOffset);
                            else if (vName == "Gear") playerGearVal = BitConverter.ToInt32(dataBuf, vOffset);
                            else if (vName == "LapDistPct") scalarLapDistPct = BitConverter.ToSingle(dataBuf, vOffset);
                            else if (vName == "CarIdxPosition")
                            {
                                for (int c = 0; c < Math.Min(64, vCount); c++) pos[c] = BitConverter.ToInt32(dataBuf, vOffset + c * 4);
                            }
                            else if (vName == "CarIdxLap")
                            {
                                for (int c = 0; c < Math.Min(64, vCount); c++) lap[c] = BitConverter.ToInt32(dataBuf, vOffset + c * 4);
                            }
                            else if (vName == "CarIdxLastLapTime")
                            {
                                for (int c = 0; c < Math.Min(64, vCount); c++) lastLap[c] = BitConverter.ToSingle(dataBuf, vOffset + c * 4);
                            }
                            else if (vName == "CarIdxBestLapTime")
                            {
                                for (int c = 0; c < Math.Min(64, vCount); c++) bestLap[c] = BitConverter.ToSingle(dataBuf, vOffset + c * 4);
                            }
                            else if (vName == "CarIdxLapDistPct")
                            {
                                for (int c = 0; c < Math.Min(64, vCount); c++) lapDist[c] = BitConverter.ToSingle(dataBuf, vOffset + c * 4);
                            }
                            else if (vName == "CarIdxF2Time")
                            {
                                for (int c = 0; c < Math.Min(64, vCount); c++) f2Time[c] = BitConverter.ToSingle(dataBuf, vOffset + c * 4);
                            }
                            else if (vName == "CarIdxOnPitRoad")
                            {
                                for (int c = 0; c < Math.Min(64, vCount); c++) onPit[c] = dataBuf[vOffset + c] != 0;
                            }
                        }

                        string trackName = ExtractYamlField(yaml, "TrackDisplayName", "iRacing Circuit");
                        int driversIdx = yaml.IndexOf("Drivers:");
                        if (driversIdx < 0) return null;

                        string driversSection = yaml.Substring(driversIdx);
                        string[] blocks = Regex.Split(driversSection, @"\r?\n\s*-\s*CarIdx:");
                        List<DriverRow> rows = new List<DriverRow>();
                        float sessionBest = float.MaxValue;

                        for (int i = 1; i < blocks.Length; i++)
                        {
                            string chunk = "CarIdx:" + blocks[i];
                            if (ExtractYamlField(chunk, "CarIsPaceCar", "0") == "1") continue;
                            if (ExtractYamlField(chunk, "IsSpectator", "0") == "1") continue;

                            int cIdx;
                            if (!int.TryParse(ExtractYamlField(chunk, "CarIdx", "0"), out cIdx)) continue;
                            if (cIdx < 0 || cIdx >= 64) continue;

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
                            float bLap = bestLap[cIdx];
                            if (bLap > 0f && bLap < sessionBest) sessionBest = bLap;

                            DriverRow row = new DriverRow();
                            row.CarIdx = cIdx;
                            row.Position = pos[cIdx];
                            row.CarNumber = ExtractYamlField(chunk, "CarNumber", cIdx.ToString());
                            row.UserName = ExtractYamlField(chunk, "UserName", "Driver #" + cIdx);
                            row.ClubName = club;
                            row.CountryCode = CountryResolver.Resolve(club, null);
                            row.IRating = ir;
                            row.LicClass = licClass;
                            row.SafetyRating = srVal;
                            row.LastLapSec = lastLap[cIdx] > 0f ? lastLap[cIdx] : 0f;
                            row.BestLapSec = bLap > 0f ? bLap : 0f;
                            row.GapToLeaderSec = Math.Max(0f, f2Time[cIdx]);
                            row.Lap = lap[cIdx];
                            row.LapDistPct = lapDist[cIdx];
                            row.OnPitRoad = onPit[cIdx];
                            row.IsPlayer = (cIdx == playerCarIdx);
                            rows.Add(row);
                        }

                        if (rows.Count == 0) return null;

                        rows.Sort(delegate(DriverRow a, DriverRow b)
                        {
                            if (a.Position > 0 && b.Position > 0) return a.Position.CompareTo(b.Position);
                            if (a.Position > 0) return -1;
                            if (b.Position > 0) return 1;
                            float progA = a.Lap + a.LapDistPct;
                            float progB = b.Lap + b.LapDistPct;
                            if (Math.Abs(progA - progB) > 0.001f) return progB.CompareTo(progA);
                            return b.IRating.CompareTo(a.IRating);
                        });

                        int irSum = 0;
                        for (int i = 0; i < rows.Count; i++) irSum += rows[i].IRating;
                        int sof = irSum / rows.Count;

                        int maxLap = 1;
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
                                d.IntervalAheadSec = Math.Max(0f, d.GapToLeaderSec - rows[i - 1].GapToLeaderSec);
                            }

                            if (d.LastLapSec > 0f && Math.Abs(d.LastLapSec - sessionBest) < 0.003f)
                                d.LastLapStatus = LapStatus.SessionBest;
                            else if (d.LastLapSec > 0f && d.BestLapSec > 0f && Math.Abs(d.LastLapSec - d.BestLapSec) < 0.003f)
                                d.LastLapStatus = LapStatus.PersonalBest;
                            else
                                d.LastLapStatus = LapStatus.Normal;

                            d.IRatingDelta = CountryResolver.EstimateIRatingDelta(d.Position, rows.Count, d.IRating, sof);
                        }

                        // Pull official track sectors from iRacing SDK YAML (SplitTimeInfo -> Sectors)
                        float[] sectorPcts = ExtractTrackSectorsFromYaml(yaml);
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

                        float playerPct = (playerCarIdx >= 0 && playerCarIdx < 64) ? lapDist[playerCarIdx] : 0f;
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
                        dt.SessionBestLapSec = lapBestLapTime > 0f ? lapBestLapTime : (sessionBest < float.MaxValue ? sessionBest : 0f);
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
                        dt.SectorDeltasSessionBest = (float[])_secSplitBest.Clone();
                        dt.SectorDeltasOptimal = (float[])_secSplitOpt.Clone();
                        dt.SectorDeltasPersonalBest = (float[])_secSplitPers.Clone();
                        dt.SectorDeltasLastLap = (float[])_secSplitLast.Clone();
                        dt.SectorDone = (bool[])_secDone.Clone();
                        dt.OffTrack = (playerTrackSurface == 0);

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

                        SessionSnapshot snap = new SessionSnapshot();
                        snap.Connected = true;
                        snap.IsLiveSource = true;
                        snap.TrackName = trackName;
                        snap.SessionType = "RACE";
                        snap.StrengthOfField = sof;
                        snap.CurrentLap = maxLap;
                        snap.TotalLaps = Math.Max(0, sessionLapsRemain);
                        snap.SessionTimeRemainSec = Math.Max(0.0, sessionTimeRemain);
                        snap.TrackTempC = trackTemp;
                        snap.Drivers = rows;
                        snap.Delta = dt;
                        snap.PlayerLapDistPct = scalarLapDistPct >= 0f ? scalarLapDistPct : playerPct;
                        snap.TrackLengthMeters = Math.Max(500f, trackLenMeters);
                        snap.PlayerBrakePct = Math.Max(0f, Math.Min(100f, playerBrakeRaw * 100.0f));
                        snap.PlayerThrottlePct = Math.Max(0f, Math.Min(100f, playerThrottleRaw * 100.0f));
                        snap.PlayerSpeedKph = Math.Max(0f, playerSpeedMs * 3.6f);
                        snap.PlayerGear = playerGearVal;
                        return snap;
                    }
                }
            }
            catch
            {
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
            ResetPositionAndSize(false);

            if (_settings.DeltaW > 100 && _settings.DeltaH > 50)
            {
                Width = _settings.DeltaW;
                Height = _settings.DeltaH;
            }
            if (_settings.DeltaX >= 0 && _settings.DeltaY >= 0)
            {
                Location = new Point(_settings.DeltaX, _settings.DeltaY);
            }
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

        private void SaveGeometry()
        {
            _settings.DeltaX = Left;
            _settings.DeltaY = Top;
            _settings.DeltaW = Width;
            _settings.DeltaH = Height;
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

            using (Font fLabel = new Font("Segoe UI", 10.5f * s, FontStyle.Bold, GraphicsUnit.Pixel))
            using (Font fSubMono = new Font("Consolas", 12.0f * s, FontStyle.Bold, GraphicsUnit.Pixel))
            using (Font fBigDelta = new Font("Consolas", 21.0f * s, FontStyle.Bold, GraphicsUnit.Pixel))
            using (Font fIconAa = new Font("Segoe UI", 11.5f * s, FontStyle.Bold, GraphicsUnit.Pixel))
            {
                StringFormat centerFmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                StringFormat leftFmt = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
                StringFormat rightFmt = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };

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
        private Rectangle _rectLockBtn;

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
            ResetPositionAndSize(false);

            if (_settings.StandingsW > 200 && _settings.StandingsH > 120)
            {
                Width = _settings.StandingsW;
                Height = _settings.StandingsH;
            }
            if (_settings.StandingsX >= 0 && _settings.StandingsY >= 0)
            {
                Location = new Point(_settings.StandingsX, _settings.StandingsY);
            }
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

        private void SaveGeometry()
        {
            _settings.StandingsX = Left;
            _settings.StandingsY = Top;
            _settings.StandingsW = Width;
            _settings.StandingsH = Height;
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
                     _rectModeCycleBtn.Contains(e.Location) || _rectGapBtn.Contains(e.Location))
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

        private static void DrawCountryFlag(Graphics g, int x, int y, int w, int h, string code, float s)
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

            using (Font fBrand = new Font("Segoe UI", 11.5f * s, FontStyle.Bold, GraphicsUnit.Pixel))
            using (Font fSmallBold = new Font("Segoe UI", 10.2f * s, FontStyle.Bold, GraphicsUnit.Pixel))
            using (Font fIconAa = new Font("Segoe UI", 11.5f * s, FontStyle.Bold, GraphicsUnit.Pixel))
            using (Font fHeader = new Font("Segoe UI", 11.5f * s, FontStyle.Bold, GraphicsUnit.Pixel))
            using (Font fRow = new Font("Segoe UI", 12.2f * s, FontStyle.Regular, GraphicsUnit.Pixel))
            using (Font fRowBold = new Font("Segoe UI", 12.2f * s, FontStyle.Bold, GraphicsUnit.Pixel))
            using (Font fMono = new Font("Consolas", 12.6f * s, FontStyle.Bold, GraphicsUnit.Pixel))
            {
                StringFormat centerFmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                StringFormat rightFmt = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
                StringFormat leftFmt = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };

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
                string metaStr = string.Format(
                    CultureInfo.InvariantCulture,
                    "LAP {0}/{1}  •  REM {2}  •  SOF {3:N0}  •  {4:0}C",
                    _snapshot.CurrentLap,
                    _snapshot.TotalLaps,
                    FormatTimer(_snapshot.SessionTimeRemainSec),
                    _snapshot.StrengthOfField,
                    _snapshot.TrackTempC
                );
                SizeF metaSz = g.MeasureString(metaStr, fSmallBold);
                int metaW = (int)Math.Ceiling(metaSz.Width) + (int)(12 * s);
                int availTrackW = Math.Max(50, Width - trackX - metaW - (int)(10 * s));

                StringFormat trackFmt = new StringFormat
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.NoWrap
                };
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
                int colNumW = (int)Math.Round(42 * s);

                int colFlagX = colNumX + colNumW + (int)Math.Round(4 * s);
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
                    g.DrawString("#", fSmallBold, mutedB, new RectangleF(colNumX, colHeadY, colNumW, colHeaderH), leftFmt);
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

                    int barW = Math.Max(2, (int)Math.Round(3 * s));
                    using (SolidBrush classBar = new SolidBrush(Color.FromArgb(255, 82, 82)))
                    {
                        g.FillRectangle(classBar, colNumX, badgeY + 1, barW, badgeH - 2);
                    }
                    g.DrawString("#" + d.CarNumber, fRowBold, Brushes.WhiteSmoke,
                        new RectangleF(colNumX + barW + 3 * s, rowY, colNumW - barW, rowH), leftFmt);

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
            ResetPositionAndSize(false);

            if (_settings.Garage61W > 200 && _settings.Garage61H > 70)
            {
                Width = _settings.Garage61W;
                Height = _settings.Garage61H;
            }
            if (_settings.Garage61X >= 0 && _settings.Garage61Y >= 0)
            {
                Location = new Point(_settings.Garage61X, _settings.Garage61Y);
            }
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

        private void SaveGeometry()
        {
            _settings.Garage61X = Left;
            _settings.Garage61Y = Top;
            _settings.Garage61W = Width;
            _settings.Garage61H = Height;
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
            else if (_rectCornerModeBtn.Contains(e.Location) || _rectStyleCycleBtn.Contains(e.Location))
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
                // Approaching Braking Zone -> Countdown bar & distance
                using (SolidBrush bg = new SolidBrush(Color.FromArgb(20, 26, 38))) g.FillRectangle(bg, rect);
                using (Pen bp = new Pen(Color.FromArgb(90, 56, 189, 248), 1f)) g.DrawRectangle(bp, rect);

                string leftTxt = string.Format(CultureInfo.InvariantCulture, "NEXT BRAKE ({0}): IN {1:0}m", zName, distToStart);
                using (SolidBrush sb = new SolidBrush(Color.FromArgb(125, 211, 252)))
                {
                    g.DrawString(leftTxt, fBold, sb, new RectangleF(rect.Left + 8 * s, rect.Top, rect.Width * 0.45f, rect.Height), leftFmt);
                }

                // Closing-in countdown progress bar in the middle
                int barW = (int)Math.Round(rect.Width * 0.24f);
                int barH = Math.Max(6, (int)Math.Round(9 * s));
                Rectangle progBg = new Rectangle(rect.Left + (rect.Width - barW) / 2, rect.Top + (rect.Height - barH) / 2, barW, barH);
                using (SolidBrush pbg = new SolidBrush(Color.FromArgb(30, 41, 59))) g.FillRectangle(pbg, progBg);
                float fillPct = Math.Max(0f, Math.Min(1f, (220f - distToStart) / 220f));
                Color barCol = distToStart < 40f ? Color.FromArgb(244, 63, 94) : (distToStart < 95f ? Color.FromArgb(251, 191, 36) : Color.FromArgb(56, 189, 248));
                using (SolidBrush pfg = new SolidBrush(barCol))
                {
                    g.FillRectangle(pfg, progBg.Left, progBg.Top, (int)Math.Round(progBg.Width * fillPct), progBg.Height);
                }

                string rightTxt = string.Format(CultureInfo.InvariantCulture, "TARGET PEAK: {0:0}%", peakTarget);
                using (SolidBrush rb = new SolidBrush(Color.FromArgb(251, 113, 133)))
                {
                    g.DrawString(rightTxt, fMono, rb, new RectangleF(rect.Right - rect.Width * 0.32f, rect.Top, rect.Width * 0.32f - 8 * s, rect.Height), rightFmt);
                }
            }
            else if (distToStart <= 3.0f && z != null)
            {
                // Actively inside Braking Zone -> Show Brake Spot Delta + Live Pressure Diff
                using (SolidBrush bg = new SolidBrush(Color.FromArgb(48, 136, 19, 55))) g.FillRectangle(bg, rect);
                using (Pen bp = new Pen(Color.FromArgb(160, 244, 63, 94), 1f)) g.DrawRectangle(bp, rect);

                float spotDelta = _g61.HasCurrentBrakeSpotDelta ? _g61.CurrentBrakeSpotDeltaMeters : 2.4f;
                string spotTxt = string.Format(
                    CultureInfo.InvariantCulture,
                    "BRAKE PT ({0}): {1:0.0}m {2}",
                    zName,
                    Math.Abs(spotDelta),
                    spotDelta >= 0f ? "LATE" : "EARLY"
                );
                using (SolidBrush ab = new SolidBrush(Color.FromArgb(253, 224, 71)))
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

            int numSamples = 90;
            List<PointF> g61BrakePts = new List<PointF>(numSamples + 1);
            List<PointF> g61ThrotPts = new List<PointF>(numSamples + 1);
            List<PointF> youBrakePts = new List<PointF>(numSamples + 1);
            List<PointF> youThrotPts = new List<PointF>(numSamples + 1);

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
                g61BrakePts.Add(new PointF(x, yb));
                g61ThrotPts.Add(new PointF(x, yt));

                if (dp <= 0.0005f)
                {
                    float pb = (i == numSamples || Math.Abs(dp) < 0.0012f) ? _snapshot.PlayerBrakePct : _g61.GetPlayerBrakeAtPct(samplePct);
                    float pt = (i == numSamples || Math.Abs(dp) < 0.0012f) ? _snapshot.PlayerThrottlePct : _g61.GetPlayerThrottleAtPct(samplePct);
                    float pyb = graphRect.Bottom - botPad - (pb / 100.0f) * plotH;
                    float pyt = graphRect.Bottom - botPad - (pt / 100.0f) * plotH;
                    youBrakePts.Add(new PointF(x, pyb));
                    youThrotPts.Add(new PointF(x, pyt));
                }
            }

            // Draw Throttle traces (G61 dashed green, Player solid green)
            using (Pen g61ThrPen = new Pen(Color.FromArgb(115, 52, 211, 153), Math.Max(1.2f, 1.4f * s)))
            {
                g61ThrPen.DashStyle = DashStyle.Dash;
                if (g61ThrotPts.Count > 1) g.DrawLines(g61ThrPen, g61ThrotPts.ToArray());
            }
            using (Pen youThrPen = new Pen(Color.FromArgb(210, 16, 185, 129), Math.Max(1.5f, 1.8f * s)))
            {
                if (youThrotPts.Count > 1) g.DrawLines(youThrPen, youThrotPts.ToArray());
            }

            // Draw Brake traces (G61 dashed cyan extending into Lookahead, Player solid bright red up to NOW)
            using (Pen g61BrkPen = new Pen(Color.FromArgb(235, 56, 189, 248), Math.Max(1.8f, 2.2f * s)))
            {
                g61BrkPen.DashStyle = DashStyle.Dash;
                if (g61BrakePts.Count > 1) g.DrawLines(g61BrkPen, g61BrakePts.ToArray());
            }
            using (Pen youBrkPen = new Pen(Color.FromArgb(255, 244, 63, 94), Math.Max(2.2f, 2.8f * s)))
            {
                if (youBrakePts.Count > 1) g.DrawLines(youBrkPen, youBrakePts.ToArray());
            }

            // Highlight any G61 Braking Point marker inside the visible window
            float wrappedNow = nowPct - (float)Math.Floor(nowPct);
            for (int z = 0; z < _g61.Zones.Count; z++)
            {
                float dp = _g61.Zones[z].StartPct - wrappedNow;
                if (dp < -0.5f) dp += 1.0f;
                if (dp > 0.5f) dp -= 1.0f;
                if (dp >= -winPastPct && dp <= winAheadPct)
                {
                    int bx = graphRect.Left + (int)Math.Round(((dp + winPastPct) / totalWinPct) * graphRect.Width);
                    using (Pen mPen = new Pen(Color.FromArgb(200, 56, 189, 248), Math.Max(1f, 1.4f * s)))
                    {
                        mPen.DashStyle = DashStyle.Dot;
                        g.DrawLine(mPen, bx, graphRect.Top + 2, bx, graphRect.Bottom - 2);
                    }
                    int tagW = (int)Math.Round(56 * s);
                    int tagH = (int)Math.Round(13 * s);
                    Rectangle tagR = new Rectangle(Math.Max(graphRect.Left + 2, Math.Min(graphRect.Right - tagW - 2, bx - tagW / 2)), graphRect.Top + 2, tagW, tagH);
                    using (SolidBrush tb = new SolidBrush(Color.FromArgb(220, 2, 132, 199))) g.FillRectangle(tb, tagR);
                    StringFormat cf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString("G61 BRK", fSmall, Brushes.White, tagR, cf);
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

            using (Font fHeader = new Font("Segoe UI", 10.5f * s, FontStyle.Bold, GraphicsUnit.Pixel))
            using (Font fSmall = new Font("Segoe UI", 9.5f * s, FontStyle.Bold, GraphicsUnit.Pixel))
            using (Font fMono = new Font("Consolas", 11.5f * s, FontStyle.Bold, GraphicsUnit.Pixel))
            using (Font fBigMono = new Font("Consolas", 15.0f * s, FontStyle.Bold, GraphicsUnit.Pixel))
            using (Font fIconAa = new Font("Segoe UI", 11.5f * s, FontStyle.Bold, GraphicsUnit.Pixel))
            {
                StringFormat leftFmt = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
                StringFormat rightFmt = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
                StringFormat centerFmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

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

                g.DrawString(_g61.SourceLabel, fHeader, Brushes.Gainsboro,
                    new RectangleF(_rectStyleCycleBtn.Right + 8 * s, topY, Width * 0.48f, topBarH), leftFmt);

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
                    g.DrawString("BRAKE", fSmall, new SolidBrush(Color.FromArgb(251, 113, 133)), new RectangleF(leftBox.Left + inPad, curY, innerW * 0.5f, 15 * s), leftFmt);
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
                    g.DrawString("THROTTLE", fSmall, new SolidBrush(Color.FromArgb(52, 211, 153)), new RectangleF(leftBox.Left + inPad, curY, innerW * 0.55f, 15 * s), leftFmt);
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

        private readonly OverlaySettings _settings;
        private readonly MockSimulator _mockSim;
        private readonly LiveIracingReader _liveReader;
        private readonly Garage61LapManager _g61Manager;
        private readonly OverlayForm _standingsForm;
        private readonly DeltaBarForm _deltaBarForm;
        private readonly Garage61OverlayForm _garage61Form;
        private readonly Timer _timer;
        private readonly NotifyIcon _trayIcon;

        private SessionSnapshot _lastSnapshot;
        private float _dpiScale = 1.0f;
        private int _tickCounter = 0;

        // Interactive Hub hit rectangles
        private Rectangle _btnLockAll;
        private Rectangle _btnCycleMode;
        private Rectangle _btnHideHub;
        private Rectangle _btnQuitApp;

        // Standings Card controls
        private Rectangle _btnStandingsToggle;
        private Rectangle _btnStandingsGap;
        private Rectangle _btnStandingsCompactIr;
        private Rectangle _btnStandingsDeltaIr;
        private Rectangle _btnStandingsFontMinus;
        private Rectangle _btnStandingsFontPlus;
        private Rectangle _btnStandingsOpacMinus;
        private Rectangle _btnStandingsOpacPlus;
        private Rectangle _btnStandingsReset;

        // Delta Bar Card controls
        private Rectangle _btnDeltaToggle;
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

        // Garage61 Card controls
        private Rectangle _btnG61Toggle;
        private Rectangle _btnG61StyleA;
        private Rectangle _btnG61StyleB;
        private Rectangle _btnG61StyleC;
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
            Width = (int)Math.Round(1080 * _dpiScale);
            Height = (int)Math.Round(560 * _dpiScale);

            ContextMenu trayMenu = new ContextMenu();
            trayMenu.MenuItems.Add("Open Free Overlay Hub (F5)", delegate(object s, EventArgs e) { ToggleHubVisibility(); });
            trayMenu.MenuItems.Add("Toggle Standings Overlay", delegate(object s, EventArgs e)
            {
                _settings.StandingsEnabled = !_settings.StandingsEnabled;
                _settings.NotifyChanged();
            });
            trayMenu.MenuItems.Add("Toggle Delta Bar Overlay", delegate(object s, EventArgs e)
            {
                _settings.DeltaBarEnabled = !_settings.DeltaBarEnabled;
                _settings.NotifyChanged();
            });
            trayMenu.MenuItems.Add("Toggle Garage61 Coach Overlay", delegate(object s, EventArgs e)
            {
                _settings.Garage61Enabled = !_settings.Garage61Enabled;
                _settings.NotifyChanged();
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

            _timer = new Timer();
            _timer.Interval = 50; // 20 FPS smooth Delta Bar & Garage61 Coach update
            _timer.Tick += OnTimerTick;
            _timer.Start();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            _dpiScale = DpiHelper.GetScale(Handle);
            Width = (int)Math.Round(1080 * _dpiScale);
            Height = (int)Math.Round(560 * _dpiScale);

            RegisterHotKey(Handle, HOTKEY_HUB, 0, (uint)Keys.F5);
            RegisterHotKey(Handle, HOTKEY_LOCK, 0, (uint)Keys.F6);
            RegisterHotKey(Handle, HOTKEY_MODE, 0, (uint)Keys.F7);
            RegisterHotKey(Handle, HOTKEY_QUIT, 0, (uint)Keys.F8);

            SyncOverlaysVisibilityAndState();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
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
            _settings.Save();
            _timer.Stop();
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
            SessionSnapshot snap = null;

            if (_settings.Mode == TelemetryMode.Live || _settings.Mode == TelemetryMode.Auto)
            {
                snap = _liveReader.TryReadLive();
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
                    snap = _mockSim.Tick(0.05);
                }
            }

            _lastSnapshot = snap;
            _g61Manager.UpdatePlayerTelemetry(snap.PlayerLapDistPct, snap.PlayerBrakePct, snap.PlayerThrottlePct, snap.TrackLengthMeters);

            if (_settings.DeltaBarEnabled && _deltaBarForm.Visible)
            {
                _deltaBarForm.UpdateSnapshot(snap);
            }

            if (_settings.Garage61Enabled && _garage61Form.Visible)
            {
                _garage61Form.UpdateSnapshot(snap);
            }

            // Update Standings at ~5 Hz (every 4 ticks = 200ms)
            if (_settings.StandingsEnabled && _standingsForm.Visible && (_tickCounter % 4 == 0))
            {
                _standingsForm.UpdateSnapshot(snap);
            }

            if (Visible && (_tickCounter % 10 == 0))
            {
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
                    Width = (int)Math.Round(1080 * _dpiScale);
                    Height = (int)Math.Round(560 * _dpiScale);
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

            // Standings card buttons
            if (_btnStandingsToggle.Contains(pt))
            {
                _settings.StandingsEnabled = !_settings.StandingsEnabled;
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

            // Delta Bar card buttons
            if (_btnDeltaToggle.Contains(pt))
            {
                _settings.DeltaBarEnabled = !_settings.DeltaBarEnabled;
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

            // Garage61 Coach card buttons
            if (_btnG61Toggle.Contains(pt))
            {
                _settings.Garage61Enabled = !_settings.Garage61Enabled;
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

            // Drag Hub window by top header bar
            if (pt.Y < (int)Math.Round(60 * _dpiScale))
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

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            float s = _dpiScale;
            int headerH = (int)Math.Round(56 * s);
            int footerH = (int)Math.Round(36 * s);
            int pad = (int)Math.Round(16 * s);

            using (Font fTitle = new Font("Segoe UI", 16.0f * s, FontStyle.Bold, GraphicsUnit.Pixel))
            using (Font fCardTitle = new Font("Segoe UI", 12.8f * s, FontStyle.Bold, GraphicsUnit.Pixel))
            using (Font fLabel = new Font("Segoe UI", 11.2f * s, FontStyle.Regular, GraphicsUnit.Pixel))
            using (Font fBtn = new Font("Segoe UI", 10.6f * s, FontStyle.Bold, GraphicsUnit.Pixel))
            using (Font fMono = new Font("Consolas", 11.5f * s, FontStyle.Bold, GraphicsUnit.Pixel))
            {
                StringFormat leftFmt = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
                StringFormat rightFmt = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
                StringFormat centerFmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

                // Header bar
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

                // Header Right Controls
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

                // Three Side-by-Side Overlay Configuration Cards
                int cardsTop = headerH + pad;
                int cardsH = Height - cardsTop - footerH - pad;
                int cardW = (Width - pad * 4) / 3;

                Rectangle card1 = new Rectangle(pad, cardsTop, cardW, cardsH);
                Rectangle card2 = new Rectangle(pad * 2 + cardW, cardsTop, cardW, cardsH);
                Rectangle card3 = new Rectangle(pad * 3 + cardW * 2, cardsTop, Width - pad * 4 - cardW * 2, cardsH);

                using (SolidBrush cb = new SolidBrush(Color.FromArgb(17, 22, 33)))
                {
                    g.FillRectangle(cb, card1);
                    g.FillRectangle(cb, card2);
                    g.FillRectangle(cb, card3);
                }
                using (Pen cp = new Pen(Color.FromArgb(45, 255, 255, 255), 1.0f))
                {
                    g.DrawRectangle(cp, card1);
                    g.DrawRectangle(cp, card2);
                    g.DrawRectangle(cp, card3);
                }

                int rowH = (int)Math.Round(30 * s);
                int gapY = (int)Math.Round(11 * s);
                int togW = (int)Math.Round(86 * s);
                int optW = (int)Math.Round(136 * s);
                int pmW = (int)Math.Round(32 * s);
                int valW = optW - pmW * 2;
                int resetY = card1.Bottom - rowH - (int)Math.Round(14 * s);

                // =====================================================
                // CARD 1: STANDINGS OVERLAY
                // =====================================================
                int c1X = card1.Left + (int)Math.Round(14 * s);
                int c1W = card1.Width - (int)Math.Round(28 * s);
                int curY = card1.Top + (int)Math.Round(12 * s);

                g.DrawString("1. STANDINGS", fCardTitle, Brushes.White,
                    new RectangleF(c1X, curY, c1W * 0.65f, rowH), leftFmt);

                _btnStandingsToggle = DrawButton(
                    g, fBtn,
                    _settings.StandingsEnabled ? "ENABLED" : "OFF",
                    c1X + c1W - togW, curY + 2, togW, rowH - 4,
                    _settings.StandingsEnabled,
                    _settings.StandingsEnabled ? Color.FromArgb(34, 197, 94) : Color.FromArgb(107, 114, 128),
                    s
                );
                curY += rowH + (int)Math.Round(12 * s);

                int r1Start = c1X + c1W - optW;
                g.DrawString("Timing Column", fLabel, Brushes.Gainsboro, new RectangleF(c1X, curY, c1W * 0.50f, rowH), leftFmt);
                _btnStandingsGap = DrawButton(
                    g, fBtn,
                    _settings.ShowInterval ? "INTERVAL AHEAD" : "GAP TO LEADER",
                    r1Start, curY, optW, rowH,
                    true, Color.FromArgb(56, 189, 248), s
                );
                curY += rowH + gapY;

                g.DrawString("iRating Display", fLabel, Brushes.Gainsboro, new RectangleF(c1X, curY, c1W * 0.50f, rowH), leftFmt);
                _btnStandingsCompactIr = DrawButton(
                    g, fBtn,
                    _settings.CompactIR ? "COMPACT (4.1k)" : "FULL (4,125)",
                    r1Start, curY, optW, rowH,
                    _settings.CompactIR, Color.FromArgb(56, 189, 248), s
                );
                curY += rowH + gapY;

                g.DrawString("Projected +/- iR", fLabel, Brushes.Gainsboro, new RectangleF(c1X, curY, c1W * 0.50f, rowH), leftFmt);
                _btnStandingsDeltaIr = DrawButton(
                    g, fBtn,
                    _settings.ShowDeltaIR ? "SHOW (+/- iR)" : "HIDDEN",
                    r1Start, curY, optW, rowH,
                    _settings.ShowDeltaIR, Color.FromArgb(34, 197, 94), s
                );
                curY += rowH + gapY;

                g.DrawString("Font Scale", fLabel, Brushes.Gainsboro, new RectangleF(c1X, curY, c1W * 0.50f, rowH), leftFmt);
                _btnStandingsFontMinus = DrawButton(g, fBtn, "-", r1Start, curY, pmW, rowH, false, Color.Gray, s);
                g.DrawString((int)Math.Round(_settings.StandingsFontScale * 100) + "%", fMono, Brushes.White,
                    new RectangleF(r1Start + pmW, curY, valW, rowH), centerFmt);
                _btnStandingsFontPlus = DrawButton(g, fBtn, "+", r1Start + pmW + valW, curY, pmW, rowH, false, Color.Gray, s);
                curY += rowH + gapY;

                g.DrawString("Window Opacity", fLabel, Brushes.Gainsboro, new RectangleF(c1X, curY, c1W * 0.50f, rowH), leftFmt);
                _btnStandingsOpacMinus = DrawButton(g, fBtn, "-", r1Start, curY, pmW, rowH, false, Color.Gray, s);
                g.DrawString((int)Math.Round(_settings.StandingsOpacity * 100) + "%", fMono, Brushes.White,
                    new RectangleF(r1Start + pmW, curY, valW, rowH), centerFmt);
                _btnStandingsOpacPlus = DrawButton(g, fBtn, "+", r1Start + pmW + valW, curY, pmW, rowH, false, Color.Gray, s);

                _btnStandingsReset = DrawButton(g, fBtn, "RESET POSITION & SIZE", c1X, resetY, c1W, rowH, false, Color.FromArgb(56, 189, 248), s);

                // =====================================================
                // CARD 2: DELTA BAR OVERLAY (OPTION C)
                // =====================================================
                int c2X = card2.Left + (int)Math.Round(14 * s);
                int c2W = card2.Width - (int)Math.Round(28 * s);
                curY = card2.Top + (int)Math.Round(12 * s);

                g.DrawString("2. DELTA BAR", fCardTitle, Brushes.White,
                    new RectangleF(c2X, curY, c2W * 0.65f, rowH), leftFmt);

                _btnDeltaToggle = DrawButton(
                    g, fBtn,
                    _settings.DeltaBarEnabled ? "ENABLED" : "OFF",
                    c2X + c2W - togW, curY + 2, togW, rowH - 4,
                    _settings.DeltaBarEnabled,
                    _settings.DeltaBarEnabled ? Color.FromArgb(34, 197, 94) : Color.FromArgb(107, 114, 128),
                    s
                );
                curY += rowH + (int)Math.Round(12 * s);

                g.DrawString("Reference Lap Mode", fLabel, Brushes.Gainsboro, new RectangleF(c2X, curY, c2W, (int)(18 * s)), leftFmt);
                curY += (int)Math.Round(20 * s);
                int refGap = (int)Math.Round(4 * s);
                int refW = (c2W - refGap * 3) / 4;
                int smallBtnH = (int)Math.Round(26 * s);
                _btnDeltaRefBest = DrawButton(g, fBtn, "BEST", c2X, curY, refW, smallBtnH,
                    _settings.DeltaRefMode == DeltaReferenceMode.SessionBest, Color.FromArgb(56, 189, 248), s);
                _btnDeltaRefOpt = DrawButton(g, fBtn, "OPTIMAL", c2X + (refW + refGap), curY, refW, smallBtnH,
                    _settings.DeltaRefMode == DeltaReferenceMode.SessionOptimal, Color.FromArgb(192, 132, 252), s);
                _btnDeltaRefPers = DrawButton(g, fBtn, "PERS", c2X + (refW + refGap) * 2, curY, refW, smallBtnH,
                    _settings.DeltaRefMode == DeltaReferenceMode.PersonalBest, Color.FromArgb(34, 197, 94), s);
                _btnDeltaRefLast = DrawButton(g, fBtn, "LAST", c2X + (refW + refGap) * 3, curY, c2W - (refW + refGap) * 3, smallBtnH,
                    _settings.DeltaRefMode == DeltaReferenceMode.LastLap, Color.FromArgb(245, 158, 11), s);
                curY += smallBtnH + gapY;

                int r2Start = c2X + c2W - optW;
                int rngW = (optW - 8) / 3;
                g.DrawString("Bar Scale Range", fLabel, Brushes.Gainsboro, new RectangleF(c2X, curY, c2W * 0.48f, rowH), leftFmt);
                _btnDeltaRange1 = DrawButton(g, fBtn, "±1.0s", r2Start, curY, rngW, rowH,
                    Math.Abs(_settings.DeltaBarRangeSec - 1.0f) < 0.1f, Color.FromArgb(56, 189, 248), s);
                _btnDeltaRange2 = DrawButton(g, fBtn, "±2.0s", r2Start + rngW + 4, curY, rngW, rowH,
                    Math.Abs(_settings.DeltaBarRangeSec - 2.0f) < 0.1f, Color.FromArgb(56, 189, 248), s);
                _btnDeltaRange5 = DrawButton(g, fBtn, "±5.0s", r2Start + (rngW + 4) * 2, curY, optW - (rngW + 4) * 2, rowH,
                    Math.Abs(_settings.DeltaBarRangeSec - 5.0f) < 0.1f, Color.FromArgb(56, 189, 248), s);
                curY += rowH + gapY;

                g.DrawString("Official Sectors", fLabel, Brushes.Gainsboro, new RectangleF(c2X, curY, c2W * 0.50f, rowH), leftFmt);
                _btnDeltaSectors = DrawButton(
                    g, fBtn,
                    _settings.ShowSectorSplits ? "SHOW SPLITS" : "HIDDEN",
                    r2Start, curY, optW, rowH,
                    _settings.ShowSectorSplits, Color.FromArgb(34, 197, 94), s
                );
                curY += rowH + gapY;

                g.DrawString("Font Scale", fLabel, Brushes.Gainsboro, new RectangleF(c2X, curY, c2W * 0.48f, rowH), leftFmt);
                _btnDeltaFontMinus = DrawButton(g, fBtn, "-", r2Start, curY, pmW, rowH, false, Color.Gray, s);
                g.DrawString((int)Math.Round(_settings.DeltaFontScale * 100) + "%", fMono, Brushes.White,
                    new RectangleF(r2Start + pmW, curY, valW, rowH), centerFmt);
                _btnDeltaFontPlus = DrawButton(g, fBtn, "+", r2Start + pmW + valW, curY, pmW, rowH, false, Color.Gray, s);
                curY += rowH + gapY;

                g.DrawString("Window Opacity", fLabel, Brushes.Gainsboro, new RectangleF(c2X, curY, c2W * 0.48f, rowH), leftFmt);
                _btnDeltaOpacMinus = DrawButton(g, fBtn, "-", r2Start, curY, pmW, rowH, false, Color.Gray, s);
                g.DrawString((int)Math.Round(_settings.DeltaOpacity * 100) + "%", fMono, Brushes.White,
                    new RectangleF(r2Start + pmW, curY, valW, rowH), centerFmt);
                _btnDeltaOpacPlus = DrawButton(g, fBtn, "+", r2Start + pmW + valW, curY, pmW, rowH, false, Color.Gray, s);

                _btnDeltaReset = DrawButton(g, fBtn, "RESET POSITION & SIZE", c2X, resetY, c2W, rowH, false, Color.FromArgb(56, 189, 248), s);

                // =====================================================
                // CARD 3: GARAGE61 TELEMETRY & BRAKING COACH (A / B / C)
                // =====================================================
                int c3X = card3.Left + (int)Math.Round(14 * s);
                int c3W = card3.Width - (int)Math.Round(28 * s);
                curY = card3.Top + (int)Math.Round(12 * s);

                g.DrawString("3. GARAGE61 COACH", fCardTitle, Brushes.White,
                    new RectangleF(c3X, curY, c3W * 0.68f, rowH), leftFmt);

                _btnG61Toggle = DrawButton(
                    g, fBtn,
                    _settings.Garage61Enabled ? "ENABLED" : "OFF",
                    c3X + c3W - togW, curY + 2, togW, rowH - 4,
                    _settings.Garage61Enabled,
                    _settings.Garage61Enabled ? Color.FromArgb(34, 197, 94) : Color.FromArgb(107, 114, 128),
                    s
                );
                curY += rowH + (int)Math.Round(12 * s);

                // Selectable Design Style (Option A / Option B / Option C)
                g.DrawString("Overlay Design Style", fLabel, Brushes.Gainsboro, new RectangleF(c3X, curY, c3W, (int)(18 * s)), leftFmt);
                curY += (int)Math.Round(20 * s);
                int stW = (c3W - refGap * 2) / 3;
                _btnG61StyleA = DrawButton(g, fBtn, "OPT A: TRACE", c3X, curY, stW, smallBtnH,
                    _settings.Garage61Style == Garage61LayoutStyle.OptionA_LookaheadTraceAndPedals, Color.FromArgb(56, 189, 248), s);
                _btnG61StyleB = DrawButton(g, fBtn, "OPT B: BAR", c3X + stW + refGap, curY, stW, smallBtnH,
                    _settings.Garage61Style == Garage61LayoutStyle.OptionB_CompactBrakeCoach, Color.FromArgb(244, 63, 94), s);
                _btnG61StyleC = DrawButton(g, fBtn, "OPT C: AUDIT", c3X + (stW + refGap) * 2, curY, c3W - (stW + refGap) * 2, smallBtnH,
                    _settings.Garage61Style == Garage61LayoutStyle.OptionC_ProBoxWithCornerAudit, Color.FromArgb(192, 132, 252), s);
                curY += smallBtnH + gapY;

                int r3Start = c3X + c3W - optW;
                g.DrawString("Lookahead Window", fLabel, Brushes.Gainsboro, new RectangleF(c3X, curY, c3W * 0.48f, rowH), leftFmt);
                _btnG61Look80 = DrawButton(g, fBtn, "+80m", r3Start, curY, rngW, rowH,
                    _settings.Garage61LookaheadMeters == 80, Color.FromArgb(56, 189, 248), s);
                _btnG61Look120 = DrawButton(g, fBtn, "+120m", r3Start + rngW + 4, curY, rngW, rowH,
                    _settings.Garage61LookaheadMeters == 120, Color.FromArgb(56, 189, 248), s);
                _btnG61Look180 = DrawButton(g, fBtn, "+180m", r3Start + (rngW + 4) * 2, curY, optW - (rngW + 4) * 2, rowH,
                    _settings.Garage61LookaheadMeters == 180, Color.FromArgb(56, 189, 248), s);
                curY += rowH + gapY;

                g.DrawString("Reference Lap CSV", fLabel, Brushes.Gainsboro, new RectangleF(c3X, curY, c3W * 0.48f, rowH), leftFmt);
                int csvW = (int)Math.Round(optW * 0.62f);
                int defW = optW - csvW - 4;
                _btnG61LoadCsv = DrawButton(g, fBtn, "LOAD CSV...", r3Start, curY, csvW, rowH, true, Color.FromArgb(34, 197, 94), s);
                _btnG61DefaultLap = DrawButton(g, fBtn, "SPA", r3Start + csvW + 4, curY, defW, rowH, string.IsNullOrEmpty(_settings.Garage61CsvPath), Color.FromArgb(56, 189, 248), s);
                curY += rowH + gapY;

                g.DrawString("Font Scale", fLabel, Brushes.Gainsboro, new RectangleF(c3X, curY, c3W * 0.48f, rowH), leftFmt);
                _btnG61FontMinus = DrawButton(g, fBtn, "-", r3Start, curY, pmW, rowH, false, Color.Gray, s);
                g.DrawString((int)Math.Round(_settings.Garage61FontScale * 100) + "%", fMono, Brushes.White,
                    new RectangleF(r3Start + pmW, curY, valW, rowH), centerFmt);
                _btnG61FontPlus = DrawButton(g, fBtn, "+", r3Start + pmW + valW, curY, pmW, rowH, false, Color.Gray, s);
                curY += rowH + gapY;

                g.DrawString("Window Opacity", fLabel, Brushes.Gainsboro, new RectangleF(c3X, curY, c3W * 0.48f, rowH), leftFmt);
                _btnG61OpacMinus = DrawButton(g, fBtn, "-", r3Start, curY, pmW, rowH, false, Color.Gray, s);
                g.DrawString((int)Math.Round(_settings.Garage61Opacity * 100) + "%", fMono, Brushes.White,
                    new RectangleF(r3Start + pmW, curY, valW, rowH), centerFmt);
                _btnG61OpacPlus = DrawButton(g, fBtn, "+", r3Start + pmW + valW, curY, pmW, rowH, false, Color.Gray, s);

                _btnG61Reset = DrawButton(g, fBtn, "RESET POSITION & SIZE", c3X, resetY, c3W, rowH, false, Color.FromArgb(56, 189, 248), s);

                // Footer status bar
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
