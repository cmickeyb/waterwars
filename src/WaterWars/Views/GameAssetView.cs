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
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Framework.Scenes.Serialization;
using WaterWars;
using WaterWars.Models;
using WaterWars.Views.Widgets;
using WaterWars.Views.Widgets.Behaviours;    

namespace WaterWars.Views
{        
    public abstract class GameAssetView : WaterWarsView 
    {
//        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Signals whether an incomplete asset has had a build step this turn
        /// </summary>
        protected StepBuiltDecorator m_stepBuiltDecorator;
        
        /// <summary>
        /// Signals whether water has been allocated
        /// </summary>
        protected WaterAllocationDecorator m_waterAllocationDecorator;
        
        protected GameAssetButton m_button;

        /// <summary>
        /// List of names to use when changing between different scene objects
        /// </summary>
        protected string[,] m_veSceneObjectNames;

        public GameAssetView(WaterWarsController controller, Scene scene, AbstractGameAsset asset, AbstractView itemStoreView)
            : base(controller, scene, itemStoreView)
        {                   
            GameAsset = asset;  
        }

        public AbstractGameAsset GameAsset { get; private set; }

        public override void Initialize(Vector3 pos)
        {
            SceneObjectGroup so 
                = RezSceneObjectFromInventoryItem(
                    m_veSceneObjectNames[GameAsset.Level, 0], GameAsset, GameAsset.OwnerUuid, pos);                                   

            base.Initialize(so);          
            
            // We can only change the name after we've initialized, since the registration code currently relies on
            // the original name.
            so.Name = GameAsset.Name;
            
            Vector3 assetPos = RootPart.ParentGroup.AbsolutePosition;
            float terrainHeight = (float)m_scene.Heightmap[(int)assetPos.X, (int)(assetPos.Y)];
            
            float heightAboveGround = 0;
            if (GameAsset is Crops)
                heightAboveGround = 9;
            else if (GameAsset is Houses)
                heightAboveGround = 11;
            else if (GameAsset is Factory)
                heightAboveGround = 17;                   
            
            m_stepBuiltDecorator 
                = new StepBuiltDecorator(
                    m_controller,
                    m_scene, 
                    GameAsset,         
                    m_itemStoreView, 
                    new Vector3(
                        assetPos.X, assetPos.Y, terrainHeight + heightAboveGround));
            
            m_waterAllocationDecorator 
                = new WaterAllocationDecorator(
                    m_controller,
                    m_scene,
                    GameAsset,
                    m_itemStoreView, 
                    new Vector3(
                        assetPos.X, assetPos.Y, terrainHeight + heightAboveGround));                       
            
            // We can only reposition after we've passed the scene object up to the parent class
            so.AbsolutePosition = FindOnGroundPosition(so);                    

//            Console.Write(
//                string.Format(
//                    "[WATER WARS]: {0}, local ID {1}, textures {2}", 
//                    item.Name, so.RootPart.LocalId, so.RootPart.Shape.Textures));
            
            // As well as indicating with the decorator, change the game asset colour if it isn't actually built yet
            if (!GameAsset.IsBuilt)
            {
                Color4 partialBuildColour = new Color4(200, 200, 0, 255);
                m_button.ChangeTextureColor(partialBuildColour);              
            }
            
            m_controller.EventManager.OnGameAssetBuildCompleted += OnGameAssetBuildCompleted;            
            m_controller.EventManager.OnGameAssetUpgraded += OnGameAssetUpgraded;            
            
            so.ScheduleGroupForFullUpdate();            
                    
            // FIXME: We have to do this manually right now but really it's the responsibilty of OpenSim.
            so.CreateScriptInstances(0, true, m_scene.DefaultScriptEngine, 0);  
        }        
        
        public override void Close()
        {
//            m_log.InfoFormat("[WATER WARS]: Closing GameAssetView {0} {1}", RootPart.Name, RootPart.LocalId);
            
            m_controller.EventManager.OnGameAssetBuildCompleted -= OnGameAssetBuildCompleted; 
            m_controller.EventManager.OnGameAssetUpgraded -= OnGameAssetUpgraded;            
            
            if (m_stepBuiltDecorator != null)
                m_stepBuiltDecorator.Close();
            
            if (m_waterAllocationDecorator != null)
                m_waterAllocationDecorator.Close();
            
            m_button.Close();
            base.Close();
        }        
        
