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
using Nini.Config;
using WaterWars.Models;

namespace WaterWars.Rules.Economic.Distributors
{
    /// <summary>
    /// Allocate potential revenue amongst the game assets
    /// </summary>
    public interface IEconomicDistributor
    {        
        /// <summary>
        /// Perform the allocation
        /// </summary>
        /// <param name="economicConditions">The economic conditions prevailing for each game asset type</param>
        /// <param name="gameAsset">Game asset on which to allocate</param>
        /// <returns>
        /// The maximum revenue achievable by this asset for this turn
        /// The caller is responsible for updating this in the model.
        /// </returns>
        int Allocate(IDictionary<AbstractGameAssetType, double[]> economicConditions, AbstractGameAsset gameAsset);
        
        /// <summary>
        /// Perform the allocation
        /// </summary>
        /// <param name="economicConditions">The economic conditions prevailing for each game asset type</param>
        /// <param name="gameAssets">Game assets over which to allocate</param>
        /// <returns>
        /// The maximum revenue achievable by each asset for this turn
        /// The caller is responsible for updating this in the model.
        /// </returns>
        Dictionary<AbstractGameAsset, int> Allocate(
            IDictionary<AbstractGameAssetType, double[]> economicConditions, ICollection<AbstractGameAsset> gameAssets);

        /// <summary>
        /// Update the configuration.
        /// </summary>
        /// <param name="configSource"></param>
        void UpdateConfiguration(IConfigSource configSource);        
    }
}