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
using WaterWars.Views.Widgets;

namespace WaterWars.Views.Widgets.Behaviours
{        
    public class FadeInAndOutBehaviour : IDisplayBehaviour
    {        
        public OsButton Button { get; set; }

        /// <value>
        /// Don't do anything with text
        /// </value>
        public string Text { get; set; }
        
        public UUID EnabledTexture { get; set; }        
        public UUID DisabledTexture { get; set; }
        
        public void UpdateAppearance() 
        {
            Color4 color;
            
            if (Button.Enabled)
            {
                OpenMetaverse.Primitive.TextureEntry tex = Button.Part.Shape.Textures;
                for (uint i = 0; i < Button.Part.GetNumberOfSides(); i++)
                {
                    if (tex.FaceTextures[i] != null)
                    {
                        color = tex.FaceTextures[i].RGBA;
                        color.A = 0.9f;
                        tex.FaceTextures[i].RGBA = color;
                    }
                }
                
                color = tex.DefaultTexture.RGBA;
                color.A = 0.9f;
                tex.DefaultTexture.RGBA = color;
                
                Button.Part.UpdateTexture(tex);                
            }
            else
            {
                OpenMetaverse.Primitive.TextureEntry tex = Button.Part.Shape.Textures;
                for (uint i = 0; i < Button.Part.GetNumberOfSides(); i++)
                {
                    if (tex.FaceTextures[i] != null)
                    {
                        color = tex.FaceTextures[i].RGBA;
                        color.A = 0.1f;
                        tex.FaceTextures[i].RGBA = color;
                    }
                }
                
                color = tex.DefaultTexture.RGBA;
                color.A = 0.1f;
                tex.DefaultTexture.RGBA = color;
                
                Button.Part.UpdateTexture(tex);                    
            }
        }
    }
}