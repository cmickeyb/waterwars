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
using WaterWars.Models;

namespace WaterWars.States
{    
    /// <summary>
    /// An abstract class for stages when the game is in play
    /// </summary>
    public abstract class AbstractPlayState : AbstractState
    {       
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        const string HUD_STATUS_MSG = "Year, turns and turns left\n{0} ({1}/{2})";

        /// <summary>
        /// Players who have ended their turn
        /// </summary>
        protected Dictionary<UUID, bool> PlayersInEndTurn = new Dictionary<UUID, bool>();
        
        public AbstractPlayState(WaterWarsController controller, GameStateType type) : base(controller, type) {}

        protected override void StartState()
        {
            base.StartState();

            m_controller.StageTimer.Start();
                    
            List<Player> players;
            lock (Game.Players)
                players = Game.Players.Values.ToList();
            
            foreach (Player player in players)
                m_controller.SendGo(player.Uuid);
        }        

        public override void ResetGame()
        {
            m_controller.StageTimer.Stop();
            base.ResetGame();
        }
        
        public override void EndTurn(UUID playerId)
        {                           
            lock (Game.Players)
            {
                if (!Game.Players.ContainsKey(playerId))
                    return;
                
                lock (PlayersInEndTurn)
                {
                    PlayersInEndTurn[playerId] = true;
                    Player p = Game.Players[playerId];
                    
//                    m_log.InfoFormat("[WATER WARS]: Player {0} ended their turn", p.Name);
                    
                    m_controller.EventManager.TriggerPlayerEndedTurn(p);
        
                    if (PlayersInEndTurn.Count == Game.Players.Count)
                        EndStage();
                }
            }
        }

        /// <summary>
        /// End the phase.  This is called when the phase ends normally (e.g. it isn't the end of the game, the
        /// game hasn't been reset, etc.)
        /// </summary>
        public override void EndStage()
        {
            m_controller.StageTimer.Stop();            
        }
        
        public override void EndGame() 
        {
            Game.TotalRounds = Game.CurrentRound;
            UpdateAllStatus();
        }
        
        public override void GiveMoney(Player p, int amount) 
        {
            m_log.InfoFormat("[WATER WARS]: Giving {0} to {1}", WaterWarsUtils.GetMoneyUnitsText(amount), p.Name);
            
            p.Money += amount;
            
            m_controller.EventManager.TriggerMoneyGiven(p, amount);
            
            p.TriggerChanged();
            
            // FIXME: Should be done via event subscription.
            UpdateHudStatus(p);
        }
        
        public override void GiveWater(Player p, int amount) 
        {
            m_log.InfoFormat("[WATER WARS]: Giving {0} to {1}", WaterWarsUtils.GetWaterUnitsText(amount), p.Name);
            
            p.Water += amount;
            
            m_controller.EventManager.TriggerWaterGiven(p, amount);
            
            p.TriggerChanged();
            
            // FIXME: Should be done via event subscription.
            UpdateHudStatus(p);            
        }         
        
        public override void GiveWaterRights(Player p, int amount) 
        {
            m_log.InfoFormat("[WATER WARS]: Giving rights to {0} to {1}", WaterWarsUtils.GetWaterUnitsText(amount), p.Name);
            
            p.WaterEntitlement += amount;
            
            m_controller.EventManager.TriggerWaterRightsGiven(p, amount);
            
            p.TriggerChanged();
            
            // FIXME: Should be done via event subscription.
            UpdateHudStatus(p);            
        }        

        public override void UpdateHudStatus(Player player)
        {
//            m_log.InfoFormat("[WATER WARS]: Updating hud status for {0}", player);
            
            m_controller.SendHudStatus(
                player, 
                string.Format(
                    HUD_STATUS_MSG, 
                    m_controller.Game.CurrentDate.ToString("yyyy"), 
                    m_controller.Game.CurrentRound, m_controller.Game.TotalRounds));                    
            
            lock (PlayersInEndTurn)
            {
                if (PlayersInEndTurn.ContainsKey(player.Uuid))
                    m_controller.SendStop(player.Uuid);
                else
                    m_controller.SendGo(player.Uuid);
            }
        }
        
        /// <summary>
        /// Has the player in question ended their turn?
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public bool HasPlayerEndedTurn(Player player)
        {
            lock (PlayersInEndTurn)
                return (PlayersInEndTurn.ContainsKey(player.Uuid));
        }
		
		public override void ChangeBuyPointName(BuyPoint bp, string newName)
		{
			bp.Name = newName;
            bp.TriggerChanged();
		}
    }
}