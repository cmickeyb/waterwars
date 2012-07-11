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
using WaterWars.Tests;
using WaterWars.Tests.Mock;

namespace WaterWars.Config.Tests
{ 
    [TestFixture]
    public class ConfigTests : AbstractGameTests
    {        
        /// <summary>
        /// Check that we've loaded crop configuration correctly
        /// </summary>
        [Test]
        public void TestCropConfigLoading()
        {
            TestHelper.InMethod();
            
            AddPlayers();
            StartGame();

            Assert.That(Crops.Template.ConstructionCosts[1], Is.EqualTo(50));
            Assert.That(Crops.Template.ConstructionCosts[2], Is.EqualTo(30));
            Assert.That(Crops.Template.ConstructionCosts[3], Is.EqualTo(80));
        }
        
        [Test]
        public void TestZoneLoading()
        {
            TestHelper.InMethod();
            
            AddPlayers();
            AddBuyPoints();
            
            bp1.Zone = "river";
            
            StartGame();            
            
            Assert.That(bp1.CombinedPrice, Is.EqualTo(1000));
            Assert.That(bp1.InitialWaterRights, Is.EqualTo(900));
            Assert.That(bp2.CombinedPrice, Is.EqualTo(350));
            Assert.That(bp2.InitialWaterRights, Is.EqualTo(1000));
        }
    }
}