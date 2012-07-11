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
using WaterWars.Rules.Economic.Generators;
using WaterWars.States;
using WaterWars.Tests.Mock;

namespace WaterWars.Tests
{
    /// <summary>
    /// Water related game logic tests.
    /// </summary>
    [TestFixture]
    public class WaterTests : AbstractGameTests
    {       
        [Test]
        public void TestSellWater()
        {
            AddPlayers(Farmer.Singleton, Manufacturer.Singleton);
            StartGame();

            // Move to the water phase
            EndTurns();

            p1.Water = 1000;
            p2.Water = 900;
            
            int p1WaterBefore = p1.Water;
            int p2WaterBefore = p2.Water;            
            int p1MoneyBefore = p1.Money;
            int p2MoneyBefore = p2.Money;

            int water = 300;
            int price = 500;
            
            m_controller.State.SellWater(p1, p2, water, price);

            Assert.That(p1.Water, Is.EqualTo(p1WaterBefore - water));
            Assert.That(p2.Water, Is.EqualTo(p2WaterBefore + water));
            Assert.That(p1.Money, Is.EqualTo(p1MoneyBefore + price));
            Assert.That(p2.Money, Is.EqualTo(p2MoneyBefore - price));
        }
        
        /// <summary>
        /// Try selling more water than we actually have
        /// </summary>
        [Test]
        public void TestSellMoreWaterThanAvailable()
        {
            TestHelper.InMethod();
            //log4net.Config.XmlConfigurator.Configure();

            AddPlayers(Farmer.Singleton, Manufacturer.Singleton);
            StartGame();

            // Move to the water phase
            EndTurns();

            p1.Water = 200;
            p2.Water = 900;
            
            int p1WaterBefore = p1.Water;
            int p2WaterBefore = p2.Water;            
            int p1MoneyBefore = p1.Money;
            int p2MoneyBefore = p2.Money;

            int water = 300;
            int price = 500;
            
            bool gotException = false;
            try
            {            
                m_controller.State.SellWater(p1, p2, water, price);
            }
            catch (WaterWarsGameLogicException)
            {
                gotException = true;
            }                

            Assert.That(gotException, Is.True);
            
            Assert.That(p1.Water, Is.EqualTo(p1WaterBefore));
            Assert.That(p2.Water, Is.EqualTo(p2WaterBefore));
            Assert.That(p1.Money, Is.EqualTo(p1MoneyBefore));
            Assert.That(p2.Money, Is.EqualTo(p2MoneyBefore));
        }        

