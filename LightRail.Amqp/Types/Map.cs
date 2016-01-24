//  ------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation
//  All rights reserved. 
//  
//  Licensed under the Apache License, Version 2.0 (the ""License""); you may not use this 
//  file except in compliance with the License. You may obtain a copy of the License at 
//  http://www.apache.org/licenses/LICENSE-2.0  
//  
//  THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, 
//  EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR 
//  CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR 
//  NON-INFRINGEMENT. 
// 
//  See the Apache Version 2.0 License for specific language governing permissions and 
//  limitations under the License.
//  ------------------------------------------------------------------------------------
//
//  ------------------------------------------------------------------------------------
// Modifications Copyright (c) 2016 Joseph Daigle
// Licensed under the MIT License. See LICENSE file in the repository root for license information.
//  ------------------------------------------------------------------------------------

using System;
using System.Collections;

namespace LightRail.Amqp.Types
{
    /// <summary>
    /// A Map class is an AMQP map.
    /// </summary>
    public class Map : Hashtable
    {
        public object GetValue(object key)
        {
            return base[key];
        }

        /// <summary>
        /// Gets or sets an item in the map.
        /// </summary>
        /// <param name="key">The key of the item.</param>
        /// <returns></returns>
        public new object this[object key]
        {
            get
            {
                this.CheckKeyType(key.GetType());
                return this.GetValue(key);
            }

            set
            {
                this.CheckKeyType(key.GetType());
                base[key] = value;
            }
        }

        internal static void ValidateKeyType(Type expected, Type actual)
        {
            if (expected != actual)
            {
                throw new InvalidOperationException($"The key type {actual.Name} is invalid. The map key is restricted to {expected.Name}.");
            }
        }

        protected virtual void CheckKeyType(Type keyType)
        {
        }
    }
}
