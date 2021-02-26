#region License
/* Copyright 2019 James F. Bellinger <http://www.zer7.com/software/hidsharp>

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

      http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing,
   software distributed under the License is distributed on an
   "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
   KIND, either express or implied.  See the License for the
   specific language governing permissions and limitations
   under the License. */
#endregion

using System;
using System.Diagnostics;

namespace HidSharp.Utility
{
    public static class HidSharpDiagnostics
    {
        static HidSharpDiagnostics()
        {
            PerformStrictChecks = false;
        }

        internal static void PerformStrictCheck(bool condition, string message)
        {
            if (!condition)
            {
                message += "\n\nTo disable this exception, set HidSharpDiagnostics.PerformStrictChecks to false.";
                throw new InvalidOperationException(message);
            }
        }

        internal static void Trace(string message)
        {
            if (!EnableTracing) { return; }
            System.Diagnostics.Trace.WriteLine(message, "HIDSharp");
        }

        internal static void Trace(string formattedMessage, object arg)
        {
            if (!EnableTracing) { return; }
            Trace(string.Format(formattedMessage, arg));
        }

        internal static void Trace(string formattedMessage, params object[] args)
        {
            if (!EnableTracing) { return; }
            Trace(string.Format(formattedMessage, args));
        }

        public static bool EnableTracing
        {
            get;
            set;
        }

        public static bool PerformStrictChecks
        {
            get;
            set;
        }
    }
}
