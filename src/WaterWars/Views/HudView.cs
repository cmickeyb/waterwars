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
using System.Text;
using System.Timers;
using log4net;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Scenes;
using WaterWars;
using WaterWars.Events;
using WaterWars.Models;
using WaterWars.Views.Interactions;
using WaterWars.Views.Widgets.Behaviours;
using WaterWars.Views.Widgets;

namespace WaterWars.Views
{        
    public class HudView : WaterWarsView
    {
//        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
                
        protected const string GAME_STARTED_MSG = "Game started.";        
        protected const string BUILD_PHASE_STARTED_MSG = "Build Phase Started.";
        protected const string WATER_PHASE_STARTED_MSG = "Water Phase Started.";
        protected const string REVENUE_PHASE_STARTED_MSG = "Revenue Phase Started.  Please wait while calculations are made.";
        protected const string GAME_RESET_MSG = "GAME RESET.";
        protected const string GAME_ENDED_MSG = "GAME ENDED.";
        
        protected const string PLAYER_TURN_ENDED_MSG = "{0} ended their turn.";
        
        public const string IN_WORLD_NAME = "Water Wars HUD";                
        public const string MONEY_BUTTON = "money-button";
        public const string LAND_BUTTON = "land-button";
        public const string WATER_BUTTON = "water-button";
//        public const string MARKET_BUTTON = "market-button";
        public const string STATUS_BUTTON = "status-button";
        public const string PHASE_BUTTON = "phase-button";
        public const string TIME_REMAINING_BUTTON = "time-remaining-button";
        public const string END_TURN_BUTTON = "end-turn-button";
        public const string SHOW_BROWSER_BUTTON = "show-browser-button";
        public const string TICKER_BUTTON = "ticker-button";

        protected MoneyButton m_moneyButton;
        protected LandButton m_landButton;
        protected WaterButton m_waterButton;
        public OsButton m_statusButton;
//        protected MarketButton m_marketButton;
        protected PhaseButton m_phaseButton;
        protected TimeRemainingButton m_timeRemainingButton;
        protected EndTurnButton m_endTurnButton;
        protected ShowBrowserButton m_showBrowserButton;
        protected TickerButton m_tickerButton;

        public UUID UserId { get; private set; }
        public uint RootLocalId { get; private set; }

        public int Money { set { m_moneyButton.Money = value; } }
        public int DevelopmentRightsOwned { set { m_landButton.DevelopmentRightsOwned = value; } }
        public int Water { set { m_waterButton.Water = value; } }
        public int WaterEntitlement { set { m_waterButton.WaterEntitlement = value; } }
        
        /// <value>
        /// Expressed in seconds
        /// </value>
        public int TimeRemaining { set { m_timeRemainingButton.TimeRemaining = value; } }
        
        public string Status { set { m_statusButton.LabelBehaviour.Text = value; } }
//        public bool EnableSellWater { set { m_marketButton.Enabled = value; } }        
        public bool EnableEndTurn { set { m_endTurnButton.Enabled = value; } }        

        public HudView(WaterWarsController controller, Scene scene, UUID userId) : base(controller, scene) 
        {
            UserId = userId;  
            
            m_controller.EventManager.OnStateStarted += ProcessStateStarted;
            m_controller.EventManager.OnPlayerEndedTurn += ProcessPlayerEndTurn;            
            m_controller.EventManager.OnGameAssetSoldToEconomy += ProcessGameAssetSoldToEconomy;
        }
        
        public override void Close()
        {   
            m_controller.EventManager.OnStateStarted -= ProcessStateStarted;
            m_controller.EventManager.OnPlayerEndedTurn -= ProcessPlayerEndTurn;            
            m_controller.EventManager.OnGameAssetSoldToEconomy -= ProcessGameAssetSoldToEconomy;
            
            m_moneyButton.Close();
            m_landButton.Close();
            m_waterButton.Close();
//            m_marketButton.Close();
            m_statusButton.Close();
            m_timeRemainingButton.Close();
            m_endTurnButton.Close();
            m_tickerButton.Close();
            m_showBrowserButton.Close();
            
            // We won't actually propogate close since we shouldn't kill the hud prims - the viewer has to do this!
            //base.Close();
        }        
        
