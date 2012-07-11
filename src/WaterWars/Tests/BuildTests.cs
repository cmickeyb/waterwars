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
using Nini.Config;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using OpenMetaverse;
using OpenSim.Region.CoreModules.World.Land;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Tests.Common;
using WaterWars.Models;
using WaterWars.Models.Roles;
using WaterWars.Rules;
using WaterWars.Rules.Distributors;
using WaterWars.Rules.Generators;
using WaterWars.States;
using WaterWars.Tests.Mock;

namespace WaterWars.Tests
{
    /// <summary>
    /// Test actions that involve money
    /// </summary>
    [TestFixture]
    public class BuildTests : AbstractGameTests
    {        
        [Test]
        public void TestBuyLandRights()
        {
            TestHelper.InMethod();
            //log4net.Config.XmlConfigurator.Configure();

            AddBuyPoints();
            AddPlayers(Farmer.Singleton, Developer.Singleton);
            StartGame();                                                

            Assert.That(bp1.DevelopmentRightsOwner, Is.EqualTo(Player.None));
            Assert.That(bp1.WaterRightsOwner, Is.EqualTo(Player.None));
            Assert.That(bp1.HasAnyOwner, Is.False);
            Assert.That(bp1.Cropss.Count, Is.EqualTo(0));

            int p1StartMoney = p1.Money;
            
            m_controller.State.BuyLandRights(bp1, p1);

            Assert.That(bp1.DevelopmentRightsOwner, Is.EqualTo(p1));
            Assert.That(bp1.WaterRightsOwner, Is.EqualTo(p1));
            Assert.That(bp1.HasAnyOwner, Is.True);
            Assert.That(bp1.Cropss.Count, Is.EqualTo(0));
            
            Assert.That(p1.Money, Is.EqualTo(
                p1StartMoney 
              - bp1.CombinedPrice));            
        }

        [Test]
        public void TestBuyCrops()
        {
            TestHelper.InMethod();
            
            AddBuyPoints();
            AddPlayers(Farmer.Singleton, Developer.Singleton);
            StartGame();

            int p1StartMoney = p1.Money;

            m_controller.State.BuyLandRights(bp1, p1);
            
            Assert.That(p1.Money, Is.EqualTo(
                p1StartMoney 
              - bp1.CombinedPrice));
            Assert.That(bp1.Cropss.Count, Is.EqualTo(0));
            Assert.That(bp1.WaterRequired, Is.EqualTo(0));
            Assert.That(p1.WaterRequired, Is.EqualTo(0));
                        
            AddCrops();

            Assert.That(bp1.Cropss.Count, Is.EqualTo(1));
            Assert.That(bp1.WaterRequired, Is.EqualTo(c1.WaterUsage));
            Assert.That(p1.WaterRequired, Is.EqualTo(c1.WaterUsage));
            Assert.That(p1.Money, Is.EqualTo(
                p1StartMoney 
              - bp1.CombinedPrice
              - c1.ConstructionCost));            
        }        

