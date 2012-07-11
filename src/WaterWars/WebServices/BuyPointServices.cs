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
using System.Reflection;
using System.Text;
using HttpServer;
using HttpServer.FormDecoders;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using WaterWars;
using WaterWars.Models;

namespace WaterWars.WebServices
{
    public class BuyPointServices : AbstractServices
    {
//        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);       

        public const string BUY_POINT_PATH = "buypoint/";
        public const string ASSETS_PATH = "assets";
        public const string FIELD_PATH = "field";    
        
        public BuyPointServices(WaterWarsController controller) : base(controller) {}

        public Hashtable HandleRequest(string requestUri, Hashtable request)
        {
//            m_log.InfoFormat("[WATER WARS]: Handling buy point request for {0}", requestUri);

            requestUri = requestUri.Substring(BUY_POINT_PATH.Length);

            Hashtable response = null;
            
            List<string> requestComponents = new List<string>(requestUri.Split(new char[] { '/' }));
            string rawBpId = requestComponents[0];

            if (requestComponents.Count == 1)
            {
                response = HandleBuyPointChange(rawBpId, request);
            }
            else if (requestComponents[1] == ASSETS_PATH)
            {
                string rawGameAssetId = requestComponents[2];
                string method = ((string)request["_method"]).ToLower();
                
                if ("delete" == method)
                    response = HandleSellAsset(rawBpId, rawGameAssetId, request);
                else if ("put" == method)
                    response = HandleChangeAsset(rawBpId, rawGameAssetId, request);
                else
                    response = HandleGetAsset(rawBpId, rawGameAssetId, request);
            }
            else if (requestComponents[1] == FIELD_PATH)
            {
                string rawFieldId = requestComponents[2];
                string method = ((string)request["_method"]).ToLower();

                if ("post" == method)
                    response = HandleBuyAsset(rawBpId, rawFieldId, request);
            }

            return response;
        }

        protected Hashtable HandleBuyPointChange(string rawBpId, Hashtable request)
        {           
            JObject json = GetJsonFromPost(request);
            
            string name = (string)json.SelectToken("Name");
            bool changingName = name != null;
            
            string rawDevelopmentRightsBuyerId = (string)json.SelectToken("DevelopmentRightsOwner.Uuid.Guid");
            bool buyingDevelopmentRights = rawDevelopmentRightsBuyerId != null;
            
            string rawWaterRightsBuyerId = (string)json.SelectToken("WaterRightsOwner.Uuid.Guid");
            bool buyingWaterRights = rawWaterRightsBuyerId != null;
            
            int? developmentRightsPrice = (int?)json.SelectToken("DevelopmentRightsPrice");
            int? waterRightsPrice = (int?)json.SelectToken("WaterRightsPrice");
            int? combinedRightsPrice = (int?)json.SelectToken("CombinedPrice");         
          
//            m_log.InfoFormat(
//                "rawBpId [{0}], buyingDevelopmentRights [{1}], buyingWaterRights [{2}]", 
//                rawBpId, buyingDevelopmentRights, buyingWaterRights);

            UUID bpId = WaterWarsUtils.ParseRawId(rawBpId);
            BuyPoint bp = null;
            
            m_controller.Game.BuyPoints.TryGetValue(bpId, out bp);        

            if (changingName)
                return HandleChangeName(bp, name);                
            else if (buyingDevelopmentRights || buyingWaterRights)
                return HandleBuyRights(
                    bp, buyingDevelopmentRights, buyingWaterRights,
                    rawDevelopmentRightsBuyerId, rawWaterRightsBuyerId,
                    developmentRightsPrice, waterRightsPrice, combinedRightsPrice, request);           

            // TODO: Deal with error situations: uuid not valid, no such buy point, not enough money, etc.

            Hashtable reply = new Hashtable();
            reply["int_response_code"] = 200; // 200 OK
            reply["str_response_string"] = "hoorah";
            reply["content_type"] = "text/plain";            

            return reply;
        }

