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
using System.Reflection;
using log4net;
using OpenMetaverse;
using System.Collections;
using System.Collections.Generic;

namespace WaterWars.Models
{
    /// <summary>
    /// The top level class for all Water Wars models
    /// </summary> 
    [Serializable]   
    public class AbstractModel
    {                
//        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        public delegate void OnChangeDelegate(AbstractModel model);
        
        /// <summary>
        /// Fired when the mode has changed in some way
        /// </summary>
        [field:NonSerialized]
        public virtual event OnChangeDelegate OnChange;      
        
        /// <value>
        /// OpenSim's unique id
        /// </value>
        public virtual UUID Uuid { get; set; }

        /// <value>
        /// Name.
        /// </value>
        public virtual string Name { get; set; }

        public AbstractModel() {}

        public AbstractModel(UUID uuid) : this(uuid, string.Empty) {}        

        public AbstractModel(UUID uuid, string name) : this()
        {
            Uuid = uuid;
            Name = name;            
        }
                
        /// <summary>
        /// Notify listeners that this model has changed.
        /// </summary>
        /// 
        /// FIXME: This really, really should be done in a transactional model rather than manually by the caller
        public virtual void TriggerChanged()
        {
//            m_log.InfoFormat("[WATER WARS]: OnChange triggered for {0}", ToString());
            
            if (OnChange != null)
                OnChange(this);
        }
                
        public override string ToString()
        {
            return string.Format("Uuid={0}, Name={1}", Uuid, Name);
        }
    }   
}