        protected override void RegisterPart(SceneObjectPart part)
        {
            if (part.Name == MONEY_BUTTON)
                m_moneyButton = new MoneyButton(m_controller, part);
            else if (part.Name == LAND_BUTTON)
                m_landButton = new LandButton(m_controller, part);
            else if (part.Name == WATER_BUTTON)
                m_waterButton = new WaterButton(m_controller, part);
//            else if (part.Name == MARKET_BUTTON)
//                m_marketButton = new MarketButton(m_controller, part);
            else if (part.Name == STATUS_BUTTON)
                m_statusButton = new StatusButton(m_controller, part);
            else if (part.Name == PHASE_BUTTON)
                m_phaseButton = new PhaseButton(m_controller, part);
            else if (part.Name == TIME_REMAINING_BUTTON)
                m_timeRemainingButton = new TimeRemainingButton(m_controller, part);
            else if (part.Name == END_TURN_BUTTON)
                m_endTurnButton = new EndTurnButton(m_controller, part, UserId);
            else if (part.Name == SHOW_BROWSER_BUTTON)
                m_showBrowserButton = new ShowBrowserButton(m_controller, part, UserId);
            else if (part.Name == TICKER_BUTTON)
                m_tickerButton = new TickerButton(m_controller, part);

            if (part.IsRoot)
            {
                RootLocalId = part.LocalId;

                // XXX: Nasty nasty nasty hack to resolve an issue where attached non-root prims do not always appear
//                SceneObjectGroup group = part.ParentGroup;                
//                group.HasGroupChanged = true;
//                group.ScheduleGroupForFullUpdate();
            }
        }
        
        protected void ProcessStateStarted(GameStateType type)
        {
            string msg = null;
            
            if (type == GameStateType.Game_Starting)
                msg = GAME_STARTED_MSG;
            // We won't show the build phase started message because this will obscure the revenue details message.
            // The start of the game is taken care of by the game started message.
//            else if (type == GameStateType.Build)
//                msg = BUILD_PHASE_STARTED_MSG;
            else if (type == GameStateType.Water)
                msg = WATER_PHASE_STARTED_MSG;                     
            else if (type == GameStateType.Revenue)
                msg = REVENUE_PHASE_STARTED_MSG;
            else if (type == GameStateType.Game_Ended)
                msg = GAME_ENDED_MSG;            
            
            if (msg != null)
                m_controller.Events.Post(UserId, msg, EventLevel.All);
        }
        
        protected void ProcessPlayerEndTurn(Player p)
        {            
            m_controller.Events.Post(UserId, string.Format(PLAYER_TURN_ENDED_MSG, p.Name), EventLevel.Crawl);
            
            if (p.Uuid != UserId)
                return;
            
            EnableEndTurn = false;
        }
        
        protected void ProcessGameAssetSoldToEconomy(AbstractGameAsset ga, Player p, int price)
        {
            if (p.Uuid != UserId)
                return;
            
            DevelopmentRightsOwned = p.DevelopmentRightsOwned.Count;
            Money = p.Money;                    
            Water = p.Water;
            WaterEntitlement = p.WaterEntitlement;            
            
            m_statusButton.SendAlert(
                p.Uuid, "You just sold {0} to the market for {1}{2}", ga.Name, WaterWarsConstants.MONEY_UNIT, price);
        }
        
        public void AddTextToTick(string text)
        {
            m_tickerButton.AddTextToTick(text);
        }  
		
		public void SetTickerFromPreviousHud(HudView oldHud)
		{
			m_tickerButton.m_visibleTickText = oldHud.m_tickerButton.m_visibleTickText;
			m_tickerButton.m_bufferedTickText = oldHud.m_tickerButton.m_bufferedTickText;
		}
        
        /// <summary>
        /// The money display on the hud
        /// </summary>
        public class MoneyButton : WaterWarsButton
        {
            public int Money
            {
                set { LabelBehaviour.Text = string.Format("Money\n{0}{1}", WaterWarsConstants.MONEY_UNIT, value); }
            }
            
            public MoneyButton(WaterWarsController controller, SceneObjectPart part) 
                : base(controller, part, new FixedTextureBehaviour())
            {
                Money = 0;
            }
        }

        public class LandButton : WaterWarsButton
        {
            public const string LABEL_FORMAT = "Parcels owned\n{0}";

            public int DevelopmentRightsOwned
            {
//                set { LabelBehaviour.Text = string.Format(LABEL_FORMAT, value, ((value != 1) ? "s" : "")); }
                set { LabelBehaviour.Text = string.Format(LABEL_FORMAT, value); }
            }

            public LandButton(WaterWarsController controller, SceneObjectPart part) 
                : base(controller, part, new FixedTextureBehaviour()) {}
        }

        public class WaterButton : WaterWarsButton
        {
            public int Water
            {
                set
                {
                    m_water = value;
                    UpdateLabel();
                }
            }
            protected int m_water;

