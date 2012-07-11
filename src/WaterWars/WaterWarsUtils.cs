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
using System.Globalization;
using System.Reflection;
using System.Text;
using log4net;
using OpenMetaverse;
using OpenSim.Region.Framework.Interfaces;

namespace WaterWars
{    
    /// <summary>
    /// Utility methods
    /// </summary>
    public static class WaterWarsUtils
    {
//        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        public static Random Random = new Random();
        
        private static readonly DateTime s_unixEpoch =
            DateTime.ParseExact("1970-01-01 00:00:00 +0", "yyyy-MM-dd hh:mm:ss z", DateTimeFormatInfo.InvariantInfo).ToUniversalTime();        
        
        /// <summary>
        /// Return a string uuid such that we throw an exception if it doesn't parse
        /// </summary>
        /// <param name="rawId"></param>
        /// <returns></returns>
        public static UUID ParseRawId(string rawId)
        {
            UUID uuid;
            if (!UUID.TryParse(rawId, out uuid))
                throw new Exception(String.Format("Could not parse raw id [{0}]", rawId));

            return uuid;
        }

        /// <summary>
        /// Finds sw and ne parcel corner points assuming a square parcel.
        /// </summary>
        /// Height values are not filled in.
        /// 
        /// <param name="lo"></param>
        /// <param name="p1">sw corner</param>
        /// <param name="p2">ne corner</param>
        public static void FindSquareParcelCorners(ILandObject lo, out Vector3 p1, out Vector3 p2)
        {
            bool[,] landBitmap = lo.LandBitmap;
            int x1 = 0, x2 = 63, y1 = 0, y2 = 63;
            int lengthX = landBitmap.GetLength(0);
            int lengthY = landBitmap.GetLength(1);
            bool foundXY1 = false, foundX2 = false, foundY2 = false;

//            List<string> debugParcelLines = new List<string>();
//            for (int y = lengthY - 1; y >= 0; y--)
//            {
//                StringBuilder sb = new StringBuilder();
//                
//                for (int x = 0; x < lengthX; x++)
//                {
//                    if (landBitmap[x, y])
//                        sb.Append("*");
//                    else
//                        sb.Append("-");
//                }
//
//                debugParcelLines.Add(sb.ToString());
//            }
//
//            foreach (string line in debugParcelLines)
//                m_log.InfoFormat("[WATER WARS]: {0}", line);
            
            for (int y = 0; y < lengthY; y++)
            {
                if (!foundXY1)
                {
                    for (int x = 0; x < lengthX; x++)
                    {
//                        m_log.InfoFormat("[WATER WARS]: Land bitmap at point ({0},{1}) for {2} is {3}", x, y, m_bp.Parcel.LandData.Name, landBitmap[x, y]);
                        if (!foundXY1 && landBitmap[x, y])
                        {
                            x1 = x;
                            y1 = y;
                            foundXY1 = true;
                        }
    
                        if (foundXY1 && !foundX2 && !landBitmap[x, y])
                        {
                            x2 = x;
                            foundX2 = true;
                        }
                    }
                }
                else if (!landBitmap[x1, y] && !foundY2)
                {
                    y2 = y;
                    foundY2 = true;
                }
            }                        

            // Each land object occupies 4m so we have to multiply to get the 1m-spaced terrain points.
            x1 *= 4;
            y1 *= 4;
            x2 *= 4;
            y2 *= 4;

//            m_log.InfoFormat("[WATER WARS]: Found terrain co-ords (({0},{1}),({2},{3}))", x1, y1, x2, y2);

            p1 = new Vector3(x1, y1, 0);
            p2 = new Vector3(x2, y2, 0);
        }
                
        public static long ToUnixTime(DateTime stamp)
        {
            TimeSpan t = stamp.ToUniversalTime() - s_unixEpoch;
            return (long)Math.Floor(t.TotalSeconds);
        }        
        
        /// <summary>
        /// Get string for given amount with appropriate money units symbol attached
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        public static string GetMoneyUnitsText(int amount)
        {
            return string.Format("{0}{1}", WaterWarsConstants.MONEY_UNIT, amount);
        }        
        
        /// <summary>
        /// Get string for given amount with appropriate water units symbol attached
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        public static string GetWaterUnitsText(int amount)
        {
            return string.Format("{0} {1}", amount, WaterWarsConstants.WATER_UNIT);
        }
    }
}