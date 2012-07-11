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
using System.Reflection;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Scenes;
using log4net;
using WaterWars;
using WaterWars.Models;

namespace WaterWars.Views
{        
    public class WaterWarsView : AbstractView
    {
//        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        protected WaterWarsController m_controller;
        
        /// <summary>
        /// The view in which items to be rezzed are stored, including self if applicable
        /// </summary>
        protected AbstractView m_itemStoreView;        
        
        public WaterWarsView(WaterWarsController controller, Scene scene) : base(scene)
        {
            m_controller = controller;
        }
        
        public WaterWarsView(WaterWarsController controller, Scene scene, AbstractView itemStoreView) 
            : this(controller, scene)
        {
            m_itemStoreView = itemStoreView;
        }
        
        public SceneObjectGroup ReplaceSceneObjectFromInventoryItem(
            string itemName, AbstractGameModel gm, UUID ownerUuid)
        {
            RootPart.ParentGroup.Scene.DeleteSceneObject(RootPart.ParentGroup, false);                
            SceneObjectGroup so = RezSceneObjectFromInventoryItem(itemName, gm, ownerUuid, RootPart.AbsolutePosition);
            
            // This should be pushed down into RezSceneObjectFromInventoryItem(), but other callers rely on the 
            // scene object maintaining its original name for identification purposes (e.g. crops).
            so.Name = gm.Name;
            
            m_rootPart = so.RootPart;                
            so.ScheduleGroupForFullUpdate();             
            
            return so;
        }
        
        /// <summary>
        /// Rez a scene object from a given inventory item.
        /// </summary>
        /// <remarks>
        /// FIXME: In a better world we could just get the owner from AbstractGameModel.  However, various complications
        /// (e.g. the fact that buy points have development rights owners and water rights owners) prevents this  
        /// currently.
        /// </remarks>
        /// <param name="itemName"></param>
        /// <param name="gm"></param>
        /// <param name="ownerUuid"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        protected SceneObjectGroup RezSceneObjectFromInventoryItem(
            string itemName, AbstractGameModel gm, UUID ownerUuid, Vector3 pos)
        {
//            m_log.InfoFormat(
//                "[WATER WARS]: Rezzing item {0} corresponding to game model {1} at {2}", itemName, gm.Name, pos);
            
            TaskInventoryItem item = GetItem(m_itemStoreView, itemName);
            
            // we're only doing this beforehand so that we can get the rotation (since the existing RezObject() doesn't
            // retain it.  FIXME FIXME FIXME
//            AssetBase objectAsset = m_scene.AssetService.Get(item.AssetID.ToString());
//            string xml = Utils.BytesToString(objectAsset.Data);
//            SceneObjectGroup so = SceneObjectSerializer.FromOriginalXmlFormat(xml);
            
            SceneObjectGroup so = m_itemStoreView.RootPart.Inventory.GetRezReadySceneObject(item);
            so.UUID = gm.Uuid;            
            so.OwnerID = ownerUuid;
            m_scene.AddNewSceneObject(so, true, pos, so.Rotation, Vector3.Zero);            
            
            return so;
        }
    }
}