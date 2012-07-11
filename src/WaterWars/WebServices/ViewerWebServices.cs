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
using System.IO;
using System.Reflection;
using log4net;
using Newtonsoft.Json;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using WaterWars;
using WaterWars.Models;

namespace WaterWars.WebServices
{
    /// <summary>
    /// Web services available to supplement the in-world viewer through shared media (2.0) or the media browser (1.x)
    /// </summary>
    public class ViewerWebServices : AbstractServices
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        protected BuyPointServices m_buyPointServices;
        public PlayerServices PlayerServices { get; set; }
        
        public ViewerWebServices(WaterWarsController controller) : base(controller)
        {
            m_buyPointServices = new BuyPointServices(m_controller);
            PlayerServices = new PlayerServices(m_controller);
            
            Initialise();
        }
        
        public void Initialise()
        {
            IHttpServer httpServer = MainServer.Instance;
            if (httpServer != null)
            {
                // We want to handle anything that occurs in the Waters Wars namespace
                httpServer.AddHTTPHandler(WaterWarsConstants.WEB_SERVICE_PREFIX, HandleRequest);
            }
        }

        public void Close()
        {
            // Not going to bother yet since there's no way to dynamically load/unload region modules anyway
            //MainServer.Instance.RemoveHTTPHandler(...);
        }

        public Hashtable HandleRequest(Hashtable request)
        {
            Hashtable reply = null;
            string requestUri = null;
            
            try
            {
//                  m_log.InfoFormat("[WATER WARS]: Uri requested [{0}]", request["uri"]);
               
//                string body = (string)request["body"];
//                if (!((string)request["uri"]).EndsWith(PlayerServices.UPDATE_PATH))
//                {
//                    m_log.InfoFormat(
//                        "[WATER WARS]: Uri requested [{0}], data [{1}]", 
//                        request["uri"], (body == null ? null : body.Replace('\n', '|')));
//                }
    
                /*
                if (body != null && body != string.Empty)
                {
                    string[] lines = body.Split(new char[] { '\n' });
                    foreach (string line in lines)
                        m_log.InfoFormat("[WATER WARS]: {0}", line);
                }
                */
                
                requestUri = ((string)request["uri"]).Substring(WaterWarsConstants.WEB_SERVICE_PREFIX.Length);                
    
                if (requestUri.StartsWith(PlayerServices.LOGIN_PATH))
                {
                    reply = PlayerServices.HandleRequest(requestUri, request);
                }
                if (requestUri.StartsWith(PlayerServices.PLAYER_PATH))
                {
                    reply = PlayerServices.HandleRequest(requestUri, request);
                }
                else if (requestUri.StartsWith(BuyPointServices.BUY_POINT_PATH))
                {
                    reply = m_buyPointServices.HandleRequest(requestUri, request);
                }
            }
            catch (NotImplementedException)
            {
                reply = new Hashtable();
                reply["int_response_code"] = 403;
                reply["str_response_string"] = "";                
            }
            catch (Exception e)
            {
                m_log.Error(
                    string.Format(
                        "[WATER WARS]: Caught exception while servicing webservice request for {0}", 
                        request["uri"]), e);

                reply = new Hashtable();
                reply["int_response_code"] = 501;
                reply["str_response_string"] = "";
            }

            if (null == reply)
            {
                reply = new Hashtable();
                reply["int_response_code"] = 404;
                reply["str_response_string"] = string.Empty;
                reply["content_type"] = "text/plain";              
                m_log.WarnFormat("[WATER WARS]: Received unrecognized request for {0}", requestUri);
            }

//            m_log.InfoFormat(
//                "[WATER WARS]: Returning response code [{0}], data [{1}]", 
//                reply["int_response_code"], reply["str_response_string"]);

            return reply;
        }
    }
}