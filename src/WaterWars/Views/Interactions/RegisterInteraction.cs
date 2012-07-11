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
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;
using WaterWars;
using WaterWars.Models;
using WaterWars.Models.Roles;
using WaterWars.Views.Widgets;

namespace WaterWars.Views.Interactions
{    
    public class RegisterInteraction : WaterWarsInteraction
    {
        public RegisterInteraction(WaterWarsController controller, UUID playerId, OsButton button) 
            : base(controller, playerId, button)
        {            
            bool checksPassed = true;
            
            try
            {
                m_controller.Groups.CheckForRequiredSetup();
            }
            catch (GroupsSetupException e)
            {
                checksPassed = false;
                SendAlert(playerId, string.Format("Could not register to play.  {0}", e.Message));                
            }

            if (!controller.HudManager.HasHud(playerId))
            {
                checksPassed = false;
                SendAlert(playerId, 
                    "Please attach your Water Wars head-up display (HUD) before registering for the game."
                    + "  You can get a hud by clicking the state capital building.");
            }

            if (checksPassed)
            {
                UserAccount ua = m_controller.Scenes[0].UserAccountService.GetUserAccount(UUID.Zero, playerId);
                
                if (!m_controller.Groups.IsPlayerInRequiredGroup(ua))
                {
                    m_controller.Groups.AddPlayerToRequiredGroup(ua);
                    SendAlert(playerId, string.Format("Adding you to group {0}", WaterWarsConstants.GROUP_NAME));
                }
                
                AskWhichRole(playerId);
            }
        }
        
        protected void AskWhichRole(UUID playerId)
        {
            List<string> playableRoles = new List<string>();
            foreach (IRole role in m_controller.Game.Roles)
                if (role.IsPlayable)
                    playableRoles.Add(role.Type.ToString());
            
            SendDialog(playerId, "Which role do you want to play?", playableRoles, ProcessWhichRole); 
        }

        protected void ProcessWhichRole(OSChatMessage chat)
        {
            RoleType role = (RoleType)Enum.Parse(typeof(RoleType), chat.Message);
            Player player = m_controller.Resolver.AddPlayer(chat.Sender.AgentId.ToString(), role);

            Scene scene = ButtonMap[player.Uuid].Part.ParentGroup.Scene;
            scene.SimChat(
                string.Format(
                    "{0} registered for the next game as a {1}", 
                    player.Name, player.Role.Type), WaterWarsConstants.SYSTEM_ANNOUNCEMENT_NAME);           
        }        
    }
}