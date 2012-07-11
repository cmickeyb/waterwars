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
using System.Text;
using Newtonsoft.Json;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using OpenMetaverse;
using OpenSim.Region.CoreModules.World.Land;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Tests.Common;
using WaterWars.Models;
using WaterWars.Models.Roles;
using WaterWars.Rules;
using WaterWars.States;
using WaterWars.Tests;
using WaterWars.WebServices;

namespace WaterWars.WebServices.Tests
{        
    [TestFixture]
    public class AbstractWebServiceTests : AbstractGameTests
    {
        protected Hashtable SubmitRequest(string uri, string method)
        {
            return SubmitRequest(uri, method, null);          
        }

        protected Hashtable SubmitRequest(string uri, string method, string json)
        {
            return SubmitRequest(uri, method, json, 200);
        }
        
        protected Hashtable SubmitRequest(string uri, string method, string json, int expectedResponseCode)
        {
            Hashtable request = new Hashtable();
            request["uri"] = uri;
            request["_method"] = method;

            if (json != null)
                EncodeMultipartForm(request, json);

            Hashtable response = m_controller.ViewerWebServices.HandleRequest(request);
            int responseCode = (int)response["int_response_code"];

            Assert.That(responseCode, Is.EqualTo(expectedResponseCode));

            return response;
        }                                

        protected static void EncodeMultipartForm(Hashtable request, string json)
        {
            StringBuilder sb = new StringBuilder();
            
            string formDataBoundary = "-----------------------------12345678901234";
            request["content-type"] = "multipart/form-data; boundary=" + formDataBoundary;

            string postData 
                = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"\r\n\r\n{2}\r\n",
                    formDataBoundary, "json", json);
            sb.Append(postData);
 
            string footer = "\r\n--" + formDataBoundary + "--\r\n";
            sb.Append(footer);

            request["body"] = sb.ToString();
        }
    }
}
