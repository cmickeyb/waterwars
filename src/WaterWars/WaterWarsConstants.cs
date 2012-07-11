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
using OpenMetaverse;

namespace WaterWars
{
    public class WaterWarsConstants
    {        
        public static bool IS_WEBSERVICE_SECURITY_ON = false;
        
        /// <summary>
        /// Unit used for ui money displays
        /// </summary>
        public const string MONEY_UNIT = "$";

        /// <summary>
        /// Unit used for ui water displays
        /// </summary>        
        public const string WATER_UNIT = "wu";
        
        /// <value>
        /// The name of the registration notecard that signals that an object is a buy point.
        /// </value>
        public const string REGISTRATION_INI_NAME = "Registration Configuration";         

        /// <summary>
        /// Used to route in-world script messages to this module
        /// </summary>
        public const string MODULE_NAME = "WaterWars";

        /// <summary>
        /// Path in which to store state files.
        /// </summary>
        public const string STATE_PATH = "state";
        
        /// <summary>
        /// File in which state is stored.
        /// </summary>
        public const string STATE_FILE_NAME = "game.dat";
        
        /// <summary>
        /// Directory in which the game record is stored
        /// </summary>
        public const string RECORDER_DIR = "recorder";
        
        /// <summary>
        /// The prefix for any game records
        /// </summary>
        public const string RECORD_PREFIX = "waterwars-game-record";
        
        /// <summary>
        /// The prefix for all calls made to the Water Wars web service
        /// </summary>
        public const string WEB_SERVICE_PREFIX = "/waterwars/";
        
        /// <summary>
        /// First name of the dummy user that acts for the system.
        /// </summary>
        public const string SYSTEM_PLAYER_FIRST_NAME = "Water";

        /// <summary>
        /// Last name of the dummy user that acts for the system.
        /// </summary>        
        public const string SYSTEM_PLAYER_LAST_NAME = "Wars";
       
        /// <summary>
        /// First name of the dummy user that represents the economy.
        /// </summary>
        public const string ECONOMY_PLAYER_FIRST_NAME = "Water";
        
        /// <summary>
        /// First name of the dummy user that represents the economy.
        /// </summary>
        public const string ECONOMY_PLAYER_LAST_NAME = "Wars";       

        /// <summary>
        /// The group to which players will belong in-world
        /// </summary>
        public const string GROUP_NAME = "Water Wars Players";
        
        /// <summary>
        /// The group to which players will belong in-world
        /// </summary>
        public const string ADMIN_GROUP_NAME = "Water Wars Administrators";        
        
        /// <summary>
        /// The name under which the system will send messages to groups and in-region chat.
        /// </summary>
        public const string SYSTEM_ANNOUNCEMENT_NAME = "Water Wars";
                
        /// <summary>
        /// Should we enable groups checks at all?  If disabled, then all checks return true and other calls don't
        /// do anything.
        /// </summary>
        public static bool ENABLE_GROUPS = true;
        
        /// <summary>
        /// Should we enforce administrator checks?
        /// </summary>
        public static bool CHECK_ADMIN = true;
    }
}