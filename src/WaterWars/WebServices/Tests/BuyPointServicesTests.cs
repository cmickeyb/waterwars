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
using System.Collections;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using OpenMetaverse;
using OpenSim.Region.CoreModules.World.Land;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Tests.Common;
using WaterWars.Models;
using WaterWars.Models.Roles;
using WaterWars.Rules;
using WaterWars.States;
using WaterWars.Tests;
using WaterWars.WebServices;

namespace WaterWars.WebServices.Tests
{
    /// <summary>
    /// These tests concentrate on the Water Wars web services and required parameter validation.
    /// </summary>
    /// Game logic tests should take place in GameTests.
    [TestFixture]
    public class BuyPointServicesTests : AbstractWebServiceTests
    {
        [Test]
        public void TestChangeName()
        {
            TestHelper.InMethod();
            //log4net.Config.XmlConfigurator.Configure();
            
            AddBuyPoints();
            AddPlayers(Developer.Singleton);
            StartGame();
            
            m_controller.State.BuyLandRights(bp1, p1);                        
            
            string newName = "bob";  
            
            string json = "{ \"Name\" : \"" + newName + "\" }";
            
            m_controller.State.ChangeBuyPointName(bp1, newName);
            
            string uri
                = string.Format("{0}{1}{2}", 
                    WaterWarsConstants.WEB_SERVICE_PREFIX, 
                    BuyPointServices.BUY_POINT_PATH,
                    bp1.Uuid);
            SubmitRequest(uri, "put", json);
            
            Assert.That(bp1.Name, Is.EqualTo(newName));            
        }
        
        [Test]
        public void TestBuildAsset()
        {
            TestHelper.InMethod();
            //log4net.Config.XmlConfigurator.Configure();

            AddBuyPoints();
            AddPlayers(Developer.Singleton, Manufacturer.Singleton);
            StartGame();
            
            m_controller.State.BuyLandRights(bp1, p1);

            Field field = null;
            foreach (Field f in bp1.Fields.Values)
            {
                field = f;
                break;
            }
            
            string json = "{ \"Level\" : 2 }";
            string uri
                = string.Format("{0}{1}{2}/{3}/{4}", 
                    WaterWarsConstants.WEB_SERVICE_PREFIX, 
                    BuyPointServices.BUY_POINT_PATH, 
                    bp1.Uuid, 
                    BuyPointServices.FIELD_PATH,
                    field.Uuid);
            SubmitRequest(uri, "post", json);

            Assert.That(bp1.Housess.Count, Is.EqualTo(1));            
        }   
        
        [Test]
        public void TestContinueBuildAsset()
        {
            TestHelper.InMethod();
//            log4net.Config.XmlConfigurator.Configure();

            AddBuyPoints();
            AddPlayers(Developer.Singleton, Manufacturer.Singleton);
            StartGame();
            
            m_controller.State.BuyLandRights(bp1, p1);
            AddHouses();
            
            // Flip to next build phase
            EndTurns(2);
            
            string json = "{ \"Uuid\" : { \"Guid\" : \"" + h1.Uuid + "\" }, \"TurnsBuilt\" : 2 }";
            string uri
                = string.Format("{0}{1}{2}/{3}/{4}", 
                    WaterWarsConstants.WEB_SERVICE_PREFIX, 
                    BuyPointServices.BUY_POINT_PATH, 
                    bp1.Uuid, 
                    BuyPointServices.ASSETS_PATH,
                    h1.Uuid);
            SubmitRequest(uri, "put", json);            

            Assert.That(h1.StepsBuilt, Is.EqualTo(2));
        }

        [Test]
        public void TestAllocateWater()
        {
            TestHelper.InMethod();
            //log4net.Config.XmlConfigurator.Configure();

            AddBuyPoints();
            AddPlayers(Manufacturer.Singleton, Developer.Singleton);
            StartGame();
            
            m_controller.State.BuyLandRights(bp1, p1);
            AddFactories();

            // Move to the water phase
            m_controller.State.EndTurn(p1.Uuid);
            m_controller.State.EndTurn(p2.Uuid);            

            JObject json 
                = new JObject(
                    new JProperty("Uuid",
                        new JObject(
                            new JProperty("Guid", f1.Uuid.ToString())
                        )
                    ),    
                    new JProperty("WaterAllocated", f1.WaterUsage));
            
            string uri
                = string.Format("{0}{1}{2}/{3}/{4}", 
                    WaterWarsConstants.WEB_SERVICE_PREFIX, 
                    BuyPointServices.BUY_POINT_PATH, 
                    bp1.Uuid, 
                    BuyPointServices.ASSETS_PATH,
                    f1.Uuid);
            SubmitRequest(uri, "put", json.ToString());

            Assert.That(f1.WaterAllocated, Is.EqualTo(f1.WaterUsage));            
        }

