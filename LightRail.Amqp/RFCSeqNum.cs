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

namespace LightRail.Amqp
{
    /// <summary>
    /// An implementation of RFC1982: http://tools.ietf.org/html/rfc1982.
    /// 
    /// 
    /// </summary>
    public struct RFCSeqNum
    {
        private readonly int sequenceNumber;

        public RFCSeqNum(uint value)
        {
            sequenceNumber = (int)value;
        }

        public static implicit operator RFCSeqNum(uint value)
        {
            return new RFCSeqNum(value);
        }

        public static implicit operator uint(RFCSeqNum value)
        {
            return (uint)value.sequenceNumber;
        }

        public int CompareTo(RFCSeqNum value)
        {
            int delta = this.sequenceNumber - value.sequenceNumber;
            if (delta == int.MinValue)
            {
                // Behavior of comparing 0u-2147483648u, 1u-2147483649u, ...
                // is undefined, so we do not allow it.
                throw new AmqpException(ErrorCode.NotAllowed
                    , $"Comparison of {this.sequenceNumber.ToString()} and {value.sequenceNumber.ToString()} is invalid because the result is undefined.");
            }

            return delta;
        }

        public static RFCSeqNum operator ++(RFCSeqNum value)
        {
            return (uint)unchecked(value.sequenceNumber + 1);
        }

        public static RFCSeqNum operator +(RFCSeqNum value1, int delta)
        {
            return (uint)unchecked(value1.sequenceNumber + delta);
        }

        public static RFCSeqNum operator -(RFCSeqNum value1, int delta)
        {
            return (uint)unchecked(value1.sequenceNumber - delta);
        }

        public static int operator -(RFCSeqNum value1, RFCSeqNum value2)
        {
            return value1.sequenceNumber - value2.sequenceNumber;
        }

        public static bool operator ==(RFCSeqNum value1, RFCSeqNum value2)
        {
            return value1.sequenceNumber == value2.sequenceNumber;
        }

        public static bool operator !=(RFCSeqNum value1, RFCSeqNum value2)
        {
            return value1.sequenceNumber != value2.sequenceNumber;
        }

        public static bool operator >(RFCSeqNum value1, RFCSeqNum value2)
        {
            return value1.CompareTo(value2) > 0;
        }

        public static bool operator >=(RFCSeqNum value1, RFCSeqNum value2)
        {
            return value1.CompareTo(value2) >= 0;
        }

        public static bool operator <(RFCSeqNum value1, RFCSeqNum value2)
        {
            return value1.CompareTo(value2) < 0;
        }

        public static bool operator <=(RFCSeqNum value1, RFCSeqNum value2)
        {
            return value1.CompareTo(value2) <= 0;
        }

        public override int GetHashCode()
        {
            return this.sequenceNumber.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj is RFCSeqNum && ((RFCSeqNum)obj).sequenceNumber == this.sequenceNumber;
        }

        public override string ToString()
        {
            return this.sequenceNumber.ToString();
        }
    }
}