            public int WaterEntitlement
            {
                set
                {
                    m_waterEntitlement = value;
                    UpdateLabel();
                }
            }
            protected int m_waterEntitlement;
            
            public WaterButton(WaterWarsController controller, SceneObjectPart part) 
                : base(controller, part, new FixedTextureBehaviour())
            {
                Water = 0;
                WaterEntitlement = 0;
            }

            protected void UpdateLabel()
            {
                if (m_controller.Game.State == GameStateType.Build)
                    LabelBehaviour.Text = string.Format("Rights owned\n{0}", WaterWarsUtils.GetWaterUnitsText(m_waterEntitlement));
                else
                    LabelBehaviour.Text = string.Format("Available for use\n{0}", WaterWarsUtils.GetWaterUnitsText(m_water));
            }
        }        

        /// <summary>
        /// Tells the player what game phase we're in.
        /// </summary>
        public class PhaseButton : WaterWarsButton
        {
            public PhaseButton(WaterWarsController controller, SceneObjectPart part) 
                : base(controller, part, new FixedTextureBehaviour()) 
            {
                UpdateLabel(m_controller.Game.State);
                m_controller.EventManager.OnStateStarted += UpdateLabel;
            }
            
            protected void UpdateLabel(GameStateType newState)
            {
                LabelBehaviour.Text = string.Format("Phase\n{0}", newState.ToString().ToUpper());
            }
            
            public override void Close()
            {
                m_controller.EventManager.OnStateStarted -= UpdateLabel;
                base.Close();
            }
        }
        
        /// <summary>
        /// Tell the user how much time they have remaining in this phase.
        /// </summary>
        public class TimeRemainingButton : WaterWarsButton
        {
            public const string LABEL_FORMAT = "Time remaining\n{0:D2}:{1:D2}:{2:D2}";

            public int TimeRemaining 
            { 
                get 
                {
                    return m_timeRemaining;
                }
                set
                {
                    m_timeRemaining = value;
                    UpdateLabel();
                }
            }
            protected int m_timeRemaining;
            
            public TimeRemainingButton(WaterWarsController controller, SceneObjectPart part) 
                : base(controller, part, new FixedTextureBehaviour())
            {
                TimeRemaining = 0;
            }

            protected void UpdateLabel()
            {
                int hours = TimeRemaining / 3600;
                int secondsRemainder = TimeRemaining % 3600;
                int minutes = secondsRemainder / 60;
                int seconds = secondsRemainder % 60;                
                LabelBehaviour.Text = string.Format(LABEL_FORMAT, hours, minutes, seconds);
            }            
        }
        
        public class EndTurnButton : WaterWarsButton
        {
    //        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
            
            public EndTurnButton(WaterWarsController controller, SceneObjectPart part, UUID playerId) 
                : base(controller, part, new FadeInAndOutBehaviour())
            {
                DisplayBehaviour.Text = "end turn";
                OnClick 
                    += delegate(Vector3 offsetPos, IClientAPI remoteClient, SurfaceTouchEventArgs surfaceArgs)
                        { Util.FireAndForget(delegate { controller.State.EndTurn(playerId); }); };
            }             
        }

        public class ShowBrowserButton : WaterWarsButton
        {
    //        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
            
            protected UUID m_playerId;
            
            public ShowBrowserButton(WaterWarsController controller, SceneObjectPart part, UUID playerId) 
                : base(controller, part, new FadeInAndOutBehaviour())
            {           
                m_playerId = playerId;
                
                OnClick 
                    += delegate(Vector3 offsetPos, IClientAPI remoteClient, SurfaceTouchEventArgs surfaceArgs)
                        { OpenMediaBrowser(); };
                controller.EventManager.OnStateStarted += OnStateChange;
                
                Enabled = true;                
            }
            
            protected void OnStateChange(GameStateType newState)
            {
                if (newState == GameStateType.Game_Starting && m_controller.Game.Players.ContainsKey(m_playerId))
                    OpenMediaBrowser();
            }            
            
            public void OpenMediaBrowser()
            {
                // TODO: At the moment we assume that the webserver is on the same machine as the sim.  However,
                // at some point this needs to become more configurable
                string hostName = Part.ParentGroup.Scene.RegionInfo.ExternalHostName;

                UUID loginToken = m_controller.ViewerWebServices.PlayerServices.CreateLoginToken(m_playerId);
                
                string url 
                    = string.Format("http://{0}/waterwars?selfId={1}&loginToken={2}", hostName, m_playerId, loginToken);
                
                DialogModule.SendUrlToUser(
                    m_playerId, Part.Name, Part.UUID, Part.OwnerID, false, 
                    "Click \"Go to page\" to open the Water Wars User Interface", url);
            } 
            
