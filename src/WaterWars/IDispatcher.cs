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
using OpenMetaverse;
using WaterWars.Models;
using WaterWars.States;
using WaterWars.Views;

namespace WaterWars
{
    /// <summary>
    /// Interface for dispatching messages back and forth with the virtual environment.
    /// </summary>
    public interface IDispatcher
    {
        void RegisterGameManagerView(GameManagerView gmv);
        void RegisterBuyPointView(BuyPointView bpv);

        /// <summary>
        /// Fetch the game configuration from in-world
        /// </summary>
        /// <returns></returns>        
        string FetchGameConfiguration();
        
        /// <summary>
        /// Fetch a parcel's configuration from in-world
        /// </summary>
        /// <param name="bp"></param>
        /// <returns></returns>
        string FetchBuyPointConfiguration(BuyPoint bp);

        /// <summary>
        /// Tell in-world players that there have been a configuration failure.
        /// </summary>
        /// <param name="message"></param>
        void AlertConfigurationFailure(string message);
            
        /// <summary>
        /// Specialize the buy point for a particular game asset type.
        /// </summary>
        /// <param name="bp"></param>
        /// <param name="type"></param>
        /// <param name="numberOfFields">Number of fields to create</param>
        /// <returns>The fields created as a result of the specialization change</returns>
        Dictionary<UUID, Field> ChangeBuyPointSpecialization(
            BuyPoint bp, AbstractGameAssetType type, int numberOfFields);

        /// <summary>
        /// Remove the given buy point view
        /// </summary>
        /// <param name="bp">Buy point for the view to be removed</param>
        void RemoveBuyPointView(BuyPoint bp);

        /// <summary>
        /// Remove a game asset view
        /// </summary>
        /// <param name="asset"></param>
        /// <returns>The field that replaces it.</returns>
        Field RemoveGameAssetView(AbstractGameAsset asset);
    }
}