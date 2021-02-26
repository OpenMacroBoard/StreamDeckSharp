#region License
/* Copyright 2018 James F. Bellinger <http://www.zer7.com/software/hidsharp>

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

namespace HidSharp.Reports
{
    public enum ExpectedUsageType
    {
        /// <summary>
        /// Level-triggered. A momentary button. 0 indicates not pressed, 1 indicates pressed.
        /// </summary>
        PushButton = 1,

        /// <summary>
        /// Level-triggered. Toggle buttons maintain their state. 0 indicates not pressed, 1 indicates pressed.
        /// </summary>
        ToggleButton,

        /// <summary>
        /// Edge-triggered. A 0-to-1 transition should activate the one-shot function.
        /// </summary>
        OneShot,

        /// <summary>
        /// Edge-triggered. Each report of -1 goes down. Each report of 1 goes up.
        /// </summary>
        UpDown
    }
}
