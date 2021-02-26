#region License
/* Copyright 2017 James F. Bellinger <http://www.zer7.com/software/hidsharp>

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
using System.Reflection;
using System.Runtime.InteropServices;

namespace HidSharp.Utility
{
    [Obsolete("This class is experimental and its functionality may be moved elsewhere in the future. Please do not rely on it.")]
    public static class HResult
    {
        public const int FileNotFound = unchecked((int)0x80070002);     // ERROR_FILE_NOT_FOUND
        public const int SharingViolation = unchecked((int)0x80070020); // ERROR_SHARING_VIOLATION
        public const int SemTimeout = unchecked((int)0x80070079);       // ERROR_SEM_TIMEOUT

        public static int FromException(Exception exception)
        {
            Throw.If.Null(exception);

            try
            {
                // This works with .NET 4.0 as well as later versions. Also, it does not change any state.
                return (int)exception.GetType().InvokeMember("HResult",
                    BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, exception, new object[0]);
            }
            catch
            {
                return Marshal.GetHRForException(exception);
            }
        }
    }
}
