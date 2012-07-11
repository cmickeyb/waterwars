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
using System.Reflection;
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Framework.Interfaces;
using WaterWars.Events;
using WaterWars.Models;
using WaterWars.Models.Roles;
using WaterWars.States;
using WaterWars.Views.Interactions;

namespace WaterWars
{
    /// <summary>
    /// Resolve incoming requests in terms of OpenSim concepts and objects.
    /// </summary>    
    /// 
    /// We're routing through this class rather than calling state directly in order to consistently generate
    /// useful error messages if something goes wrong.
    /// 
    /// This class also acts as an intermediary which poses questions to players if they are actively in-world.
    /// Otherwise the request just goes straight through!
    public class OpenSimResolver
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);       
        
        WaterWarsController m_controller;
        
        public OpenSimResolver(WaterWarsController controller)
        {
            m_controller = controller;
        }

        /// <summary>
        /// Add a player
        /// </summary>
        /// <param name="rawPlayerId"></param>
        /// <param name="roleType"></param>
        /// <returns>Player added</returns>
        public Player AddPlayer(string rawPlayerId, RoleType roleType)
        {
            IRole role = null;

            switch (roleType)
            {
                case RoleType.Developer:
                    role = Developer.Singleton;
                    break;
                case RoleType.Farmer:
                    role = Farmer.Singleton;
                    break;
                case RoleType.Manufacturer:
                    role = Manufacturer.Singleton;
                    break;
                case RoleType.WaterMaster:
                    role = WaterMaster.Singleton;
                    break;
            }
                
            UUID playerId = WaterWarsUtils.ParseRawId(rawPlayerId);

            ScenePresence scenePresence = null;

            // Look for the presence in every scene.  If this kind of thing becomes common we will need to refactor the
            // code
            foreach (Scene scene in m_controller.Scenes)
            {
                ScenePresence sp = scene.GetScenePresence(playerId);
                if (sp != null)
                {
                    scenePresence = sp;
                    break;
                }                                       
            }

            if (null == scenePresence)
                throw new Exception(
                    string.Format(
                        "ScenePresence unexpectedly null for player {0} registering for role {1}", playerId, roleType));
                        
            Player newPlayer = m_controller.ModelFactory.CreatePlayer(scenePresence.UUID, scenePresence.Name, role);
            m_controller.State.AddPlayer(newPlayer);
            
            return newPlayer;
        }

        /// <summary>
        /// Buy a game asset.
        /// </summary>
        /// This is called by code which only has the ids available.
        /// <param name="rawBuyPointId"></param>
        /// <param name="rawFieldId"></param>
        /// <param name="level"></param>
        public AbstractGameAsset BuildGameAsset(string rawBuyPointId, string rawFieldId, int level)
        {            
            UUID buyPointId = WaterWarsUtils.ParseRawId(rawBuyPointId);
            UUID fieldId = WaterWarsUtils.ParseRawId(rawFieldId);

            BuyPoint bp = GetBuyPoint(buyPointId);
            Field f = GetField(bp, fieldId);
            Player p = bp.DevelopmentRightsOwner;
            
            return BuildGameAsset(f, p.Role.AllowedAssets[0], level);
        }
        
        /// <summary>
        /// Buy a game asset
        /// </summary>
        /// <param name="f"></param>
        /// <param name="template"></param>
        /// <param name="level"></param>
        public AbstractGameAsset BuildGameAsset(
            Field f, AbstractGameAsset template, int level)
        {
            return m_controller.State.BuildGameAsset(f, template, level);
        }        

        /// <summary>
        /// Buy a game asset.
        /// </summary>
        /// <param name="rawBuyPointId"></param>
        /// <param name="rawAssetId"></param>
        public AbstractGameAsset ContinueBuildingGameAsset(string rawBuyPointId, string rawAssetId)
        {            
            UUID buyPointId = WaterWarsUtils.ParseRawId(rawBuyPointId);
            BuyPoint bp = GetBuyPoint(buyPointId);
                
            UUID assetId = WaterWarsUtils.ParseRawId(rawAssetId);                
            AbstractGameAsset asset = GetAsset(bp, assetId);

            return m_controller.State.ContinueBuildingGameAsset(asset);
        }
        
        /// <summary>
        /// Upgrade a game asset.
        /// </summary>
        /// This is called by code which only has the ids available.
        /// <param name="rawBuyPointId"></param>
        /// <param name="rawPlayerId"></param>
        /// <param name="level"></param>
        public void UpgradeGameAsset(string rawBuyPointId, string rawAssetId, int level)
        {
            UUID buyPointId = WaterWarsUtils.ParseRawId(rawBuyPointId);            
            BuyPoint bp = GetBuyPoint(buyPointId);

            UUID assetId = WaterWarsUtils.ParseRawId(rawAssetId);                
            AbstractGameAsset asset = GetAsset(bp, assetId);

            m_controller.State.UpgradeGameAsset(bp.DevelopmentRightsOwner, asset, level);
        }        

        /// <summary>
        /// Sell a game asset to the economy
        /// </summary>
        /// <param name="rawBuyPointId"></param>
        /// <param name="rawGameAssetId"></param>
        public void SellGameAssetToEconomy(string rawBuyPointId, string rawGameAssetId)
        {
            UUID buyPointId = WaterWarsUtils.ParseRawId(rawBuyPointId);
            UUID gameAssetId = WaterWarsUtils.ParseRawId(rawGameAssetId);

            BuyPoint bp = GetBuyPoint(buyPointId);
            AbstractGameAsset ga = GetAsset(bp, gameAssetId);
            
            m_controller.State.SellGameAssetToEconomy(ga);
        }
        
        /// <summary>
        /// Remove a particular game asset on a parcel
        /// </summary>
        /// <param name="buyPointId"></param>
        /// <param name="rawGameAssetId">
        public Field RemoveGameAsset(string rawBuyPointId, string rawGameAssetId)
        {            
            UUID buyPointId = WaterWarsUtils.ParseRawId(rawBuyPointId);
            UUID gameAssetId = WaterWarsUtils.ParseRawId(rawGameAssetId);

            BuyPoint bp = GetBuyPoint(buyPointId);
            AbstractGameAsset ga = GetAsset(bp, gameAssetId);
            
            return m_controller.State.RemoveGameAsset(ga);
        }
        
        /// <summary>
        /// Register a buy point
        /// </summary>
        /// <param name="buyPointId"></param>
        /// <param name="buyPointName"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        public BuyPoint RegisterBuyPoint(SceneObjectGroup so)
        {
            Vector3 pos = so.AbsolutePosition;
            ILandObject osParcel = so.Scene.LandChannel.GetLandObject(pos.X, pos.Y);
            RegionInfo regionInfo = so.Scene.RegionInfo;
            BuyPoint bp = m_controller.ModelFactory.CreateBuyPoint(so.UUID, so.Name, pos, osParcel, regionInfo);
            
            m_controller.State.RegisterBuyPoint(bp);
            
            return bp;
        }

        /// <summary>
        /// Register a field.
        /// </summary>
        ///
        /// This is only used for registration shortly before deletion, currently.  Not for continuing a restored game.
        /// 
        /// <returns></returns>
        public Field RegisterField(SceneObjectGroup so)
        {
            Vector3 pos = so.AbsolutePosition;
            BuyPoint bpFound = FindBuyPointForPosition(so.Scene, pos);

            if (null == bpFound)
                throw new Exception(string.Format("Could not register field {0} at {1} with any parcel", so.Name, pos));
            
            return m_controller.ModelFactory.CreateField(bpFound, so.UUID, so.Name);
        }

