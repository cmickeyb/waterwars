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
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
//using NHibernate;
//using NHibernate.Cfg;
//using NHibernate.Tool.hbm2ddl;
using WaterWars;
using WaterWars.Models;

namespace WaterWars.Persistence
{
    /// <summary>
    /// Handle persistent store duties
    /// Currently largely commented out since I don't want a dependency on NHibernate
    /// </summary>
    public class Persister
    {
        protected WaterWarsController m_controller;
        
//        /// <summary>
//        /// Should only be used externally by tests
//        /// </summary>
//        public ISessionFactory m_sessionFactory;
        
        public void Initialize(WaterWarsController controller)
        {
            m_controller = controller;
//            
//            var cfg = new Configuration();
//            cfg.Configure();
//            cfg.AddAssembly(typeof (Houses).Assembly);             
//            
//            // This defeats the whole point of persistence at the moment.  Need to do this conditionally depending
//            // on whether the table already exists
//            new SchemaExport(cfg).Create(true, true);
//            
//            m_sessionFactory = cfg.BuildSessionFactory();
//            
//            if (m_controller.Scenes.Count > 0)
//                m_controller.Scenes[0].EventManager.OnShutdown += Close;
//            
//            m_controller.EventManager.OnSystemInitialized += SystemInitialized;
//            m_controller.EventManager.OnBuyPointRegistered += Save;
//            m_controller.EventManager.OnStateChanged += StateChanged;
//                        
//            m_controller.EventManager.OnPlayerAdded += Save;
//                        
//            m_controller.EventManager.OnGameStarted += UpdateGameBuyPointsAndPlayers;
//            m_controller.EventManager.OnGameReset += ResetGame;
//            m_controller.EventManager.OnWaterStageStarted += WaterStageStarted;
//            m_controller.EventManager.OnRevenueStageEnded += RevenueStageEnded;            
//            
//            m_controller.EventManager.OnBuyPointNameChanged += Update;
//            m_controller.EventManager.OnLandRightsBought += LandRightsBought;
//            m_controller.EventManager.OnLandRightsSold += LandRightsSold;            
//            m_controller.EventManager.OnGameAssetBought += GameAssetBought;
//            m_controller.EventManager.OnGameAssetSold += Delete;            
//            m_controller.EventManager.OnGameAssetUpgraded += UpgradeGameAsset;
//            
//            m_controller.EventManager.OnWaterUsed += WaterUsed;
//            m_controller.EventManager.OnWaterSold += WaterSold;
        }        

//        public void Save(Object o)
//        {
//            using (ISession session = m_sessionFactory.OpenSession())
//            {            
//                using (session.BeginTransaction())
//                {
//                    session.Save(o);
//                    session.Transaction.Commit();
//                }
//            }            
//        }
//        
//        public void Delete(Object o)
//        {
//            using (ISession session = m_sessionFactory.OpenSession())
//            {            
//                using (session.BeginTransaction())
//                {
//                    session.Delete(o);
//                    session.Transaction.Commit();
//                }
//            }              
//        }
//        
//        public void Update(AbstractModel am)
//        {
//            using (ISession session = m_sessionFactory.OpenSession())
//            {            
//                using (session.BeginTransaction())
//                {
//                    session.Update(am, am.Uuid);
//                    session.Transaction.Commit();
//                }
//            }              
//        }        
//        
//        protected void SystemInitialized()
//        {
//            using (ISession session = m_sessionFactory.OpenSession())
//            {            
//                using (session.BeginTransaction())
//                {
//                    Save(m_controller.Game);
//                    session.Transaction.Commit();
//                }
//            }                          
//        }
//        
//        /// <summary>
//        /// We handle game state updates in this event if the state isn't doing anything else that needs more objects
//        /// to be done in a single transaction.
//        /// </summary>
//        /// 
//        /// This probably isn't a good idea.
//        /// 
//        /// <param name="state"></param>
//        protected void StateChanged(GameStateType state)
//        {
//            if (state == GameStateType.Registration
//                || state == GameStateType.Build
//                || state == GameStateType.Game_Ended)
//                Update(m_controller.Game);
//            
//            if (state == GameStateType.Build)
//            {            
//                using (
//                    FileStream fs 
//                        = new FileStream(Path.Combine(WaterWarsConstants.STATE_PATH, WaterWarsConstants.STATE_FILE_NAME), FileMode.Create))
//                {
//                    BinaryFormatter bf = new BinaryFormatter();
//                    bf.Serialize(fs, m_controller.Game);
//                }                
//            }
//        }
//        
//        protected void UpdateGameBuyPointsAndPlayers()
//        {
//            using (ISession session = m_sessionFactory.OpenSession())
//            {            
//                using (session.BeginTransaction())
//                {
//                    session.Update(m_controller.Game, m_controller.Game.Uuid);
//                    
//                    lock (m_controller.Game.Players)
//                    {                    
//                        foreach (Player player in m_controller.Game.Players.Values)
//                            session.Update(player, player.Uuid);                            
//                    }
//                    
//                    lock (m_controller.Game.BuyPoints)
//                    {
//                        foreach (BuyPoint bp in m_controller.Game.BuyPoints.Values)
//                            session.Update(bp, bp.Uuid);
//                    }
//                    
//                    session.Transaction.Commit();
//                }
//            }              
//        }
//        
//        public void WaterStageStarted()
//        {
//            UpdateGameBuyPointsAndPlayers();           
//        }
//        
//        public void RevenueStageEnded(List<AbstractGameAsset> assetsRemoved)
//        {
//            // TODO: Update buypoints
//            
//            using (ISession session = m_sessionFactory.OpenSession())
//            {            
//                using (session.BeginTransaction())
//                {                    
//                    session.Update(m_controller.Game, m_controller.Game.Uuid);
//                    
//                    // All game assets and players change
//                    lock (m_controller.Game.BuyPoints)
//                        foreach (BuyPoint bp in m_controller.Game.BuyPoints.Values)
//                            foreach (AbstractGameAsset ga in bp.GameAssets.Values)
//                                session.Update(ga, ga.Uuid);
//                    
//                    lock (m_controller.Game.Players)
//                        foreach (Player p in m_controller.Game.Players.Values)
//                            session.Update(p, p.Uuid);
//                    
//                    // Some game assets are aged away
//                    foreach (AbstractGameAsset asset in assetsRemoved)
//                        session.Delete(asset);
//
//                    session.Transaction.Commit();
//                }
//            }
//        }        
//
//        public void ResetGame()
//        {
//            using (ISession session = m_sessionFactory.OpenSession())
//            {            
//                using (session.BeginTransaction())
//                {
//                    session.Update(m_controller.Game, m_controller.Game.Uuid);
//                    
//                    // Delete all players, all game assets
//                    session.Delete("from AbstractGameAsset");
//                    session.Delete("from Player");
//                    session.Transaction.Commit();
//                }
//            }
//        }
//        
//        public void LandRightsBought(BuyPoint bp, Player p)
//        {
//            using (ISession session = m_sessionFactory.OpenSession())
//            {            
//                using (session.BeginTransaction())
//                {
//                    // TODO: update buypoint
//                    session.Update(p, p.Uuid);
//                    session.Transaction.Commit();
//                }
//            }            
//        }
//        
//        public void LandRightsSold(BuyPoint bp, Player buyer, Player seller)
//        {
//            using (ISession session = m_sessionFactory.OpenSession())
//            {            
//                using (session.BeginTransaction())
//                {
//                    // TODO: update buypoint
//                    session.Update(buyer, buyer.Uuid);
//                    session.Update(seller, seller.Uuid);
//                    session.Transaction.Commit();
//                }
//            }            
//        }        
//        
//        public void GameAssetBought(AbstractGameAsset ga)
//        {
//            using (ISession session = m_sessionFactory.OpenSession())
//            {            
//                using (session.BeginTransaction())
//                {
//                    session.Save(ga); 
//                    session.Update(ga.Field.BuyPoint, ga.Field.BuyPoint.Uuid);
//                    session.Update(ga.Field.Owner, ga.Field.Owner.Uuid);                    
//                    session.Transaction.Commit();
//                }
//            }                 
//        }
//        
//        public void UpgradeGameAsset(AbstractGameAsset ga, Player upgrader)
//        {
//            using (ISession session = m_sessionFactory.OpenSession())
//            {            
//                using (session.BeginTransaction())
//                {
//                    session.Update(ga, ga.Uuid);                  
//                    session.Update(upgrader, upgrader.Uuid);                    
//                    session.Transaction.Commit();
//                }
//            }                 
//        }
//        
//        public void WaterUsed(AbstractGameAsset ga, Player user, int amount)
//        {
//            // We don't need to update the user because water records are kept on the buy points
//        }
//        
//        public void WaterSold(BuyPoint bp, Player buyer, Player seller, int amount, int price)
//        {
//            using (ISession session = m_sessionFactory.OpenSession())
//            {            
//                using (session.BeginTransaction())
//                {
//                    // TODO: update buypoint
//                    session.Update(buyer, buyer.Uuid);
//                    session.Update(seller, seller.Uuid);
//                    session.Transaction.Commit();
//                }
//            }            
//        } 
        
