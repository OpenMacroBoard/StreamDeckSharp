#region License
/* Copyright 2016, 2017 James F. Bellinger <http://www.zer7.com/software/hidsharp>

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

namespace HidSharp
{
    /// <summary>
    /// Describes all options for opening a device stream.
    /// </summary>
    public class OpenConfiguration : ICloneable
    {
        Dictionary<OpenOption, object> _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenConfiguration"/> class.
        /// </summary>
        public OpenConfiguration()
        {
            _options = new Dictionary<OpenOption, object>();
        }

        OpenConfiguration(Dictionary<OpenOption, object> options)
        {
            _options = new Dictionary<OpenOption, object>(options);
        }

        public OpenConfiguration Clone()
        {
            return new OpenConfiguration(_options);
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        /// <summary>
        /// Gets the current value of an option.
        /// </summary>
        /// <param name="option">The option.</param>
        /// <returns>The option's value.</returns>
        public object GetOption(OpenOption option)
        {
            Throw.If.Null(option, "option");
            
            object value;
            return _options.TryGetValue(option, out value) ? value : option.DefaultValue;
        }

        /// <summary>
        /// Gets a list of all currently set options.
        /// </summary>
        /// <returns>The options list.</returns>
        public IEnumerable<OpenOption> GetOptionsList()
        {
            return _options.Keys;
        }

        /// <summary>
        /// Checks if an option has been set.
        /// </summary>
        /// <param name="option">The option.</param>
        /// <returns><c>true</c> if the option has been set.</returns>
        public bool IsOptionSet(OpenOption option)
        {
            Throw.If.Null(option, "option");

            return _options.ContainsKey(option);
        }

        /// <summary>
        /// Sets the current value of an option.
        /// </summary>
        /// <param name="option">The option.</param>
        /// <param name="value">The value to set it to.</param>
        public void SetOption(OpenOption option, object value)
        {
            Throw.If.Null(option, "option");

            if (value != null)
            {
                _options[option] = value;
            }
            else
            {
                _options.Remove(option);
            }
        }
    }
}
