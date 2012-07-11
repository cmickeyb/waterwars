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
using System.Text;
using log4net;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Scenes;
using WaterWars;
using WaterWars.Models;
using WaterWars.States;
using WaterWars.Views.Interactions;
using WaterWars.Views.Widgets;
using WaterWars.Views.Widgets.Behaviours;
    
namespace WaterWars.Views
{ 
    public class BuyPointView : WaterWarsView
    {
//        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);       
        
        const string WAITING_FOR_GAME_TO_BEGIN_BUY_POINT_STATUS_MSG = "Waiting for game to begin";
        const string GAME_ENDED_STATUS_MSG = "Game ended.  Thanks for playing!";
        const string BUY_POINT_SALE_MSG 
            = "Development rights owned by {0}\nRights price: " 
                + WaterWarsConstants.MONEY_UNIT + "{1}\nTransferable water rights: {2}\nClick me to buy";   
        const string BUY_POINT_STATUS_MSG = "Development rights owned by {0}\nAssets on parcel: {1}";                
        public const string GAME_RESETTING_STATUS_MSG = "Game resetting, please wait.";

        /// <value>
        /// TEMPORARY STORAGE FOR FIELD VIEWS
        /// </value>
        protected Dictionary<UUID, FieldView> m_fieldViews = new Dictionary<UUID, FieldView>();

        /// <value>
        /// Scale of fields.
        /// </value>
        protected Vector3 m_fieldViewScale = Vector3.Zero;
        
        protected BuyPoint m_bp;
        BuyPointViewButton m_button;

        /// <summary>
        /// List of names to use when changing between different scene objects
        /// </summary>
        protected Dictionary<AbstractGameAssetType, string> m_veSceneObjectNames 
            = new Dictionary<AbstractGameAssetType, string>();
        
        public string Status
        {
            get { return m_button.LabelBehaviour.Text; }
            set { m_button.LabelBehaviour.Text = value; }
        }
        
        public BuyPointView(WaterWarsController controller, Scene scene, AbstractView itemStoreView, BuyPoint bp) 
            : base (controller, scene, itemStoreView)
        {
            m_bp = bp;
            m_bp.OnChange += Update;

            m_veSceneObjectNames[AbstractGameAssetType.None] = "For Sale";
            m_veSceneObjectNames[AbstractGameAssetType.Crops] = "Farmhouse";
            m_veSceneObjectNames[AbstractGameAssetType.Houses] = "Site Office";
            m_veSceneObjectNames[AbstractGameAssetType.Factory] = "Portacabin";
        }
        
        public override void Close()
        {
            m_bp.OnChange -= Update;
        }
        
        public override void Initialize(Vector3 pos)
        {
//            m_log.InfoFormat("[WATER WARS]: Creating BuyPointView at {0}", pos);
            
            TaskInventoryItem item = GetItem(m_itemStoreView, m_veSceneObjectNames[AbstractGameAssetType.None]);
            SceneObjectGroup so 
                = m_scene.RezObject(
                    m_itemStoreView.RootPart, item, pos, Quaternion.Identity, Vector3.Zero, 0);

//            m_log.InfoFormat("[WATER WARS]: Created scene object with name {0}", so.Name);

            Initialize(so);  

            // We can only reposition after we've passed the scene object up to the parent class
            so.AbsolutePosition = FindOnGroundPosition(so);
            so.SendGroupFullUpdate();
            
            // FIXME: We have to do this manually right now but really it's the responsibilty of OpenSim.
            so.CreateScriptInstances(0, true, m_scene.DefaultScriptEngine, 0);
        }

        public override void Initialize(SceneObjectPart rootPart)
        {
            base.Initialize(rootPart);
            
            m_button.Initialize(m_bp);            
        }

        protected override void RegisterPart(SceneObjectPart part)
        {
            if (part.IsRoot)
                m_button = new BuyPointViewButton(m_controller, part);            
        }

