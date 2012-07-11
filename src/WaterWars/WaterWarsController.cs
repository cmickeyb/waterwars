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
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using log4net;
using Nini.Config;
using OpenMetaverse;
using System.Reflection;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;
using WaterWars.Config;
using WaterWars.Events;
using WaterWars.Feeds;
using WaterWars.Models;
using WaterWars.Persistence;
using WaterWars.Persistence.Recorder;
using WaterWars.Rules;
using WaterWars.Rules.Allocators;
using WaterWars.Rules.Distributors;
using WaterWars.Rules.Forecasters;
using WaterWars.Rules.Generators;
using WaterWars.Rules.Economic.Distributors;
using WaterWars.Rules.Economic.Forecasters;
using WaterWars.Rules.Economic.Generators;
using WaterWars.Rules.Startup;
using WaterWars.States;
using WaterWars.Views;
using WaterWars.WebServices;

namespace WaterWars
{        
    public class WaterWarsController
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);       

        public const string NOT_IN_GAME_HUD_STATUS_MSG = "Not in game";

        protected IScriptModuleComms m_scriptModuleComms;

        /// <value>
        /// The configuration of this game.
        /// </value>
        public IConfigSource Configuration { get; set; }

        /// <value>
        /// Is this game actually attached to a virtual environment.  If not then some view code is not invoked
        /// </value>
        public bool AttachedToVe { get; private set; } 
        
        /// <value>
        /// User account that represents the economy.
        /// </value>
        public UserAccount EconomyUserAccount { get; private set; }
        
        /// <summary>
        /// Handles commands that can be executed externally from the game.
        /// </summary>
        public WaterWarsCommands Commands { get; private set; }
        
        /// <value>
        /// Subscribe to this to receive game events.
        /// </value>
        public WaterWarsEventManager EventManager { get; private set; }
        
        /// <value>
        /// Used to dispatch messages to the virtual environment
        /// </value>
        public IDispatcher Dispatcher { get; set; }

        /// <value>
        /// Used to change the data provided by the viewer web services (these supplement the viewer) via the media
        /// browser or shared textures.
        /// </value>
        public ViewerWebServices ViewerWebServices { get; set; }

        /// <value>
        /// Water Wars Events.  Callers should post here.
        /// </value>
        public WaterWars.Events.Events Events { get; set; }
        
        /// <value>
        /// Event feeds.
        /// </value>
        public WaterWars.Feeds.Feeds Feeds { get; set; }        

        /// <value>
        /// Used to create game assets
        /// </value>
        public ModelFactory ModelFactory { get; private set; }

        /// <summary>
        /// Water forecaster
        /// </summary>
        public IWaterForecaster Forecaster { get; set; }
        
        /// <value>
        /// The rule that we're using to generate rainfall
        /// </value>
        public IRainfallGenerator RainfallGenerator { get; set; }

        /// <value>
        /// The rule that we're using to allocate rainfall
        /// </value>        
        public IWaterDistributor WaterDistributor { get; set; }
        
        /// <value>
        /// The rule that we're using to allocate water to an asset
        /// </value>
        public IWaterAllocator WaterAllocator { get; set; }                
        
        /// <summary>
        /// Economic forecaster
        /// </summary>
        public IEconomicForecaster EconomicForecaster { get; set; }
        
        /// <summary>
        /// Generate overall economic activity
        /// </summary>
        public IEconomicGenerator EconomicGenerator { get; set; }
        
        /// <summary>
        /// Distribute max revenue values to game assets for each turn
        /// </summary>
        public IEconomicDistributor EconomicDistributor { get; set; }

        /// <value>
        /// The rule that we're using to advance the game date.
        /// </value>
        public GameDateManager GameDateManager { get; set; }
        
        /// <value>
        /// Manages game rounds.
        /// </value>
        public RoundManager RoundManager { get; set; }
        
        /// <value>
        /// The rule that governs the timing of stages
        /// </value>
        public StageTimer StageTimer { get; set; }
        
        /// <value>
        /// Used to fetch objects from OpenSim before passing neutral entities on to the state objects
        /// </value>
        public OpenSimResolver Resolver { get; private set; }

        /// <value>
        /// Provide relevant group functionality for WaterWars
        /// </value>
        public OpenSimGroupsMediator Groups { get; set; }

        /// <value>
        /// Manages huds
        /// </value>
        public HudViewManager HudManager { get; set; }
        
        /// <summary>
        /// Don't use this directly.  Only here for test benefit.
        /// </summary>
        public Persister Persister { get; private set; }
        
        /// <summary>
        /// Rule to run when the game is started.
        /// </summary>
        public IStartupRule StartupRule { get; set; }        
        
        /// <summary>
        /// Record displays to which we want to send current game status
        /// </summary>
        protected HashSet<UUID> m_statusDisplayIds = new HashSet<UUID>();       

        /// <value>
        /// Expose scenes to the rest of the water wars code.
        /// </value>        
        public List<Scene> Scenes { get; private set; }

        /// <value>
        /// The game model.  This encompasses states like registration as well as actual game play
        /// </value>
        public Game Game { get; private set; }

        /// <value>
        /// The top level game view.  This is used to create other views.
        /// </value>
        public GameManagerView GameManagerView { get; set; }
        
        /// <summary>
        /// Current game state
        /// </summary>
        protected internal IGameState State
        {
            get
            {
                return m_state;
            }
            
            set
            {
                m_state = value;
                Game.State = m_state.Type;
            }
        }
        private IGameState m_state;

        /// <summary>
        /// Records game events for later analysis.
        /// </summary>
        protected Recorder m_recorder;
        
        /// <summary>
        /// Constructor.  Used by unit tests
        /// </summary>
        public WaterWarsController(
            Persister persister, Recorder recorder, IWaterForecaster forecaster, IRainfallGenerator rainfallGenerator, 
            IWaterDistributor distributor, IWaterAllocator allocator, 
            IEconomicForecaster economicForecaster, IEconomicGenerator economicGenerator, IEconomicDistributor economicDistributor, 
            UserAccount economyUserAccount)
            : this(new List<Scene>(), persister, recorder, forecaster, rainfallGenerator, distributor, allocator, 
                  economicForecaster, economicGenerator, economicDistributor, economyUserAccount) {}
        
        public WaterWarsController(
            List<Scene> scenes, Persister persister, Recorder recorder, IWaterForecaster forecaster,
            IRainfallGenerator rainfallGenerator, IWaterDistributor distributor, IWaterAllocator allocator,
            IEconomicForecaster economicForecaster, IEconomicGenerator economicGenerator, IEconomicDistributor economicDistributor, 
            UserAccount economyUserAccount)            
        {
            Scenes = scenes;
            EconomyUserAccount = economyUserAccount;
            Persister = persister;
            m_recorder = recorder;
            Forecaster = forecaster;
            RainfallGenerator = rainfallGenerator;
            WaterDistributor = distributor;
            WaterAllocator = allocator;
            EconomicForecaster = economicForecaster;
            EconomicGenerator = economicGenerator;
            EconomicDistributor = economicDistributor;
            
            // We have to do this in the constructor so that when the Dispatcher is set before initialize it has 
            // a valid class on which to start listening for events
            EventManager = new WaterWarsEventManager();            
        }

        /// <summary>
        /// Initialize the controller
        /// </summary>
        /// <param name="attachToVe">If true then the game is attached to the virtual environment</param>
        public void Initialise(bool attachToVe)
        {
            AttachedToVe = attachToVe;
            
            ModelFactory = new ModelFactory(this);
            Game = ModelFactory.CreateGame(UUID.Random(), "Game1");
                        
            Commands = new WaterWarsCommands(this);
            Resolver = new OpenSimResolver(this);            
            ViewerWebServices = new ViewerWebServices(this);
            HudManager = new HudViewManager(this);
            Events = new WaterWars.Events.Events(this);
            Feeds = new WaterWars.Feeds.Feeds(this);
            GameDateManager = new GameDateManager(this);
            RoundManager = new RoundManager(this);
            StageTimer = new StageTimer(this);
            
            RainfallGenerator.Initialize(EventManager);
            WaterDistributor.Initialize(EventManager);
            
            if (null != Persister)
                Persister.Initialize(this);
            
            if (null != m_recorder)
                m_recorder.Initialize(this);
            
            // This has to be called before we establish a game state so that the persisted game object
            // can first be created
            EventManager.TriggerSystemInitialized();
            
            new RegistrationState(this).Activate();
            
            if (AttachedToVe)
            {
                Groups = new OpenSimGroupsMediator(this);
                
                CheckVeRequirements();

                foreach (Scene scene in Scenes)
                {
                    // Stop players deleting or editing objects they 'own'
                    scene.Permissions.OnRezObject += delegate(int objectCount, UUID owner, Vector3 objectPosition, Scene myScene) {
                        return Groups.IsPlayerAnAdmin(owner);
                    };
                    scene.Permissions.OnDeleteObject += delegate(UUID objectID, UUID userID, Scene myScene) {
                        return Groups.IsPlayerAnAdmin(userID);
                    };
                    scene.Permissions.OnTakeObject += delegate(UUID objectID, UUID userID, Scene myScene) {
                        return Groups.IsPlayerAnAdmin(userID);               
                    };
                    scene.Permissions.OnTakeCopyObject += delegate(UUID objectID, UUID userID, Scene myScene) {
                        return Groups.IsPlayerAnAdmin(userID);                  
                    };  
                    scene.Permissions.OnEditObject += delegate(UUID objectID, UUID userID, Scene myScene) {
                        return Groups.IsPlayerAnAdmin(userID);
                    };
                    scene.Permissions.OnMoveObject += delegate(UUID objectID, UUID userID, Scene myScene) {
                        return Groups.IsPlayerAnAdmin(userID);          
                    };                                        
                    
                    EntityBase[] entities = scene.Entities.GetAllByType<SceneObjectGroup>();

                    // Pass 1 - pick up the game manager view (first level of the hierarchy)
                    foreach (EntityBase e in entities)
                    {
                        SceneObjectGroup so = (SceneObjectGroup)e;
                        
    //                    m_log.InfoFormat(
    //                        "[WATER WARS]: Pass 1 - processing {0} {1} at {2} in existing scene", 
    //                        so.Name, so.LocalId, so.AbsolutePosition);

                        // This is messy, but there's actually only one game manager from which objects can come.
                        if (so.Name == GameManagerView.IN_WORLD_NAME)
                        {
                            GameManagerView = new GameManagerView(this, scene);
                            GameManagerView.Initialize(so);
                            Dispatcher.RegisterGameManagerView(GameManagerView);
                        }
                        else if (
                            so.Name == StepBuiltDecorator.IN_WORLD_NAME
                            || so.Name == WaterAllocationDecorator.IN_WORLD_NAME)
                        {
                            // FIXME: Temporarily, just delete our one decorator by name.
                            // Eventually, the decorators will need to be re-registered with their game asset view.
                            so.Scene.DeleteSceneObject(so, false);
                        }                        
                    }
                }
                
                if (GameManagerView == null)
                    throw new Exception(
                        string.Format(
                            "Could not find GameManagerView named {0} in any of the scenes.  ABORTING.", 
                            GameManagerView.IN_WORLD_NAME));

                foreach (Scene scene in Scenes)
                {       
                    m_log.InfoFormat("[WATER WARS]: Processing buypoints on scene {0}", scene.RegionInfo.RegionName);                    
                    
                    // We'll keep track of these so that we can use them in pass 3
                    Dictionary<UUID, BuyPointView> buyPointViews = new Dictionary<UUID, BuyPointView>();
                    
                    EntityBase[] entities = scene.Entities.GetAllByType<SceneObjectGroup>();
                    
                    // Pass 2 - pick up the buy points (second level of the hierarchy)
                    foreach (EntityBase e in entities)
                    {
                        SceneObjectGroup so = (SceneObjectGroup)e;
                            
                        try
                        {
        //                    m_log.InfoFormat(
        //                        "[WATER WARS]: Pass 2 - processing {0} {1} at {2} in existing scene", 
        //                        so.Name, so.LocalId, so.AbsolutePosition);
    
                            IConfig config = GetPrimConfig(so);
                            
                            if (config != null)
                            {
                                AbstractGameAssetType modelType = GetModelTypeFromPrimConfig(config);
    
                                if (modelType == AbstractGameAssetType.Parcel)
                                {
                                    // We're using in-world buy point positioning to register them - not taking these from an
                                    // internal database and pushing them back up to the ve
                                    //
                                    // FIXME: The below might be old advice since I have now changed things such that
                                    // we can specify the uuid upfront
                                    //
                                    // We can't incorporate the first update buy point call within bpv.Initialize() because it
                                    // won't yet have registered with the dispatcher (which forms an intermediate layer between the
                                    // game logic and the view code).
                                    // We can't register with the dispatcher before initializing the view because no UUID will yet
                                    // exist for the dispatcher to record.
                                    // It might be possible to simplify this if we adapt OpenSim to allow us to specify the UUID
                                    // up-front.  But other VE systems may not allow this.
                                    BuyPoint bp = Resolver.RegisterBuyPoint(so);                                                                        
                                    BuyPointView bpv = GameManagerView.CreateBuyPointView(so, bp);
                                    buyPointViews.Add(bpv.Uuid, bpv);
                                    State.UpdateBuyPointStatus(bp);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            m_log.Error(
                                string.Format(
                                    "[WATER WARS]: Could not register {0} at {1} in {2}, ", 
                                    so.Name, so.AbsolutePosition, so.Scene.RegionInfo.RegionName), 
                                ex);
                        }
                    }

                    // Pass 3 - pick up the fields and game assets (third level of the hierarchy)
                    foreach (EntityBase e in entities)
                    {
                        SceneObjectGroup so = (SceneObjectGroup)e;
                            
                        try
                        {
                            IConfig config = GetPrimConfig(so);
                            
                            if (config != null)
                            {
                                AbstractGameAssetType modelType = GetModelTypeFromPrimConfig(config);
    
                                if (modelType == AbstractGameAssetType.Field)
                                {
                                    Field f = Resolver.RegisterField(so);
                                    BuyPointView bpv = null;
                                    if (buyPointViews.TryGetValue(f.BuyPoint.Uuid, out bpv))
                                        bpv.CreateFieldView(so);
                                    else
                                        throw new Exception(
                                            string.Format(
                                                "Could not find BuyPointView for field {0}, parcel {1} at {2}", 
                                                f.Name, f.BuyPoint.Name, f.BuyPoint.Location.LocalPosition));
                                }
                                else if (
                                    modelType == AbstractGameAssetType.Crops 
                                    || modelType == AbstractGameAssetType.Houses
                                    || modelType == AbstractGameAssetType.Factory)
                                {
                                    // For now, just delete any existing game assets immediately.
                                    so.Scene.DeleteSceneObject(so, false);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            m_log.Error(
                                string.Format(
                                    "[WATER WARS]: Could not register {0} at {1} in {2}, ", 
                                    so.Name, so.AbsolutePosition, so.Scene.RegionInfo.RegionName), 
                                ex);
                        }
                    }
                }

                HudManager.Initialise();
    
                // Register in-game objects as they are created
                //m_scene.EventManager.OnObjectBeingAddedToScene += OnObjectAddedToScene;

                // At the moment, reset the game immediately on restarting so that we have a blank slate
                State.ResetGame();
                
//                string statePath = Path.Combine(WaterWarsConstants.STATE_PATH, WaterWarsConstants.STATE_FILE_NAME);
//                if (File.Exists(statePath))
//                    RestoreGame(statePath);
            }           
        }
        
        /// <summary>
        /// Check that the requirements for running the game in a live virtual environment are met
        /// </summary>
        /// 
        /// <exception cref="GroupsSetupException">Thrown if a groups setup requirement wasn't met</exception>
        protected void CheckVeRequirements()
        {
            Groups.CheckForRequiredSetup();         
        }

//        protected void RestoreGame(string statePath)
//        {       
//            m_log.InfoFormat("[WATER WARS]: Restoring game from {0}", statePath);
//            
//            using (FileStream fs = new FileStream(statePath, FileMode.Open))
//            {
//                BinaryFormatter bf = new BinaryFormatter();
//                Game = (Game)bf.Deserialize(fs);
//            }      
//            
//            // Reconcile buy points (since these remain on the board even when everything else is wiped)
//            foreach (BuyPoint bp in Game.BuyPoints.Values)
//            {
//                Dispatcher.ChangeBuyPointSpecialization(
//                    bp, bp.ChosenGameAssetTemplate == null ? AbstractGameAssetType.None : bp.ChosenGameAssetTemplate.Type, bp.Fields == null ? 0 : bp.Fields.Count);
//            }
//            
//            // TODO: Restore objects
//            
//            // XXX: At the moment, we only ever restore to the build state
//            State = new BuildStageState(this);
//        }
        
        public void Close() {}

//        protected void OnObjectAddedToScene(SceneObjectGroup sog)
//        {
//            m_log.InfoFormat(
//                "[WATER WARS]: Got notice of {0} {1} at {2} added to scene", 
//                sog.Name, sog.LocalId, sog.AbsolutePosition);
//            
////            if (sog.Name == GameManager.IN_WORLD_NAME)
////                new GameManager(this, sog);
//        }

        /// <summary>
        /// Get our configuration object from a given prim
        /// </summary>
        /// <param name="so"></param>
        /// <returns>The config found, null if none could be found</returns>
        protected IConfig GetPrimConfig(SceneObjectGroup so)
        {
            IConfig config = null;
            
            IList<TaskInventoryItem> gmConfigItems 
                = so.RootPart.Inventory.GetInventoryItems(WaterWarsConstants.REGISTRATION_INI_NAME);
                            
            if (gmConfigItems.Count > 0)
            {
               
                AssetBase asset = Scenes[0].AssetService.Get(gmConfigItems[0].AssetID.ToString());
                IConfigSource gmConfigSource
                    = GameModelConfigurationParser.Parse(
                        SLUtil.ParseNotecardToString(Encoding.UTF8.GetString(asset.Data)));
                config = gmConfigSource.Configs[GameModelConfigurationParser.GENERAL_SECTION_NAME];            
            }
            
            return config;
        }
        
        /// <summary>
        /// Retrieve the model type from the prim config
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        protected AbstractGameAssetType GetModelTypeFromPrimConfig(IConfig config)
        {
            string rawModelType = config.GetString(GameModelConfigurationParser.MODEL_TYPE_KEY);
            return (AbstractGameAssetType)Enum.Parse(typeof(AbstractGameAssetType), rawModelType, true);            
        }       

        /// <summary>
        /// Reset a player's HUD back to the pre-game state
        /// </summary>
        /// <param name="playerId">
        /// A <see cref="UUID"/>
        /// </param>
        public void ResetHud(UUID playerId)
        {           
            HudManager.ResetHud(playerId);
        }
        
        /// <summary>
        /// Signal to the player's hud that it is their turn
        /// </summary>
        /// <param name="playerId"></param>
        public void SendGo(UUID playerId)
        {
            HudManager.SendGo(playerId);
        }

        /// <summary>
        /// Signal to a player's hud that it is no longer their turn
        /// </summary>
        /// <param name="playerId"></param>        
        public void SendStop(UUID playerId)
        {
            HudManager.SendStop(playerId);
        }
        
        public void UpdateHudTimeRemaining(int seconds)
        {
            HudManager.UpdateTimeRemaining(seconds);
        }

        /// <summary>
        /// Send a player's current status message to their HUD.  Nothing happens if no suitable hud is registered
        /// </summary>
        /// <param name="playerId"></param>
        /// <param name="message"></param>
        public void SendHudStatus(Player player, string message)
        {
            HudManager.SendHudStatus(player, message);
        }
    }
}