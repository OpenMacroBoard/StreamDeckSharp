#region License
/* Copyright 2011, 2013 James F. Bellinger <http://www.zer7.com/software/hidsharp>

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

namespace HidSharp.Reports.Units
{
    /// <summary>
    /// Defines the possible units of length.
    /// </summary>
    public enum LengthUnit
    {
        /// <summary>
        /// The unit system has no unit of length.
        /// </summary>
        None,

        /// <summary>
        /// The unit of length is the centimeter (occurs in the SI Linear unit system).
        /// </summary>
        Centimeter,

        /// <summary>
        /// The unit of length is the radian (occurs in the SI Rotation unit system).
        /// </summary>
        Radians,

        /// <summary>
        /// The unit of length is the inch (occurs in the English Linear unit system).
        /// </summary>
        Inch,

        /// <summary>
        /// The unit of length is the degree (occurs in the English Rotation unit system).
        /// </summary>
        Degrees
    }
}
