using System;
using System.Collections.Generic;

namespace TerraViewer
{
    public  enum SpaceTimeActions { Slower, Faster, PauseTime, SetRealtimeNow, NextSiderealDay, LastSiderealDay, NextDay, LastDay, NextSiderealYear, LastSidrealYear, NextYear, LastYear };
    public class SpaceTimeController : IScriptable
    {
        static public SpaceTimeController ScriptInterface = new SpaceTimeController();

        static public void UpdateClock()
        {
            if (syncToClock)
            {
                var justNow = MetaNow;

                if (timeRate != 1.0)
                {
                    var ts = justNow - last;
                    var ticks = (long)(ts.Ticks * timeRate);
                    offset = offset.Add(new TimeSpan(ticks));
                }
                last = justNow;
                try
                {
                    now = justNow.Add(offset);
                    nowUtc = now.ToUniversalTime();
                }
                catch
                {
                    now = new DateTime(1, 12, 25, 23, 59, 59);
                    offset = now - MetaNow;
                    nowUtc = now.ToUniversalTime();

                }

                if (now.Year > 4000)
                {
                    now = new DateTime(4000, 12, 31, 23, 59, 59);
                    offset = now - MetaNow;
                    nowUtc = now.ToUniversalTime();
                }

                if (now.Year < 1)
                {
                    now = new DateTime(0, 12, 25, 23, 59, 59);
                    offset = now - MetaNow;
                    nowUtc = now.ToUniversalTime();
                }

            }
        }

        private static void AdjustNow(TimeSpan ts)
        {

            try
            {
                now = now.Add(ts);
                nowUtc = now.ToUniversalTime();
                offset = now - MetaNow;
            }
            catch
            {
                now = new DateTime(1, 12, 25, 23, 59, 59);
                nowUtc = now.ToUniversalTime();
                offset = now - MetaNow;
            }

            if (now.Year > 4000)
            {
                now = new DateTime(4000, 12, 31, 23, 59, 59);
                nowUtc = now.ToUniversalTime();
                offset = now - MetaNow;
            }

            if (now.Year < 1)
            {
                now = new DateTime(0, 12, 25, 23, 59, 59);
                nowUtc = now.ToUniversalTime();
                offset = now - MetaNow;
            }
        }

        static public DateTime GetTimeForFutureTime(double delta)
        {
            try
            {
                if (syncToClock)
                {
                    var future = Now.Add(new TimeSpan((long)((delta * 10000000) * timeRate)));
                    return future;
                }
                return Now;
            }
            catch
            {
                return Now;
            }
        }
        static public double GetJNowForFutureTime(double delta)
        {
            try
            {
                if (syncToClock)
                {
                    var future = Now.Add(new TimeSpan((long)((delta * 10000000) * timeRate)));
                    return UtcToJulian(future);
                }
                return UtcToJulian(Now);
            }
            catch
            {
                return UtcToJulian(Now);
            }
        }
        static public DateTime Now
        {
            get
            {
                return nowUtc;
            }
            set
            {
                now = value.ToLocalTime();
                offset = now - MetaNow;
                last = MetaNow;
                nowUtc = now.ToUniversalTime();
            }
        }
        static DateTime last = MetaNow;


        static DateTime metaNow = DateTime.Now;

        public static double FramesPerSecond = 30;

        public static bool FrameDumping = false;
        public static bool CancelFrameDump = false;

        public static int CurrentFrameNumber = 0;
        public static int TotalFrames = 0;

        public static Int64 factor = (HiResTimer.Frequency * 1000) / TimeSpan.FromMilliseconds(1000.0).Ticks;

        public static DateTime MetaNow
        {
            get
            {
                //if (FrameDumping)
                {
                    return metaNow;
                }
                //else
                //{
                //    return DateTime.Now;
                //}
            }
            set
            {
                if (!FrameDumping)
                {
                    metaNow = value;
                }
            }
        }



        public static Int64 MetaNowTickCount
        {
            get
            {
                if (FrameDumping)
                {

                    // todo fix ticks to proper ratio

                    return metaNow.Ticks * factor / 1000;
                }
                return HiResTimer.TickCount;
            }
        }

        public static void NextFrame()
        {
            metaNow += TimeSpan.FromMilliseconds(1000.0 / FramesPerSecond);
            CurrentFrameNumber++;
        }

