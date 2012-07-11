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
    /// Represents houses in the game
    /// </summary>
    [Serializable]
    public class Houses : AbstractGameAsset
    {
        public static Houses Template = new Houses("TEMPLATE", UUID.Zero, Vector3.Zero);

        /// <summary>
        /// For NHibernate
        /// </summary>
        protected Houses() {}
        
        public Houses(string name, UUID uuid, Vector3 position) : this(name, uuid, position, 1) {}
            
        public Houses(string name, UUID uuid, Vector3 position, int level) 
            : base(name, uuid, AbstractGameAssetType.Houses, position, level, 1, 3)
        {
            CanBeAllocatedWater = false;
            CanBeSoldToEconomy = true;
            CanPartiallyAllocateWater = false;
            CanUpgradeInPrinciple = false;
            IsDependentOnWaterToExist = false;
        }   
        
        public override int NominalMaximumProfitThisTurn
        {
            get
            {
                // Don't charge accrued maintenance costs but just the minimum possible amount if houses are
                // built and sold as efficiently as possible.
                return RevenueThisTurn - ConstructionCost - ((StepsToBuild - 1) * MaintenanceCost);
            }
        } 
        
        public override int Profit
        {
            get
            {
                int achievablePrice = 0;
                
                if (IsBuilt)
                    achievablePrice += MarketPrice;
                
                return achievablePrice - ConstructionCost - AccruedMaintenanceCost;             
            }
        }             
    }
}