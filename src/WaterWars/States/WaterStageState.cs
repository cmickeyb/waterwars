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
using OpenMetaverse;
using WaterWars;
using WaterWars.Events;
using WaterWars.Models;
using WaterWars.Rules;
using WaterWars.Rules.Distributors;
using WaterWars.Rules.Generators;

namespace WaterWars.States
{
    /// <summary>
    /// The game is in play and in the water stage
    /// </summary>    
    public class WaterStageState : AbstractPlayState
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        const string FEED_RAINFALL_MSG = "Water phase starting.\nYou have been allocated {0} this turn";    
        const string SELL_WATER_CRAWL_MSG = "{0} sold {1} to {2}";     
        
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="controller"></param>
        public WaterStageState(WaterWarsController controller) : base(controller, GameStateType.Water) {}      

        protected override void StartState()
        {
            base.StartState();    
            
            // We do not provide a forecast for the next water phase during the current water phase.
            Game.Forecast.Economic = null;
            Game.Forecast.Water = "Available next build phase";            
         
            List<Player> players;
            lock (Game.Players)
                players = Game.Players.Values.ToList();
            
            foreach (Player p in players)
                m_controller.Events.Post(
                    p,
                    string.Format(FEED_RAINFALL_MSG, WaterWarsUtils.GetWaterUnitsText(p.WaterReceived)),
                    EventLevel.Alert);
                        
            UpdateAllStatus();
        }

        public override void EndStage()
        {
            base.EndStage();
            EndState(new RevenueState(m_controller));
        }

        public override void UseWater(AbstractGameAsset a, Player p, int amount)
        {            
            BuyPoint bp = a.Field.BuyPoint;
            
            if (bp.DevelopmentRightsOwner != p)
                throw new WaterWarsGameLogicException(
                    "{0} tried to allocate water on asset {1} but development rights are owned by {2}",
                    p, a, bp.DevelopmentRightsOwner);
            
            if (!a.CanBeAllocatedWater)
                throw new WaterWarsGameLogicException(
                    "{0} tried to allocate water on asset {1} but this asset type does not allow water allocation", 
                    p.Name, a.Name); 
            
            if (!a.IsBuilt)
                throw new WaterWarsGameLogicException(
                    "{0} tried to allocate water on asset {1} but it is only partially built", 
                    p.Name, a.Name);            
            
            m_log.InfoFormat(
                "[WATER WARS]: {0} using {1} on {2} at {3} in {4}", 
                p.Name, WaterWarsUtils.GetWaterUnitsText(amount), a.Name, bp.Name, bp.Location.RegionName);

            m_controller.WaterAllocator.ChangeAllocation(a, p, amount);
            m_controller.EventManager.TriggerWaterUsed(a, p, amount);
            a.TriggerChanged();
            bp.TriggerChanged();
            p.TriggerChanged();
            
            UpdateHudStatus(p);
        }

        public override void UndoUseWater(AbstractGameAsset a)
        {           
            if (!a.CanBeAllocatedWater)
                throw new WaterWarsGameLogicException(
                    string.Format(
                        "Attempt made to undo allocate water on asset {0} but this asset type does not allow water allocation", 
                        a.Name));
            
            if (!a.IsBuilt)
                throw new WaterWarsGameLogicException(
                    "{0} tried to undo allocate water on asset {1} but it is only partially built", 
                    a.OwnerName, a.Name);               
         
            Player p = a.Field.BuyPoint.DevelopmentRightsOwner;
            BuyPoint bp = a.Field.BuyPoint;
            
            m_log.InfoFormat(
                "[WATER WARS]: {0} undoing use of {1} on {2} at {3} in {4}", 
                p.Name, WaterWarsUtils.GetWaterUnitsText(a.WaterAllocated), a.Name, bp.Name, bp.Location.RegionName);
                        
            m_controller.WaterAllocator.ChangeAllocation(a, p, 0);
            m_controller.EventManager.TriggerWaterUsed(a, p, 0);
            a.TriggerChanged();
            bp.TriggerChanged();
            p.TriggerChanged();
            
            UpdateHudStatus(p);
        }

        public override void SellWater(Player seller, Player buyer, int water, int price) 
        {
            if (seller.Water < water)
                throw new WaterWarsGameLogicException(
                    string.Format(
                        "{0} only has {1} but is trying to sell {2} to {3}", 
                        seller, WaterWarsUtils.GetWaterUnitsText(seller.Water), WaterWarsUtils.GetWaterUnitsText(water), buyer));

            if (buyer.Money < price)
                throw new WaterWarsGameLogicException(
                    string.Format(
                        "{0} only has {1} but {2} is trying to sell them water for {3}",
                        buyer, buyer.Money, seller, price));

            m_log.InfoFormat(
                "[WATER WARS]: {0} selling {1} to {2} for {3}", 
                seller.Name, WaterWarsUtils.GetWaterUnitsText(water), buyer.Name, price);         
            
            seller.Money += price;
            buyer.Money -= price;
            buyer.WaterCostsThisTurn += price;
            seller.WaterRevenueThisTurn += price;
            seller.Water -= water;
            buyer.Water += water;            

            m_controller.EventManager.TriggerWaterSold(buyer, seller, water, price, true);          
            buyer.TriggerChanged();
            seller.TriggerChanged();
                       
            UpdateHudStatus(seller);
            UpdateHudStatus(buyer);

            m_controller.Events.PostToAll(
                string.Format(
                    SELL_WATER_CRAWL_MSG, seller.Name, WaterWarsUtils.GetWaterUnitsText(water), buyer.Name), 
                    EventLevel.Crawl);            
        }
    }
}