        public static void SyncTime()
        {
            offset = new TimeSpan();
            now = DateTime.Now;
            nowUtc = now.ToUniversalTime();
            syncToClock = true;
        }

        static TimeSpan offset;

        static DateTime now = DateTime.Now;
        static DateTime nowUtc = DateTime.Now.ToUniversalTime();
        static CAADate converter = new CAADate();
        static public double JNow
        {
            get
            {
                return UtcToJulian(Now);
            }
            //set
            //{
            //    converter.Set(value, true);
            //    int Year = 0;
            //    int Month = 0;
            //    int Day = 0;
            //    int Hour = 0;
            //    int Minute = 0;
            //    double Second = 0;
            //    converter.Get(ref Year, ref Month, ref Day, ref Hour, ref Minute, ref Second);
            //    int Ms = ((int)(Second * 1000)) % 1000;
            //    Now = new DateTime(Year, Month, Day, Hour, Minute, (int)Second, Ms);
            //}
        }

        static bool syncToClock = true;

        public static bool SyncToClock
        {
            get { return syncToClock; }
            set
            {
                if (syncToClock != value)
                {
                    syncToClock = value;
                    if (value)
                    {
                        last = DateTime.Now;
                        offset = now - DateTime.Now;
                    }
                    else
                    {
                        now = DateTime.Now.Add(offset);
                        nowUtc = now.ToUniversalTime();
                    }
                }
            }
        }

        static private double timeRate = 1;

        static public double TimeRate
        {
            get { return timeRate; }
            set { timeRate = value; }
        }

        static private Coordinates location;
        static private double altitude;

        public static double Altitude
        {
            get { return altitude; }
            set { altitude = value; }
        }

        static public Coordinates Location
        {
            get
            {
                //if (location == null)
                {
                    location = Coordinates.FromLatLng((double)AppSettings.SettingsBase["LocationLat"], (double)AppSettings.SettingsBase["LocationLng"]);
                    altitude = (double)AppSettings.SettingsBase["LocationAltitude"];
                }
                return location;
            }
            set
            {
                if ((double)AppSettings.SettingsBase["LocationLat"] != value.Lat)
                {
                    AppSettings.SettingsBase["LocationLat"] = value.Lat;
                }

                if ((double)AppSettings.SettingsBase["LocationLng"] != value.Lng)
                {
                    AppSettings.SettingsBase["LocationLng"] = value.Lng;
                }
                location = value;
            }
        }

        public static double UtcToJulian(DateTime utc)
        {
            var year = utc.Year;
            var month = utc.Month;
            double day = utc.Day;
            double hour = utc.Hour;
            double minute = utc.Minute;
            var second = utc.Second + utc.Millisecond / 1000.0;
            var dblDay = day + (hour / 24.0) + (minute / 1440.0) + (second / 86400.0);
            return AstroCalc.AstroCalc.GetJulianDay(year, month, dblDay);
        }

        public static double TwoLineDateToJulian(string p)
        {
            var pre1950 = Convert.ToInt32(p.Substring(0, 1)) < 6;
            var year = Convert.ToInt32((pre1950 ? " 20" : "19") + p.Substring(0, 2));
            double days = Convert.ToInt32(p.Substring(2, 3));
            var fraction = Convert.ToDouble(p.Substring(5));

            var ts = TimeSpan.FromDays(days - 1 + fraction);
            var date = new DateTime(year, 1, 1, 0, 0, 0, 0);
            date += ts;
            return UtcToJulian(date);
        }

        public static bool DoneDumping()
        {

            if (!FrameDumping || CancelFrameDump || (CurrentFrameNumber >= TotalFrames))
            {
                return true;
            }
            return false;
        }

        public static void Faster()
        {
            SyncToClock = true;
            if (TimeRate > .9)
            {
                TimeRate *= 1.1;
            }
            else if (TimeRate < -1)
            {
                TimeRate /= 1.1;
            }
            else
            {
                TimeRate = 1.0;
            }

            if (TimeRate > 1000000000)
            {
                TimeRate = 1000000000;
            }
        }


        public static void Slower()
        {
            SyncToClock = true;
            if (TimeRate < -.9)
            {
                TimeRate *= 1.1;
            }
            else if (TimeRate > 1)
            {
                TimeRate /= 1.1;
            }
            else
            {
                TimeRate = -1.0;
            }

            if (TimeRate < -1000000000)
            {
                TimeRate = -1000000000;
            }
        }

