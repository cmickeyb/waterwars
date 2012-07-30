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

using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;

namespace WaterWars.Views.Widgets.Behaviours
{        
    public class BlankableDynamicTextureBehaviour : IDisplayBehaviour
    {
        public OsButton Button { get; set; }
        
        public string Text 
        { 
            get { return m_text; } 
            set { m_text = value; EnabledTexture = UUID.Zero; }
        }
        protected string m_text = string.Empty;

        public UUID EnabledTexture { get; set; }        
        public UUID DisabledTexture { get; set; }
        
        protected Scene m_scene;
        protected IDynamicTextureManager m_textureManager;        
        
        public BlankableDynamicTextureBehaviour(Scene scene)
        {
            m_scene = scene;
            m_textureManager = scene.RequestModuleInterface<IDynamicTextureManager>();
            EnabledTexture = UUID.Zero;
            DisabledTexture = Util.BLANK_TEXTURE_UUID;
        }
        
        public void UpdateAppearance()
        {
            if (Button.Enabled)
            {
                if (EnabledTexture == UUID.Zero)
                {
                    string drawList = "MoveTo 30, 40;";
                    drawList += "FontSize 48;";
                    drawList += "Text " + Text + ";";
                            
                    m_textureManager.AddDynamicTextureData(
                        m_scene.RegionInfo.RegionID, Button.Part.UUID, "vector", drawList, "256", 0, false, 0, 255, -1);
//                        m_textureManager.AddDynamicTextureData(
//                            m_scene.RegionInfo.RegionID, m_part.UUID, "vector", drawList, "256", 0, false, 255);                        
                    EnabledTexture = Button.Part.Shape.Textures.DefaultTexture.TextureID;
                }
                else
                {
                    OpenMetaverse.Primitive.TextureEntry tex = Button.Part.Shape.Textures;
                    for (uint i = 0; i < Button.Part.GetNumberOfSides(); i++)
                    {
                        if (tex.FaceTextures[i] != null)
                        {
                            tex.FaceTextures[i].TextureID = EnabledTexture;
                        }
                    }
                    tex.DefaultTexture.TextureID = EnabledTexture;
                    Button.Part.UpdateTextureEntry(tex);
                }
            }
            else
            {
                OpenMetaverse.Primitive.TextureEntry tex = Button.Part.Shape.Textures;
                for (uint i = 0; i < Button.Part.GetNumberOfSides(); i++)
                {
                    if (tex.FaceTextures[i] != null)
                    {
                        tex.FaceTextures[i].TextureID = DisabledTexture;
                    }
                }
                tex.DefaultTexture.TextureID = DisabledTexture;
                Button.Part.UpdateTextureEntry(tex);
            }            
        }        
    }
}