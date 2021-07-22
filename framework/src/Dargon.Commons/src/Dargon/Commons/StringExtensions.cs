using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Dargon.Commons {
   public static class StringExtensions {
      /**
       * foreach (var x in new[] { "testString", "TestString", "This is a test", "hello-world" }) {
       *    Console.WriteLine(x + " " + x.ToUpperCamelCase() + " " + x.ToLowerCamelCase() + " " + x.ToDashedSnakeCase());
       * }
       */
      public static string ToUpperCamelCase(this string s) => ToCase(s, true, true, null);

      public static string ToLowerCamelCase(this string s) => ToCase(s, false, true, null);

      /// <summary>
      /// a2i => a2i
      /// ascii2Integer => ascii2-integer
      /// UpperCamel => upper-camel
      /// lowerCamel => lower-camel
      /// </summary>
      public static string ToDashedSnakeCase(this string s) => ToCase(s, false, false, "-");

      private static string ToCase(string s, bool upperElseLowerFirst, bool upperElseLowerFollowing, string snakeDash) {
         var sb = new StringBuilder();
         var firstWord = true;
         var mode = 0; // 0: out of word, 1: entered word, 2: in word

         for (var i = 0; i < s.Length; i++) {
            var c = s[i];
            if (char.IsNumber(c)) {
               sb.Append(c);
               continue;
            }
            if (!char.IsLetter(c)) {
               if (mode != 0 && snakeDash != null) sb.Append(snakeDash);
               mode = 0;
               continue;
            }

            if (mode == 2 && char.IsUpper(c)) {
               if (snakeDash != null) sb.Append(snakeDash);
               mode = 0;
            }

            var upperElseLower = mode == 0 
               ? (firstWord ? upperElseLowerFirst : upperElseLowerFollowing) 
               : false;
            firstWord = false;

            if (mode == 0) mode = 1;
            else if (mode == 1 && !char.IsUpper(c)) mode = 2;
          
            sb.Append(upperElseLower ? char.ToUpper(c) : char.ToLower(c));
         }

         return sb.ToString();
      }

      private static Regex base64Regex = new Regex("^(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==|[A-Za-z0-9+/]{3}=)?$");

      public static bool IsBase64(this string s) => base64Regex.IsMatch(s);

      /// <summary>
      /// Reverses the given string.
      /// http://dotnetperls.com/reverse-string
      /// </summary>
      public static string Reverse(this string s) {
         char[] arr = s.ToCharArray();
         Array.Reverse(arr);
         return new string(arr);
      }

      /// <summary>
      /// Splits the given string at the given index and returns subarrays.
      /// </summary>
      public static string[] SplitAtIndex(this string s, int index) {
         if (index == s.Length) return new string[] { s };
         return new string[] { s.Substring(0, index), s.Substring(index + 1) };
      }

      /// <summary>
      /// Formats a string, shorthand for string.Format
      /// </summary>
      [Obsolete]
      public static string F(this string s, params object[] p) {
         return string.Format(s, p);
      }

      /// <summary>
      /// Repeats the given string, s, N times
      /// </summary>
      public static string Repeat(this string s, int n) {
         StringBuilder sb = new StringBuilder();
         for (int i = 0; i < n; i++) {
            sb.Append(s);
         }

         return sb.ToString();
      }

      /// <summary>
      /// Quotation aware string split.  Will not break up 'words contained in quotes'... useful for handling console
      /// such as: del "C:\Derp a de herp\Lerp a merp\"
      /// </summary>
      public static string[] QASS(this string s, char delimiter = ' ') {
         StringBuilder curPartSB = new StringBuilder();
         List<string> finalParts = new List<string>();
         bool inDoubleQuotes = false;
         bool inSingleQuotes = false;
         for (int i = 0; i < s.Length; i++) {
            if (s[i] == '"')
               if (!inSingleQuotes)
                  inDoubleQuotes = !inDoubleQuotes;
               else
                  curPartSB.Append(s[i]);
            else if (s[i] == '\'')
               if (!inDoubleQuotes)
                  inSingleQuotes = !inSingleQuotes;
               else
                  curPartSB.Append(s[i]);
            else if (s[i] == delimiter) {
               if (!inDoubleQuotes && !inSingleQuotes) {
                  if (curPartSB.ToString() != "") {
                     finalParts.Add(curPartSB.ToString());
                     curPartSB.Clear();
                  }
               } else {
                  curPartSB.Append(s[i]);
               }
            } else
               curPartSB.Append(s[i]);
         }

         if (curPartSB.ToString() != "") {
            finalParts.Add(curPartSB.ToString());
         }

         return finalParts.ToArray();
      }

      /// <summary>
      /// Removes surrounding quotations of the given string, if they exist.
      /// </summary>
      public static string RemoveOuterQuote(this string s) {
         if (s.Length > 1) {
            char lastChar = s[s.Length - 1];
            if ((s[0] == '\'' && lastChar == '\'') ||
                (s[0] == '"' && lastChar == '"')
            )
               return s.Substring(1, s.Length - 2);
            else
               return s;
         } else
            return s;
      }

      /// <summary>
      /// Makes string.split() behave like JS's "string".split(delim) as opposed to c#'s requirement for StringSplitOptions
      /// The delimiter is no longer an array.
      /// </summary>
      public static string[] Split(this string s, string delimiter) {
         return s.Split(new string[] { delimiter }, StringSplitOptions.None);
      }

      public static string[] Split(this string s, string delimiter, StringSplitOptions sso) {
         return s.Split(new string[] { delimiter }, sso);
      }

      /// <summary>
      /// Returns whether or not a string ends with any of the given in the given array.
      /// Useful for checking if a file name ends with ".txt", ".ini", etc....
      /// </summary>
      public static bool EndsWithAny(this string s, string[] enders) {
         return EndsWithAny(s, enders, StringComparison.CurrentCulture);
      }

      /// <summary>
      /// Returns whether or not a string ends with any of the given in the given array.
      /// Useful for checking if a file name ends with ".txt", ".ini", etc....
      /// </summary>
      public static bool EndsWithAny(this string s, IEnumerable<string> enders, StringComparison comparison) {
         return enders.Any(x => s.EndsWith(x, comparison));
      }

      public static bool ContainsAny(this string self, string[] strings, StringComparison comp = StringComparison.CurrentCulture) {
         return strings.Any(x => self.IndexOf(x, comp) >= 0);
      }

      public static string ToEscapedStringLiteral(this string s) {
         var sb = new StringBuilder(s.Length * 2 + 2);
         sb.Append('"');

         // https://en.wikipedia.org/wiki/Escape_sequences_in_C, \e \? not in c#
         foreach (var c in s) {
            sb.Append(c.ToStringLiteralChar());
         }

         sb.Append('"');
         return sb.ToString();
      }

      /// <summary>
      /// E.g. \0 => \\0
      /// Output isn't surrounded by quotes. E.g. \\0, not "\\0".
      /// </summary>
      public static string ToStringLiteralChar(this char c) {
         if (c == '\0') {
            return "\\0";
         } else if (c == '\a') {
            return "\\a";
         } else if (c == '\b') {
            return "\\b";
         } else if (c == '\f') {
            return "\\f";
         } else if (c == '\n') {
            return "\\n";
         } else if (c == '\r') {
            return "\\r";
         } else if (c == '\t') {
            return "\\t";
         } else if (c == '\v') {
            return "\\v";
         } else if (c == '\\') {
            return "\\\\";
         } else if (c == '\'') {
            return "\\'";
         } else if (c == '\"') {
            return "\\\"";
         } else if (c >= 0x20 && c <= 0x7e) {
            return c.ToString();
         } else {
            return @"\u" + ((int)c).ToString("x4");
         }
      }

      private static readonly char[] newlineChars = new[] { '\n', '\r' };

      public static string TrimNewlines(this string s) => s.Trim(newlineChars);
      public static string TrimLeadingNewlines(this string s) => s.TrimStart(newlineChars);
      public static string TrimTrailingNewlines(this string s) => s.TrimEnd(newlineChars);
   }
}