            public override void Close()
            {
                // No need to remove OnClick since the part is going away anyway
                m_controller.EventManager.OnStateStarted -= OnStateChange;
            }
        }
        
        public class TickerButton : WaterWarsButton
        {
            /// <value>
            /// Text that separates crawls.
            /// </value>
            public string CrawlSeperator 
            {   
                get { return m_crawlSeperator; }
                set { m_crawlSeperator = value; m_emptyLead = new String(' ', m_crawlSeperator.Length); }
            }
            protected string m_crawlSeperator;
            
            protected Timer m_timer = new Timer(100);

            /// <value>
            /// Number of characters available in the ticker.
            /// </value>
            protected int m_widthAvailable = 150;

            /// <value>
            /// The text shown when the crawler is empty
            /// </value>
            protected string m_emptyCrawler;

            /// <value>
            /// Text shown when the lead part of the crawler (where new crawls are introduced) is empty.
            /// </value>
            protected string m_emptyLead = string.Empty;
            
            /// <value>
            /// The tick text that is visible
            /// </value>
            protected internal StringBuilder m_visibleTickText;

            /// <value>
            /// Tick text waiting to become visible
            /// </value>
            protected internal StringBuilder m_bufferedTickText = new StringBuilder();            
            
            public TickerButton(WaterWarsController controller, SceneObjectPart part)
                : base(controller, part, new FixedTextureBehaviour())
            {
                m_emptyCrawler = new string(' ', m_widthAvailable);
                m_visibleTickText = new StringBuilder(m_emptyCrawler);
                
                CrawlSeperator = " ... ";

//                AddTextToTick("Shall I compare thee to a summer's day?  Thou art more lovely and more temperate."
//                + "  Rough winds do shake the darling buds of May.  And summer's lease hath all too short a date."
//                + "  Sometime too hot the eye of heaven shines.  And often is his gold complexion dimm'd."
//                + "  And every fair from fair somtimes declines.  By chance, or nature's changing course, untrimm'd.");
                
                m_controller.EventManager.OnStateStarted += OnStateStarted;
                m_timer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
                m_timer.Start();
            }

            public override void Close()
            {
                m_controller.EventManager.OnStateStarted -= OnStateStarted;
                m_timer.Stop();

                base.Close();
            }            
            
            protected void OnStateStarted(GameStateType newState)
            {
                if (newState != GameStateType.Game_Resetting)
                    return;
                
                ClearText();
            }

            protected void OnTimedEvent(object source, ElapsedEventArgs e) 
            {
                lock (m_visibleTickText)
                {
                    // If we have some text to display or we need to blank the crawler then update the display
                    if (!(m_visibleTickText.ToString() == m_emptyCrawler && LabelBehaviour.Text == m_emptyCrawler))
                        LabelBehaviour.Text = m_visibleTickText.ToString();                                                    
    
                    if (m_bufferedTickText.Length != 0)
                    {                
                        m_visibleTickText.Remove(0, 1);
                        m_visibleTickText.Append(m_bufferedTickText[0]);
                        m_bufferedTickText.Remove(0, 1);                     
                    }
                    else if (m_visibleTickText.ToString() != m_emptyCrawler)
                    {
                        m_visibleTickText.Remove(0, 1);
                        m_visibleTickText.Append(" ");
                    }
                }
            }

            public void AddTextToTick(string text)
            {
                lock (m_visibleTickText)
                {
                    if (   m_bufferedTickText.Length > 0 
                        || m_bufferedTickText.Length == 0 && !m_visibleTickText.ToString().EndsWith(m_emptyLead))
                        m_bufferedTickText.Append(CrawlSeperator);
                    
                    m_bufferedTickText.Append(text);
                }
            }
            
            /// <summary>
            /// Clear the visible and buffered text.
            /// </summary>
            public void ClearText()
            {
                lock (m_visibleTickText)
                {
                    m_bufferedTickText.Length = 0;
                    m_visibleTickText.Length = 0;
                    m_visibleTickText.Append(m_emptyCrawler);
                }
            }
        }
        
        public class StatusButton : WaterWarsButton
        {
    //        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
            
            public StatusButton(WaterWarsController controller, SceneObjectPart part) 
                : base(controller, part, 1471, new FixedTextureBehaviour())
            {
                Enabled = true; // XXX: Temporarily enable so that it will pass on chat to the interaction hanging off it                
            }             
        }        
    }
}