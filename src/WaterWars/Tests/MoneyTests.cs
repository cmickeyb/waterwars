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
    public class MoneyTests : AbstractGameTests
    {        
        /// <summary>
        /// Test that we can get into a negative balance and that the player is stopped from performing certain actions
        /// </summary>
        [Test]
        public void TestNegativeBalance()
        {
            TestHelper.InMethod();
//            log4net.Config.XmlConfigurator.Configure();

            AddBuyPoints();
            AddPlayers(Manufacturer.Singleton, Manufacturer.Singleton);
            StartGame();

            int p1StartMoney = p1.Money;

            // FIXME: Really we should fix all the prices and costs here to guarantee that we go into negative territory
            m_controller.State.BuyLandRights(bp1, p1);
            m_controller.State.BuyLandRights(bp2, p1);
            Factory f1 = (Factory)AddGameAsset(bp1, 0, Factory.Template, 3);
            Factory f2 = (Factory)AddGameAsset(bp2, 0, Factory.Template, 1);
            Assert.That(bp1.Factories.Count, Is.EqualTo(1));

            // Should fail due to lack of funds
            Factory f3 = null;
            bool exceptionCaught = false;
            try
            {
                f3 = (Factory)AddGameAsset(bp1, 1, Factory.Template, 2);
            }
            catch (WaterWarsGameLogicException)
            {
                exceptionCaught = true;                
            }
            
            Assert.That(exceptionCaught, Is.True);
            Assert.That(p1.Money, Is.EqualTo(p1StartMoney - bp1.CombinedPrice - bp2.CombinedPrice - f1.ConstructionCost - f2.ConstructionCost));            
            Assert.That(f3, Is.Null);
            Assert.That(bp1.Factories.Count, Is.EqualTo(1));

            // Do enough to push the player into the red.  Going in to the red is allowed
            int i = 5;
            while (i-- > 0)
            {
                // Move to the water phase
                m_controller.State.EndTurn(p1.Uuid);
                m_controller.State.EndTurn(p2.Uuid);
    
                // Move back to the build phase
                m_controller.State.EndTurn(p1.Uuid);
                m_controller.State.EndTurn(p2.Uuid);
            }

            int expectedMoney 
                  = p1StartMoney
                  - bp1.CombinedPrice
                  - bp2.CombinedPrice
                  - f1.ConstructionCost 
                  - f2.ConstructionCost
                  - f1.MaintenanceCost * 5
                  - f2.MaintenanceCost * 5;
                    
            Assert.That(p1.Money, Is.EqualTo(expectedMoney));

            // Buying land should fail
            exceptionCaught = false;

            try
            {
                m_controller.State.BuyLandRights(bp2, p1);
            }
            catch (WaterWarsGameLogicException)
            {
                exceptionCaught = true;
            }

            Assert.That(exceptionCaught, Is.True);
            Assert.That(bp3.DevelopmentRightsOwner, Is.EqualTo(Player.None));
            Assert.That(p1.Money, Is.EqualTo(expectedMoney));

            // Upgrading an existing asset should fail
            bool exceptionThrown = false;
            
            try
            {
                m_controller.State.UpgradeGameAsset(p1, f2, 2);
            }
            catch (WaterWarsGameLogicException)
            {
                exceptionThrown = true;
            }
            
            Assert.That(exceptionThrown, Is.True);
            Assert.That(f2.Level, Is.EqualTo(1));
            Assert.That(p1.Money, Is.EqualTo(expectedMoney));                        
        }   
        
        [Test]
        public void TestLandOwningRevenue()
        {
            TestHelper.InMethod();
            //log4net.Config.XmlConfigurator.Configure();
            
            AddBuyPoints();
            AddPlayers(Developer.Singleton);
            StartGame();

            int p1PreMoney = p1.Money;

            int rightsCost = bp1.CombinedPrice;
            m_controller.State.BuyLandRights(bp1, p1);
            Assert.That(p1.Money, Is.EqualTo(p1PreMoney - rightsCost));

            // Ending the build phase shouldn't result in any revenue
            p1PreMoney = p1.Money;
            EndTurns();
            Assert.That(p1.Money, Is.EqualTo(p1PreMoney));

            // Simply owning land shouldn't result in any revenue when we exit the round
            p1PreMoney = p1.Money;
            EndTurns();
            Assert.That(p1.Money, Is.EqualTo(p1PreMoney));
        }

        [Test]
        public void TestUnwateredAssetRevenue()
        {
            TestHelper.InMethod();
            //log4net.Config.XmlConfigurator.Configure();
            
            AddBuyPoints();
            AddPlayers(Developer.Singleton);
            StartGame();

            m_controller.State.BuyLandRights(bp1, p1);
            Houses h1 = (Houses)AddGameAsset(bp1, 0, Houses.Template, 1);            

            // Condos cost money to maintain but unwatered don't earn.
            int p1PreMoney = p1.Money;
            EndTurns(2);
            Assert.That(p1.Money, Is.EqualTo(p1PreMoney - h1.MaintenanceCost));            
        }
        
        [Test]
        public void TestWateredAssetRevenue()
        {
            TestHelper.InMethod();
            //log4net.Config.XmlConfigurator.Configure();
            
            AddBuyPoints();
            AddPlayers(Manufacturer.Singleton);
            StartGame();

            m_controller.State.BuyLandRights(bp1, p1);
            AddFactories();
            EndTurns(1);
            
            m_controller.State.UseWater(f1, p1, f1.WaterUsage);
            int p1PreMoney = p1.Money;
            EndTurns(1);
            
            Assert.That(p1.Money, Is.EqualTo(p1PreMoney - f1.MaintenanceCost + f1.NormalRevenue));
        }   
        
        /// <summary>
        /// Test that a partially watered asset returns us the same partial proportion of income (for an applicable asset)
        /// </summary>
        [Test]
        public void TestPartiallyWateredAssetRevenue()
        {
            TestHelper.InMethod();
            //log4net.Config.XmlConfigurator.Configure();
            
            AddBuyPoints();
            AddPlayers(Manufacturer.Singleton);
            StartGame();

            m_controller.State.BuyLandRights(bp1, p1);
            Factory f1 = (Factory)AddGameAsset(bp1, 0, Factory.Template, 1);            
            EndTurns(1);
            
            m_controller.State.UseWater(f1, p1, (int)Math.Ceiling(f1.WaterUsage / (double)2));
            int p1PreMoney = p1.Money;
            EndTurns(1);
            
            Assert.That(p1.Money, Is.EqualTo(p1PreMoney - f1.MaintenanceCost + (int)Math.Ceiling(f1.NormalRevenue / (double)2)));
        }   
        
        [Test]
        public void TestProjectedHouseProfit()
        {
            TestHelper.InMethod();
            //log4net.Config.XmlConfigurator.Configure();
            
            AddBuyPoints();
            AddPlayers(Developer.Singleton);
            StartGame();

            m_controller.State.BuyLandRights(bp1, p1);
            Houses myH1 = (Houses)AddGameAsset(bp1, 0, Houses.Template, 1);            
            Assert.That(myH1.Profit, Is.EqualTo(-myH1.ConstructionCost - myH1.AccruedMaintenanceCost));
            EndTurns(2);
            
            m_controller.State.ContinueBuildingGameAsset(myH1);
            Assert.That(
                myH1.Profit, Is.EqualTo(myH1.MarketPrice - myH1.ConstructionCost - myH1.AccruedMaintenanceCost));
        }
        
        /// <summary>
        /// We shouldn't receive any operating revenue from houses.
        /// </summary>
        [Test]
        public void TestHouseOperatingRevenue()
        {
            TestHelper.InMethod();
            //log4net.Config.XmlConfigurator.Configure();
            
            AddBuyPoints();
            AddPlayers(Developer.Singleton);
            StartGame();

            m_controller.State.BuyLandRights(bp1, p1);
            Houses myH1 = (Houses)AddGameAsset(bp1, 0, Houses.Template, 1);            
            int p1PreMoney = p1.Money;            
            EndTurns(2);
            
            Assert.That(p1.Money, Is.EqualTo(p1PreMoney - myH1.MaintenanceCost));            
            
            m_controller.State.ContinueBuildingGameAsset(myH1);
            p1PreMoney = p1.Money;            
            EndTurns(2);
            
            Assert.That(p1.Money, Is.EqualTo(p1PreMoney - myH1.MaintenanceCost));            
            
            m_controller.State.SellGameAssetToEconomy(myH1);
            p1PreMoney = p1.Money;            
            EndTurns(2);
            
            Assert.That(p1.Money, Is.EqualTo(p1PreMoney));              
        }           
        
        [Test]
        public void TestCostOfLiving()
        {
            TestHelper.InMethod();
            //log4net.Config.XmlConfigurator.Configure();
            
            AddBuyPoints();
            AddPlayers(Manufacturer.Singleton);
            StartGame();            
            
            p1.CostOfLiving = 50;
            int p1PreMoney = p1.Money;
            EndTurns(2);
            
            Assert.That(p1.Money, Is.EqualTo(p1PreMoney - 50));
        }
        
        /// <summary>
        /// Test the maximum profit of crops that are around for only one turn.
        /// </summary>
        [Test]
        public void TestOneTurnMaximumCropsProfit()
        {
            TestHelper.InMethod();
            //log4net.Config.XmlConfigurator.Configure();
            
            AddBuyPoints();
            AddPlayers(Farmer.Singleton);
            StartGame();

            m_controller.State.BuyLandRights(bp1, p1);
            AddCrops();
            Assert.That(
                c1.NominalMaximumProfitThisTurn, 
                Is.EqualTo(c1.RevenueThisTurn - c1.ConstructionCost - c1.MaintenanceCost));
        } 
        
        /// <summary>
        /// Test the maximum profit of crops that are around for only one turn.
        /// </summary>
        [Test]
        public void TestPerennialMaximumCropsProfit()
        {
            TestHelper.InMethod();
            //log4net.Config.XmlConfigurator.Configure();
            
            AddBuyPoints();
            AddPlayers(Farmer.Singleton);
            StartGame();

            m_controller.State.BuyLandRights(bp1, p1);
            
            // Level 3 is perennial grapes
            Crops c1 = (Crops)AddGameAsset(bp1, 0, Crops.Template, 3);
            
            Assert.That(
                c1.NominalMaximumProfitThisTurn, 
                Is.EqualTo(c1.RevenueThisTurn - c1.MaintenanceCost));
            
            EndTurns(1);
            
            m_controller.State.UseWater(c1, p1, c1.WaterUsage);
            EndTurns(1);            
            
            Assert.That(
                c1.NominalMaximumProfitThisTurn, 
                Is.EqualTo(c1.RevenueThisTurn - c1.MaintenanceCost));            
        }        
    }
}