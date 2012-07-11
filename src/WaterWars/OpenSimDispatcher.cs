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
using log4net;
using OpenMetaverse;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using WaterWars.Models;
using WaterWars.States;
using WaterWars.Views;

namespace WaterWars
{       
    /// <summary>
    /// Provides methods for dispatching messages back and forth from OpenSim
    /// </summary>    
    public class OpenSimDispatcher : IDispatcher
    {
//        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        protected IScriptModuleComms m_scriptModuleComms;
        protected WaterWarsController m_controller;

        protected Dictionary<UUID, GameManagerView> m_gameManagerViews = new Dictionary<UUID, GameManagerView>();
        protected Dictionary<UUID, BuyPointView> m_buyPointViews = new Dictionary<UUID, BuyPointView>();
        protected Dictionary<UUID, GameAssetView> m_gameAssetViews = new Dictionary<UUID, GameAssetView>();
        
        public OpenSimDispatcher(WaterWarsController controller)
        {
            m_controller = controller;
            
            m_controller.EventManager.OnGameAssetBuildStarted += CreateGameAssetView;

            // FIXME: If we use this again, we'll need to get one per scene since IScriptModuleComms is not shared
            m_scriptModuleComms = m_controller.Scenes[0].RequestModuleInterface<IScriptModuleComms>();
//            m_scriptModuleComms.OnScriptCommand += ProcessScriptCommand;              
        }

        public void RegisterGameManagerView(GameManagerView gmv)
        {
            lock (m_gameManagerViews)
                m_gameManagerViews.Add(gmv.Uuid, gmv);            
        }
        
        public void RegisterBuyPointView(BuyPointView bpv)
        {
            lock (m_buyPointViews)
                m_buyPointViews.Add(bpv.Uuid, bpv);
        }       

        public string FetchGameConfiguration()
        {
            // There's actually only one game manager.  If this ever changes we will need to fetch configuration from
            // some other view
            lock (m_gameManagerViews)
                foreach (GameManagerView gmv in m_gameManagerViews.Values)
                    return gmv.FetchGameConfiguration();
                //return m_gameManagerViews.Values[0].FetchGameConfiguration();
            
            return string.Empty;           
        }
        
        public string FetchBuyPointConfiguration(BuyPoint bp)
        {
            lock (m_buyPointViews)
            {
                if (m_buyPointViews.ContainsKey(bp.Uuid))
                    return m_buyPointViews[bp.Uuid].FetchConfiguration();
            }
            
            return null;
        }

        public void AlertConfigurationFailure(string message)
        {
            // There's actually only one game manager.  If this ever changes we will need to fetch configuration from
            // some other view
            lock (m_gameManagerViews)
                foreach (GameManagerView gmv in m_gameManagerViews.Values)
                    gmv.AlertConfigurationFailure(message);            
        }

        public void RemoveBuyPointView(BuyPoint bp)
        {
            lock (m_buyPointViews)
                GetBuyPointView(bp.Uuid).Close();                
        }
        
        protected void CreateGameAssetView(AbstractGameAsset asset)
        {
            lock (m_buyPointViews)
            {
                GameAssetView gav = GetBuyPointView(asset.BuyPointUuid).CreateGameAssetView(asset);
                asset.Position = gav.RootPart.AbsolutePosition;

                lock (m_gameAssetViews)
                    m_gameAssetViews.Add(gav.Uuid, gav);
            }
        }

        public Field RemoveGameAssetView(AbstractGameAsset asset)
        {
            Field f = null;
            
            lock (m_gameAssetViews)
            {
                GameAssetView gav = GetGameAssetView(asset.Uuid);
                Vector3 pos = gav.RootPart.AbsolutePosition;
                gav.Close();

                lock (m_buyPointViews)
                {
                    f = asset.Field;
                    BuyPoint bp = f.BuyPoint;
                    GetBuyPointView(bp.Uuid).CreateFieldView(f, pos);
                }
            }
            
            f.TriggerChanged();

            return f;
        }

        public Dictionary<UUID, Field> ChangeBuyPointSpecialization(
             BuyPoint bp, AbstractGameAssetType type, int numberOfFields)
        {                        
            Dictionary<UUID, Field> fields = new Dictionary<UUID, Field>();
            for (int i = 1; i <= numberOfFields; i++)
            {
                Field f = m_controller.ModelFactory.CreateField(bp, UUID.Random(), string.Format("Field {0}", i));
                fields[f.Uuid] = f;
            }                
            
            lock (m_buyPointViews)
                GetBuyPointView(bp.Uuid).ChangeSpecialization(type, fields.Values.ToList());                

            return fields;
        }

        protected BuyPointView GetBuyPointView(UUID uuid)
        {
            if (!m_buyPointViews.ContainsKey(uuid))
                throw new Exception(string.Format("Attempt to retrieve BuyPointView with id {0} but none exists", uuid));
            else
                return m_buyPointViews[uuid];
        }
        
        protected GameAssetView GetGameAssetView(UUID uuid)
        {
            if (!m_gameAssetViews.ContainsKey(uuid))
                throw new Exception(string.Format("Attempt to retrieve GameAssetView with id {0} but none exists", uuid));
            else
                return m_gameAssetViews[uuid];
        }
                    
//        void DispatchReply(UUID scriptId, int code, string text, string key)
//        {
//            string logText = text.Replace("\n", @"\n");
//            m_log.InfoFormat("[WATER WARS]: <== Sending command {0} to script {1} ==>", logText, scriptId);
//            
//            m_scriptModuleComms.DispatchReply(scriptId, code, text, key);
//        }
    }
}
