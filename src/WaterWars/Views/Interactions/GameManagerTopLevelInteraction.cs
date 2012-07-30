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
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using log4net;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using WaterWars;
using WaterWars.Models;
using WaterWars.Models.Roles;
using WaterWars.States;
using WaterWars.Views.Widgets;

namespace WaterWars.Views.Interactions
{    
    public class GameManagerTopLevelInteraction : WaterWarsInteraction
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        protected static string HUD_OPTION = "Get hud";
        protected static string INFO_OPTION = "Game info";
        protected static string INSTRUCTIONS_OPTION = "Help";
        protected static string REGISTER_OPTION = "Register";
        protected static string END_GAME_OPTION = "End game";
        protected static string START_GAME_OPTION = "Start game";
        protected static string RESET_GAME_OPTION = "Reset";
        
        /// <summary>
        /// The text that will appear if a menu option is not available
        /// </summary>
        protected static string DISABLED_OPTION = "";
        
        protected static string HUD_ITEM_NAME = "Water Wars HUD";
        protected static string INSTRUCTIONS_ITEM_NAME = "Water Wars Instructions";
        
        /// <value>
        /// UUID of the player taking part in this interaction.  We may not have a Water Wars player object yet
        /// since this may be pre-registration
        /// </value>
        protected UUID m_playerId;
        
        /// <summary>
        /// We're going to keep a separate reference to this button so that we can tell which menu options should be
        /// illuminated
        /// </summary>
        protected GameManagerView.GameManagerButton m_gmButton;
        
        public GameManagerTopLevelInteraction(
            WaterWarsController controller, UUID playerId, GameManagerView.GameManagerButton button) 
            : base(controller, playerId, button)
        {
            m_playerId = playerId;
            m_gmButton = button;
            
            AskTopLevelChoice();
        }
        
        protected void AskTopLevelChoice()
        {  
            List<string> options = new List<string>();
            
            // Row 3
            if (m_controller.State is RegistrationState)
                options.Add(REGISTER_OPTION);
            else
                options.Add(DISABLED_OPTION);
                        
            if (m_controller.State is RegistrationState 
                && m_controller.Game.Players.Count > 0
                && m_controller.Groups.IsPlayerAnAdmin(m_playerId))
                options.Add(START_GAME_OPTION);
            else
                options.Add(DISABLED_OPTION);
            
            if (m_controller.Groups.IsPlayerAnAdmin(m_playerId))                
                options.Add(RESET_GAME_OPTION);
            else
                options.Add(DISABLED_OPTION);            
            
            // Row 2
            options.Add(DISABLED_OPTION);
            options.Add(DISABLED_OPTION);
            
            if (m_controller.State is AbstractPlayState && m_controller.Groups.IsPlayerAnAdmin(m_playerId))
                options.Add(END_GAME_OPTION);
            else
                options.Add(DISABLED_OPTION);
                
            // Row 1
            options.Add(INSTRUCTIONS_OPTION);
            options.Add(HUD_OPTION);
            options.Add(INFO_OPTION);
            
            SendDialog(m_playerId, "Please select an option", options, ProcessWhichTopLevelChoice); 
        }

        protected void ProcessWhichTopLevelChoice(OSChatMessage chat)
        {
            if (chat.Message == HUD_OPTION)
                OfferInventory(HUD_ITEM_NAME);
            else if (chat.Message == INSTRUCTIONS_OPTION)
                OfferInventory(INSTRUCTIONS_ITEM_NAME);
            else if (chat.Message == REGISTER_OPTION)
                new RegisterInteraction(m_controller, m_playerId, ButtonMap[m_playerId]);
            else if (chat.Message == START_GAME_OPTION)
                new StartGameInteraction(m_controller, m_playerId, ButtonMap[m_playerId]);
            else if (chat.Message == END_GAME_OPTION)
                EndGame();
            else if (chat.Message == RESET_GAME_OPTION)
                ResetGame();
            else if (chat.Message == INFO_OPTION)
                DisplayGameInfo();
        }       
        
        protected void OfferInventory(string itemName)
        {
            SceneObjectPart sop = ButtonMap[m_playerId].Part;           
            TaskInventoryItem item = sop.Inventory.GetInventoryItems(itemName)[0];
            
            // destination is an avatar
            InventoryItemBase agentItem 
                = sop.ParentGroup.Scene.MoveTaskInventoryItem(m_playerId, UUID.Zero, sop, item.ItemID);

            byte[] bucket = new byte[17];
            bucket[0] = (byte)agentItem.InvType;
            byte[] objBytes = agentItem.ID.GetBytes();
            Array.Copy(objBytes, 0, bucket, 1, 16);

            GridInstantMessage msg 
                = new GridInstantMessage(
                    sop.ParentGroup.Scene,
                    sop.UUID, 
                    sop.Name, 
                    m_playerId,
                    (byte)InstantMessageDialog.InventoryOffered,
                    false, 
                    item.Name + "\n" + sop.Name + " is located at " +
                    sop.ParentGroup.Scene.RegionInfo.RegionName + " " +
                    sop.AbsolutePosition.ToString(),
                    agentItem.ID, 
                    true, 
                    sop.AbsolutePosition,
                    bucket);
            
            m_log.InfoFormat("[WATER WARS]: IMSessionId on sending inventory offer [{0}]", agentItem.ID);

            IMessageTransferModule module = sop.ParentGroup.Scene.RequestModuleInterface<IMessageTransferModule>();
            if (module != null)
            {
                IClientAPI client = sop.ParentGroup.Scene.GetScenePresence(m_playerId).ControllingClient;                
                client.OnInstantMessage += OnInstantMessage;
                module.SendInstantMessage(msg, delegate(bool success) {});          
            }
        }
        
