#region License
/* Copyright 2011, 2018 James F. Bellinger <http://www.zer7.com/software/hidsharp>

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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using HidSharp.Reports.Encodings;

namespace HidSharp.Reports
{
    public class DescriptorCollectionItem : DescriptorItem
    {
        ReportCollectionItemChildren _children;

        public DescriptorCollectionItem()
        {
            _children = new ReportCollectionItemChildren(this);
        }

        public override IList<DescriptorItem> ChildItems
        {
            get { return _children; }
        }

        public CollectionType CollectionType
        {
            get;
            set;
        }

        #region ReportCollectionItemChildren
        sealed class ReportCollectionItemChildren : Collection<DescriptorItem>
        {
            DescriptorCollectionItem _item;

            public ReportCollectionItemChildren(DescriptorCollectionItem item)
            {
                Debug.Assert(item != null);
                _item = item;
            }

            protected override void ClearItems()
            {
                foreach (var item in this) { item.ParentItem = null; }
                base.ClearItems();
            }

            protected override void InsertItem(int index, DescriptorItem item)
            {
                Throw.If.Null(item).False(item.ParentItem == null);
                item.ParentItem = _item;
                base.InsertItem(index, item);
            }

            protected override void RemoveItem(int index)
            {
                this[index].ParentItem = null;
                base.RemoveItem(index);
            }

            protected override void SetItem(int index, DescriptorItem item)
            {
                throw new NotImplementedException();
            }
        }
        #endregion
    }
}
