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
using System.Timers;
using log4net;
using OpenMetaverse;
using OpenSim.Region.Framework.Scenes;
using WaterWars.Models;
using WaterWars.Views;

namespace WaterWars
{
    /// <summary>
    /// Manages player huds
    /// </summary>
    public class HudViewManager
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        protected WaterWarsController m_controller;
        
        /// <value>
        /// Map part local ids to player huds
        /// </value>
        public Dictionary<uint, HudView> m_localIdToHud = new Dictionary<uint, HudView>();

        /// <value>
        /// Map player ids to huds
        /// FIXME: Temporarily public so that we can retrieve a hud to retrieve a button to use in an interaction from
        /// the resolver.
        /// </value>
        public Dictionary<UUID, HudView> m_playerIdToHud = new Dictionary<UUID, HudView>();
        
        public HudViewManager(WaterWarsController controller)
        {
            m_controller = controller;           
        }

        public void Initialise()
        {
            foreach (Scene scene in m_controller.Scenes)
            {
                // Deal with situations where the user directly attaches the hud
                scene.EventManager.OnAttach += OnAttach;                
        
                // Deal with situations where the user enters a sim with their hud already attached
                scene.EventManager.OnIncomingSceneObject += OnIncomingSceneObject;
                //m_scene.EventManager.OnMakeRootAgent += OnMakeRootAgent;
    
                // Deal with situations where the user moves/teleports to a sim within child agent range
//                scene.EventManager.OnMakeChildAgent 
//                    += delegate(ScenePresence sp) 
//                    { 
//                        m_log.InfoFormat(
//                            "[WATER WARS]: Processing OnMakeChildAgent for {0} in {1}", 
//                            sp.Name, sp.Scene.RegionInfo.RegionName);
//                        DeregisterHudByPlayerId(sp.UUID); 
//                    };
    
                // Deal with situations where the user moves/logs off out of child agent range
//                scene.EventManager.OnRemovePresence 
//                    += delegate(UUID uuid)
//                    {
//                        m_log.InfoFormat("[WATER WARS]: Processing OnRemovePresence for {0}", uuid);
//                        DeregisterHudByPlayerId(uuid);
//                    };
            }
        }
        
        protected void OnAttach(uint localID, UUID itemID, UUID avatarID)
        {
            m_log.InfoFormat("[WATER WARS]: Processing attach of {0} {1} for {2}", localID, itemID, avatarID);

            SceneObjectPart part = null;

            // Look for the part in every scene.  If this kind of thing becomes common we will need to refactor the
            // code
            foreach (Scene scene in m_controller.Scenes)
            {
                SceneObjectPart sop = scene.GetSceneObjectPart(localID);
                if (sop != null)
                {
                    part = sop;
                    break;
                }                                       
            }
            
            if (null == part)
                throw new Exception(
                    string.Format(
                        "Unexpectedly could not find attached part with local ID {0} from item {1} for avatar {2} in any scene", 
                        localID, itemID, avatarID));

            if (part.Name != HudView.IN_WORLD_NAME)
                return;            
            
            if (avatarID != UUID.Zero)
            {
                RegisterHud(avatarID, part);
            }
//            else
//            {
//                DeregisterHudByLocalId(part.LocalId);
//            }
        }        
        
        protected void OnIncomingSceneObject(SceneObjectGroup sog)
        {
            m_log.DebugFormat(
                "[WATER WARS]: Processing incoming scene object {0} to {1}", sog.Name, sog.Scene.RegionInfo.RegionName);
            
            if (!sog.IsAttachment || sog.Name != HudView.IN_WORLD_NAME)
                return;

            RegisterHud(sog.OwnerID, sog.RootPart);
        }
        
        protected void OnMakeRootAgent(ScenePresence sp)
        {
            m_log.InfoFormat("[WATER WARS]: Processing OnMakeRootAgent for {0}", sp.Name);
            
            List<SceneObjectGroup> attachments = sp.GetAttachments();

            lock (attachments)
            {
                foreach (SceneObjectGroup sog in attachments)
                {
                    if (sog.Name != HudView.IN_WORLD_NAME)
                        continue;

                    RegisterHud(sp.UUID, sog.RootPart);                     
                }
            }
        }        

