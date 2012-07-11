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
using OpenMetaverse;
using WaterWars.Models;

namespace WaterWars.Rules.Allocators
{
    /// <summary>
    /// Water allocator that first allocates from the same parcel as an asset, then draws from other parcels in water-
    /// richest order.  Water draw isn't even - the richest parcel will have all its water exhausted first before moving
    /// on to the next one.  This is the simplest way to do it.
    /// </summary>    
    public class ParcelOrientedAllocator : IWaterAllocator
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        public void ChangeAllocation(AbstractGameAsset a, Player p, int allocation)
        {
            int adjustment = allocation - a.WaterAllocated;
            
            if (adjustment > p.Water)
                throw new WaterWarsGameLogicException(
                    string.Format(
                        "Player {0} only has {1} water, not enough to satisfy allocation adjustment of {2}",
                        p.Name, p.Water, adjustment));

            if (adjustment == 0)
                return;
            else if (adjustment > 0)
                TakeWater(a, p, adjustment);
            else
                GiveBackWater(a, p, -adjustment);
        }

        protected void TakeWater(AbstractGameAsset a, Player p, int waterRequired)
        {           
            m_log.InfoFormat(
                "[WATER WARS]: Allocating {0} water to {1} owned by {2}", 
                waterRequired, a.Name, a.Field.BuyPoint.DevelopmentRightsOwner);
            
            int totalWaterTaken = 0;
            
            if (a.Field.BuyPoint.WaterRightsOwner == p)
            {
                int waterTaken = Math.Min(a.Field.BuyPoint.WaterAvailable, waterRequired);            
                a.Field.BuyPoint.WaterAvailable -= waterTaken;
                waterRequired -= waterTaken;
                totalWaterTaken += waterTaken;
            }

            // We need to pull in water from other places.
            if (waterRequired > 0)
            {
                lock (p.WaterRightsOwned)
                {
                    Dictionary<UUID, BuyPoint> otherRights = new Dictionary<UUID, BuyPoint>(p.WaterRightsOwned);
                    otherRights.Remove(a.Field.BuyPoint.Uuid);
                    
                    while (waterRequired > 0)
                    {    
                        BuyPoint waterMaxBp = BuyPoint.None;
    
                        foreach (BuyPoint bp in otherRights.Values)
                        {
                            if (bp.WaterAvailable > waterMaxBp.WaterAvailable)
                                waterMaxBp = bp;
                        }
    
                        m_log.InfoFormat(
                            "[WATER WARS]: ParcelOrientedAllocator determined buy point {0} has max water of {1}", 
                            waterMaxBp.Name, waterMaxBp.WaterAvailable);

                        int waterTaken = Math.Min(waterMaxBp.WaterAvailable, waterRequired);                       
                        waterMaxBp.WaterAvailable -= waterTaken;
                        waterRequired -= waterTaken;                        
                        totalWaterTaken += waterTaken;
                    }
                }
            }

            a.WaterAllocated += totalWaterTaken;
            //p.Water -= a.WaterUsage;            
            a.TriggerChanged();
        }

        protected void GiveBackWater(AbstractGameAsset a, Player p, int waterToReturn)
        {
            m_log.InfoFormat(
                "[WATER WARS]: Deallocating {0} water from {1} owned by {2}", 
                waterToReturn, a.Name, a.Field.BuyPoint.DevelopmentRightsOwner);
            
            BuyPoint assetBp = a.Field.BuyPoint;

            // If the asset owner has no water rights anywhere then the water is simply lost.
            if (assetBp.DevelopmentRightsOwner.WaterRightsOwned.Count == 0)
                return;

            // If the asset sits in a parcel for which the owner also owns water rights, then just return the water
            // there
            if (assetBp.DevelopmentRightsOwner == assetBp.WaterRightsOwner)
            {
                assetBp.WaterAvailable += waterToReturn;
                a.WaterAllocated -= waterToReturn;
            }
            else
            {
                lock (p.WaterRightsOwned)
                {
                    int waterForEach = (int)Math.Ceiling(waterToReturn / (double)p.WaterRightsOwned.Count);
                    a.WaterAllocated -= waterToReturn;

                    foreach (BuyPoint bp in p.WaterRightsOwned.Values)
                    {
                        int water = Math.Min(waterForEach, waterToReturn);
                        bp.WaterAvailable += water;
                        waterToReturn -= water;

                        if (waterToReturn <= 0)
                            break;
                    }
                }
            }                            
            
            a.TriggerChanged();
        }
    }
}