        protected void OnGameAssetBuildCompleted(AbstractGameAsset ga)
        {
            if (ga != GameAsset)
                return;
                        
            if (ga.IsMultiStepBuild)
            {            
                int lastPhaseIndex = m_veSceneObjectNames.GetLength(1) - 1;
                ReplaceSceneObjectFromInventoryItem(
                    m_veSceneObjectNames[GameAsset.Level, lastPhaseIndex], GameAsset, GameAsset.OwnerUuid);                              
                
                m_button.ResetButtonPrims(m_rootPart);
            }
        }        
        
        protected void OnGameAssetUpgraded(AbstractGameAsset ga, int oldLevel)
        {
            if (ga != GameAsset)
                return;  

            int lastPhaseIndex = m_veSceneObjectNames.GetLength(1) - 1;            
            ReplaceSceneObjectFromInventoryItem(
                m_veSceneObjectNames[GameAsset.Level, lastPhaseIndex], GameAsset, GameAsset.OwnerUuid);                              
                
            m_button.ResetButtonPrims(m_rootPart);                                                 
        }
        
        public class GameAssetButton : WaterWarsButton
        {       
            const string CROPS_STATUS_MSG = "Crops\nWater usage: {0}\nRevenue: {1}";
            const string FACTORY_STATUS_MSG = "Factory\nLevel: {0}\nWater usage: {1}\nRevenue: {2}";
            const string HOUSES_STATUS_MSG = "Houses\nLevel: {0}\nWater usage: {1}\nRevenue: {2}";  
            
            protected GameAssetView m_gav;
            
            public GameAssetButton(WaterWarsController controller, SceneObjectPart part, GameAssetView gav) 
                : base(controller, part, new FixedTextureBehaviour())
            {     
                m_gav = gav;
                
                Enabled = true;
                OnClick 
                    += delegate(Vector3 offsetPos, IClientAPI remoteClient, SurfaceTouchEventArgs surfaceArgs)
                        { Util.FireAndForget(
                            delegate { 
                                m_controller.ViewerWebServices.PlayerServices.SetLastSelected(
                                    remoteClient.AgentId, m_gav.GameAsset); }); };                 
            
                m_controller.EventManager.OnGameAssetBuildStarted += OnGameAssetBuildStarted;
                m_controller.EventManager.OnGameAssetBuildCompleted += OnGameAssetBuildCompleted;
                m_controller.EventManager.OnGameAssetUpgraded += OnGameAssetUpgraded;
                m_controller.EventManager.OnGameAssetSoldToEconomy += OnGameAssetSoldToEconomy;                
            } 
            
            protected void OnGameAssetBuildStarted(AbstractGameAsset ga)
            {
                if (ga != m_gav.GameAsset)
                    return;  
                
                LabelBehaviour.Text = GetGameAssetStatusMessage();        
            }   
            
            protected void OnGameAssetUpgraded(AbstractGameAsset ga, int oldLevel)
            {
                if (ga != m_gav.GameAsset)
                    return;  
                
                LabelBehaviour.Text = GetGameAssetStatusMessage();                                        
            }              
            
            protected string GetGameAssetStatusMessage()
            {
                string message = "ERROR";
                AbstractGameAsset ga = m_gav.GameAsset;
                
                switch (ga.Type)
                {
                    case AbstractGameAssetType.Crops:
                        message = string.Format(CROPS_STATUS_MSG, ga.WaterUsage, ga.NormalRevenue);
                        break;
                    case AbstractGameAssetType.Houses:
                        message = string.Format(HOUSES_STATUS_MSG, ga.Level, ga.WaterUsage, ga.NormalRevenue);
                        break;
                    case AbstractGameAssetType.Factory:
                        message = string.Format(FACTORY_STATUS_MSG, ga.Level, ga.WaterUsage, ga.NormalRevenue);
                        break;
                }
    
                return message;
            }            
                
            protected void OnGameAssetBuildCompleted(AbstractGameAsset ga)
            {
                if (ga != m_gav.GameAsset)
                    return;
                
                Color4 neutralColour = new Color4(255, 255, 255, 255);
                ChangeTextureColor(neutralColour);            
                Part.ParentGroup.ScheduleGroupForFullUpdate();            
                
                LabelBehaviour.Text = GetGameAssetStatusMessage();
            }
                    
