﻿using System;

namespace Dargon.Commons {
   public static class TimeExtensions {
      private static readonly DateTime kUnixEpochUtc = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

      public static long ToUnixTimeSeconds(this DateTime val) {
         return (long)(val.ToUniversalTime() - kUnixEpochUtc).TotalSeconds;
      }

      public static long ToUnixTimeMillis(this DateTime val) {
         return (long)(val.ToUniversalTime() - kUnixEpochUtc).TotalMilliseconds;
      }

      public static string ToHttpTimestamp(this DateTime dt) {
         DateTime utc = dt.ToUniversalTime();
         string day = utc.Day.ToString().PadLeft(2, '0');
         string dayOfWeek = utc.DayOfWeek.ToString().Substring(0, 3);
         string year = utc.Year.ToString();
         string mon = new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" }[utc.Month - 1];
         return dayOfWeek + ", " + day + " " + mon + " " + year + " " + utc.Hour.ToString().PadLeft(2, '0') + ":" + utc.Minute.ToString().PadLeft(2, '0') + ":" + utc.Second.ToString().PadLeft(2, '0') + " GMT";
      }
   }
}