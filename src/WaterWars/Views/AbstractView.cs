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
using System.Linq;
using System.Reflection;
using log4net;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Framework.Scenes.Serialization;

namespace WaterWars.Views
{        
    /// <summary>
    /// Abstract view class.  Other views inherit from this.
    /// </summary>
    /// 
    /// Don't make any WaterWars references here.
    public abstract class AbstractView : IView
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        public UUID Uuid { get { return RootPart.UUID; } }

        /// <summary>
        /// The scene that this view belongs to
        /// </summary>
        protected Scene m_scene;
        
        /// <summary>
        /// Root part
        /// </summary>
        public SceneObjectPart RootPart 
        { 
            get { return m_rootPart; }
        }
        protected SceneObjectPart m_rootPart;

        public AbstractView(Scene scene)
        {
            m_scene = scene;            
        }
        
        public virtual void Initialize(Vector3 pos) { throw new NotImplementedException(); }
        public void Initialize(SceneObjectGroup sog) { Initialize(sog.RootPart); }
        public virtual void Initialize(SceneObjectPart rootPart)
        {
            m_rootPart = rootPart;
            //m_scene = rootPart.ParentGroup.Scene;

            foreach (SceneObjectPart part in RootPart.ParentGroup.Parts)
            {
//                    m_log.InfoFormat(
//                        "[WATER WARS]: View processing part {0} {1}", part.Name, part.LocalId);

                RegisterPart(part);
            }            
        }

        public virtual void Close()
        {
            // If OpenSim has a problem during the deletion then try to carry on so that it doesn't become impossible
            // to reset the game
            try
            {
                if (RootPart != null)
                    RootPart.ParentGroup.Scene.DeleteSceneObject(RootPart.ParentGroup, false);            
            }
            catch (Exception e)
            {
                m_log.WarnFormat(
                    "[WATER WARS]: Failure when closing view for {0} at {1}.  Continuing.  Exception {2}{3}", 
                    RootPart.Name, RootPart.AbsolutePosition, e.Message, e.StackTrace);
            }
        }

        protected virtual void RegisterPart(SceneObjectPart part) {}

        /// <summary>
        /// Change the scene object in-world that represents this view.
        /// </summary>
        public virtual void ChangeSceneObject(AbstractView newObjectContainer, string newObjectName)
        {
            SceneObjectGroup group = RootPart.ParentGroup;
            
//            m_log.InfoFormat(
//                "[WATER WARS]: Entering AbstractView.ChangeSceneObject to change object {0} to {1}", 
//                group.Name, newObjectName);
            
            // delete everything except the root prim
                
//                m_log.InfoFormat(
//                    "[WATER WARS]: Deleting {0} old prims from {1}", 
//                    originalParts.Count, group.Name);                
                
            foreach (SceneObjectPart part in group.Parts)
            {
                if (!part.IsRoot)
                {
                    SceneObjectGroup groupToDelete = group.DelinkFromGroup(part, false);
                    m_scene.DeleteSceneObject(groupToDelete, false);
                }
            }

            // Super-annoying pause to avoid ghost objects where a final update comes in after the kill command
            // FIXME: Really need a better way of ensuring that this problem doesn't happen
            //System.Threading.Thread.Sleep(1000);            

            // get the new thing that we want to look like
            AssetBase objectAsset 
                = m_scene.AssetService.Get(GetItem(newObjectContainer, newObjectName).AssetID.ToString());

            string xml = Utils.BytesToString(objectAsset.Data);
            SceneObjectGroup sogToCopy = SceneObjectSerializer.FromOriginalXmlFormat(xml);

            // change the name of the morphed group to reflect its new identity
            group.Name = sogToCopy.Name;
            
            // change the shape of the root prim
            group.RootPart.Shape = sogToCopy.RootPart.Shape;
            
//            m_log.InfoFormat(
//                "[WATER WARS]: Adding {0} prims as part of change from {1} to {2}", 
//                sogToCopy.Children.Count, group.Name, newObjectName);
            
            // create all the linked parts of the morph
            foreach (SceneObjectPart part in sogToCopy.Parts)
            {
                if (!part.IsRoot)
                {
                    part.UUID = UUID.Random();
                                
                    // This is necessary to stop inventory item IDs being identical and failing persistence.
                    part.Inventory.ResetInventoryIDs();
                    
                    part.LocalId = m_scene.AllocateLocalId();
                    part.ParentID = group.RootPart.LocalId;
                    part.ParentUUID = group.RootPart.UUID;
                    group.AddPart(part);
                }
            }                        

            // XXX: Nasty nasty nasty
            group.HasGroupChanged = true;
            group.ScheduleGroupForFullUpdate();
        }

        /// <summary>
        /// Get the appropriate inventory item
        /// </summary>
        /// <param name="newObjectContainer"></param>
        /// <param name="newObjectName"></param>
        /// <returns></returns>
        protected TaskInventoryItem GetItem(AbstractView newObjectContainer, string newObjectName)
        {
            SceneObjectPart part = newObjectContainer.RootPart;
            
            IList<TaskInventoryItem> items = part.Inventory.GetInventoryItems(newObjectName);

            if (items.Count == 0)
                throw new Exception(string.Format("Could not find scene object with name {0} to initialize", newObjectName));
            else if (items.Count > 1)
                m_log.ErrorFormat(
                    "[WATER WARS]: Unexpectedly found {0} {1} items in {2}", items.Count, newObjectName, part.Name);

            return items[0];
        }        

