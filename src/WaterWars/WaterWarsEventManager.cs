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
using WaterWars.Models;

namespace WaterWars
{
    public class WaterWarsEventManager
    {
        public delegate void RightsSoldDelegate(BuyPoint bp, Player buyer, Player seller, RightsType type, int price, bool succeeded);        
        public delegate void WaterSoldDelegate(Player buyer, Player seller, int amount, int price, bool succeeded);                        
        
        #region Initialization events
        /// <summary>
        /// Fired when the water wars system is initialized
        /// </summary>
        public event Action OnSystemInitialized;        
        public event Action<BuyPoint> OnBuyPointRegistered;
        #endregion
        
        #region UI events
        public event Action<Player, AbstractGameModel> OnGameModelSelected;
        #endregion
        
        /// <summary>
        /// Triggered when a player is added to the game.
        /// </summary>
        /// <remarks>
        /// This can occur multiple times if a player changes their role before the game starts
        /// </remarks>
        public event Action<Player> OnPlayerAdded;        
        public event Action<List<Player>> OnPlayersRemoved;        
        
        public event Action<GameStateType> OnStateStarted;        
        public event Action<GameStateType> OnStateEnded;
        public event Action<Player> OnPlayerEndedTurn;
        
        // TODO: We want to put the state on the revenue stage itself and convert this to a generic action
        public event Action<List<AbstractGameAsset>> OnRevenueStageEnded;        
        
        #region General play events
        public event Action<Player, int> OnMoneyGiven;
        public event Action<Player, int> OnWaterGiven;
        public event Action<Player, int> OnWaterRightsGiven;
        #endregion
        
        public event Action<BuyPoint> OnBuyPointNameChanged;
        public event Action<BuyPoint, Player> OnLandRightsBought;
        public event Action<BuyPoint> OnLandRightsGiven;
        public event RightsSoldDelegate OnLandRightsSold;
        public event WaterSoldDelegate OnWaterRightsSold;
        public event Action<AbstractGameAsset> OnGameAssetBuildStarted;
        public event Action<AbstractGameAsset> OnGameAssetBuildContinued;
        public event Action<AbstractGameAsset> OnGameAssetBuildCompleted;
        public event Action<AbstractGameAsset, Player, int> OnGameAssetSoldToEconomy;
        public event Action<AbstractGameAsset> OnGameAssetRemoved;
        public event Action<AbstractGameAsset, int> OnGameAssetUpgraded;

        public event Action<int> OnWaterGenerated;
        public event Action<Player, int> OnWaterAllocated;
        
        public event Action<AbstractGameAsset, Player, int> OnWaterUsed;
        public event WaterSoldDelegate OnWaterSold;
          
        public event Action<Player, int, int, int> OnRevenueReceived;

        #region System triggers
        public void TriggerSystemInitialized()
        {
            if (OnSystemInitialized != null)
                OnSystemInitialized();
        }
        
        public void TriggerBuyPointRegistered(BuyPoint bp)
        {
            if (OnBuyPointRegistered != null)
                OnBuyPointRegistered(bp);
        }        
        #endregion System triggers
        
        #region UI triggers
        public void TriggerGameModelSelected(Player p, AbstractGameModel gm)
        {
            if (OnGameModelSelected != null)
                OnGameModelSelected(p, gm);
        }
        #endregion
                
        public void TriggerStateStarted(GameStateType newState)
        {
            if (OnStateStarted != null)
                OnStateStarted(newState);
        }
        
        public void TriggerStateEnded(GameStateType newState)
        {
            if (OnStateEnded != null)
                OnStateEnded(newState);
        }      
        
        public void TriggerPlayerEndedTurn(Player p)
        {
            if (OnPlayerEndedTurn != null)
                OnPlayerEndedTurn(p);
        }

        public void TriggerRevenueStageEnded(List<AbstractGameAsset> assetsRemoved)
        {
            if (OnRevenueStageEnded != null)
                OnRevenueStageEnded(assetsRemoved);
        }
        
        public void TriggerMoneyGiven(Player p, int amount)
        {
            if (OnMoneyGiven != null)
                OnMoneyGiven(p, amount);
        }
        
