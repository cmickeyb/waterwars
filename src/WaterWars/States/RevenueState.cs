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
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using WaterWars;
using WaterWars.Events;
using WaterWars.Models;
using WaterWars.Models.Roles;

namespace WaterWars.States
{
    /// <summary>
    /// Calculate revenue and reset values at the end of a turn
    /// </summary>
    public class RevenueState : AbstractState
    {          
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        const string GAME_ENDED_STATUS_MSG = "Game ended.  Thanks for playing!";
//        const string REVENUE_AND_COST_MSG = "This round you made {0} ({1} revenue - {2} costs, excluding {3} capital costs).";    
        const string REVENUE_AND_COST_MSG = "This round you made {0}.";
        
        public RevenueState(WaterWarsController controller) : base(controller, GameStateType.Revenue) {}
        
        protected override void PostStartState()
        {
            Dictionary<Player, int> eotRevenues = CalculateOperatingRevenue();
            Dictionary<Player, int> eotCosts = CalculateMaintenanceCosts();
            Dictionary<Player, string> eotMessages = new Dictionary<Player, string>();
            
            foreach (Player p in Game.Players.Values)
            {     
                // Right now we also want to reset all water the player has in hand.
                // TODO: This should be a separate rule
                p.Water = 0;
                
                p.Money += eotRevenues[p];
                p.Money -= eotCosts[p];                
                p.Money -= p.CostOfLiving;
                
                m_controller.EventManager.TriggerRevenueReceived(p, eotRevenues[p], eotCosts[p], p.CostOfLiving);
                
                p.RecordHistory();
                
                int revenue = 0, costs = 0, capitalRevenue = 0, capitalCosts = 0;
                
                // Common financial components
                revenue = p.WaterRevenueThisTurn;
                costs = p.MaintenanceCosts + p.WaterCostsThisTurn + p.CostOfLiving;
                capitalRevenue = p.LandRevenueThisTurn + p.WaterRightsRevenueThisTurn;
                capitalCosts = p.LandCostsThisTurn + p.WaterRightsCostsThisTurn; 
                
                if (p.Role.Type == RoleType.Farmer)
                {
                    revenue += p.ProjectedRevenueFromProducts;
                    costs += p.BuildCostsThisTurn;
                }
                else if (p.Role.Type == RoleType.Developer)
                {
                    revenue += p.BuildRevenueThisTurn;
                    costs += p.BuildCostsThisTurn;                   
                }
                else if (p.Role.Type == RoleType.Manufacturer)
                {
                    revenue += p.ProjectedRevenueFromProducts;
                    capitalCosts += p.BuildCostsThisTurn;         
                }
                
                int profit = revenue - costs;                
                string profitText = WaterWarsUtils.GetMoneyUnitsText(profit);
                string revenueText = WaterWarsUtils.GetMoneyUnitsText(revenue);
                string costsText = WaterWarsUtils.GetMoneyUnitsText(costs);
                string capitalRevenueText = WaterWarsUtils.GetMoneyUnitsText(capitalRevenue);
                string capitalCostsText = WaterWarsUtils.GetMoneyUnitsText(capitalCosts);
                
                m_log.InfoFormat(
                    "[WATER WARS]: {0} made {1} ({2} revenue - {3} costs) this turn, excluding {4} capital revenue, {5} capital costs", 
                    p.Name, profitText, revenueText, costsText, capitalRevenueText, capitalCostsText);
                
                string msg = string.Format(REVENUE_AND_COST_MSG, profitText) + "\n";
//                string msg = string.Format(
//                    REVENUE_AND_COST_MSG, profitText, revenueText, costsText, capitalCostsText) + "\n";                
                                
                if (m_controller.Game.IsLastRound)                    
                    msg += GAME_ENDED_STATUS_MSG;
                else
                    msg += BuildStageState.BUILD_PHASE_STARTING_MSG;
                
                eotMessages[p] = msg;
            }         
            
            // Do these actions after recording history so that project revenues (based on ga allocations) are correct
            List<AbstractGameAsset> assetsRemoved = AgeGameAssets();            
            
            foreach (Player p in Game.Players.Values)
                m_controller.Events.Post(p, eotMessages[p], EventLevel.All);                
            
            ResetPerTurnProperties();            
            
            m_controller.EventManager.TriggerRevenueStageEnded(assetsRemoved);
            
            if (!m_controller.RoundManager.EndRound())
            {
                m_controller.GameDateManager.AdvanceDate();                
                EndState(new BuildStageState(m_controller));
            }
            else
            {
                EndState(new GameEndedState(m_controller));
            }
        }  
                
