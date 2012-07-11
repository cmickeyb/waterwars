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
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using log4net;
using Nini.Config;
using OpenMetaverse;
using System.Reflection;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;
using WaterWars.Events;
using WaterWars.Feeds;
using WaterWars.Models;
using WaterWars.Persistence;
using WaterWars.Persistence.Recorder;
using WaterWars.Rules;
using WaterWars.Rules.Allocators;
using WaterWars.Rules.Distributors;
using WaterWars.Rules.Generators;
using WaterWars.States;
using WaterWars.Views;
using WaterWars.WebServices;

namespace WaterWars
{
    /// <summary>
    /// Commands that can be executed against Water Wars from outside the game.
    /// </summary>
    public class WaterWarsCommands
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        protected WaterWarsController m_controller;
        
        public WaterWarsCommands(WaterWarsController controller)
        {
            m_controller = controller;
        }        

        /// <summary>
        /// Add plinths to the given scene.
        /// </summary>
        /// <param name="scene"></param>
        public void AddPlinths(Scene scene)
        {
            List<ILandObject> parcels = scene.LandChannel.AllParcels();

            // We want to place the plinth a little back from the corner
            Vector3 rezAdjustment = new Vector3(-4, -4, 0);

            foreach (ILandObject lo in parcels)
            {
                Vector3 swPoint, nePoint;
                WaterWarsUtils.FindSquareParcelCorners(lo, out swPoint, out nePoint);

                Vector3 rezPoint = nePoint + rezAdjustment;

                //BuyPoint bp = Resolver.RegisterBuyPoint(State, so);
                m_controller.GameManagerView.CreateBuyPointView(scene, rezPoint);
//                Dispatcher.RegisterBuyPointView(bpv);
//                State.UpdateBuyPointStatus(bp);                
            }
        }

        /// <summary>
        /// Remove plinths from the given scene.
        /// </summary>
        /// <param name="scene"></param>
        public void RemovePlinths(Scene scene)
        {
            IDictionary<UUID, BuyPoint> buyPoints = m_controller.Game.BuyPoints;

            lock (buyPoints)
            {
                foreach (BuyPoint bp in buyPoints.Values)
                {
                    if (bp.Location.RegionName == scene.RegionInfo.RegionName)
                    {
                        m_log.InfoFormat("[WATER WARS]: Removing buy point {0} in region {1}", bp.Name, bp.Location.RegionName);
                        m_controller.Dispatcher.RemoveBuyPointView(bp);
                    }
                }
            }
        }  
        
        /// <summary>
        /// End the turn of the given player.
        /// </summary>
        /// <param name="p"></param>
        public void EndTurn(Player p)
        {
            m_controller.State.EndTurn(p.Uuid);
        }
        
        /// <summary>
        /// Give money to a player.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="amount"></param>
        public void GiveMoney(Player p, int amount)
        {
            m_controller.State.GiveMoney(p, amount);
        }
        
        /// <summary>
        /// Give water to a player.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="amount"></param>
        public void GiveWater(Player p, int amount)
        {
            m_controller.State.GiveWater(p, amount);
        }        
        
        /// <summary>
        /// Give water rights to a player.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="amount"></param>
        public void GiveWaterRights(Player p, int amount)
        {
            m_controller.State.GiveWaterRights(p, amount);
        }
        
