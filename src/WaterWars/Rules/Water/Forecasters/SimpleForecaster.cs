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

namespace WaterWars.Rules.Forecasters
{
    /// <summary>
    /// Approximate but always correct forecast
    /// </summary>
    public class SimpleForecaster : IWaterForecaster
    {                  
        /// <summary>
        /// Deviations from utopia.  Each index is a round.  Round 0 counts as the initial build round before the
        /// first water round.
        /// </summary>
        public List<double> Deviations { get; set; }
        
        public void Initialize(WaterWarsEventManager em)
        {
        }
        
        public string Forecast(Game game)
        {           
            double forecast;
            
            if (game.CurrentRound <= Deviations.Count - 1)
                forecast = Deviations[game.CurrentRound];
            else
                forecast = 1;
            
            string humanForecast;
            if (forecast >= 1)
                humanForecast = "Normal";
            else if (forecast >= 0.65)
                humanForecast = "Below Normal";
            else
                humanForecast = "Drought";
            
            return humanForecast;
        }

        public void UpdateConfiguration(IConfigSource configSource)
        {
            IConfig config = configSource.Configs[ConfigurationParser.GENERAL_SECTION_NAME];

            Deviations = ConfigurationParser.ParseSeries(config.GetString(ConfigurationParser.WATER_DELIVERY_SERIES));
        }        
    }
}