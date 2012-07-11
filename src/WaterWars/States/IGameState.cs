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
using OpenMetaverse;
using OpenSim.Region.Framework.Interfaces;
using WaterWars.Models;
using WaterWars.States;

namespace WaterWars.States
{  
    public interface IGameState
    {
        /// <value>
        /// The type of the state
        /// </value>
        GameStateType Type { get; }
        
        /// <summary>
        /// Add a player.
        /// </summary>
        /// <param name="player"></param>
        void AddPlayer(Player player);
        
        /// <summary>
        /// Give money to a player.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="amount"></param>
        void GiveMoney(Player player, int amount);
        
        /// <summary>
        /// Give water rights to a player.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="amount"></param>
        void GiveWaterRights(Player player, int amount);
        
        /// <summary>
        /// Give water to a player.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="amount"></param>
        void GiveWater(Player player, int amount);        
		
		/// <summary>
		/// Change a buy point's name
		/// </summary>
		/// <param name="bp"></param>
		/// <param name="newName"></param>
		void ChangeBuyPointName(BuyPoint bp, string newName);

        /// <summary>
        /// Starting building a game asset
        /// </summary>
        /// <param name="f"></param>
        /// <param name="templateAsset"></param>
        /// <param name="level"></param>
        /// <returns>The asset built or on which building has started.</returns>        
        AbstractGameAsset BuildGameAsset(Field f, AbstractGameAsset templateAsset, int level);        
        
        /// <summary>
        /// Continue building an existing but incomplete game asset
        /// </summary>
        /// <param name="bp"></param>
        /// <param name="ga"></param>
        /// <returns>Game asset being built</returns>
        AbstractGameAsset ContinueBuildingGameAsset(AbstractGameAsset ga);

        /// <summary>
        /// Upgrade a game asset
        /// </summary>
        /// <param name="p"></param>
        /// <param name="ga"></param>
        /// <param name="level"></param>
        void UpgradeGameAsset(Player p, AbstractGameAsset ga, int level);

        /// <summary>
        /// Sell the given game asset to the economy.
        /// </summary>
        /// <param name="ga"></param>
        void SellGameAssetToEconomy(AbstractGameAsset ga);

        /// <summary>
        /// Remove a particular game asset
        /// </summary>
        /// <param name="ga"></param>
        /// <returns>Field that replaces the game asset</returns>
        Field RemoveGameAsset(AbstractGameAsset ga);
        
        /// <summary>
        /// Buy water and development rights for a particular parcel
        /// </summary>
        /// <exception cref="WaterWarsGameLogicException">Thrown if the parcel could not be bought</exception>
        /// <param name="bp"></param>
        /// <param name="p"></param>
        void BuyLandRights(BuyPoint bp, Player p);                

        /// <summary>
        /// Sell rights for a particular parcel
        /// </summary>
        /// <param name="bp"></param>
        /// <param name="buyer"></param>
        /// <param name="type"></param>
        /// <param name="salePrice"></param>
        void SellRights(BuyPoint bp, Player buyer, RightsType type, int salePrice);
        
        /// <summary>
        /// Sell the pooled water rights of a given player
        /// </summary>
        /// <param name="buyer"></param>
        /// <param name="seller"></param>
        /// <param name="amount"></param>
        /// <param name="salePrice"></param>
        void SellWaterRights(Player buyer, Player seller, int amount, int salePrice);
        
        /// <summary>
        /// Use available water on a particular asset
        /// </summary>
        /// <param name="a"></param>
        /// <param name="p"></param>
        /// <param name="amount">The amount of water to apply.</param>
        void UseWater(AbstractGameAsset a, Player p, int amount);

        /// <summary>
        /// Undo water use on a particular asset
        /// </summary>
        /// <param name="a"></param>
        void UndoUseWater(AbstractGameAsset a);

        /// <summary>
        /// Sell water from one player to another
        /// </summary>
        /// This method will throw an Exception if the seller doesn't have enough water or the buyer doesn't have enough
        /// money - these factors should be checked before calling this method.
        /// <param name="seller"></param>
        /// <param name="buyer"></param>
        /// <param name="waterAmount"></param>
        /// <param name="price"></param>
        void SellWater(Player seller, Player buyer, int waterAmount, int price);
        
        /// <summary>
        /// End a player's turn
        /// </summary>
        /// <param name="playerId"></param>
        /// <returns>Post method game state</returns>
        void EndTurn(UUID playerId);

        /// <summary>
        /// End the stage
        /// </summary>
        void EndStage();
        
        /// <summary>
        /// End the game early.
        /// </summary>
        void EndGame();

        /// <summary>
        /// Register a buy point
        /// </summary>
        /// <param name="bp"></param>
        void RegisterBuyPoint(BuyPoint bp);

        /// <summary>
        /// Start the game
        /// </summary>
        /// <exception cref="ConfigurationException">Thrown if there is a configuration problem</exception>
        void StartGame();
        
        /// <summary>
        /// Reset the game
        /// </summary>
        void ResetGame();

        /// <summary>
        /// Make a status update for a given buy point.  Used externally by buy point views when they first register.
        /// </summary>
        void UpdateBuyPointStatus(BuyPoint bp);
        
        /// <summary>
        /// Make a hud status update for a given player.  Used externally by huds when they first register
        /// </summary>
        /// <param name="playerId"></param>
        bool UpdateHudStatus(UUID playerId);
    }
}