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
using Newtonsoft.Json;
using OpenMetaverse;

namespace WaterWars.Models
{   
    [Serializable]
    public class Field : AbstractGameModel
    {
        public static Field None = new Field(UUID.Zero, "No Name");
        
        /// <value>
        /// Reference back to the buypoint.  This is only here for JSON purposes.
        /// </value>
        public UUID BuyPointUuid { get { return BuyPoint.Uuid; } }

        /// <value>
        /// Reference to the owner.  Again, this is only here for JSON purposes.
        /// </value>        
        public Player Owner { get; set; }

        /// <value>
        /// The parcel that this field is in
        /// </value>
        [JsonIgnore]
        public BuyPoint BuyPoint 
        { 
            get { return m_buyPoint; }            
            set 
            {
                if (m_buyPoint != null)
                    m_buyPoint.Fields.Remove(Uuid);
                
                m_buyPoint = value;

                if (m_buyPoint != null)
                    value.Fields[Uuid] = this;                
            }
        }
        protected BuyPoint m_buyPoint;

        public override Dictionary<string, bool> OwnerActions 
        { 
            get
            {
                Dictionary<string, bool> actions = base.OwnerActions;
                
                // We're naughty here and give the BuyAsset hint if we are in the revenue stage in case this field
                // is being displayed after an asset has been removed.
                if (Game.State == GameStateType.Build || Game.State == GameStateType.Revenue)
                {
                    if (BuyPoint.DevelopmentRightsOwner != Player.None)
                    {
                        AbstractGameAsset template = BuyPoint.DevelopmentRightsOwner.Role.AllowedAssets[0];

                        if (BuyPoint.DevelopmentRightsOwner.Money 
                            >= template.ConstructionCostsPerBuildStep[template.MinLevel])
                            actions["BuyAsset"] = true;
                    }
                }

                return actions;
            }  
        }
        
        public Field(UUID uuid, string name) : base(uuid, AbstractGameAssetType.Field, name)
        {
            BuyPoint = BuyPoint.None;
        }        

        public override string ToString()
        {
            return string.Format("{0}, BuyPoint = {1}", base.ToString(), BuyPoint);
        }
    }
}