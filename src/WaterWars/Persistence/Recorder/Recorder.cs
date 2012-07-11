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
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml;
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Scenes;
using WaterWars;
using WaterWars.Models;

namespace WaterWars.Persistence.Recorder
{
    /// <summary>
    /// Records game events for later analysis
    /// </summary>
    public class Recorder
    {       
//        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        public const string GameElement = "Game";
        public const string ConfigElement = "Config";          
        public const string RegionsElement = "Regions";
        public const string RegionElement = "Region";
        public const string StageElement = "Stage";
        public const string RoundElement = "Round";
        public const string SnapshotElement = "Snapshot";
        public const string EventsElement = "Events";
        
        public const string ParcelsElement = "Parcels";
        public const string ParcelElement = "Parcel";
        public const string PlayersElement = "Players";
        public const string PlayerElement = "Player";
        public const string AssetsElement = "Assets";
        public const string AssetElement = "Asset";
        
        public const string ParcelReferenceElement = "ParcelRef";
        public const string PlayerReferenceElement = "PlayerRef";
        public const string AssetReferenceElement = "AssetRef";
        public const string GroupReferenceElement = "GroupRef";
                
        public const string GameStartedStageName = "Game Start";
        public const string BuildStageName = "Build";
        public const string GameEndedStageName = "Game Ended";
        public const string GameResetStageName = "Game Reset";
                
        public const string ForecastsElement = "Forecasts";
        public const string ForecastElement = "Forecast";
        
        #region OpenSim events
        public const string ChatElement = "Chat";        
        public const string SpeakerElement = "Speaker";
        public const string HeardByElement = "HeardBy";
        
        public const string InstantMessageElement = "IM";
        public const string FromElement = "From";
        public const string ToElement = "To";
        #endregion
        
        #region UI events
        public const string SelectedElement = "AssetSelected";
        #endregion
        
        #region All play stage events
        public const string MoneyGivenElement = "MoneyGiven";
        public const string WaterGivenElement = "WaterGiven";
        public const string WaterRightsGivenElement = "WaterRightsGiven";
        #endregion
        
        #region Game starting events
        public const string ParcelGivenElement = "ParcelGiven";
        #endregion
        
        #region Build stage events
        public const string ParcelSoldElement = "ParcelSold";
        public const string TypeElement = "Type";        
        
        public const string WaterRightsSoldElement = "WaterRightsSold";
        
        public const string GameAssetBuildStartedElement = "AssetBuildStarted";        
        public const string GameAssetBuildContinuedElement = "AssetBuildContinued";
        public const string GameAssetBuildCompletedElement = "AssetBuildCompleted";
        
        public const string GameAssetUpgradedElement = "AssetUpgraded";   
        public const string OldLevelElement = "OldLevel";        
        
        public const string GameAssetSoldToEconomyElement = "AssetSoldToEconomy";
        public const string GameAssetRemovedElement = "AssetRemoved";
        #endregion
        
        #region Allocation stage events
        public const string WaterGeneratedElement = "WaterGenerated";
        public const string WaterAllocatedElement = "WaterAllocated";        
        #endregion
        
        #region Water stage events
        public const string WaterUsedElement = "WaterUsed";
        public const string WaterSoldElement = "WaterLeased";
        #endregion
        
        #region Revenue stage events
        public const string RevenueReceivedElement = "OperatingRevenueReceived";
        public const string RevenueElement = "OperatingRevenue";
        public const string CostsElement = "OperatingCosts";
        #endregion
        
        #region Elements used in common by events
        public const string BuyerElement = "Buyer";
        public const string SellerElement = "Seller";
        public const string ReceiverElement = "Receiver";
        public const string PriceElement = "Price";
        public const string MoneyElement = "Money";
        public const string WaterElement = "Water";
        public const string WaterRightsElement = "WaterRights";
        public const string SucceededElement = "Succeeded";
        public const string MessageElement = "Message";
        #endregion        
        
        protected WaterWarsController m_controller;
        
        /// <summary>
        /// Destination for records
        /// </summary>
        protected IDestination m_dest;
        
        protected XmlTextWriter m_xtw;
        
        public Recorder(IDestination dest)
        {
            m_dest = dest;
        }
        
