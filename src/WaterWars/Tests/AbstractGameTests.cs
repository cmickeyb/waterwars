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
using Nini.Config;
using NUnit.Framework;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.CoreModules.World.Land;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Services.Interfaces;
using OpenSim.Tests.Common;
using WaterWars.Models;
using WaterWars.Models.Roles;
using WaterWars.Persistence;
using WaterWars.Rules;
using WaterWars.Rules.Allocators;
using WaterWars.Rules.Distributors;
using WaterWars.Rules.Forecasters;
using WaterWars.Rules.Generators;
using WaterWars.Rules.Economic.Distributors;
using WaterWars.Rules.Economic.Forecasters;
using WaterWars.Rules.Economic.Generators;
using WaterWars.States;
using WaterWars.Tests.Mock;

namespace WaterWars.Tests
{
    /// <summary>
    /// Common base class for tests which require the full game structure to be set up.
    /// </summary>
    public class AbstractGameTests
    {        
        protected WaterWarsController m_controller;
        protected MockDispatcher m_mockDispatcher;
        
        protected ILandObject lo1, lo2, lo3;
        protected BuyPoint bp1, bp2, bp3;
        
        protected Player p1, p2;

        protected Crops c1;
        protected Houses h1, h2, h3;
        protected Factory f1;

        [SetUp]
        public virtual void SetUp()
        {
            lo1 = new LandObject(UUID.Zero, false, null);
            lo2 = new LandObject(UUID.Zero, false, null);
            lo3 = new LandObject(UUID.Zero, false, null);

            bp1 = null; bp2 = null; bp3 = null;

            p1 = null; p2 = null;

            c1 = null;
            h1 = null; h2 = null; h3 = null;
            
            m_controller = CreateController();            
            m_mockDispatcher = new MockDispatcher(m_controller);         
            m_mockDispatcher.Configuration = @"
[General]
start_date = 11/2/1904
seconds_per_stage = 0
rounds_per_game = 10
water_delivery_series = 1

[Players]
manufacturer_start_money = 17000
developer_start_money = 16000
farmer_start_money = 15000

manufacturer_cost_of_living = 0
developer_cost_of_living = 0
farmer_cost_of_living = 0

[Parcels]
rights_price = 350
water_entitlement = 1000

hill_zone_rights_price = 500
hill_zone_water_entitlement = 300

river_zone_rights_price = 1000
river_zone_water_entitlement = 900

[Crops]
alfalfa_cost_per_build_step = 50
chillis_cost_per_build_step = 30
grapes_cost_per_build_step = 80

alfalfa_build_steps = 1;
chillis_build_steps = 1;
grapes_build_steps = 1;

alfalfa_revenue = 100
chillis_revenue = 100
grapes_revenue = 40

alfalfa_maintenance = 0
chillis_maintenance = 0
grapes_maintenance = 0

alfalfa_water = 200
chillis_water = 500
grapes_water = 100

[Condos]
condos_1_cost_per_build_step = 50
condos_2_cost_per_build_step = 90
condos_3_cost_per_build_step = 120

condos_1_build_steps = 2;
condos_2_build_steps = 2;
condos_3_build_steps = 2;

condos_1_revenue = 150
condos_2_revenue = 200
condos_3_revenue = 250

condos_1_revenue_series = 1
condos_2_revenue_series = 1
condos_3_revenue_series = 1

condos_1_maintenance = 5
condos_2_maintenance = 4
condos_3_maintenance = 3

condos_1_water = 10
condos_2_water = 18
condos_3_water = 24

[Factories]
factories_1_cost_per_build_step = 5000
factories_2_cost_per_build_step = 7000
factories_3_cost_per_build_step = 9000

factories_1_build_steps = 1
factories_2_build_steps = 1
factories_3_build_steps = 1

factories_1_revenue = 2000
factories_2_revenue = 3000
factories_3_revenue = 4000

factories_revenue_series = 1

factories_1_maintenance = 500
factories_2_maintenance = 700
factories_3_maintenance = 900

factories_1_water = 1000
factories_2_water = 800
factories_3_water = 600";                    
            
            m_controller.Dispatcher = m_mockDispatcher;
            m_controller.Initialise(false);            
        }
        
