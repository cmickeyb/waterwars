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
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Framework.Scenes.Serialization;
using WaterWars.Views.Interactions;
using WaterWars.Views.Widgets.Behaviours;

namespace WaterWars.Views.Widgets
{
    /// <summary>
    /// Decorate a view
    /// </summary>
    public class OsDecorator : AbstractView
    {   
//        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);        
        
        /// <summary>
        /// Should the decorator be displayed?
        /// </summary>
        public bool IsDisplayed
        { 
            get 
            { 
                return m_isDisplayed; 
            }
            
            set 
            { 
                if (value == m_isDisplayed)
                    return;
                
                if (value)
                    Display();
                else
                    NoDisplay();
                        
                m_isDisplayed = value; 
            }
        }
        protected bool m_isDisplayed;
        
        /// <summary>
        /// This is where the item for self rezzing is actually stored
        /// </summary>
        protected AbstractView m_itemStoreView;
        
        /// <summary>
        /// The name of the item that will act as the decorator
        /// </summary>
        public string m_itemName;
        
        /// <summary>
        /// Position at which to display the decorator if it's showing.
        /// </summary>
        protected Vector3 m_positionToDisplay;        
        
        public OsDecorator(Scene scene, AbstractView itemStoreView, string itemName, Vector3 positionToDisplay)
            : base(scene)
        {
            m_itemStoreView = itemStoreView;         
            m_itemName = itemName;
            m_positionToDisplay = positionToDisplay;
        }
        
        protected virtual void Display()
        {
//            m_log.InfoFormat("[WATER WARS]: Rezzing decorator {0} at {1}", m_itemName, m_positionToDisplay);
            
            TaskInventoryItem item = GetItem(m_itemStoreView, m_itemName);
            
            // we're only doing this beforehand so that we can get the rotation (since the existing RezObject() doesn't
            // retain it.  FIXME FIXME FIXME
            AssetBase objectAsset = m_scene.AssetService.Get(item.AssetID.ToString());
            string xml = Utils.BytesToString(objectAsset.Data);
            SceneObjectGroup originalSog = SceneObjectSerializer.FromOriginalXmlFormat(xml);
            
//            m_log.DebugFormat("[WATER WARS]: Using rotation {0} for decorator {1}", originalSog.GroupRotation, m_itemName);
            
            SceneObjectGroup so 
                = m_scene.RezObject(
                    m_itemStoreView.RootPart, item, m_positionToDisplay, originalSog.GroupRotation, Vector3.Zero, 0);

            base.Initialize(so);
            
            // We can only reposition after we've passed the scene object up to the parent class
//            so.AbsolutePosition = FindOnGroundPosition(so);
            
            // FIXME: We have to do this manually right now but really it's the responsibilty of OpenSim.
//            so.CreateScriptInstances(0, true, m_scene.DefaultScriptEngine, 0);            

//            Console.Write(
//                string.Format(
//                    "[WATER WARS]: {0}, local ID {1}, textures {2}", 
//                    item.Name, so.RootPart.LocalId, so.RootPart.Shape.Textures));
        }  
        
        protected virtual void NoDisplay()
        {
            m_rootPart.ParentGroup.Scene.DeleteSceneObject(RootPart.ParentGroup, false);
            m_rootPart = null;
        }
    }
}