        // normalize an angle between -PI and PI (-180 to +180 degrees)
        protected static double NormalizeAngle(double angle)
        {
            if (angle > -Math.PI && angle < Math.PI)
                return angle;

            int numPis = (int)(Math.PI / angle);
            double remainder = angle - Math.PI * numPis;
            if (numPis % 2 == 1)
                return Math.PI - angle;
            return remainder;
        }

        // Old implementation of llRot2Euler, now normalized

        public static Vector3 llRot2Euler(Quaternion r)
        {
            //This implementation is from http://lslwiki.net/lslwiki/wakka.php?wakka=LibraryRotationFunctions. ckrinke
            Quaternion t = new Quaternion(r.X * r.X, r.Y * r.Y, r.Z * r.Z, r.W * r.W);
            double m = (t.X + t.Y + t.Z + t.W);
            if (m == 0) return new Vector3();
            double n = 2 * (r.Y * r.W + r.X * r.Z);
            double p = m * m - n * n;
            if (p > 0)
                return new Vector3((float)NormalizeAngle(Math.Atan2(2.0 * (r.X * r.W - r.Y * r.Z), (-t.X - t.Y + t.Z + t.W))),
                                   (float)NormalizeAngle(Math.Atan2(n, Math.Sqrt(p))),
                                   (float)NormalizeAngle(Math.Atan2(2.0 * (r.Z * r.W - r.X * r.Y), (t.X - t.Y - t.Z + t.W))));
            else if (n > 0)
                return new Vector3(0.0f, (float)(Math.PI * 0.5), (float)NormalizeAngle(Math.Atan2((r.Z * r.W + r.X * r.Y), 0.5 - t.X - t.Z)));
            else
                return new Vector3(0.0f, (float)(-Math.PI * 0.5), (float)NormalizeAngle(Math.Atan2((r.Z * r.W + r.X * r.Y), 0.5 - t.X - t.Z)));
        }
        
        /// <summary>
        /// Find a suitable position for the given object such that it's lowest prim rests on the ground at
        /// the centre point.
        /// </summary>
        /// <returns></returns>        
        public Vector3 FindOnGroundPosition(SceneObjectGroup so)
        {                        
//            m_log.InfoFormat(
//                "[WATER WARS]: Inspecting {0} at {1} for on ground position", so.Name, so.AbsolutePosition);

            Vector3 lowestPoint = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
//            SceneObjectPart lowestPart = null;

            foreach (SceneObjectPart part in so.Parts)
            {
                Vector3 eulerRot = llRot2Euler(part.GetWorldRotation()) * Utils.RAD_TO_DEG;
                
                // XXX: Huge fudge factor since I can't be bothered to calculate a corrected z-distance for 
                // rotation out of the horizontal plane yet
//                    if ((eulerRot.X > 10 && (eulerRot.X < 170 || eulerRot.X > 190))
//                        || (eulerRot.Y > 10 && (eulerRot.Y < 170 || eulerRot.Y > 190)))
                if ((eulerRot.X < -10 || (eulerRot.X > 10 && eulerRot.X < 170))
                    || (eulerRot.Y < -10 || (eulerRot.Y > 10 && eulerRot.Y < 170)))
                {
//                        m_log.InfoFormat(
//                            "[WATER WARS]: Ignoring part {0} {1} scale {2} at {3} since it's z-rotation of {3} is neither < 10 nor 180 +- 10",
//                            part.LinkNum, so.Name, part.Scale, part.AbsolutePosition, eulerRot, part.Scale);
                    continue;
                }
                
//                    m_log.InfoFormat(
//                        "[WATER WARS]: Part {0} of {1} has position {2}, rotation {3}, scale {4}", 
//                        part.LinkNum, so.Name, part.AbsolutePosition, eulerRot, part.Scale);                        
                
                Vector3 point = part.AbsolutePosition;
                point.Z -= part.Scale.Z / 2;

                if (point.Z < lowestPoint.Z)
                {
                    lowestPoint = point;
//                        lowestPart = part;
                }
            }

//            m_log.InfoFormat(
//                "[WATER WARS]: Lowest point for {0} is part {1} at {2}", so.Name, lowestPart.LinkNum, lowestPoint);

            double terrainHeight 
                = m_scene.Heightmap[(int)Math.Floor(lowestPoint.X), (int)Math.Floor(lowestPoint.Y)];            

//            m_log.InfoFormat("[WATER WARS]: Terrain height at {0} is {1}", lowestPoint, terrainHeight);
            
            terrainHeight = (double)Math.Ceiling(terrainHeight);

            Vector3 onGroundPosition 
                = new Vector3(
                    so.AbsolutePosition.X, 
                    so.AbsolutePosition.Y, 
                    (float)terrainHeight + so.AbsolutePosition.Z - lowestPoint.Z);

//            m_log.InfoFormat("[WATER WARS]: On ground position for {0} is {1}", so.Name, onGroundPosition);

            return onGroundPosition;
        }    
                
        /// <summary>
        /// Refresh this view.
        /// </summary>
        /// <remarks>
        /// This shouldn't normally be required.  However, it might be worth calling manually in cases where an 
        /// important update has got lost.
        /// </remarks>
        public void Refresh()
        {
            RootPart.ParentGroup.ScheduleGroupForFullUpdate();
        }        
    }
}