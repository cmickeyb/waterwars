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
    public class SimpleFairPerParcelWaterDistributorTests
    {
        SimpleFairPerParcelWaterDistributor distributor;

        [SetUp]
        public void SetUp()
        {
            distributor = new SimpleFairPerParcelWaterDistributor();            
            distributor.Initialize(new WaterWarsEventManager());
        }

        /// <summary>
        /// Test logic where the water generated is below players' total entitlement
        /// </summary>
        [Test]
        public void TestGenerationBelowEntitlement()
        {
            TestHelpers.InMethod();
            //log4net.Config.XmlConfigurator.Configure();
            
            int parcelWaterEntitlement = 1000;

            Player p1 = new Player("Alfred", UUID.Parse("00000000-0000-0000-0000-000000000001"));
            Player p2 = new Player("Betty", UUID.Parse("00000000-0000-0000-0000-000000000002"));
            List<Player> players = new List<Player> { p1, p2 };            

            BuyPoint bp1 = new BuyPoint(UUID.Parse("00000000-0000-0000-0000-000000000011")) 
                { InitialWaterRights = parcelWaterEntitlement, WaterRightsOwner = p1 };
            bp1.Location.LocalPosition = new Vector3(10, 30, 10);
            
            BuyPoint bp2 = new BuyPoint(UUID.Parse("00000000-0000-0000-0000-000000000012")) 
                { InitialWaterRights = parcelWaterEntitlement, WaterRightsOwner = p2 };
            bp2.Location.LocalPosition = new Vector3(30, 30, 20);
            
            BuyPoint bp3 = new BuyPoint(UUID.Parse("00000000-0000-0000-0000-000000000013")) 
                { InitialWaterRights = parcelWaterEntitlement, WaterRightsOwner = p1 };
            bp3.Location.LocalPosition = new Vector3(50, 30, 20);
            
            BuyPoint bp4 = new BuyPoint(UUID.Parse("00000000-0000-0000-0000-000000000014"));
            bp4.Location.LocalPosition = new Vector3(52, 30, 30);
            
            List<BuyPoint> buyPoints = new List<BuyPoint> { bp1, bp2, bp3, bp4 };

            Dictionary<Player, int> allocation = distributor.Allocate(2999, players, buyPoints);

            int eachParcelGets = 999;           
            Assert.That(allocation[p1], Is.EqualTo(eachParcelGets * 2));
            Assert.That(allocation[p2], Is.EqualTo(eachParcelGets));

            Assert.That(bp1.WaterAvailable, Is.EqualTo(eachParcelGets));
            Assert.That(bp2.WaterAvailable, Is.EqualTo(eachParcelGets));
            Assert.That(bp3.WaterAvailable, Is.EqualTo(eachParcelGets));
            Assert.That(bp4.WaterAvailable, Is.EqualTo(0));
        }

        /// <summary>
        /// Test logic where the water generated is exactly players' total entitlement
        /// </summary>
        [Test]
        public void TestGenerationExactlyEntitlement()
        {
            TestHelpers.InMethod();
            //log4net.Config.XmlConfigurator.Configure();
            
            int parcelWaterEntitlement = 1000;

            Player p1 = new Player("Alfred", UUID.Parse("00000000-0000-0000-0000-000000000001"));
            Player p2 = new Player("Betty", UUID.Parse("00000000-0000-0000-0000-000000000002"));
            List<Player> players = new List<Player> { p1, p2 };            
            
            BuyPoint bp1 = new BuyPoint(UUID.Parse("00000000-0000-0000-0000-000000000011")) 
                { InitialWaterRights = parcelWaterEntitlement, WaterRightsOwner = p1 };
            bp1.Location.LocalPosition = new Vector3(10, 30, 10);
            
            BuyPoint bp2 = new BuyPoint(UUID.Parse("00000000-0000-0000-0000-000000000012")) 
                { InitialWaterRights = parcelWaterEntitlement, WaterRightsOwner = p2 };
            bp2.Location.LocalPosition = new Vector3(30, 30, 20);
            
            BuyPoint bp3 = new BuyPoint(UUID.Parse("00000000-0000-0000-0000-000000000013")) 
                { InitialWaterRights = parcelWaterEntitlement, WaterRightsOwner = p1 };
            bp3.Location.LocalPosition = new Vector3(50, 30, 20);
            
            BuyPoint bp4 = new BuyPoint(UUID.Parse("00000000-0000-0000-0000-000000000014"));
            bp4.Location.LocalPosition = new Vector3(52, 30, 30);            
           
            List<BuyPoint> buyPoints = new List<BuyPoint> { bp1, bp2, bp3, bp4 };

            Dictionary<Player, int> allocation = distributor.Allocate(3000, players, buyPoints);

            int eachParcelGets = parcelWaterEntitlement;           
            Assert.That(allocation[p1], Is.EqualTo(eachParcelGets * 2));
            Assert.That(allocation[p2], Is.EqualTo(eachParcelGets));

            Assert.That(bp1.WaterAvailable, Is.EqualTo(eachParcelGets));
            Assert.That(bp2.WaterAvailable, Is.EqualTo(eachParcelGets));
            Assert.That(bp3.WaterAvailable, Is.EqualTo(eachParcelGets));
            Assert.That(bp4.WaterAvailable, Is.EqualTo(0));            
        }

        /// <summary>
        /// Test logic where the water generated is above players' total entitlement
        /// </summary>
        [Test]
        public void TestGenerationAboveEntitlement()
        {
            TestHelpers.InMethod();
            //log4net.Config.XmlConfigurator.Configure();
            
            int parcelWaterEntitlement = 1000;

            Player p1 = new Player("Alfred", UUID.Parse("00000000-0000-0000-0000-000000000001"));
            Player p2 = new Player("Betty", UUID.Parse("00000000-0000-0000-0000-000000000002"));
            List<Player> players = new List<Player> { p1, p2 };       
            
            BuyPoint bp1 = new BuyPoint(UUID.Parse("00000000-0000-0000-0000-000000000011")) 
                { InitialWaterRights = parcelWaterEntitlement, WaterRightsOwner = p1 };
            bp1.Location.LocalPosition = new Vector3(10, 30, 10);
            
            BuyPoint bp2 = new BuyPoint(UUID.Parse("00000000-0000-0000-0000-000000000012")) 
                { InitialWaterRights = parcelWaterEntitlement, WaterRightsOwner = p2 };
            bp2.Location.LocalPosition = new Vector3(30, 30, 20);
            
            BuyPoint bp3 = new BuyPoint(UUID.Parse("00000000-0000-0000-0000-000000000013")) 
                { InitialWaterRights = parcelWaterEntitlement, WaterRightsOwner = p1 };
            bp3.Location.LocalPosition = new Vector3(50, 30, 20);
            
            BuyPoint bp4 = new BuyPoint(UUID.Parse("00000000-0000-0000-0000-000000000014"));
            bp4.Location.LocalPosition = new Vector3(52, 30, 30);            
         
            List<BuyPoint> buyPoints = new List<BuyPoint> { bp1, bp2, bp3, bp4 };

            Dictionary<Player, int> allocation = distributor.Allocate(4000, players, buyPoints);

            int eachParcelGets = parcelWaterEntitlement;           
            Assert.That(allocation[p1], Is.EqualTo(eachParcelGets * 2));
            Assert.That(allocation[p2], Is.EqualTo(eachParcelGets));

            Assert.That(bp1.WaterAvailable, Is.EqualTo(eachParcelGets));
            Assert.That(bp2.WaterAvailable, Is.EqualTo(eachParcelGets));
            Assert.That(bp3.WaterAvailable, Is.EqualTo(eachParcelGets));
            Assert.That(bp4.WaterAvailable, Is.EqualTo(0));            
        }        
    }
}