        [Test]
        public void TestBuildHouses()
        {
            TestHelper.InMethod();
            
            AddBuyPoints();
            AddPlayers(Developer.Singleton, Manufacturer.Singleton);
            StartGame();

            int p1StartMoney = p1.Money;
            
            m_controller.State.BuyLandRights(bp1, p1);
            
            Assert.That(p1.Money, Is.EqualTo(
                p1StartMoney 
              - bp1.CombinedPrice));
            Assert.That(bp1.Housess.Count, Is.EqualTo(0));
            Assert.That(bp1.WaterRequired, Is.EqualTo(0));

            AddHouses();
            
            int expectedh1Cost = (int)Math.Ceiling(h1.ConstructionCost / (float)h1.StepsToBuild);               
            int expectedh2Cost = (int)Math.Ceiling(h2.ConstructionCost / (float)h2.StepsToBuild);               
            int expectedh3Cost = (int)Math.Ceiling(h3.ConstructionCost / (float)h3.StepsToBuild);               

            Assert.That(bp1.Housess.Count, Is.EqualTo(3));
            Assert.That(bp1.WaterRequired, Is.EqualTo(0));
            Assert.That(p1.WaterRequired, Is.EqualTo(0));
            Assert.That(p1.Money, Is.EqualTo(
                p1StartMoney 
              - bp1.CombinedPrice
              - expectedh1Cost - expectedh2Cost - expectedh3Cost));            
            
            // Move to next build phase
            EndTurns(2);
            
            expectedh1Cost = h1.ConstructionCostPerBuildStep;
            expectedh2Cost = h2.ConstructionCostPerBuildStep;
            expectedh3Cost = h3.ConstructionCostPerBuildStep;
            p1StartMoney = p1.Money;
            
            // Finish building houses
            m_controller.State.ContinueBuildingGameAsset(h1);
            m_controller.State.ContinueBuildingGameAsset(h2);
            m_controller.State.ContinueBuildingGameAsset(h3);

            Assert.That(p1.WaterRequired, Is.EqualTo(h1.WaterUsage + h2.WaterUsage + h3.WaterUsage));
            Assert.That(p1.Money, Is.EqualTo(p1StartMoney - expectedh1Cost - expectedh2Cost - expectedh3Cost));
        }

        /// <summary>
        /// Check that the player can start to build a house even if they currently don't have enough money to complete
        /// it.
        /// </summary>
        [Test]
        public void TestPartialBuildHouses()
        {
            TestHelper.InMethod();
            
            AddBuyPoints();
            AddPlayers(Developer.Singleton, Manufacturer.Singleton);
            StartGame();            
            
            m_controller.State.BuyLandRights(bp1, p1);            
            p1.Money = Houses.Template.ConstructionCostsPerBuildStep[1] + 10;
            
            h1 = (Houses)AddGameAsset(bp1, 0, Houses.Template, 1);
            
            // Move to next build phase
            EndTurns(2);
            
            bool exceptionThrown = false;
            try
            {
                m_controller.State.ContinueBuildingGameAsset(h1);
            }
            catch (WaterWarsGameLogicException)
            {
                exceptionThrown = true;
            }
            
            Assert.That(exceptionThrown);
        }
        
        [Test]
        /// <remarks>
        /// Currently, our test factories are single step builds.
        /// </remarks>
        public void TestBuildFactories()
        {
            TestHelper.InMethod();
            
            AddBuyPoints();
            AddPlayers(Manufacturer.Singleton);
            StartGame();
            
            m_controller.State.BuyLandRights(bp1, p1);
            AddFactories();
            
            Assert.That(p1.MaintenanceCosts, Is.EqualTo(f1.MaintenanceCost));
            Assert.That(p1.WaterRequired, Is.EqualTo(f1.WaterUsage));
        }        

        [Test]
        public void TestRemoveHouse()
        {
            TestHelper.InMethod();
            //log4net.Config.XmlConfigurator.Configure();
            
            AddBuyPoints();
            AddPlayers(Developer.Singleton, Farmer.Singleton);
            StartGame();
                                             
            m_controller.State.BuyLandRights(bp1, p1);

            AddHouses();           

            Assert.That(bp1.Housess.Count, Is.EqualTo(3));

            m_controller.State.RemoveGameAsset(h1);
            Assert.That(bp1.Housess.Count, Is.EqualTo(2));
            Assert.That(h1.Field, Is.EqualTo(Field.None));      
        }
        
