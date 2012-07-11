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
using System.Linq;
using System.Reflection;
using System.ServiceModel.Syndication;
using System.Threading;
using System.Xml;
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
    public class PlayerServices : AbstractServices
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);                

        public const string PLAYER_PATH = "player";
        public const string SELECTED_PATH = "selected";
        public const string LOGIN_PATH = "login";
        public const string NEWS_PATH = "news";
        public const string UPDATE_PATH = "update";

        /// <value>
        /// The game model last selected for a particular player.
        /// </value>        
        protected Dictionary<UUID, AbstractGameModel> m_lastSelected = new Dictionary<UUID, AbstractGameModel>();
                
        /// <summary>
        /// Triggered when the player selects a new asset.
        /// </summary>
        protected Dictionary<UUID, Object> m_lastSelectedSyncObjects = new Dictionary<UUID, Object>();        
        
        /// <summary>
        /// Record the change delegates we subscribe to events so that we can successfully remove them again.
        /// </summary>
        protected Dictionary<UUID, AbstractModel.OnChangeDelegate> m_lastSelectedChangeDelegates 
            = new Dictionary<UUID, AbstractModel.OnChangeDelegate>();

        /// <value>
        /// Login tokens for players.
        /// </value>
        protected Dictionary<UUID, UUID> m_loginTokens = new Dictionary<UUID, UUID>();       
        
        public PlayerServices(WaterWarsController controller) : base(controller)
        {            
            m_controller.EventManager.OnPlayerAdded 
                += delegate(Player p) 
                {
                    m_lastSelected[p.Uuid] = AbstractGameAsset.None;
                    m_lastSelectedSyncObjects[p.Uuid] = new Object();
                };
            
            // Update any players who have selected assets that are removed at the end of the revenue stage
            m_controller.EventManager.OnRevenueStageEnded += HandleOnRevenueStageEnded;
            
            // Leave it up to the state themselves to trigger general change events
            // This is because some states perform multiple actions for which we want only one event raised, in order
            // to prevent race conditions on the comet web interface
//            m_controller.EventManager.OnStateStarted += delegate(GameStateType type) 
//            {                
//                // If the game changes then update all long polls, since the owner/non-owner actions available may well
//                // have changed.
//                // However, if the game is in the revenue state while transitioning from the water state back
//                // to the build state then ignore the state update.
//                // The reason for this is that the revenue state update will fire and then the build update very
//                // shortly afterwards before the client has a chance to repoll.  Therefore, build owner actions will
//                // not appear correctly until the owner reselects the game asset.                
//                if (m_controller.Game.State == GameStateType.Revenue)
//                {
////                    m_log.InfoFormat(
////                        "[WATER WARS]: Not pulsing all sync objects as ignoring state change to {0}", type);
//                    return;
//                }
//                
////                m_log.InfoFormat("[WATER WARS]: Pulsing all sync objects due to state change to {0}", type);
//                
//                foreach (Object o in m_lastSelectedSyncObjects.Values)
//                {
//                    lock (o)
//                        Monitor.PulseAll(o);
//                }
//            };
        } 
        
        protected void HandleOnRevenueStageEnded(List<AbstractGameAsset> removedAssets)
        {
            lock (m_lastSelected)
            {
                // Use a copy of the last selected dictionary so that we can modify the original as we go along
                Dictionary<UUID, AbstractGameModel> lastSelected 
                    = new Dictionary<UUID, AbstractGameModel>(m_lastSelected);
                
                foreach (UUID playerId in lastSelected.Keys)
                {
                    AbstractGameModel m = lastSelected[playerId];
                    
                    foreach (AbstractGameAsset removedAsset in removedAssets)
                    {
                        if (removedAsset.Uuid == m.Uuid)
                            SetLastSelected(playerId, removedAsset.Field);                            
                    }                
                }
            }            
        }

        public Hashtable HandleRequest(string requestUri, Hashtable request)
        {            
            string method = "get";
            if (request.ContainsKey("_method"))
                method = ((string)request["_method"]).ToLower();
            
//            m_log.InfoFormat("[WATER WARS]: Handling player request for {0}, method {1}", requestUri, method);            

            string[] requestComponents = requestUri.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);                                    

            Hashtable reply = null;

            if (requestComponents.Length == 1)
            {                
                reply = HandleGetPlayers();
            }
            else if (requestComponents.Length == 2)
            {
                string rawPlayerId = requestComponents[1];
                UUID playerId = new UUID(rawPlayerId);

                if ("put" == method)
                    reply = HandlePlayerChange(playerId, request);
                else
                    reply = HandleGetPlayer(playerId);                
            }
            else if (requestComponents.Length > 2)
            {                                
                string rawPlayerId = requestComponents[1];
                UUID playerId = new UUID(rawPlayerId);
                string requestType = requestComponents[2];                
                
//                m_log.InfoFormat(
//                    "[WATER WARS]: Handling player webservice request [{0}] from {1}", requestType, playerId);                
                
                if (requestType == SELECTED_PATH)
                {
                    if ("get" == method)
                    {
                        reply = HandleGetLastSelected(playerId);
                    }
                    else
                    {
                        string[] remainingRequestComponents = new string[requestComponents.Length - 3];
                        Array.Copy(requestComponents, 3, remainingRequestComponents, 0, requestComponents.Length - 3);
                        reply = HandleSetLastSelected(playerId, remainingRequestComponents);
                    }
                }
                else if (requestType == LOGIN_PATH)
                {                    
                    reply = HandleLogin(playerId, request);
                }
                else if (requestType == NEWS_PATH)
                {
                    reply = HandleGetNews(playerId);
                }
                else if (requestType == UPDATE_PATH)
                {
                    reply = HandleGetUpdate(playerId, request);
                }                
            }

            if (null == reply)
            {
                reply = new Hashtable();
                m_log.WarnFormat("[WATER WARS]: Received unknown player service request {0}", requestComponents[1]);
                reply["int_response_code"] = 404;
                reply["str_response_string"] = string.Empty;
                reply["content_type"] = "text/plain";                              
            }       

            return reply;
        }   
        
        protected Hashtable HandlePlayerChange(UUID playerId, Hashtable request)
        {
            JObject json = GetJsonFromPost(request);            
            bool sellWaterRights = false;
            bool sellWater = false;
            bool requestWaterRights = false;
            bool requestWater = false;                        
            
            string rawWaterBuyerId = (string)json.SelectToken("WaterBuyer.Uuid.Guid");                
            string rawWaterRightsBuyerId = (string)json.SelectToken("WaterRightsBuyer.Uuid.Guid");            
            int? rightsAmount = (int?)json.SelectToken("DeltaWaterEntitlement");
            int? waterAmount = (int?)json.SelectToken("DeltaWater");
            int? price = -(int?)json.SelectToken("DeltaMoney");            
            
            if (rightsAmount != null)
                if (rawWaterRightsBuyerId != null)
                    sellWaterRights = true;
                else
                    requestWaterRights = true;
            
            if (waterAmount != null)
                if (rawWaterBuyerId != null)
                    sellWater = true;
                else
                    requestWater = true;            
            
            if (sellWaterRights)
                return HandleSellWaterRights(playerId, UUID.Parse(rawWaterRightsBuyerId), (int)rightsAmount, (int)price);
            
            if (sellWater)
                return HandleSellWater(playerId, UUID.Parse(rawWaterBuyerId), (int)waterAmount, (int)price);          
            
            if (requestWaterRights)
                return HandleRequestWaterRights(playerId, (int)rightsAmount);
            
            if (requestWater)
                return HandleRequestWater(playerId, (int)waterAmount);
                        
            m_log.WarnFormat("[WATER WARS]: Received unknown player change request from {0}", playerId);
            Hashtable reply = new Hashtable();
            reply["int_response_code"] = 404;
            reply["str_response_string"] = string.Empty;
            reply["content_type"] = "text/plain";                              
            
            return reply;
        }

        protected Hashtable HandleGetNews(UUID playerId)
        {
            Hashtable reply = new Hashtable();

            reply["int_response_code"] = 200;
            reply["str_response_string"] = GetNews(playerId);

            // The type has to be set for the browser (at least firefox) to intrepret the feed propertly as rss
            reply["content_type"] = "application/rss+xml";
                        
            return reply;
        }

        protected string GetNews(UUID playerId)
        {
            string news = m_controller.Feeds.GetSerializedFeed(playerId);

            //m_log.InfoFormat("[WATER WARS]: News for {0} was {1}", playerId, news);
            
            return news;
        }
        
        protected Hashtable HandleGetPlayer(UUID playerId)
        {
            Hashtable reply = new Hashtable();
            string json = "";
            
            IDictionary<UUID, Player> players = m_controller.Game.Players;

            lock (players)
            {
                if (players.ContainsKey(playerId))
                    json = JsonConvert.SerializeObject(players[playerId], Newtonsoft.Json.Formatting.Indented);
            }

            reply["int_response_code"] = 200; // 200 OK
            reply["str_response_string"] = json;
            reply["content_type"] = "text/plain";

            //System.Console.WriteLine(json);

            return reply;                 
        }
        
        protected Hashtable HandleGetPlayers()
        {
            Hashtable reply = new Hashtable();
            string json;
            
            IDictionary<UUID, Player> players = m_controller.Game.Players;

            lock (players)
                json = JsonConvert.SerializeObject(players.Values, Newtonsoft.Json.Formatting.Indented);

            reply["int_response_code"] = 200; // 200 OK
            reply["str_response_string"] = json;
            reply["content_type"] = "text/plain";

            //System.Console.WriteLine(json);

            return reply;            
        }
        
        protected Hashtable HandleGetLastSelected(UUID playerId)
        {
            Hashtable reply = new Hashtable();
                
            reply["int_response_code"] = 200; // 200 OK
            reply["str_response_string"] = Serialize(GetLastSelected(playerId, false));
            reply["content_type"] = "text/plain";                        

            return reply;
        }
        
        /// <summary>
        /// Allow a game model to be selected via the webservice, not just from in-world
        /// </summary>
        /// 
        /// FIXME: This currently only allows a player to be selected.
        /// 
        /// <param name="playerId"></param>
        /// <param name="components"></param>
        /// <returns></returns>
        protected Hashtable HandleSetLastSelected(UUID playerId, string[] components)
        {
//            m_log.InfoFormat(
//                "[WATER WARS]: Handling SetLastSelected for {0}, path [{1}]", playerId, string.Join("/", components));
            
            Hashtable reply = new Hashtable();
            
            reply["int_response_code"] = 200; // 200 OK
            reply["str_response_string"] = "nada";
            reply["content_type"] = "text/plain";             
            
            if (components[0] != "player")
                return reply;
            
            UUID selectedId = UUID.Parse((string)components[1]);
            
            AbstractGameModel agm = null;
            Dictionary<UUID, Player> players = m_controller.Game.Players;
            lock (players)
            {
                if (players.ContainsKey(selectedId))
                    agm = players[selectedId];
            }
            
            if (null != agm)                            
                SetLastSelected(playerId, agm);
            
            // TODO: Error response
            
            return reply;
        }

        /// <summary>
        /// Get the game model that the player last selected in-world.
        /// </summary>
        /// <param name="playerId"></param>
        /// <param name="wait">If true, then wait for a player selection (no wait will occur if the player has already
        /// previously selected something that hasn't been fetched).  If false, then always return the last selected
        /// item</param>
        /// <returns></returns>
        public AbstractGameModel GetLastSelected(UUID playerId, bool wait)
        {
//            m_log.InfoFormat("[WATER WARS]: Received last selected request from {0}, wait {1}", playerId, wait);
            
            AbstractGameModel lastSelected = null;
            
            lock (m_lastSelected)
            {
                // If we receive a selection request for a non-player (e.g. an observer) then create their selection
                // queue dynamically.  We don't yet do this for players since they may perform a selection before
                // fetching it (this could probably be resolved by also creating dynamically upon set if no selection
                // has been set up for the user id
                if (!m_lastSelected.ContainsKey(playerId))
                {
//                    m_log.InfoFormat("[WATER WARS]: Received last selected request from non-player {0}", playerId);
                    m_lastSelected[playerId] = AbstractGameAsset.None;
                    m_lastSelectedSyncObjects[playerId] = new Object();                    
                }
            }

            if (wait)
            {
                Object o = m_lastSelectedSyncObjects[playerId];
                
                lock (o)
                    Monitor.Wait(o);
            }
                
            lock (m_lastSelected)
                lastSelected = m_lastSelected[playerId];                                              
            
//            m_log.InfoFormat(
//                "[WATER WARS]: Returning last selected [{0}] type {1} for {2}", 
//                lastSelected.Name, lastSelected.Type, playerId);            
            
//            if (lastSelected is AbstractGameAsset)
//                m_log.InfoFormat("[WATER WARS]: Returning last selected [{0}] has game state {1}", lastSelected, ((AbstractGameAsset)lastSelected).Game.State);            
            
            return lastSelected;
        }
        
        protected Hashtable HandleGetUpdate(UUID playerId, Hashtable request)
        {
            Hashtable reply = new Hashtable();

            bool wait = false;
            bool.TryParse((string)request["wait"], out wait);
            
            reply["int_response_code"] = 200;
            reply["str_response_string"] 
                = JsonConvert.SerializeObject(
                    new Update(GetLastSelected(playerId, wait), GetNews(playerId)), Newtonsoft.Json.Formatting.Indented);
            reply["content_type"] = "text/plain";

            return reply;
        }
        
        protected Hashtable HandleSellWater(UUID sellerId, UUID buyerId, int amount, int price)
        {
            m_controller.Resolver.SellWater(sellerId, buyerId, amount, price);

            // TODO: Deal with error situations: uuid not valid, no such buy point, not enough money, etc.

            Hashtable reply = new Hashtable();
            reply["int_response_code"] = 200; // 200 OK
            reply["str_response_string"] = "hoorahaz";
            reply["content_type"] = "text/plain";            

            return reply;               
        }        
        
        protected Hashtable HandleSellWaterRights(UUID sellerId, UUID buyerId, int amount, int price)
        {
            Hashtable reply = new Hashtable();           
            
            Player buyer, seller;
            lock (m_controller.Game.Players)
            {
                buyer = m_controller.Resolver.GetPlayer(buyerId);                
                seller = m_controller.Resolver.GetPlayer(sellerId);
            }
            
            m_controller.Resolver.SellWaterRights(buyer, seller, amount, price);
            
            reply["int_response_code"] = 200; // 200 OK
            reply["str_response_string"] = "eggs";
            reply["content_type"] = "text/plain";            
            
            return reply;          
        }
        
        protected Hashtable HandleRequestWaterRights(UUID requesterId, int amount)
        {
            Hashtable reply = new Hashtable();    
            m_controller.Resolver.RequestWaterRights(requesterId, amount);
            
            reply["int_response_code"] = 200; // 200 OK
            reply["str_response_string"] = "nog";
            reply["content_type"] = "text/plain";            
            
            return reply;          
        }        
        
        protected Hashtable HandleRequestWater(UUID requesterId, int amount)
        {
            Hashtable reply = new Hashtable();    
            m_controller.Resolver.RequestWater(requesterId, amount);
            
            reply["int_response_code"] = 200; // 200 OK
            reply["str_response_string"] = "nug";
            reply["content_type"] = "text/plain";            
            
            return reply;          
        }          

        protected Hashtable HandleLogin(UUID playerId, Hashtable request)
        {
            Hashtable reply = new Hashtable();

            // We have to give some response string otherwise the return code crashes
            reply["str_response_string"] = "";            

            UUID loginToken = UUID.Zero;

            lock (m_loginTokens)
                m_loginTokens.TryGetValue(playerId, out loginToken);

            bool passed = false;
            
            if (!WaterWarsConstants.IS_WEBSERVICE_SECURITY_ON)
            {
                passed = true;
            }
            else
            {            
                UUID submittedLoginToken = WaterWarsUtils.ParseRawId((string)request["loginToken"]);
    
                if (loginToken == submittedLoginToken)
                    passed = true;
            }

            if (passed)
            {
                reply["int_response_code"] = 200;

                lock (m_loginTokens)
                    m_loginTokens.Remove(playerId);

                // TODO: Establish the second authorization token                
            }
            else
            {
                reply["int_response_code"] = 401;
            }

            return reply;
        }

        /// <summary>
        /// Clear the last selected object for the given player
        /// </summary>
        /// <param name="playerId"></param>
        public void ClearLastSelected(UUID playerId)
        {
            SetLastSelected(playerId, AbstractGameAsset.None);
        }
        
        /// <summary>
        /// Set the game object last selected by the player in-world
        /// </summary>
        /// <param name="playerId"></param>
        /// <param name="model"></param>
        public void SetLastSelected(UUID playerId, AbstractGameModel m)
        {
//            m_log.InfoFormat("[WATER WARS]: Setting last selected for {0} to {1} type {2}", playerId, m.Name, m.Type);
                        
            Player p = null;
            lock (m_controller.Game.Players)            
                m_controller.Game.Players.TryGetValue(playerId, out p);
            
//            m_log.InfoFormat(
//                "[WATER WARS]: In SetLastSelected found {0} for uuid {1} when trying to set {2} type {3}", 
//                p != null ? p.Name : null, playerId, m.Name, m.Type);
            
            // XXX: At the moment, we don't serve selections to non-players            
            if (null == p)
                return;
                                    
            lock (m_lastSelected)
            {
                // Remove our previous change listener
                if (m_lastSelectedChangeDelegates.ContainsKey(playerId))
                    m_lastSelected[playerId].OnChange -= m_lastSelectedChangeDelegates[playerId];
                
                // Set up a change listener so that any alterations to the model get propogated to the media browser
                // (the view)
                m_lastSelected[playerId] = m;
                m_lastSelectedChangeDelegates[playerId] 
                    = delegate(AbstractModel model) 
                    {
                        // XXX: Probably not required now that we have stopped listening to state changes and are
                        // firing general change events manually to avoid race conditions and multiple updates.
//                        // If the game is in the revenue state while transitioning from the water state back
//                        // to the build state then ignore any game asset updates.  
//                        // The reason for this is that the revenue changes will fire game asset updates, but
//                        // these object updates will not include build owner actions.  While the client is updating and before
//                        // it makes the next long poll, the subsequent start build stage event can fire.  However, this event
//                        // will not be seen by the client and the user will end up with no build actions until they reselect
//                        // the game asset.
//                        if (m_controller.Game.State == GameStateType.Revenue)
//                        {
////                            m_log.InfoFormat(
////                                "[WATER WARS]: Ignoring change of {0} for {1} since state is {2}", 
////                                model.Name, playerId, m_controller.Game.State);
//                                return;
//                        }

//                        m_log.InfoFormat(
//                            "[WATER WARS]: Pulsing change of {0} for {1} since state is {2}", 
//                            model.Name, playerId, m_controller.Game.State);                        
                    
                        Object obj = m_lastSelectedSyncObjects[playerId];
                    
                        lock (obj)
                            Monitor.PulseAll(obj);
                    };
                                
                m_controller.EventManager.TriggerGameModelSelected(p, m);
                m.OnChange += m_lastSelectedChangeDelegates[playerId];                
            }

            // Pulse anybody waiting since the entire selected object has changed.
            // No need to lock since m_lastSelectedSyncObjects is only set on initialization
            Object o = m_lastSelectedSyncObjects[playerId];
            
            lock (o)
                Monitor.PulseAll(o);
            //m_lastSelectedResetEvents[playerId].Set();
        }

        /// <summary>
        /// Register a login token for a particular user.  This is only good for one login.
        /// </summary>
        /// <param name="p"></param>
        /// <returns>The login token created</returns>
        public UUID CreateLoginToken(UUID userId)
        {
            UUID token = UUID.Random();
            
            lock (m_loginTokens)
                m_loginTokens[userId] = token;

            return token;
        }        
        
        protected string Serialize(AbstractModel model)
        {
            return JsonConvert.SerializeObject(model, Newtonsoft.Json.Formatting.Indented);
        }
    }
}