        [Test]
        public void TestWaterReceiptAndExpiry()
        {
            TestHelper.InMethod();
            //log4net.Config.XmlConfigurator.Configure();

            AddBuyPoints();
            AddPlayers(Farmer.Singleton, Manufacturer.Singleton);
            StartGame();

            m_controller.State.BuyLandRights(bp1, p1);
            
            Assert.That(p1.Water, Is.EqualTo(0));
            Assert.That(p2.Water, Is.EqualTo(0));

            // Move to water stage
            EndTurns();

            Assert.That(p1.Water, Is.EqualTo(1000));
            Assert.That(p2.Water, Is.EqualTo(0));

            // Move back to build stage
            EndTurns();

            Assert.That(p1.Water, Is.EqualTo(0));
            Assert.That(p2.Water, Is.EqualTo(0));            
        }
        
//        [Test]
//        public void TestSellWaterFromParcel()
//        {
//            TestHelper.InMethod();
//            //log4net.Config.XmlConfigurator.Configure();
//
//            AddBuyPoints();
//            AddPlayers(Farmer.Singleton, Manufacturer.Singleton);
//            StartGame();
//
//            m_controller.State.BuyLandRights(bp1, p1);
//            m_controller.State.BuyLandRights(bp2, p2);
//            m_controller.State.BuyLandRights(bp3, p2);
//            
//            // Move to the water phase
//            m_controller.State.EndTurn(p1.Uuid);
//            m_controller.State.EndTurn(p2.Uuid);
//            
//            int p1WaterBefore = p1.Water;
//            int p2WaterBefore = p2.Water;            
//            int p1MoneyBefore = p1.Money;
//            int p2MoneyBefore = p2.Money;
//
//            Assert.That(bp1.WaterAvailable, Is.EqualTo(1000));
//            Assert.That(bp2.WaterAvailable, Is.EqualTo(1000));
//            Assert.That(bp3.WaterAvailable, Is.EqualTo(1000));
//
//            int water = 300;
//            int price = 500;
//            
//            m_controller.State.SellWater(bp1, p1, p2, water, price);
//
//            Assert.That(bp1.WaterAvailable, Is.EqualTo(700));
//            Assert.That(bp2.WaterAvailable, Is.EqualTo(1150));
//            Assert.That(bp3.WaterAvailable, Is.EqualTo(1150));            
//
//            Assert.That(p1.Water, Is.EqualTo(p1WaterBefore - water));
//            Assert.That(p2.Water, Is.EqualTo(p2WaterBefore + water));
//            Assert.That(p1.Money, Is.EqualTo(p1MoneyBefore + price));
//            Assert.That(p2.Money, Is.EqualTo(p2MoneyBefore - price));
//        }        

//        [Test]
        /// <summary>
        /// Try selling more water than we actually have on the parcel
        /// </summary>
//        public void TestSellMoreWaterThanAvailableOnParcel()
//        {
//            TestHelper.InMethod();
//            //log4net.Config.XmlConfigurator.Configure();
//
//            AddBuyPoints();
//            AddPlayers(Farmer.Singleton, Manufacturer.Singleton);
//            StartGame();
//
//            m_controller.State.BuyLandRights(bp1, p1);
//            m_controller.State.BuyLandRights(bp2, p2);
//            m_controller.State.BuyLandRights(bp3, p2);
//            
//            // Move to the water phase
//            m_controller.State.EndTurn(p1.Uuid);
//            m_controller.State.EndTurn(p2.Uuid);
//            
//            int p1WaterBefore = p1.Water;
//            int p2WaterBefore = p2.Water;            
//            int p1MoneyBefore = p1.Money;
//            int p2MoneyBefore = p2.Money;
//
//            Assert.That(bp1.WaterAvailable, Is.EqualTo(1000));
//            Assert.That(bp2.WaterAvailable, Is.EqualTo(1000));
//            Assert.That(bp3.WaterAvailable, Is.EqualTo(1000));
//
//            int water = 1300;
//            int price = 500;
//
//            bool gotException = false;
//            try
//            {
//                m_controller.State.SellWater(bp1, p1, p2, water, price);
//            }
//            catch (WaterWarsGameLogicException)
//            {
//                gotException = true;
//            }
//
//            Assert.That(gotException, Is.True);
//
//            Assert.That(bp1.WaterAvailable, Is.EqualTo(1000));
//            Assert.That(bp2.WaterAvailable, Is.EqualTo(1000));
//            Assert.That(bp3.WaterAvailable, Is.EqualTo(1000));            
//
//            Assert.That(p1.Water, Is.EqualTo(p1WaterBefore));
//            Assert.That(p2.Water, Is.EqualTo(p2WaterBefore));
//            Assert.That(p1.Money, Is.EqualTo(p1MoneyBefore));
//            Assert.That(p2.Money, Is.EqualTo(p2MoneyBefore));
//        }

//        [Test]
        /// <summary>
        /// Try selling water to a player who has nowhere to store it
        /// </summary>
//        public void TestSellWaterBuyerHasNoRights()
//        {
//            TestHelper.InMethod();
//            //log4net.Config.XmlConfigurator.Configure();
//
//            AddBuyPoints();
//            AddPlayers(Farmer.Singleton, Manufacturer.Singleton);
//            StartGame();
//
//            m_controller.State.BuyLandRights(bp1, p1);
//            
//            // Move to the water phase
//            EndTurns();
//            
//            int p1WaterBefore = p1.Water;
//            int p2WaterBefore = p2.Water;            
//            int p1MoneyBefore = p1.Money;
//            int p2MoneyBefore = p2.Money;
//
//            Assert.That(p1.Water, Is.EqualTo(p1WaterBefore));
//            Assert.That(p2.Water, Is.EqualTo(p2WaterBefore));
//            Assert.That(p1.Money, Is.EqualTo(p1MoneyBefore));
//            Assert.That(p2.Money, Is.EqualTo(p2MoneyBefore));            
//
//            int water = 400;
//            int price = 500;
//
//            bool gotException = false;
//            try
//            {
//                m_controller.State.SellWater(bp1, p1, p2, water, price);
//            }
//            catch (WaterWarsGameLogicException)
//            {
//                gotException = true;
//            }
//
//            Assert.That(gotException, Is.True);
//
//            Assert.That(p1.Water, Is.EqualTo(p1WaterBefore));
//            Assert.That(p2.Water, Is.EqualTo(p2WaterBefore));
//            Assert.That(p1.Money, Is.EqualTo(p1MoneyBefore));
//            Assert.That(p2.Money, Is.EqualTo(p2MoneyBefore)); 
//        }
        
        [Test]
        public void TestSellMoreWaterRightsThanAvailable()
        {
            TestHelper.InMethod();
            //log4net.Config.XmlConfigurator.Configure();
            
            AddBuyPoints();
            AddPlayers(Farmer.Singleton, Manufacturer.Singleton);
            StartGame();

            m_controller.State.BuyLandRights(bp1, p1);

            // Test overselling of the water rights from p1 to p2
            int price = 456;
            int amount = p1.WaterEntitlement + 100;
            int p1ProjectedMoney = p1.Money;
            int p2ProjectedMoney = p2.Money; 
            int p1ProjectedWaterRights = p1.WaterEntitlement;
            int p2ProjectedWaterRights = p2.WaterEntitlement;
            
            bool exceptionThrown = false;            
            try
            {
                m_controller.State.SellWaterRights(p2, p1, amount, price);      
            }
            catch (WaterWarsGameLogicException)
            {
                exceptionThrown = true;
            }

            Assert.That(exceptionThrown, Is.True);
            Assert.That(p1.Money, Is.EqualTo(p1ProjectedMoney));
            Assert.That(p2.Money, Is.EqualTo(p2ProjectedMoney));
            Assert.That(p1.WaterEntitlement, Is.EqualTo(p1ProjectedWaterRights));
            Assert.That(p2.WaterEntitlement, Is.EqualTo(p2ProjectedWaterRights));                        
        }         
        
