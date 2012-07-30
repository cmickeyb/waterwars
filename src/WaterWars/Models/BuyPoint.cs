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
using System.Reflection;
using log4net;
using Newtonsoft.Json;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;

namespace WaterWars.Models
{   
    /// <summary>
    /// Represents a buy point in the game
    /// </summary>
    [Serializable]
    public class BuyPoint : AbstractGameModel
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static BuyPoint None = new BuyPoint(UUID.Zero) { Name = "None" };        

        public override Dictionary<string, bool> OwnerActions 
        { 
            get
            {
                Dictionary<string, bool> actions = base.OwnerActions;
                if (Game.State == GameStateType.Build)
                {
                    if (DevelopmentRightsOwner != Player.None)                 
                    {
                        List<AbstractGameAsset> gameAssets;
                        lock (GameAssets)
                            gameAssets = GameAssets.Values.ToList();
                        
                        if (gameAssets.TrueForAll(
                            delegate(AbstractGameAsset ga) { return ga.OwnerUuid != Game.Economy.Uuid; }))
                            actions["SellDevelopmentRights"] = true;                    
                    }                                                                              
                }                

                return actions;
            }
        }

        public override Dictionary<string, bool> NonOwnerActions 
        { 
            get
            {
                Dictionary<string, bool> actions = base.NonOwnerActions;
                
                if (Game.State == GameStateType.Build)
                {
                    if (DevelopmentRightsOwner == Player.None)                 
                        actions["BuyDevelopmentRights"] = true;

                    if (WaterRightsOwner == Player.None)
                        actions["BuyWaterRights"] = true;
                }

                return actions;
            }
        }
        
        /// <summary>
        /// Location information
        /// </summary>
        public virtual Location Location { get; private set; }
        
        /// <summary>
        /// Which zone is this parcel in?
        /// </summary>
        /// <remarks>This is a Water Wars concept rather than an OpenSim one</remarks>
        public virtual string Zone { get; set; }

        /// <value>
        /// The fields belonging to this parcel.  Referential integrity is maintained by changing the buy point reference
        /// in the field, not by editing this dictionary directly.
        /// </value>
        [JsonIgnore]
        public virtual Dictionary<UUID, Field> Fields { get; set; }
        
        /// <value>
        /// true if anybody owns any of the rights, false otherwise 
        /// </value>
        public virtual bool HasAnyOwner
        {
            get { return DevelopmentRightsOwner != Player.None || WaterRightsOwner != Player.None; }
        }

        /// <value>
        /// Who owns the development rights for the land represented by this buy point?  
        /// Will be null if there is no owner
        /// </value>        
        public virtual Player DevelopmentRightsOwner 
        { 
            get { return m_developmentRightsOwner; }
            set
            {
                m_developmentRightsOwner.DevelopmentRightsOwned.Remove(Uuid);
                m_developmentRightsOwner = value;
                value.DevelopmentRightsOwned[Uuid] = this;
            }                
        }
        protected Player m_developmentRightsOwner = Player.None;

        /// <value>
        /// Who owns the water rights for the land represented by this buy point?  
        /// Will be null if there is no owner
        /// </value>
        public virtual Player WaterRightsOwner
        { 
            get
            {
                return m_waterRightsOwner;
            }
            set
            {
                m_waterRightsOwner.WaterRightsOwned.Remove(Uuid);
                m_waterRightsOwner = value;
                value.WaterRightsOwned[Uuid] = this;
            }                
        }
        protected Player m_waterRightsOwner = Player.None;        

        /// <value>
        /// Price of the development rights
        /// </value>
        public virtual int DevelopmentRightsPrice { get; set; }

        /// <value>
        /// Price of the water rights
        /// </value>
        public virtual int WaterRightsPrice { get; set; }

        /// <value>
        /// Water rights that the first buyer receives.  These go into the buyer's pool, so they are not received
        /// by subsequent buyers.
        /// </value>
        public virtual int InitialWaterRights { get; set; }

