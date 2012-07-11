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
using System.Reflection;
using log4net;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;

namespace WaterWars
{
    /// <summary>
    /// Interact with the required groups that WaterWars needs.
    /// </summary>   
    public class OpenSimGroupsMediator
    {        
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected WaterWarsController m_controller;
        protected IGroupsModule m_groupsModule;
        protected IGroupsMessagingModule m_groupsMessagingModule;
        
        protected UserAccount m_systemAccount;
        public GroupRecord WaterWarsGroup { get; set; }
        public GroupRecord WaterWarsAdminGroup { get; set; }
        
        public OpenSimGroupsMediator(WaterWarsController controller)
        {
            m_controller = controller;
        }

        /// <summary>
        /// Check that the required groups structure is in place.
        /// </summary>
        /// <exception cref="GroupsException">Thrown if there is something wrong with the groups setup</exception>
        public void CheckForRequiredSetup()
        {           
            if (!WaterWarsConstants.ENABLE_GROUPS)
                return;
            
            if (null == m_groupsModule)
            {
                m_groupsModule = m_controller.Scenes[0].RequestModuleInterface<IGroupsModule>();

                if (null == m_groupsModule)
                    throw new GroupsSetupException("No groups module present on server");
            }

            if (null == m_groupsMessagingModule)
            {
                m_groupsMessagingModule = m_controller.Scenes[0].RequestModuleInterface<IGroupsMessagingModule>();

                if (null == m_groupsMessagingModule)
                    throw new GroupsSetupException("No groups messaging module present on server");
            }

            if (null == WaterWarsGroup)
                WaterWarsGroup = GetGroup(WaterWarsConstants.GROUP_NAME);
            
            if (null == WaterWarsAdminGroup)
                WaterWarsAdminGroup = GetGroup(WaterWarsConstants.ADMIN_GROUP_NAME);
            
            m_systemAccount 
                = m_controller.Scenes[0].UserAccountService.GetUserAccount(
                    UUID.Zero, WaterWarsConstants.SYSTEM_PLAYER_FIRST_NAME, WaterWarsConstants.SYSTEM_PLAYER_LAST_NAME);
            
            string name 
                = string.Format(
                    "{0} {1}", WaterWarsConstants.SYSTEM_PLAYER_FIRST_NAME, WaterWarsConstants.SYSTEM_PLAYER_LAST_NAME);
            
            if (null == m_systemAccount)
                throw new GroupsSetupException(
                    string.Format(
                        "System player {0} not present.  Please create this player before restarting OpenSim", name));
            
            if (!IsPlayerInRequiredGroup(m_systemAccount))
                throw new GroupsSetupException(
                    string.Format(
                        "System player {0} is not in group {1}.  Please correct this.",
                        name, WaterWarsGroup.GroupName));
        }

        /// <summary>
        /// Get the given group.
        /// </summary>
        /// <exception cref="GroupsSetupException">Thrown if the group cannot be found</exception>
        /// <param name="groupName"></param>
        protected GroupRecord GetGroup(string groupName)
        {
            GroupRecord group = m_groupsModule.GetGroupRecord(groupName);

            if (null == group)
                throw new GroupsSetupException(
                    string.Format("Could not find group {0}.  Please create it", groupName));            
            
            return group;
        }
        
        /// <summary>
        /// Add player to the required group for the game
        /// </summary>
        /// <param name="ua"></param>
        /// <returns>true if add was successful, false if not</returns>
        public bool AddPlayerToRequiredGroup(UserAccount ua)
        {
            if (!WaterWarsConstants.ENABLE_GROUPS)
                return true;
            
            m_log.InfoFormat(
                "[WATER WARS]: Adding player {0} to group {1} {2}", 
                ua.Name, WaterWarsGroup.GroupName, WaterWarsGroup.GroupID);

            ScenePresence scenePresence = null;

            // Look for the presence in every scene.  If this kind of thing becomes common we will need to refactor the
            // code
            foreach (Scene scene in m_controller.Scenes)
            {
                ScenePresence sp = scene.GetScenePresence(ua.PrincipalID);
                if (sp != null)
                {
                    scenePresence = sp;
                    break;
                }                                       
            }
            
            if (scenePresence != null)
            {
                m_groupsModule.JoinGroupRequest(scenePresence.ControllingClient, WaterWarsGroup.GroupID);
                return true;
            }

            return false;
        }

        public bool IsPlayerInRequiredGroup(UserAccount ua)
        {        
            if (!WaterWarsConstants.ENABLE_GROUPS)
                return true;
            
            return IsPlayerAMemberOf(WaterWarsGroup.GroupID, ua);
        }
        
        public bool IsPlayerAnAdmin(UUID playerId)
        {
            if (!WaterWarsConstants.ENABLE_GROUPS)
                return true;
            
            return !WaterWarsConstants.CHECK_ADMIN || IsPlayerAMemberOf(WaterWarsAdminGroup.GroupID, playerId);          
        }
        
        public bool IsPlayerAMemberOf(UUID groupId, UserAccount ua)
        {   
            if (!WaterWarsConstants.ENABLE_GROUPS)
                return true;
            
            GroupMembershipData groupData = m_groupsModule.GetMembershipData(groupId, ua.PrincipalID);            
            bool foundPlayer = groupData != null;
            
            if (foundPlayer)
                m_log.InfoFormat(
                    "[WATER WARS]: Found player {0} in group {1} {2}", 
                    ua.Name, groupData.GroupName, groupData.GroupID);
            
            return foundPlayer;
        }
        
        public bool IsPlayerAMemberOf(UUID groupId, UUID playerId)
        {   
            if (!WaterWarsConstants.ENABLE_GROUPS)
                return true;
            
            GroupMembershipData groupData = m_groupsModule.GetMembershipData(groupId, playerId);
            return groupData != null;
        }        

        /// <summary>
        /// Send a message to the water wars group.
        /// </summary>
        /// <param name="message"></param>
        public void SendMessageToGroup(string message)
        {
            if (!WaterWarsConstants.ENABLE_GROUPS)
                return;
            
            UUID myAgentId = m_systemAccount.PrincipalID;
            UUID groupId = WaterWarsGroup.GroupID;
            
            m_groupsMessagingModule.StartGroupChatSession(myAgentId, groupId);
            
            GridInstantMessage msg = new GridInstantMessage();            
            msg.imSessionID = groupId.Guid;
            msg.fromAgentID = myAgentId.Guid;
            msg.fromAgentName = WaterWarsConstants.SYSTEM_ANNOUNCEMENT_NAME;
            msg.message = message;
            msg.dialog = (byte)InstantMessageDialog.SessionSend;
            
            m_groupsMessagingModule.SendMessageToGroup(msg, groupId);
        }
    }

    public class GroupsSetupException : Exception
    {        
        public GroupsSetupException(string message) : base(message) {}
        public GroupsSetupException(string message, Exception e) : base(message, e) {}
    }                
}