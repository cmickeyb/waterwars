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
using Newtonsoft.Json;
using OpenMetaverse;
using OpenSim.Region.Framework.Scenes;
using WaterWars.Models.Roles;

namespace WaterWars.Models
{
    /// <summary>
    /// Represents a player in the game
    /// </summary>
    [Serializable]
    public class Player : AbstractGameModel
    {
        public static Player None = new Player("Nobody", UUID.Zero) { Money = -999, Role = NullRole.Singleton };      

        public override Dictionary<string, bool> OwnerActions 
        { 
            get
            {
                Dictionary<string, bool> actions = base.OwnerActions;
                
                if (Game.State == GameStateType.Build)
                {
                    actions["RequestWaterRights"] = true;                    
                    
                    if (WaterEntitlement > 0)
                        actions["SellWaterRights"] = true;                                        
                }                
                else if (Game.State == GameStateType.Water)
                {
                    actions["RequestWater"] = true;                    
                    
                    if (Water > 0)
                        actions["SellWater"] = true;
                }

                return actions;
            } 
        } 
        
        /// <value>
        /// Parcels where the player owns development rights.  Only ever changed by the BuyPoint.
        /// </value>
        [JsonIgnore]
        public virtual Dictionary<UUID, BuyPoint> DevelopmentRightsOwned { get; private set; }

        /// <value>
        /// Parcels where the player owns water rights.  Only ever changed by the BuyPoint.
        /// </value>
        [JsonIgnore]
        public virtual Dictionary<UUID, BuyPoint> WaterRightsOwned { get; private set; }
        
        /// <summary>
        /// Return the number of development rights owned by this player.  This exists purely so that we can propogate
        /// it via the webservice without infinite recursion
        /// </summary>
        public virtual int DevelopmentRightsOwnedCount { get { return DevelopmentRightsOwned.Count; } }

        /// <summary>
        /// Return the number of water rights owned by this player.  This exists purely so that we can propogate
        /// it via the webservice without infinite recursion        
        public virtual int WaterRightsOwnedCount { get { return WaterRightsOwned.Count; } }

        /// <value>
        /// Role of the player
        /// </value>
        public virtual IRole Role { get; set; }    
        
        /// <summary>
        /// The money the player had at the start of the turn.
        /// </summary>
        public virtual int StartTurnMoney { get; set; }
        
        /// <value>
        /// The money balance of this player
        /// </value>
        public virtual int Money 
        { 
            get { return m_money; }
            set 
            { 
                if (value > m_money)
                    MoneyReceivedThisTurn += value - m_money;
                else if (value < m_money)
                    MoneySpentThisTurn += m_money - value;
                
                m_money = value;
            }
        }
        protected int m_money;
        
        /// <summary>
        /// A cost charged to the player each turn for living expenditures.
        /// </summary>
        public virtual int CostOfLiving { get; set; }
        
        /// <summary>
        /// Money received this turn thus far
        /// </summary>
        public virtual int MoneyReceivedThisTurn { get; private set; }        
        
        /// <summary>
        /// Money spent this turn thus far
        /// </summary>
        public virtual int MoneySpentThisTurn { get; private set; }
        
        /// <summary>
        /// Money received from selling land this turn
        /// </summary>
        public virtual int LandRevenueThisTurn { get; set; }
        
        /// <summary>
        /// Costs incurred in acquiring land
        /// </summary>
        public virtual int LandCostsThisTurn { get; set; }
        
        /// <summary>
        /// Money received from selling water rights this turn
        /// </summary>
        public virtual int WaterRightsRevenueThisTurn { get; set; }
        
        /// <summary>
        /// Money spent acquiring water rights this turn
        /// </summary>
        public virtual int WaterRightsCostsThisTurn { get; set; }
        
        /// <summary>
        /// Money received from selling builds this turn
        /// </summary>
        public virtual int BuildRevenueThisTurn { get; set; }
        
        /// <summary>
        /// Money spent building assets this turn
        /// </summary>
        public virtual int BuildCostsThisTurn { get; set; }
                
        /// <summary>
        /// Money received from leasing water this turn
        /// </summary>
        public virtual int WaterRevenueThisTurn { get; set; }
        
        /// <summary>
        /// Money spent acquiring water this turn
        /// </summary>
        public virtual int WaterCostsThisTurn { get; set; }                               
        
        /// <summary>
        /// Projected maintenance costs from player owned assets
        /// </summary>
        public virtual int MaintenanceCosts
        {
            get
            {
                int costs = 0;
                
                List<BuyPoint> buyPoints;
                lock (DevelopmentRightsOwned)
                    buyPoints = DevelopmentRightsOwned.Values.ToList();
                
                List<AbstractGameAsset> gameAssets;
                
                foreach (BuyPoint bp in buyPoints)
                {
                    lock (bp.GameAssets)
                        gameAssets = bp.GameAssets.Values.ToList();
                    
                    foreach (AbstractGameAsset ga in gameAssets)
                        if (ga.Field.Owner == this)
                            costs += ga.MaintenanceCost;
                }
                
                return costs;
            }
        }
        