        [Test]
        public void TestUseWater()
        {
            TestHelper.InMethod();
            //log4net.Config.XmlConfigurator.Configure();                        
            
            AddBuyPoints();
            AddPlayers(Manufacturer.Singleton, Farmer.Singleton);
            StartGame();
   
            m_controller.State.BuyLandRights(bp1, p1);
            m_controller.State.BuyLandRights(bp2, p1);

            Assert.That(bp1.Factories.Count, Is.EqualTo(0));
            Assert.That(bp1.WaterRequired, Is.EqualTo(0));
                        
            // We also need to buy a factory to which we will give water
            AddGameAsset(bp1, 0, Factory.Template, 1);

            Assert.That(bp1.Factories.Count, Is.EqualTo(1));
            Factory f1 = bp1.Factories[0];
            
            Assert.That(bp1.WaterRequired, Is.EqualTo(f1.WaterUsage));

            // And one that we won't
            AddGameAsset(bp2, 1, Factory.Template, 1);
            
            SeriesEconomicGenerator econGen = (SeriesEconomicGenerator)m_controller.EconomicGenerator;        
            double thisRoundEconomicActivity = 1.2;
            econGen.Deviations[AbstractGameAssetType.Factory][f1.Level] = new double[] { 1, thisRoundEconomicActivity };

            // Move to the water phase
            EndTurns();

            Assert.That(f1.WaterAllocated, Is.EqualTo(0));
            Assert.That(bp1.WaterAllocated, Is.EqualTo(0));
            Assert.That(bp2.WaterAllocated, Is.EqualTo(0));
            Assert.That(p1.Water, Is.EqualTo(bp1.InitialWaterRights + bp2.InitialWaterRights));
            Assert.That(f1.ProjectedRevenue, Is.EqualTo(0));

            int p1WaterBefore = p1.Water;
            m_controller.State.UseWater(f1, p1, f1.WaterUsage);
            Assert.That(p1.Water, Is.EqualTo(p1WaterBefore - f1.WaterUsage));
            Assert.That(f1.WaterAllocated, Is.EqualTo(f1.WaterUsage));
            Assert.That(bp1.WaterAllocated, Is.EqualTo(bp1.WaterRequired));
            Assert.That(bp2.WaterAllocated, Is.EqualTo(0));
            Assert.That(f1.ProjectedRevenue, Is.EqualTo(f1.NormalRevenue * thisRoundEconomicActivity));
            
            // Move back to the build phase
            EndTurns();            

            Assert.That(bp1.WaterAllocated, Is.EqualTo(0));
            Assert.That(bp2.WaterAllocated, Is.EqualTo(0));            
            Assert.That(f1.WaterAllocated, Is.EqualTo(0));
            Assert.That(f1.ProjectedRevenue, Is.EqualTo(0));
        }   
        
        [Test]
        public void TestUseNegativeWater()
        {
            TestHelper.InMethod();
//            log4net.Config.XmlConfigurator.Configure();
            
            AddBuyPoints();
            AddPlayers(Farmer.Singleton);
            StartGame();
   
            m_controller.State.BuyLandRights(bp1, p1);
            AddCrops();

            // Move to the water phase
            EndTurns();
            
            bool exceptionThrown = false;
            
            try
            {               
                m_controller.State.UseWater(c1, p1, -1);
            }
            catch (WaterWarsGameLogicException)
            {
                exceptionThrown = true;
            }

            Assert.That(exceptionThrown, Is.True);
            Assert.That(c1.WaterAllocated, Is.EqualTo(0));
        }
        
        [Test]
        public void TestUseMoreWaterThanAvailable()
        {
            TestHelper.InMethod();
            //log4net.Config.XmlConfigurator.Configure();
            
            AddBuyPoints();
            AddPlayers(Manufacturer.Singleton);
            StartGame();
   
            m_controller.State.BuyLandRights(bp1, p1);
            AddGameAsset(bp1, 0, Factory.Template, 1);
            Factory f1 = bp1.Factories[0];

            // Move to the water phase
            EndTurns();

            int p1WaterBefore = p1.Water;
            
            bool exceptionThrown = false;            
            try
            {
                m_controller.State.UseWater(f1, p1, 999999);
            }
            catch (WaterWarsGameLogicException)
            {
                exceptionThrown = true;
            }
            
            Assert.That(exceptionThrown, Is.True);
            Assert.That(p1.Water, Is.EqualTo(p1WaterBefore));
            Assert.That(f1.WaterAllocated, Is.EqualTo(0));
            Assert.That(bp1.WaterAllocated, Is.EqualTo(0));
            Assert.That(bp2.WaterAllocated, Is.EqualTo(0));
        }         
    }
}