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
using Nini.Config;
using OpenMetaverse;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using WaterWars;
using WaterWars.Config;
using WaterWars.Events;
using WaterWars.Models;
using WaterWars.Rules.Startup;

namespace WaterWars.States
{
    /// <summary>
    /// The game is waiting for player registrations
    /// </summary>
    public class RegistrationState : AbstractState
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        const string REGISTRATION_READY_MSG = "Ready for registration";
        const string WAITING_FOR_GAME_TO_BEGIN_PUBLIC_STATUS_MSG = "Waiting for game to begin";
        //const string WAITING_FOR_GAME_TO_BEGIN_HUD_STATUS_MSG = "Registered for play as {0} - waiting for game to begin";        
        const string ADD_PLAYER_CRAWL_MSG = "{0} registered for the next game as a {1}";

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="controller"></param>
        public RegistrationState(WaterWarsController controller) : base(controller, GameStateType.Registration) {}      

        protected override void StartState() 
        { 
            base.StartState();          
                                
            UpdateAllStatus();
            
            m_controller.Events.PostToAll(REGISTRATION_READY_MSG, EventLevel.Alert);            
            
            List<BuyPoint> buyPoints;
            lock (Game.BuyPoints)
                buyPoints = Game.BuyPoints.Values.ToList();
            
            foreach (BuyPoint bp in buyPoints)
                bp.TriggerChanged();
            
            // We can only reset the players here since if we do it any earlier (e.g. in the game resetting state)
            // then we can't send them further status messages!
            // The actual deletion from persistence is done in the reset stage.  This is messy.
            lock (Game.Players)                                        
                Game.Players.Clear();            
        }
        
        public override void AddPlayer(Player newPlayer)
        {
            lock (Game.Players)               
            {
                Game.Players[newPlayer.Uuid] = newPlayer;
            
                m_controller.EventManager.TriggerPlayerAdded(newPlayer);
                UpdateHudStatus(newPlayer);

                m_controller.Events.PostToAll(
                    string.Format(ADD_PLAYER_CRAWL_MSG, newPlayer.Name, newPlayer.Role.Type), 
                    EventLevel.Crawl);               
                
                m_log.InfoFormat("[WATER WARS]: Registered player {0} as a {1}", newPlayer.Name, newPlayer.Role.Type);
            }
        }