        /// <summary>
        /// Randomly buys parcels for players.
        /// </summary>
        /// This exists only for testing
        /// <param name="parcelsToBuy">The total number of parcels to buy for all players.  Asset numbers are still uncontrolled</param>
        public void BuyRandom(int parcelsToBuy)
        {
            if (parcelsToBuy < 0)
                throw new Exception("Number of parcels to buy must be greater than 0");
            
            int boughtCount = 0;
            int unownedCount = 0;
            
            List<BuyPoint> buyPoints;            
            lock (m_controller.Game.BuyPoints)
                buyPoints = m_controller.Game.BuyPoints.Values.ToList();
            
            List<Player> players;
            lock (m_controller.Game.Players)
                players = m_controller.Game.Players.Values.ToList();
                
            foreach (BuyPoint bp in buyPoints)
                if (bp.DevelopmentRightsOwner == Player.None)
                    unownedCount++;                
            
            // Constrained the number of parcels to buy to the number actually available
            if (unownedCount < parcelsToBuy)
                parcelsToBuy = unownedCount;
            
            m_log.InfoFormat("[WATER WARS]: Buy random buying {0} parcels", parcelsToBuy);
            
            try
            {
                while (boughtCount < parcelsToBuy)
                {
                    foreach (Player p in players)
                    {           
                        AbstractGameAsset templateAsset = p.Role.AllowedAssets[0];
                        BuyPoint bp = buyPoints[WaterWarsUtils.Random.Next(0, buyPoints.Count)];
                        
                        if (bp.DevelopmentRightsOwner == Player.None)
                        {
                            m_controller.State.BuyLandRights(bp, p);
                            boughtCount++;
                            
                            int assetsToBuy;                            
                            lock (bp.Fields)
                                assetsToBuy = WaterWarsUtils.Random.Next(0, bp.Fields.Count) + 1;
                            
                            while (assetsToBuy-- > 0)
                            {
                                Field f;
                                lock (bp.Fields)
                                    f = bp.Fields.Values.ToList()[WaterWarsUtils.Random.Next(0, bp.Fields.Count)];
                                
                                bool foundExistingField = false;
                                
                                lock (bp.GameAssets)
                                {
                                    // FIXME: This is the only way we can currently determine in game logic if a field
                                    // is currently visible or not!
                                    foreach (AbstractGameAsset ga in bp.GameAssets.Values)
                                        if (ga.Field == f)
                                            foundExistingField = true;
                                }
                                
                                if (!foundExistingField)                                
                                    m_controller.State.BuildGameAsset(
                                        f, templateAsset, WaterWarsUtils.Random.Next(templateAsset.MinLevel, templateAsset.MaxLevel + 1));
                            }
                            
                            break;
                        }
                    }
                }
            }
            catch (WaterWarsGameLogicException e)
            {
                // BuyLandRights throws this exception if player does not have enough money or for other 
                // conditions.  This is not yet consistent across the board (e.g. BuyGameAsset doesn't do this).
                m_log.WarnFormat(
                    "[WATER WARS]: Buy random command finished with {0}{1}", e.Message, e.StackTrace);
            }
            
            m_log.InfoFormat(
                "[WATER WARS]: ww buy random command bought {0} parcels out of {1} unowned", boughtCount, unownedCount);
        }
        
        /// <summary>
        /// Make the terrain for all game parcels completely level.
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="raise">Raise the land?  If this is false then we are lowering</param>
        public void LevelGameParcels(Scene scene, bool raise)
        {
            List<BuyPoint> buyPoints;
            lock (m_controller.Game.BuyPoints)
                buyPoints = m_controller.Game.BuyPoints.Values.ToList();            
            
            m_log.InfoFormat("[WATER WARS]: Leveling parcels in {0}", scene.RegionInfo.RegionName);
            
            foreach (BuyPoint bp in buyPoints)
            {
                ILandObject lo = bp.Location.Parcel;
                
                if (lo.RegionUUID != scene.RegionInfo.RegionID)
                    continue;
                
                Vector3 swPoint, nePoint;
                WaterWarsUtils.FindSquareParcelCorners(lo, out swPoint, out nePoint);                
                
                Vector3 refHeightPoint;
                if (raise)
                    refHeightPoint = new Vector3(0, 0, 0);
                else
                    refHeightPoint = new Vector3(0, 0, 255);
                
                for (int x = (int)Math.Floor(swPoint.X); x <= (int)Math.Floor(nePoint.X); x++)
                {
                    for (int y = (int)Math.Floor(swPoint.Y); y <= (int)Math.Floor(nePoint.Y); y++)
                    {
                        float height = (float)scene.Heightmap[x, y];
                        
                        if (raise)
                        {
                            if (height > refHeightPoint.Z)
                                refHeightPoint = new Vector3(x, y, height);       
                        }
                        else
                        {
                            if (height < refHeightPoint.Z)
                                refHeightPoint = new Vector3(x, y, height);       
                        }
                    }
                }
                
                m_log.InfoFormat(
                    "[WATER WARS]: Found {0} height point for parcel ({1},{2}) at {3}", 
                    (raise ? "max" : "min"), nePoint, swPoint, refHeightPoint);
                
                //refHeightPoint.Z = (float)Math.Ceiling(refHeightPoint.Z);
                
                m_log.InfoFormat(
                    "[WATER WARS]: Setting all terrain on parcel ({0},{1}) to {2}", nePoint, swPoint, refHeightPoint.Z);
                
                for (int x = (int)Math.Floor(swPoint.X); x <= (int)Math.Floor(nePoint.X); x++)
                    for (int y = (int)Math.Floor(swPoint.Y); y <= (int)Math.Floor(nePoint.Y); y++)
                        scene.Heightmap[x, y] = refHeightPoint.Z;
            }
            
            ITerrainModule terrainModule = scene.RequestModuleInterface<ITerrainModule>();
            terrainModule.TaintTerrain();
        }
        
