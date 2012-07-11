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

using System.Collections.Generic;
using System.Reflection;
using log4net;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using WaterWars.Views.Widgets;

namespace WaterWars.Views.Interactions
{
    public abstract class AbstractInteraction
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static string OK_OPTION = "OK";
        public static string CANCEL_OPTION = "Cancel";        
        public static string[] OK_CANCEL_OPTIONS = new string[] { OK_OPTION, CANCEL_OPTION };

        protected static string YES_OPTION = "Yes";
        protected static string NO_OPTION = "No";
        protected static string[] YES_NO_OPTIONS = new string[] { YES_OPTION, NO_OPTION };        

        /// <value>
        /// The player id from which we're awaiting a reply
        /// </value>
        protected UUID m_awaitingReplyFrom;

        /// <value>
        /// Relates the player id to the button that should be used to interact with them.
        /// </value>
        public Dictionary<UUID, OsButton> ButtonMap { get; set; }

        protected delegate void NextProcessDelegate(OSChatMessage chat);

        /// <summary>
        /// Child interactions must fill this in so that the next step in their chain of processes is called correctly
        /// </summary>
        protected NextProcessDelegate m_nextProcess;

        /// <summary>
        /// Create an interaction for a single user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="button"></param>
        public AbstractInteraction(UUID userId, OsButton button)
        {
            ButtonMap = new Dictionary<UUID, OsButton>();
            ButtonMap.Add(userId, button);
        }

        /// <summary>
        /// Create an interaction for two users
        /// </summary>
        /// <param name="user1Id"></param>
        /// <param name="user1Button"></param>
        /// <param name="user2Id"></param>
        /// <param name="user2Button"></param>
        public AbstractInteraction(UUID user1Id, OsButton user1Button, UUID user2Id, OsButton user2Button)
        {
            ButtonMap = new Dictionary<UUID, OsButton>();
            ButtonMap.Add(user1Id, user1Button);
            ButtonMap.Add(user2Id, user2Button);            
        }
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="buttonMap">Relates player uuid to the button that should be used to interact with them</param>
        public AbstractInteraction(Dictionary<UUID, OsButton> buttonMap)
        {
            ButtonMap = buttonMap;
        }           

        public void Close()
        {
            Deactivate();
        }

        /// <summary>
        /// Start listening for text from the given user.  This is usually as part of a dialog reply.
        /// </summary>
        /// <param name="awaitingReplyFrom"></param>
        /// <param name="nextProcess"></param>
        protected void Activate(UUID awaitingReplyFrom, NextProcessDelegate nextProcess) 
        {
            m_awaitingReplyFrom = awaitingReplyFrom;
            m_nextProcess = nextProcess;

            // XXX: Is this right?  Shouldn't we only listen for chat from the m_waitingReplyFrom button?
            foreach (OsButton button in ButtonMap.Values)
                button.OnChat += OnChatEvent; 
        }
        
        protected void Deactivate() 
        {
            m_awaitingReplyFrom = UUID.Zero;
            m_nextProcess = null;

            foreach (OsButton button in ButtonMap.Values)
                button.OnChat -= OnChatEvent;
        }
        
        protected virtual void OnChatEvent(OSChatMessage chat)
        {
            if (chat.Sender.AgentId != m_awaitingReplyFrom)
                return;
            
            if (chat.Channel != ButtonMap[chat.Sender.AgentId].Channel)
                return;

            NextProcessDelegate nextProcess = m_nextProcess;
            
            Deactivate();
            
            m_log.InfoFormat(
                "[WATER WARS]: Received chat [{0}] from {1}", chat, chat.Sender.AgentId);

            nextProcess(chat);
        }

        /// <summary>
        /// Send a dialog to the user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="text"></param>
        /// <param name="options"></param>
        /// <param name="next">The process to execute once information has been received back from this dialog</param>
        protected void SendDialog(UUID userId, string text, List<string> options, NextProcessDelegate next)
        {
            SendDialog(userId, text, options.ToArray(), next);           
        }           
        
        /// <summary>
        /// Send a dialog to the user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="text"></param>
        /// <param name="options"></param>
        /// <param name="next">The process to execute once information has been received back from this dialog</param>
        protected void SendDialog(UUID userId, string text, string[] options, NextProcessDelegate next)
        {
            Activate(userId, next);            
            ButtonMap[userId].SendDialog(userId, text, options);            
        }        

        /// <summary>
        /// Send an alert to the user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="text"></param>
        protected void SendAlert(UUID userId, string text)
        {
            ButtonMap[userId].SendAlert(userId, text);
        }    
    }
}