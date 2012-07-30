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
using System.Linq;
using Nini.Config;
using NUnit.Framework;
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
    [TestFixture]
    public class GameTests : AbstractGameTests
    {        
        [Test]
        public void TestRegisterBuyPoints()
        {
            TestHelpers.InMethod();            

            AddBuyPoints();
            
            Assert.That(m_controller.Game.BuyPoints.Count, Is.EqualTo(3));
        }
        
        [Test]
        public void TestAddPlayers()
        {
            TestHelpers.InMethod();
            
            AddPlayers();
            StartGame();
            
            Assert.That(m_controller.Game.Players.Count, Is.EqualTo(2));
            Assert.That(m_controller.State.Type, Is.EqualTo(GameStateType.Build));
        }

        [Test]
        /// <summary>
        /// Check that the game ends when it should.
        /// </summary>
        public void TestGameEnd()
        {
            TestHelpers.InMethod();

            Assert.That(m_controller.State is RegistrationState);
            
            AddPlayers();
            StartGame();            
            Assert.That(m_controller.State is BuildStageState);
            
            EndTurns(20);
            Assert.That(m_controller.State is GameEndedState);
            
            m_controller.State.ResetGame();
            Assert.That(m_controller.State is RegistrationState);
        }
		
		[Test]
		public void TestChangeBuyPointName()
		{
            TestHelpers.InMethod();
            //log4net.Config.XmlConfigurator.Configure();
            
            AddBuyPoints();
            AddPlayers(Developer.Singleton);
            StartGame();
			
			m_controller.State.BuyLandRights(bp1, p1);
            
            string newName = "bob";  
            m_controller.State.ChangeBuyPointName(bp1, newName);
            
            Assert.That(bp1.Name, Is.EqualTo(newName));
		}

        [Test]
        public void TestCropExpiry()
        {
            TestHelpers.InMethod();
            
            AddBuyPoints();
            AddPlayers(Farmer.Singleton, Farmer.Singleton);
            StartGame();

            m_controller.State.BuyLandRights(bp1, p1);

            Assert.That(bp1.GameAssets.Count, Is.EqualTo(0));
            Assert.That(bp1.Cropss.Count, Is.EqualTo(0));
            Assert.That(bp1.WaterRequired, Is.EqualTo(0));
            AddCrops();
            Assert.That(bp1.GameAssets.Count, Is.EqualTo(1));
            Assert.That(bp1.Cropss.Count, Is.EqualTo(1));
            Assert.That(bp1.WaterRequired, Is.EqualTo(c1.WaterUsage));
            
            // Move to the water phase and back to the build phase
            m_controller.State.EndTurn(p1.Uuid);
            m_controller.State.EndTurn(p2.Uuid);
            m_controller.State.EndTurn(p1.Uuid);
            m_controller.State.EndTurn(p2.Uuid);

            // Crops should have expired
            Assert.That(bp1.GameAssets.Count, Is.EqualTo(0));
            Assert.That(bp1.Cropss.Count, Is.EqualTo(0));
            Assert.That(bp1.WaterRequired, Is.EqualTo(0));
        }
        
        [Test]
        public void TestWaterDependentCropBehaviour()
        {
            TestHelpers.InMethod();
            
            AddBuyPoints();
            AddPlayers(Farmer.Singleton);
            StartGame();

            m_controller.State.BuyLandRights(bp1, p1);
            m_controller.State.BuyLandRights(bp2, p1);
            AbstractGameAsset c1 
                = m_controller.State.BuildGameAsset(bp1.Fields.Values.ToList()[0], Crops.Template, (int)CropType.Grapes);

            EndTurns();
            
            m_controller.State.UseWater(c1, p1, c1.WaterUsage);
            
            EndTurns();

            Assert.That(bp1.Cropss.Count, Is.EqualTo(1));
            
            // Switch from build to water and back again
            EndTurns(2);
            
            // Without watering, the crops should have died
            Assert.That(bp1.Cropss.Count, Is.EqualTo(0));
        }
        
        /// <summary>
        /// Test the player values that get set and reset each turn.
        /// </summary>
        [Test]
        public void TestPerTurnPlayerValues()
        {
            TestHelpers.InMethod();
            
            AddBuyPoints();
            AddPlayers(Farmer.Singleton, Developer.Singleton);
            StartGame();
            
            // Initial build stage
            int p1StartTurnMoney = p1.Money;
                        
            Assert.That(p1.StartTurnMoney, Is.EqualTo(p1StartTurnMoney));
            Assert.That(p1.MoneyReceivedThisTurn, Is.EqualTo(0));
            Assert.That(p1.MoneySpentThisTurn, Is.EqualTo(0));
            Assert.That(p1.WaterRevenueThisTurn, Is.EqualTo(0));
            Assert.That(p2.WaterReceived, Is.EqualTo(0));
            Assert.That(p2.WaterCostsThisTurn, Is.EqualTo(0));
            
            m_controller.State.BuyLandRights(bp1, p1);            
            
            Assert.That(p1.StartTurnMoney, Is.EqualTo(p1StartTurnMoney));
            Assert.That(p1.MoneySpentThisTurn, Is.EqualTo(bp1.CombinedPrice));
            
            // Switch to water stage
            EndTurns(1);
            
            Assert.That(p2.WaterReceived, Is.EqualTo(p2.WaterEntitlement));            
            
            int waterLeasePrice = 99;
            
            m_controller.State.SellWater(p1, p2, 10, waterLeasePrice);            
            
            Assert.That(p1.StartTurnMoney, Is.EqualTo(p1StartTurnMoney));
            Assert.That(p1.MoneyReceivedThisTurn, Is.EqualTo(waterLeasePrice));            
            Assert.That(p1.WaterRevenueThisTurn, Is.EqualTo(waterLeasePrice));
            Assert.That(p2.WaterCostsThisTurn, Is.EqualTo(waterLeasePrice));
            
            // Switch back to build stage
            EndTurns(1);
            p1StartTurnMoney = p1.Money;
            
            Assert.That(p1.StartTurnMoney, Is.EqualTo(p1StartTurnMoney));
            Assert.That(p1.MoneyReceivedThisTurn, Is.EqualTo(0));
            Assert.That(p1.MoneySpentThisTurn, Is.EqualTo(0));
            Assert.That(p1.WaterRevenueThisTurn, Is.EqualTo(0));
            Assert.That(p2.WaterReceived, Is.EqualTo(0));  
            Assert.That(p2.WaterCostsThisTurn, Is.EqualTo(0));
        }
        
        /// <summary>
        /// Test per turn build revenue for players
        /// </summary>
        /// <remarks>Done separately from TestPerTurnPlayerValues() to stop that test getting too complicated</remarks>
        [Test]
        public void TestPerTurnBuildRevenue()
        {
            TestHelpers.InMethod();
            
            AddBuyPoints();
            AddPlayers(Developer.Singleton);
            StartGame();            
            
            m_controller.State.BuyLandRights(bp1, p1);
            AddHouses();
            
            // Move back to next build phase
            EndTurns(2);
            
            Assert.That(p1.BuildRevenueThisTurn, Is.EqualTo(0));            
            
            m_controller.State.ContinueBuildingGameAsset(h1);
            
            int h1Revenue = h1.NormalRevenue;            
            
            m_controller.State.SellGameAssetToEconomy(h1);                        
            
            Assert.That(p1.BuildRevenueThisTurn, Is.EqualTo(h1Revenue));
            
            EndTurns(2);
            
            Assert.That(p1.BuildRevenueThisTurn, Is.EqualTo(0));
        }
    }
}