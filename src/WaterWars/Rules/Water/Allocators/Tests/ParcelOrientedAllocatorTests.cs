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
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using OpenMetaverse;
using OpenSim.Tests.Common;
using WaterWars.Models;
using WaterWars.Rules;

namespace WaterWars.Rules.Allocators.Tests
{ 
    [TestFixture]
    public class ParcelOrientedAllocatorTests
    {
//        [Test]
        public void TestExactWaterOnParcel()
        {
            TestHelper.InMethod();
//            log4net.Config.XmlConfigurator.Configure();
            
            ParcelOrientedAllocator allocator = new ParcelOrientedAllocator();

            Player p1 = new Player("Alfred", UUID.Parse("00000000-0000-0000-0000-000000000001"));
            BuyPoint bp1 = new BuyPoint(UUID.Parse("00000000-0000-0000-0000-000000000010")) { WaterAvailable = 10, WaterRightsOwner = p1 };
            Field f1 = new Field(UUID.Parse("00000000-0000-0000-0001-000000000000"), "f1") { BuyPoint = bp1 };
            Houses h1 = new Houses("Houses", UUID.Parse("00000000-0000-0000-0010-000000000000"), Vector3.Zero, 1) { WaterUsage = 10, Field = f1 };
            bp1.AddGameAsset(h1);

            Assert.That(h1.WaterAllocated, Is.EqualTo(0));
            Assert.That(bp1.WaterAllocated, Is.EqualTo(0));
            Assert.That(bp1.WaterAvailable, Is.EqualTo(10));
            Assert.That(p1.Water, Is.EqualTo(10));

            allocator.ChangeAllocation(h1, p1, h1.WaterUsage);

            Assert.That(h1.WaterAllocated, Is.EqualTo(10));
            Assert.That(bp1.WaterAllocated, Is.EqualTo(10));
            Assert.That(bp1.WaterAvailable, Is.EqualTo(0));
            Assert.That(p1.Water, Is.EqualTo(0));
        }

//        [Test]
        public void TestExtraWaterOnParcel()
        {
            TestHelper.InMethod();
            //log4net.Config.XmlConfigurator.Configure();
            
            ParcelOrientedAllocator allocator = new ParcelOrientedAllocator();

            Player p1 = new Player("Alfred", UUID.Parse("00000000-0000-0000-0000-000000000001"));
            BuyPoint bp1 = new BuyPoint(UUID.Parse("00000000-0000-0000-0000-000000000010")) { WaterAvailable = 13, WaterRightsOwner = p1 };
            Field f1 = new Field(UUID.Parse("00000000-0000-0000-0001-000000000000"), "f1") { BuyPoint = bp1 };
            Houses h1 = new Houses("Houses", UUID.Parse("00000000-0000-0000-0010-000000000000"), Vector3.Zero, 1) { WaterUsage = 10, Field = f1 };
            bp1.AddGameAsset(h1);

            Assert.That(h1.WaterAllocated, Is.EqualTo(0));
            Assert.That(bp1.WaterAllocated, Is.EqualTo(0));
            Assert.That(bp1.WaterAvailable, Is.EqualTo(13));
            Assert.That(p1.Water, Is.EqualTo(13));

            allocator.ChangeAllocation(h1, p1, h1.WaterUsage);

            Assert.That(h1.WaterAllocated, Is.EqualTo(10));
            Assert.That(bp1.WaterAllocated, Is.EqualTo(10));
            Assert.That(bp1.WaterAvailable, Is.EqualTo(3));
            Assert.That(p1.Water, Is.EqualTo(3));
        }        

//        [Test]
        public void TestEnoughWaterWithAnotherParcel()
        {
            TestHelper.InMethod();
            //log4net.Config.XmlConfigurator.Configure();
            
            ParcelOrientedAllocator allocator = new ParcelOrientedAllocator();

            Player p1 = new Player("Alfred", UUID.Parse("00000000-0000-0000-0000-000000000001"));
            BuyPoint bp1 = new BuyPoint(UUID.Parse("00000000-0000-0000-0000-000000000010")) { WaterAvailable = 10, WaterRightsOwner = p1 };
            BuyPoint bp2 = new BuyPoint(UUID.Parse("00000000-0000-0000-0000-000000000020")) { WaterAvailable =  8, WaterRightsOwner = p1 };
            Field f1 = new Field(UUID.Parse("00000000-0000-0000-0001-000000000000"), "f1") { BuyPoint = bp1 };
            Houses h1 = new Houses("Houses", UUID.Parse("00000000-0000-0000-0010-000000000000"), Vector3.Zero, 1) { WaterUsage = 15, Field = f1 };
            bp1.AddGameAsset(h1);

            Assert.That(h1.WaterAllocated, Is.EqualTo(0));
            Assert.That(bp1.WaterAllocated, Is.EqualTo(0));
            Assert.That(bp1.WaterAvailable, Is.EqualTo(10));
            Assert.That(bp2.WaterAvailable, Is.EqualTo(8));
            Assert.That(p1.Water, Is.EqualTo(18));

            allocator.ChangeAllocation(h1, p1, h1.WaterUsage);

            Assert.That(h1.WaterAllocated, Is.EqualTo(15));
            Assert.That(bp1.WaterAllocated, Is.EqualTo(15));
            Assert.That(bp1.WaterAvailable, Is.EqualTo(0));
            Assert.That(bp2.WaterAvailable, Is.EqualTo(3));
            Assert.That(p1.Water, Is.EqualTo(3));            
        }

//        [Test]
        public void TestEnoughWaterWithMultipleOtherParcels()
        {
            TestHelper.InMethod();
            //log4net.Config.XmlConfigurator.Configure();
            
            ParcelOrientedAllocator allocator = new ParcelOrientedAllocator();

            Player p1 = new Player("Alfred", UUID.Parse("00000000-0000-0000-0000-000000000001"));
            BuyPoint bp1 = new BuyPoint(UUID.Parse("00000000-0000-0000-0000-000000000010")) { WaterAvailable = 10, WaterRightsOwner = p1 };
            BuyPoint bp2 = new BuyPoint(UUID.Parse("00000000-0000-0000-0000-000000000020")) { WaterAvailable =  8, WaterRightsOwner = p1 };
            BuyPoint bp3 = new BuyPoint(UUID.Parse("00000000-0000-0000-0000-000000000030")) { WaterAvailable =  4, WaterRightsOwner = p1 };
            Field f1 = new Field(UUID.Parse("00000000-0000-0000-0001-000000000000"), "f1") { BuyPoint = bp1 };
            Houses h1 = new Houses("Houses", UUID.Parse("00000000-0000-0000-0010-000000000000"), Vector3.Zero, 1) { WaterUsage = 20, Field = f1 };
            bp1.AddGameAsset(h1);

            Assert.That(h1.WaterAllocated, Is.EqualTo(0));
            Assert.That(bp1.WaterAllocated, Is.EqualTo(0));
            Assert.That(bp1.WaterAvailable, Is.EqualTo(10));
            Assert.That(bp2.WaterAvailable, Is.EqualTo(8));
            Assert.That(bp3.WaterAvailable, Is.EqualTo(4));
            Assert.That(p1.Water, Is.EqualTo(22));

            allocator.ChangeAllocation(h1, p1, h1.WaterUsage);

            Assert.That(h1.WaterAllocated, Is.EqualTo(20));
            Assert.That(bp1.WaterAllocated, Is.EqualTo(20));
            Assert.That(bp1.WaterAvailable, Is.EqualTo(0));
            Assert.That(bp2.WaterAvailable, Is.EqualTo(0));
            Assert.That(bp3.WaterAvailable, Is.EqualTo(2));
            Assert.That(p1.Water, Is.EqualTo(2));            
        }        

//        [Test]
        /// <summary>
        /// Test correct behaviour occurs when the asset owner does not own the sitting parcel water rights
        /// </summary>
        public void TestAssetParcelWaterRightsNotOwned()
        {
            TestHelper.InMethod();
            //log4net.Config.XmlConfigurator.Configure();
            
            ParcelOrientedAllocator allocator = new ParcelOrientedAllocator();

            Player p1 = new Player("Alfred", UUID.Parse("00000000-0000-0000-0000-000000000001"));
            BuyPoint bp1 = new BuyPoint(UUID.Parse("00000000-0000-0000-0000-000000000010")) { WaterAvailable = 10 };
            BuyPoint bp2 = new BuyPoint(UUID.Parse("00000000-0000-0000-0000-000000000020")) { WaterAvailable = 18, WaterRightsOwner = p1 };
            BuyPoint bp3 = new BuyPoint(UUID.Parse("00000000-0000-0000-0000-000000000030")) { WaterAvailable = 14, WaterRightsOwner = p1 };
            Field f1 = new Field(UUID.Parse("00000000-0000-0000-0001-000000000000"), "f1") { BuyPoint = bp1 };
            Houses h1 = new Houses("Houses", UUID.Parse("00000000-0000-0000-0010-000000000000"), Vector3.Zero, 1) { WaterUsage = 20, Field = f1 };
            bp1.AddGameAsset(h1);

            Assert.That(h1.WaterAllocated, Is.EqualTo(0));
            Assert.That(bp1.WaterAllocated, Is.EqualTo(0));
            Assert.That(bp1.WaterAvailable, Is.EqualTo(10));
            Assert.That(bp2.WaterAvailable, Is.EqualTo(18));
            Assert.That(bp3.WaterAvailable, Is.EqualTo(14));
            Assert.That(p1.Water, Is.EqualTo(32));

            allocator.ChangeAllocation(h1, p1, h1.WaterUsage);

            Assert.That(h1.WaterAllocated, Is.EqualTo(20));
            Assert.That(bp1.WaterAllocated, Is.EqualTo(20));
            Assert.That(bp1.WaterAvailable, Is.EqualTo(10));
            Assert.That(bp2.WaterAvailable, Is.EqualTo(0));
            Assert.That(bp3.WaterAvailable, Is.EqualTo(12));
            Assert.That(p1.Water, Is.EqualTo(12));            
        }
        
//        [Test]
        public void TestNotEnoughWaterOnParcel()
        {
            TestHelper.InMethod();
            //log4net.Config.XmlConfigurator.Configure();
            
            ParcelOrientedAllocator allocator = new ParcelOrientedAllocator();

            Player p1 = new Player("Alfred", UUID.Parse("00000000-0000-0000-0000-000000000001"));
            BuyPoint bp1 = new BuyPoint(UUID.Parse("00000000-0000-0000-0000-000000000010")) { WaterAvailable = 8, WaterRightsOwner = p1 };
            Field f1 = new Field(UUID.Parse("00000000-0000-0000-0001-000000000000"), "f1") { BuyPoint = bp1 };
            Houses h1 = new Houses("Houses", UUID.Parse("00000000-0000-0000-0010-000000000000"), Vector3.Zero, 1) { WaterUsage = 10, Field = f1 };
            bp1.AddGameAsset(h1);

            Assert.That(h1.WaterAllocated, Is.EqualTo(0));
            Assert.That(bp1.WaterAllocated, Is.EqualTo(0));
            Assert.That(bp1.WaterAvailable, Is.EqualTo(8));
            Assert.That(p1.Water, Is.EqualTo(8));

            bool exceptionThrown = false;
            try
            {
                allocator.ChangeAllocation(h1, p1, h1.WaterUsage);
            }
            catch (WaterWarsGameLogicException)
            {
                exceptionThrown = true;
            }

            Assert.That(exceptionThrown, Is.True);
            
            Assert.That(h1.WaterAllocated, Is.EqualTo(0));
            Assert.That(bp1.WaterAllocated, Is.EqualTo(0));
            Assert.That(bp1.WaterAvailable, Is.EqualTo(8));
            Assert.That(p1.Water, Is.EqualTo(8));
        }        

//        [Test]
        public void TestNotEnoughWaterWithAnotherParcel()
        {
            TestHelper.InMethod();
            //log4net.Config.XmlConfigurator.Configure();
            
            ParcelOrientedAllocator allocator = new ParcelOrientedAllocator();

            Player p1 = new Player("Alfred", UUID.Parse("00000000-0000-0000-0000-000000000001"));
            BuyPoint bp1 = new BuyPoint(UUID.Parse("00000000-0000-0000-0000-000000000010")) { WaterAvailable = 10, WaterRightsOwner = p1 };
            BuyPoint bp2 = new BuyPoint(UUID.Parse("00000000-0000-0000-0000-000000000020")) { WaterAvailable =  8, WaterRightsOwner = p1 };
            Field f1 = new Field(UUID.Parse("00000000-0000-0000-0001-000000000000"), "f1") { BuyPoint = bp1 };
            Houses h1 = new Houses("Houses", UUID.Parse("00000000-0000-0000-0010-000000000000"), Vector3.Zero, 1) { WaterUsage = 25, Field = f1 };
            bp1.AddGameAsset(h1);

            Assert.That(h1.WaterAllocated, Is.EqualTo(0));
            Assert.That(bp1.WaterAllocated, Is.EqualTo(0));
            Assert.That(bp1.WaterAvailable, Is.EqualTo(10));
            Assert.That(bp2.WaterAvailable, Is.EqualTo(8));
            Assert.That(p1.Water, Is.EqualTo(18));

            bool exceptionThrown = false;
            try
            {            
                allocator.ChangeAllocation(h1, p1, h1.WaterUsage);
            }
            catch (WaterWarsGameLogicException)
            {
                exceptionThrown = true;
            }

            Assert.That(exceptionThrown, Is.True);                

            Assert.That(h1.WaterAllocated, Is.EqualTo(0));
            Assert.That(bp1.WaterAllocated, Is.EqualTo(0));
            Assert.That(bp1.WaterAvailable, Is.EqualTo(10));
            Assert.That(bp2.WaterAvailable, Is.EqualTo(8));
            Assert.That(p1.Water, Is.EqualTo(18));           
        }        

//        [Test]
        public void TestUndoOwnsAssetParcelWaterRights()
        {
            TestHelper.InMethod();
            //log4net.Config.XmlConfigurator.Configure();
            
            ParcelOrientedAllocator allocator = new ParcelOrientedAllocator();

            Player p1 = new Player("Alfred", UUID.Parse("00000000-0000-0000-0000-000000000001"));
            BuyPoint bp1 = new BuyPoint(UUID.Parse("00000000-0000-0000-0000-000000000010")) { WaterAvailable = 15, DevelopmentRightsOwner = p1, WaterRightsOwner = p1 };
            BuyPoint bp2 = new BuyPoint(UUID.Parse("00000000-0000-0000-0000-000000000020")) { WaterAvailable =  8, WaterRightsOwner = p1 };
            Field f1 = new Field(UUID.Parse("00000000-0000-0000-0001-000000000000"), "f1") { BuyPoint = bp1 };
            Houses h1 = new Houses("Houses", UUID.Parse("00000000-0000-0000-0010-000000000000"), Vector3.Zero, 1) { WaterUsage = 10, Field = f1 };
            bp1.AddGameAsset(h1);

            allocator.ChangeAllocation(h1, p1, h1.WaterUsage);
            allocator.ChangeAllocation(h1, p1, 0);

            Assert.That(h1.WaterAllocated, Is.EqualTo(0));
            Assert.That(bp1.WaterAvailable, Is.EqualTo(15));
            Assert.That(bp2.WaterAvailable, Is.EqualTo(8));
        }

//        [Test]
        public void TestUndoOwnsOtherParcelWaterRights()
        {
            TestHelper.InMethod();
            //log4net.Config.XmlConfigurator.Configure();
            
            ParcelOrientedAllocator allocator = new ParcelOrientedAllocator();

            Player p1 = new Player("Alfred", UUID.Parse("00000000-0000-0000-0000-000000000001"));
            BuyPoint bp1 = new BuyPoint(UUID.Parse("00000000-0000-0000-0000-000000000010")) { WaterAvailable = 15, DevelopmentRightsOwner = p1 };
            BuyPoint bp2 = new BuyPoint(UUID.Parse("00000000-0000-0000-0000-000000000020")) { WaterAvailable =  8, WaterRightsOwner = p1 };
            BuyPoint bp3 = new BuyPoint(UUID.Parse("00000000-0000-0000-0000-000000000030")) { WaterAvailable =  5, WaterRightsOwner = p1 };
            Field f1 = new Field(UUID.Parse("00000000-0000-0000-0001-000000000000"), "f1") { BuyPoint = bp1 };
            Houses h1 = new Houses("Houses", UUID.Parse("00000000-0000-0000-0010-000000000000"), Vector3.Zero, 1) { WaterUsage = 10, Field = f1 };
            bp1.AddGameAsset(h1);

            allocator.ChangeAllocation(h1, p1, h1.WaterUsage);
            allocator.ChangeAllocation(h1, p1, 0);

            Assert.That(h1.WaterAllocated, Is.EqualTo(0));
            Assert.That(bp1.WaterAvailable, Is.EqualTo(15));

            // We take water from the richest parcel first.  But when we undo, water is distributed back evenly.
            Assert.That(bp2.WaterAvailable, Is.EqualTo(5));
            Assert.That(bp3.WaterAvailable, Is.EqualTo(8));
        }  
        
