/*
 * Copyright (2011) Intel Corporation and Sandia Corporation. Under the
 * terms of Contract DE-AC04-94AL85000 with Sandia Corporation, the
 * U.S. Government retains certain rights in this software.
 * 
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 * 
 * -- Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 * -- Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 * -- Neither the name of the Intel Corporation nor the names of its
 *    contributors may be used to endorse or promote products derived from
 *    this software without specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
 * ``AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
 * LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A
 * PARTICULAR PURPOSE ARE DISCLAIMED.  IN NO EVENT SHALL THE INTEL OR ITS
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
 * EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
 * PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
 * PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
 * LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using NHibernate;
using NHibernate.UserTypes;
using OpenMetaverse;

namespace WaterWars.Persistence
{
    /// <summary>
    /// A mapper to tranlate between OpenMetaverse.UUID and the database
    /// Commented out for now as I don't want the NHibernate reference
    /// </summary>
    public class UUIDMapper //: IUserType
    {
//        public object Assemble(object cached, object owner)
//        {
//            return DeepCopy(cached);
//        }
// 
//        public object DeepCopy(object value)
//        {
//            return value;
//        }
// 
//        public object Disassemble(object value)
//        {
//            return DeepCopy(value);
//        }
// 
//        public new bool Equals(object x, object y)
//        {
//            if (object.ReferenceEquals(x, y)) return true;
//            if (x == null || y == null) return false;
//            return x.Equals(y);
//        }
// 
//        public int GetHashCode(object x)
//        {
//            return x.GetHashCode();
//        }
// 
//        public bool IsMutable
//        {
//            get { return false; }
//        }
// 
//        public object NullSafeGet(System.Data.IDataReader rs, string[] names, object owner)
//        {
//            string rawUuid = NHibernateUtil.String.NullSafeGet(rs, names[0]) as string;
//            return new UUID(rawUuid);
//        }
// 
//        public void NullSafeSet(System.Data.IDbCommand cmd, object value, int index)
//        {            
//            IDataParameter parameter = (IDataParameter)cmd.Parameters[index];
//            if (value == null)
//                parameter.Value = DBNull.Value;
//            else
//                parameter.Value = ((UUID)value).ToString();
//        }
// 
//        public object Replace(object original, object target, object owner)
//        {
//            return original;
//        }
// 
//        public Type ReturnedType
//        {
//            //the .Net type that this maps to
//            get { return typeof(UUID); }
//        }
// 
//        public NHibernate.SqlTypes.SqlType[] SqlTypes
//        {
//            //the sql type that this maps to
//            get { return new NHibernate.SqlTypes.SqlType[] { NHibernateUtil.String.SqlType }; }
//        }
    }
}