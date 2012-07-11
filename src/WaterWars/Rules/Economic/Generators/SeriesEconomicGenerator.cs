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
using WaterWars;
using WaterWars.Config;
using WaterWars.Models;

namespace WaterWars.Rules.Economic.Generators
{        
    /// <summary>
    /// Generate economic activity as a known series of deviations from normal
    /// </summary>
    public class SeriesEconomicGenerator : IEconomicGenerator
    {  
//        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        public const string REVENUE_SERIES_POSTFIX = "revenue_series";
        public const string FACTORY_REVENUE_SERIES = "factories_revenue_series";      
        
        /// <summary>
        /// Deviations from normal.  
        /// The first index is the game asset type
        /// The second index is its level, hence 0 is unused
        /// The third index is the round number, hence 0 is unused.
        /// </summary>
        public Dictionary<AbstractGameAssetType, double[][]> Deviations { get; set; }
        
        public IDictionary<AbstractGameAssetType, double[]> Generate(Game game)
        {
            IDictionary<AbstractGameAssetType, double[]> activityByAgat 
                = new Dictionary<AbstractGameAssetType, double[]>();
            
            int[] agatValues = (int[])Enum.GetValues(typeof(AbstractGameAssetType));
            foreach (int agatValue in agatValues)
            {
                AbstractGameAssetType type = (AbstractGameAssetType)agatValue;
                double[] levelValues = new double[4];
                
                if (Deviations.ContainsKey(type))
                {
                    double[][] levelDeviations = Deviations[type];
                    
                    for (int i = 1; i < levelDeviations.Length; i++)
                    {
//                        m_log.InfoFormat(
//                            "[WATER WARS]: Fetching deviation for {0} at level [{1}], round [{2}]", 
//                            type, i, game.CurrentRound);
                        
                        if (levelDeviations[i] == null)
                            levelValues[i] = 1;
                        else if (levelDeviations[i].Length - 1 >= game.CurrentRound)
                            levelValues[i] = levelDeviations[i][game.CurrentRound];
                        else
                            levelValues[i] = 1;
                    }
                }
                else
                {                    
                    for (int i = 1; i <= 3; i++)
                        levelValues[i] = 1;
                }
                
                activityByAgat[type] = levelValues;
            }
            
            return activityByAgat;
        }

        public void UpdateConfiguration(IConfigSource configSource)
        {   
            Deviations = LoadSeriesData(configSource);
        }
        
        public static Dictionary<AbstractGameAssetType, double[][]> LoadSeriesData(IConfigSource configSource)
        {
            Dictionary<AbstractGameAssetType, double[][]> deviations = new Dictionary<AbstractGameAssetType, double[][]>();                                                
            
            double[][] allCondoDeviations = new double[Houses.Template.MaxLevel + 1][];
            
            IConfig housesConfig = configSource.Configs[ConfigurationParser.CONDOS_SECTION_NAME];
            for (int level = Houses.Template.MinLevel; level <= Houses.Template.MaxLevel; level++)
            {
                string keyName 
                    = ConfigurationParser.GetAssetKey(
                        ConfigurationParser.CONDO_KEY_PREFIX, level, REVENUE_SERIES_POSTFIX); 
                
                if (housesConfig.Contains(keyName))
                    allCondoDeviations[level] 
                        = ConfigurationParser.ParseSeries(housesConfig.GetString(keyName)).ToArray();
            }
            
            deviations[AbstractGameAssetType.Houses] = allCondoDeviations;
            
            IConfig factoriesConfig = configSource.Configs[ConfigurationParser.FACTORIES_SECTION_NAME];                                   
            if (factoriesConfig.Contains(FACTORY_REVENUE_SERIES))
            {
                double[] factoryDeviations 
                    = ConfigurationParser.ParseSeries(factoriesConfig.GetString(FACTORY_REVENUE_SERIES)).ToArray();
                
                double[][] allFactoryDeviations = new double[Factory.Template.MaxLevel + 1][];            
                for (int level = Factory.Template.MinLevel; level <= Factory.Template.MaxLevel; level++)
                {
                    allFactoryDeviations[level] = factoryDeviations;                
                }
                            
                deviations[AbstractGameAssetType.Factory] = allFactoryDeviations;
            }
            
            return deviations;
        }        
    }
}