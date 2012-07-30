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
    /// <summary>
    /// Test events
    /// </summary>
    [TestFixture]
    public class EventTests : AbstractGameTests
    {
        [Test]
        public void TestBuildToWaterStageEvents()
        {
            TestHelpers.InMethod();

            AddBuyPoints();
            AddPlayers(Manufacturer.Singleton);
            StartGame();

            m_controller.State.BuyLandRights(bp1, p1);
            Field field = bp1.Fields.Values.ToList()[1];
            AddFactories();            

            int p1ChangedEventTriggered = 0;
            int bp1ChangedEventTriggered = 0;
            int f1ChangedEventTriggered = 0;
            int fieldChangedEventTriggered = 0;
            
            p1.OnChange += delegate(AbstractModel am) { p1ChangedEventTriggered++; };
            bp1.OnChange += delegate(AbstractModel am) { bp1ChangedEventTriggered++; };
            f1.OnChange += delegate(AbstractModel am) { f1ChangedEventTriggered++; };
            field.OnChange += delegate(AbstractModel am) { fieldChangedEventTriggered++; };
            
            // Move to the water phase
            EndTurns();

            Assert.That(p1ChangedEventTriggered, Is.EqualTo(1));
            Assert.That(bp1ChangedEventTriggered, Is.EqualTo(1));
            Assert.That(f1ChangedEventTriggered, Is.EqualTo(1));
            Assert.That(fieldChangedEventTriggered, Is.EqualTo(1));
        }
        
        [Test]
        public void TestWaterToBuildStageEvents()
        {
            TestHelpers.InMethod();
            
            AddBuyPoints();
            AddPlayers(Manufacturer.Singleton);
            StartGame();
            
            m_controller.State.BuyLandRights(bp1, p1);
            Field field = bp1.Fields.Values.ToList()[1];            
            AddFactories();
            
            // Move to the water phase
            EndTurns();            

            int p1ChangedEventTriggered = 0;
            int bp1ChangedEventTriggered = 0;
            int f1ChangedEventTriggered = 0;
            int fieldChangedEventTriggered = 0;
            
            p1.OnChange += delegate(AbstractModel am) { p1ChangedEventTriggered++; };
            bp1.OnChange += delegate(AbstractModel am) { bp1ChangedEventTriggered++; };
            f1.OnChange += delegate(AbstractModel am) { f1ChangedEventTriggered++; };
            field.OnChange += delegate(AbstractModel am) { fieldChangedEventTriggered++; };
            
            // Move back to the build phase
            EndTurns();

            Assert.That(p1ChangedEventTriggered, Is.EqualTo(1));
            Assert.That(bp1ChangedEventTriggered, Is.EqualTo(1));
            Assert.That(f1ChangedEventTriggered, Is.EqualTo(1));
            Assert.That(fieldChangedEventTriggered, Is.EqualTo(1));
        }  
        
        [Test]
        public void TestWaterRightsSoldEvents()
        {
            TestHelpers.InMethod();
            
            AddBuyPoints();
            AddPlayers(Manufacturer.Singleton, Developer.Singleton);
            StartGame();            
            
            m_controller.State.BuyLandRights(bp1, p1);
            
            int p1ChangedEventTriggered = 0;
            int p2ChangedEventTriggered = 0;            
            
            p1.OnChange += delegate(AbstractModel model) { p1ChangedEventTriggered++; };
            p2.OnChange += delegate(AbstractModel model) { p2ChangedEventTriggered++; };
            
            m_controller.State.SellWaterRights(p2, p1, 23, 12);
            
            Assert.That(p1ChangedEventTriggered, Is.EqualTo(1));
            Assert.That(p2ChangedEventTriggered, Is.EqualTo(1));                                   
        }
    }
}