        public void TriggerWaterGiven(Player p, int amount)
        {
            if (OnWaterGiven != null)
                OnWaterGiven(p, amount);
        }      
        
        public void TriggerWaterRightsGiven(Player p, int amount)
        {
            if (OnWaterRightsGiven != null)
                OnWaterRightsGiven(p, amount);
        }           
     
        public void TriggerOnBuyPointNameChanged(BuyPoint bp)
        {
            if (OnBuyPointNameChanged != null)
                OnBuyPointNameChanged(bp);
        }
        
        public void TriggerLandRightsBought(BuyPoint bp, Player p)
        {            
            if (OnLandRightsBought != null)
                OnLandRightsBought(bp, p);
        }
        
        public void TriggerLandRightsSold(BuyPoint bp, Player buyer, Player seller, RightsType type, int salePrice, bool success)
        {
            if (OnLandRightsSold != null)
                OnLandRightsSold(bp, buyer, seller, type, salePrice, success);            
        }
        
        public void TriggerLandRightsGiven(BuyPoint bp)
        {
            if (OnLandRightsGiven != null)
                OnLandRightsGiven(bp);
        }
        
        public void TriggerWaterRightsSold(Player buyer, Player seller, int amount, int price, bool success)
        {
            if (OnWaterRightsSold != null)
                OnWaterRightsSold(buyer, seller, amount, price, success);
        }
        
        public void TriggerGameAssetBuildStarted(AbstractGameAsset ga)
        {
            if (OnGameAssetBuildStarted != null)
                OnGameAssetBuildStarted(ga);
        }
        
        public void TriggerGameAssetBuildContinued(AbstractGameAsset ga)
        {
            if (OnGameAssetBuildContinued != null)
                OnGameAssetBuildContinued(ga);
        }
        
        public void TriggerGameAssetBuildCompleted(AbstractGameAsset ga)
        {
            if (OnGameAssetBuildCompleted != null)
                OnGameAssetBuildCompleted(ga);
        }        
        
        public void TriggerGameAssetSoldToEconomy(AbstractGameAsset ga, Player p, int price)
        {            
            if (OnGameAssetSoldToEconomy != null)
                OnGameAssetSoldToEconomy(ga, p, price);
        }
        
        public void TriggerGameAssetRemoved(AbstractGameAsset ga)
        {
            if (OnGameAssetRemoved != null)
                OnGameAssetRemoved(ga);
        }
        
        public void TriggerGameAssetUpgraded(AbstractGameAsset ga, int oldLevel)
        {
            if (OnGameAssetUpgraded != null)
                OnGameAssetUpgraded(ga, oldLevel);            
        }
        
        #region Allocation state events
        public void TriggerWaterGenerated(int water)
        {
            if (OnWaterGenerated != null)
                OnWaterGenerated(water);
        }
        
        public void TriggerWaterAllocated(Player p, int water)
        {
            if (OnWaterAllocated != null)
                OnWaterAllocated(p, water);
        }
        #endregion
        
        #region Water state events
        public void TriggerWaterUsed(AbstractGameAsset ga, Player user, int amount)
        {
            if (OnWaterUsed != null)
                OnWaterUsed(ga, user, amount);
        }        
        
        public void TriggerWaterSold(Player buyer, Player seller, int amount, int price, bool succeeded)
        {
            if (OnWaterSold != null)
                OnWaterSold(buyer, seller, amount, price, succeeded);
        }
        #endregion
        
        #region Revenue state events
        public void TriggerRevenueReceived(Player p, int operatingRevenue, int operatingCosts, int costOfLiving)
        {
            if (OnRevenueReceived != null)
                OnRevenueReceived(p, operatingRevenue, operatingCosts, costOfLiving);
        }
        
        #endregion
                
        public void TriggerPlayerAdded(Player p)
        {
            if (OnPlayerAdded != null)
                OnPlayerAdded(p);               
        }
        
        public void TriggerPlayersRemoved(List<Player> players)
        {
            if (OnPlayersRemoved != null)
                OnPlayersRemoved(players);               
        }        
    }
}