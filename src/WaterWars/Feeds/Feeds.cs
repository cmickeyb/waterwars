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
using WaterWars;
using WaterWars.Models;

namespace WaterWars.Feeds
{    
    /// <summary>
    /// Manages event feeds for Water Wars
    /// </summary>
    public class Feeds
    {
//        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        protected WaterWarsController m_controller;
        
        /// <value>
        /// Player oriented feeds
        /// </value>
        protected Dictionary<UUID, SyndicationFeed> m_feeds = new Dictionary<UUID, SyndicationFeed>();

        /// <value>
        /// Feed items.  There will always be one collection per feed.
        /// </value>
        protected Dictionary<UUID, List<SyndicationItem>> m_feedItems = new Dictionary<UUID, List<SyndicationItem>>();

        public Feeds(WaterWarsController controller)
        {
            m_controller = controller;
            m_controller.EventManager.OnPlayerAdded += AddPlayer;
        }
        
        protected void AddPlayer(Player p)
        {
            // If we don't have a feed for a registered player, then create one pre-emptorily, so that they don't
            // miss any events.
            lock (m_feeds)
            {
                if (!m_feeds.ContainsKey(p.Uuid))
                    CreateFeed(p.Uuid);
            }
        }
        
        public void Notify(Player p, string msg)
        {
            Notify(p.Uuid, msg);
        }
        
        public void Notify(UUID userId, string msg)
        {
            lock (m_feeds)
            {
                SyndicationItem item = new SyndicationItem();
                item.Title = new TextSyndicationContent(msg);      
                item.PublishDate = new DateTimeOffset(m_controller.Game.CurrentDate);                
                
                m_feedItems[userId].Add(item);
            }
        }
        
        /// <summary>
        /// Notify all players of the given message
        /// </summary>
        /// 
        /// This is a separate method so that an all players notification has the same timestamp for everyone.
        /// <param name="string"></param>
        public void NotifyAll(string msg)
        {
            SyndicationItem item = new SyndicationItem();
            item.Title = new TextSyndicationContent(msg);      
            item.PublishDate = new DateTimeOffset(m_controller.Game.CurrentDate);
            
            lock (m_feeds)
            {
                foreach (KeyValuePair<UUID, List<SyndicationItem>> tuple in m_feedItems)
                {
//                    m_log.InfoFormat("[WATER WARS]: Adding to feed for {0} news item {1}", tuple.Key, msg);                    
                    tuple.Value.Add(item);
                }
            }
        }
        
        /// <summary>
        /// Get a serialized feed for the user.  They may not be an active player of the game.
        /// </summary>
        /// <param name="playerId"></param>
        /// <returns></returns>
        public string GetSerializedFeed(UUID userId)
        {              
            StringWriter sw = new StringWriter();
            SyndicationFeed feed = null;
          
            // Even though we create feeds up front for players, still create on dynamically if necessary for 
            // non-player observers.
            lock (m_feeds)
            {
                if (!m_feeds.ContainsKey(userId))
                    CreateFeed(userId);

                feed = m_feeds[userId];

                using (XmlTextWriter xtw = new XmlTextWriter(sw))
                {
                    // This needs to be under lock since the formatter iterates through an enumeration of list items
                    Rss20FeedFormatter f = new Rss20FeedFormatter(feed);
                    f.WriteTo(xtw);
                }
                
//                if (m_feedItems[userId].Count > 0)
//                    m_log.InfoFormat("[WATER WARS]: Feeding {0} items to {1}", m_feedItems[userId].Count, userId);        
            
                m_feedItems[userId].Clear();
            }

            return sw.ToString();
        }

        protected void CreateFeed(UUID playerId)
        {
//            m_log.InfoFormat("[WATER WARS]: Creating feed for {0}", playerId);
            
            SyndicationFeed feed = new SyndicationFeed();
                        
            /*
            SyndicationFeed feed = new SyndicationFeed("TechnicalNews Feed", "Technical News Feed", new Uri("http://WcfInsiders.com"));            
            feed.Authors.Add(new SyndicationPerson(""</span">"</span">adnanmasood@gmail.com"));           
            feed.Categories.Add(new SyndicationCategory("Technical News"));           
            feed.Description = new TextSyndicationContent("Technical News demo for RSS and ATOM publishing via WCF");
            */

            List<SyndicationItem> items = new List<SyndicationItem>();                
            feed.Items = items;

//            SyndicationItem item1 = new SyndicationItem();
//            item1.Title = new TextSyndicationContent("Helloooooo");
//            
//            items.Add(item1);
            
            /*
            SyndicationItem item1 
                = new SyndicationItem(
                    "Overhaul of net addresses begins", 
                    "The first big steps on the road to overhauling the net's core addressing system have been taken. On Monday the master address books for thenet are being updated to include records prepared in a new format known as IP version 6...",
                    new Uri("http://news.bbc.co.uk"),               
                    System.Guid.NewGuid ().ToString(),           
                    DateTime.Now);
                    */

            lock (m_feeds)
            {
                m_feeds[playerId] = feed;
                m_feedItems[playerId] = items;            
            }
        }        
    }
}