        public override void StartGame()
        {
            HashSet<string> zones = GatherZones();
            
            try
            {
                // TODO: We actually need to read the configuration when the notecard is saved so that we can control
                // max players.  Should not allow max player configuration changes with any players already registered to play
                string configuration = m_controller.Dispatcher.FetchGameConfiguration();
//                string printConfiguration = configuration.Replace("\n", "|");
//                Console.WriteLine("Configuration:\n" + printConfiguration);                
                m_controller.Configuration = ConfigurationParser.Parse(configuration, zones);                
            }
            catch (ConfigurationException e)
            {
                m_log.Error("[WATER WARS]: ", e);
                throw e;
            }

            m_controller.Forecaster.UpdateConfiguration(m_controller.Configuration);
            m_controller.EconomicForecaster.UpdateConfiguration(m_controller.Configuration);
            m_controller.EconomicGenerator.UpdateConfiguration(m_controller.Configuration);
            m_controller.EconomicDistributor.UpdateConfiguration(m_controller.Configuration);
            m_controller.RainfallGenerator.UpdateConfiguration(m_controller.Configuration);
            m_controller.WaterDistributor.UpdateConfiguration(m_controller.Configuration);
            m_controller.GameDateManager.UpdateConfiguration(m_controller.Configuration);
            m_controller.RoundManager.UpdateConfiguration(m_controller.Configuration);
            m_controller.StageTimer.UpdateConfiguration(m_controller.Configuration);

            ConfigureBuyPoints(zones);

            // Crops
            IConfig cropsConfig = m_controller.Configuration.Configs[ConfigurationParser.CROPS_SECTION_NAME];
            List<string> names = new List<string>();
            names.Add("");
            names.AddRange(Enum.GetNames(typeof(CropType)));
            LoadGameAssetValues(cropsConfig, names, Crops.Template);             
            
            // XXX: Currently non-configurable
            int[] timesToLive = new int[Crops.Template.MaxLevel + 1];
            timesToLive[(int)CropType.Alfalfa] = 1;
            timesToLive[(int)CropType.Chillis] = 1;
            timesToLive[(int)CropType.Grapes] = AbstractGameAsset.INFINITE_TIME_TO_LIVE;
            Crops.Template.InitialTimesToLive = timesToLive;
            
            Crops.Template.InitialNames 
                = new String[] 
                    { "", CropType.Alfalfa.ToString(), CropType.Chillis.ToString(), CropType.Grapes.ToString() };
            
            Crops.Template.TriggerChanged();
                        
            // Condos
            IConfig condosConfig = m_controller.Configuration.Configs[ConfigurationParser.CONDOS_SECTION_NAME];
            names = new List<string>();
            names.Add("");                        
            for (int i = Houses.Template.MinLevel; i <= Houses.Template.MaxLevel; i++)
                names.Add(string.Format("{0}_{1}", ConfigurationParser.CONDO_KEY_PREFIX, i));
            LoadGameAssetValues(condosConfig, names, Houses.Template);                                  
            
            // XXX: Currently non-configurable
            Houses.Template.InitialNames 
                = new String[] { "", "Single Family Homes", "Multi Family Homes", "Estate Homes" };            
            
            Houses.Template.TriggerChanged();

            // Factories
            IConfig factoriesConfig = m_controller.Configuration.Configs[ConfigurationParser.FACTORIES_SECTION_NAME];
            names = new List<string>();
            names.Add("");           
            for (int i = Factory.Template.MinLevel; i <= Factory.Template.MaxLevel; i++)
                names.Add(string.Format("{0}_{1}", ConfigurationParser.FACTORY_KEY_PREFIX, i));
            LoadGameAssetValues(factoriesConfig, names, Factory.Template);              
            
            // XXX: Currently non-configurable
            Factory.Template.InitialNames = new String[] { "", "Factory Level 1", "Factory Level 2", "Factory Level 3" };                        
            
            Factory.Template.TriggerChanged();
            
            IConfig config = m_controller.Configuration.Configs[ConfigurationParser.GENERAL_SECTION_NAME];            
            if (config.Contains(ConfigurationParser.PREALLOCATE_PARCELS_KEY) && config.GetBoolean(ConfigurationParser.PREALLOCATE_PARCELS_KEY))
                m_controller.StartupRule = new PreallocateLandToFarmers(m_controller);
            else
                m_controller.StartupRule = new NullStartupRule();
            
            lock (Game.Players)
            {
                m_log.InfoFormat("[WATER WARS]: Starting game with {0} players", Game.Players.Count);

                // We have to wait until now to set some player parameters from the configuration.
                foreach (Player player in Game.Players.Values)
                {
                    player.Money 
                        = m_controller.Configuration.Configs[ConfigurationParser.PLAYERS_SECTION_NAME].GetInt(
                            ConfigurationParser.GetPlayerKey(
                                player.Role.Type, ConfigurationParser.PLAYER_START_MONEY_KEY));
                    
                    player.CostOfLiving 
                        = m_controller.Configuration.Configs[ConfigurationParser.PLAYERS_SECTION_NAME].GetInt(
                            ConfigurationParser.GetPlayerKey(
                                player.Role.Type, ConfigurationParser.PLAYER_COST_OF_LIVING_KEY));
                }
            }
                            
            EndState(new GameStartingState(m_controller));
        }
        
        /// <summary>
        /// Gather all the zones defined on buypoints
        /// </summary>
        /// <remarks>This also sets the zone on the buypoint</remarks>
        /// <returns></returns>
        protected HashSet<string> GatherZones()
        {
            HashSet<string> zones = new HashSet<string>();            
            
            lock (Game.BuyPoints)
            {                                
                foreach (BuyPoint bp in Game.BuyPoints.Values)
                {
                    string configuration = m_controller.Dispatcher.FetchBuyPointConfiguration(bp);
                    
                    if (configuration != null)
                    {
                        IConfigSource configSource = GameModelConfigurationParser.Parse(configuration);
                        IConfig gmConfig 
                            = GameModelConfigurationParser.GetConfig(
                                configSource, GameModelConfigurationParser.GENERAL_SECTION_NAME);                                    
                        bp.Zone = gmConfig.GetString(GameModelConfigurationParser.ZONE_KEY, null);                       
                        
//                        m_log.InfoFormat(
//                           "[WATER WARS]: Found configured zone [{0}] for {1} in {2}", bp.Zone, bp.Name, bp.RegionName);
                    }
                    
                    string zone = bp.Zone;
                    
                    if (zone != null)
                        zones.Add(zone);                
                }
            }
            
            return zones;
        }
        