        public void RefreshHuds()
        {
            m_log.InfoFormat("[WATER WARS]: Refreshing all huds.");
            
            m_controller.HudManager.RefreshHuds();
        }
        
        public void ShowHuds()
        {
            string playerIdToHudFormat = "{0,-38}{1,-38}{2,-12}";
            m_log.InfoFormat(playerIdToHudFormat, "Player UUID", "Hud UUID", "Hud Local ID");
            foreach (KeyValuePair<UUID, HudView> kvp in m_controller.HudManager.m_playerIdToHud)
                m_log.InfoFormat(playerIdToHudFormat, kvp.Key, kvp.Value.RootPart.UUID, kvp.Value.RootLocalId);
            
            string localIdToHudFormat = "{0,-38}{1,-8}";
            m_log.InfoFormat(localIdToHudFormat, "Hud Local ID", "Hud UUID");
            foreach (KeyValuePair<uint, HudView> kvp in m_controller.HudManager.m_localIdToHud)
                m_log.InfoFormat(localIdToHudFormat, kvp.Key, kvp.Value.RootPart.UUID);            
        }
        
        public void ShowParcels()
        {
            List<BuyPoint> buyPoints;
            lock (m_controller.Game.BuyPoints)
                buyPoints = m_controller.Game.BuyPoints.Values.ToList();
            
            m_log.InfoFormat("There are {0} parcels", buyPoints.Count);
            
            string tableFormat = "{0,-16}{1,-16}{2,-33}{3,-16}{4,-16}";
            m_log.InfoFormat(tableFormat, "Name", "Region", "Position", "Zone", "Owner");
            
            foreach (BuyPoint bp in buyPoints)
                m_log.InfoFormat(tableFormat, bp.Name, bp.Location.RegionName, bp.Location.LocalPosition, bp.Zone, bp.DevelopmentRightsOwner);
        }
        
        public void ShowStatus()
        {
            m_log.InfoFormat(
                "[WATER WARS]: Game status is {0}, turn {1} of {2}", 
                m_controller.Game.State, m_controller.Game.CurrentRound, m_controller.Game.TotalRounds);
            
            string playersTableFormat = "{0,-32}{1,-14}{2,-17}{3,-14}{4,-14}{5,-7}{6,-8}{7,-12}";
            
            List<Player> players;
            lock (m_controller.Game.Players)
                players = m_controller.Game.Players.Values.ToList();
            
            m_log.InfoFormat("There are {0} players", players.Count);
            m_log.InfoFormat(playersTableFormat, "Player", "Role", "Parcels owned", "Assets owned", "Water rights", "Water", "Cash", "Turn Ended");
            foreach (Player p in players)
            {
                List<BuyPoint> buyPointsOwned;
                lock (p.DevelopmentRightsOwned)
                    buyPointsOwned = p.DevelopmentRightsOwned.Values.ToList();
                
                int assetsCount = 0;
                
                foreach (BuyPoint bp in buyPointsOwned)
                {
                    lock (bp.GameAssets)
                        assetsCount += bp.GameAssets.Count;
                }
                
                string turnEnded;
                
                if (m_controller.State is AbstractPlayState)
                {
                    AbstractPlayState aps = m_controller.State as AbstractPlayState;
                    
                    if (aps.HasPlayerEndedTurn(p))
                        turnEnded = "yes";
                    else
                        turnEnded = "no";
                }                    
                else
                {
                    turnEnded = "n/a";
                }
                
                m_log.InfoFormat(
                    playersTableFormat, p.Name, p.Role.Type, buyPointsOwned.Count, assetsCount, p.WaterEntitlement, p.Water, p.Money, turnEnded);
            }
        }        
    }
}