        [Test]
        public void TestUndoAllocateWater()
        {
            TestHelper.InMethod();

            AddBuyPoints();
            AddPlayers(Manufacturer.Singleton, Developer.Singleton);
            StartGame();
            
            m_controller.State.BuyLandRights(bp1, p1);
            AddFactories();

            // Move to the water phase
            m_controller.State.EndTurn(p1.Uuid);
            m_controller.State.EndTurn(p2.Uuid);

            m_controller.State.UseWater(f1, p1, f1.WaterUsage);

            string json = "{ \"Uuid\" : { \"Guid\" : \"" + f1.Uuid + "\" }, \"WaterAllocated\" : 0 }";
            string uri
                = string.Format("{0}{1}{2}/{3}/{4}", 
                    WaterWarsConstants.WEB_SERVICE_PREFIX, 
                    BuyPointServices.BUY_POINT_PATH, 
                    bp1.Uuid, 
                    BuyPointServices.ASSETS_PATH,
                    f1.Uuid);
            SubmitRequest(uri, "put", json);

            Assert.That(f1.WaterAllocated, Is.EqualTo(0));
        }        
        
        [Test]
        public void TestUpgradeAsset()
        {
            TestHelper.InMethod();
            //log4net.Config.XmlConfigurator.Configure();

            AddBuyPoints();
            AddPlayers(Manufacturer.Singleton, Developer.Singleton);
            StartGame();
            
            m_controller.State.BuyLandRights(bp1, p1);
            AddFactories();
            
            string json = "{ \"Uuid\" : { \"Guid\" : \"" + f1.Uuid + "\" }, \"Level\" : 3 }";
            string uri
                = string.Format("{0}{1}{2}/{3}/{4}", 
                    WaterWarsConstants.WEB_SERVICE_PREFIX, 
                    BuyPointServices.BUY_POINT_PATH, 
                    bp1.Uuid, 
                    BuyPointServices.ASSETS_PATH,
                    f1.Uuid);
            SubmitRequest(uri, "put", json);

            Assert.That(f1.Level, Is.EqualTo(3));
        }
        
        [Test]
        public void TestSellAsset()
        {
            TestHelper.InMethod();
            //log4net.Config.XmlConfigurator.Configure();

            AddBuyPoints();
            AddPlayers(Developer.Singleton, Manufacturer.Singleton);
            StartGame();
            
            m_controller.State.BuyLandRights(bp1, p1);
            AddHouses();
            
            EndTurns(2);
            m_controller.State.ContinueBuildingGameAsset(h1);
            
            int p1Money = p1.Money;
            
            string json 
                = string.Format(
                    @"{{ ""Uuid"" : {{ ""Guid"" : ""{0}"" }}, ""OwnerUuid"" : {{ ""Guid"" : ""{1}"" }} }}",
                    h1.Uuid, m_controller.Game.Economy.Uuid);
            
            string uri
                = string.Format("{0}{1}{2}/{3}/{4}", 
                    WaterWarsConstants.WEB_SERVICE_PREFIX, 
                    BuyPointServices.BUY_POINT_PATH, 
                    bp1.Uuid, 
                    BuyPointServices.ASSETS_PATH,
                    h1.Uuid);
            SubmitRequest(uri, "put", json);

            Assert.That(h1.Field.Owner, Is.EqualTo(m_controller.Game.Economy));
            Assert.That(p1.Money, Is.EqualTo(p1Money + h1.NormalRevenue));
        }        
        