        /// <summary>
        /// Configure the buy points for a new game
        /// </summary>
        /// <param name="zones">The set of zones for which to load configuration</param>
        protected void ConfigureBuyPoints(HashSet<string> zoneNames)
        {
            IConfig parcelsConfig = m_controller.Configuration.Configs[ConfigurationParser.PARCELS_SECTION_NAME];
                        
            int devRightsPrice = parcelsConfig.GetInt(ConfigurationParser.DEFAULT_RIGHTS_PRICE_KEY);
//            int waterRightsPrice = parcelsConfig.GetInt(ConfigurationParser.INITIAL_WATER_RIGHTS_PRICE_KEY);
            int waterEntitlement = parcelsConfig.GetInt(ConfigurationParser.DEFAULT_WATER_ENTITLEMENT_KEY);
            
            // FIXME: Should probably make zones first class objects
            Dictionary<string, int> zoneRightsPrices = new Dictionary<string, int>();
            Dictionary<string, int> zoneWaterEntitlements = new Dictionary<string, int>();
            
            foreach (string zoneName in zoneNames)
            {
                zoneRightsPrices[zoneName]
                    = parcelsConfig.GetInt(
                        ConfigurationParser.GetZoneKey(zoneName, ConfigurationParser.ZONE_RIGHTS_PRICE_KEY));
                zoneWaterEntitlements[zoneName]
                    = parcelsConfig.GetInt(
                        ConfigurationParser.GetZoneKey(zoneName, ConfigurationParser.ZONE_WATER_ENTITLEMENT_KEY));        
            }
            
            lock (Game.BuyPoints)
            {
                foreach (BuyPoint bp in Game.BuyPoints.Values)
                {      
                    if (bp.Zone == null)
                    {
                        bp.DevelopmentRightsPrice = devRightsPrice;
//                        bp.WaterRightsPrice = waterRightsPrice;
                        bp.InitialWaterRights = waterEntitlement;
                    }
                    else
                    {
                        bp.DevelopmentRightsPrice = zoneRightsPrices[bp.Zone];
                        bp.InitialWaterRights = zoneWaterEntitlements[bp.Zone];           
                    }
                }
            }            
        }
        
        /// <summary>
        /// Load values for the given game asset type
        /// </summary>
        /// <param name="config"></param>
        /// <param name="keyNames"></param>
        /// <param name="template"></param>
        protected void LoadGameAssetValues(
            IConfig config, List<string> keyNames, AbstractGameAsset template)
        {            
            template.ConstructionCostsPerBuildStep
                = LoadValues(config, keyNames, ConfigurationParser.COST_POSTFIX_KEY, template);
            
            template.StepsToBuilds
                = LoadValues(config, keyNames, ConfigurationParser.BUILD_TURNS_POSTFIX_KEY, template);
            
            template.NormalRevenues 
                = LoadValues(config, keyNames, ConfigurationParser.REVENUE_POSTFIX_KEY, template);     
            
            template.MaintenanceCosts 
                = LoadValues(config, keyNames, ConfigurationParser.MAINTENANCE_POSTFIX_KEY, template);                       
            
            template.WaterUsages 
                = LoadValues(config, keyNames, ConfigurationParser.WATER_POSTFIX_KEY, template);             
        }
        
        /// <summary>
        /// Load a set of values for a given configurable parameter of a game asset (e.g. costs)
        /// </summary>
        /// <param name="config"></param>
        /// <param name="keyNames"></param>
        /// <param name="keyPostFix"></param>
        /// <param name="template"></param>
        /// <returns></returns>
        protected int[] LoadValues(
            IConfig config, List<string> keyNames, string keyPostFix, AbstractGameAsset template)
        {
            int[] values = new int[keyNames.Count];            
            for (int i = template.MinLevel; i <= template.MaxLevel; i++)
            {                    
                string keyName 
                    = string.Format(ConfigurationParser.GAME_ASSET_KEY_FORMAT, keyNames[i].ToLower(), keyPostFix);
                values[i] = config.GetInt(keyName);
            }
            
            return values;            
        }

        public override void RegisterBuyPoint(BuyPoint bp)
        {
            Vector3 swPoint, nePoint;
            WaterWarsUtils.FindSquareParcelCorners(bp.Location.Parcel, out swPoint, out nePoint);  
            
            if (!Game.BuyPoints.ContainsKey(bp.Uuid))
            {                
                m_log.InfoFormat(
                    "[WATER WARS]: Registering buy point {0} named {1} at {2} in parcel {3} ({4},{5})", 
                     bp.Uuid, bp.Name, bp.Location.LocalPosition, bp.Location.Parcel.LandData.Name, swPoint, nePoint);                
                Game.BuyPoints[bp.Uuid] = bp;                
            }
            else
            {
                m_log.WarnFormat(
                    "[WATER WARS]: Attempt to register duplicate buy point {0} named {1} at {2} in parcel {3} ({4},{5})",
                    bp.Uuid, bp.Name, bp.Location.LocalPosition, bp.Location.Parcel.LandData.Name, swPoint, nePoint);              
            } 
            
            m_controller.EventManager.TriggerBuyPointRegistered(bp);
        }

        public override void UpdateHudStatus(Player player)
        {
            m_controller.SendStop(player.Uuid);
//            m_controller.SendHudStatus(
//                player, string.Format(WAITING_FOR_GAME_TO_BEGIN_HUD_STATUS_MSG, player.Role.Type));
        }        
    }
}