        public void Initialize(WaterWarsController controller)
        {
            m_controller = controller;                        
            
            // OpenSim events
            if (m_controller.Scenes.Count > 0)
                m_controller.Scenes[0].EventManager.OnShutdown += Close;
            
            foreach (Scene scene in m_controller.Scenes)
            {
                scene.EventManager.OnChatToClients += ProcessChatToClients;
                scene.EventManager.OnNewClient += ProcessNewClient;
                scene.EventManager.OnClientClosed += ProcessClientClosed;
            }
            
            // UI events
            m_controller.EventManager.OnGameModelSelected += ProcessGameModelSelected;
            
            // General stage events
            m_controller.EventManager.OnStateStarted += ProcessStateStarted;            
            m_controller.EventManager.OnStateEnded += ProcessStateEnded; 
            
            // Any play stage events
            m_controller.EventManager.OnMoneyGiven += ProcessMoneyGiven;
            m_controller.EventManager.OnWaterGiven += ProcessWaterGiven;
            m_controller.EventManager.OnWaterRightsGiven += ProcessWaterRightsGiven;
            
            // Build stage events
            m_controller.EventManager.OnLandRightsBought += ProcessParcelBought;
            m_controller.EventManager.OnLandRightsSold += ProcessParcelSold;
            m_controller.EventManager.OnLandRightsGiven += ProcessParcelGiven;
            m_controller.EventManager.OnWaterRightsSold += ProcessWaterRightsSold;
            m_controller.EventManager.OnGameAssetBuildStarted += ProcessGameAssetBuildStarted;
            m_controller.EventManager.OnGameAssetBuildContinued += ProcessGameAssetBuildContinued;
            m_controller.EventManager.OnGameAssetBuildCompleted += ProcessGameAssetBuildCompleted;
            m_controller.EventManager.OnGameAssetUpgraded += ProcessGameAssetUpgraded;
            m_controller.EventManager.OnGameAssetSoldToEconomy += ProcessGameAssetSoldToEconomy;
            m_controller.EventManager.OnGameAssetRemoved += ProcessGameAssetRemoved;
            
            // Allocation stage events
            m_controller.EventManager.OnWaterGenerated += ProcessWaterGenerated;            
            m_controller.EventManager.OnWaterAllocated += ProcessWaterAllocated;
            
            // Water stage events
            m_controller.EventManager.OnWaterUsed += ProcessWaterUsed;            
            m_controller.EventManager.OnWaterSold += ProcessWaterSold;
            
            // Revenue stage events
            m_controller.EventManager.OnRevenueReceived += ProcessRevenueReceived;
        }
        
        public void Close()
        {
            m_controller.Scenes[0].EventManager.OnShutdown -= Close;          
            
            foreach (Scene scene in m_controller.Scenes)
            {
                scene.EventManager.OnChatToClients -= ProcessChatToClients;            
                scene.EventManager.OnNewClient += ProcessNewClient;
                scene.EventManager.OnClientClosed += ProcessClientClosed;
            }
            
            // UI events
            m_controller.EventManager.OnGameModelSelected -= ProcessGameModelSelected;
            
            // General stage events
            m_controller.EventManager.OnStateEnded -= ProcessStateEnded;            
            m_controller.EventManager.OnStateStarted -= ProcessStateStarted;
            
            // Build stage events
            m_controller.EventManager.OnLandRightsBought -= ProcessParcelBought;
            m_controller.EventManager.OnLandRightsSold -= ProcessParcelSold;
            m_controller.EventManager.OnWaterRightsSold -= ProcessWaterRightsSold;
            m_controller.EventManager.OnGameAssetBuildStarted -= ProcessGameAssetBuildStarted;
            m_controller.EventManager.OnGameAssetUpgraded -= ProcessGameAssetUpgraded;
            m_controller.EventManager.OnGameAssetSoldToEconomy -= ProcessGameAssetSoldToEconomy;
            m_controller.EventManager.OnGameAssetRemoved -= ProcessGameAssetRemoved;
            
            // Allocation stage events
            m_controller.EventManager.OnWaterGenerated -= ProcessWaterGenerated;            
            m_controller.EventManager.OnWaterAllocated -= ProcessWaterAllocated;
            
            // Water stage events
            m_controller.EventManager.OnWaterUsed -= ProcessWaterUsed;
            m_controller.EventManager.OnWaterSold -= ProcessWaterSold;
            
            // Revenue stage events
            m_controller.EventManager.OnRevenueReceived -= ProcessRevenueReceived;            
            
            EndRecording(GameResetStageName);
        }
        
        protected void StartRecording()
        {
            m_xtw = new XmlTextWriter(m_dest.GetTextWriter());
            m_xtw.Formatting = Formatting.Indented;
            m_xtw.WriteStartDocument(); 
            m_xtw.WriteStartElement(GameElement);
        }
        
        protected void EndRecording(string finalStageName)
        {
            // Can be called from Close()
            if (null == m_xtw)
                return;
            
            lock (this)
            {               
                RecordPhaseStart(finalStageName);                
                RecordSnapshot();            
                m_xtw.WriteEndDocument();
                m_xtw.Close();     
                m_xtw = null;
            }            
        }
        
