#region License
/* Copyright 2011 James F. Bellinger <http://www.zer7.com/software/hidsharp>

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

namespace HidSharp.Reports
{
    [Flags]
    public enum DataItemFlags : uint
    {
        /// <summary>
        /// No flags are set.
        /// </summary>
        None = 0,

        /// <summary>
        /// Constant values cannot be changed.
        /// </summary>
        Constant = 1 << 0,

        /// <summary>
        /// Each variable field corresponds to a particular value.
        /// The alternative is an array, where each field specifies an index.
        /// For example, with eight buttons, a variable field would have eight bits.
        /// An array would have an index of which button is pressed.
        /// </summary>
        Variable = 1 << 1,

        /// <summary>
        /// Mouse motion is in relative coordinates.
        /// Most sensors -- joysticks, accelerometers, etc. -- output absolute coordinates.
        /// </summary>
        Relative = 1 << 2,

        /// <summary>
        /// The value wraps around in a continuous manner.
        /// </summary>
        Wrap = 1 << 3,

        Nonlinear = 1 << 4,

        NoPreferred = 1 << 5,

        NullState = 1 << 6,

        Volatile = 1 << 7,

        BufferedBytes = 1 << 8
    }
}