        /// <summary>
        /// Register the hud
        /// </summary>
        /// <param name="playerId"></param>
        /// <param name="part"></param>
        protected HudView RegisterHud(UUID playerId, SceneObjectPart part)
        {
            HudView hud;

            lock (m_localIdToHud)
            {
				HudView oldHud = null;
				
                // If a player logs in directly to the region, this will get fired both by the attachment and by the
                // make root agent procedure.
                if (m_playerIdToHud.ContainsKey(playerId))
                    oldHud = DeregisterHudByPlayerId(playerId);
                
                m_log.DebugFormat("[WATER WARS]: Registering hud {0} {1} for {2}", part.Name, part.LocalId, playerId);
                
                hud = new HudView(m_controller, part.ParentGroup.Scene, playerId);
                hud.Initialize(part);
                m_localIdToHud[part.LocalId] = hud;                   
                m_playerIdToHud[playerId] = hud;
				
				if (oldHud != null)
					hud.SetTickerFromPreviousHud(oldHud);

                if (!m_controller.State.UpdateHudStatus(playerId))
                    ResetHud(playerId);

                // An experiment in trying to resolve hud display problems by forcing an update.  Unfortunately, works
                // occasionally but not at all often.
                /*
                Timer forceUpdateTimer = new Timer(2000);
                forceUpdateTimer.AutoReset = false;
                forceUpdateTimer.Elapsed 
                    += new ElapsedEventHandler(
                        delegate(object source, ElapsedEventArgs e) 
                        { 
                            m_log.InfoFormat("[WATER WARS]: Forcing hud client update for {0}", playerId); 
                            part.ParentGroup.SendGroupFullUpdate(); 
                        }
                    );
                forceUpdateTimer.Start();
                */
                
                return hud;
            }
        }

		/// <summary>
		/// Deregister the current hud given a player id
		/// </summary>
		/// <param name="playerId"></param>
		/// <returns>The deregistered hud, null if there was no hud registered for this player.</returns>
        protected HudView DeregisterHudByPlayerId(UUID playerId)
        {            
//            m_log.DebugFormat("[WATER WARS]: Deregistering hud for player {0}", playerId);
            
            lock (m_localIdToHud)
            {
//                // When crossing region borders within the game, we receive the detach event after the attach for the
//                // new region.  To stop things getting complicated, we'll just check whether we can find the player in
//                // any region before taking action.
//                ScenePresence scenePresence = null;
//    
//                // Look for the presence in every scene.  If this kind of thing becomes common we will need to refactor the
//                // code
//                foreach (Scene scene in m_controller.Scenes)
//                {
//                    ScenePresence sp = scene.GetScenePresence(playerId);
//                    if (sp != null)
//                    {
//                        scenePresence = sp;
//                        break;
//                    }                                       
//                }

                // and OnIncomingSceneObject messages arrive the other way around!
                if (m_playerIdToHud.ContainsKey(playerId))
                {                    
                    HudView hud = m_playerIdToHud[playerId];
                    m_localIdToHud.Remove(hud.RootLocalId);
                    m_playerIdToHud.Remove(playerId);
                    hud.Close();
					
					return hud;
                }
            }
			
			return null;
        }

		/// <summary>
		/// Deregister a hud by its local id
		/// </summary>
		/// <param name="localId"></param>
		/// <returns>The deregistered hud, null if there was no hud registered for this local id.</returns>		
        protected HudView DeregisterHudByLocalId(uint localId)
        {
            lock (m_localIdToHud)
            {
                if (m_localIdToHud.ContainsKey(localId))
                {
                    HudView hud = m_localIdToHud[localId];
                    m_localIdToHud.Remove(localId);
                    m_playerIdToHud.Remove(hud.UserId);                
                    hud.Close();
					
					return hud;
                }
            }
			
			return null;
        }

        /// <summary>
        /// Does the player have a registered hud?
        /// </summary>
        /// <param name="playerId"></param>
        /// <returns></returns>
        public bool HasHud(UUID playerId)
        {
            lock (m_localIdToHud)
            {
                return m_playerIdToHud.ContainsKey(playerId);
            }
        }
        
//        public void EnableSellWater(UUID playerId, bool enable)
//        {
//            lock (m_localIdToHud)
//            {
//                // The player might not actually be wearing the hud
//                if (m_playerIdToHud.ContainsKey(playerId))
//                    m_playerIdToHud[playerId].EnableSellWater = enable;
//            }            
//        }
        
