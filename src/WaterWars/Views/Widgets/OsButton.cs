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
using WaterWars.Views.Interactions;
using WaterWars.Views.Widgets.Behaviours;

namespace WaterWars.Views.Widgets
{
    /// <summary>
    /// A hud or in-world OpenSim button
    /// </summary>
    public class OsButton
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static string OK_OPTION = "OK";
        public static string CANCEL_OPTION = "Cancel";        
        public static string[] OK_CANCEL_OPTIONS = new string[] { OK_OPTION, CANCEL_OPTION };

        public static string BUY_OPTION = "Buy";
        public static string UPGRADE_OPTION = "Upgrade";
        public static string SELL_OPTION = "Sell";
        public static string[] BUY_UPGRADE_SELL_OPTIONS  = new string[] { BUY_OPTION, UPGRADE_OPTION, SELL_OPTION };
        public static string[] BUY_SELL_OPTIONS  = new string[] { BUY_OPTION, SELL_OPTION };
        public static string[] BUY_OPTIONS  = new string[] { BUY_OPTION };
        
        public static readonly UUID DUMMY_SCRIPT_UUID = UUID.Parse("00000000-0000-0000-0000-000000000001");

        /// <value>
        /// Fired if the button receives chat, as happens if it's receiving information from a dialog box
        /// </value>
        public event OnChatDelegate OnChat;
        public delegate void OnChatDelegate(OSChatMessage chat);
        
        /// <value>
        /// Fired if the button is clicked
        /// </value>
        public event OnClickDelegate OnClick;
        public delegate void OnClickDelegate(
            Vector3 offsetPos, IClientAPI remoteClient, SurfaceTouchEventArgs surfaceArgs);    

        /// <value>
        /// These handle the interaction between a particular player and this button
        /// </value>
        protected Dictionary<UUID, AbstractInteraction> m_interactions = new Dictionary<UUID, AbstractInteraction>();

        public SceneObjectPart Part { get { return m_part; } }            
        protected SceneObjectPart m_part;        

        public IDialogModule DialogModule { get { return m_dialogModule; } }
        protected IDialogModule m_dialogModule;
        
        protected Scene m_scene;
        protected EventManager m_eventManager;

        public int Channel { get; private set; }

        public IDisplayBehaviour DisplayBehaviour { get; set; }
        public ILabelBehaviour LabelBehaviour 
        { 
            get
            {
                return m_labelBehaviour;
            } 
            
            set
            {
                m_labelBehaviour = value;
                m_labelBehaviour.Button = this;
                m_labelBehaviour.UpdateAppearance();
            }
        }
        protected ILabelBehaviour m_labelBehaviour;

        /// <value>
        /// The set of prim local ids for which we will need to process events.
        /// </value>
        protected HashSet<uint> m_localIds = new HashSet<uint>();

        public OsButton(SceneObjectPart part, IDisplayBehaviour displayBehaviour)
            : this(part, displayBehaviour, new FixedLabelBehaviour()) {}

        public OsButton(SceneObjectPart part, IDisplayBehaviour displayBehaviour, ILabelBehaviour labelBehaviour)
        {
            m_part = part;
            m_scene = m_part.ParentGroup.Scene;            
            
            ResetButtonPrims();
            
            m_eventManager = m_scene.EventManager;            
            m_dialogModule = m_scene.RequestModuleInterface<IDialogModule>();            
            DisplayBehaviour = displayBehaviour;
            displayBehaviour.Button = this;
            DisplayBehaviour.UpdateAppearance();                        

            LabelBehaviour = labelBehaviour;            
        }        

        public OsButton(SceneObjectPart part, int channel, IDisplayBehaviour displayBehaviour) 
            : this(part, displayBehaviour)
        {
            Channel = channel;
        }

        public OsButton(SceneObjectPart part, int channel, IDisplayBehaviour displayBehaviour, ILabelBehaviour labelBehaviour) 
            : this(part, displayBehaviour, labelBehaviour)
        {
            Channel = channel;
        }        
        
        /// <value>
        /// Enable or disable button
        /// </value>
        public virtual bool Enabled
        {
            get { return m_enabled; }
            set
            {
                if (value == m_enabled)
                    return;
                
                m_enabled = value;

                Update();
            }
        }
        protected bool m_enabled;        
        
        /// <summary>
        /// Reset the prims the constitue this button.
        /// </summary>
        /// <remarks>
        /// This must be called if the object changes in the virtual environment.
        /// </remarks>
        /// <param name="part"></param>
        public void ResetButtonPrims(SceneObjectPart part)
        {
            m_part = part;
            ResetButtonPrims();
            Update();
        }
        
        /// <summary>
        /// Reset the prims that constitute this button
        /// </summary>
        /// <remarks>
        /// This must be called if the object is morphed in the virtual environment.
        /// </remarks>
        public void ResetButtonPrims()
        {
            m_localIds.Clear();
            
            if (m_part.IsRoot)
                m_part.ParentGroup.ForEachPart(delegate(SceneObjectPart sop) { m_localIds.Add(sop.LocalId); });
            else
                m_localIds.Add(m_part.LocalId);                                    
        }

