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
using Newtonsoft.Json;
using OpenMetaverse;

namespace WaterWars.Models
{
    /// <summary>
    /// Represents an asset in the game
    /// </summary>
    [Serializable]
    public class AbstractGameAsset : AbstractGameModel
    {
        public static AbstractGameAsset None 
            = new AbstractGameAsset("NONE", UUID.Zero, AbstractGameAssetType.None, Vector3.Zero, 0, 0, 0)
        {
            ConstructionCostsPerBuildStep = new int[] { 0 },
            StepsToBuilds = new int[] { 0 },
            NormalRevenues = new int[] { 0 },
            WaterUsages = new int[] { 0 },
            MaintenanceCosts = new int[] { 0 }
        };
        
        public const int INFINITE_TIME_TO_LIVE = -999;      

        /// <value>
        /// Reference back to the buypoint.  This is only here for JSON purposes.
        /// </value>
        public virtual UUID BuyPointUuid 
        { 
            get { return Field != null ? Field.BuyPointUuid : UUID.Zero; } 
        }

        /// <value>
        /// Reference to the owner.  Only here for JSON purposes.
        /// </value>
        public virtual UUID OwnerUuid 
        {   
            get { return Field != null ? Field.Owner.Uuid : UUID.Zero; } 
        }        
        
        /// <summary>
        /// Owner's name.  Only here for JSON purposes.
        /// </summary>
        public virtual string OwnerName
        {
            get { return Field != null ? Field.Owner.Name : "UNKNOWN"; }
        }

        public override Dictionary<string, bool> OwnerActions 
        { 
            get
            {
                Dictionary<string, bool> actions = base.OwnerActions;
                
                if (Game.State == GameStateType.Build)
                {
                    actions["Remove"] = true;

                    if (CanUpgrade && IsBuilt && Level < MaxLevel && Field.Owner.Money >= UpgradeCosts[Level + 1])
                        actions["Upgrade"] = true;
                    if (CanBeSoldToEconomy && IsBuilt && Field.Owner.WaterEntitlement >= WaterUsage)
                        actions["SellToEconomy"] = true;
                    if (!IsBuilt && !StepBuiltThisTurn && Field.Owner.Money >= ConstructionCostPerBuildStep)
                        actions["ContinueBuild"] = true;
                }    
                // We can't allocate water to incomplete builds
                else if (Game.State == GameStateType.Water && CanBeAllocatedWater && IsBuilt)
                {
                    // If we've managed to allocate it in the first place then we can always deallocate
                    if (WaterAllocated > 0)
                        actions["AllocateWater"] = true;
                    else if (Field.Owner.Water >= WaterUsage)
                        actions["AllocateWater"] = true;
                    else if (CanPartiallyAllocateWater && Field.Owner.Water > 0)
                        actions["AllocateWater"] = true;
                }
                
//                actions["StateIsBuild"] = Game.State == GameStateType.Build;
//                actions["StateIsWater"] = Game.State == GameStateType.Water;

                return actions;
            } 
        }       
        
        /// <value>
        /// The initial names of each of the different levels
        /// </summary>
        public virtual string[] InitialNames { get; set; }

        /// <value>
        /// The field which this asset is in
        /// </value>
        public virtual Field Field { get; set; }
        
        /// <value>
        /// Max level of the asset
        /// </value>
        public virtual int MaxLevel { get; set; }

        /// <value>
        /// Min level of the asset
        /// </value>
        public virtual int MinLevel { get; set; }

        /// <value>
        /// Actual level of the asset
        /// </value>
        public virtual int Level 
        { 
            get { return m_level; }
            set
            {
                if (value > MaxLevel)
                    throw new Exception(
                        string.Format(
                            "Attempt to set level {0} on {1} which is above maximum of {2}", value, this, MaxLevel));
                else if (value < MinLevel)
                    throw new Exception(
                        string.Format(
                            "Attempt to set level {0} on {1} which is below minimum of {2}", value, this, MinLevel));
                else
                    m_level = value;
            }
        }
        protected int m_level;

        /// <value>
        /// The registered position
        /// </value>
        public virtual Vector3 Position { get; set; }

        /// <value>
        /// Total construction costs for each game asset level
        /// </value>
        public virtual int[] ConstructionCosts 
        { 
            get
            {
                int[] costs = new int[ConstructionCostsPerBuildStep.Length];
                for (int i = 0; i < costs.Length; i++)
                    costs[i] = ConstructionCostsPerBuildStep[i] * StepsToBuilds[i];
                
                return costs;
            }
        }
        
