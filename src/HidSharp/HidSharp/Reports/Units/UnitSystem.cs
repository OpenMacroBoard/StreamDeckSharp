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
    /// Defines the possible unit systems.
    /// </summary>
    public enum UnitSystem
    {
        /// <summary>
        /// No units are used.
        /// </summary>
        None = 0,

        /// <summary>
        /// The SI Linear unit system uses centimeters for length, grams for mass, seconds for time,
        /// Kelvin for temperature, Amperes for current, and candelas for luminous intensity.
        /// </summary>
        SILinear,

        /// <summary>
        /// The SI Rotation unit system uses radians for length, grams for mass, seconds for time,
        /// Kelvin for temperature, Amperes for current, and candelas for luminous intensity.
        /// </summary>
        SIRotation,

        /// <summary>
        /// The English Linear unit system uses inches for length, slugs for mass, seconds for time,
        /// Fahrenheit for temperature, Amperes for current, and candelas for luminous intensity.
        /// </summary>
        EnglishLinear,

        /// <summary>
        /// The English Rotation unit system uses degrees for length, slugs for mass, seconds for time,
        /// Fahrenheit for temperature, Amperes for current, and candelas for luminous intensity.
        /// </summary>
        EnglishRotation
    }
}