        /// <summary>
        /// Signal to the player's hud that it is their turn
        /// </summary>
        /// <param name="playerId"></param>
        public void SendGo(UUID playerId)
        {
            lock (m_localIdToHud)
            {
                // The player might not actually be wearing the hud
                if (m_playerIdToHud.ContainsKey(playerId))
                    m_playerIdToHud[playerId].EnableEndTurn = true;
            }
        }

        /// <summary>
        /// Update the time remaining in all huds
        /// </summary>
        /// <param name="seconds"></param>
        public void UpdateTimeRemaining(int seconds)
        {
            lock (m_localIdToHud)
            {
                foreach (HudView hv in m_localIdToHud.Values)
                {
                    hv.TimeRemaining = seconds;
                }
            }
        }

        /// <summary>
        /// Make the player's hud reflect the fact that they are not in the game
        /// </summary>
        /// <param name="playerId"></param>
        public void ResetHud(UUID playerId)
        {
            lock (m_localIdToHud)
            {
                // The player might not actually be wearing the hud
                if (m_playerIdToHud.ContainsKey(playerId))
                {
                    // This logic should arguably be in the view itself
                    HudView hud = m_playerIdToHud[playerId];
                    hud.EnableEndTurn = false;
//                    EnableSellWater(playerId, false);
                    hud.Money = 0;
                    hud.DevelopmentRightsOwned = 0;
                    hud.Water = 0;
                    hud.WaterEntitlement = 0;
                    hud.Status = WaterWarsController.NOT_IN_GAME_HUD_STATUS_MSG;
                    hud.TimeRemaining = 0;
                }
            }
        }
        
        /// <summary>
        /// Send a player's current status message to their HUD.  Nothing happens if no suitable hud is registered
        /// </summary>
        /// <param name="playerId"></param>
        /// <param name="message"></param>
        public void SendHudStatus(Player player, string message)
        {
            lock (m_localIdToHud)
            {
                // The player might not actually be wearing the hud
                if (m_playerIdToHud.ContainsKey(player.Uuid))
                {
                    // This logic should arguably be in the view itself
                    HudView hud = m_playerIdToHud[player.Uuid];
                    hud.DevelopmentRightsOwned = player.DevelopmentRightsOwned.Count;
                    hud.Money = player.Money;                    
                    hud.Water = player.Water;
                    hud.WaterEntitlement = player.WaterEntitlement;
                    hud.Status = message;
                }
            }
        }  
        
        /// <summary>
        /// Refresh all huds.
        /// </summary>
        /// <remarks>
        /// This shouldn't be required, but might be worth calling manually in pathalogical cases where updates
        /// between the client and the server have got lost.
        /// </remarks>
        public void RefreshHuds()
        {
            lock (m_localIdToHud)
                foreach (HudView hud in m_localIdToHud.Values)
                    hud.Refresh();
        }

        /// <summary>
        /// Send ticker text to the given player.
        /// </summary>
        /// This is usually called via the Events code.
        /// <param name="p"></param>
        /// <param name="text"></param>
        public void SendTickerText(Player p, string text)
        {
            SendTickerText(p.Uuid, text);         
        }
        
        /// <summary>
        /// Send ticker text to the given user.
        /// </summary>
        /// This is usually called via the Events code.
        /// <param name="userId"></param>
        /// <param name="text"></param>
        public void SendTickerText(UUID userId, string text)
        {
            lock (m_localIdToHud)
            {
                // The player might not actually be wearing the hud
                if (m_playerIdToHud.ContainsKey(userId))
                {
                    // This logic should arguably be in the view itself
                    HudView hud = m_playerIdToHud[userId];
                    hud.AddTextToTick(text);
                }
            }            
        }        
        
         /// <summary>
        /// Signal to a player's hud that it is no longer their turn
        /// </summary>
        /// <param name="playerId"></param>        
        public void SendStop(UUID playerId)
        {
            lock (m_localIdToHud)
            {
                // The player might not actually be wearing the hud
                if (m_playerIdToHud.ContainsKey(playerId))
                    m_playerIdToHud[playerId].EnableEndTurn = false;
            }
        }       
    }
}