        [Test]
        public void TestSellHouseToEconomy()
        {
            TestHelper.InMethod();
            //log4net.Config.XmlConfigurator.Configure();
            
            AddBuyPoints();
            AddPlayers(Developer.Singleton, Farmer.Singleton);
            StartGame();
                                             
            m_controller.State.BuyLandRights(bp1, p1);

            AddHouses();           

            Assert.That(bp1.Housess.Count, Is.EqualTo(3));            
            
            EndTurns(2);
            m_controller.State.ContinueBuildingGameAsset(h1);
            
            int p1Money = p1.Money;
            int p1WaterRights = p1.WaterEntitlement;                        
            
            m_controller.State.SellGameAssetToEconomy(h1);
            Assert.That(bp1.Housess.Count, Is.EqualTo(3));
            Assert.That(h1.Field.Owner, Is.EqualTo(m_controller.Game.Economy));
            Assert.That(p1.Money, Is.EqualTo(p1Money + h1.NormalRevenue));
            Assert.That(p1.WaterEntitlement, Is.EqualTo(p1WaterRights - h1.WaterUsage));
            
            // Check that we're not being charged maintenance costs for the sold houses            
            p1Money = p1.Money;
            EndTurns();
            EndTurns();                        
            
            Assert.That(p1.MaintenanceCosts, Is.EqualTo(h2.MaintenanceCost + h3.MaintenanceCost));
            Assert.That(p1.Money, Is.EqualTo(p1Money - h2.MaintenanceCost - h3.MaintenanceCost));
            Assert.That(p1.WaterRequired, Is.EqualTo(0));
        }

        [Test]
        public void TestUpgradeAsset()
        {
            TestHelper.InMethod();

            AddBuyPoints();
            AddPlayers(Manufacturer.Singleton, Farmer.Singleton);
            StartGame();

            m_controller.State.BuyLandRights(bp1, p1);

            AddFactories();

            int oldLevel = 2;
            int newLevel = 3;
            
            Assert.That(f1.Level, Is.EqualTo(oldLevel));
            int money = p1.Money;
            int upgradeCost = f1.ConstructionCosts[newLevel] - f1.ConstructionCosts[f1.Level];                      
            
            m_controller.State.UpgradeGameAsset(p1, f1, newLevel);
            Assert.That(f1.Level, Is.EqualTo(newLevel));
            Assert.That(f1.Name, Is.EqualTo(string.Format("{0} ({1})", f1.InitialNames[newLevel], f1.Field.Name)));
            Assert.That(f1.StepsToBuild, Is.EqualTo(f1.StepsToBuilds[newLevel]));
            // Steps built after upgrade undefined
            // StepBuiltThisTurn undefined
            Assert.That(f1.ConstructionCost, Is.EqualTo(f1.ConstructionCosts[newLevel]));            
            Assert.That(f1.ConstructionCostPerBuildStep, Is.EqualTo(f1.ConstructionCostsPerBuildStep[newLevel]));
            Assert.That(f1.NormalRevenue, Is.EqualTo(f1.NormalRevenues[newLevel]));
            Assert.That(
                f1.RevenueThisTurn, 
                Is.EqualTo(Math.Ceiling(m_controller.Game.EconomicActivity[f1.Type][f1.Level] * f1.NormalRevenue)));
            Assert.That(f1.WaterUsage, Is.EqualTo(f1.WaterUsages[newLevel]));            
            Assert.That(f1.MaintenanceCost, Is.EqualTo(f1.MaintenanceCosts[newLevel]));            
            Assert.That(f1.CanUpgrade, Is.False);
            Assert.That(f1.IsBuilt, Is.True);
            
            Assert.That(p1.Money, Is.EqualTo(money - upgradeCost));            
        }