        protected Hashtable HandleChangeName(BuyPoint bp, string newName)
        {
            m_controller.State.ChangeBuyPointName(bp, newName);
            
            Hashtable reply = new Hashtable();
            reply["int_response_code"] = 200; // 200 OK
            reply["str_response_string"] = "sausages";
            reply["content_type"] = "text/plain";            

            return reply;              
        }
        
        protected Hashtable HandleBuyRights(
            BuyPoint bp, bool buyingDevelopmentRights, bool buyingWaterRights,
            string rawDevelopmentRightsBuyerId, string rawWaterRightsBuyerId,
            int? developmentRightsPrice, int? waterRightsPrice, int? combinedRightsPrice, Hashtable request)
        {
            if (bp.DevelopmentRightsOwner == Player.None)            
            {
                m_controller.Resolver.BuyLandRights(bp.Uuid.ToString(), rawDevelopmentRightsBuyerId);
            }
            else
            {
                UUID developmentRightsBuyerId = UUID.Zero;
                if (rawDevelopmentRightsBuyerId != null)
                    developmentRightsBuyerId = WaterWarsUtils.ParseRawId(rawDevelopmentRightsBuyerId);

                UUID waterRightsBuyerId = UUID.Zero;
                if (rawWaterRightsBuyerId != null)
                    waterRightsBuyerId = WaterWarsUtils.ParseRawId(rawWaterRightsBuyerId);

                // TODO: Do player ownership checks later when we have access to this via security

                if (buyingDevelopmentRights && buyingWaterRights)
                {
                    if (null == combinedRightsPrice)
                        throw new Exception(
                            string.Format("No combined rights price specified for sale of {0} rights", bp));
                    
                    m_controller.Resolver.SellRights(
                        bp, developmentRightsBuyerId, RightsType.Combined, (int)combinedRightsPrice);
                }
                else if (buyingDevelopmentRights)
                {
                    if (null == developmentRightsPrice)
                        throw new Exception(
                            string.Format("No development rights price specified for sale of {0} rights", bp));
                    
                    m_controller.Resolver.SellRights(
                        bp, developmentRightsBuyerId, RightsType.Development, (int)developmentRightsPrice);
                }
                else if (buyingWaterRights)
                {
                    if (null == waterRightsPrice)
                        throw new Exception(
                            string.Format("No water rights price specified for sale of {0} rights", bp));
                    
                    m_controller.Resolver.SellRights(
                        bp, waterRightsBuyerId, RightsType.Water, (int)waterRightsPrice);
                }
            }            

            // TODO: Deal with error situations: uuid not valid, no such buy point, not enough money, etc.

            Hashtable reply = new Hashtable();
            reply["int_response_code"] = 200; // 200 OK
            reply["str_response_string"] = "hoorah";
            reply["content_type"] = "text/plain";            

            return reply;            
        }
            
        protected Hashtable HandleGetAsset(string rawBpId, string rawGameAssetId, Hashtable request)
        {
            Hashtable reply = new Hashtable();

            UUID bpId = new UUID(rawBpId);
            
            IDictionary<UUID, BuyPoint> buyPoints = m_controller.Game.BuyPoints;

            BuyPoint bp;
            buyPoints.TryGetValue(bpId, out bp);

            UUID gameAssetId = new UUID(rawGameAssetId);

            AbstractGameAsset ga;

            lock (bp.GameAssets)
                bp.GameAssets.TryGetValue(gameAssetId, out ga);
                
            reply["int_response_code"] = 200; // 200 OK
            reply["str_response_string"] = BuyPointServices.Serialize(ga);
            reply["content_type"] = "text/plain";

            return reply;
        }

