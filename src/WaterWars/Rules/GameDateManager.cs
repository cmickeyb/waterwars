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
using System.Globalization;
using Nini.Config;
using WaterWars;
using WaterWars.Config;

namespace WaterWars.Rules
{
    /// <summary>
    /// Manages game dates
    /// </summary>    
    public class GameDateManager
    {
        /// <value>
        /// The amount of time to advance the game date on each request.
        /// </value>
//        public TimeSpan AdvanceInterval { get; set; }

        protected IFormatProvider m_parseCulture = new CultureInfo("en-US", true);
        
        protected WaterWarsController m_controller;
        
        public GameDateManager(WaterWarsController controller)
        {
            m_controller = controller;
        }
        
        /// <summary>
        /// Update the configuration.
        /// </summary>
        /// <param name="configSource"></param>
        public void UpdateConfiguration(IConfigSource configSource)
        {
            IConfig config = configSource.Configs[ConfigurationParser.GENERAL_SECTION_NAME];
            //StartDate = DateTime.Parse("11/2/1904", parseCulture);
			
			string rawStartDate = config.GetString(ConfigurationParser.START_DATE_KEY);
			
			try
			{
            	m_controller.Game.StartDate = DateTime.Parse(rawStartDate, m_parseCulture);
			}
			catch (FormatException e)
			{
				throw new ConfigurationException(string.Format("Game start date {0} invalid", rawStartDate), e);
			}
			
            m_controller.Game.CurrentDate = m_controller.Game.StartDate;
//            AdvanceInterval = new TimeSpan(30, 0, 0, 0);
        }

        /// <summary>
        /// Advance the current date.
        /// </summary>
        /// <returns>The newly advanced date.</returns>
        public DateTime AdvanceDate()
        {
//            CurrentDate = CurrentDate.Add(AdvanceInterval);
            m_controller.Game.CurrentDate = m_controller.Game.CurrentDate.AddYears(1);
            return m_controller.Game.CurrentDate;
        }        
    }
}