        /// <summary>
        /// Test a partial undo of allocated water
        /// </summary>
//        [Test]
        public void TestUndoPartial()
        {
            TestHelper.InMethod();
            //log4net.Config.XmlConfigurator.Configure();
            
            ParcelOrientedAllocator allocator = new ParcelOrientedAllocator();

            Player p1 = new Player("Alfred", UUID.Parse("00000000-0000-0000-0000-000000000001"));
            BuyPoint bp1 = new BuyPoint(UUID.Parse("00000000-0000-0000-0000-000000000010")) { WaterAvailable = 15, DevelopmentRightsOwner = p1, WaterRightsOwner = p1 };
            BuyPoint bp2 = new BuyPoint(UUID.Parse("00000000-0000-0000-0000-000000000020")) { WaterAvailable =  8, WaterRightsOwner = p1 };
            Field field1 = new Field(UUID.Parse("00000000-0000-0000-0001-000000000000"), "field1") { BuyPoint = bp1 };
            Factory f1 = new Factory("Factory1", UUID.Parse("00000000-0000-0000-0010-000000000000"), Vector3.Zero, 1) { WaterUsage = 10, Field = field1 };
            bp1.AddGameAsset(f1);

            allocator.ChangeAllocation(f1, p1, f1.WaterUsage);
            allocator.ChangeAllocation(f1, p1, 6);

            Assert.That(f1.WaterAllocated, Is.EqualTo(6));
            Assert.That(bp1.WaterAvailable, Is.EqualTo(9));
            Assert.That(bp2.WaterAvailable, Is.EqualTo(8));            
        }
    }
}