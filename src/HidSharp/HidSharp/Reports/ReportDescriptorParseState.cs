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
using System.Collections.Generic;
using HidSharp.Reports.Encodings;

namespace HidSharp.Reports
{
    // TODO: Make this public if anyone finds value in doing so. For now let's not lock ourselves in.
    sealed class ReportDescriptorParseState
    {
        public ReportDescriptorParseState()
        {
            RootItem = new DescriptorCollectionItem();
            GlobalItemStateStack = new List<IDictionary<GlobalItemTag, EncodedItem>>();
            LocalItemState = new List<KeyValuePair<LocalItemTag, uint>>();
            Reset();
        }

        public void Reset()
        {
            CurrentCollectionItem = RootItem;
            RootItem.ChildItems.Clear();
            RootItem.CollectionType = 0;

            GlobalItemStateStack.Clear();
            GlobalItemStateStack.Add(new Dictionary<GlobalItemTag, EncodedItem>());
            LocalItemState.Clear();
        }

        public EncodedItem GetGlobalItem(GlobalItemTag tag)
        {
            EncodedItem value;
            GlobalItemState.TryGetValue(tag, out value);
            return value;
        }

        public uint GetGlobalItemValue(GlobalItemTag tag)
        {
            EncodedItem item = GetGlobalItem(tag);
            return item != null ? item.DataValue : 0;
        }

        public bool IsGlobalItemSet(GlobalItemTag tag)
        {
            return GlobalItemState.ContainsKey(tag);
        }

        public DescriptorCollectionItem CurrentCollectionItem
        {
            get;
            set;
        }

        public DescriptorCollectionItem RootItem
        {
            get;
            private set;
        }

        public IDictionary<GlobalItemTag, EncodedItem> GlobalItemState
        {
            get { return GlobalItemStateStack[GlobalItemStateStack.Count - 1]; }
        }

        public IList<IDictionary<GlobalItemTag, EncodedItem>> GlobalItemStateStack
        {
            get;
            private set;
        }

        public IList<KeyValuePair<LocalItemTag, uint>> LocalItemState
        {
            get;
            private set;
        }
    }
}
