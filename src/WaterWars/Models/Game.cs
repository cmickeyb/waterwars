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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Scenes;
using WaterWars.Models.Roles;

namespace WaterWars.Models
{
    /// <summary>
    /// A model representing the game itself
    /// </summary>
    [Serializable]
    public class Game : AbstractModel
    {
        public static Game None = new Game(UUID.Zero, "Null Game", GameStateType.None);     
        
        public virtual GameStateType State { get; set; }

        /// <value>
        /// Game start date
        /// </value>
        public DateTime StartDate { get; set; }
        
        /// <summary>
        /// Game start date as a milliseconds unix timestamp
        /// </summary>
        public long StartDateAsMs { get { return WaterWarsUtils.ToUnixTime(StartDate) * 1000; } }

        /// <value>
        /// Current game date
        /// </value>
        public DateTime CurrentDate { get; set; }

        /// <summary>
        /// Current game date as a milliseconds unix timestamp
        /// </summary>
        public long CurrentDateAsMs { get { return WaterWarsUtils.ToUnixTime(CurrentDate) * 1000; } }
        
        /// <value>
        /// CurrentRound
        /// </value>
        public int CurrentRound { get; set; }
        
        /// <value>
        /// Total number of rounds for the game
        /// </value>
        public int TotalRounds { get; set; }      
                
        /// <summary>
        /// Is it the last round?
        /// </summary>
        public bool IsLastRound { get { return CurrentRound >= TotalRounds; } }         
        
        /// <summary>
        /// Roles available for this game.  Includes non-playable roles.
        /// </summary>
        public List<IRole> Roles { get; set; }   
        
        /// <summary>
        /// Represents the game economy
        /// </summary>
        /// We can't pass this along as JSON since there would be a circular reference with the Game property on the 
        /// economy player.
        [JsonIgnore]
        public virtual Player Economy { get; set; }
        
        /// <summary>
        /// The players of this game.
        /// </summary>
        /// Almost always, you will want to act on a list copy of this dictionary rather than locking the dictionary
        /// itself.
        [JsonIgnore]
        public virtual Dictionary<UUID, Player> Players { get; set; }
        
        /// <summary>
        /// The parcels of this game
        /// </summary>
        /// Almost always, you will want to act on a list copy of this dictionary rather than locking the dictionary
        /// itself.
        [JsonIgnore]
        public virtual Dictionary<UUID, BuyPoint> BuyPoints { get; set; }
        
        /// <summary>
        /// The game assets of this game
        /// </summary>
        /// Almost always, you will want to act on a list copy of this dictionary rather than locking the dictionary
        /// itself.
        [JsonIgnore]
        public virtual Dictionary<UUID, AbstractGameAsset> GameAssets { get; set; }        
        
        /// <summary>
        /// Economic activity for each game asset type at the current time
        /// </summary>
        public virtual IDictionary<AbstractGameAssetType, double[]> EconomicActivity { get; set; }
        
        /// <summary>
        /// Water forecast for next turn
        /// </summary>
        public virtual Forecast Forecast { get; set; }
        
        /// <summary>
        /// Perform the given action for each player in the game
        /// </summary>
        /// <param name="action"></param>
        public void ForEachPlayer(Action<Player> action)
        {
            List<Player> players;
            lock (Players)
                players = Players.Values.ToList();
            
            players.ForEach(action);
        }
        
        /// <summary>
        /// For NHibernate
        /// </summary>
        protected Game() {}
        
        public Game(UUID uuid, string name, GameStateType state) : base(uuid, name) 
        {
            State = state;            
            Roles = new List<IRole>();                      
            BuyPoints = new Dictionary<UUID, BuyPoint>();
            Players = new Dictionary<UUID, Player>();
            GameAssets = new Dictionary<UUID, AbstractGameAsset>();
        }              

        public override string ToString()
        {
            return Name;
        }
    }
           
    public class Forecast
    {
        public string Water { get; set; }
        public IDictionary<AbstractGameAssetType, string[]> Economic { get; set; }
    }    
}