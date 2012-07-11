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
using System.Reflection;
using log4net;
using Nini.Config;
using WaterWars.Models;

namespace WaterWars.Rules.Distributors
{
    /// <summary>
    /// A simple water distributor.  Allocates water to players depending on their total entitlement.  Also allocates
    /// water to rights which aren't yet owned by any player
    /// </summary>
    public class FairWaterDistributor : AbstractWaterDistributor, IWaterDistributor
    {
        public Dictionary<Player, int> Allocate(int water, ICollection<Player> players, ICollection<BuyPoint> buyPoints)
        {
            Dictionary<Player, int> waterAllocated = new Dictionary<Player, int>();
            
            double totalEntitlement = 0;
            
            foreach (Player p in players)
                totalEntitlement += p.WaterEntitlement;
            
            foreach (BuyPoint bp in buyPoints)
                if (!bp.HasAnyOwner)
                    totalEntitlement += bp.InitialWaterRights;
            
            // Either we have enough water to satisfy all requirements or we can only give each player a proportion
            double abundenceRatio = Math.Min(1, water / totalEntitlement);

            foreach (Player p in players)
                waterAllocated[p] = (int)Math.Ceiling(p.WaterEntitlement * abundenceRatio);
            
            return waterAllocated;
        }

        public void UpdateConfiguration(IConfigSource configSource) {}
    }
}