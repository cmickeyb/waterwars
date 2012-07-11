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
using System.Collections.Generic;
using OpenMetaverse;

namespace WaterWars.Models
{
    /// <summary>
    /// The top level class for all Water Wars game models.  This does not include the game itself or players.
    /// </summary>    
    [Serializable]
    public class AbstractGameModel : AbstractModel
    { 
        /// <value>
        /// The type of this model
        /// </value>
        public virtual AbstractGameAssetType Type { get; set; }

        /// <value>
        /// The game to which this asset belongs.
        /// </value>        
        public virtual Game Game { get; set; }
        
        /// <value>
        /// These are actions that the owner can take.  This collection should only be used for a short time since it
        /// will not reflect a change in game state.
        /// </value>
        public virtual Dictionary<string, bool> OwnerActions { get { return new Dictionary<string, bool>(); } }

        /// <value>
        /// These are actions that non-owners can take.  This collection should only be used for a short time since it
        /// will not reflect a change in game state.
        /// </value>        
        public virtual Dictionary<string, bool> NonOwnerActions { get { return new Dictionary<string, bool>(); } }

        /// <summary>
        /// For NHibernate
        /// </summary>
        protected AbstractGameModel() {}
        
        public AbstractGameModel(UUID uuid) : this(uuid, AbstractGameAssetType.None, string.Empty) {}        

        public AbstractGameModel(UUID uuid, AbstractGameAssetType type, string name) : base(uuid, name)
        {
            Game = Game.None;
            Type = type;           
        }

        public override string ToString()
        {
            return string.Format("Uuid={0}, Name={1}, Type={2}", Uuid, Name, Type);
        }
    }
}