        /// <summary>
        /// Buy an asset in a given field.
        /// </summary>
        /// <param name="rawBpId"></param>
        /// <param name="rawFieldId"></param>
        /// <param name="request"></param>
        /// <returns></returns>        
        protected Hashtable HandleBuyAsset(string rawBpId, string rawFieldId, Hashtable request)
        {
            JObject json = GetJsonFromPost(request);
            int level = (int)json.SelectToken("Level");

//            m_log.InfoFormat(
//                "[WATER WARS]: Processing webservices [buy game asset] request for buypoint {0}, field {1} to level {2}", 
//                rawBpId, rawFieldId, level);

            // Automatically switch to the game asset from the field           
            AbstractGameAsset ga = m_controller.Resolver.BuildGameAsset(rawBpId, rawFieldId, level);

            // TODO: return success condition
            Hashtable reply = new Hashtable();
            reply["int_response_code"] = 200; // 200 OK
            reply["str_response_string"] = "oh yeah baby";
            reply["content_type"] = "text/plain";

            m_controller.ViewerWebServices.PlayerServices.SetLastSelected(
                ga.Field.BuyPoint.DevelopmentRightsOwner.Uuid, ga);

            return reply;        
        }        

        protected Hashtable HandleChangeAsset(string rawBpId, string rawAssetId, Hashtable request)
        {
            bool upgradeAsset = false;
            bool continueBuild = false;
            bool sellAssetToEconomy = false;
            bool waterAllocated = false;
            
            JObject json = GetJsonFromPost(request);
            
            int? level = (int?)json.SelectToken("Level");
            if (level != null)
                upgradeAsset = true;
            
            int? waterAllocation = (int?)json.SelectToken("WaterAllocated");
            if (waterAllocation != null)
                waterAllocated = true;
            
            if (json.SelectToken("OwnerUuid") != null)
                sellAssetToEconomy = true;
            
            if (json.SelectToken("TurnsBuilt") != null)
                continueBuild = true;

            if (upgradeAsset)
            {
//                m_log.InfoFormat(
//                    "[WATER WARS]: Processing webservices upgrade game asset request for buypoint {0}, asset {1} to level {2}", 
//                    rawBpId, rawAssetId, level);
                m_controller.Resolver.UpgradeGameAsset(rawBpId, rawAssetId, (int)level);
            }

            if (waterAllocated)
            {
                if (null == waterAllocation)
                    throw new Exception(
                        string.Format(
                            "No amount specified for allocation to game asset {0} on parcel {1}", rawAssetId, rawBpId));
                
                m_controller.Resolver.UseWater(
                    rawBpId, rawAssetId, UUID.Zero.ToString(), (int)waterAllocation);
            }
            
            if (sellAssetToEconomy)
                m_controller.Resolver.SellGameAssetToEconomy(rawBpId, rawAssetId);
            
            if (continueBuild)
                m_controller.Resolver.ContinueBuildingGameAsset(rawBpId, rawAssetId);

            // TODO: return success condition
            Hashtable reply = new Hashtable();
            reply["int_response_code"] = 200; // 200 OK
            reply["str_response_string"] = "oh yeah baby";
            reply["content_type"] = "text/plain";            

            return reply;        
        }        

        protected Hashtable HandleSellAsset(string rawBpId, string rawAssetId, Hashtable request)
        {
            Field f = m_controller.Resolver.RemoveGameAsset(rawBpId, rawAssetId);

            // TODO: properly return success condition
            Hashtable reply = new Hashtable();
            reply["int_response_code"] = 200; // 200 OK
            reply["str_response_string"] = "uh huh, it's going on";
            reply["content_type"] = "text/plain";

            if (f != null)
                m_controller.ViewerWebServices.PlayerServices.SetLastSelected(f.BuyPoint.DevelopmentRightsOwner.Uuid, f);

            return reply;            
        }
        
        public static string SerializeBuyPoint(BuyPoint bp)
        {
            return JsonConvert.SerializeObject(bp, Formatting.Indented);          
        }

        public static string Serialize(AbstractGameAsset ga)
        {
            return JsonConvert.SerializeObject(ga, Formatting.Indented);
        }
    }
/*
    public class WaterWarsContractResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(JsonObjectContract contract, MemberInfo member)
        {
            JsonProperty p = base.CreateProperties(contract, member);
            
            if (member.GetType() == typeof(System.Enum))
            {
                p.PropertyType = typeof(String);
                
//            JsonProperty p = base.CreateProperties(contract, member);

            
 

    // only serializer properties that start with the specified character

    properties = 

      properties.Where(p => p.PropertyName.StartsWith(_startingWithChar.ToString())).ToList();

 

    return properties;

  }

}
*/
    
}