        /// <summary>
        /// Construction costs per build turn for each asset level
        /// </summary>
        public virtual int[] ConstructionCostsPerBuildStep { get; set; }

        /// <value>
        /// Revenues for each game asset level under normal economic conditions
        /// </value>
        public virtual int[] NormalRevenues { get; set; }

        /// <value>
        /// Water usage for each game asset level to produce revenue or the entitlement required for it to be sold to
        /// the market.
        /// </value>
        public virtual int[] WaterUsages { get; set; }

        /// <value>
        /// Maintenance cost for each game asset level
        /// </value>
        public virtual int[] MaintenanceCosts { get; set; }
        
        /// <summary>
        /// Total steps required to build each level of asset
        /// </summary>
        public virtual int[] StepsToBuilds { get; set; }
        
        /// <value>
        /// Time that assets at different levels will last.  If INFINITE_TIME_TO_LIVE then it never dies. 
        /// </value>
        public virtual int[] InitialTimesToLive { get; set; }        
        
        /// <summary>
        /// Total steps required to build this asset
        /// </summary>
        public virtual int StepsToBuild { get { return StepsToBuilds[Level]; } }
        
        /// <summary>
        /// Number of steps which have been built.
        /// </summary>
        public virtual int StepsBuilt { get; set; }
        
        /// <summary>
        /// Has a step been built this turn?
        /// </summary>
        public bool StepBuiltThisTurn { get; set; }

        /// <value>
        /// Total cost to build
        /// </value>
        public virtual int ConstructionCost 
        { 
            get { return ConstructionCosts[Level]; }
        }
        
        /// <summary>
        /// Cost to construct each build step
        /// </summary>
        public virtual int ConstructionCostPerBuildStep { get { return ConstructionCostsPerBuildStep[Level]; } }                      
        
        /// <value>
        /// Time that this asset will last.  If INFINITE_TIME_TO_LIVE then it never dies. 
        /// </value>
        public virtual int TimeToLive { get; set; }
        
        /// <summary>
        /// Market price of this asset
        /// </summary>
        public virtual int MarketPrice { get; set; }

        /// <value>
        /// The revenue from this game asset if all water demand is satisfied in normal economic conditions
        /// </value>
        public virtual int NormalRevenue 
        { 
            get { return NormalRevenues[Level]; }
        }
        
        /// <summary>
        /// The revenue this asset would return if it was fully allocated with water
        /// </summary>
        public virtual int RevenueThisTurn { get; set; }
        
        /// <summary>
        /// Projected level of revenue from this asset at current level of water allocation
        /// </summary>
        public virtual int ProjectedRevenue 
        {
            get 
            { 
                // Don't gain revenue from partially built assets.
                if (!IsBuilt)
                    return 0;
                
                int revenue = 0;
                
                // If we can't allocate water to the asset then it doesn't depend on that for its revenue
                if (!CanBeAllocatedWater || (!CanPartiallyAllocateWater && WaterAllocated >= WaterUsage))
                    revenue = RevenueThisTurn;
                else if (CanPartiallyAllocateWater && WaterAllocated > 0)
                    revenue = (int)Math.Ceiling(RevenueThisTurn * (WaterAllocated / (double)WaterUsage));
                
                return revenue;
            }
        }

        /// <value>
        /// Water required for the asset to produce revenue, or the water entitlement required for it to be sold
        /// to the market.
        /// </value>
        public virtual int WaterUsage 
        { 
            get 
            {                   
                return WaterUsages[Level]; 
            }
            
            set 
            { 
                WaterUsages[Level] = value; 
            }
        }

        /// <value>
        /// Maintenance cost each turn for this asset
        /// </value>
        /// <remarks>
        /// Maintenance is required even if the asset is not fully built
        /// </remarks>
        public virtual int MaintenanceCost 
        { 
            get { return MaintenanceCosts[Level]; }
            set { MaintenanceCosts[Level] = value; }
        }
        
        /// <summary>
        /// Accrued maintenance costs for this asset.
        /// </summary>
        public virtual int AccruedMaintenanceCost { get; set; }
        
        /// <summary>
        /// The maximum profit one can achieve from the asset this turn.  
        /// </summary>
        /// <remarks>
        /// This differs per asset type.  For instance, a factory does not count build costs in this (they are treated
        /// as capital costs), while a house does.
        /// 
        /// The cost is nominal because the accrued costs of maintaining the asset before sale (in the case of houses)
        /// are not fully charged - rather a (turns - 1) * MaintenanceCost is charged.
        /// </remarks>
        public virtual int NominalMaximumProfitThisTurn { get { return -999; } }
        