        /// <summary>
        /// Write a vector to xml
        /// </summary>
        /// <param name="name"></param>
        /// <param name="vector"></param>
        protected void WriteVectorAttribute(string name, Vector3 vector)
        {
            m_xtw.WriteAttributeString(name, string.Format("{0}, {1}, {2}", vector.X, vector.Y, vector.Z));
        }
        
        protected void ProcessNewClient(IClientAPI client)
        {
            client.OnInstantMessage += ProcessInstantMessage;
        }        
        
        protected void ProcessClientClosed(UUID clientID, Scene scene)
        {
            IClientAPI client;
            scene.TryGetClient(clientID, out client);
            if (client != null)
                client.OnInstantMessage -= ProcessInstantMessage;
        }
             
        protected void ProcessGameModelSelected(Player p, AbstractGameModel gm)
        {
            if (null == m_xtw)
                return;
            
            if (!(gm is AbstractGameAsset || gm is BuyPoint))
                return;
            
            lock (this)
            {
                RecordEventStart(SelectedElement);                
                RecordPlayerReference(p);
                if (gm is AbstractGameAsset)
                    RecordGameAssetReference(gm as AbstractGameAsset);
                else
                    RecordBuyPointReference(gm as BuyPoint);
                RecordEventEnd();                
            }
        }       
        
        #region OpenSim events
        protected void ProcessChatToClients(UUID senderID, HashSet<UUID> receiverIDs, 
            string message, ChatTypeEnum type, Vector3 fromPos, string fromName, 
            ChatSourceType src, ChatAudibleLevel level)
        {
            if (null == m_xtw)
                return;
            
            if (ChatTypeEnum.DebugChannel == type | ChatTypeEnum.StartTyping == type | ChatTypeEnum.StopTyping == type)
                return;
            
            Player speaker;       
            List<Player> heardBy = new List<Player>();
            
            lock (m_controller.Game.Players)            
            {
                m_controller.Game.Players.TryGetValue(senderID, out speaker);
            
                if (null == speaker)
                    return;
            
                foreach (UUID receiverId in receiverIDs)
                    if (m_controller.Game.Players.ContainsKey(receiverId))
                        heardBy.Add(m_controller.Game.Players[receiverId]);
            }            
                        
            lock (this)
            {
                RecordEventStart(ChatElement);                
                m_xtw.WriteStartElement(SpeakerElement);
                RecordPlayerReference(speaker);
                m_xtw.WriteEndElement();
                m_xtw.WriteStartElement(HeardByElement);
                foreach (Player p in heardBy)
                    RecordPlayerReference(p);
                m_xtw.WriteEndElement();
                m_xtw.WriteElementString(MessageElement, message);
                RecordEventEnd();
            }
        }
        
        protected void ProcessInstantMessage(IClientAPI client, GridInstantMessage msg)
        {
//            m_log.InfoFormat(
//                "[WATER WARS]: Received instant message {0} from {1} to {2}, session {3}, dialog {4}", 
//                msg.message, msg.fromAgentID, msg.toAgentID, msg.imSessionID, (InstantMessageDialog)msg.dialog);
            
            if (null == m_xtw)
                return;
            
            if (msg.dialog != (byte)InstantMessageDialog.MessageFromAgent 
                && msg.dialog != (byte)InstantMessageDialog.SessionSend)             
                return;
            
            Player sender;
            Player receiver;            
            lock (m_controller.Game.Players)
            {
                m_controller.Game.Players.TryGetValue(new UUID(msg.fromAgentID), out sender);
                m_controller.Game.Players.TryGetValue(new UUID(msg.toAgentID), out receiver);
            }
            
            if (null == sender)
                return;
            
            if (null == receiver && msg.imSessionID != m_controller.Groups.WaterWarsGroup.GroupID.Guid)
            {
//                m_log.InfoFormat(
//                    "[WATER WARS]: Not delivering message {0} from {1} to {2} because receiver is {3}, msg.imSessionID is {4} and Water Wars group id is {5}", 
//                    msg.message, msg.fromAgentID, msg.toAgentID, receiver, 
//                    msg.imSessionID, m_controller.Groups.WaterWarsGroup.GroupID.Guid);
                return;
            }
            
            lock (this)
            {
                RecordEventStart(InstantMessageElement);
                m_xtw.WriteStartElement(FromElement);
                RecordPlayerReference(sender);
                m_xtw.WriteEndElement();
                m_xtw.WriteStartElement(ToElement);
                if (null != receiver)
                    RecordPlayerReference(receiver);
                else
                    RecordGroupReference(m_controller.Groups.WaterWarsGroup);                    
                m_xtw.WriteEndElement();
                m_xtw.WriteElementString(MessageElement, msg.message);
                RecordEventEnd();
            }
        }
        #endregion
                