        /// <value>
        /// Units of water that this parcel requires for its assets
        /// </value>
        public virtual int WaterRequired 
        { 
            get 
            {
                int required = 0;

                lock (GameAssets)
                {
                    foreach (AbstractGameAsset a in GameAssets.Values)
                        if (a.OwnerUuid == DevelopmentRightsOwner.Uuid && a.IsBuilt)
                            required += a.WaterUsage;
                }

                return required;
            } 
        }

        /// <value>
        /// Water currently available for allocation
        /// </value>
        public virtual int WaterAvailable { get; set; }            

        /// <value>
        /// Water allocated to all the game assets in this parcel this turn
        /// </value>
        public virtual int WaterAllocated 
        { 
            get 
            {
                int allocated = 0;

                lock (GameAssets)
                {
                    foreach (AbstractGameAsset a in GameAssets.Values)
                        allocated += a.WaterAllocated;
                }

                return allocated;
            } 
        }
        
        /// <value>
        /// Combined price for all the rights represented by this buy point
        /// </value>
        public virtual int CombinedPrice { get { return DevelopmentRightsPrice + WaterRightsPrice; } }

        /// <value>
        /// Which asset type has been chosen for this buy point?  There can only be one.
        /// </value>
        public virtual AbstractGameAsset ChosenGameAssetTemplate { get; set; }
        
        /// <value>
        /// Associated game assets.  Do not edit this list directly
        /// </value>                
        [JsonIgnore]
        public virtual Dictionary<UUID, AbstractGameAsset> GameAssets { get; set; }
        
        /// <value>
        /// Associated crops.  Do not edit this list directly
        /// </value>        
        [JsonIgnore]
        public virtual List<Crops> Cropss { get; set; }
        
        /// <value>
        /// Associated factories.  Do not edit this list directly
        /// </value>
        [JsonIgnore]
        public virtual List<Factory> Factories { get; set; }

        /// <value>
        /// Associated houses.  Do not edit this list directly
        /// </value>
        [JsonIgnore]
        public virtual List<Houses> Housess { get; set; }

        /// <summary>
        /// For NHibernate
        /// </summary>
        protected BuyPoint() {}
        
        public BuyPoint(UUID uuid) : base(uuid, AbstractGameAssetType.Parcel, string.Empty)
        {    
            Location = new Location();
            Reset();
        }
        
        public BuyPoint(UUID uuid, string name, Vector3 position, ILandObject parcel) : this(uuid)
        {
            Name = name;
            Location.LocalPosition = position;
            Location.Parcel = parcel;
        }

        /// <summary>
        /// Add a game asset.  
        /// </summary>
        /// Will throw an exception if the asset is different from the asset type chosen for this buy point.
        /// <param name="asset"></param>
        public virtual void AddGameAsset(AbstractGameAsset asset)
        {
            if (ChosenGameAssetTemplate != AbstractGameAsset.None && asset.Type != ChosenGameAssetTemplate.Type)
                throw new Exception(
                    string.Format(
                        "Tried to add game asset {0} to a buy point which has chosen {1}", asset, ChosenGameAssetTemplate.Type));                        

            lock (GameAssets)
            {
                GameAssets.Add(asset.Uuid, asset);
                
                switch (asset.Type)
                {
                    case AbstractGameAssetType.Crops:
                        ChosenGameAssetTemplate = Crops.Template;
                        Cropss.Add((Crops)asset);
                        break;
                    case AbstractGameAssetType.Houses:
                        ChosenGameAssetTemplate = Houses.Template;
                        Housess.Add((Houses)asset);
                        break;
                    case AbstractGameAssetType.Factory:
                        ChosenGameAssetTemplate = Factory.Template;
                        Factories.Add((Factory)asset);
                        break;
                    default:
                        throw new Exception(string.Format("Unrecognized asset {0}", asset));
                }
                
                lock (Game.GameAssets)
                    Game.GameAssets[asset.Uuid] = asset;
            }
        }

        /// <summary>
        /// Remove all game assets from the parcel
        /// </summary>
        public virtual void RemoveAllGameAssets()
        {
            lock (GameAssets)
            {
                lock (Game.GameAssets)
                    foreach (AbstractGameAsset ga in GameAssets.Values)
                        Game.GameAssets.Remove(ga.Uuid);
                
                GameAssets.Clear();
                Cropss.Clear();
                Housess.Clear();
                Factories.Clear();
                ChosenGameAssetTemplate = AbstractGameAsset.None;
            }
        }

