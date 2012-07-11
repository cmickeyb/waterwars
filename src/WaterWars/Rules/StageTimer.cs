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

using System.Timers;
using Nini.Config;
using WaterWars;
using WaterWars.Config;

namespace WaterWars.Rules
{
    /// <summary>
    /// Time game stages.
    /// </summary>
    public class StageTimer
    {
        protected WaterWarsController m_controller;
        protected Timer m_timer = new Timer(1000);
        public int SecondsPerTurn { get; set; }
        public int SecondsLeft { get; private set; }        
        
        public StageTimer(WaterWarsController controller)
        {
            m_controller = controller;
            m_timer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
        }

        /// <summary>
        /// Update the configuration.
        /// </summary>
        /// <param name="configSource"></param>
        public void UpdateConfiguration(IConfigSource configSource)
        {
            IConfig config = configSource.Configs[ConfigurationParser.GENERAL_SECTION_NAME];
            SecondsPerTurn = config.GetInt(ConfigurationParser.SECONDS_PER_STAGE_KEY);                        
        }
        
        public void Start()
        {
            if (SecondsPerTurn > 0)
            {
                SecondsLeft = SecondsPerTurn;
                m_controller.UpdateHudTimeRemaining(SecondsLeft);
                m_timer.Start();
            }
        }

        protected void OnTimedEvent(object source, ElapsedEventArgs e) 
        {
            if (0 == SecondsLeft)
                m_controller.State.EndStage();
            else
                m_controller.UpdateHudTimeRemaining(--SecondsLeft);            
        }        
        
        public void Stop()
        {
            m_timer.Stop();
            m_controller.UpdateHudTimeRemaining(0);            
        }
    }
}