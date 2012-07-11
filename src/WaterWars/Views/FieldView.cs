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
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Framework.Scenes.Serialization;
using WaterWars;
using WaterWars.Models;
using WaterWars.Views.Widgets;
using WaterWars.Views.Widgets.Behaviours;

namespace WaterWars.Views
{        
    public class FieldView : WaterWarsView 
    {
//        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected static string ITEM_NAME = "Field";
        
        protected Field m_field;
        protected WaterWarsButton m_button;        
        protected Vector3 m_scale;
        protected string m_name;

        public FieldView(
            WaterWarsController controller, Scene scene, Field field, Vector3 scale, AbstractView itemStoreView)
            : base(controller, scene, itemStoreView)
        {
            m_field = field;
            m_scale = scale;
        }
        
        public override void Initialize(Vector3 pos)
        {
            SceneObjectGroup so = RezSceneObjectFromInventoryItem(ITEM_NAME, m_field, m_field.Owner.Uuid, pos);
            so.Name = m_field.Name;

            base.Initialize(so);  
            
            m_button.Enabled = true;
            m_button.OnClick 
                += delegate(Vector3 offsetPos, IClientAPI remoteClient, SurfaceTouchEventArgs surfaceArgs)
                    { Util.FireAndForget(
                        delegate { 
                            m_controller.ViewerWebServices.PlayerServices.SetLastSelected(
                                remoteClient.AgentId, m_field); }); };              

            // We can only reposition after we've passed the scene object up to the parent class
            so.AbsolutePosition = FindOnGroundPosition(so);

            // This is a hack to rescale the object
            // XXX: What we really need in OpenSim is better methods that allow us to pass in the scene object for
            // creation directly
            so.RootPart.Scale = m_scale;
            so.SendGroupFullUpdate();
            
            // FIXME: We have to do this manually right now but really it's the responsibilty of OpenSim.
            so.CreateScriptInstances(0, true, m_scene.DefaultScriptEngine, 0);            

//            Console.Write(
//                string.Format(
//                    "[WATER WARS]: {0}, local ID {1}, textures {2}", 
//                    item.Name, so.RootPart.LocalId, so.RootPart.Shape.Textures));                               
        }        

        protected override void RegisterPart(SceneObjectPart part)
        {
            if (part.Name.StartsWith(ITEM_NAME))
                m_button = new WaterWarsButton(m_controller, part, new FixedTextureBehaviour());
        }        

        public override void Close()
        {
            m_button.Close();
            base.Close();
        }         
    }
}