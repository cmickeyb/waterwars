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
    /// Test actions that involve player history records
    /// </summary>
    [TestFixture]
    public class HistoryTests : AbstractGameTests
    {        
        [Test]
        public void TestBuyLandRights()
        {
            TestHelpers.InMethod();
            //log4net.Config.XmlConfigurator.Configure();
            
            AddBuyPoints();
            AddPlayers(Manufacturer.Singleton);
            StartGame();

            m_controller.State.BuyLandRights(bp1, p1);
            EndTurns(2);
            
            Assert.That(p1.History[1].BuildRevenueThisTurn, Is.EqualTo(0));
            Assert.That(p1.History[1].WaterRevenueThisTurn, Is.EqualTo(0));
            Assert.That(p1.History[1].BuildCostsThisTurn, Is.EqualTo(0));
            Assert.That(p1.History[1].WaterCostsThisTurn, Is.EqualTo(0));
            Assert.That(p1.History[1].CostOfLiving, Is.EqualTo(p1.CostOfLiving));
            Assert.That(p1.History[1].ProjectedRevenueFromProducts, Is.EqualTo(0));
            Assert.That(p1.History[1].Profit, Is.EqualTo(-p1.CostOfLiving));         
        }
        
        [Test]
        public void TestSalesRevenue()
        {
            TestHelpers.InMethod();
            //log4net.Config.XmlConfigurator.Configure();
            
            AddBuyPoints();
            AddPlayers(Farmer.Singleton);
            StartGame();

            m_controller.State.BuyLandRights(bp1, p1);
            m_controller.State.BuyLandRights(bp2, p1);
            AbstractGameAsset c1 = m_controller.State.BuildGameAsset(bp1.Fields.Values.ToList()[0], Crops.Template, 1);
            EndTurns(1);
            
            m_controller.State.UseWater(c1, p1, c1.WaterUsage);
            EndTurns(1);
            
            Assert.That(p1.History[1].BuildRevenueThisTurn, Is.EqualTo(0));
            Assert.That(p1.History[1].WaterRevenueThisTurn, Is.EqualTo(0));
            Assert.That(p1.History[1].BuildCostsThisTurn, Is.EqualTo(c1.ConstructionCost));
            Assert.That(p1.History[1].WaterCostsThisTurn, Is.EqualTo(0));
            Assert.That(p1.History[1].CostOfLiving, Is.EqualTo(p1.CostOfLiving));
            Assert.That(p1.History[1].ProjectedRevenueFromProducts, Is.EqualTo(c1.ProjectedRevenue));
            Assert.That(p1.History[1].Profit, Is.EqualTo(c1.ProjectedRevenue - c1.ConstructionCost -p1.CostOfLiving));              
        }
    }
}