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
    /// Defines the possible units of temperature.
    /// </summary>
    public enum TemperatureUnit
    {
        /// <summary>
        /// The unit system has no unit of temperature.
        /// </summary>
        None,

        /// <summary>
        /// The unit of temperature is Kelvin (occurs in SI Linear and Rotation unit systems).
        /// </summary>
        Kelvin,

        /// <summary>
        /// The unit of temperature is Fahrenheit (occurs in English Linear and Rotation unit systems).
        /// </summary>
        Fahrenheit
    }
}