        protected void ProcessGameStarted()
        {       
            lock (this)
            {
                StartRecording();
                
                RecordPhaseStart(GameStartedStageName);
                RecordConfiguration(m_controller.Configuration);                           
            }
        } 
        
        protected void ProcessStateStarted(GameStateType stateType)
        {
            if (GameStateType.Game_Starting == stateType)
                ProcessGameStarted();
            
            if (null == m_xtw)
                return;
            
            if (GameStateType.Game_Ended == stateType)
                ProcessGameEnded();
            else if (GameStateType.Game_Resetting == stateType)
                ProcessGameReset();
            
            // Process other states that we can handle generically
            lock (this)
            {
                if (
                    GameStateType.Build == stateType
                    || GameStateType.Allocation == stateType 
                    || GameStateType.Water == stateType 
                    || GameStateType.Revenue == stateType)
                {
                    if (GameStateType.Build == stateType)
                        RecordRoundStart();
                    
                    RecordPhaseStart(stateType.ToString());
                    RecordSnapshot();
                    m_xtw.WriteStartElement(EventsElement);
                }
            }
        }      
        
        protected void ProcessStateEnded(GameStateType stateType)
        {
            if (null == m_xtw)
                return;
            
            lock (this)
            {
                if (GameStateType.Game_Starting == stateType 
                    || GameStateType.Build == stateType 
                    || GameStateType.Water == stateType 
                    || GameStateType.Revenue == stateType
                    || GameStateType.Allocation == stateType)
                {
//                    System.Console.WriteLine("Processing end state {0}", stateType.ToString());
                    
                    // Only write the events end element if we are dealing with a play state
                    if (GameStateType.Build == stateType
                        || GameStateType.Water == stateType
                        || GameStateType.Revenue == stateType
                        || GameStateType.Allocation == stateType)                        
                        m_xtw.WriteEndElement();                 
                    
                    RecordPhaseEnd(); 
                
                    if (GameStateType.Revenue == stateType)                               
                        RecordRoundEnd();                     
                }
            }
        }        
        
        protected void ProcessGameEnded()
        {
            EndRecording(GameEndedStageName);
        }

        protected void ProcessGameReset()
        {
            EndRecording(GameResetStageName);
        }  
        
        protected void ProcessMoneyGiven(Player p, int amount)
        {
            ProcessResourceGiven(p, amount, MoneyGivenElement, MoneyElement);           
        }
        
        protected void ProcessWaterGiven(Player p, int amount)
        {
            ProcessResourceGiven(p, amount, WaterGivenElement, WaterElement);               
        }   
        
        protected void ProcessWaterRightsGiven(Player p, int amount)
        {
            ProcessResourceGiven(p, amount, WaterRightsGivenElement, WaterRightsElement);      
        }      
        
        /// <summary>
        /// Generic method for recording resource gifts (this doesn't include parcel gifts)
        /// </summary>
        /// <param name="p"></param>
        /// <param name="amount"></param>
        /// <param name="eventElement"></param>
        /// <param name="amountElement"></param>
        protected void ProcessResourceGiven(Player p, int amount, string eventElement, string amountElement)
        {
            if (null == m_xtw)
                return;  
            
            lock (this)
            {
                RecordEventStart(eventElement);                                   
                m_xtw.WriteStartElement(ReceiverElement);
                RecordPlayerReference(p);        
                m_xtw.WriteEndElement();                   
                m_xtw.WriteElementString(amountElement, amount.ToString());
                RecordEventEnd();
            }             
        }
      
        #region Build stage events
        protected void ProcessParcelBought(BuyPoint bp, Player buyer)
        {
            ProcessGeneralParcelSold(
                bp, buyer, Player.None, RightsType.Combined, bp.CombinedPrice, bp.InitialWaterRights, true);
        }
        
        protected void ProcessParcelSold(
            BuyPoint bp, Player buyer, Player seller, RightsType type, int price, bool success)
        {
            ProcessGeneralParcelSold(bp, buyer, seller, type, price, 0, success);
        }
        