        [Test]
        public void TestRemoveAsset()
        {
            TestHelper.InMethod();
            //log4net.Config.XmlConfigurator.Configure();

            AddBuyPoints();
            AddPlayers(Developer.Singleton, Manufacturer.Singleton);
            StartGame();
            
            m_controller.State.BuyLandRights(bp1, p1);
            AddHouses();

            string uri 
                = string.Format("{0}{1}{2}/{3}/{4}", 
                    WaterWarsConstants.WEB_SERVICE_PREFIX, 
                    BuyPointServices.BUY_POINT_PATH, 
                    bp1.Uuid, 
                    BuyPointServices.ASSETS_PATH,
                    h1.Uuid);
            SubmitRequest(uri, "delete");

            Assert.That(bp1.Housess.Count, Is.EqualTo(2));              
        }

        [Test]
        public void TestBuyRights()
        {
            TestHelper.InMethod();
            //log4net.Config.XmlConfigurator.Configure();

            AddBuyPoints();
            AddPlayers(Developer.Singleton, Manufacturer.Singleton);
            StartGame();

            string json 
                = "{ \"DevelopmentRightsOwner\" : { \"Uuid\" : { \"Guid\" : \"" + p1.Uuid + "\" } }, "
                + "  \"WaterRightsOwner\" : { \"Uuid\" : { \"Guid\" : \"" + p1.Uuid + "\" } } }";
            string uri
                = string.Format("{0}{1}{2}", 
                    WaterWarsConstants.WEB_SERVICE_PREFIX, 
                    BuyPointServices.BUY_POINT_PATH,
                    bp1.Uuid);
            SubmitRequest(uri, "put", json);

            Assert.That(bp1.DevelopmentRightsOwner, Is.EqualTo(p1));
            Assert.That(bp1.WaterRightsOwner, Is.EqualTo(p1));
        }

        [Test]
        public void TestSellOneRight()
        {
            TestHelper.InMethod();
//            log4net.Config.XmlConfigurator.Configure();

            AddBuyPoints();
            AddPlayers(Developer.Singleton, Manufacturer.Singleton);
            StartGame();
            
            m_controller.State.BuyLandRights(bp1, p1);

            int waterRightsPrice = 43;
            int p1Money = p1.Money;
            int p2Money = p2.Money;            

            string json 
                = "{ \"WaterRightsOwner\" : { \"Uuid\" : { \"Guid\" : \"" + p2.Uuid + "\" } }, "
                + "  \"WaterRightsPrice\" : " + waterRightsPrice + " }";
            string uri
                = string.Format("{0}{1}{2}", 
                    WaterWarsConstants.WEB_SERVICE_PREFIX, 
                    BuyPointServices.BUY_POINT_PATH,
                    bp1.Uuid);
            SubmitRequest(uri, "put", json);

            Assert.That(bp1.DevelopmentRightsOwner, Is.EqualTo(p1));
            Assert.That(bp1.WaterRightsOwner, Is.EqualTo(p2));
            Assert.That(p1.Money, Is.EqualTo(p1Money + waterRightsPrice));
            Assert.That(p2.Money, Is.EqualTo(p2Money - waterRightsPrice));                        
        }        

        [Test]
        public void TestSellCombinedRights()
        {
            TestHelper.InMethod();
//            log4net.Config.XmlConfigurator.Configure();

            AddBuyPoints();
            AddPlayers(Developer.Singleton, Manufacturer.Singleton);
            StartGame();
            
            m_controller.State.BuyLandRights(bp1, p1);

            int combinedRightsPrice = 98;
            int p1Money = p1.Money;
            int p2Money = p2.Money;

            string json 
                = "{ \"DevelopmentRightsOwner\" : { \"Uuid\" : { \"Guid\" : \"" + p2.Uuid + "\" } }, "
                + "  \"WaterRightsOwner\" : { \"Uuid\" : { \"Guid\" : \"" + p2.Uuid + "\" } }, "
                + "  \"CombinedPrice\" : " + combinedRightsPrice + " }";
            string uri
                = string.Format("{0}{1}{2}", 
                    WaterWarsConstants.WEB_SERVICE_PREFIX, 
                    BuyPointServices.BUY_POINT_PATH,
                    bp1.Uuid);
            SubmitRequest(uri, "put", json);

            Assert.That(bp1.DevelopmentRightsOwner, Is.EqualTo(p2));
            Assert.That(bp1.WaterRightsOwner, Is.EqualTo(p2));
            Assert.That(p1.Money, Is.EqualTo(p1Money + combinedRightsPrice));
            Assert.That(p2.Money, Is.EqualTo(p2Money - combinedRightsPrice));                        
        }
    }
}