        /// <summary>
        /// Projected revenue from the products that the player is on course to make this turn.
        /// </summary>
        public virtual int ProjectedRevenueFromProducts
        {
            get
            {
                int revenue = 0;
                
                List<BuyPoint> buyPoints;
                lock (DevelopmentRightsOwned)
                    buyPoints = DevelopmentRightsOwned.Values.ToList();
                
                List<AbstractGameAsset> gameAssets;
                
                foreach (BuyPoint bp in buyPoints)
                {
                    lock (bp.GameAssets)
                        gameAssets = bp.GameAssets.Values.ToList();
                    
                    foreach (AbstractGameAsset ga in gameAssets)
                    {
                        if (ga.Field.Owner == this)
                            revenue += ga.ProjectedRevenue;
                    }
                }
                
                return revenue;
            }
        }

        /// <value>
        /// The water balance of this player
        /// </value>
        public virtual int Water { get; set; }                                   
                
        /// <summary>
        /// The total amount of water to which this player is entitled
        /// </summary>
        public virtual int WaterEntitlement { get; set; }      

        /// <value>
        /// The total water recieved by a player in a particular water phase.
        /// </value>
        public virtual int WaterReceived { get; set; }
        
        /// <summary>
        /// The amount of water that the player requires to operate their current assets
        /// </summary>
        public virtual int WaterRequired
        {
            get 
            {
                int waterRequired = 0;
                
                List<BuyPoint> buyPoints;
                lock (DevelopmentRightsOwned)
                    buyPoints = new List<BuyPoint>(DevelopmentRightsOwned.Values);
                
                foreach (BuyPoint bp in buyPoints)
                    waterRequired += bp.WaterRequired;
                
                return waterRequired;
            }
        }
        
        /// <summary>
        /// Projected player profit.  This will be negative in the event of a loss.
        /// </summary>
        /// <remarks>
        /// The composition varies by role.  For instance, we count build costs for a developer but not for an 
        /// industrialist (here they are considered capital costs).
        /// </remarks>
        public virtual int Profit
        {
            get
            {
                int profit = WaterRevenueThisTurn - MaintenanceCosts - WaterCostsThisTurn - CostOfLiving;
                
                if (Role.Type == RoleType.Developer)
                    profit += BuildRevenueThisTurn - BuildCostsThisTurn;
                else if (Role.Type == RoleType.Manufacturer)
                    profit += ProjectedRevenueFromProducts;
                else if (Role.Type == RoleType.Farmer)
                    profit += ProjectedRevenueFromProducts - BuildCostsThisTurn;
                
                return profit;
            }
        }
        
        /// <summary>
        /// History of profit and loss for this player
        /// </summary>
        public virtual List<Record> History { get; set; }

        /// <summary>
        /// For NHibernate
        /// </summary>
        protected Player() {}
        
        public Player(string name, UUID uuid) : base(uuid, AbstractGameAssetType.Player, name) 
        {            
            DevelopmentRightsOwned = new Dictionary<UUID, BuyPoint>();
            WaterRightsOwned = new Dictionary<UUID, BuyPoint>(); 
            History = new List<Record>();
            
            // To align index numbers with rounds we have a null first record
            History.Add(null);
        }

        /// <summary>
        /// Record round in player history
        /// </summary>
        public void RecordHistory()
        {
            Record record = new Record();
            record.LandRevenueThisTurn = LandRevenueThisTurn;
            record.LandCostsThisTurn = LandCostsThisTurn;
            record.WaterRightsRevenueThisTurn = WaterRightsRevenueThisTurn;
            record.WaterRightsCostsThisTurn = WaterRightsCostsThisTurn;
            record.BuildRevenueThisTurn = BuildRevenueThisTurn;
            record.WaterRevenueThisTurn = WaterRevenueThisTurn;
            record.BuildCostsThisTurn = BuildCostsThisTurn;
            record.WaterCostsThisTurn = WaterCostsThisTurn;
            record.CostOfLiving = CostOfLiving;
            record.ProjectedRevenueFromProducts = ProjectedRevenueFromProducts;
            record.MaintenanceCosts = MaintenanceCosts;
            record.Profit = Profit;
            History.Add(record);            
        }
        
        /// <summary>
        /// Reset the numbers which keep track of shifting status in a turn
        /// </summary>
        public void ResetForNextTurn()
        {
            StartTurnMoney = Money;
            MoneyReceivedThisTurn = 0;
            MoneySpentThisTurn = 0;
            BuildCostsThisTurn = 0;
            WaterCostsThisTurn = 0;
            LandRevenueThisTurn = 0;
            LandCostsThisTurn = 0;
            WaterRightsRevenueThisTurn = 0;
            WaterRightsCostsThisTurn = 0;
            BuildRevenueThisTurn = 0;
            WaterRevenueThisTurn = 0;
            WaterReceived = 0;
        }

        public override string ToString()
        {
            return Name;
        }
        
        /// <summary>
        /// Record of results for a particular round
        /// </summary>
        /// <remarks>
        /// Property names here must be identical to the corresponding property name for current year values.
        /// This is to make accessing these properties via Javascript easier.
        /// </remarks>
        public class Record
        {
            public int LandRevenueThisTurn { get; set; }
            public int LandCostsThisTurn { get; set; }
            public int WaterRightsRevenueThisTurn { get; set; }
            public int WaterRightsCostsThisTurn { get; set; }
            public int BuildRevenueThisTurn { get; set; }
            public int WaterRevenueThisTurn { get; set; }
            public int BuildCostsThisTurn { get; set; }
            public int WaterCostsThisTurn { get; set; }
            public int CostOfLiving { get; set; }
            public int ProjectedRevenueFromProducts { get; set; }
            public int MaintenanceCosts { get; set; }
            public int Profit { get; set; }
        }
    }
}