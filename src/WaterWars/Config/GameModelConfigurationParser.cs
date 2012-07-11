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
using Nini.Config;
using Nini.Ini;
using System.IO;
using WaterWars.Models;

namespace WaterWars.Config
{  
    /// <summary>
    /// Parses game model configurations
    /// </summary>    
    public class GameModelConfigurationParser : AbstractConfigurationParser
    {                
        public const string GENERAL_SECTION_NAME = "General";
        public const string MODEL_TYPE_KEY = "model_type";
        public const string ZONE_KEY = "zone";

        public static IConfigSource Parse(string rawConfiguration)
        {
            IConfigSource source;
            
            try
            {                
                source = new IniConfigSource(new StringReader(rawConfiguration));
            }
            catch (IniException e)
            {
                throw new ConfigurationException(string.Format("Configuration load failed - {0}", e.Message), e);
            }

            // Check that all the parameters and sections are there.
            try
            {
                IConfig generalConfig = GetConfig(source, GENERAL_SECTION_NAME);
                string rawModelType = generalConfig.GetString(MODEL_TYPE_KEY);

                Enum.Parse(typeof(AbstractGameAssetType), rawModelType, true);
            }
            catch (ArgumentException e)
            {
                throw new ConfigurationException(string.Format("Configuration problem - {0}", e.Message), e);
            }            

            return source;
        }                
    }
}