        public void Close()
        {
//            m_controller.Scenes[0].EventManager.OnShutdown -= Close;
//            
//            m_controller.EventManager.OnSystemInitialized -= SystemInitialized;
//            m_controller.EventManager.OnBuyPointRegistered -= Save;
//            m_controller.EventManager.OnStateChanged -= StateChanged;
//            
//            m_controller.EventManager.OnPlayerAdded -= Save;
//                        
//            m_controller.EventManager.OnGameStarted -= UpdateGameBuyPointsAndPlayers;
//            m_controller.EventManager.OnGameReset -= ResetGame;
//            m_controller.EventManager.OnWaterStageStarted -= WaterStageStarted;                      
//            m_controller.EventManager.OnRevenueStageEnded -= RevenueStageEnded;            
//            
//            m_controller.EventManager.OnBuyPointNameChanged -= Update;            
//            m_controller.EventManager.OnLandRightsBought -= LandRightsBought;
//            m_controller.EventManager.OnLandRightsSold -= LandRightsSold;            
//            m_controller.EventManager.OnGameAssetBought -= GameAssetBought;
//            m_controller.EventManager.OnGameAssetSold -= Delete;            
//            m_controller.EventManager.OnGameAssetUpgraded -= UpgradeGameAsset;
//            
//            m_controller.EventManager.OnWaterUsed -= WaterUsed;
//            m_controller.EventManager.OnWaterSold -= WaterSold;
//            
//            m_sessionFactory.Close();
        }
    }
}