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
using OpenMetaverse;

namespace WaterWars.Models
{
    /// <summary>
    /// Represents crops in the game
    /// </summary>
    [Serializable]
    public class Crops : AbstractGameAsset
    {
        public static Crops Template = new Crops("TEMPLATE", UUID.Zero, Vector3.Zero);
  
        /// <summary>
        /// For NHibernate
        /// </summary>
        protected Crops() {}
        
        public Crops(string name, UUID uuid, Vector3 position) : this(name, uuid, position, 1) {}
                                                                                 
        public Crops(string name, UUID uuid, Vector3 position, int level) 
            : base(name, uuid, AbstractGameAssetType.Crops, position, level, 1, 3)
        {            
            CanBeAllocatedWater = true;
            CanBeSoldToEconomy = false;
            CanPartiallyAllocateWater = false;
            CanUpgradeInPrinciple = false;
            IsDependentOnWaterToExist = true;
        }    
        
        public override int NominalMaximumProfitThisTurn
        {
            get
            {
                // Infinitely lived crops don't take into account the initial construction cost
                if (TimeToLive == INFINITE_TIME_TO_LIVE)
                    return RevenueThisTurn - MaintenanceCost;
                else
                    return RevenueThisTurn - ConstructionCost - MaintenanceCost;                    
            }
        }  
        
        public override int Profit
        {
            get
            {
                // Infinitely lived crops don't take into account the initial construction cost
                if (TimeToLive == INFINITE_TIME_TO_LIVE)
                    return ProjectedRevenue - MaintenanceCost;
                else
                    return ProjectedRevenue - ConstructionCost - MaintenanceCost;                
            }
        }           
    }
}