        public virtual void RemoveGameAsset(AbstractGameAsset asset)
        {
            if (asset.Type != ChosenGameAssetTemplate.Type)
                throw new Exception(
                    string.Format(
                        "Tried to remove game asset {0} from a buy point which has chosen {1}", 
                        asset, ChosenGameAssetTemplate.Type));

            lock (GameAssets)
            {
                if (GameAssets.ContainsKey(asset.Uuid))
                {
                    GameAssets.Remove(asset.Uuid);
    
                    switch (asset.Type)
                    {
                        case AbstractGameAssetType.Crops:
                            Cropss.Remove((Crops)asset);
                            break;
                        case AbstractGameAssetType.Houses:
                            Housess.Remove((Houses)asset);
                            break;
                        case AbstractGameAssetType.Factory:
                            Factories.Remove((Factory)asset);
                            break;
                        default:
                            throw new Exception(string.Format("Unrecognized asset {0}", asset));
                    }
                }
                else
                {
                    m_log.WarnFormat(
                        "[WATER WARS]: Request to remove asset {0} but it wasn't in buy point {1}", asset, this);
                }
    
                if (GameAssets.Count == 0)
                    ChosenGameAssetTemplate = AbstractGameAsset.None;
                
                lock (Game.GameAssets)
                    Game.GameAssets.Remove(asset.Uuid);
            }
        }

        /// <summary>
        /// Reset game information in the buypoint.
        /// </summary>
        /// <remarks>
        /// No information related to the virtual environment (e.g. position) should be reset here.
        /// </remarks>
        public virtual void Reset()
        {
            DevelopmentRightsPrice = 0;
            WaterRightsPrice = 0;
            WaterAvailable = 0;
            DevelopmentRightsOwner = Player.None;
            WaterRightsOwner = Player.None;
            ChosenGameAssetTemplate = AbstractGameAsset.None;
            Fields = new Dictionary<UUID, Field>();
            GameAssets = new Dictionary<UUID, AbstractGameAsset>();
            Cropss = new List<Crops>();
            Factories = new List<Factory>();
            Housess = new List<Houses>();
        }

//        public class FieldDictionary : Dictionary<UUID, Field>
//        {
//            public new void Clear()
//            {
//                m_log.Debug("[WATER WARS]: Clearing fields");
//                
//                lock (this)
//                {
//                    foreach (Field f in Values)
//                    {
//                        f.BuyPoint = null;
//                    }
//                }
//                
//                base.Clear();
//            }
//        }

//        public class FieldDictionary : IDictionary<UUID, Field>
//        {
//            protected Dictionary<UUID, Field> dict = new Dictionary<UUID, Field>();
//            
//            public new void Clear()
//            {
//                m_log.Debug("[WATER WARS]: Clearing fields");
//                
//                lock (this)
//                {
//                    foreach (Field f in dict.Values)
//                    {
//                        f.BuyPoint = null;
//                    }
//                }
//                
//                dict.Clear();
//            }
//        }        
    }
    
    public class Location
    {
        /// <value>
        /// The associated region name
        /// </value>
        public virtual string RegionName { get; set; }
        
        /// <summary>
        /// The associated unique region id
        /// </summary>
        public virtual UUID RegionId { get; set; }
        
        /// <summary>
        /// Region X co-ord on the global map
        /// </summary>
        public virtual uint RegionX { get; set; }
        
        /// <summary>
        /// Region Y co-ord on the global map
        /// </summary>
        public virtual uint RegionY { get; set; }        
        
        /// <summary>
        /// Registered position of the buypoint within the region.
        /// </summary>
        public virtual Vector3 LocalPosition { get; set; }
                
        /// <value>
        /// The associated land parcel
        /// </value>
        [JsonIgnore]        
        public virtual ILandObject Parcel 
        { 
            get { return m_parcel; } 
            set { m_parcel = value; }
        }
        [NonSerialized]
        protected ILandObject m_parcel;        
    }
}