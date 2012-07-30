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

using System.Collections.Generic;
using NUnit.Framework;
using OpenMetaverse;
using OpenSim.Tests.Common;
using WaterWars.Models;
using WaterWars.Rules;

namespace WaterWars.Rules.Distributors.Tests
{ 
    [TestFixture]
    public class TieredWaterDistributorTests
    {
        TieredWaterDistributor distributor = new TieredWaterDistributor();
        public int TestParcelWaterEntitlement = 1000;

        [SetUp]
        public void SetUp()
        {
            distributor.Initialize(new WaterWarsEventManager());
            distributor.ParcelWaterEntitlement = TestParcelWaterEntitlement;
        }

        /// <summary>
        /// All parcels are on the same tier with not enough water to fully satisfy entitelements.
        /// </summary>
        [Test]
        public void TestOneTierUnderAllocation()
        {
            TestHelpers.InMethod();
            
            int water = 700;
            
            Player p1 = new Player("Alfred", UUID.Parse("00000000-0000-0000-0000-000000000001"));
            Player p2 = new Player("Betty", UUID.Parse("00000000-0000-0000-0000-000000000002"));
            List<Player> players = new List<Player> { p1, p2 };

            BuyPoint bp1 = new BuyPoint(UUID.Parse("00000000-0000-0000-0000-000000000010")) { WaterRightsOwner = p1 };
            bp1.Location.LocalPosition = new Vector3(10, 30, 10);
            
            BuyPoint bp2 = new BuyPoint(UUID.Parse("00000000-0000-0000-0000-000000000020")) { WaterRightsOwner = p2 };
            bp2.Location.LocalPosition = new Vector3(30, 30, 20);
            
            BuyPoint bp3 = new BuyPoint(UUID.Parse("00000000-0000-0000-0000-000000000030")) { WaterRightsOwner = p1 };
            bp3.Location.LocalPosition = new Vector3(50, 30, 20);
            
            BuyPoint bp4 = new BuyPoint(UUID.Parse("00000000-0000-0000-0000-000000000040"));
            bp4.Location.LocalPosition = new Vector3(52, 30, 30);
            
            List<BuyPoint> buyPoints = new List<BuyPoint> { bp1, bp2, bp3, bp4 };

            Dictionary<Player, int> allocation = distributor.Allocate(water, players, buyPoints);

            int eachParcelGets = water / 4;            
            Assert.That(allocation[p1], Is.EqualTo(eachParcelGets * 2));
            Assert.That(allocation[p2], Is.EqualTo(eachParcelGets));

            Assert.That(bp1.WaterAvailable, Is.EqualTo(eachParcelGets));
            Assert.That(bp2.WaterAvailable, Is.EqualTo(eachParcelGets));
            Assert.That(bp3.WaterAvailable, Is.EqualTo(eachParcelGets));
            Assert.That(bp4.WaterAvailable, Is.EqualTo(eachParcelGets));            
        }

        /// <summary>
        /// All parcels are on the same tier with more than enough water to fully satisfy entitlements.
        /// </summary>        
        [Test]
        public void TestOneTierOverAllocation()
        {
            TestHelpers.InMethod();
            
            int water = 5000;
            
            Player p1 = new Player("Alfred", UUID.Parse("00000000-0000-0000-0000-000000000001"));
            Player p2 = new Player("Betty", UUID.Parse("00000000-0000-0000-0000-000000000002"));
            List<Player> players = new List<Player> { p1, p2 };

            BuyPoint bp1 = new BuyPoint(UUID.Parse("00000000-0000-0000-0000-000000000010")) { WaterRightsOwner = p1 };
            bp1.Location.LocalPosition = new Vector3(10, 30, 10);
            
            BuyPoint bp2 = new BuyPoint(UUID.Parse("00000000-0000-0000-0000-000000000020")) { WaterRightsOwner = p2 };
            bp2.Location.LocalPosition = new Vector3(30, 30, 20);
            
            BuyPoint bp3 = new BuyPoint(UUID.Parse("00000000-0000-0000-0000-000000000030")) { WaterRightsOwner = p1 };
            bp3.Location.LocalPosition = new Vector3(50, 30, 20);
            
            BuyPoint bp4 = new BuyPoint(UUID.Parse("00000000-0000-0000-0000-000000000040"));
            bp4.Location.LocalPosition = new Vector3(52, 30, 30);
            
            List<BuyPoint> buyPoints = new List<BuyPoint> { bp1, bp2, bp3, bp4 };

            Dictionary<Player, int> allocation = distributor.Allocate(water, players, buyPoints);

            // Expecting full allocation of water
            Assert.That(allocation[p1], Is.EqualTo(TestParcelWaterEntitlement * 2));
            Assert.That(allocation[p2], Is.EqualTo(TestParcelWaterEntitlement));

            Assert.That(bp1.WaterAvailable, Is.EqualTo(TestParcelWaterEntitlement));
            Assert.That(bp2.WaterAvailable, Is.EqualTo(TestParcelWaterEntitlement));
            Assert.That(bp3.WaterAvailable, Is.EqualTo(TestParcelWaterEntitlement));
            Assert.That(bp4.WaterAvailable, Is.EqualTo(TestParcelWaterEntitlement));              
        }        

