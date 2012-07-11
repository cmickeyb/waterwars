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

using System.Collections.Generic;
using System.Linq;
using Nini.Config;
using WaterWars;
using WaterWars.Config;
using WaterWars.Models;

namespace WaterWars.Rules.Generators
{        
    /// <summary>
    /// Generate enough water to satisfy all water rights +/- (initial per parcel water rights * 4)
    /// </summary>
    public class SimpleRandomBelowIdealRainfallGenerator : IRainfallGenerator
    {
        public int IdealWaterPerParcel { get; set; }
        
        public void Initialize(WaterWarsEventManager em)
        {
        }
        
        public int Generate(Game game)
        {
            List<BuyPoint> buyPoints = game.BuyPoints.Values.ToList();
            
            // Ideal rainfall that would cover all parcel entitlements
            int idealRainfall = buyPoints.Count * IdealWaterPerParcel;
            
            // Variable range of rainfall
            /*
            int variableRainfallRange = BuyPoints.Count * WaterWarsConstants.PARCEL_WATER_ENTITLEMENT;
            
            RainfallTotal 
                = idealRainfall + WaterWarsUtils.Random.Next(-variableRainfallRange / 2, variableRainfallRange / 2);
            */

            int variableRainfallRange = (buyPoints.Count / 4) * IdealWaterPerParcel;
            int water = idealRainfall - WaterWarsUtils.Random.Next(0, variableRainfallRange);
            
            return water;            
        }

        public void UpdateConfiguration(IConfigSource configSource)
        {
            IConfig parcelsConfig = configSource.Configs[ConfigurationParser.PARCELS_SECTION_NAME];
            IdealWaterPerParcel = parcelsConfig.GetInt(ConfigurationParser.DEFAULT_WATER_ENTITLEMENT_KEY);            
        }        
    }
}