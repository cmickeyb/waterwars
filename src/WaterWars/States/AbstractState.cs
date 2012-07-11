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
using WaterWars.Events;
using WaterWars.Models;

namespace WaterWars.States
{       
    public abstract class AbstractState : IGameState
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);      
        
        public GameStateType Type { get; private set; }
        
        /// <value>
        /// The game model.  This encompasses states like registration as well as actual game play
        /// </value>
        public Game Game { get; private set; }        

        /// <value>
        /// The game controller.
        /// </value>
        protected WaterWarsController m_controller;     

        public AbstractState(WaterWarsController controller, GameStateType type)
        {
            m_controller = controller;            
            Game = m_controller.Game;
            Type = type;
        }
        
        /// <summary>
        /// Used only by Abstract state and the controller initially to get things going.  StartState() is the one that is overriden by descendent
        /// classes
        /// </summary>
        public void Activate()
        {
            m_log.InfoFormat("[WATER WARS]: Starting state {0}", Type);
            
            StartState();
            
            // This has to take place after the state has been fully started so that event listeners will see the
            // full consequences.
            m_controller.EventManager.TriggerStateStarted(Type);            
            
            PostStartState();            
        }

        /// <summary>
        /// Start the state.  This is executed on construction of the stage.
        /// </summary>
        protected virtual void StartState()
        {
            // We have to set the state early so that status updates sent by the controller will have the correct state
            m_controller.State = this;
        }
        
        /// <summary>
        /// Implemented by states that want to carry out activity immediately after the state has started rather than
        /// be user driven.
        /// </summary>
        protected virtual void PostStartState() {}
        
        protected void EndState(AbstractState nextState)
        {
            m_controller.EventManager.TriggerStateEnded(Type);                        
            
            nextState.Activate();
        }     

        /// <summary>
        /// Update the status of all registered game objects
        /// </summary>
        /// <remarks>
        /// Triggers the change event for every game asset.  This is necessary when we move back and forth between
        /// build and water states since every asset can change its owner action hints.
        /// </remarks>
        protected void UpdateAllStatus()
        {           
            List<Player> players;
            lock (Game.Players)
                players = Game.Players.Values.ToList();
            
            foreach (Player p in players)
            {
                p.TriggerChanged();
                UpdateHudStatus(p); // FIXME: This should be done via the TriggerChanged event.
            }
            
            List<BuyPoint> buyPoints;
            lock (Game.BuyPoints)
                buyPoints = Game.BuyPoints.Values.ToList();
            
            foreach (BuyPoint bp in buyPoints)
            {
                bp.TriggerChanged();
                
                List<AbstractGameAsset> assets;
                lock (bp.GameAssets)
                    assets = bp.GameAssets.Values.ToList();
                
                foreach (AbstractGameAsset ga in assets)
                    ga.TriggerChanged();
                
                List<Field> fields;
                lock (bp.Fields)
                    fields = bp.Fields.Values.ToList();
                
                foreach (Field f in fields)
                    f.TriggerChanged();                    
            }
        }
        
        public virtual bool UpdateHudStatus(UUID playerId)
        {
            Player p;
            
            lock (Game.Players)
                Game.Players.TryGetValue(playerId, out p);

            if (p != null)
            {
                UpdateHudStatus(p);
                return true;
            }

            return false;
        }
                
        public virtual void ResetGame()
        {
            EndState(new GameResettingState(m_controller));
        }        

        protected void TransferAllRights(BuyPoint bp, Player p)
        {
            TransferDevelopmentRights(bp, p);
            TransferWaterRights(bp, p);
        }
        
        /// <summary>
        /// Transfer development rights to a given player.  
        /// </summary>
        /// This method does not invoke status updates for in-world views.
        /// <param name="bp"></param>
        /// <param name="p"></param>
        protected void TransferDevelopmentRights(BuyPoint bp, Player p)
        {
            // Hack: First we always clear the parcel of existing fields, even if the old and new specializations
            // are no different
            ChangeBuyPointSpecialization(bp, AbstractGameAssetType.None, 0);
            
            bp.DevelopmentRightsOwner = p;            

            AbstractGameAssetType allowedType = p.Role.AllowedAssets[0].Type;
            int fields = 0;

            // FIXME: Temporarily hardcoded
            switch (allowedType)
            {
                case AbstractGameAssetType.Houses:
                    fields = 20;
                    break;
                case AbstractGameAssetType.Crops:
                    fields = 6;
                    break;
                case AbstractGameAssetType.Factory:
                    fields = 2;
                    break;
            }
            
            // Morph plynth and adjust fields
            // FIXME: This should be done via event subscription
            ChangeBuyPointSpecialization(bp, allowedType, fields);
        }        
        
        /// <summary>
        /// Transfer water rights to a given player from a parcel. 
        /// </summary>
        /// <param name="bp"></param>
        /// <param name="p"></param>        
        protected void TransferWaterRights(BuyPoint bp, Player p)
        {
            bp.WaterRightsOwner = p;
            p.WaterEntitlement += bp.InitialWaterRights;    
        }
        
        protected void ChangeBuyPointSpecialization(BuyPoint bp, AbstractGameAssetType type, int numberOfFields)
        {
            IDictionary<UUID, Field> oldFields = bp.Fields;

            lock (oldFields)
            {
                List<Field> oldFieldsList = new List<Field>(oldFields.Values);
                
                foreach (Field f in oldFieldsList)
                {
                    // HACK: This is a nasty approach to maintaining referential integrity.  Nulling the buy point
                    // on the field also removes it from the buypoint list.
                    f.BuyPoint = null;
                }
            }   
            
            m_controller.Dispatcher.ChangeBuyPointSpecialization(bp, type, numberOfFields);
        }

        public virtual void UpdateBuyPointStatus(BuyPoint bp) {}
        public virtual void UpdateHudStatus(Player player) {}

        public virtual void StartGame() { throw new NotImplementedException(); }        
        public virtual void EndGame() { throw new NotImplementedException(); }
		public virtual void ChangeBuyPointName(BuyPoint bp, string newName) { throw new NotImplementedException(); }
        
        public virtual AbstractGameAsset BuildGameAsset(Field f, AbstractGameAsset templateAsset, int level)
            { throw new NotImplementedException(); }
        public virtual AbstractGameAsset ContinueBuildingGameAsset(AbstractGameAsset ga)
            { throw new NotImplementedException(); }
        public virtual void UpgradeGameAsset(Player p, AbstractGameAsset ga, int level) { throw new NotImplementedException(); }
        public virtual void SellGameAssetToEconomy(AbstractGameAsset ga) { throw new NotImplementedException(); }
        public virtual Field RemoveGameAsset(AbstractGameAsset ga) { throw new NotImplementedException(); }
        
        public virtual void GiveMoney(Player p, int amount) {}
        public virtual void GiveWater(Player p, int amount) {}
        public virtual void GiveWaterRights(Player p, int amount) {}
        
        public virtual void BuyLandRights(BuyPoint bp, Player p) { throw new NotImplementedException(); }
        public virtual void SellRights(BuyPoint bp, Player buyer, RightsType type, int salePrice) { throw new NotImplementedException(); }
        public virtual void SellWaterRights(Player buyer, Player seller, int amount, int salePrice) { throw new NotImplementedException(); }
        public virtual void UseWater(AbstractGameAsset a, Player p, int amount) { throw new NotImplementedException(); }
        public virtual void UndoUseWater(AbstractGameAsset a) { throw new NotImplementedException(); }
        public virtual void SellWater(Player seller, Player buyer, int waterAmount, int price) { throw new NotImplementedException(); }
        public virtual void EndTurn(UUID playerId) { throw new NotImplementedException(); }
        public virtual void EndStage() { throw new NotImplementedException(); }
        public virtual void AddPlayer(Player player) { throw new NotImplementedException(); }
        public virtual void RegisterBuyPoint(BuyPoint bp) { throw new NotImplementedException(); }
    }
}