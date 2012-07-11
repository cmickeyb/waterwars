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
using System.IO;
using Nini.Config;
using OpenMetaverse;
using WaterWars;
using WaterWars.Models;
using WaterWars.States;
using WaterWars.Views;

namespace WaterWars.Tests.Mock
{      
    public class MockDispatcher : IDispatcher
    {
        public string Configuration { get; set; }
        public UUID NextUuid { get; set; }
        public List<UUID> FieldUuids { get; set; }
        
        protected WaterWarsController m_controller;

        public MockDispatcher(WaterWarsController controller)
        {
            m_controller = controller;
            NextUuid = UUID.Random();
        }
        
        public string FetchGameConfiguration() 
        {
            return Configuration;
        }
        
        public string FetchBuyPointConfiguration(BuyPoint bp)
        {
            return null;
        }        

        public void AlertConfigurationFailure(string message) 
        { 
            throw new Exception("MockDispatcher configuration failure - " + message); 
        }

        public Dictionary<UUID, Field> ChangeBuyPointSpecialization(BuyPoint bp, AbstractGameAssetType type, int numberOfFields) 
        {            
            Dictionary<UUID, Field> fields = new Dictionary<UUID, Field>();
            
            for (int i = 0; i < numberOfFields; i++)
            {
                Field f = m_controller.ModelFactory.CreateField(bp, UUID.Random(), "Test Field");
                fields.Add(f.Uuid, f);
            }

            return fields;
        }        

        public void RemoveBuyPointView(BuyPoint bp) {}      
        public Field RemoveGameAssetView(AbstractGameAsset asset) { return null; }
        public void RegisterGameManagerView(GameManagerView gmv) {}
        public void RegisterBuyPointView(BuyPointView bpv) {}
    }
}