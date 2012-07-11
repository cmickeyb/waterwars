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
using System.Reflection;
using log4net;
using Nini.Config;
using WaterWars;
using WaterWars.Config;
using WaterWars.Models;

namespace WaterWars.Rules.Distributors
{
    /// <summary>
    /// A tiered water allocator.  Parcels where are unowned get water.
    /// </summary>
    /// Water is allocated from north to south.
    public class TieredWaterDistributor : AbstractWaterDistributor, IWaterDistributor
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public int ParcelWaterEntitlement { get; set; }

        /// <value>
        /// The range of each tier.
        /// </value>
        protected const int TIER_RANGE = 15;
        
        public Dictionary<Player, int> Allocate(int water, ICollection<Player> players, ICollection<BuyPoint> buyPoints)
        {
            Dictionary<Player, int> waterAllocated = new Dictionary<Player, int>();
            foreach (Player p in players)
                waterAllocated[p] = 0;
            
            if (buyPoints.Count == 0)
                return waterAllocated;
            
            // Allocate the rainfall by sorting the buy points into tiers.  The more northerly a buy point is, the
            // greater its y-coord
            List<BuyPoint> sortedBuyPoints = new List<BuyPoint>(buyPoints);
            sortedBuyPoints.Sort(
                delegate(BuyPoint p1, BuyPoint p2) 
                {
                    if (p1.Location.LocalPosition.Y < p2.Location.LocalPosition.Y)
                        return 1;
                    else if (p1.Location.LocalPosition.Y > p2.Location.LocalPosition.Y)
                        return -1;
                    else
                        return 0;
                });
            
            List<List<BuyPoint>> tieredBuyPoints = new List<List<BuyPoint>>();
            int tierBeginsIndex = 0, i = 1;

            m_log.InfoFormat(
                "[WATER WARS]: Processing parcel {0} at {1}", 
                tierBeginsIndex, sortedBuyPoints[tierBeginsIndex].Location.LocalPosition);
            for (; i < sortedBuyPoints.Count; i++)
            {
                m_log.InfoFormat("[WATER WARS]: Processing parcel {0} at {1}", i, sortedBuyPoints[i].Location.LocalPosition);
                
                if (!(sortedBuyPoints[tierBeginsIndex].Location.LocalPosition.Y - sortedBuyPoints[i].Location.LocalPosition.Y <= TIER_RANGE))
                {
                    tieredBuyPoints.Add(sortedBuyPoints.GetRange(tierBeginsIndex, i - tierBeginsIndex));
                    tierBeginsIndex = i;
                }                    
            }

            // Add the last tier
            tieredBuyPoints.Add(sortedBuyPoints.GetRange(tierBeginsIndex, sortedBuyPoints.Count - tierBeginsIndex));

            m_log.InfoFormat("[WATER WARS]: Found {0} tiers", tieredBuyPoints.Count);            

            bool waterExhausted = false;
            
            // Now allocate amongst the tiers
            for (i = 0; i < tieredBuyPoints.Count; i++)
            {
                List<BuyPoint> tier = tieredBuyPoints[i];
                int maxPossibleAllocationForEach = water / tier.Count;
                int allocationEach;

                if (maxPossibleAllocationForEach > ParcelWaterEntitlement)
                {
                    allocationEach = ParcelWaterEntitlement;
                }
                else
                {
                    allocationEach = maxPossibleAllocationForEach;
                    waterExhausted = true;
                }

                m_log.InfoFormat(
                    "[WATER WARS]: Allocating {0} water units to each parcel on tier {1}", allocationEach, i + 1);

                foreach (BuyPoint bp in tier)
                {
                    bp.WaterAvailable = allocationEach;
                    
                    if (bp.WaterRightsOwner != Player.None)
                        waterAllocated[bp.WaterRightsOwner] += allocationEach;
                    
                    water -= allocationEach;

                    if (bp.WaterRightsOwner != Player.None)
                        m_log.InfoFormat(
                            "[WATER WARS]: Player {0} now has {1} water units", 
                            bp.WaterRightsOwner.Name, bp.WaterRightsOwner.Water);
                }

                // Stop allocating if we've run out of water
                if (waterExhausted)
                    break;
            }
            
            // We won't bother trying to sort out whether some buypoints didn't actually change at this stage
            foreach (BuyPoint bp in buyPoints)
                bp.TriggerChanged();                    
            
            return waterAllocated;
        }

        public void UpdateConfiguration(IConfigSource configSource)
        {
            IConfig parcelsConfig = configSource.Configs[ConfigurationParser.PARCELS_SECTION_NAME];
            ParcelWaterEntitlement = parcelsConfig.GetInt(ConfigurationParser.PARCEL_WATER_ENTITLEMENT_KEY);            
        }        
    }
}