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

using Nini.Config;
using WaterWars;
using WaterWars.Config;

namespace WaterWars.Rules
{
    /// <summary>
    /// Manages rounds for the game
    /// </summary>    
    public class RoundManager
    {
        /// <value>
        /// Has the game ended?
        /// </value>
        public bool GameEnded { get; set; }
        
        protected WaterWarsController m_controller;
        
        public RoundManager(WaterWarsController controller)
        {
            m_controller = controller;

            Reset();
        }

        /// <summary>
        /// Update the configuration.
        /// </summary>
        /// <param name="configSource"></param>
        public void UpdateConfiguration(IConfigSource configSource)
        {
            IConfig config = configSource.Configs[ConfigurationParser.GENERAL_SECTION_NAME];
            m_controller.Game.TotalRounds = config.GetInt(ConfigurationParser.ROUNDS_PER_GAME_KEY);                        
        }        

        /// <summary>
        /// End the round.
        /// </summary>
        /// <returns>true if the game now ends, false otherwise.</returns>
        public bool EndRound()
        {
            if (m_controller.Game.IsLastRound)
                GameEnded = true;
            else
                m_controller.Game.CurrentRound++;

            return GameEnded;                
        }
        
        /// <summary>
        /// Start the round manager.
        /// </summary>
        /// <remarks>
        /// This only needs to be called once, not at the start of each round.
        /// </remarks>
        public void Start()
        {
            m_controller.Game.CurrentRound = 1;
        }

        public void Reset()
        {
            m_controller.Game.CurrentRound = 0;
            GameEnded = false;
        }
    }
}