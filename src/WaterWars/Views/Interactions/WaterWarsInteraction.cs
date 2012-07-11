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

using System.Collections.Generic;
using OpenMetaverse;
using WaterWars;
using WaterWars.Models;
using WaterWars.Views.Widgets;

namespace WaterWars.Views.Interactions
{
    /// <summary>
    /// Generic interaction with some specific WaterWars stuff
    /// </summary>
    public class WaterWarsInteraction : AbstractInteraction
    {
        public static string BUY_OPTION = "Buy";
        public static string UPGRADE_OPTION = "Upgrade";
        public static string SELL_OPTION = "Sell";
        public static string[] BUY_UPGRADE_SELL_OPTIONS  = new string[] { BUY_OPTION, UPGRADE_OPTION, SELL_OPTION };
        public static string[] BUY_SELL_OPTIONS  = new string[] { BUY_OPTION, SELL_OPTION };
        public static string[] BUY_OPTIONS  = new string[] { BUY_OPTION };
        
        protected WaterWarsController m_controller;

        public WaterWarsInteraction(WaterWarsController controller, HudView hud) 
            : base(hud.UserId, hud.m_statusButton)
        {
            m_controller = controller;          
        }

        public WaterWarsInteraction(WaterWarsController controller, HudView p1Hud, HudView p2Hud)
            : base(p1Hud.UserId, p1Hud.m_statusButton, p2Hud.UserId, p2Hud.m_statusButton)
        {
            m_controller = controller;          
        }                

        public WaterWarsInteraction(WaterWarsController controller, UUID playerId, OsButton button) 
            : base(playerId, button)
        {
            m_controller = controller;          
        }
        
        public WaterWarsInteraction(WaterWarsController controller, Dictionary<UUID, OsButton> buttonMap) 
            : base(buttonMap)
        {
            m_controller = controller;          
        }

        /// <summary>
        /// Send a dialog to the user
        /// </summary>
        /// <param name="player"></param>
        /// <param name="text"></param>
        /// <param name="options"></param>
        /// <param name="next">The process to execute once information has been received back from this dialog</param>
        protected void SendDialog(Player player, string text, List<string> options, NextProcessDelegate next)
        {
            SendDialog(player.Uuid, text, options, next);
        }        

        /// <summary>
        /// Send a dialog to the user
        /// </summary>
        /// <param name="player"></param>
        /// <param name="text"></param>
        /// <param name="options"></param>
        /// <param name="next">The process to execute once information has been received back from this dialog</param>
        protected void SendDialog(Player player, string text, string[] options, NextProcessDelegate next)
        {
            SendDialog(player.Uuid, text, options, next);
        }

        /// <summary>
        /// Send an alert to the user
        /// </summary>
        /// <param name="player"></param>
        /// <param name="text"></param>
        protected void SendAlert(Player player, string text)
        {
            SendAlert(player.Uuid, text);
        }
    }
}