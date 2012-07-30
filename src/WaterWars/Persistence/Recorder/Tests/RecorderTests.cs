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
using System.IO;
using NUnit.Framework;
using OpenMetaverse;
using OpenSim.Services.Interfaces;
using OpenSim.Tests.Common;
using WaterWars.Models;
using WaterWars.Models.Roles;
using WaterWars.Persistence.Recorder;
using WaterWars.Rules.Allocators;
using WaterWars.Rules.Distributors;
using WaterWars.Rules.Forecasters;
using WaterWars.Rules.Generators;
using WaterWars.Rules.Economic.Distributors;
using WaterWars.Rules.Economic.Forecasters;
using WaterWars.Rules.Economic.Generators;
using WaterWars.Tests;

namespace WaterWars.Persistence.Recorder.Tests
{ 
    [TestFixture]
    public class RecorderTests : AbstractGameTests
    {
        /// <summary>
        /// The recorder destination used for each test.
        /// </summary>
        protected MemoryDestination m_recorderDestination;
        
        [SetUp]
        public override void SetUp()
        {
            m_recorderDestination = new MemoryDestination();
            base.SetUp();
        }
        
        protected override WaterWarsController CreateController()
        {
            UserAccount economyUserAccount 
                = new UserAccount() 
                    { PrincipalID = UUID.Parse("99999999-9999-9999-9999-999999999999"), FirstName = "The", LastName = "Economy" };
            
            return 
                new WaterWarsController(
                    new Persister(),
                    new Recorder(m_recorderDestination),
                    new SimpleForecaster(),
                    new SimpleUtopianRainfallGenerator(), 
                    new SimpleFairPerParcelWaterDistributor(), 
                    new ParcelOrientedAllocator(),
                    new SimpleEconomicForecaster(),
                    new SeriesEconomicGenerator(),                                        
                    new SimpleEconomicDistributor(),
                    economyUserAccount);            
        } 
        
        /// <summary>
        /// This test steps through as many actions as possible to look for recorder runtime failures.  It does not
        /// test the output except insofar as we test that anything has been generated at all.
        /// </summary>
        [Test]
        public void TestRecorder()
        {
            TestHelpers.InMethod();
            
            AddBuyPoints();
            AddPlayers(Manufacturer.Singleton, Developer.Singleton);
            StartGame();
            
            Assert.That(m_recorderDestination.Contents, Is.Not.EqualTo(""));
            
            // Build phase
            m_controller.State.BuyLandRights(bp1, p1);
            m_controller.State.BuyLandRights(bp2, p2);
            AddFactories(bp1);
            m_controller.State.UpgradeGameAsset(p1, f1, 3);                                               
            
            AddHouses(bp2);
            
            // Go through water and back to build, so that we can complete house building
            EndTurns(2);
            m_controller.State.ContinueBuildingGameAsset(h1);
            m_controller.State.SellGameAssetToEconomy(h1);
            EndTurns();
            
            // Water phase
            m_controller.State.UseWater(f1, p1, f1.WaterUsage);
            m_controller.State.UseWater(f1, p1, 0);
            m_controller.State.SellWater(p1, p2, 10, 15);
            EndTurns();
            
            // Build phase
            m_controller.State.RemoveGameAsset(f1);
            EndTurns();
            
            m_controller.State.ResetGame();
        }            
    }
}