        public static void SetRealtimeNow()
        {
            SyncToClock = true;
            SyncTime();
            TimeRate = 1.0;
        }

        public static void PauseTime()
        {
            SyncToClock = !SyncToClock;
        }

        public static void InvokeAction(string actionString, string value)
        {
            try
            {
                var action = (SpaceTimeActions)Enum.Parse(typeof(SpaceTimeActions), actionString, true);

                switch (action)
                {
                    case SpaceTimeActions.Slower:
                        Slower();
                        break;
                    case SpaceTimeActions.Faster:
                        Faster();
                        break;
                    case SpaceTimeActions.PauseTime:
                        PauseTime();
                        break;
                    case SpaceTimeActions.SetRealtimeNow:
                        SetRealtimeNow();
                        break;
                    case SpaceTimeActions.NextSiderealDay:
                        NextSiderealDay();
                        break;
                    case SpaceTimeActions.LastSiderealDay:
                        LastSiderealDay();
                        break;
                    case SpaceTimeActions.NextDay:
                        NextDay();
                        break;
                    case SpaceTimeActions.LastDay:
                        LastDay();
                        break;
                    case SpaceTimeActions.NextSiderealYear:
                        NextSiderealYear();
                        break;
                    case SpaceTimeActions.LastSidrealYear:
                        LastSidrealYear();
                        break;
                    case SpaceTimeActions.NextYear:
                        NextYear();
                        break;
                    case SpaceTimeActions.LastYear:
                        LastYear();
                        break;
                    default:
                        break;
                }
            }
            catch
            {
            }
        }



        private static void LastSidrealYear()
        {
            AdjustNow(TimeSpan.FromDays(-365.256363004));
        }

        private static void NextSiderealYear()
        {
            AdjustNow(TimeSpan.FromDays(365.256363004));
        }

        private static void LastYear()
        {
            if (DateTime.IsLeapYear(now.Year))
            {
                if (now.Month > 2)
                {
                    AdjustNow(TimeSpan.FromDays(-366));
                }
            }
            AdjustNow(TimeSpan.FromDays(-365));

        }

        private static void NextYear()
        {
            if (DateTime.IsLeapYear(now.Year))
            {
                if (now.Month < 3)
                {
                    AdjustNow(TimeSpan.FromDays(366));
                }
            }
            AdjustNow(TimeSpan.FromDays(365));
        }

        private static void LastDay()
        {
            AdjustNow(TimeSpan.FromDays(-1));
        }

        private static void NextDay()
        {
            AdjustNow(TimeSpan.FromDays(1));
        }

        private static void LastSiderealDay()
        {
            AdjustNow(TimeSpan.FromHours(-23.93447));
        }

        private static void NextSiderealDay()
        {
            AdjustNow(TimeSpan.FromHours(23.93447));
        }

        public static string[] GetActionsList()
        {
            return Enum.GetNames(typeof(SpaceTimeActions));
        }


        #region IScriptable Members

        ScriptableProperty[] IScriptable.GetProperties()
        {
            var props = new List<ScriptableProperty>();

            props.Add(new ScriptableProperty("TimeRate", ScriptablePropertyTypes.Double, ScriptablePropertyScale.Log, -1000000, 1000000, false));
            props.Add(new ScriptableProperty("Pause", ScriptablePropertyTypes.BlendState, ScriptablePropertyScale.Linear, -90, +90, true));
            return props.ToArray();
        }

        string[] IScriptable.GetActions()
        {
            return GetActionsList();
        }

        void IScriptable.InvokeAction(string name, string value)
        {
            InvokeAction(name, value);
        }

        void IScriptable.SetProperty(string name, string value)
        {
            switch (name.ToLower())
            {
                case "timerate":
                    TimeRate = double.Parse(value);
                    break;
                case "pause":
                    SyncToClock = !bool.Parse(value);
                    break;
            }
            return;
        }

        string IScriptable.GetProperty(string name)
        {
            switch (name.ToLower())
            {
                case "timerate":
                    return TimeRate.ToString();

                case "pause":
                    return SyncToClock.ToString();

            }
            return null;
        }

        bool IScriptable.ToggleProperty(string name)
        {
            switch (name.ToLower())
            {

                case "pause":
                    SyncToClock = !SyncToClock;
                    return !syncToClock;
            }
            return false;
        }

        #endregion
    }
}
