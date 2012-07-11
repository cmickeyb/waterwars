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
using System.Reflection;
using log4net;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Framework.Scenes.Serialization;
using WaterWars;
using WaterWars.Models;
using WaterWars.Models.Roles;
using WaterWars.States;
using WaterWars.Views.Widgets;

namespace WaterWars.Views.Interactions
{    
    public class AskWaterBuyerInteraction : WaterWarsInteraction
    {
//        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <value>
        /// This player
        /// </value>
        protected Player m_player;
        
        /// <value>
        /// Holds the target player while a water conversation is going on.
        /// </value>
        protected Player m_targetPlayer;

        /// <value>
        /// Hold water sell amount while a conversation is going on
        /// </value>
        protected int m_waterToSell;

        /// <value>
        /// Hold water sale price while a conversation is going on 
        /// </value>
        protected int m_salePrice;
        
        public AskWaterBuyerInteraction(
            WaterWarsController controller, HudView playerHud, HudView targetPlayerHud, 
            int amount, int salePrice)
            : base(controller, playerHud, targetPlayerHud)
        {
            if (!m_controller.Game.Players.TryGetValue(playerHud.UserId, out m_player)) return;
            if (!m_controller.Game.Players.TryGetValue(targetPlayerHud.UserId, out m_targetPlayer)) return;
            m_salePrice = salePrice;
            m_waterToSell = amount;
            
            AskBuyer();
        }
        
        protected void AskBuyer()
        {
            SendDialog(
                m_targetPlayer,
                string.Format(
                    "Would you like to lease {0} units of water for {1}{2} from {3}?", 
                    m_waterToSell, WaterWarsConstants.MONEY_UNIT, m_salePrice, m_player.Name),
                YES_NO_OPTIONS, ProcessCustomerReply);      

            SendAlert(
                m_player, 
                string.Format(
                    "Offer to lease {0} units of water for {1}{2} has been sent to {3}", 
                    m_waterToSell, WaterWarsConstants.MONEY_UNIT, m_salePrice, m_targetPlayer.Name));            
        }

        protected void ProcessCustomerReply(OSChatMessage chat)
        {
            bool accepted = false;
            
            if (YES_OPTION == chat.Message)
            {
                if (m_targetPlayer.Money >= m_salePrice)
                    accepted = true;
                else
                    SendAlert(m_targetPlayer.Uuid, "You don't have enough money to accept that offer!");
            }

            if (accepted)
            {
                m_controller.State.SellWater(m_player, m_targetPlayer, m_waterToSell, m_salePrice);

                SendAlert(
                    m_player, 
                    string.Format(
                        "You successfully leased {0} units of water to {1} for {2}{3}", 
                        m_waterToSell, m_targetPlayer.Name, WaterWarsConstants.MONEY_UNIT, m_salePrice));

                SendAlert(
                    m_targetPlayer,
                    string.Format(
                        "You successfully leased {0} units of water from {1} for {2}{3}",
                        m_waterToSell, m_player.Name, WaterWarsConstants.MONEY_UNIT, m_salePrice));
            }
            else
            {
                m_controller.EventManager.TriggerWaterSold(
                    m_targetPlayer, m_player, m_waterToSell, m_salePrice, false);          
                
                SendAlert(
                    m_player, 
                    string.Format(
                        "{0} declined your offer to lease them {1} units of water for {2}{3}", 
                        m_targetPlayer.Name, m_waterToSell, WaterWarsConstants.MONEY_UNIT, m_salePrice));
            }
        }            
    }
}