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

namespace HidSharp
{
    struct SerialSettings
    {
        public static readonly SerialSettings Default = new SerialSettings()
        {
            BaudRate = 9600, DataBits = 8, Parity = SerialParity.None, StopBits = 1
        };

        public int BaudRate;
        public int DataBits;
        public SerialParity Parity;
        public int StopBits;

        public void SetBaudRate(int baudRate, object @lock, ref bool settingsChanged)
        {
            if (baudRate < 0) { throw new NotSupportedException(); }

            lock (@lock)
            {
                if (BaudRate == baudRate) { return; }
//Console.WriteLine(string.Format("Baud {0} -> {1}", BaudRate, baudRate));
                BaudRate = baudRate; settingsChanged = true;
            }
        }

        public void SetDataBits(int dataBits, object @lock, ref bool settingsChanged)
        {
            if (dataBits < 7 || dataBits > 8) { throw new NotSupportedException(); }

            lock (@lock)
            {
                if (DataBits == dataBits) { return; }
//Console.WriteLine(string.Format("Data Bits {0} -> {1}", DataBits, dataBits));
                DataBits = dataBits; settingsChanged = true;
            }
        }

        public void SetParity(SerialParity parity, object @lock, ref bool settingsChanged)
        {
            lock (@lock)
            {
                if (Parity == parity) { return; }
//Console.WriteLine(string.Format("Parity {0} -> {1}", Parity, parity));
                Parity = parity; settingsChanged = true;
            }
        }

        public void SetStopBits(int stopBits, object @lock, ref bool settingsChanged)
        {
            if (stopBits < 1 || stopBits > 2) { throw new NotSupportedException(); }

            lock (@lock)
            {
                if (StopBits == stopBits) { return; }
//Console.WriteLine(string.Format("Stop Bits {0} -> {1}", StopBits, stopBits));
                StopBits = stopBits; settingsChanged = true;
            }
        }
    }
}