        protected void ProcessGeneralParcelSold(
            BuyPoint bp, Player buyer, Player seller, RightsType type, int price, int waterRights, bool success)
        {
            if (null == m_xtw)
                return;
            
            lock (this)
            {
                RecordEventStart(ParcelSoldElement);
                RecordBuyPointReference(bp);                
                m_xtw.WriteStartElement(BuyerElement);                                    
                RecordPlayerReference(buyer);        
                m_xtw.WriteEndElement();                   
                m_xtw.WriteStartElement(SellerElement);
                RecordPlayerReference(seller);
                m_xtw.WriteEndElement();
                m_xtw.WriteElementString(PriceElement, price.ToString());
                m_xtw.WriteElementString(WaterRightsElement, waterRights.ToString());
                m_xtw.WriteElementString(SucceededElement, success.ToString());
                RecordEventEnd();
            }
        }
        
        protected void ProcessParcelGiven(BuyPoint bp)
        {
            if (null == m_xtw)
                return;
            
            lock (this)
            {
                RecordEventStart(ParcelGivenElement);
                RecordBuyPointReference(bp);                
                m_xtw.WriteStartElement(ReceiverElement);                 
                RecordPlayerReference(bp.DevelopmentRightsOwner);        
                m_xtw.WriteEndElement();                  
                m_xtw.WriteElementString(WaterRightsElement, bp.InitialWaterRights.ToString());
                RecordEventEnd();
            }
        }      
        
        protected void ProcessWaterRightsSold(Player buyer, Player seller, int price, int amount, bool success)
        {
            if (null == m_xtw)
                return;
            
            lock (this)
            {
                RecordEventStart(WaterRightsSoldElement);
                m_xtw.WriteStartElement(BuyerElement);
                RecordPlayerReference(buyer);
                m_xtw.WriteEndElement();
                m_xtw.WriteStartElement(SellerElement);
                RecordPlayerReference(seller);
                m_xtw.WriteEndElement();
                m_xtw.WriteElementString(WaterElement, amount.ToString());
                m_xtw.WriteElementString(PriceElement, price.ToString());
                m_xtw.WriteElementString(SucceededElement, success.ToString());
                RecordEventEnd();            
            }
        }
        
        protected void ProcessGameAssetBuildStarted(AbstractGameAsset ga)
        {
            ProcessGameAssetBuildEvent(ga, GameAssetBuildStartedElement);
        }
        
        protected void ProcessGameAssetBuildContinued(AbstractGameAsset ga)
        {
            ProcessGameAssetBuildEvent(ga, GameAssetBuildContinuedElement);
        }
        
        protected void ProcessGameAssetBuildCompleted(AbstractGameAsset ga)
        {
            ProcessGameAssetBuildEvent(ga, GameAssetBuildCompletedElement);
        }  
        
        protected void ProcessGameAssetBuildEvent(AbstractGameAsset ga, string eventElementName)
        {
            if (null == m_xtw)
                return;
            
            lock (this)
            {
                RecordEventStart(eventElementName);
                RecordPlayerReference(ga.Field.Owner);        
                RecordBuyPointReference(ga.Field.BuyPoint);
                RecordGameAsset(ga);
                RecordEventEnd();
            }            
        }
        
        protected void ProcessGameAssetUpgraded(AbstractGameAsset ga, int oldLevel)
        {
            if (null == m_xtw)
                return;
            
            lock (this)
            {
                RecordEventStart(GameAssetUpgradedElement);
                RecordPlayerReference(ga.Field.Owner);
                m_xtw.WriteElementString(OldLevelElement, oldLevel.ToString());
                m_xtw.WriteElementString(PriceElement, (ga.ConstructionCost - ga.ConstructionCosts[oldLevel]).ToString());
                RecordGameAsset(ga);
                RecordEventEnd();
            }
        }       
        
        protected void ProcessGameAssetSoldToEconomy(AbstractGameAsset ga, Player previousOwner, int price)
        {
            if (null == m_xtw)
                return;
            
            lock (this)
            {
                RecordEventStart(GameAssetSoldToEconomyElement);
                RecordGameAssetReference(ga);
                m_xtw.WriteStartElement(SellerElement);
                RecordPlayerReference(previousOwner);
                m_xtw.WriteEndElement();
                m_xtw.WriteElementString(PriceElement, price.ToString());
                RecordEventEnd();
            }
        }        
        
        protected void ProcessGameAssetRemoved(AbstractGameAsset ga)
        {
            if (null == m_xtw)
                return;
            
            lock (this)
            {
                RecordEventStart(GameAssetRemovedElement);
                RecordGameAssetReference(ga);
                RecordPlayerReference(ga.Field.Owner);                
                RecordEventEnd();
            }
        }
        #endregion
        
        #region Allocation stage processors
        protected void ProcessWaterGenerated(int water)
        {
            if (null == m_xtw)
                return;
            
            lock (this)
            {
                RecordEventStart(WaterGeneratedElement);
                m_xtw.WriteElementString(WaterElement, water.ToString());
                RecordEventEnd();
            }
        }
        