        [Test]
        public void TestSellDevelopmentRights()
        {
            TestHelper.InMethod();
            //log4net.Config.XmlConfigurator.Configure();
            
            AddBuyPoints();
            AddPlayers(Farmer.Singleton, Manufacturer.Singleton);
            StartGame();

            m_controller.State.BuyLandRights(bp1, p1);

            // Put a factory on the parcel so that we can make sure these are automatically removed when the parcel
            // changes hands
            AddGameAsset(bp1, 0, Crops.Template, 1);

            // Fields should match the appropriate number for the player's role
            Assert.That(bp1.Fields.Count, Is.EqualTo(6));

            // Test selling of the water rights from p1 to p2
            int saleAmount = 456;
            int p1ProjectedMoney = p1.Money + saleAmount;
            int p2ProjectedMoney = p2.Money - saleAmount;            
            m_controller.State.SellRights(bp1, p2, RightsType.Water, saleAmount);            

            Assert.That(bp1.DevelopmentRightsOwner, Is.EqualTo(p1));
            Assert.That(bp1.WaterRightsOwner, Is.EqualTo(p2));
            Assert.That(p1.Money, Is.EqualTo(p1ProjectedMoney));
            Assert.That(p2.Money, Is.EqualTo(p2ProjectedMoney));
            Assert.That(bp1.Fields.Count, Is.EqualTo(6));

            // Subsequent selling of the development rights from p1 to p2 should be fine
            saleAmount = 768;
            p1ProjectedMoney += saleAmount;
            p2ProjectedMoney -= saleAmount;            
            m_controller.State.SellRights(bp1, p2, RightsType.Development, saleAmount);

            Assert.That(bp1.DevelopmentRightsOwner, Is.EqualTo(p2));
            Assert.That(bp1.WaterRightsOwner, Is.EqualTo(p2));            
            Assert.That(p1.Money, Is.EqualTo(p1ProjectedMoney));
            Assert.That(p2.Money, Is.EqualTo(p2ProjectedMoney));
            Assert.That(bp1.GameAssets.Count, Is.EqualTo(0));
            Assert.That(bp1.Fields.Count, Is.EqualTo(2));

            // Now p2 should be able to sell back the combined rights to p1
            saleAmount = 911;
            p1ProjectedMoney -= saleAmount;
            p2ProjectedMoney += saleAmount;
            m_controller.State.SellRights(bp1, p1, RightsType.Combined, saleAmount);

            Assert.That(bp1.DevelopmentRightsOwner, Is.EqualTo(p1));
            Assert.That(bp1.WaterRightsOwner, Is.EqualTo(p1));            
            Assert.That(p1.Money, Is.EqualTo(p1ProjectedMoney));
            Assert.That(p2.Money, Is.EqualTo(p2ProjectedMoney));
            Assert.That(bp1.GameAssets.Count, Is.EqualTo(0));
            Assert.That(bp1.Fields.Count, Is.EqualTo(6));
        }
        
        /// <summary>
        /// Test that trying to sell development rights on parcels which contain economy owned assets fails
        /// </summary>
        [Test]
        public void TestSellDevelopmentRightsAlreadyEconomyOwnership()
        {
            TestHelper.InMethod();
            //log4net.Config.XmlConfigurator.Configure();
            
            AddBuyPoints();
            AddPlayers(Developer.Singleton, Farmer.Singleton);
            StartGame();

            m_controller.State.BuyLandRights(bp1, p1);
            AddHouses(); 
            
            EndTurns(2);
            m_controller.State.ContinueBuildingGameAsset(h1);
            m_controller.State.SellGameAssetToEconomy(h1);

            bool exceptionThrown = false;
            try
            {
                m_controller.State.SellRights(bp1, p2, RightsType.Development, 2);
            }
            catch (WaterWarsGameLogicException)
            {
                exceptionThrown = true;
            }
            
            Assert.That(exceptionThrown, Is.True);            
            Assert.That(bp1.DevelopmentRightsOwner, Is.EqualTo(p1));
        }        
        
        [Test]
        public void TestSellWaterRights()
        {
            TestHelper.InMethod();
            //log4net.Config.XmlConfigurator.Configure();
            
            AddBuyPoints();
            AddPlayers(Farmer.Singleton, Manufacturer.Singleton);
            StartGame();

            m_controller.State.BuyLandRights(bp1, p1);

            // Test selling of the water rights from p1 to p2
            int price = 456;
            int amount = 21;
            int p1ProjectedMoney = p1.Money + price;
            int p2ProjectedMoney = p2.Money - price; 
            int p1ProjectedWaterRights = p1.WaterEntitlement - amount;
            int p2ProjectedWaterRights = p2.WaterEntitlement + amount;
            
            m_controller.State.SellWaterRights(p2, p1, amount, price);      
            
            Assert.That(p1.Money, Is.EqualTo(p1ProjectedMoney));
            Assert.That(p2.Money, Is.EqualTo(p2ProjectedMoney));
            Assert.That(p1.WaterEntitlement, Is.EqualTo(p1ProjectedWaterRights));
            Assert.That(p2.WaterEntitlement, Is.EqualTo(p2ProjectedWaterRights));                        
        }        
    }
}