//        public AbstractGameAsset RegisterGameAsset(IGameState state, SceneObjectGroup so, AbstractGameAssetType type)
//        {
//            BuyPoint bpFound = FindBuyPointForPosition(state, so.Scene, pos);
//        }

        /// <summary>
        /// Find the buypoint that covers a given position
        /// </summary>
        /// <param name="pos"></param>
        /// <returns>null if no buypoint was found</returns>
        protected BuyPoint FindBuyPointForPosition(Scene scene, Vector3 pos)
        {
            ILandObject osParcel = scene.LandChannel.GetLandObject(pos.X, pos.Y);

            BuyPoint bpFound = null;
            
            foreach (BuyPoint bp in m_controller.Game.BuyPoints.Values)
            {
                if (bp.Location.Parcel == osParcel)
                {
                    bpFound = bp;
                    break;
                }
            }

            return bpFound;
        }

        /// <summary>
        /// Buy water and development rights for a particular parcel
        /// </summary>
        /// <param name="rawBuyPointId"></param>
        /// <param name="rawPlayerId"></param>
        public void BuyLandRights(string rawBuyPointId, string rawPlayerId)
        {
            UUID playerId = WaterWarsUtils.ParseRawId(rawPlayerId);
            
            Player buyer = GetPlayer(playerId);

            UUID buyPointId = WaterWarsUtils.ParseRawId(rawBuyPointId);

            BuyPoint bp = GetBuyPoint(buyPointId);

            if (buyer.Money < bp.CombinedPrice)
            {
                // A messy abstraction breaking hack to alert the player that they can't go ahead.
                m_controller.HudManager.m_playerIdToHud[buyer.Uuid].m_statusButton.SendAlert(
                    buyer.Uuid, 
                    string.Format(
                        "Can't buy rights because they cost {0}{1} and you only have {2}{3}",
                        WaterWarsConstants.MONEY_UNIT, bp.CombinedPrice, 
                        WaterWarsConstants.MONEY_UNIT, buyer.Money));                     
            }
            else
            {
                m_controller.State.BuyLandRights(bp, buyer);
            }
        }        

        /// <summary>
        /// Sell rights for a particular parcel
        /// </summary>
        /// <param name="bp"></param>
        /// <param name="buyerId"></param>
        /// <param name="rightsType"></param>
        /// <param name="price"></param>
        public void SellRights(BuyPoint bp, UUID buyerId, RightsType rightsType, int price)
        {
            Player buyer = GetPlayer(buyerId);
            Player sellingPlayer = null;

            if (rightsType == RightsType.Water)
                sellingPlayer = bp.WaterRightsOwner;
            else
                sellingPlayer = bp.DevelopmentRightsOwner;
            
            m_log.InfoFormat(
                "[WATER WARS]: Starting process of player {0} selling {1} rights on {2} to {3} for {4}",
                sellingPlayer.Name, rightsType, bp.Name, buyer.Name, price);

            if (m_controller.AttachedToVe)               
                new AskLandBuyerInteraction(
                    m_controller, 
                    m_controller.HudManager.m_playerIdToHud[sellingPlayer.Uuid],
                    m_controller.HudManager.m_playerIdToHud[buyer.Uuid],                                            
                    bp,
                    price,
                    rightsType);
            else
                m_controller.State.SellRights(bp, buyer, rightsType, price);
        }
        
        /// <summary>
        /// Sell Water Rights from the player pool.
        /// </summary>
        /// <param name="bp"></param>
        /// <param name="buyerId"></param>
        /// <param name="rightsType"></param>
        /// <param name="price"></param>
        public void SellWaterRights(Player buyer, Player seller, int price, int amount)
        {            
            m_log.InfoFormat(
                "[WATER WARS]: Starting process of player {0} selling {1} waters rights to {2} for {3}",
                seller.Name, amount, buyer.Name, price);

            if (m_controller.AttachedToVe)               
                new AskWaterRightsBuyerInteraction(
                    m_controller, 
                    m_controller.HudManager.m_playerIdToHud[seller.Uuid],
                    m_controller.HudManager.m_playerIdToHud[buyer.Uuid],                                            
                    amount,
                    price);
            else
                m_controller.State.SellWaterRights(buyer, seller, price, amount);
        }     
        
        /// <summary>
        /// Request water rights from other players
        /// </summary>
        /// <param name="requesterId"></param>
        /// <param name="amount"></param>
        public void RequestWaterRights(UUID requesterId, int amount)
        {          
            Player requester = GetPlayer(requesterId);
            
            m_log.InfoFormat(
                "[WATER WARS]: Starting process of player {0} requesting {1} water rights",
                requester.Name, amount);
            
            m_controller.Events.PostToAll(
                string.Format("{0} would like to buy rights to {1}", 
                    requester.Name, WaterWarsUtils.GetWaterUnitsText(amount)),
                EventLevel.Alert);
        }     
        
        /// <summary>
        /// Request water from other players
        /// </summary>
        /// <param name="requesterId"></param>
        /// <param name="amount"></param>
        public void RequestWater(UUID requesterId, int amount)
        {          
            Player requester = GetPlayer(requesterId);
            
            m_log.InfoFormat(
                "[WATER WARS]: Starting process of player {0} requesting {1} water",
                requester.Name, amount);
            
            m_controller.Events.PostToAll(
                string.Format("{0} would like to lease {1}", 
                    requester.Name, WaterWarsUtils.GetWaterUnitsText(amount)),
                EventLevel.Alert);
        }           

        /// <summary>
        /// Sell water available on a particular parcel
        /// </summary>
        /// <param name="sellerId"></param>
        /// <param name="buyerId"></param>
        /// <param name="amount"></param>
        /// <param name="price"></param>
        public void SellWater(UUID sellerId, UUID buyerId, int amount, int price)
        {
            Player buyer = GetPlayer(buyerId);
            Player seller = GetPlayer(sellerId);

            if (m_controller.AttachedToVe)               
            {
                if (amount > seller.Water)
                {
                    // A messy abstraction breaking hack to alert the player that they can't go ahead.
                    m_controller.HudManager.m_playerIdToHud[seller.Uuid].m_statusButton.SendAlert(
                        seller.Uuid, 
                        string.Format(
                            "Can't sell {0} to {1} since you only have {2}",
                            WaterWarsUtils.GetWaterUnitsText(amount), buyer.Name, 
                            WaterWarsUtils.GetWaterUnitsText(seller.Water)));
                }
                else
                {
                    new AskWaterBuyerInteraction(
                        m_controller, 
                        m_controller.HudManager.m_playerIdToHud[seller.Uuid],
                        m_controller.HudManager.m_playerIdToHud[buyer.Uuid],  
                        amount,
                        price);
                }
            }
            else
            {
                m_controller.State.SellWater(seller, buyer, amount, price);
            }
        }
        
        /// <summary>
        /// Use available water on a particular asset on a particular parcel
        /// </summary>
        /// <param name="rawBuyPointId"></param>
        /// <param name="rawAssetId"></param>
        /// <param name="rawPlayerId">Temporarily, this can be UUID.Zero if no player id was supplied in the request</param>
        /// <param name="amount">
        /// Amount of water to use.  If this is zero and there already is some water allocated then this signals undo
        /// </param>
        public void UseWater(string rawBuyPointId, string rawAssetId, string rawPlayerId, int amount)
        {
            UUID buyPointId = WaterWarsUtils.ParseRawId(rawBuyPointId);
            UUID assetId = WaterWarsUtils.ParseRawId(rawAssetId);
            UUID playerId = WaterWarsUtils.ParseRawId(rawPlayerId);

            BuyPoint bp = GetBuyPoint(buyPointId);            
            AbstractGameAsset a = GetAsset(bp, assetId);

            if (0 == amount)
            {
                m_controller.State.UndoUseWater(a);
            }
            else
            {            
                Player p = null;
    
                if (playerId != UUID.Zero)
                    p = GetPlayer(playerId);
                else
                    p = bp.DevelopmentRightsOwner;
                          
                m_controller.State.UseWater(a, p, amount);
            }
        }
        
        public BuyPoint GetBuyPoint(string rawUuid)
        {
            return GetBuyPoint(WaterWarsUtils.ParseRawId(rawUuid));
        }  
        
        public Player GetPlayer(string rawUuid)
        {
            return GetPlayer(WaterWarsUtils.ParseRawId(rawUuid));
        }        
        
        public AbstractGameAsset GetAsset(BuyPoint bp, string rawUuid)
        {
            return GetAsset(bp, WaterWarsUtils.ParseRawId(rawUuid));   
        }      
        
        public Field GetField(BuyPoint bp, string rawUuid)
        {
            return GetField(bp, WaterWarsUtils.ParseRawId(rawUuid));
        }         

        public BuyPoint GetBuyPoint(UUID uuid)
        {
            IDictionary<UUID, BuyPoint> buypoints = m_controller.Game.BuyPoints;
            
            lock (buypoints)
            {
                if (!buypoints.ContainsKey(uuid))
                    throw new Exception(string.Format("BuyPoint with id {0} does not exist", uuid));
                else
                    return buypoints[uuid];
            }
        }        

        public Player GetPlayer(UUID uuid)
        {
            IDictionary<UUID, Player> players = m_controller.Game.Players;
            
            lock (players)
            {
                if (!players.ContainsKey(uuid))
                    throw new Exception(string.Format("Player with id {0} does not exist", uuid));
                else
                    return players[uuid];
            }
        }
        
        public AbstractGameAsset GetAsset(BuyPoint bp, UUID uuid)
        {
            lock (bp.GameAssets)
            {
                if (!bp.GameAssets.ContainsKey(uuid))
                    throw new Exception(string.Format("Game asset with id {0} in buypoint {1} does not exist", uuid, bp.Name));
                else
                    return bp.GameAssets[uuid];
            }     
        }
        
        public Field GetField(BuyPoint bp, UUID uuid)
        {
            lock (bp.Fields)
            {
                if (!bp.Fields.ContainsKey(uuid))
                    throw new Exception(string.Format("Field with id {0} in buypoint {1} does not exist", uuid, bp.Name));
                else
                    return bp.Fields[uuid];
            }
        }               
    }
}
