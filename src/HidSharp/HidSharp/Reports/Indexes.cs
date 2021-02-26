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

using System.Collections.Generic;
using System.Linq;

namespace HidSharp.Reports
{
    public class Indexes
    {
        static readonly Indexes _unset = new Indexes();

        public bool ContainsValue(uint value)
        {
            int index; return TryGetIndexFromValue(value, out index);
        }

        public IEnumerable<uint> GetAllValues()
        {
            return Enumerable.Range(0, Count).SelectMany(index => GetValuesFromIndex(index));
        }

        public virtual bool TryGetIndexFromValue(uint value, out int elementIndex)
        {
            elementIndex = -1; return false;
        }

        public virtual IEnumerable<uint> GetValuesFromIndex(int elementIndex)
        {
            yield break;
        }

        public virtual int Count
        {
            get { return 0; }
        }

        public static Indexes Unset
        {
            get { return _unset; }
        }
    }
}
