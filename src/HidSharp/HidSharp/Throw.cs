#region License
/* Copyright 2013 James F. Bellinger <http://www.zer7.com/software/hidsharp>

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
using System.Collections.Generic;

namespace HidSharp
{
    sealed class Throw
    {
        Throw()
        {

        }

        public static Throw If
        {
            get { return null; }
        }
    }

    static class ThrowExtensions
    {
        public static Throw False(this Throw self, bool condition)
        {
            if (!condition) { throw new ArgumentException(); }
            return null;
        }

        public static Throw False(this Throw self, bool condition, string message, string paramName)
        {
            if (!condition) { throw new ArgumentException(message, paramName); }
            return null;
        }

        public static Throw Negative(this Throw self, int value, string paramName)
        {
            if (value < 0) { throw new ArgumentOutOfRangeException(paramName); }
            return null;
        }

        public static Throw Null<T>(this Throw self, T value)
        {
            if (value == null) { throw new ArgumentNullException(); }
            return null;
        }

        public static Throw Null<T>(this Throw self, T value, string paramName)
        {
            if (value == null) { throw new ArgumentNullException(paramName); }
            return null;
        }

        public static Throw NullOrEmpty(this Throw self, string value, string paramName)
        {
            Throw.If.Null(value, paramName);
            if (value.Length == 0) { throw new ArgumentException("Must not be empty.", paramName); }
            return null;
        }

        public static Throw OutOfRange(this Throw self, int bufferSize, int offset, int count)
        {
            if (offset < 0 || offset > bufferSize) { throw new ArgumentOutOfRangeException("offset"); }
            if (count < 0 || count > bufferSize - offset) { throw new ArgumentOutOfRangeException("count"); }
            return null;
        }

        public static Throw OutOfRange<T>(this Throw self, IList<T> buffer, int offset, int count)
        {
            Throw.If.Null(buffer, "buffer").OutOfRange(buffer.Count, offset, count);
            return null;
        }
    }
}