        protected void ProcessWaterAllocated(Player p, int water)
        {
            if (null == m_xtw)
                return;
            
            lock (this)
            {                
                RecordEventStart(WaterAllocatedElement);
                RecordPlayerReference(p);
                m_xtw.WriteElementString(WaterElement, water.ToString());
                RecordEventEnd();
            }
        }
        #endregion
        
        #region Water stage processors
        protected void ProcessWaterUsed(AbstractGameAsset ga, Player p, int water)
        {
            if (null == m_xtw)
                return;
            
            lock (this)
            {
                RecordEventStart(WaterUsedElement);
                RecordGameAssetReference(ga);
                RecordPlayerReference(p);
                m_xtw.WriteElementString(WaterElement, water.ToString());
                RecordEventEnd();
            }
        }
        
        protected void ProcessWaterSold(Player buyer, Player seller, int amount, int price, bool succeeded)
        {
            if (null == m_xtw)
                return;
            
            lock (this)
            {
                RecordEventStart(WaterSoldElement);
                m_xtw.WriteStartElement(BuyerElement);
                RecordPlayerReference(buyer);
                m_xtw.WriteEndElement();
                m_xtw.WriteStartElement(SellerElement);
                RecordPlayerReference(seller);
                m_xtw.WriteEndElement();
                m_xtw.WriteElementString(WaterElement, amount.ToString());
                m_xtw.WriteElementString(PriceElement, price.ToString());
                m_xtw.WriteElementString(SucceededElement, succeeded.ToString());
                RecordEventEnd();
            }
        }
        #endregion
        
        #region Revenue stage processors
        protected void ProcessRevenueReceived(Player p, int operatingRevenue, int operatingCosts, int costOfLiving)
        {
            if (null == m_xtw)
                return;
            
            lock (this)
            {
                RecordEventStart(RevenueReceivedElement);
                RecordPlayerReference(p);
                m_xtw.WriteElementString(RevenueElement, operatingRevenue.ToString());
                m_xtw.WriteElementString(CostsElement, operatingCosts.ToString());
                m_xtw.WriteElementString("CostOfLiving", costOfLiving.ToString());
                RecordEventEnd();
            }
        }
        #endregion
        
        protected void RecordRoundStart()
        {
            m_xtw.WriteStartElement(RoundElement);
            m_xtw.WriteAttributeString("Current", m_controller.Game.CurrentRound.ToString());
            m_xtw.WriteAttributeString("Total", m_controller.Game.TotalRounds.ToString());
            m_xtw.WriteStartElement(ForecastsElement);
            RecordForecast("Water", m_controller.Game.Forecast.Water);
            RecordForecast("FactoryOutputDemand", m_controller.Game.Forecast.Economic[AbstractGameAssetType.Factory][1]);
            RecordForecast("SingleFamilyHomeDemand", m_controller.Game.Forecast.Economic[AbstractGameAssetType.Houses][1]);
            RecordForecast("MultiFamilyHomeDemand", m_controller.Game.Forecast.Economic[AbstractGameAssetType.Houses][2]);
            RecordForecast("EstateHomeDemand", m_controller.Game.Forecast.Economic[AbstractGameAssetType.Houses][3]);
            m_xtw.WriteEndElement();
        }
        
        protected void RecordForecast(string forecastName, string forecast)
        {
            m_xtw.WriteStartElement(ForecastElement);
            m_xtw.WriteAttributeString("Name", forecastName);
            m_xtw.WriteString(forecast);
            m_xtw.WriteEndElement();
        }
        
        protected void RecordRoundEnd()
        {
            m_xtw.WriteEndElement();
        }
        
        protected void RecordPhaseStart(string type)
        {
            m_xtw.WriteStartElement(StageElement);
            m_xtw.WriteAttributeString("Type", type);
            RecordTimeAttributes();
        }
        
        protected void RecordTimeAttributes()
        {            
            m_xtw.WriteAttributeString("RealTime", DateTime.Now.ToUniversalTime().ToString("u"));
            m_xtw.WriteAttributeString("GameTime", m_controller.Game.CurrentDate.ToString("u"));
        }
        
        protected void RecordPhaseEnd()
        {
            m_xtw.WriteEndElement(); 
            
            // We'll keep writing out so that we don't lose too much of the record if the system crashes
            m_xtw.Flush();            
        }
        
        protected void RecordEventStart(string elementName)
        {
            m_xtw.WriteStartElement(elementName);
            RecordTimeAttributes();
        }
        
