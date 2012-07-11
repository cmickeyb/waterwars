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
    public class GameEndedState : AbstractState
    {    
        const string HUD_STATUS_MSG = "Year, turns and turns left\n{0} ({1}/{2})";
        
        public GameEndedState(WaterWarsController controller) : base(controller, GameStateType.Game_Ended) {}

        protected override void StartState()
        {
            base.StartState();
            
            // This is currently being done in the water phase so that we can amalgamate the messages.
            //m_controller.Events.PostToAll(GAME_ENDED_STATUS_MSG, EventLevel.Alert);
            
            UpdateAllStatus();
        }   
        
        public override void UpdateHudStatus(Player player)
        {
//            m_log.InfoFormat("[WATER WARS]: Updating hud status for {0}", player);
        
            // We need to update here so that if a player reconnects after the game is ended, they see the current
            // status information.
            m_controller.SendHudStatus(
                player, 
                string.Format(
                    HUD_STATUS_MSG, 
                    m_controller.Game.CurrentDate.ToString("yyyy"), 
                    m_controller.Game.CurrentRound, m_controller.Game.TotalRounds));
        }        
    }
}