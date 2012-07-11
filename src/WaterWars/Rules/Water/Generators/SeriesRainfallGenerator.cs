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
using WaterWars;
using WaterWars.Config;
using WaterWars.Models;

namespace WaterWars.Rules.Generators
{        
    /// <summary>
    /// Generate water as a known series of deviations from utopia
    /// </summary>
    public class SeriesRainfallGenerator : IRainfallGenerator
    {  
        public static char[] DELIVERY_SERIES_SEPARATOR = new char[] { ',' };
        
        /// <summary>
        /// Deviations from utopia.  Each index is a round, hence 0 is unused.
        /// </summary>
        public List<double> Deviations { get; set; }
        
        public void Initialize(WaterWarsEventManager em)
        {
        }
        
        public int Generate(Game game)
        {
            double totalEntitlement = 0;
            
            foreach (Player p in game.Players.Values)
                totalEntitlement += p.WaterEntitlement;
            
            foreach (BuyPoint bp in game.BuyPoints.Values)
                if (!bp.HasAnyOwner)
                    totalEntitlement += bp.InitialWaterRights;
            
            double generationRatio;
            
            if (game.CurrentRound <= Deviations.Count - 1)
                generationRatio = Deviations[game.CurrentRound];
            else
                generationRatio = 1;
            
            return (int)Math.Ceiling(totalEntitlement * generationRatio);
        }

        public void UpdateConfiguration(IConfigSource configSource)
        {
            IConfig config = configSource.Configs[ConfigurationParser.GENERAL_SECTION_NAME];

            Deviations = ConfigurationParser.ParseSeries(config.GetString(ConfigurationParser.WATER_DELIVERY_SERIES));
        }        
    }
}