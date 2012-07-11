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
using WaterWars.Events;
using WaterWars.Models;

namespace WaterWars.States
{
    /// <summary>
    /// Game has ended
    /// </summary>
    public class GameResettingState : AbstractState
    {        
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        public const string GAME_RESETTING_STATUS_MSG = "Game resetting, please wait.";
        
        public GameResettingState(WaterWarsController controller) : base(controller, GameStateType.Game_Resetting) {}

        protected override void PostStartState()
        {         
            m_log.InfoFormat("[WATER WARS]: Resetting game");
            
            m_controller.Events.PostToAll(GAME_RESETTING_STATUS_MSG, EventLevel.Alert);
            
            UpdateAllStatus();
            
            ClearBoard(); 
            
            // FIXME: This should really be done via an event
            m_controller.Game.CurrentDate = default(DateTime);          
            
            m_controller.RoundManager.Reset();
            
            m_log.InfoFormat("[WATER WARS]: Game reset");
            
            EndState(new RegistrationState(m_controller));
        }

        public override void ResetGame()
        {
            // Ignore reset commands while the game is being reset.
        }  
        
        /// <summary>
        /// Clear everything from the game and start again.
        /// </summary>
        /// Players get reset later on at the start of registration so that we can continue to send them status messages
        /// and update their hud
        protected void ClearBoard()
        {
            DateTime t1 = DateTime.Now;              
            
            // Reset all registered game objects
            foreach (BuyPoint bp in Game.BuyPoints.Values)
            {
                m_log.InfoFormat(
                    "[WATER WARS]: Resetting buypoint {0} in {1} at {2}", 
                    bp.Name, bp.Location.RegionName, bp.Location.LocalPosition);
                
                Dictionary<UUID, AbstractGameAsset> assets = bp.GameAssets;

                lock (assets)
                {
                    // Remove all game pieces
                    foreach (AbstractGameAsset a in assets.Values)
                        m_controller.Dispatcher.RemoveGameAssetView(a);
                }
                
                bp.Reset();
                ChangeBuyPointSpecialization(bp, AbstractGameAssetType.None, 0);
                UpdateBuyPointStatus(bp);
            }
            
            List<Player> players;
            lock (Game.Players)
                players = Game.Players.Values.ToList();
            
            foreach (Player p in players)
            {
                // FIXME: Should be done via events
                m_controller.ResetHud(p.Uuid);
                m_controller.ViewerWebServices.PlayerServices.ClearLastSelected(p.Uuid);
            }                        

//            m_controller.Dispatcher.EnableRegistration(true);
//            m_controller.Dispatcher.EnableStartGame(true);
            
            m_log.InfoFormat("[WATER WARS]: ClearBoard() took {0}ms", (DateTime.Now - t1).TotalMilliseconds);
        }      
    }
}