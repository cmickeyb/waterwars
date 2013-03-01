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

using log4net;
using Mono.Addins;
using Nini.Config;
using OpenMetaverse;
using System;
using System.Collections.Generic;
using System.Reflection;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;
using WaterWars.Models;
using WaterWars.Persistence;
using WaterWars.Persistence.Recorder;
using WaterWars.Rules.Allocators;
using WaterWars.Rules.Distributors;
using WaterWars.Rules.Forecasters;
using WaterWars.Rules.Generators;
using WaterWars.Rules.Economic.Distributors;
using WaterWars.Rules.Economic.Forecasters;
using WaterWars.Rules.Economic.Generators;

[assembly: Addin("WaterWars", "1.0")]
[assembly: AddinDependency("OpenSim", "0.5")]

namespace WaterWars
{
    /// <summary>
    /// This is the Water Wars OpenSim region module
    /// </summary>
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "WaterWarsModule")]
    public class WaterWarsModule : ISharedRegionModule
    {        
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected List<Scene> m_scenes = new List<Scene>();
        protected WaterWarsController m_controller;

        private bool m_initialized;
        
        public string Name 
        { 
            get { return WaterWarsConstants.MODULE_NAME; } 
        }

        public Type ReplaceableInterface 
        {
            get { return null; }
        }

        public void Initialise(IConfigSource config)
        {
            SceneManager.Instance.OnRegionsReadyStatusChange += HandleOnRegionsReadyStatusChange;
        }
        
        public void AddRegion(Scene scene)
        {
            m_log.InfoFormat("[WATER WARS]: Initialising with scene {0}", scene.RegionInfo.RegionName);
            m_scenes.Add(scene);

            if (m_scenes.Count == 1)
            {
                scene.AddCommand(
                    this, "ww add plinths",
                    "ww add plinths <region name>",
                    "Add plinths to parcels in the named region.", HandleAddPlinthsConsoleCommand);

                scene.AddCommand(
                    this, "ww remove plinths",
                    "ww remove plinths <region name>",
                    "Remove plinths to parcels in the named region.", HandleRemovePlinthsConsoleCommand);                 
                
                scene.AddCommand(
                    this, "ww end turn",
                    "ww end turn <first name> <last name>",
                    "End the turn of the given player.", HandleEndTurnConsoleCommand);                 
                
                scene.AddCommand(
                    this, "ww give money",
                    "ww give money <first name> <last name> <amount>",
                    "Give money to a player.", HandleGiveMoneyConsoleCommand);                 
                
                scene.AddCommand(
                    this, "ww give water",
                    "ww give water <first name> <last name> <amount>",
                    "Give water to a player.", HandleGiveWaterConsoleCommand);                
                
                scene.AddCommand(
                    this, "ww give rights",
                    "ww give rights <first name> <last name> <amount>",
                    "Give water rights to a player.", HandleGiveRightsConsoleCommand);                    
                
                scene.AddCommand(
                    this, "ww buy random",
                    "ww buy random <number of parcels>",
                    "Buy a random parcel for each current player, up to the total number specified", HandleBuyRandomConsoleCommand);                                 
                
                scene.AddCommand(
                    this, "ww level parcels",
                    "ww level parcels <raise|lower> <region name>",
                    "Completely level the terrain of all game parcels", HandleLevelParcelsConsoleCommand);                                                       
                
                scene.AddCommand(
                    this, "ww refresh huds",
                    "ww refresh huds",
                    "Refresh all the huds in the game", HandleRefreshHudsConsoleCommand);                
                
                scene.AddCommand(
                    this, "ww show huds",
                    "ww show huds",
                    "Show the status of huds in the game", HandleShowHudsConsoleCommand);                                       
                
                scene.AddCommand(
                    this, "ww show parcels",
                    "ww show parcels",
                    "Show the status of parcels in the game", HandleShowParcelsConsoleCommand);                  
                
                scene.AddCommand(
                    this, "ww show status",
                    "ww show status",
                    "Show the status of the game", HandleShowStatusConsoleCommand);                                                 
            }            
        }

        void HandleOnRegionReadyStatusChange(IScene obj)
        {

        }

        public void RegionLoaded(Scene scene)
        {
        }

        public void RemoveRegion(Scene scene)
        {
        }

        private void HandleOnRegionsReadyStatusChange(SceneManager obj)
        {
            if (m_initialized || !obj.AllRegionsReady)
                return;

            try
            {
                UserAccount economyUserAccount
                    = m_scenes[0].UserAccountService.GetUserAccount(
                        UUID.Zero, WaterWarsConstants.ECONOMY_PLAYER_FIRST_NAME, WaterWarsConstants.ECONOMY_PLAYER_LAST_NAME);
                
                if (null == economyUserAccount)
                    throw new Exception(
                        string.Format(
                            "Economy user \"{0} {1}\" not present.  Please create this user before restarting", 
                            WaterWarsConstants.ECONOMY_PLAYER_FIRST_NAME, WaterWarsConstants.ECONOMY_PLAYER_LAST_NAME));
                
                m_controller 
                    = new WaterWarsController(
                        m_scenes, 
                        null,
                        new Recorder(new FileDestination()),                    
                        new SimpleForecaster(),
                        new SeriesRainfallGenerator(), 
                        new FairWaterDistributor(), 
                        new SimpleWaterAllocator(),
                        new SimpleEconomicForecaster(),
                        new SeriesEconomicGenerator(),
                        new SimpleEconomicDistributor(),
                        economyUserAccount);
                
                m_controller.Dispatcher = new OpenSimDispatcher(m_controller);
                m_controller.Initialise(true);
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[WATER WARS]: ERROR {0} {1}", e.Message, e.StackTrace);
            }
            finally
            {
                m_initialized = true;
            }
        }

        public void PostInitialise() 
        {
        }

        public void Close()
        {
            m_controller.Close();
        }

        /// <summary>
        /// Add plinths to the named region
        /// </summary>
        /// <param name="cmdparams"></param>
        protected void HandleAddPlinthsConsoleCommand(string module, string[] cmdparams)
        {
            if (cmdparams.Length != 4)
            {
                m_log.Error(
                    "[WATER WARS]: usage is ww add plinths <region name>");
                return;
            }

            string regionName = cmdparams[3];
            Scene foundScene = FindScene(regionName);

            if (null == foundScene)
            {
                m_log.ErrorFormat("[WATER WARS]: Could not find region {0} for add plinths command", regionName);
                return;
            }

            m_controller.Commands.AddPlinths(foundScene);
        }

        /// <summary>
        /// Remove plinths from the named region
        /// </summary>
        /// <param name="cmdparams"></param>
        protected void HandleRemovePlinthsConsoleCommand(string module, string[] cmdparams)
        {
            if (cmdparams.Length != 4)
            {
                m_log.Error(
                    "[WATER WARS]: usage is ww remove plinths <region name>");
                return;
            }

            string regionName = cmdparams[3];
            Scene foundScene = FindScene(regionName);

            if (null == foundScene)
            {
                m_log.ErrorFormat("[WATER WARS]: Could not find region {0} for remove plinths command", regionName);
                return;
            }

            m_controller.Commands.RemovePlinths(foundScene);
        }   
        
        /// <summary>
        /// End a player's turn
        /// </summary>
        /// <param name="cmdparams"></param>
        protected void HandleEndTurnConsoleCommand(string module, string[] cmdparams)
        {
            if (cmdparams.Length != 5)
            {
                m_log.Error(
                    "[WATER WARS]: usage is ww end turn <first name> <last name>");
                return;
            }
            
            string firstName = cmdparams[3];
            string lastName = cmdparams[4];
            
            Player p = GetPlayer(firstName, lastName);

            if (p != null)            
                m_controller.Commands.EndTurn(p);
        }    
        
        /// <summary>
        /// Give money to a player
        /// </summary>
        /// <param name="cmdparams"></param>
        protected void HandleGiveMoneyConsoleCommand(string module, string[] cmdparams)
        {
            if (cmdparams.Length != 6)
            {
                m_log.Error(
                    "[WATER WARS]: usage is ww give money <first name> <last name> <amount>");
                return;
            }
            
            string firstName = cmdparams[3];
            string lastName = cmdparams[4];
            int amount = int.Parse(cmdparams[5]);
            
            Player p = GetPlayer(firstName, lastName);

            if (p != null)
                m_controller.Commands.GiveMoney(p, amount);
        }    
        
        /// <summary>
        /// Give money to a player
        /// </summary>
        /// <param name="cmdparams"></param>
        protected void HandleGiveWaterConsoleCommand(string module, string[] cmdparams)
        {
            if (cmdparams.Length != 6)
            {
                m_log.Error(
                    "[WATER WARS]: usage is ww give water <first name> <last name> <amount>");
                return;
            }
            
            string firstName = cmdparams[3];
            string lastName = cmdparams[4];
            int amount = int.Parse(cmdparams[5]);
            
            Player p = GetPlayer(firstName, lastName);

            if (p != null)
                m_controller.Commands.GiveWater(p, amount);
        }          
        
        /// <summary>
        /// Give water rights to a player
        /// </summary>
        /// <param name="cmdparams"></param>
        protected void HandleGiveRightsConsoleCommand(string module, string[] cmdparams)
        {
            if (cmdparams.Length != 6)
            {
                m_log.Error(
                    "[WATER WARS]: usage is ww give rights <first name> <last name> <amount>");
                return;
            }
            
            string firstName = cmdparams[3];
            string lastName = cmdparams[4];
            int amount = int.Parse(cmdparams[5]);
            
            Player p = GetPlayer(firstName, lastName);

            if (p != null)
                m_controller.Commands.GiveWaterRights(p, amount);
        }           
        
        protected void HandleBuyRandomConsoleCommand(string module, string[] cmdParams)
        {
            if (cmdParams.Length != 4)
            {
                m_log.Error(
                    "[WATER WARS]: usage is ww buy random <number of parcels>");
                return;
            }   
            
            int numberOfParcels = int.Parse(cmdParams[3]);
            
            m_controller.Commands.BuyRandom(numberOfParcels);
        }
        
        protected void HandleLevelParcelsConsoleCommand(string module, string[] cmdparams)
        {
            if (cmdparams.Length != 5)
            {
                m_log.Error(
                    "[WATER WARS]: usage is ww level parcels <raise|lower> <region name>");
                return;
            }   

            string regionName = cmdparams[4];
            Scene foundScene = FindScene(regionName);

            if (null == foundScene)
            {
                m_log.ErrorFormat("[WATER WARS]: Could not find region {0}", regionName);
                return;
            }          
            
            m_controller.Commands.LevelGameParcels(foundScene, cmdparams[3] == "raise" ? true : false);            
        }
        
        protected void HandleRefreshHudsConsoleCommand(string module, string[] cmdparams)
        {
            if (cmdparams.Length != 3)
            {
                m_log.Error(
                    "[WATER WARS]: usage is ww refresh huds");
                return;
            }   
            
            m_controller.Commands.RefreshHuds();
        }         
        
        protected void HandleShowHudsConsoleCommand(string module, string[] cmdparams)
        {
            if (cmdparams.Length != 3)
            {
                m_log.Error(
                    "[WATER WARS]: usage is ww show huds");
                return;
            }   
            
            m_controller.Commands.ShowHuds();
        }  
        
        protected void HandleShowParcelsConsoleCommand(string module, string[] cmdparams)
        {
            if (cmdparams.Length != 3)
            {
                m_log.Error(
                    "[WATER WARS]: usage is ww show parcels");
                return;
            }   
            
            m_controller.Commands.ShowParcels();
        }            
        
        protected void HandleShowStatusConsoleCommand(string module, string[] cmdparams)
        {
            if (cmdparams.Length != 3)
            {
                m_log.Error(
                    "[WATER WARS]: usage is ww show status");
                return;
            }   
            
            m_controller.Commands.ShowStatus();
        }        
        
//        void OnNewClient(IClientAPI client)
//        {
//            client.OnParcelPropertiesRequest += OnParcelPropertiesRequest;
//        }
//        
//        public void OnParcelPropertiesRequest(
//            int start_x, int start_y, int end_x, int end_y, int sequence_id,
//            bool snap_selection, IClientAPI remote_client)
//        {
//            m_log.InfoFormat(
//                "[WATER WARS]: Received click at (({0},{1}),({2},{3})), sequence_id={4}, snap_selection={5} from {6}",
//                start_x, start_y, end_x, end_y, sequence_id, snap_selection, remote_client.Name);              
//        }        
        
        protected Player GetPlayer(string firstName, string lastName)
        {
            UserAccount userAccount 
                = m_scenes[0].UserAccountService.GetUserAccount(UUID.Zero, firstName, lastName);
            
            if (userAccount == null)
            {
                m_log.ErrorFormat("[WATER WARS]: No user found with name {0} {1}", firstName, lastName);
                return null;
            }
            
            Player p;
            lock (m_controller.Game.Players)
                m_controller.Game.Players.TryGetValue(userAccount.PrincipalID, out p);
            
            if (p == null)
            {
                m_log.ErrorFormat("[WATER WARS]: {0} {1} is not playing the game!", firstName, lastName);
                return null;
            }            
            
            return p;
        }
        
        /// <summary>
        /// Find the given scene.
        /// </summary>
        /// <param name="name"></param>
        /// <returns>null if no such scene was found</returns>
        protected Scene FindScene(string regionName)
        {
            Scene foundScene = null;

            foreach (Scene scene in m_scenes)
            {
                if (scene.RegionInfo.RegionName == regionName)
                {
                    foundScene = scene;
                    break;
                }
            }   
            
            return foundScene;
        }
    }
}