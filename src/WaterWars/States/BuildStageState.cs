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
using OpenSim.Region.Framework.Interfaces;
using WaterWars;
using WaterWars.Events;
using WaterWars.Models;
using WaterWars.Rules.Forecasters;

namespace WaterWars.States
{    
    /// <summary>
    /// The game is in play and in the build stage
    /// </summary>    
    public class BuildStageState : AbstractPlayState
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        public const string BUILD_PHASE_STARTING_MSG = "Build phase starting.";
        const string BUY_RIGHTS_CRAWL_MSG = "{0} bought rights for parcel {1} in {2}";
        const string SELL_RIGHTS_CRAWL_MSG = "{0} sold {1} rights on parcel {2} in {3} to {4}";
        const string SELL_WATER_RIGHTS_CRAWL_MSG = "{0} sold rights for {1} to {2}";

        // Spacing between game assets
        protected static Vector3 ASSET_SPACING = new Vector3(-5, 0, 0);        

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="controller"></param>
        public BuildStageState(WaterWarsController controller) : base(controller, GameStateType.Build) {}

        protected override void StartState()
        {
            base.StartState();
            
            Forecast forecast = new Forecast(); 
            forecast.Economic = m_controller.EconomicForecaster.Forecast(Game);
            forecast.Water = m_controller.Forecaster.Forecast(Game);
            Game.Forecast = forecast;
            
            Game.ForEachPlayer(delegate(Player p) { p.ResetForNextTurn(); });                        
            
            UpdateAllStatus();
        }
        
        public override void EndStage()
        {
            base.EndStage();
            EndState(new AllocationState(m_controller));
        }

        public override AbstractGameAsset BuildGameAsset(Field f, AbstractGameAsset templateAsset, int level)
        {
            BuyPoint bp = f.BuyPoint;
            Player p = bp.DevelopmentRightsOwner;                       
            
            if (!p.Role.AllowedAssets.Contains(templateAsset))
                throw new WaterWarsGameLogicException(
                    "[WATER WARS]: Player {0} tried to buy a {1} on {2} but this is not one of their allowed assets", 
                    p.Name, templateAsset.Type, bp.Name);                   

            AbstractGameAsset ga 
                = m_controller.ModelFactory.CreateGameAsset(f, templateAsset, Vector3.Zero, level);  
            
            int price = ga.ConstructionCostPerBuildStep;
            if (p.Money < price)
            {
                // TODO: Signal this to the player in-world in some way
                throw new WaterWarsGameLogicException(
                    "[WATER WARS]: Player {0} has {1}, not enough to starting building a {2} costing {3}",
                    p, p.Money, templateAsset.Type, price);
            }                 

            m_log.InfoFormat(
                "[WATER WARS]: Player {0} building a {1} on {2} in {3} for {4} (approx {5} each turn)", 
                p.Name, ga.Type, bp.Name, bp.Location.RegionName, ga.ConstructionCost, price);
            
            p.Money -= price;
            p.BuildCostsThisTurn += price;
            ga.StepsBuilt++;
            ga.StepBuiltThisTurn = true;
            
            // We have to remove the field from the buy point - it is now attached to the game asset if we want it back
            // later on.  This is pretty nasty - fields and game assets should probably occupy specific slots on the
            // BuyPoint now.
            // NO!  The buy point has to be kept here since this is actually the only way that the game asset can
            // retrieve the field.
//            f.BuyPoint = null;
            
            bp.AddGameAsset(ga);
            
            m_controller.EventManager.TriggerGameAssetBuildStarted(ga);
            if (ga.IsBuilt)
                m_controller.EventManager.TriggerGameAssetBuildCompleted(ga);
            
            p.TriggerChanged();            
            bp.TriggerChanged();
            
            // Don't trigger this change since we are deleting the field view
            //f.TriggerChanged();
            
            UpdateHudStatus(p);

            return ga;
        }        
        
