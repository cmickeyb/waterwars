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
using Nini.Config;
using OpenMetaverse;
using WaterWars;
using WaterWars.Models.Roles;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Services.Interfaces;

namespace WaterWars.Models
{
    /// <summary>
    /// A factory for creating models
    /// </summary>    
    public class ModelFactory
    {
        protected WaterWarsController m_controller;
        
        public ModelFactory(WaterWarsController controller)
        {
            m_controller = controller;
        }
        
        public Game CreateGame(UUID uuid, string name)
        {
            Game game = new Game(UUID.Random(), "Game1", GameStateType.None);
            
            // This must be done after the constructor is called, since creating the player will require a valid
            // game object and we can't use the one on the controller since it hasn't been returned yet.
            UserAccount economyAccount = m_controller.EconomyUserAccount;
            game.Economy = CreatePlayer(economyAccount.PrincipalID, economyAccount.Name, Economy.Singleton, game);
            game.Roles.Add(Developer.Singleton);
            game.Roles.Add(Economy.Singleton);
            game.Roles.Add(Farmer.Singleton);
            game.Roles.Add(Manufacturer.Singleton);
            game.Roles.Add(WaterMaster.Singleton);  
            
            return game;
        }
        
        protected Player CreatePlayer(UUID uuid, string name, IRole role, Game game)
        {
//            System.Console.WriteLine("Player {0} created with game [{1}]", name, game.Name);
            return new Player(name, uuid) { Game = game, Role = role };
        }        
        
        public Player CreatePlayer(UUID uuid, string name, IRole role)
        {
            return CreatePlayer(uuid, name, role, m_controller.Game);
        }

        public BuyPoint CreateBuyPoint(UUID uuid, string name, Vector3 pos, ILandObject osParcel, RegionInfo regionInfo)
        {
            BuyPoint bp = new BuyPoint(uuid, name, pos, osParcel) { Game = m_controller.Game };
            bp.Location.RegionName = regionInfo.RegionName;
            bp.Location.RegionId = regionInfo.RegionID;
            bp.Location.RegionX = regionInfo.RegionLocX;
            bp.Location.RegionY = regionInfo.RegionLocY;
            
            return bp;            
        }

        public Field CreateField(BuyPoint bp, UUID uuid, string name)
        {
            return new Field(uuid, name) { BuyPoint = bp, Owner = bp.DevelopmentRightsOwner, Game = m_controller.Game };
        }

        public AbstractGameAsset CreateGameAsset(Field f, AbstractGameAsset template, Vector3 pos, int level)
        {
            UUID uuid = UUID.Random();
            
            AbstractGameAsset asset = null;
            string name = string.Format("{0} ({1})", template.InitialNames[level], f.Name);
            
            if (template is Factory)
                asset = new Factory(name, uuid, pos, level);   
            else if (template is Houses)
                asset = new Houses(name, uuid, pos, level);
            else if (template is Crops)
                asset = new Crops(name, uuid, pos, level);
            else
                throw new Exception(string.Format("Unrecognized asset type {0}", template));

            asset.InitialNames = template.InitialNames;
            asset.ConstructionCostsPerBuildStep = template.ConstructionCostsPerBuildStep;
            asset.StepsToBuilds = template.StepsToBuilds;
            asset.NormalRevenues = template.NormalRevenues;
            asset.WaterUsages = template.WaterUsages;
            asset.MaintenanceCosts = template.MaintenanceCosts;            
            asset.InitialTimesToLive = template.InitialTimesToLive;
            asset.TimeToLive = asset.InitialTimesToLive[level];
            asset.Field = f;           
            asset.Game = m_controller.Game;         
            
            int revenue = m_controller.EconomicDistributor.Allocate(asset.Game.EconomicActivity, asset);
            if (template is Houses)                
                asset.MarketPrice = revenue;
            else
                asset.RevenueThisTurn = revenue;
            
            return asset;
        }
    }
}