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
    public class SimplePlayerOnlyWaterDistributorTests
    {
        protected SimplePlayerOnlyWaterDistributor distributor;

        [SetUp]
        public void SetUp()
        {
            distributor = new SimplePlayerOnlyWaterDistributor();            
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
            
            Player p1 = new Player("Alfred", UUID.Parse("00000000-0000-0000-0000-000000000001")) { WaterEntitlement = 500 };
            Player p2 = new Player("Betty", UUID.Parse("00000000-0000-0000-0000-000000000002")) { WaterEntitlement = 250 };
            List<Player> players = new List<Player> { p1, p2 };            

            Dictionary<Player, int> allocation = distributor.Allocate(375, players, null);

            Assert.That(allocation[p1], Is.EqualTo(250));
            Assert.That(allocation[p2], Is.EqualTo(125));
        }        
    }
}