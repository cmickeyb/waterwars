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
using NUnit.Framework;
using OpenMetaverse;
using OpenSim.Tests.Common;
using WaterWars.Models;
using WaterWars.Models.Roles;
using WaterWars.Tests;

namespace WaterWars.Models.Tests
{ 
    /// <summary>
    /// Check that the model is offering the right action hints as the right times.
    /// </summary>    
    [TestFixture]
    public class ActionsTests : AbstractGameTests
    {
        [Test]
        public void TestPlayerActions()
        {
            TestHelpers.InMethod();
            
            AddBuyPoints();
            AddPlayers(Developer.Singleton, Farmer.Singleton);
            StartGame();
            
            Assert.That(p1.OwnerActions.ContainsKey("SellWaterRights"), Is.False);
            Assert.That(p1.OwnerActions.ContainsKey("SellWater"), Is.False);
            
            m_controller.State.BuyLandRights(bp1, p1);
            Assert.That(p1.OwnerActions.ContainsKey("SellWaterRights"), Is.True);
            Assert.That(p1.OwnerActions.ContainsKey("SellWater"), Is.False);
            
            EndTurns();
            
            Assert.That(p1.OwnerActions.ContainsKey("SellWaterRights"), Is.False);
            Assert.That(p1.OwnerActions.ContainsKey("SellWater"), Is.True);
            Assert.That(p2.OwnerActions.ContainsKey("SellWater"), Is.False);
            
            m_controller.State.SellWater(p1, p2, p1.Water, 2);
            Assert.That(p1.OwnerActions.ContainsKey("SellWater"), Is.False);
            Assert.That(p2.OwnerActions.ContainsKey("SellWater"), Is.True);
        }
        
        [Test]
        public void TestFieldActions()
        {
            TestHelpers.InMethod();
            
            AddBuyPoints();
            AddPlayers(Developer.Singleton, Manufacturer.Singleton);
            StartGame(); 
            
            m_controller.State.BuyLandRights(bp1, p1);
            
            Field field = bp1.Fields.Values.ToList()[0];                       
            p1.Money = Houses.Template.ConstructionCostsPerBuildStep[1] + 10;
            Assert.That(field.OwnerActions.ContainsKey("BuyAsset"), Is.True);
            
            p1.Money = Houses.Template.ConstructionCostsPerBuildStep[1] - 10;
            Assert.That(field.OwnerActions.ContainsKey("BuyAsset"), Is.False);
        }
        
        [Test]
        public void TestHousesActions()
        {
            TestHelpers.InMethod();
            
            AddBuyPoints();
            AddPlayers(Developer.Singleton, Manufacturer.Singleton);
            StartGame();
            
            m_controller.State.BuyLandRights(bp1, p1);
            
            AddHouses();
            
            Assert.That(h1.OwnerActions.ContainsKey("AllocateWater"), Is.False);
            Assert.That(h1.OwnerActions.ContainsKey("ContinueBuild"), Is.False);
            Assert.That(h1.OwnerActions.ContainsKey("CanUpgrade"   ), Is.False);
            Assert.That(h1.OwnerActions.ContainsKey("SellToEconomy"), Is.False);
            
            EndTurns();
            
            // Houses cannot be manually allocated water
            Assert.That(h1.OwnerActions.ContainsKey("AllocateWater"), Is.False);
            Assert.That(h1.OwnerActions.ContainsKey("UndoAllocateWater"), Is.False);             
            Assert.That(h1.OwnerActions.ContainsKey("ContinueBuild"), Is.False);
            Assert.That(h1.OwnerActions.ContainsKey("CanUpgrade"   ), Is.False);
            Assert.That(h1.OwnerActions.ContainsKey("SellToEconomy"), Is.False);
            
            EndTurns();
            
            int p1StartMoney = p1.Money;
            p1.Money = h1.ConstructionCostPerBuildStep - 10;
            Assert.That(h1.OwnerActions.ContainsKey("ContinueBuild"), Is.False);
            
            p1.Money = p1StartMoney;
            
            Assert.That(h1.OwnerActions.ContainsKey("AllocateWater"), Is.False);
            Assert.That(h1.OwnerActions.ContainsKey("ContinueBuild"), Is.True );
            Assert.That(h1.OwnerActions.ContainsKey("CanUpgrade"   ), Is.False);
            Assert.That(h1.OwnerActions.ContainsKey("SellToEconomy"), Is.False);
            
            m_controller.State.ContinueBuildingGameAsset(h1);
            Assert.That(h1.OwnerActions.ContainsKey("AllocateWater"), Is.False);
            Assert.That(h1.OwnerActions.ContainsKey("ContinueBuild"), Is.False);
            Assert.That(h1.OwnerActions.ContainsKey("CanUpgrade"   ), Is.False);
            Assert.That(h1.OwnerActions.ContainsKey("SellToEconomy"), Is.True );
        }
        