        public override AbstractGameAsset ContinueBuildingGameAsset(AbstractGameAsset ga)
        {
            BuyPoint bp = ga.Field.BuyPoint;            
            
            if (ga.IsBuilt)
                throw new WaterWarsGameLogicException(
                    "{0} tried to continue building game asset {1} on {2} in {3} but this is already fully built",
                    bp.DevelopmentRightsOwner.Name, ga.Name, bp.Name, bp.Location.RegionName);                                    
            
            Player p = bp.DevelopmentRightsOwner; 
            
            int price = ga.ConstructionCostPerBuildStep;
            if (p.Money < price)
            {
                // TODO: Signal this to the player in-world in some way
                throw new WaterWarsGameLogicException(
                    "[WATER WARS]: Player {0} has {1}, not enough to build the next phase of {2} in {3} at {4} costing {5}",
                    p, p.Money, ga.Name, bp.Name, bp.Location.RegionName, price);
            }                
            
            p.Money -= price;  
            p.BuildCostsThisTurn += price;
            ga.StepsBuilt++;
            ga.StepBuiltThisTurn = true;
            
            m_controller.EventManager.TriggerGameAssetBuildContinued(ga);
            if (ga.IsBuilt)
                m_controller.EventManager.TriggerGameAssetBuildCompleted(ga);
            
            ga.TriggerChanged();
            
            p.TriggerChanged();            
            bp.TriggerChanged();
          
            UpdateHudStatus(p);            
            
            return ga;
        }

        public override void UpgradeGameAsset(Player p, AbstractGameAsset ga, int newLevel)
        {
            BuyPoint bp = ga.Field.BuyPoint;
            
            if (!ga.IsBuilt)
                throw new WaterWarsGameLogicException(
                    "{0} tried to upgrade {1} at {2} in {3} but the asset is only partially built", 
                    p.Name, ga.Name, bp.Name, bp.Location.RegionName);
            
            if (newLevel <= ga.Level)
                throw new WaterWarsGameLogicException(
                    "{0} tried to upgrade {1} to level {2} but asset is already at level {3}",
                    p, ga, newLevel, ga.Level);
            
            if (newLevel > ga.MaxLevel)
                throw new WaterWarsGameLogicException(
                    "{0} tried to upgrade {1} to level {2} but max level of asset is {3}", 
                    p, ga, newLevel, ga.MaxLevel);

            int price = ga.ConstructionCosts[newLevel] - ga.ConstructionCosts[ga.Level];

            if (p.Money < price)            
                throw new WaterWarsGameLogicException(
                    "{0} has {1}, not enough to upgrade {2} to level {3} which costs {4}",
                    p, p.Money, ga, newLevel, price);
            
            m_log.InfoFormat(
                "[WATER WARS]: {0} upgrading {1} on {2} in {3} to level {4}", 
                p.Name, ga.Name, bp.Name, bp.Location.RegionName, newLevel);

            // Perform the transaction
            p.Money -= price;
            int oldLevel = ga.Level;
            ga.Level = newLevel;
            ga.Name = string.Format("{0} ({1})", ga.InitialNames[ga.Level], ga.Field.Name);
            ga.RevenueThisTurn = m_controller.EconomicDistributor.Allocate(ga.Game.EconomicActivity, ga);            
            
            m_controller.EventManager.TriggerGameAssetUpgraded(ga, oldLevel);
            
            bp.TriggerChanged();
            p.TriggerChanged();
            ga.TriggerChanged();
                     
            UpdateHudStatus(p);        
        }             
        
