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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading;
using System.Xml;
using HttpServer;
using HttpServer.FormDecoders;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using WaterWars;
using WaterWars.Models;

namespace WaterWars.WebServices
{        
    public abstract class AbstractServices
    {
        protected WaterWarsController m_controller; 
        
        public AbstractServices(WaterWarsController controller)
        {
            m_controller = controller;
        }        
                
        protected JObject GetJsonFromPost(Hashtable request)
        {
            HttpForm form = DecodePost(request);
            string rawJson = (string)form["json"].Value;
            // FIXME: Nasty way of unescaping the \"
            rawJson = rawJson.Replace(@"\", "");            
            return JObject.Parse(rawJson);           
        }      
        
        protected HttpForm DecodePost(Hashtable request)
        {
//            Hashtable form = (Hashtable)request["form"];
//            m_log.InfoFormat("[WATER WARS]: Got {0} items in form", form.Count);
//            foreach (object key in form)
//            {
//                DictionaryEntry tuple = (DictionaryEntry)key;
//                m_log.InfoFormat("[WATER WARS]: Form tuple {0}={1}", tuple.Key, tuple.Value);
//            }

            IFormDecoder formDecoder = new MultipartDecoder();
            HttpForm form2 
                = formDecoder.Decode(
                    new MemoryStream(Encoding.UTF8.GetBytes((string)request["body"])), 
                    (string)request["content-type"], 
                    Encoding.UTF8);
//            m_log.InfoFormat("[WATER WARS]: Got {0} items in form2", form2.Count);
//            foreach (object key in form2)
//            {
//                DictionaryEntry tuple = (DictionaryEntry)key;
//                m_log.InfoFormat("[WATER WARS]: Form2 tuple {0}={1}", tuple.Key, tuple.Value);
//            }            

//            foreach (HttpInputItem item in form2)
//            {
//                m_log.InfoFormat("[WATER WARS]: Got form item {0}={1}", item.Name, item.Value);
//            }

            return form2;
        }        
    }
}