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
using WaterWars.Models.Roles;
using WaterWars.Rules.Startup;

namespace WaterWars.States
{
    /// <summary>
    /// Game is starting
    /// </summary>
    public class GameStartingState : AbstractState
    {
//        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
                
        public GameStartingState(WaterWarsController controller) : base(controller, GameStateType.Game_Starting) {}

        protected override void StartState()
        {
            base.StartState();
            
            // Perform economic activity for the first time so that assets created in the first round have some 
            // market value
            Game.EconomicActivity = m_controller.EconomicGenerator.Generate(Game);                        
            
            m_controller.StartupRule.Execute(this);           
            
            // This must be called only after economic activity generation, since that relies on the current round
            // being zero.
            m_controller.RoundManager.Start();
        }
        
        protected override void PostStartState()
        {
            EndState(new BuildStageState(m_controller));
        }     
        
        /// <summary>
        /// Refactor with common parts of parcel purchase
        /// </summary>
        /// <param name="bp"></param>
        /// <param name="owner"></param>
        public void AllocateParcel(BuyPoint bp, Player owner)
        {            
            TransferAllRights(bp, owner);        
            
            owner.TriggerChanged();
            bp.TriggerChanged();                       
            
            // TODO: This should really be a transfer event - since they weren't 'bought'
            m_controller.EventManager.TriggerLandRightsGiven(bp);

            // FIXME: This should be done via events
            UpdateHudStatus(owner);

//            m_controller.Events.PostToAll(
//                string.Format(BUY_RIGHTS_CRAWL_MSG, buyer.Name, bp.Name, bp.RegionName), 
//                EventLevel.Crawl);               
        }
    }
}