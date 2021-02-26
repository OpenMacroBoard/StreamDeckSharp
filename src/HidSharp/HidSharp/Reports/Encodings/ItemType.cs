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

namespace HidSharp.Reports.Encodings
{
    /// <summary>
    /// Describes the manner in which an item affects the descriptor.
    /// </summary>
    public enum ItemType : byte
    {
        /// <summary>
        /// Main items determine the report being described.
        /// For example, a main item switches between Input and Output reports.
        /// </summary>
        Main = 0,

        /// <summary>
        /// Global items affect all reports later in the descriptor.
        /// </summary>
        Global,

        /// <summary>
        /// Local items only affect the current report.
        /// </summary>
        Local,

        /// <summary>
        /// Long items use this type.
        /// </summary>
        Reserved
    }
}