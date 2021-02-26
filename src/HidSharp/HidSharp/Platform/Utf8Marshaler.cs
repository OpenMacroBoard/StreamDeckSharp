#region License
/* Copyright 2012 James F. Bellinger <http://www.zer7.com/software/hidsharp>

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
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace HidSharp.Platform
{
    sealed class Utf8Marshaler : ICustomMarshaler
    {
        [ThreadStatic]
        static HashSet<IntPtr> _allocations; // Workaround for Mono bug 4722.

        static HashSet<IntPtr> GetAllocations()
        {
            if (_allocations == null) { _allocations = new HashSet<IntPtr>(); }
            return _allocations;
        }

        public void CleanUpManagedData(object obj)
        {

        }

        public void CleanUpNativeData(IntPtr ptr)
        {
            var allocations = GetAllocations();
			if (IntPtr.Zero == ptr || !allocations.Contains(ptr)) { return; }
            Marshal.FreeHGlobal(ptr); allocations.Remove(ptr);
        }

        public int GetNativeDataSize()
        {
            return -1;
        }

        public IntPtr MarshalManagedToNative(object obj)
        {
            string str = obj as string;
            if (str == null) { return IntPtr.Zero; }

            byte[] bytes = Encoding.UTF8.GetBytes(str);
            IntPtr ptr = Marshal.AllocHGlobal(bytes.Length + 1);
            Marshal.Copy(bytes, 0, ptr, bytes.Length);
            Marshal.WriteByte(ptr, bytes.Length, 0);

            var allocations = GetAllocations();
            allocations.Add(ptr); return ptr;
        }

        public object MarshalNativeToManaged(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero) { return null; }

            int length;
            for (length = 0; Marshal.ReadByte(ptr, length) != 0; length++) ;

            byte[] bytes = new byte[length];
            Marshal.Copy(ptr, bytes, 0, bytes.Length);
            string str = Encoding.UTF8.GetString(bytes);
            return str;
        }
		
        // This method needs to keep its original name.
        [Obfuscation(Exclude = true)]
		public static ICustomMarshaler GetInstance(string cookie)
		{
			return new Utf8Marshaler();
		}
    }
}
