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
using System.Drawing;
using System.Reflection;
using System.Text;
using log4net;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Scenes;
using WaterWars.Config;
using WaterWars.Models;
using WaterWars.Models.Roles;
using WaterWars.Views.Interactions;
using WaterWars.Views.Widgets;
using WaterWars.Views.Widgets.Behaviours;

namespace WaterWars.Views
{        
    public class GameManagerView : WaterWarsView
    {
//        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        public const string IN_WORLD_NAME = "New Mexico State Capital";
		
		protected GameManagerButton m_gameManagerButton;
            
        public GameManagerView(WaterWarsController controller, Scene scene) : base(controller, scene) {}

        protected override void RegisterPart(SceneObjectPart part)
        {
            if (part.Name == IN_WORLD_NAME)
                m_gameManagerButton = new GameManagerButton(m_controller, part);
        }

        /// <summary>
        /// Create a buy point view with an existing scene object.
        /// </summary>
        /// <param name="so">An existing scene object that will represent this view in-world</param>
        /// <param name="bp"></param>
        /// <returns></returns>
        public BuyPointView CreateBuyPointView(SceneObjectGroup so, BuyPoint bp)
        {
            BuyPointView bpv = new BuyPointView(m_controller, so.Scene, this, bp);                        
            bpv.Initialize(so);
            m_controller.Dispatcher.RegisterBuyPointView(bpv);

            return bpv;
        }

        /// <summary>
        /// Create a buy point view and an accompanying in-world object.
        /// </summary>              
        /// <param name="scene"></param>
        /// <param name="rezPoint"></param>
        /// <returns></returns>
        public BuyPointView CreateBuyPointView(Scene scene, Vector3 rezPoint)
        {           
            BuyPointView bpv = new BuyPointView(m_controller, scene, this, null);                        
            bpv.Initialize(rezPoint);

            // TODO: Add code to register ex-nihilio buy points on the fly, if necessary.

            return bpv;
        }

        public void AlertConfigurationFailure(string message)
        {
            m_gameManagerButton.SendAlert(message);
        }
        
        /// <summary>
        /// Fetch game configuration.
        /// </summary>
        /// <returns></returns>
        public string FetchGameConfiguration()
        {
            return m_gameManagerButton.FetchGameConfiguration();
        }

        /// <summary>
        /// Handle configuration.
        /// </summary>
        public class GameManagerButton : WaterWarsButton
        {
            public const string GAME_CONFIGURATION_NOTECARD_NAME = "Game Configuration";
            
            public GameManagerButton(WaterWarsController controller, SceneObjectPart part) 
                : base(controller, part, 4500, new FixedTextureBehaviour())
            {                
                LabelBehaviour = new BlankableDynamicLabelBehaviour();
                LabelBehaviour.TextColor = Color.MediumBlue;
                UpdateLabel(m_controller.Game.State);
                
                m_controller.EventManager.OnStateStarted += UpdateLabel;
                
                Enabled = true;             
            }

            /// <summary>
            /// Fetch game configuration.
            /// </summary>
            /// <exception cref="ConfigurationException">Thrown if there is a problem with the configuration.</exception>
            /// <returns></returns>            
            public string FetchGameConfiguration()
            {
                return FetchConfiguration(GAME_CONFIGURATION_NOTECARD_NAME);
            }  
            
            protected override AbstractInteraction CreateInteraction(IClientAPI remoteClient)
            {
                return new GameManagerTopLevelInteraction(m_controller, remoteClient.AgentId, this);
            }   
            
            protected void UpdateLabel(GameStateType newState)
            {
                LabelBehaviour.Text 
                    = string.Format("CLICK ME FOR GAME OPTIONS\nCurrent game phase is {0}\n \n \n \n \n", newState);
            }
        }
    }
}