        /// <summary>
        /// Current profit this turn.  
        /// </summary>
        /// <remarks>
        /// This differs per asset type.  For instance, a factory does not count build costs in this (they are treated
        /// as capital costs), while a house does.
        /// 
        /// The cost is nominal because the accrued costs of maintaining the asset before sale (in the case of houses)
        /// are not fully charged - rather a (turns - 1) * MaintenanceCost is charged.
        /// </remarks>
        public virtual int Profit { get { return -999; } }        

        /// <value>
        /// Water allocated to the game asset this turn
        /// </value>
        public virtual int WaterAllocated 
        { 
            get { return m_waterAllocated; }
            set 
            {
                if (value < 0)
                {
                    throw new WaterWarsGameLogicException(
                        string.Format("Cannot allocate negative value {0} water to {1}", value, Name));
                }

                if (value > WaterUsage)
                {
                    throw new WaterWarsGameLogicException(
                        string.Format(
                            "Cannot allocate {0} water to {1} since {2} is the maximum that the asset can use",
                            value, Name, WaterUsage));
                }
                
                if (!CanPartiallyAllocateWater && value != WaterUsage && value != 0)
                {
                    throw new WaterWarsGameLogicException(
                        string.Format(
                            "Cannot partially allocate {0} water to game asset {1}.  It requires {2}", 
                            value, Name, WaterUsage));
                }

                m_waterAllocated = value;
            }
        }
        protected int m_waterAllocated;

        /// <value>
        /// Can one partially allocate water to this asset?
        /// </value>
        public virtual bool CanPartiallyAllocateWater { get; set; }

        /// <value>
        /// Can this asset be upgraded right now?
        /// </value>
        public virtual bool CanUpgrade { get { return CanUpgradeInPrinciple && Level < MaxLevel; } }
        
        /// <summary>
        /// Can this asset be upgraded in principle?
        /// </summary>
        public virtual bool CanUpgradeInPrinciple { get; set; }

        /// <value>
        /// The cost of upgrading this asset to a given level from its current level
        /// </value>
        public virtual int[] UpgradeCosts
        {                       
            get
            {
                int[] upgradePrices = new int[MaxLevel + 1];
                
                for (int i = MinLevel; i <= MaxLevel; i++)
                {
                    if (Level >= i)
                        upgradePrices[i] = 0;
                    else
                        upgradePrices[i] = ConstructionCosts[i] - ConstructionCost;
                }
                
                return upgradePrices;
            }                                    
        }
        
        /// <summary>
        /// Can this game asset be manually allocated water?
        /// </summary>
        public virtual bool CanBeAllocatedWater { get; set; }
        
        /// <summary>
        /// If this game asset is not allocated water, does it disappear/die?
        /// </summary>
        public virtual bool IsDependentOnWaterToExist { get; set; }
        
        /// <summary>
        /// Can this asset be sold to the economy?
        /// </summary>
        public virtual bool CanBeSoldToEconomy { get; set; }
        
        /// <summary>
        /// Has this asset been sold to the economy?
        /// </summary>
        public virtual bool IsSoldToEconomy { get; set; }
        
        /// <summary>
        /// Does the asset require more than one step to build?
        /// </summary>
        public virtual bool IsMultiStepBuild { get { return StepsToBuilds[Level] > 1; } }
        
        /// <summary>
        /// Has this asset been fully built?
        /// </summary>
        public virtual bool IsBuilt 
        { 
            get 
            { 
                return StepsToBuilds[Level] > 0  // Make sure that the null asset returns false
                    && StepsBuilt >= StepsToBuilds[Level]; 
            } 
        }
        
        /// <summary>
        /// For NHibernate
        /// </summary>
        protected AbstractGameAsset() {}
        
        public AbstractGameAsset(
            string name, UUID uuid, AbstractGameAssetType type, Vector3 position, int level, int minLevel, int maxLevel)
            : base(uuid, type, name)
        {   
            // Don't set field to None, since this causes a loop with Field.BuyPoint = BuyPoint.None
            // We don't need Field.None here anyway, since there is never a situation conceptually or practically where
            // a game asset exists without some field (i.e. we set it before it is used anywhere).
//            Field = Field.None;
            Position = position;
            MinLevel = minLevel;            
            MaxLevel = maxLevel;
            Level = level;
                        
            InitialTimesToLive = new int[MaxLevel + 1];
            for (int l = MinLevel; l <= MaxLevel; l++)
                InitialTimesToLive[l] = INFINITE_TIME_TO_LIVE;
            TimeToLive = INFINITE_TIME_TO_LIVE;
        }

        public override string ToString()
        {
            return string.Format("{0} {1} {2}", Name, Uuid, Type);
        }          
    }
}