        public override void SellGameAssetToEconomy(AbstractGameAsset ga)
        {
            if (!ga.CanBeSoldToEconomy)
                throw new WaterWarsGameLogicException(
                    "{0} tried to sell asset {1} on {2} in {3} to the economy but it is not of the appropriate type",
                    ga.OwnerName, ga.Name, ga.Field.BuyPoint.Name, ga.Field.BuyPoint.Location.RegionName);
            
            if (!ga.IsBuilt)
                throw new WaterWarsGameLogicException(
                    "{0} tried to sell asset {1} on {2} in {3} to the economy but it is only partially built", 
                    ga.OwnerName, ga.Name, ga.Field.BuyPoint.Name, ga.Field.BuyPoint.Location.RegionName);
            
            Player owner = ga.Field.Owner;
            BuyPoint bp = ga.Field.BuyPoint;
            
            if (ga.WaterUsage > owner.WaterEntitlement)
                throw new WaterWarsGameLogicException(
                    "Game asset {0} on parcel {1} in {2} cannot be sold to the market since this requires water rights of {3} whereas {4} has only {5} available",
                    ga.Name, bp.Name, bp.Location.RegionName, owner.Name, owner.WaterEntitlement);
            
            m_log.InfoFormat(
                "[WATER WARS]: {0} selling {1} on {2} in {3} to economy", owner.Name, ga.Name, bp.Name, bp.Location.RegionName);
            
            int revenue = ga.MarketPrice;
            owner.Money += revenue;
            owner.BuildRevenueThisTurn += revenue;
            owner.WaterEntitlement -= ga.WaterUsage;
            ga.IsSoldToEconomy = true;
            ga.Field.Owner = m_controller.Game.Economy;            
            
            ga.TriggerChanged();
            bp.TriggerChanged();
            owner.TriggerChanged();            
            
            m_controller.EventManager.TriggerGameAssetSoldToEconomy(ga, owner, revenue);
        }

        public override Field RemoveGameAsset(AbstractGameAsset ga)
        {
            Player owner = ga.Field.Owner;
            BuyPoint bp = ga.Field.BuyPoint;
            
            m_log.InfoFormat(
                "[WATER WARS]: {0} removing {1} on {2} in {3}", owner.Name, ga.Name, bp.Name, bp.Location.RegionName);
            
            // Player balance does not need to change since sold assets actually do not recoup any cash            
            bp.RemoveGameAsset(ga); 
            Field replacementField = m_controller.Dispatcher.RemoveGameAssetView(ga);                
            m_controller.EventManager.TriggerGameAssetRemoved(ga);
            bp.TriggerChanged();                        
            
            // Do this after events have been triggered so that listeners can still retrieve field details about
            // the asset removed
            ga.Field = Field.None;

            return replacementField;
        }
        
        public override void BuyLandRights(BuyPoint bp, Player buyer)
        {
            if (bp.DevelopmentRightsOwner != Player.None)
                throw new WaterWarsGameLogicException(
                    string.Format(                                  
                        "[WATER WARS]: {0} tried to buy parcel {1} but development rights are already owned by {2}", 
                        buyer, bp, bp.DevelopmentRightsOwner));

            if (bp.WaterRightsOwner != Player.None)
                throw new WaterWarsGameLogicException(
                    string.Format(  
                        "[WATER WARS]: {0} tried to buy parcel {1} but water rights are already owned by {2}", 
                        buyer, bp, bp.WaterRightsOwner));

            if (buyer.Money < bp.CombinedPrice)
                // TODO: Signal this to the player in-world in some way
                throw new WaterWarsGameLogicException(
                    string.Format("[WATER WARS]: Player {0} didn't have enough money to buy {1}", buyer, bp));

            m_log.InfoFormat("[WATER WARS]: Player {0} buying land rights for {1}", buyer, bp);
            
            buyer.Money -= bp.CombinedPrice;
            buyer.LandCostsThisTurn += bp.CombinedPrice;

            TransferAllRights(bp, buyer);
            
            buyer.TriggerChanged();
            bp.TriggerChanged();                       
            m_controller.EventManager.TriggerLandRightsBought(bp, buyer);

            // FIXME: This should be done via events
            UpdateHudStatus(buyer);

            m_controller.Events.PostToAll(
                string.Format(BUY_RIGHTS_CRAWL_MSG, buyer.Name, bp.Name, bp.Location.RegionName), 
                EventLevel.Crawl);            
        }                 

