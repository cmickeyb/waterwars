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
using System.Collections.Generic;
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
    public class PlayerServicesTests : AbstractWebServiceTests
    {
        [Test]
        public void TestGetLastSelected()
        {
            TestHelper.InMethod();
//            log4net.Config.XmlConfigurator.Configure();

            AddBuyPoints();
            AddPlayers(Developer.Singleton, Manufacturer.Singleton);
            StartGame();
            
            m_controller.ViewerWebServices.PlayerServices.SetLastSelected(p1.Uuid, bp1);
            
            string uri
                = string.Format("{0}{1}/{2}/{3}", 
                    WaterWarsConstants.WEB_SERVICE_PREFIX,
                    PlayerServices.PLAYER_PATH,
                    p1.Uuid,
                    PlayerServices.SELECTED_PATH);
            string reply = (string)SubmitRequest(uri, "get")["str_response_string"];

            UUID bpId = default(UUID);
            JsonReader jr = new JsonTextReader(new StringReader(reply));
            while (jr.Read())
            {
                //System.Console.WriteLine(string.Format("{0},{1},{2},{3}", jr.TokenType, jr.ValueType, jr.Depth, jr.Value));
                if (jr.TokenType == JsonToken.PropertyName && (string)jr.Value == "Guid" && jr.Depth == 3)
                {
                    jr.Read();
                    bpId = new UUID((string)jr.Value);
                    break;
                }
            }

            Assert.That(bpId, Is.EqualTo(bp1.Uuid));
        }
        
        [Test]
        public void TestHandleSetLastSelected()
        {
            TestHelper.InMethod();
//            log4net.Config.XmlConfigurator.Configure();

            AddBuyPoints();
            AddPlayers(Developer.Singleton, Manufacturer.Singleton);
            StartGame();
            
            string uri
                = string.Format("{0}{1}/{2}/{3}/{4}/{5}", 
                    WaterWarsConstants.WEB_SERVICE_PREFIX,
                    PlayerServices.PLAYER_PATH,
                    p1.Uuid,
                    PlayerServices.SELECTED_PATH,
                    PlayerServices.PLAYER_PATH,
                    p1.Uuid);
            
            SubmitRequest(uri, "put");

            Assert.That(
                m_controller.ViewerWebServices.PlayerServices.GetLastSelected(p1.Uuid, false).Uuid, 
                Is.EqualTo(p1.Uuid));
        }
        
        [Test]
        public void TestGetPlayer()
        {
            TestHelper.InMethod();
//            log4net.Config.XmlConfigurator.Configure();

            AddPlayers(Developer.Singleton, Manufacturer.Singleton);
            StartGame();
            
            string uri
                = string.Format("{0}{1}/{2}",
                    WaterWarsConstants.WEB_SERVICE_PREFIX,
                    PlayerServices.PLAYER_PATH,
                    p1.Uuid);
            string reply = (string)SubmitRequest(uri, "get")["str_response_string"];

            UUID returnedId = UUID.Zero;
            int returnedRole = -999;
            
            JsonReader jr = new JsonTextReader(new StringReader(reply));
            while (jr.Read())
            {
//                System.Console.WriteLine(string.Format("{0},{1},{2},{3}", jr.TokenType, jr.ValueType, jr.Depth, jr.Value));
                if (jr.TokenType == JsonToken.PropertyName)
                {
                    if ((string)jr.Value == "Guid" && jr.Depth == 3)
                    {
                        jr.Read();
                        returnedId = new UUID((string)jr.Value);
                    }
                    else if ((string)jr.Value == "Type" && returnedRole == -999) // Yes, this isn't the only type!  Poor way of getting values, I know
                    {
                        jr.Read();
                        returnedRole = (int)(long)jr.Value;
                    }                    
                }
            }

            Assert.That(returnedId, Is.EqualTo(p1.Uuid));
            Assert.That(returnedRole, Is.EqualTo((int)RoleType.Developer)); 
        }
        
        [Test]
        public void TestGetPlayers()
        {
            TestHelper.InMethod();
//            log4net.Config.XmlConfigurator.Configure();

            AddPlayers(Developer.Singleton, Manufacturer.Singleton);
            StartGame();
            
            string uri
                = string.Format("{0}{1}",
                    WaterWarsConstants.WEB_SERVICE_PREFIX,
                    PlayerServices.PLAYER_PATH);
            string reply = (string)SubmitRequest(uri, "get")["str_response_string"];

            HashSet<UUID> returnedIds = new HashSet<UUID>();
            
            JsonReader jr = new JsonTextReader(new StringReader(reply));
            while (jr.Read())
            {
                //System.Console.WriteLine(string.Format("{0},{1},{2},{3}", jr.TokenType, jr.ValueType, jr.Depth, jr.Value));
                if (jr.TokenType == JsonToken.PropertyName && (string)jr.Value == "Guid" && jr.Depth == 4)
                {
                    jr.Read();
                    returnedIds.Add(new UUID((string)jr.Value));
                }
            }

            Assert.That(
                returnedIds.Remove(p1.Uuid), 
                Is.True, 
                string.Format("p1's uuid {0} was not in the set of returned ids", p1.Uuid));
            Assert.That(
                returnedIds.Remove(p2.Uuid), 
                Is.True, 
                string.Format("p2's uuid {0} was not in the set of returned ids", p2.Uuid));            
            Assert.That(
                returnedIds.Count, Is.EqualTo(0), "Received extras ids that do not belong to any registered player");
        }  
        
        [Test]
        public void TestSellWaterRights()
        {
            TestHelper.InMethod();
//            log4net.Config.XmlConfigurator.Configure();

            AddBuyPoints();
            AddPlayers(Developer.Singleton, Manufacturer.Singleton);
            StartGame();
            
            m_controller.State.BuyLandRights(bp1, p1);

            int amount = 17;
            int waterRightsPrice = 43;
            int p1ProjectedMoney = p1.Money + waterRightsPrice;
            int p2ProjectedMoney = p2.Money - waterRightsPrice;   
            int p1ProjectedWaterRights = p1.WaterEntitlement - amount;
            int p2ProjectedWaterRights = p2.WaterEntitlement + amount;
            
            JObject json = new JObject(
                new JProperty("WaterRightsBuyer", new JObject(new JProperty("Uuid", new JObject(new JProperty("Guid", p2.Uuid.ToString()))))),
                new JProperty("DeltaMoney", -waterRightsPrice),
                new JProperty("DeltaWaterEntitlement", amount));
            
            string uri = Path.Combine(WaterWarsConstants.WEB_SERVICE_PREFIX, Path.Combine(PlayerServices.PLAYER_PATH, p1.Uuid.ToString()));
            SubmitRequest(uri, "put", json.ToString());        

            Assert.That(p1.Money, Is.EqualTo(p1ProjectedMoney));
            Assert.That(p2.Money, Is.EqualTo(p2ProjectedMoney));
            Assert.That(p1.WaterEntitlement, Is.EqualTo(p1ProjectedWaterRights));
            Assert.That(p2.WaterEntitlement, Is.EqualTo(p2ProjectedWaterRights));                       
        }        
        
        [Test]
        public void TestSellWater()
        {
            TestHelper.InMethod();
//            log4net.Config.XmlConfigurator.Configure();

            AddBuyPoints();
            AddPlayers(Developer.Singleton, Manufacturer.Singleton);
            StartGame();
            
            m_controller.State.BuyLandRights(bp1, p1);
            m_controller.State.BuyLandRights(bp2, p2);

            EndTurns(1);

            int p1StartMoney = p1.Money;
            int p2StartMoney = p2.Money;
            int p1StartWater = p1.Water;
            int p2StartWater = p2.Water;
            
            // Selling 200 water for 500 money
            int water = 200;
            int price = 500;
            
            JObject json = new JObject(
                new JProperty("WaterBuyer", new JObject(new JProperty("Uuid", new JObject(new JProperty("Guid", p2.Uuid.ToString()))))),
                new JProperty("DeltaMoney", -price),
                new JProperty("DeltaWater", water));
            
            string uri = Path.Combine(WaterWarsConstants.WEB_SERVICE_PREFIX, Path.Combine(PlayerServices.PLAYER_PATH, p1.Uuid.ToString()));
            SubmitRequest(uri, "put", json.ToString());

            Assert.That(p1.Money, Is.EqualTo(p1StartMoney + price));
            Assert.That(p2.Money, Is.EqualTo(p2StartMoney - price));
            Assert.That(p1.Water, Is.EqualTo(p1StartWater - water));
            Assert.That(p2.Water, Is.EqualTo(p2StartWater + water));
        }        
    }
}