        /// <summary>
        /// Parcels are on two separate tiers with not enough water to go round
        /// </summary>
        [Test]
        public void TestMultiTiersUnderAllocation()
        {
            TestHelpers.InMethod();
            //log4net.Config.XmlConfigurator.Configure();
            
            int water = 2600;
            
            Player p1 = new Player("Alfred", UUID.Parse("00000000-0000-0000-0000-000000000001"));
            Player p2 = new Player("Betty", UUID.Parse("00000000-0000-0000-0000-000000000002"));
            List<Player> players = new List<Player> { p1, p2 };

            BuyPoint bp1_1 = new BuyPoint(UUID.Parse("00000000-0000-0000-0000-000000000010")) { WaterRightsOwner = p1 };
            bp1_1.Location.LocalPosition = new Vector3(10, 80, 10);            
            BuyPoint bp1_2 = new BuyPoint(UUID.Parse("00000000-0000-0000-0000-000000000020")) {  WaterRightsOwner = p2 };
            bp1_2.Location.LocalPosition = new Vector3(30, 70, 20);
            
            BuyPoint bp2_1 = new BuyPoint(UUID.Parse("00000000-0000-0000-0000-000000000030")) { WaterRightsOwner = p1 };
            bp2_1.Location.LocalPosition = new Vector3(05, 35, 20);
            BuyPoint bp2_2 = new BuyPoint(UUID.Parse("00000000-0000-0000-0000-000000000040")) { WaterRightsOwner = p2 };
            bp2_2.Location.LocalPosition = new Vector3(10, 25, 20);
            BuyPoint bp2_3 = new BuyPoint(UUID.Parse("00000000-0000-0000-0000-000000000050")) { WaterRightsOwner = p1 };
            bp2_3.Location.LocalPosition = new Vector3(30, 27, 20);
            
            List<BuyPoint> buyPoints = new List<BuyPoint> { bp1_1, bp1_2, bp2_1, bp2_2, bp2_3 };

            Dictionary<Player, int> allocation = distributor.Allocate(water, players, buyPoints);

            int firstTierParcelsGet = TestParcelWaterEntitlement;
            int secondTierParcelsGet = (water - TestParcelWaterEntitlement * 2) / 3;         
            Assert.That(allocation[p1], Is.EqualTo(firstTierParcelsGet + secondTierParcelsGet * 2));
            Assert.That(allocation[p2], Is.EqualTo(firstTierParcelsGet + secondTierParcelsGet));

            Assert.That(bp1_1.WaterAvailable, Is.EqualTo(firstTierParcelsGet));
            Assert.That(bp1_2.WaterAvailable, Is.EqualTo(firstTierParcelsGet));
            
            Assert.That(bp2_1.WaterAvailable, Is.EqualTo(secondTierParcelsGet));
            Assert.That(bp2_2.WaterAvailable, Is.EqualTo(secondTierParcelsGet));
            Assert.That(bp2_3.WaterAvailable, Is.EqualTo(secondTierParcelsGet));
        }        
    }
}