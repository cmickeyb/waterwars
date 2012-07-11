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
using Nini.Ini;
using System.IO;
using WaterWars.Models;
using WaterWars.Models.Roles;

namespace WaterWars.Config
{    
    /// <summary>
    /// Parse configuration data to make sure that it's valid.
    /// </summary>
    public class ConfigurationParser : AbstractConfigurationParser
    {
        public const string GENERAL_SECTION_NAME = "General";
        public const string SECONDS_PER_STAGE_KEY = "seconds_per_stage";
        public const string START_DATE_KEY = "start_date";
        public const string ROUNDS_PER_GAME_KEY = "rounds_per_game";
        public const string WATER_DELIVERY_SERIES = "water_delivery_series";
        public const string PREALLOCATE_PARCELS_KEY = "preallocate_parcels";
        
        public const string PLAYERS_SECTION_NAME = "Players";
        public const string PLAYER_START_MONEY_KEY = "start_money";
        public const string PLAYER_COST_OF_LIVING_KEY = "cost_of_living";

        public const string PARCELS_SECTION_NAME = "Parcels";
//        public const string INITIAL_DEVELOPMENT_RIGHTS_PRICE_KEY = "initial_development_rights_price";
//        public const string INITIAL_WATER_RIGHTS_PRICE_KEY = "initial_water_rights_price";
        public const string PARCEL_WATER_ENTITLEMENT_KEY = "parcel_water_entitlement";
        public const string ZONE_RIGHTS_PRICE_KEY = "rights_price";
        public const string ZONE_WATER_ENTITLEMENT_KEY = "water_entitlement";
        public const string DEFAULT_RIGHTS_PRICE_KEY = ZONE_RIGHTS_PRICE_KEY;
        public const string DEFAULT_WATER_ENTITLEMENT_KEY = ZONE_WATER_ENTITLEMENT_KEY;
        
        public const string TOTAL_WATER_AVAILABLE_PER_TURN_KEY = "total_water_available_per_turn";

        public const string CROPS_SECTION_NAME = "Crops";

        /// <value>
        /// {0} - crop name
        /// {1} - parameter type
        /// </value>
        public const string GAME_ASSET_KEY_FORMAT = "{0}_{1}";
        
        public const string CONDOS_SECTION_NAME = "Condos";
        public const string CONDO_KEY_PREFIX = "condos";
        
        public const string FACTORIES_SECTION_NAME = "Factories";
        public const string FACTORY_KEY_PREFIX = "factories";

        public const string COST_POSTFIX_KEY = "cost_per_build_step";
        public const string BUILD_TURNS_POSTFIX_KEY = "build_steps";
        public const string REVENUE_POSTFIX_KEY = "revenue";
        public const string MAINTENANCE_POSTFIX_KEY = "maintenance";
        public const string WATER_POSTFIX_KEY = "water";

        public static readonly string[] GAME_ASSET_POSTFIX_KEYS 
            = new string[] 
                { COST_POSTFIX_KEY, BUILD_TURNS_POSTFIX_KEY, REVENUE_POSTFIX_KEY, 
                  MAINTENANCE_POSTFIX_KEY, WATER_POSTFIX_KEY };

        public static IConfigSource Parse(string rawConfiguration, HashSet<string> zones)
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
                IConfig parcelsConfig = GetConfig(source, PARCELS_SECTION_NAME);
//                parcelsConfig.GetInt(INITIAL_DEVELOPMENT_RIGHTS_PRICE_KEY);
//                parcelsConfig.GetInt(INITIAL_WATER_RIGHTS_PRICE_KEY);
//                parcelsConfig.GetInt(PARCEL_WATER_ENTITLEMENT_KEY);                
                parcelsConfig.GetInt(DEFAULT_RIGHTS_PRICE_KEY);
                parcelsConfig.GetInt(DEFAULT_WATER_ENTITLEMENT_KEY);                
                
                foreach (string zone in zones)
                {
                    parcelsConfig.GetInt(GetZoneKey(zone, ZONE_RIGHTS_PRICE_KEY));
                    parcelsConfig.GetInt(GetZoneKey(zone, ZONE_WATER_ENTITLEMENT_KEY));
                }
                
                IConfig playersConfig = GetConfig(source, PLAYERS_SECTION_NAME);            
                playersConfig.GetInt(GetPlayerKey(RoleType.Manufacturer, PLAYER_START_MONEY_KEY));
                playersConfig.GetInt(GetPlayerKey(RoleType.Developer, PLAYER_START_MONEY_KEY));
                playersConfig.GetInt(GetPlayerKey(RoleType.Farmer, PLAYER_START_MONEY_KEY));
                
                playersConfig.GetInt(GetPlayerKey(RoleType.Manufacturer, PLAYER_COST_OF_LIVING_KEY));
                playersConfig.GetInt(GetPlayerKey(RoleType.Developer, PLAYER_COST_OF_LIVING_KEY));
                playersConfig.GetInt(GetPlayerKey(RoleType.Farmer, PLAYER_COST_OF_LIVING_KEY));
    
                IConfig cropsConfig = GetConfig(source, CROPS_SECTION_NAME);            
                foreach (string postfix in GAME_ASSET_POSTFIX_KEYS)
                {
                    foreach (string name in Enum.GetNames(typeof(CropType)))
                    {
                        string keyName = string.Format(GAME_ASSET_KEY_FORMAT, name.ToLower(), postfix);
                        cropsConfig.GetInt(keyName);
                    }
                }

                IConfig condosConfig = GetConfig(source, CONDOS_SECTION_NAME);
                foreach (string postfix in GAME_ASSET_POSTFIX_KEYS)
                {                
                    for (int i = Houses.Template.MinLevel; i <= Houses.Template.MaxLevel; i++)
                        condosConfig.GetInt(GetAssetKey(CONDO_KEY_PREFIX, i, postfix));                  
                }                                                           

                IConfig factoriesConfig = GetConfig(source, FACTORIES_SECTION_NAME);
                foreach (string postfix in GAME_ASSET_POSTFIX_KEYS)
                {                
                    for (int i = Factory.Template.MinLevel; i <= Factory.Template.MaxLevel; i++)
                        factoriesConfig.GetInt(GetAssetKey(FACTORY_KEY_PREFIX, i, postfix));
                }                 
            }
            catch (ArgumentException e)
            {
                throw new ConfigurationException(string.Format("Configuration problem - {0}", e.Message), e);
            }            
            
            return source;
        }
        
        public static string GetAssetKey(string type, int level, string keyPostfix)
        {
            return string.Format("{0}_{1}_{2}", type, level, keyPostfix);
        }
        
        public static string GetPlayerKey(RoleType roleType, string keyPostfix)
        {
            return string.Format("{0}_{1}", roleType.ToString().ToLower(), keyPostfix);
        }
        
        public static string GetZoneKey(string zone, string keyPostFix)
        {
            return string.Format("{0}_zone_{1}", zone, keyPostFix);
        }
    }
}