        /// <summary>
        /// Age the assets in the game and remove this if they have expired or died.
        /// </summary>
        /// <returns>List of game assets removed</returns>
        protected List<AbstractGameAsset> AgeGameAssets()
        {
            m_log.InfoFormat("[WATER WARS]: Ageing game assets");
            
            List<AbstractGameAsset> assetsRemoved = new List<AbstractGameAsset>();
            
            foreach (BuyPoint bp in Game.BuyPoints.Values)
            {
                List<AbstractGameAsset> assetsToRemove = new List<AbstractGameAsset>();
                
                foreach (AbstractGameAsset a in bp.GameAssets.Values)
                {
                    bool removeAsset = false;
                    
                    if (a.IsDependentOnWaterToExist && a.WaterAllocated < a.WaterUsage)
                        removeAsset = true;
                    else if (a.TimeToLive != AbstractGameAsset.INFINITE_TIME_TO_LIVE && --a.TimeToLive == 0)
                        removeAsset = true;
                    
                    if (removeAsset)
                        assetsToRemove.Add(a);
                }
                
                //m_log.InfoFormat("[WATER WARS]: Removing {0} aged assets", assetsToRemove.Count);

                foreach (AbstractGameAsset a in assetsToRemove)
                {
                    bp.RemoveGameAsset(a);
                    
                    // Asynchronous deleting does not work well (only one asset is deleted!).  Probably a race
                    // condition.
                    //Util.FireAndForget(delegate { m_controller.Dispatcher.RemoveGameAssetView(a); });
//                    m_log.InfoFormat("[WATER WARS]: Deleting aged asset {0} in {1}", a.Name, bp.Location.RegionName);
                    m_controller.Dispatcher.RemoveGameAssetView(a);
                }
                
                assetsRemoved.AddRange(assetsToRemove);
            }            
            
            return assetsRemoved;
        }

        /// <summary>
        /// Reset per turn properties.
        /// </summary>
        protected void ResetPerTurnProperties()
        {
            foreach (BuyPoint bp in Game.BuyPoints.Values)
            {
                bp.WaterAvailable = 0;
                
                foreach (AbstractGameAsset ga in bp.GameAssets.Values)
                {
                    ga.StepBuiltThisTurn = false;                                             
                    ga.WaterAllocated = 0;                    
                }
            }
            
            // Player water per turn are reset separately by the caller.
        } 
        
        /// <summary>
        /// Calculate each player's operating revenue
        /// </summary>
        protected Dictionary<Player, int> CalculateOperatingRevenue()
        {
            Dictionary<Player, int> playerRevenue = new Dictionary<Player, int>();

            lock (Game.Players)
            {
                foreach (Player player in Game.Players.Values)
                    playerRevenue[player] = player.ProjectedRevenueFromProducts;
            }
            
            return playerRevenue;
        }

        /// <summary>
        /// Calculate each player's maintenance costs
        /// </summary>
        /// <remarks>
        /// This method also has the side effect of updating theaccrued maintenance costs.
        /// FIXME: We're not using Player.MaintenanceCosts because we want to accrue individual maintenance charges
        /// to game assets.  But duplication of the code is very poor.
        /// </remarks>
        /// <returns>
        /// Dictionary of the maintenance costs.  This has not yet been applied to the player.
        /// </returns>
        protected Dictionary<Player, int> CalculateMaintenanceCosts()
        {
            Dictionary<Player, int> playerCosts = new Dictionary<Player, int>();

            lock (Game.Players)
                foreach (Player player in Game.Players.Values)
                    playerCosts[player] = 0;
            
            foreach (BuyPoint bp in Game.BuyPoints.Values)
            {                
                Player player = bp.DevelopmentRightsOwner;

                if (player != Player.None)
                {
                    int costs = 0;

                    foreach (AbstractGameAsset a in bp.GameAssets.Values)
                    {
                        // Don't inflict maintenance costs for assets that have been sold to the economy
                        if (a.Field.Owner != player)
                            continue;                        
                        
                        int maintenanceCost = a.MaintenanceCost;
                        costs += maintenanceCost;
                        a.AccruedMaintenanceCost += maintenanceCost;
                    }
                    
                    playerCosts[player] += costs;
                }
            }
            
            return playerCosts;
        }        
    }
}