        public override void SellRights(BuyPoint bp, Player buyer, RightsType type, int salePrice)
        {                                                                                                     
            Player seller = Player.None;
            
            if (type == RightsType.Development || type == RightsType.Combined)
                seller = bp.DevelopmentRightsOwner;
            else
                seller = bp.WaterRightsOwner;            
            
            if (!bp.OwnerActions.ContainsKey("SellDevelopmentRights"))
                throw new WaterWarsGameLogicException(
                    "Player {0} tried to sell development rights on {1} in {2} but they have already sold game assets to the economy on this parcel",
                    seller.Name, bp.Name, bp.Location.RegionName);                  

            if (buyer.Money < salePrice)
                throw new WaterWarsGameLogicException(
                    "Player {0} tried to buy {1} rights for {2} from {3} for {4} but they only have {5}",
                    buyer, type, bp, seller, salePrice, buyer.Money);
                        
            m_log.InfoFormat(
                "[WATER WARS]: Player {0} selling {1} rights of {2} to {3} for {4}", 
                seller, type, bp, buyer, salePrice);
            
            // Perform the transaction
            if (type == RightsType.Water || type == RightsType.Combined)
                bp.WaterRightsOwner = buyer;

            buyer.Money -= salePrice;
            buyer.LandCostsThisTurn += salePrice;
            seller.Money += salePrice;
            seller.LandRevenueThisTurn += salePrice;

            // If we're selling development rights then we also need to remove any game assets already on the parcel
            if (type == RightsType.Development || type == RightsType.Combined)
            {               
                lock (bp.GameAssets)
                {
                    foreach (AbstractGameAsset asset in bp.GameAssets.Values)
                        m_controller.Dispatcher.RemoveGameAssetView(asset);
                }
                
                bp.RemoveAllGameAssets();                
            }

            if (type == RightsType.Development || type == RightsType.Combined)
                TransferDevelopmentRights(bp, buyer);            

            m_controller.EventManager.TriggerLandRightsSold(bp, buyer, seller, type, salePrice, true);
            buyer.TriggerChanged();
            seller.TriggerChanged();
            bp.TriggerChanged();
            
            // FIXME: Should be done via event subscription.
            UpdateHudStatus(buyer);
            UpdateHudStatus(seller);
                
            m_controller.Events.PostToAll(
                string.Format(SELL_RIGHTS_CRAWL_MSG, seller.Name, type, bp.Name, bp.Location.RegionName, buyer.Name), 
                EventLevel.Crawl);
        }
        
        public override void SellWaterRights(Player buyer, Player seller, int amount, int salePrice)
        {
            if (buyer.Money < salePrice)
                throw new WaterWarsGameLogicException(
                    string.Format(
                        "Player {0} tried to buy {1} water rights from {2} for {3} but they only have {4}",
                        buyer, amount, seller, salePrice, buyer.Money));            
            
            if (seller.WaterEntitlement < amount)
                throw new WaterWarsGameLogicException(
                    string.Format(
                        "Player {0} tried to sell {1} water rights to {2} but they only have {3}",
                        seller, amount, buyer, WaterWarsUtils.GetWaterUnitsText(seller.WaterEntitlement)));                          
            
            m_log.InfoFormat(
                "[WATER WARS]: Player {0} selling {1} water rights to {2} for {3}", 
                seller, amount, buyer, salePrice);            
            
            buyer.Money -= salePrice;
            buyer.WaterRightsCostsThisTurn += salePrice;
            seller.Money += salePrice;
            seller.WaterRightsRevenueThisTurn += salePrice;
            buyer.WaterEntitlement += amount;
            seller.WaterEntitlement -= amount;
            
            m_controller.EventManager.TriggerWaterRightsSold(buyer, seller, amount, salePrice, true);
            
            buyer.TriggerChanged();
            seller.TriggerChanged();
            
            // FIXME: Should be done via event subscription.
            UpdateHudStatus(buyer);
            UpdateHudStatus(seller);
                
            m_controller.Events.PostToAll(
                string.Format(SELL_WATER_RIGHTS_CRAWL_MSG, seller.Name, WaterWarsUtils.GetWaterUnitsText(amount), buyer.Name), 
                EventLevel.Crawl);         
        }     
    }
}