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
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using OpenMetaverse;
using OpenSim.Tests.Common;
using WaterWars.Models;
using WaterWars.Models.Roles;
using WaterWars.Rules.Allocators;
using WaterWars.Rules.Distributors;
using WaterWars.Rules.Generators;
using WaterWars.Tests;

namespace WaterWars.Persistence.Tests
{ 
    /// <summary>
    /// Everything is commented out since tests are not currently active and I don't want to depend on NHibernate
    /// </summary>
    //[TestFixture]
    public class AbstractGameAssetPersistenceTests : AbstractGameTests
    {
//        protected override WaterWarsController CreateController()
//        {
//            return 
//                new WaterWarsController(
//                    new Persister(),
//                    null,
//                    new SimpleUtopianRainfallGenerator(), 
//                    new SimpleFairPerParcelWaterDistributor(), 
//                    new ParcelOrientedAllocator());            
//        } 
//                
//        /// <summary>
//        /// Test state serialization
//        /// </summary>
//        //[Test]
//        public void TestSerialization()
//        {
//            TestHelper.InMethod();
//            
//            File.Delete(Path.Combine(WaterWarsConstants.STATE_PATH, WaterWarsConstants.STATE_FILE_NAME));
//            
//            AddBuyPoints();
//            AddPlayers(Developer.Singleton);
//            StartGame();
//            
//            Assert.That(File.Exists(Path.Combine(WaterWarsConstants.STATE_PATH, WaterWarsConstants.STATE_FILE_NAME)));
//        }
//        
//        //[Test]
//        public void TestBuyHousesPersistent()
//        {
//            TestHelper.InMethod();
//            
//            AddBuyPoints();
//            AddPlayers(Developer.Singleton);
//            StartGame();
//            
//            m_controller.State.BuyLandRights(bp1, p1);
//            AddHouses();
//            
//            using (ISession session = m_controller.Persister.m_sessionFactory.OpenSession())
//            {   
//                using (session.BeginTransaction())
//                {
//                    IQuery query = session.CreateQuery("from Houses");
//                    Assert.That(query.List().Count, Is.EqualTo(3));
//                    session.Transaction.Commit();
//                }
//            }             
//        }
//        
//        //[Test]
//        public void TestUpgradeHousesPersistent()
//        {
//            TestHelper.InMethod();
//            
//            AddBuyPoints();
//            AddPlayers(Developer.Singleton);
//            StartGame();
//            
//            m_controller.State.BuyLandRights(bp1, p1);
//            AddHouses();
//            Assert.That(h1.Level, Is.EqualTo(1));
//            m_controller.State.UpgradeGameAsset(bp1, p1, h1, 3);
//            
//            using (ISession session = m_controller.Persister.m_sessionFactory.OpenSession())
//            {   
//                using (session.BeginTransaction())
//                {
//                    IQuery query = session.CreateQuery("from Houses h where h.Uuid = :uuid");
//                    query.SetString("uuid", h1.Uuid.ToString());
//                    Houses persistedHouses = query.List()[0] as Houses;
//                    Assert.That(persistedHouses.Level, Is.EqualTo(3));
//                }
//            }             
//        }        
    }
}