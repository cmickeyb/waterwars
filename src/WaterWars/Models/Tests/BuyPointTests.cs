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
    public class BuyPointTests
    {
        [Test]
        public void TestChosenAssetType()
        { 
            TestHelpers.InMethod();            
            
            BuyPoint bp1 = new BuyPoint(UUID.Zero);
            Assert.That(bp1.ChosenGameAssetTemplate, Is.EqualTo(AbstractGameAsset.None));
            Assert.That(bp1.GameAssets.Count, Is.EqualTo(0));
            Assert.That(bp1.Factories.Count, Is.EqualTo(0));

            Factory f1 
                = new Factory("factory1", UUID.Parse("00000000-0000-0000-0000-000000000101"), Vector3.Zero);
            bp1.AddGameAsset(f1);

            Assert.That(bp1.ChosenGameAssetTemplate.Type, Is.EqualTo(AbstractGameAssetType.Factory));
            Assert.That(bp1.GameAssets.Count, Is.EqualTo(1));
            Assert.That(bp1.Factories.Count, Is.EqualTo(1));            

            Factory f2
                = new Factory("factory2", UUID.Parse("00000000-0000-0000-0000-000000000201"), Vector3.Zero);
            bp1.AddGameAsset(f2);

            Assert.That(bp1.ChosenGameAssetTemplate.Type, Is.EqualTo(AbstractGameAssetType.Factory));
            Assert.That(bp1.GameAssets.Count, Is.EqualTo(2));
            Assert.That(bp1.Factories.Count, Is.EqualTo(2));

            bool gotExpectedException = false;
            Houses h1 = new Houses("houses1", UUID.Parse("00000000-0000-0000-0000-000000001001"), Vector3.Zero);
            try
            {
                bp1.AddGameAsset(h1);
            }
            catch (Exception)
            {
                gotExpectedException = true;
            }
            
            Assert.That(gotExpectedException, Is.True);
            Assert.That(bp1.ChosenGameAssetTemplate.Type, Is.EqualTo(AbstractGameAssetType.Factory));
            Assert.That(bp1.GameAssets.Count, Is.EqualTo(2));
            Assert.That(bp1.Factories.Count, Is.EqualTo(2));

            gotExpectedException = false;
            try
            {
                bp1.RemoveGameAsset(h1);
            }
            catch (Exception)
            {
                gotExpectedException = true;
            }

            Assert.That(gotExpectedException, Is.True);
            Assert.That(bp1.ChosenGameAssetTemplate.Type, Is.EqualTo(AbstractGameAssetType.Factory));
            Assert.That(bp1.GameAssets.Count, Is.EqualTo(2));
            Assert.That(bp1.Factories.Count, Is.EqualTo(2));            

            bp1.RemoveGameAsset(f1);

            Assert.That(bp1.ChosenGameAssetTemplate.Type, Is.EqualTo(AbstractGameAssetType.Factory));
            Assert.That(bp1.GameAssets.Count, Is.EqualTo(1));
            Assert.That(bp1.Factories.Count, Is.EqualTo(1));

            bp1.RemoveGameAsset(f1);

            Assert.That(bp1.ChosenGameAssetTemplate.Type, Is.EqualTo(AbstractGameAssetType.Factory));
            Assert.That(bp1.GameAssets.Count, Is.EqualTo(1));
            Assert.That(bp1.Factories.Count, Is.EqualTo(1));            

            bp1.RemoveGameAsset(f2);

            Assert.That(bp1.ChosenGameAssetTemplate, Is.EqualTo(AbstractGameAsset.None));
            Assert.That(bp1.GameAssets.Count, Is.EqualTo(0));
            Assert.That(bp1.Factories.Count, Is.EqualTo(0));            
        }

        [Test]
        public void TestDevelopmentRightsOwner()
        {
            TestHelpers.InMethod();            
            
            BuyPoint bp1 = new BuyPoint(UUID.Zero);
            Player p1 = new Player("PLAYER 1", UUID.Parse("00000000-0000-0000-0000-000000000001"));
            Player p2 = new Player("PLAYER 2", UUID.Parse("00000000-0000-0000-0000-000000000002"));

            Assert.That(bp1.DevelopmentRightsOwner, Is.EqualTo(Player.None));
            Assert.That(p1.DevelopmentRightsOwned.Count, Is.EqualTo(0));

            bp1.DevelopmentRightsOwner = p1;

            Assert.That(bp1.DevelopmentRightsOwner, Is.EqualTo(p1));
            Assert.That(p1.DevelopmentRightsOwned.Count, Is.EqualTo(1));
            Assert.That(p1.DevelopmentRightsOwned[UUID.Zero], Is.EqualTo(bp1));

            bp1.DevelopmentRightsOwner = p2;

            Assert.That(bp1.DevelopmentRightsOwner, Is.EqualTo(p2));
            Assert.That(p1.DevelopmentRightsOwned.Count, Is.EqualTo(0));
            Assert.That(p2.DevelopmentRightsOwned.Count, Is.EqualTo(1));
            Assert.That(p2.DevelopmentRightsOwned[UUID.Zero], Is.EqualTo(bp1));            
        }

        [Test]
        public void TestWaterRightsOwner()
        {
            TestHelpers.InMethod();            
            
            BuyPoint bp1 = new BuyPoint(UUID.Zero);
            Player p1 = new Player("PLAYER 1", UUID.Parse("00000000-0000-0000-0000-000000000001"));
            Player p2 = new Player("PLAYER 2", UUID.Parse("00000000-0000-0000-0000-000000000002"));

            Assert.That(bp1.WaterRightsOwner, Is.EqualTo(Player.None));
            Assert.That(p1.WaterRightsOwned.Count, Is.EqualTo(0));

            bp1.WaterRightsOwner = p1;

            Assert.That(bp1.WaterRightsOwner, Is.EqualTo(p1));
            Assert.That(p1.WaterRightsOwned.Count, Is.EqualTo(1));
            Assert.That(p1.WaterRightsOwned[UUID.Zero], Is.EqualTo(bp1));

            bp1.WaterRightsOwner = p2;

            Assert.That(bp1.WaterRightsOwner, Is.EqualTo(p2));
            Assert.That(p1.WaterRightsOwned.Count, Is.EqualTo(0));
            Assert.That(p2.WaterRightsOwned.Count, Is.EqualTo(1));
            Assert.That(p2.WaterRightsOwned[UUID.Zero], Is.EqualTo(bp1));            
        }        
    }
}