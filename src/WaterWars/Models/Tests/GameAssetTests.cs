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
using NUnit.Framework;
using OpenMetaverse;
using OpenSim.Tests.Common;
using WaterWars.Models;

namespace WaterWars.Models.Tests
{ 
    [TestFixture]
    public class GameAssetTests
    {
        [Test]
        public void TestWaterAllocationToFullOnlyAsset()
        {
            TestHelpers.InMethod();            
            
            Houses h1 
                = new Houses("houses1", UUID.Parse("00000000-0000-0000-0000-000000001001"), Vector3.Zero)
                    { StepsToBuilds = new int[] { 0, 1 }, StepsBuilt = 1 };
            h1.WaterUsages = new int[] { 0, 4 };
            h1.Level = 1;

            Assert.That(h1.WaterAllocated, Is.EqualTo(0));
            h1.WaterAllocated = 4;
            Assert.That(h1.WaterAllocated, Is.EqualTo(4));
            h1.WaterAllocated = 0;
            Assert.That(h1.WaterAllocated, Is.EqualTo(0));

            bool gotNegativeException = false;
            try
            {
                h1.WaterAllocated = -1;
            }
            catch (WaterWarsGameLogicException)
            {
                gotNegativeException = true;
            }

            Assert.That(gotNegativeException, Is.True);
            Assert.That(h1.WaterAllocated, Is.EqualTo(0));

            // Over allocation should fail
            Houses h2 = new Houses("houses2", UUID.Parse("00000000-0000-0000-0000-000000001002"), Vector3.Zero)
                { StepsToBuilds = new int[] { 0, 1 }, StepsBuilt = 1 };
            h2.WaterUsages = new int[] { 0, 4 };
            h2.Level = 1;

            bool gotOverException = false;
            try
            {
                h2.WaterAllocated = 5;
            }
            catch (WaterWarsGameLogicException)
            {
                gotOverException = true;
            }

            Assert.That(gotOverException, Is.True);
            Assert.That(h2.WaterAllocated, Is.EqualTo(0));            

            // Partial allocation should fail
            Houses h3 = new Houses("houses3", UUID.Parse("00000000-0000-0000-0000-000000001003"), Vector3.Zero)
                { StepsToBuilds = new int[] { 0, 1 }, StepsBuilt = 1 };
            h3.WaterUsages = new int[] { 0, 4 };
            h3.Level = 1;

            bool gotPartialException = false;
            try
            {
                h3.WaterAllocated = 2;
            }
            catch (WaterWarsGameLogicException)
            {
                gotPartialException = true;
            }

            Assert.That(gotPartialException, Is.True);
            Assert.That(h3.WaterAllocated, Is.EqualTo(0));
        }

        [Test]
        public void TestWaterAllocationToPartialOkayAsset()
        {
            TestHelpers.InMethod();            
            
            Factory f1 
                = new Factory("factory1", UUID.Parse("00000000-0000-0000-0000-000000001001"), Vector3.Zero)
                    { StepsToBuilds = new int[] { 0, 1 }, StepsBuilt = 1 };
            f1.WaterUsages = new int[] { 0, 4 };
            f1.Level = 1;

            Assert.That(f1.WaterAllocated, Is.EqualTo(0));
            f1.WaterAllocated = 4;
            Assert.That(f1.WaterAllocated, Is.EqualTo(4));
            f1.WaterAllocated = 2;
            Assert.That(f1.WaterAllocated, Is.EqualTo(2));

            // Over allocation should fail
            bool gotOverException = false;
            try
            {
                f1.WaterAllocated = 5;
            }
            catch (WaterWarsGameLogicException)
            {
                gotOverException = true;
            }

            Assert.That(gotOverException, Is.True);
            Assert.That(f1.WaterAllocated, Is.EqualTo(2));
            
            f1.WaterAllocated = 0;
            Assert.That(f1.WaterAllocated, Is.EqualTo(0));

            bool gotNegativeException = false;
            try
            {
                f1.WaterAllocated = -1;
            }
            catch (WaterWarsGameLogicException)
            {
                gotNegativeException = true;
            }

            Assert.That(gotNegativeException, Is.True);
            Assert.That(f1.WaterAllocated, Is.EqualTo(0));
        }        
        
        /// <summary>
        /// Check that the null asset behaves as expected.
        /// </summary>
        /// 
        /// When the game starts up, the player selection will be for the null asset, so we need it to behave properly
        [Test]
        public void TestNoneAsset()
        {
            AbstractGameAsset ga = AbstractGameAsset.None;
            Assert.That(ga.BuyPointUuid, Is.EqualTo(UUID.Zero));
            Assert.That(ga.CanBeAllocatedWater, Is.False);
            Assert.That(ga.CanBeSoldToEconomy, Is.False);
            Assert.That(ga.CanUpgrade, Is.False);
            Assert.That(ga.CanUpgradeInPrinciple, Is.False);
            Assert.That(ga.ConstructionCost, Is.EqualTo(0));
            Assert.That(ga.ConstructionCostPerBuildStep, Is.EqualTo(0));
            Assert.That(ga.ConstructionCosts, Is.EqualTo(new int[] { 0 }));
            Assert.That(ga.ConstructionCostsPerBuildStep, Is.EqualTo(new int[] { 0 }));
            Assert.That(ga.Field, Is.Null);
            Assert.That(ga.Game, Is.EqualTo(Game.None));
            Assert.That(ga.InitialNames, Is.Null);
            Assert.That(ga.IsBuilt, Is.False);
            Assert.That(ga.Level, Is.EqualTo(0));
            Assert.That(ga.MaintenanceCost, Is.EqualTo(0));
            Assert.That(ga.MaintenanceCosts, Is.EqualTo(new int[] { 0 }));
            Assert.That(ga.MaxLevel, Is.EqualTo(0));
            Assert.That(ga.MinLevel, Is.EqualTo(0));
            Assert.That(ga.Name, Is.EqualTo("NONE"));
            Assert.That(ga.OwnerName, Is.EqualTo("UNKNOWN"));
            Assert.That(ga.OwnerUuid, Is.EqualTo(UUID.Zero));
            Assert.That(ga.Position, Is.EqualTo(Vector3.Zero));
            Assert.That(ga.NormalRevenue, Is.EqualTo(0));
            Assert.That(ga.NormalRevenues, Is.EqualTo(new int[] { 0 }));
            Assert.That(ga.TimeToLive, Is.EqualTo(AbstractGameAsset.INFINITE_TIME_TO_LIVE));
            Assert.That(ga.StepsBuilt, Is.EqualTo(0));
            Assert.That(ga.StepsToBuild, Is.EqualTo(0));
            Assert.That(ga.StepsToBuilds, Is.EqualTo(new int[] { 0 }));
            Assert.That(ga.Type, Is.EqualTo(AbstractGameAssetType.None));
            Assert.That(ga.UpgradeCosts, Is.EqualTo(new int[] { 0 }));
            Assert.That(ga.Uuid, Is.EqualTo(UUID.Zero));
            Assert.That(ga.WaterAllocated, Is.EqualTo(0));
            Assert.That(ga.WaterUsage, Is.EqualTo(0));
            Assert.That(ga.WaterUsages, Is.EqualTo(new int[] { 0 }));
        }
    }
}