        protected void RecordEventEnd()
        {
            m_xtw.WriteEndElement();
        }
        
        /// <summary>
        /// Record a snapshot of the current game state
        /// </summary>
        /// <param name="players"></param>
        /// <param name="buyPoints"></param>
        protected void RecordSnapshot()
        {          
            m_xtw.WriteStartElement(SnapshotElement);
            RecordParcels();
            RecordPlayers();              
            m_xtw.WriteEndElement();
        }
        
        protected void RecordStandardAttributes(AbstractModel am)
        {
            m_xtw.WriteAttributeString("Name", am.Name);
            m_xtw.WriteAttributeString("ID", am.Uuid.ToString());              
        }
        
        protected void RecordAttributes(AbstractGameAsset ga)
        {
            RecordStandardAttributes(ga);
            WriteVectorAttribute("Position", ga.Position);             
        }
        
        protected void RecordAttributes(BuyPoint bp)
        {
            RecordStandardAttributes(bp);
            m_xtw.WriteAttributeString("Region", bp.Location.RegionName);
            m_xtw.WriteAttributeString("RegionID", bp.Location.RegionId.ToString());
            WriteVectorAttribute("LocalPosition", bp.Location.LocalPosition);
            m_xtw.WriteAttributeString("Zone", bp.Zone != null ? bp.Zone : "");
        }
        
        protected void RecordConfiguration(IConfigSource originalConfig)
        {
            m_xtw.WriteStartElement(ConfigElement);
            
            // Write out game configuration
            // We have to transfer this to an xml document to get xml                            
            XmlConfigSource intermediateConfig = new XmlConfigSource();
            intermediateConfig.Merge(originalConfig);
            
            // This is a roundabout way to get rid of the top xml processing directive that config.ToString() gives 
            // us
            XmlDocument outputDoc = new XmlDocument();
            outputDoc.LoadXml(intermediateConfig.ToString());
            
            // Remove the other document's processing instruction
            outputDoc.RemoveChild(outputDoc.FirstChild);
            
            outputDoc.WriteTo(m_xtw);
            
            m_xtw.WriteStartElement(RegionsElement);
            // Write region names, positions and IDs
            foreach (Scene scene in m_controller.Scenes)
            {
                m_xtw.WriteStartElement(RegionElement);
                m_xtw.WriteAttributeString("Name", scene.RegionInfo.RegionName);
                m_xtw.WriteAttributeString("X", scene.RegionInfo.RegionLocX.ToString());
                m_xtw.WriteAttributeString("Y", scene.RegionInfo.RegionLocY.ToString());
                m_xtw.WriteEndElement();
            }
            m_xtw.WriteEndElement();
            
            m_xtw.WriteEndElement();             
        }
        
        protected void RecordGroupReference(GroupRecord gr)
        {
            m_xtw.WriteStartElement(GroupReferenceElement);
            m_xtw.WriteAttributeString("name", gr.GroupName);
            m_xtw.WriteEndElement();
        }
        
        protected void RecordParcels()
        {
            m_xtw.WriteStartElement(ParcelsElement);
                                 
            List<BuyPoint> buyPoints;                        
            lock (m_controller.Game.BuyPoints)
                buyPoints = m_controller.Game.BuyPoints.Values.ToList();
                        
            foreach (BuyPoint bp in buyPoints)
            {
                m_xtw.WriteStartElement(ParcelElement);
                RecordAttributes(bp);
                m_xtw.WriteElementString("HasAnyOwner", bp.HasAnyOwner.ToString());
                m_xtw.WriteElementString("Owner", bp.DevelopmentRightsOwner.Name);
                m_xtw.WriteElementString("InitialPrice", bp.CombinedPrice.ToString());
                m_xtw.WriteElementString("InitialWaterRights", bp.InitialWaterRights.ToString());
                m_xtw.WriteElementString("ChosenUse", bp.ChosenGameAssetTemplate.Type.ToString());                
                
                m_xtw.WriteStartElement(AssetsElement);
                
                // We're doing this under lock but this is okay since event recording itself shouldn't start with
                // a lock
                lock (bp.GameAssets)
                {
                    foreach (AbstractGameAsset ga in bp.GameAssets.Values)
                        RecordGameAsset(ga);
                }
                
                m_xtw.WriteEndElement();
                
                m_xtw.WriteEndElement();                    
            }
            
            m_xtw.WriteEndElement();     
        }
        
        protected void RecordBuyPointReference(BuyPoint bp)
        {
            m_xtw.WriteStartElement(ParcelReferenceElement);
            RecordAttributes(bp);            
            m_xtw.WriteEndElement();
        }
        
