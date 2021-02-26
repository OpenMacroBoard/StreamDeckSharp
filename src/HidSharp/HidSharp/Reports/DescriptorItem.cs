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
using System.Collections.ObjectModel;

namespace HidSharp.Reports
{
    public class DescriptorItem
    {
        static readonly IList<DescriptorItem> _noChildren = new ReadOnlyCollection<DescriptorItem>(new DescriptorItem[0]);

        Indexes _designator, _string, _usage;

        public DescriptorItem()
        {

        }

        public virtual IList<DescriptorItem> ChildItems
        {
            get { return _noChildren; }
        }

        public DescriptorCollectionItem ParentItem
        {
            get;
            internal set;
        }

        public Indexes Designators
        {
            get { return _designator ?? Indexes.Unset; }
            set { _designator = value; }
        }

        public Indexes Strings
        {
            get { return _string ?? Indexes.Unset; }
            set { _string = value; }
        }

        public Indexes Usages
        {
            get { return _usage ?? Indexes.Unset; }
            set { _usage = value; }
        }
    }
}