        [Test]
        public void TestFactoryActions()
        {
            TestHelpers.InMethod();
            
            AddBuyPoints();
            AddPlayers(Manufacturer.Singleton, Developer.Singleton);                                    
            StartGame();
            
            // Just for this test, we'll make the factory multi-build
            Factory.Template.StepsToBuilds = new int[] { 2 ,2, 2, 2 };            
            
            m_controller.State.BuyLandRights(bp1, p1);
            AddFactories();
            
            Assert.That(f1.OwnerActions.ContainsKey("AllocateWater"), Is.False);
            
            // Switch to water
            EndTurns();
            Assert.That(f1.OwnerActions.ContainsKey("AllocateWater"), Is.False);
            
            // Switch back to build
            EndTurns();            
            m_controller.State.ContinueBuildingGameAsset(f1);
            Assert.That(f1.OwnerActions.ContainsKey("AllocateWater"), Is.False);

            // Switch to water
            EndTurns();            
            Assert.That(f1.OwnerActions.ContainsKey("AllocateWater"), Is.True);            
        } 
        
        [Test]
        public void TestBuyPointActions()
        {
            TestHelpers.InMethod();
//            log4net.Config.XmlConfigurator.Configure();
            
            AddBuyPoints();
            AddPlayers(Developer.Singleton, Manufacturer.Singleton);
            StartGame();        
            
            // Check that rights can be purchased for unowned parcels in build phase
            Assert.That(bp1.NonOwnerActions.ContainsKey("BuyDevelopmentRights"), Is.True);
            Assert.That(bp1.NonOwnerActions.ContainsKey("BuyWaterRights"), Is.True);
            
            m_controller.State.BuyLandRights(bp1, p1);
            Assert.That(bp1.OwnerActions.ContainsKey("SellDevelopmentRights"), Is.True);
            
            // Check that rights cannot be purchased for owned parcels in build phase
            Assert.That(bp1.NonOwnerActions.ContainsKey("BuyDevelopmentRights"), Is.False);
            Assert.That(bp1.NonOwnerActions.ContainsKey("BuyWaterRights"), Is.False);            
            
            // Check that rights can still be sold even if developer owns houses on the parcel
            AddHouses();
            Assert.That(bp1.OwnerActions.ContainsKey("SellDevelopmentRights"), Is.True);
            
            // Check that rights cannot be sold in water phase
            EndTurns();           
            Assert.That(bp1.OwnerActions.ContainsKey("SellDevelopmentRights"), Is.False);
            
            // Check that rights can be purchased for parcels in water phase
            Assert.That(bp2.NonOwnerActions.ContainsKey("BuyDevelopmentRights"), Is.False);
            Assert.That(bp2.NonOwnerActions.ContainsKey("BuyWaterRights"), Is.False);            
            
            // Check that rights cannot be sold in build phase once developer has sold a house to the economy
            EndTurns();
            m_controller.State.ContinueBuildingGameAsset(h1);
            m_controller.State.SellGameAssetToEconomy(h1);
            
            Assert.That(bp1.OwnerActions.ContainsKey("SellDevelopmentRights"), Is.False);
        }
    }
}