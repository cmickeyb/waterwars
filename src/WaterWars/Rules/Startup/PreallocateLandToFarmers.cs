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
using System.Linq;
using System.Reflection;
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using WaterWars;
using WaterWars.Events;
using WaterWars.Models;
using WaterWars.Models.Roles;
using WaterWars.States;

namespace WaterWars.Rules.Startup
{
    public class PreallocateLandToFarmers : IStartupRule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);        
        
        /// <summary>
        /// Maximum initial parcels to allocate to each farmer.
        /// </summary>
        protected const int MAXIMUM_RIVER_PARCEL_ALLOCATION = 3;
        
        protected WaterWarsController m_controller;               
        
        public PreallocateLandToFarmers(WaterWarsController controller)
        {
            m_controller = controller;
        }
        
        public void Execute(GameStartingState state)
        {
            List<Player> farmers = state.Game.Players.Values.Where(p => p.Role.Type == RoleType.Farmer).ToList();            
            List<BuyPoint> riverParcels = state.Game.BuyPoints.Values.Where(p => p.Zone == "river").ToList();
                        
            m_log.InfoFormat(
                "[WATER WARS]: Found {0} farmers and {1} river parcels", farmers.Count, riverParcels.Count);
            
            if (farmers.Count == 0)
            {
                m_log.InfoFormat("[WATER WARS]: Not allocating any farms since there are no farmers");
                return;
            }            
            
            riverParcels.Sort(
                delegate(BuyPoint bp1, BuyPoint bp2) 
                {
                    if (bp1.Location.RegionY > bp2.Location.RegionY)
                        return 1;
                    else if (bp1.Location.RegionY < bp2.Location.RegionY)
                        return -1;
                                    
                    if (bp1.Location.LocalPosition.Y > bp2.Location.LocalPosition.Y)
                        return 1;
                    else if (bp1.Location.LocalPosition.Y < bp2.Location.LocalPosition.Y)
                        return -1;
                               
                    if (bp1.Location.RegionX < bp2.Location.RegionX)
                        return 1;
                    else if (bp1.Location.RegionY > bp2.Location.RegionY)
                        return -1;
                
                    if (bp1.Location.LocalPosition.X < bp2.Location.LocalPosition.X)
                        return 1;
                    else if (bp1.Location.LocalPosition.X > bp2.Location.LocalPosition.X)
                        return -1;
                
                    return 0;
                });  
            riverParcels.Reverse();
            
            m_log.InfoFormat("[WATER WARS]: River parcels are in this order");
            string tableFormat = "{0,-20}  {1,-30}  {2,-6}  {3,-6}  {4,-20}";
            m_log.InfoFormat(tableFormat, "Parcel name", "Position", "Glob X", "Glob Y", "Region name");
            foreach (BuyPoint bp in riverParcels)
                m_log.InfoFormat(
                    tableFormat, 
                    bp.Name, bp.Location.LocalPosition, 
                    bp.Location.RegionX, bp.Location.RegionY, bp.Location.RegionName);

            int allocateToEach = Math.Min(riverParcels.Count / farmers.Count, MAXIMUM_RIVER_PARCEL_ALLOCATION);
            
            m_log.InfoFormat("[WATER WARS]: Allocating {0} river parcels to each farmer", allocateToEach);
            
            // If there are spare parcels, then allocate river parcels surrounding the middle of the region.
            int startAllocationIndex = (int)Math.Ceiling((riverParcels.Count - farmers.Count * allocateToEach) / 2.0);
            
            m_log.InfoFormat("[WATER WARS]: Starting allocation at river parcel index {0}", startAllocationIndex);
            
            foreach (Player farmer in farmers)
            {
                int toAllocate = allocateToEach;
                while (toAllocate > 0)
                {
                    BuyPoint riverParcel = riverParcels[startAllocationIndex];
                    
                    m_log.InfoFormat(
                        "[WATER WARS]: Allocating parcel {0} at {1} in {2} to {3}", 
                        riverParcel.Name, riverParcel.Location.LocalPosition, riverParcel.Location.RegionName, farmer.Name);
                    
                    state.AllocateParcel(riverParcel, farmer);
                    
                    toAllocate--;
                    startAllocationIndex++;
                }

                m_controller.Events.PostModalMessage(
                    farmer.Uuid, 
                    string.Format(
                        "As a farmer, you start the game already owning {0} parcels along the river.  In the environment, you can click on the farm buildings at each parcel to see which ones you own.", 
                        allocateToEach));                
            }
            
//            foreach (Player farmer in farmers)
//            {
//                int toAllocate = allocateToEach;
//                while (toAllocate > 0)
//                {
//                    BuyPoint riverParcel = riverParcels[WaterWarsUtils.Random.Next(0, riverParcels.Count)];
//                    if (riverParcel.HasAnyOwner)
//                        continue;
//                 
//                    m_log.InfoFormat(
//                        "[WATER WARS]: Allocating parcel {0} at {1} in {2} to {3}", 
//                        riverParcel.Name, riverParcel.Location.LocalPosition, riverParcel.Location.RegionName, farmer.Name);
//                    
//                    state.AllocateParcel(riverParcel, farmer);
//                    toAllocate--;
//                }
//                
//                m_controller.Events.PostModalMessage(
//                    farmer.Uuid, 
//                    string.Format(
//                        "As a farmer, you start the game already owning {0} parcels along the river.  In the environment, you can click on the farm buildings at each parcel to see which ones you own.", 
//                        allocateToEach));
//            }
        }
    }
}