        protected void Update(AbstractModel model)
        {
//            m_log.InfoFormat("RECEIVED UPDATE FROM BUYPOINT");
            RootPart.Name = m_bp.Name;
            RootPart.OwnerID = m_bp.DevelopmentRightsOwner.Uuid;
            
            GameStateType state = m_controller.Game.State;
            
            if (state == GameStateType.Registration)
            {
                Status = WAITING_FOR_GAME_TO_BEGIN_BUY_POINT_STATUS_MSG;
            }
            else if (state == GameStateType.Build)
            {                   
                if (m_bp.HasAnyOwner)
                {
                    Status
                        = string.Format(
                            BUY_POINT_STATUS_MSG, 
                            m_bp.DevelopmentRightsOwner.Name, m_bp.GameAssets.Count);
                }
                else
                {
                    Status
                        = string.Format(
                            BUY_POINT_SALE_MSG, 
                            m_bp.DevelopmentRightsOwner.Name, m_bp.CombinedPrice, m_bp.InitialWaterRights);                    
                }
            }
            else if (state == GameStateType.Water)
            {
                Status
                    = string.Format(
                        BUY_POINT_STATUS_MSG, 
                        m_bp.DevelopmentRightsOwner.Name, m_bp.GameAssets.Count);
            }
            else if (state == GameStateType.Game_Ended)
            {
                Status = GAME_ENDED_STATUS_MSG;
            }
            else if (state == GameStateType.Game_Resetting)
            {
                Status = GAME_RESETTING_STATUS_MSG;
            }
        }
        
        /// <summary>
        /// Change the specialization presentation of this buy point view
        /// </summary>
        /// <param name="assetType"></param>
        /// <param name="fields">Fields for which field views need to be created</param>
        public Dictionary<UUID, FieldView> ChangeSpecialization(AbstractGameAssetType assetType, List<Field> fields)
        {
            Dictionary<UUID, FieldView> fvs = new Dictionary<UUID, FieldView>();

            string morphItemName = m_veSceneObjectNames[assetType];
            ChangeSceneObject(m_itemStoreView, morphItemName);
            m_bp.Name = morphItemName;

            if (assetType != AbstractGameAssetType.None)
            {
                Vector3 p1, p2;
                WaterWarsUtils.FindSquareParcelCorners(m_bp.Location.Parcel, out p1, out p2);
                
//                m_log.InfoFormat("[WATER WARS]: Found corners of parcel at ({0}),({1})", p1, p2);
    
                int shortDimension = (int)Math.Floor(Math.Sqrt(fields.Count));
                int longDimension = (int)Math.Ceiling((float)fields.Count / shortDimension);
//                m_log.InfoFormat("[WATER WARS]: Would space as [{0}][{1}]", shortDimension, longDimension);
    
                // XXX: For now, we're always going to short space the fields on the x axis
                // This shouldn't be a problem if all our regions are square but might start to look a bit odd if they
                // were different rectangular sizes

                // Adjust dimensions to leave a gap around the edges for the buypoint
                p1.X += 5;
                p1.Y += 5;
                p2.X -= 5;
                p2.Y -= 5;

                float xSpacing = (p2.X - p1.X) / (float)shortDimension;
                float ySpacing = (p2.Y - p1.Y) / (float)longDimension;               
    
                List<Vector3> placementPoints = new List<Vector3>();
                               
//                for (int y = y1; y < y2; y += ySpacing)
//                {
//                    for (float x = x1; x < x2; x += xSpacing)
//                    {
//                        placementPoints.Add(new Vector3(x, y, (float)heightHere + 0.1f));
//                    }
//                }

                for (int y = 0; y < longDimension; y++)
                {
                    for (float x = 0; x < shortDimension; x++)
                    {
                        Vector3 spacing = new Vector3(x * xSpacing, y * ySpacing, 2f);                        
                        placementPoints.Add(p1 + spacing);
                    }
                }                

                m_fieldViewScale = new Vector3(xSpacing, ySpacing, 0.1f);
                Vector3 placementAdjustment = new Vector3(xSpacing / 2, ySpacing / 2, 0);                
                
                int i = 0;
                foreach (Vector3 v in placementPoints)
                {                    
                    FieldView fv = CreateFieldView(fields[i++], v + placementAdjustment);          
                    fvs.Add(fv.RootPart.UUID, fv);       
                }
            }
            else
            {
                lock (m_fieldViews)
                {
                    foreach (FieldView fv in m_fieldViews.Values)
                        fv.Close();                    
                    
                    m_fieldViews.Clear();
                }
            }

            return fvs;
        }