        protected void OnOsChat(object sender, OSChatMessage chat) 
        {
            if (OnChat != null)
            {
                foreach (OnChatDelegate d in OnChat.GetInvocationList())
                {
                    try
                    {
                        d(chat);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat(
                            "[WATER WARS]: Delegate for OnOsChat failed - continuing.  {0}{1}", 
                            e.Message, e.StackTrace);
                    }
                }
            }
        }
        
        protected void OnOsTouch(
            uint localID, uint originalID, Vector3 offsetPos, IClientAPI remoteClient, SurfaceTouchEventArgs surfaceArgs)
        {           
            if (!m_localIds.Contains(originalID))
                return;

            lock (m_interactions)
            {
                if (m_interactions.ContainsKey(remoteClient.AgentId))
                {
                    m_interactions[remoteClient.AgentId].Close();
                    m_interactions.Remove(remoteClient.AgentId);
                }

                AbstractInteraction interaction = CreateInteraction(remoteClient);

                // Not all clickable buttons implement interactions
                // FIXME: May change this at some stage for simplicity.
                if (interaction != null)
                    m_interactions.Add(remoteClient.AgentId, interaction);
            }            
            
            if (OnClick != null)
            {
                foreach (OnClickDelegate d in OnClick.GetInvocationList())
                {
                    try
                    {
                        d(offsetPos, remoteClient, surfaceArgs);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat(
                            "[WATER WARS]: Delegate for OnOsTouch failed - continuing.  {0}{1}", 
                            e.Message, e.StackTrace);
                    }
                }
            }

//            m_log.InfoFormat(
//                "[OS WIDGETS]: Fired OnTouch() with localID {0}, originalID {1} (part has localID {2}, Text {3})", 
//                localID, originalID, m_part.LocalId, DisplayBehaviour.Text.Replace("\n", @"\n"));            
        }
        
        public virtual void Close()
        {
            DisableEvents();
        }

        protected void DisableEvents()
        {
            m_part.SetScriptEvents(DUMMY_SCRIPT_UUID, (int)scriptEvents.None);
            m_eventManager.OnObjectGrab -= OnOsTouch;
            m_eventManager.OnChatFromClient -= OnOsChat;            
        }
        
        protected void EnableEvents()
        {
            m_part.SetScriptEvents(DUMMY_SCRIPT_UUID, (int)scriptEvents.touch);
            m_eventManager.OnObjectGrab += OnOsTouch;
            m_eventManager.OnChatFromClient += OnOsChat;            
        }

        /// <summary>
        /// Update this button to match the current event listening and display requirements.
        /// </summary>
        /// <remarks>
        /// This is called when some button state is changed or if the underlying virtual environment object changes
        /// and needs to be resynchronized.
        /// </remarks>
        protected void Update()
        {
            if (m_enabled)
            {
                EnableEvents();
            }
            else
            {
                DisableEvents();
            }

            DisplayBehaviour.UpdateAppearance();
            LabelBehaviour.UpdateAppearance();            
        }

        /// <summary>
        /// Override this if your button is involved in any dialog interactions.
        /// </summary>
        /// <param name="CreateInteraction"></param>
        protected virtual AbstractInteraction CreateInteraction(IClientAPI remoteClient) { return null; }

        /// <summary>
        /// Change the texture colour of the entire button.
        /// </summary>
        /// 
        /// It is left to the caller to initiate an update of the part.
        /// 
        /// <param name="newColor"></param>
        public void ChangeTextureColor(Color4 newColor)
        {
            Part.ParentGroup.ForEachPart(
                delegate(SceneObjectPart part)
                {
                    Primitive.TextureEntry textures = part.Shape.Textures;
                
                    if (textures.DefaultTexture != null)
                        textures.DefaultTexture.RGBA = newColor;
                
                    if (textures.FaceTextures != null)
                    {                    
                        foreach (Primitive.TextureEntryFace texture in textures.FaceTextures)
                        {
                            if (texture != null)
                                texture.RGBA = newColor;
                        }      
                    }
                
                    // Tediously we have to do this to re-serialize the data into the underlying byte[] which is 
                    // actually used.
                    part.Shape.Textures = textures;
                });            
        }
        
        /// <summary>
        /// Send an alert to everybody in the region
        /// </summary>
        /// <param name="text"></param>
        public void SendAlert(string text)
        {
            DialogModule.SendGeneralAlert(text);
        }
        
        /// <summary>
        /// Send an alert to a particular user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="text"></param>
        public void SendAlert(UUID userId, string text)
        {
            DialogModule.SendAlertToUser(userId, text);
        }     
        
        /// <summary>
        /// Send an alert to a particular user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="textFormat"></param>
        /// <param name="textParams"></param>
        public void SendAlert(UUID userId, string textFormat, params object[] textParams)
        {
            DialogModule.SendAlertToUser(userId, string.Format(textFormat, textParams));
        }          

        /// <summary>
        /// Send a dialog to the user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="text"></param>
        /// <param name="options"></param>
        public void SendDialog(UUID userId, string text, string[] options)
        {
            DialogModule.SendDialogToUser(
                userId, Part.Name, Part.UUID, Part.OwnerID,
                text,
                new UUID("00000000-0000-2222-3333-100000001000"),
                Channel, options);            
        }          
    }
}
