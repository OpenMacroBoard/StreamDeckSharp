#region License
/* Copyright 2011, 2013, 2018 James F. Bellinger <http://www.zer7.com/software/hidsharp>

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

namespace HidSharp.Reports.Units
{
    /// <summary>
    /// Describes the units of a report value.
    /// </summary>
    public struct Unit : IEquatable<Unit>
    {
        uint _rawValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="Unit"/> class.
        /// </summary>
        /// <param name="rawValue">The raw HID value describing the units.</param>
        public Unit(uint rawValue)
        {
            _rawValue = rawValue;
        }

        public override bool Equals(object obj)
        {
            return obj is Unit && Equals((Unit)obj);
        }

        public bool Equals(Unit other)
        {
            return RawValue == other.RawValue;
        }

        public override int GetHashCode()
        {
            return _rawValue.GetHashCode();
        }

        uint GetElement(UnitKind kind)
        {
            int index = (int)kind;
            return (RawValue >> (index << 2)) & 0xf;
        }

        public int GetExponent(UnitKind kind)
        {
            return DecodeExponent(GetElement(kind));
        }

        /// <summary>
        /// Decodes an encoded HID unit exponent.
        /// </summary>
        /// <param name="value">The encoded exponent.</param>
        /// <returns>The exponent.</returns>
        public static int DecodeExponent(uint value)
        {
            if (value > 15) { throw new ArgumentOutOfRangeException("value", "Value range is [0, 15]."); }
            return value >= 8 ? (int)value - 16 : (int)value;
        }

        void SetElement(UnitKind kind, uint value)
        {
            int index = (int)kind;
            RawValue &= 0xfu << (index << 2); RawValue |= (value & 0xfu) << (index << 2);
        }

        /// <summary>
        /// Encodes an exponent in HID unit form.
        /// </summary>
        /// <param name="value">The exponent.</param>
        /// <returns>The encoded exponent.</returns>
        public static uint EncodeExponent(int value)
        {
            if (value < -8 || value > 7)
                { throw new ArgumentOutOfRangeException("value", "Exponent range is [-8, 7]."); }
            return (uint)(value < 0 ? value + 16 : value);
        }

        public void SetExponent(UnitKind kind, int value)
        {
            SetElement(kind, EncodeExponent(value));
        }

        /// <summary>
        /// Gets or sets the unit system.
        /// </summary>
        public UnitSystem System
        {
            get { return (UnitSystem)GetElement(0); }
            set { SetElement(0, (uint)value); }
        }

        /// <summary>
        /// Gets or sets the exponent of the report value's units of length.
        /// </summary>
        public int LengthExponent
        {
            get { return GetExponent(UnitKind.Length); }
            set { SetExponent(UnitKind.Length, value); }
        }

        /// <summary>
        /// Gets the units of length corresponding to <see cref="System"/>.
        /// </summary>
        public LengthUnit LengthUnit
        {
            get
            {
                switch (System)
                {
                    case UnitSystem.SILinear: return LengthUnit.Centimeter;
                    case UnitSystem.SIRotation: return LengthUnit.Radians;
                    case UnitSystem.EnglishLinear: return LengthUnit.Inch;
                    case UnitSystem.EnglishRotation: return LengthUnit.Degrees;
                    default: return LengthUnit.None;
                }
            }
        }

        /// <summary>
        /// Gets or sets the exponent of the report value's units of mass.
        /// </summary>
        public int MassExponent
        {
            get { return GetExponent(UnitKind.Mass); }
            set { SetExponent(UnitKind.Mass, value); }
        }

        /// <summary>
        /// Gets the units of mass corresponding to <see cref="System"/>.
        /// </summary>
        public MassUnit MassUnit
        {
            get
            {
                switch (System)
                {
                    case UnitSystem.SILinear:
                    case UnitSystem.SIRotation: return MassUnit.Gram;
                    case UnitSystem.EnglishLinear:
                    case UnitSystem.EnglishRotation: return MassUnit.Slug;
                    default: return MassUnit.None;
                }
            }
        }

        /// <summary>
        /// Gets or sets the exponent of the report value's units of time.
        /// </summary>
        public int TimeExponent
        {
            get { return GetExponent(UnitKind.Time); }
            set { SetExponent(UnitKind.Time, value); }
        }

        /// <summary>
        /// Gets the units of time corresponding to <see cref="System"/>.
        /// </summary>
        public TimeUnit TimeUnit
        {
            get
            {
                return System != UnitSystem.None
                    ? TimeUnit.Seconds : TimeUnit.None;
            }
        }

        /// <summary>
        /// Gets or sets the exponent of the report value's units of temperature.
        /// </summary>
        public int TemperatureExponent
        {
            get { return GetExponent(UnitKind.Temperature); }
            set { SetExponent(UnitKind.Temperature, value); }
        }

        /// <summary>
        /// Gets the units of temperature corresponding to <see cref="System"/>.
        /// </summary>
        public TemperatureUnit TemperatureUnit
        {
            get
            {
                switch (System)
                {
                    case UnitSystem.SILinear:
                    case UnitSystem.SIRotation: return TemperatureUnit.Kelvin;
                    case UnitSystem.EnglishLinear:
                    case UnitSystem.EnglishRotation: return TemperatureUnit.Fahrenheit;
                    default: return TemperatureUnit.None;
                }
            }
        }

        /// <summary>
        /// Gets or sets the exponent of the report value's units of current.
        /// </summary>
        public int CurrentExponent
        {
            get { return GetExponent(UnitKind.Current); }
            set { SetExponent(UnitKind.Current, value); }
        }

        /// <summary>
        /// Gets the units of current corresponding to <see cref="System"/>.
        /// </summary>
        public CurrentUnit CurrentUnit
        {
            get
            {
                return System != UnitSystem.None
                    ? CurrentUnit.Ampere : CurrentUnit.None;
            }
        }

        /// <summary>
        /// Gets or sets the exponent of the report value's units of luminous intensity.
        /// </summary>
        public int LuminousIntensityExponent
        {
            get { return GetExponent(UnitKind.LuminousIntensity); }
            set { SetExponent(UnitKind.LuminousIntensity, value); }
        }

        /// <summary>
        /// Gets the units of luminous intensity corresponding to <see cref="System"/>.
        /// </summary>
        public LuminousIntensityUnit LuminousIntensityUnit
        {
            get
            {
                return System != UnitSystem.None
                    ? LuminousIntensityUnit.Candela : LuminousIntensityUnit.None;
            }
        }

        /// <summary>
        /// Gets or sets the raw HID value describing the units.
        /// </summary>
        public uint RawValue
        {
            get { return _rawValue; }
            set { _rawValue = value; }
        }

        public static bool operator ==(Unit unit1, Unit unit2)
        {
            return unit1.Equals(unit2);
        }

        public static bool operator !=(Unit unit1, Unit unit2)
        {
            return !unit1.Equals(unit2);
        }
    }
}