        /// <summary>
        /// Set up the controller to be used in the tests.  We allow an override here so that the persistence tests
        /// can set up a perister
        /// </summary>
        protected virtual WaterWarsController CreateController()
        {
            UserAccount economyUserAccount 
                = new UserAccount() 
                    { PrincipalID = UUID.Parse("99999999-9999-9999-9999-999999999999"), FirstName = "The", LastName = "Economy" };
            
            return 
                new WaterWarsController(
                    null,
                    null,
                    new SimpleForecaster(),
                    new SimpleUtopianRainfallGenerator(), 
                    new SimplePlayerOnlyWaterDistributor(), 
                    new SimpleWaterAllocator(),
                    new SimpleEconomicForecaster(),
                    new SeriesEconomicGenerator(),                              
                    new SimpleEconomicDistributor(),
                    economyUserAccount);            
        }        

        protected void AddBuyPoints()
        {                                   
            bp1 
                = new BuyPoint(UUID.Parse("00000000-0000-0000-0000-000000000011"))
                {
                    Name = "bp1",                    
                    Game = m_controller.Game
                };
            bp1.Location.Parcel = lo1;
            bp1.Location.LocalPosition = new Vector3(10, 30, 10);
            
            bp2 
                = new BuyPoint(UUID.Parse("00000000-0000-0000-0000-000000000012"))
                {
                    Name = "bp2",
                    Game = m_controller.Game
                };
            bp2.Location.Parcel = lo2;
            bp2.Location.LocalPosition = new Vector3(30, 30, 10);

            bp3
                = new BuyPoint(UUID.Parse("00000000-0000-0000-0000-000000000013"))
                {
                    Name = "bp3",
                    Game = m_controller.Game
                };            
            bp3.Location.Parcel = lo3;
            bp3.Location.LocalPosition = new Vector3(50, 30, 10);
            
            m_controller.State.RegisterBuyPoint(bp1);
            m_controller.State.RegisterBuyPoint(bp2);
            m_controller.State.RegisterBuyPoint(bp3);
        }

        protected void AddCrops() { AddCrops(bp1); }
        
        protected void AddCrops(BuyPoint bp)
        {            
            c1 = (Crops)AddGameAsset(bp, 0, Crops.Template, 1);            
        }        

        protected void AddHouses() { AddHouses(bp1); }
        
        protected void AddHouses(BuyPoint bp)
        {
            h1 = (Houses)AddGameAsset(bp, 0, Houses.Template, 1);
            h2 = (Houses)AddGameAsset(bp, 1, Houses.Template, 2);
            h3 = (Houses)AddGameAsset(bp, 2, Houses.Template, 3);                      
        }
    
        protected void AddFactories() { AddFactories(bp1); }
        
        protected void AddFactories(BuyPoint bp)
        {
            f1 = (Factory)AddGameAsset(bp, 0, Factory.Template, 2);
        }

        /// <summary>
        /// Add players where we don't care what their roles are
        /// </summary>
        protected void AddPlayers()
        {
            AddPlayers(Farmer.Singleton, Developer.Singleton);
        }

        protected AbstractGameAsset AddGameAsset(BuyPoint bp, int fieldNumber, AbstractGameAsset template, int level)
        {
            List<Field> fields = new List<Field>(bp.Fields.Values);
            return m_controller.State.BuildGameAsset(fields[fieldNumber], template, level);
        }

        /// <summary>
        /// Add players.
        /// </summary>
        /// <param name="role1"></param>
        /// <param name="role2"></param>
        protected void AddPlayers(params IRole[] roles)
        {
            if (roles.Length > 0)
            {
                p1 = m_controller.ModelFactory.CreatePlayer(UUID.Parse("00000000-0000-0000-0000-000000000001"), "Alfred", roles[0]);
                p1.Money = 15000;
                m_controller.State.AddPlayer(p1);
            }

            if (roles.Length > 1)
            {
                p2 = m_controller.ModelFactory.CreatePlayer(UUID.Parse("00000000-0000-0000-0000-000000000002"), "Betty", roles[1]);
                p2.Money = 15000;
                m_controller.State.AddPlayer(p2);                               
            }                      
        }

        protected void StartGame()
        {
            m_controller.State.StartGame();
        }

        /// <summary>
        /// End all player's turns
        /// </summary>
        protected void EndTurns()
        {
            m_controller.State.EndTurn(p1.Uuid);

            if (p2 != null)
                m_controller.State.EndTurn(p2.Uuid);
        }

        /// <summary>
        /// End all players turns multiple times
        /// </summary>
        protected void EndTurns(int phases)
        {
            for (int i = 0; i < phases; i++)
                EndTurns();
        }        
    }
}