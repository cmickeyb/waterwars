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
using System.ServiceModel.Syndication;
using System.Xml;
using log4net;
using Newtonsoft.Json;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using WaterWars;
using WaterWars.Models;

namespace WaterWars.Events
{
    /// <summary>
    /// Handles Water Wars Events
    /// </summary>    
    public class Events
    {
//        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        protected WaterWarsController m_controller;
        
        public Events(WaterWarsController controller)
        {
            m_controller = controller;
        }
        
        public void Post(Player p, string e, EventLevel level)
        {
            Post(p.Uuid, e, level);          
        }
        
        public void Post(UUID userId, string e, EventLevel level)
        {
            if (EventLevel.All == level || EventLevel.Alert == level)
                PostAlert(userId, e);
            
            if (EventLevel.All == level || EventLevel.Crawl == level)
                PostCrawl(userId, e);            
        }
                        
        public void PostToAll(string e, EventLevel level)
        {            
            //m_log.InfoFormat("[WATER WARS]: Dialog module [{0}], event level [{1}], event [{2}]", m_dialogModule, level, e);
            
            IDictionary<UUID, Player> players = m_controller.Game.Players;
            
            lock (players)
            {
                foreach (Player p in players.Values)
                {
                    if (EventLevel.Alert == level)
                        PostAlert(p, e);
                    else if (EventLevel.Crawl == level)
                        PostCrawl(p, e);
                }
            }
        }
        
        public void PostAlert(Player p, string e)
        {
            PostAlert(p.Uuid, e);         
        }
        
        public void PostAlert(UUID userId, string e)
        {
            PostNotification(userId, e, false);          
        }          
        
        public void PostModalMessage(UUID userId, string e)
        {
            PostNotification(userId, e, true);         
        }
        
        protected void PostNotification(UUID userId, string e, bool modal)
        {
            // Put absolutely everything in news
//            m_controller.Feeds.Notify(userId, e);
                        
            // We have to search each scene for the player since we don't track this data.
            foreach (Scene scene in m_controller.Scenes)
            {
                ScenePresence sp = scene.GetScenePresence(userId);
                if (sp != null && !sp.IsChildAgent)
                {
                    IDialogModule dialogModule = scene.RequestModuleInterface<IDialogModule>();
                    dialogModule.SendAlertToUser(userId, e, modal);
                    break;
                }
            }                
        }
        
        public void PostCrawl(Player p, string e)
        {
            PostCrawl(p.Uuid, e);
        }
        
        public void PostCrawl(UUID userId, string e)
        {
            // Put absolutely everything in news
            //m_controller.Feeds.Notify(userId, e);
            
            // Crawler messages cannot contain newlines, otherwise we get a two-tier crawl effect.  Replace with a
            // sentence seperator instead.
            e = e.Replace("\n", "  ");
            
            m_controller.HudManager.SendTickerText(userId, e);
        }         
    }
}