        protected void RecordGameAsset(AbstractGameAsset ga)
        {
            m_xtw.WriteStartElement(AssetElement);
            RecordAttributes(ga);
            m_xtw.WriteElementString("Level", ga.Level.ToString());
            m_xtw.WriteElementString("MinLevel", ga.MinLevel.ToString());
            m_xtw.WriteElementString("MaxLevel", ga.MaxLevel.ToString());
            m_xtw.WriteElementString(PriceElement, ga.ConstructionCost.ToString());
            m_xtw.WriteElementString("PricePerBuildStep", ga.ConstructionCostPerBuildStep.ToString());
            m_xtw.WriteElementString("StepsBuilt", ga.StepsBuilt.ToString());
            m_xtw.WriteElementString("StepsRequired", ga.StepsToBuild.ToString());
            m_xtw.WriteElementString("StepBuiltThisTurn", ga.StepBuiltThisTurn.ToString());
            m_xtw.WriteElementString("IsMultiStepBuild", ga.IsMultiStepBuild.ToString());
            m_xtw.WriteElementString("IsBuilt", ga.IsBuilt.ToString());
            m_xtw.WriteElementString("IsDependentOnWaterToExist", ga.IsDependentOnWaterToExist.ToString());
            m_xtw.WriteElementString("IsSoldToEconomy", ga.IsSoldToEconomy.ToString());
            m_xtw.WriteElementString("MarketPrice", ga.MarketPrice.ToString());
            m_xtw.WriteElementString("NormalOperatingRevenue", ga.NormalRevenue.ToString());
            m_xtw.WriteElementString("OperatingRevenue", ga.ProjectedRevenue.ToString());
            m_xtw.WriteElementString("NominalMaximumProfitThisTurn", ga.NominalMaximumProfitThisTurn.ToString());
            m_xtw.WriteElementString("Profit", ga.Profit.ToString());
            m_xtw.WriteElementString("WaterUsage", ga.WaterUsage.ToString());
            m_xtw.WriteElementString("WaterAllocated", ga.WaterAllocated.ToString());
            m_xtw.WriteElementString("MaintenanceCost", ga.MaintenanceCost.ToString());
            m_xtw.WriteElementString("AccruedMaintenanceCost", ga.AccruedMaintenanceCost.ToString());            
            m_xtw.WriteElementString("TimeToLive", ga.TimeToLive.ToString());
            m_xtw.WriteElementString("CanBeAllocatedWater", ga.CanBeAllocatedWater.ToString());
            m_xtw.WriteElementString("CanBePartiallyAllocatedWater", ga.CanPartiallyAllocateWater.ToString());
            m_xtw.WriteElementString("CanBeSoldToEconomy", ga.CanBeSoldToEconomy.ToString());
            m_xtw.WriteElementString("CanUpgradeInPrinciple", ga.CanUpgradeInPrinciple.ToString());
            m_xtw.WriteElementString("CanUpgrade", ga.CanUpgrade.ToString());
            m_xtw.WriteEndElement();             
        }
        
        protected void RecordGameAssetReference(AbstractGameAsset ga)
        {
            m_xtw.WriteStartElement(AssetReferenceElement);
            RecordAttributes(ga);
            m_xtw.WriteEndElement();
        }
        
        protected void RecordPlayers()
        {
            m_xtw.WriteStartElement(PlayersElement);
            
            List<Player> players;
            lock (m_controller.Game.Players)
                players = m_controller.Game.Players.Values.ToList();
            
            foreach (Player p in players)
            {
                m_xtw.WriteStartElement(PlayerElement);
                RecordStandardAttributes(p);       
                m_xtw.WriteElementString("Role", p.Role.Type.ToString());
                m_xtw.WriteElementString(MoneyElement, p.Money.ToString());
                m_xtw.WriteElementString(WaterRightsElement, p.WaterEntitlement.ToString());
                m_xtw.WriteElementString("WaterRequired", p.WaterRequired.ToString());
                m_xtw.WriteElementString(WaterElement, p.Water.ToString());                   
                m_xtw.WriteElementString("Parcels", p.DevelopmentRightsOwnedCount.ToString());
                m_xtw.WriteElementString("CostOfLiving", p.CostOfLiving.ToString());
                m_xtw.WriteElementString("MaintenanceCosts", p.MaintenanceCosts.ToString());
                m_xtw.WriteEndElement();
            }   
            
            m_xtw.WriteEndElement();
        }
        
        protected void RecordPlayerReference(Player p)
        {
            m_xtw.WriteStartElement(PlayerReferenceElement);
            RecordStandardAttributes(p);
            m_xtw.WriteEndElement();
        }        
    }
}