        /// <summary>
        /// Display game information in-world
        /// </summary>
        protected void DisplayGameInfo()
        {
            Scene scene = ButtonMap[m_playerId].Part.ParentGroup.Scene;
            scene.SimChat(string.Format("Current game state is {0}", m_controller.Game.State), WaterWarsConstants.SYSTEM_ANNOUNCEMENT_NAME);
            List<Player> players = m_controller.Game.Players.Values.ToList();
            scene.SimChat(string.Format("There are {0} registered players", players.Count), WaterWarsConstants.SYSTEM_ANNOUNCEMENT_NAME);
            foreach (Player player in players)
            {
                StringBuilder sb = new StringBuilder(string.Format("  {0} [{1}]", player.Name, player.Role.Type));
                                              
                if (m_controller.State is AbstractPlayState)
                {
                    AbstractPlayState aps = m_controller.State as AbstractPlayState;
                    
                    if (aps.HasPlayerEndedTurn(player))
                        sb.Append(" (ended turn)");
                    else
                        sb.Append(" (not ended turn)");
                }            

                scene.SimChat(sb.ToString(), WaterWarsConstants.SYSTEM_ANNOUNCEMENT_NAME);
            }
        }
        
        protected void EndGame()
        {
            m_controller.State.EndGame();            
            
            Scene scene = ButtonMap[m_playerId].Part.ParentGroup.Scene;
            scene.SimChat("End game triggered early.  Game will now end after this round.", WaterWarsConstants.SYSTEM_ANNOUNCEMENT_NAME);             
        }        
        
        protected void ResetGame()
        {
            m_controller.State.ResetGame();            
            
            Scene scene = ButtonMap[m_playerId].Part.ParentGroup.Scene;
            scene.SimChat("Waters wars game reset.  Now open for player registration.", WaterWarsConstants.SYSTEM_ANNOUNCEMENT_NAME);             
        }
        
        protected void OnInstantMessage(IClientAPI client, GridInstantMessage im)
        {
            m_log.InfoFormat("[WATER WARS]: GameManagerTopLevelInteraction.OnInstantMessage received");
            
            if (im.dialog == (byte)InstantMessageDialog.InventoryAccepted
                && client.AgentId == m_playerId)
            {
                m_log.InfoFormat("[WATER WARS]: Now we get to do something cool!  IMSessionId [{0}]", im.imSessionID);
                
                UUID itemId = new UUID(im.imSessionID);
                Scene scene = client.Scene as Scene;                
                
                // This really need to be a 'has inventory item' method in Scene.Inventory.cs
                InventoryItemBase item = new InventoryItemBase(itemId, m_playerId);
                item = scene.InventoryService.GetItem(item);

                if (item == null)
                {
                    m_log.Error("[WATER WARS]: Failed to find item " + itemId);
                    return;
                }
                else if (item.Name != HUD_ITEM_NAME)
                {
                    m_log.InfoFormat(
                        "[WATER WARS]: Ignoring hud item {0} since it's not a {1}", item.Name, HUD_ITEM_NAME);
                    return;
                }
                
                uint attachmentPoint = (uint)AttachmentPoint.HUDTop;
                ScenePresence sp = scene.GetScenePresence(client.AgentId);
                List<SceneObjectGroup> existingAttachments = sp.GetAttachments(attachmentPoint);
                
                if (existingAttachments.Count == 0)
                {                
                    IAttachmentsModule module = client.Scene.RequestModuleInterface<IAttachmentsModule>();
                    SceneObjectGroup sog
                        = module.RezSingleAttachmentFromInventory(
                            sp, new UUID(im.imSessionID), (uint)AttachmentPoint.HUDTop);
                    
                    // A tempoary messy solution to an occasional race where the attached hud sometimes ends up positioned
                    // on the avatar itself and does not show up as attached within inventory.
                    Thread.Sleep(1000);
                    
                    Vector3 newPos = new Vector3(0, 0, -0.1f);
                    m_log.InfoFormat("[WATER WARS]: Resetting HUD position to {0}", newPos);                
                    module.UpdateAttachmentPosition(sog, newPos);
    
                    /*
                    sog.UpdateGroupPosition(newPos);
                    sog.HasGroupChanged = true;
                    sog.ScheduleGroupForTerseUpdate();
                    */
                }
                else
                {
                    m_log.InfoFormat(
                        "[WATER WARS]: Not attaching given hud for {0} since something is already attached at {1}", 
                        client.Name, attachmentPoint);
                }
                
                client.OnInstantMessage -= OnInstantMessage;
            }
        }
    }
}