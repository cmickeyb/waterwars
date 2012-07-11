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
    public class AskWaterRightsBuyerInteraction : WaterWarsInteraction
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);       

        protected RightsType m_rightsToSell;
        protected int m_amount;
        protected int m_salePrice;
        protected Player m_buyer;
        protected Player m_seller;
        
        public AskWaterRightsBuyerInteraction(
            WaterWarsController controller, HudView sellerHud, HudView buyerHud, 
            int salePrice, int amount)
            : base(controller, sellerHud, buyerHud)
        {            
            if (!m_controller.Game.Players.TryGetValue(sellerHud.UserId, out m_seller)) return;
            if (!m_controller.Game.Players.TryGetValue(buyerHud.UserId, out m_buyer)) return;
            m_amount = amount;
            m_salePrice = salePrice;
            
            AskBuyer();
        }
        
        protected void AskBuyer()
        {                        
            SendDialog(
                m_buyer,
                string.Format(
                    "Would you like to buy the rights to {0} from {1} for {2}{3}?",
                    WaterWarsUtils.GetWaterUnitsText(m_amount), m_seller.Name, WaterWarsConstants.MONEY_UNIT, m_salePrice),                
                YES_NO_OPTIONS, ProcessBuyer); 
            
            SendAlert(
                m_seller, 
                string.Format(
                    "Offer to sell rights for {0} to {1} for {2}{3} has been sent", 
                    WaterWarsUtils.GetWaterUnitsText(m_amount), m_buyer.Name, WaterWarsConstants.MONEY_UNIT, m_salePrice));           
        }

        protected void ProcessBuyer(OSChatMessage chat)
        {
            m_log.InfoFormat(
                "[WATER WARS]: Processing reply {0} for {1} selling to {2}", chat.Message, m_seller, m_buyer);
            
            bool accepted = false;
            
            if (YES_OPTION == chat.Message)
            {
                if (m_buyer.Money >= m_salePrice)
                {
                    accepted = true;
                }
                else
                {
                    SendAlert(m_buyer, "You don't have enough money to accept that offer!");
                }
            }

            if (accepted)
            {
                m_controller.State.SellWaterRights(m_buyer, m_seller, m_amount, m_salePrice);
                    
                SendAlert(
                    m_seller, 
                    string.Format(
                        "You successfully sold rights to {0} to {1} for {2}{3}", 
                        WaterWarsUtils.GetWaterUnitsText(m_amount), m_buyer.Name, WaterWarsConstants.MONEY_UNIT, m_salePrice));

                SendAlert(
                    m_buyer,
                    string.Format(
                        "You successfully bought the rights to {0} from {1} for {2}{3}",
                        WaterWarsUtils.GetWaterUnitsText(m_amount), m_seller.Name, WaterWarsConstants.MONEY_UNIT, m_salePrice));
            }
            else
            {
                m_controller.EventManager.TriggerWaterRightsSold(m_buyer, m_seller, m_amount, m_salePrice, false);
                
                SendAlert(
                    m_seller, 
                    string.Format(
                        "{0} declined your offer to sell them rights to {1} for {2}{3}",
                        m_buyer.Name, WaterWarsUtils.GetWaterUnitsText(m_amount), WaterWarsConstants.MONEY_UNIT, m_salePrice));
            }
        }          
    }
}