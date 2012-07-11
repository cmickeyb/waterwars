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

namespace WaterWars.States
{
    /// <summary>
    /// Calculate water allocation
    /// </summary>
    public class AllocationState : AbstractState
    {          
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        /// <value>
        /// Total rainfall this turn
        /// </value>
        public int RainfallTotal { get; private set; }         

        public AllocationState(WaterWarsController controller) : base(controller, GameStateType.Allocation) {}
    
        protected override void PostStartState()
        {
            // Perform economic activity
            Game.EconomicActivity = m_controller.EconomicGenerator.Generate(Game);
            
            lock (Game.GameAssets)
            {
                Dictionary<AbstractGameAsset, int> allocation 
                    = m_controller.EconomicDistributor.Allocate(Game.EconomicActivity, Game.GameAssets.Values);
                
                foreach (AbstractGameAsset ga in allocation.Keys)
                {
                    // Messy.  This will need to come out as a separate Dictionary of market prices from the allocator
                    // or perhaps another source (MarketPricer?)
                    if (ga.Type == AbstractGameAssetType.Houses)
                        ga.MarketPrice = allocation[ga];
                    else
                        ga.RevenueThisTurn = allocation[ga];           
                }
            }
            
            // Perform water activity
            RainfallTotal = m_controller.RainfallGenerator.Generate(Game);
                        
            m_controller.EventManager.TriggerWaterGenerated(RainfallTotal);
                        
            m_log.InfoFormat(
                "[WATER WARS]: Generated {0} over {1} parcels", 
                WaterWarsUtils.GetWaterUnitsText(RainfallTotal), Game.BuyPoints.Count);

            List<Player> players;
            lock (Game.Players)
                players = Game.Players.Values.ToList();
            
            Dictionary <Player, int> waterAllocated 
                = m_controller.WaterDistributor.Allocate(RainfallTotal, players, Game.BuyPoints.Values);
            
            foreach (Player p in waterAllocated.Keys)
            {
                p.WaterReceived = waterAllocated[p];
                p.Water += p.WaterReceived;            
                
                m_log.InfoFormat(
                    "[WATER WARS]: Allocated {0} to {1}", WaterWarsUtils.GetWaterUnitsText(p.WaterReceived), p.Name);
                
                m_controller.EventManager.TriggerWaterAllocated(p, p.WaterReceived);
                
                // We'll leave it up to the following Water State (or possibly Reset State) to trigger the general
                // player data change event
//                p.TriggerChanged();
            }         
            
            EndState(new WaterStageState(m_controller));
        }      
    }
}