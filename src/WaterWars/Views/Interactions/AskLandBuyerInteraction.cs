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
    public class AskLandBuyerInteraction : WaterWarsInteraction
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        protected BuyPoint m_bp;

        /// <summary>
        /// Player interacting
        /// </summary>
        protected Player m_p;

        protected RightsType m_rightsToSell;
        protected int m_salePrice;
        protected Player m_targetPlayer = Player.None;
        
        public AskLandBuyerInteraction(
            WaterWarsController controller, HudView playerHud, HudView targetPlayerHud, BuyPoint bp, 
            int salePrice, RightsType rightsToSell)
            : base(controller, playerHud, targetPlayerHud)
        {
            m_bp = bp;
            if (!m_controller.Game.Players.TryGetValue(playerHud.UserId, out m_p)) return;
            if (!m_controller.Game.Players.TryGetValue(targetPlayerHud.UserId, out m_targetPlayer)) return;
            m_salePrice = salePrice;
            m_rightsToSell = rightsToSell;
            
            AskBuyer();
        }
        
        protected void AskBuyer()
        {                        
            SendDialog(
                m_targetPlayer,
                string.Format(
                    "Would you like to buy the development rights for parcel {0} at {1} in {2} from {3} for {4}{5}?",
                    m_bp.Name, m_bp.Location.LocalPosition, m_bp.Location.RegionName, m_p.Name, WaterWarsConstants.MONEY_UNIT, m_salePrice),                
                YES_NO_OPTIONS, ProcessBuyer); 
            
            SendAlert(
                m_p, 
                string.Format(
                    "Offer to sell {0} rights of parcel {1} to {2} for {3}{4} has been sent", 
                    m_rightsToSell, m_bp.Name, m_targetPlayer.Name, WaterWarsConstants.MONEY_UNIT, m_salePrice));           
        }

        protected void ProcessBuyer(OSChatMessage chat)
        {
            m_log.InfoFormat(
                "[WATER WARS]: Processing reply {0} for {1} selling to {2}", chat.Message, m_p, m_targetPlayer);
            
            bool accepted = false;
            
            if (YES_OPTION == chat.Message)
            {
                if (m_targetPlayer.Money >= m_salePrice)
                {
                    accepted = true;
                }
                else
                {
                    SendAlert(m_targetPlayer, "You don't have enough money to accept that offer!");
                }
            }

            if (accepted)
            {
                m_controller.State.SellRights(m_bp, m_targetPlayer, m_rightsToSell, m_salePrice);
                    
                SendAlert(
                    m_p, 
                    string.Format(
                        "You successfully sold the {0} rights of parcel {1} to {2} for {3}{4}", 
                        m_rightsToSell, m_bp.Name, m_targetPlayer.Name, WaterWarsConstants.MONEY_UNIT, m_salePrice));

                SendAlert(
                    m_targetPlayer,
                    string.Format(
                        "You successfully bought the {0} rights of parcel {1} from {2} for {3}{4}",
                        m_rightsToSell, m_bp.Name, m_p.Name, WaterWarsConstants.MONEY_UNIT, m_salePrice));
            }
            else
            {
                m_controller.EventManager.TriggerLandRightsSold(
                    m_bp, m_targetPlayer, m_p, m_rightsToSell, m_salePrice, false);
                
                SendAlert(
                    m_p, 
                    string.Format(
                        "{0} declined your offer to sell them the {1} rights of parcel {2} for {3}{4}",
                        m_targetPlayer.Name, m_rightsToSell, m_bp.Name, WaterWarsConstants.MONEY_UNIT, m_salePrice));
            }
        }          
    }
}