        /// <summary>
        /// Create a field view at the given position.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>         
        public FieldView CreateFieldView(SceneObjectGroup so)
        {
            // We could register an existing field here if we wanted to.
            FieldView fv 
                = new FieldView(
                    m_controller, m_scene, new Field(so.RootPart.UUID, "FieldToDelete"), m_fieldViewScale, m_itemStoreView);            
            
            fv.Initialize(so);
            
            lock (m_fieldViews)
                m_fieldViews.Add(fv.Uuid, fv);  

            return fv;
        }
        
        /// <summary>
        /// Create a field view at the given position.
        /// </summary>
        /// <param name="field"></param>
        /// <param name="pos"></param>
        /// <returns></returns>            
        public FieldView CreateFieldView(Field f, Vector3 pos)
        {
//            m_log.InfoFormat("[WATER WARS]: Placing field {0} at {1}", name, pos);
            
            FieldView fv = new FieldView(m_controller, m_scene, f, m_fieldViewScale, m_itemStoreView);
            fv.Initialize(pos);           
            
            lock (m_fieldViews)
                m_fieldViews.Add(fv.Uuid, fv);               

            return fv;
        }
        
        /// <summary>
        /// Create a game asset view
        /// </summary>
        /// <param name="asset"></param>
        /// <returns>A view that doesn't have a link to the model.  The caller needs to link this subsequently</returns>
        public GameAssetView CreateGameAssetView(AbstractGameAsset asset)
        {
            GameAssetView v = null;
            FieldView fv = m_fieldViews[asset.Field.Uuid];
            Vector3 pos = fv.RootPart.AbsolutePosition;
            
            if (asset.Type == AbstractGameAssetType.Factory)
                v = new FactoryView(m_controller, m_scene, asset, m_itemStoreView);
            else if (asset.Type == AbstractGameAssetType.Houses)
                v = new HousesView(m_controller, m_scene, asset, m_itemStoreView);
            else if (asset.Type == AbstractGameAssetType.Crops)
                v = new CropsView(m_controller, m_scene, asset, m_itemStoreView);
            else
                throw new Exception(string.Format("Unrecognized asset type {0} at position {1}", asset.Type, pos));

            fv.Close();
            m_fieldViews.Remove(asset.Field.Uuid);

            v.Initialize(pos);
            
            return v;
        }

        /// <summary>
        /// Fetch game configuration.
        /// </summary>
        /// <returns></returns>
        public string FetchConfiguration()
        {
            return m_button.FetchConfiguration();
        }
        
        public override void ChangeSceneObject(AbstractView newObjectContainer, string newObjectName)
        {
            base.ChangeSceneObject(newObjectContainer, newObjectName);                        
            m_button.ResetButtonPrims();
        }        
        
        /// <summary>
        /// Handle buy point activities
        /// </summary>
        class BuyPointViewButton : WaterWarsButton
        {
            protected BuyPoint m_bp;
            
            public BuyPointViewButton(WaterWarsController controller, SceneObjectPart part) 
                : base(controller, part, 4600, new FixedTextureBehaviour())
            {                
                Enabled = true;
                OnClick 
                    += delegate(Vector3 offsetPos, IClientAPI remoteClient, SurfaceTouchEventArgs surfaceArgs)
                        { Util.FireAndForget(
                            delegate { 
                                controller.ViewerWebServices.PlayerServices.SetLastSelected(
                                    remoteClient.AgentId, m_bp); }); };                
            }

            public void Initialize(BuyPoint bp)
            {                
                m_bp = bp;
            }
            
            public string FetchConfiguration()
            {
                return base.FetchConfiguration(WaterWarsConstants.REGISTRATION_INI_NAME);
            }
            
//            protected override AbstractInteraction CreateInteraction(IClientAPI remoteClient)
//            {
//                return new BuyPointInteraction(m_controller, this, remoteClient.AgentId, m_bp);
//            }            
        }
    }
}