            protected void OnGameAssetSoldToEconomy(AbstractGameAsset ga, Player owner, int price)
            {
                if (ga != m_gav.GameAsset)
                    return;
                
                Color4 soldColour = new Color4(32, 44, 66, 255);
                
                Part.ParentGroup.OwnerID = m_controller.Game.Economy.Uuid;
                ChangeTextureColor(soldColour);            
                Part.ParentGroup.ScheduleGroupForFullUpdate();
            }    
            
            public override void Close()
            {                                            
                m_controller.EventManager.OnGameAssetBuildStarted -= OnGameAssetBuildStarted;
                m_controller.EventManager.OnGameAssetBuildCompleted -= OnGameAssetBuildCompleted;
                m_controller.EventManager.OnGameAssetUpgraded -= OnGameAssetUpgraded;
                m_controller.EventManager.OnGameAssetSoldToEconomy -= OnGameAssetSoldToEconomy;            
            }
        }               
    }
    
    /// <summary>
    /// Show when a step has been built on the game asset but the build is not yet complete
    /// </summary>
    public class StepBuiltDecorator : WaterWarsDecorator
    {
        public const string IN_WORLD_NAME = "Step_Built_This_Turn_Icon";
        
        public AbstractGameAsset GameAsset { get; private set; }
        
        protected WaterWarsButton m_button;
        
        public StepBuiltDecorator(
            WaterWarsController controller, Scene scene, AbstractGameAsset asset, AbstractView itemStoreView, Vector3 positionToDisplay)
            : base(controller, scene, itemStoreView, IN_WORLD_NAME, positionToDisplay) 
        {
            GameAsset = asset;
            Update();
            GameAsset.OnChange += GameAssetChanged;             
        }
        
        public void GameAssetChanged(AbstractModel model)
        {
            Update();
        }
        
        public void Update()
        {
            IsDisplayed = GameAsset.StepBuiltThisTurn && !GameAsset.IsBuilt;  
        }
        
        protected override void Display()
        {
            base.Display();
            m_button = new WaterWarsButton(m_controller, RootPart, new FixedTextureBehaviour());
            m_button.LabelBehaviour.Text 
                = string.Format("Building step {0} of {1}", GameAsset.StepsBuilt, GameAsset.StepsToBuild);
        }   
        
        protected override void NoDisplay()
        {
            CloseAndRemoveButton();
            base.NoDisplay();
        }
        
        public override void Close()
        {
            GameAsset.OnChange -= GameAssetChanged;
            CloseAndRemoveButton();
            base.Close();
        }        
        
        protected void CloseAndRemoveButton()
        {
            if (m_button != null)
            {
                m_button.Close();
                m_button = null;            
            }
        }            
    }       
    
    /// <summary>
    /// Show when water is allocated
    /// </summary>
    public class WaterAllocationDecorator : WaterWarsDecorator
    {
        public const string IN_WORLD_NAME = "Water_Allocated_Icon";
        
        public AbstractGameAsset GameAsset { get; private set; }
        
        protected WaterWarsButton m_button;
        
        public WaterAllocationDecorator(
            WaterWarsController controller, Scene scene, AbstractGameAsset asset, AbstractView itemStoreView, Vector3 positionToDisplay)
            : base(controller, scene, itemStoreView, IN_WORLD_NAME, positionToDisplay) 
        {
            GameAsset = asset;
            Update();
            GameAsset.OnChange += GameAssetChanged;            
        }
        
        public void GameAssetChanged(AbstractModel model)
        {
            Update();
        }
        
        public void Update()
        {
            IsDisplayed = GameAsset.WaterAllocated > 0;       
        }        
        
        protected override void Display()
        {
            base.Display();
            m_button = new WaterWarsButton(m_controller, RootPart, new FixedTextureBehaviour());
            m_button.LabelBehaviour.Text = "Water Allocated";
        }   
        
        protected override void NoDisplay()
        {
            CloseAndRemoveButton();
            base.NoDisplay();
        }
        
        public override void Close()
        {
            GameAsset.OnChange -= GameAssetChanged;            
            CloseAndRemoveButton();
            base.Close();
        }        
        
        protected void CloseAndRemoveButton()
        {
            if (m_button != null)
            {
                m_button.Close();
                m_button = null;            
            }
        }            
    }    
}