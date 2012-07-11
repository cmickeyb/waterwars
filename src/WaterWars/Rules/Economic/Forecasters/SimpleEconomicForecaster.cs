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
using WaterWars.Rules.Economic.Generators;

namespace WaterWars.Rules.Economic.Forecasters
{
    /// <summary>
    /// Approximate but always correct forecast
    /// </summary>
    public class SimpleEconomicForecaster : IEconomicForecaster
    {                  
        /// <summary>
        /// Deviations from utopia.  Each index is a round, hence 0 is unused.
        /// </summary>
        public Dictionary<AbstractGameAssetType, double[][]> Deviations { get; set; }
        
        public IDictionary<AbstractGameAssetType, string[]> Forecast(Game game)
        {          
            Dictionary<AbstractGameAssetType, string[]> humanForecasts 
                = new Dictionary<AbstractGameAssetType, string[]>();
            
            int[] agatValues = (int[])Enum.GetValues(typeof(AbstractGameAssetType));
            foreach (int agatValue in agatValues)
            {
                AbstractGameAssetType type = (AbstractGameAssetType)agatValue;
                
                humanForecasts[type] = new string[4];
            
                for (int level = 1; level <= 3; level++)
                {
                    double forecast;
                    
                    if (!Deviations.ContainsKey(type))
                    {
                        forecast = 1;
                    }
                    else if (Deviations[type][level] == null)
                    {
                        forecast = 1;
                    }
                    else if (game.CurrentRound <= Deviations[type][level].Length - 1)
                    {
                        forecast = Deviations[type][level][game.CurrentRound];
                    }
                    else
                    {
                        forecast = 1;
                    }
                
                    string humanForecast;
                    if (forecast >= 1.65)
                        humanForecast = "Good";
                    else if (forecast >= 1)
                        humanForecast = "Normal";
                    else if (forecast >= 0.65)
                        humanForecast = "Below Normal";
                    else
                        humanForecast = "Recession";
                    
                    humanForecasts[type][level] = humanForecast;
                }                    
            }
            
            return humanForecasts;
        }

        public void UpdateConfiguration(IConfigSource configSource)
        {
            Deviations = SeriesEconomicGenerator.LoadSeriesData(configSource);
        }        
    }
}