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
//using NHibernate;
//using NHibernate.Cfg;
//using NHibernate.Tool.hbm2ddl;
using NUnit.Framework;
using OpenMetaverse;
using OpenSim.Tests.Common;
using WaterWars.Models;

namespace WaterWars.Persistence.Tests
{ 
    /// <summary>
    /// Everything is commented out since tests are not currently active and I don't want to depend on NHibernate
    /// </summary>    
    [TestFixture]
    public class SimplePersistenceTests
    {
//        //[Test]
//        public void TestGenerateSchema()
//        {
//            TestHelpers.InMethod();            
//            log4net.Config.XmlConfigurator.Configure();
//            
//            var cfg = new Configuration();
//            cfg.Configure();
//            cfg.AddAssembly(typeof (Houses).Assembly);            
//            new SchemaExport(cfg).Create(true, true);            
//        }
//        
//        [Test]
//        public void TestSimpleAddHouses()
//        {
//            TestHelpers.InMethod();            
//            //log4net.Config.XmlConfigurator.Configure();
//            
//            string housesName = "houses1";
//            UUID housesUuid = UUID.Parse("00000000-0000-0000-0000-000000001001");
//            
//            var cfg = new Configuration();
//            cfg.Configure();
//            cfg.AddAssembly(typeof (Houses).Assembly);            
//            new SchemaExport(cfg).Create(true, true);
//            ISessionFactory sessionFactory = cfg.BuildSessionFactory();
//            
//            using (ISession session = sessionFactory.OpenSession())
//            {            
//                Houses h1 = new Houses(housesName, housesUuid, Vector3.Zero);                        
//                
//                using (session.BeginTransaction())
//                {
//                    session.Save(h1);
//                    session.Transaction.Commit();
//                }
//            }
//            
//            // Check that we can retrieve the house
//            using (ISession session = sessionFactory.OpenSession())
//            {
//                Houses h1FromDb = (Houses)session.Load(typeof(Houses), housesUuid);
//                Assert.That(h1FromDb.Uuid, Is.EqualTo(housesUuid));
//                Assert.That(h1FromDb.Name, Is.EqualTo(housesName));
//            }            
//            
//            sessionFactory.Close();
//        }
    }
}