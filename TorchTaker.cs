// Project:         Torch Taker mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2020 Ralzar
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Ralzar

using System;
using UnityEngine;
using DaggerfallConnect;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Utility.AssetInjection;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop;
using DaggerfallConnect.Utility;
using System.Collections.Generic;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using DaggerfallWorkshop.Game.MagicAndEffects;

namespace TorchTaker
{
    [FullSerializer.fsObject("v1")]
    public class TorchTakerSaveData
    {
        public int DungeonID;
        public List<Vector3> DousedTorches;
    }

    public class TorchTaker : MonoBehaviour, IHasModSaveData
    {
        static Mod mod;
        static TorchTaker instance;
        static GameObject Torch;
        static int dungeonID;
        static List<Vector3> dousedTorches;
        static bool loadedDousedTorches = false;

        static PlayerEnterExit playerEnterExit = GameManager.Instance.PlayerEnterExit;
        static DaggerfallUnity dfUnity = DaggerfallUnity.Instance;

        public Type SaveDataType
        {
            get { return typeof(TorchTakerSaveData); }
        }

        public object NewSaveData()
        {
            return new TorchTakerSaveData
            {
                DungeonID = GameManager.Instance.PlayerGPS.CurrentMapID,
                DousedTorches = new List<Vector3>()
            };
        }

        public object GetSaveData()
        {
            return new TorchTakerSaveData
            {
                DungeonID = dungeonID,
                DousedTorches = dousedTorches
            };
        }

        public void RestoreSaveData(object saveData)
        {
            var torchTakerSaveData = (TorchTakerSaveData)saveData;
            loadedDousedTorches = false;
            dousedTorches = torchTakerSaveData.DousedTorches;
            EntityEffectBroker.OnNewMagicRound += DouseTorches;
        }

        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            mod = initParams.Mod;
            var go = new GameObject(mod.Title);
            go.AddComponent<TorchTaker>();
            instance = go.AddComponent<TorchTaker>();
            mod.SaveDataInterface = instance;

            PlayerActivate.RegisterCustomActivation(mod, 210, 16, TakeTorch);
            PlayerActivate.RegisterCustomActivation(mod, 210, 17, TakeTorch);
            PlayerActivate.RegisterCustomActivation(mod, 210, 18, TakeTorch);

            PlayerEnterExit.OnTransitionDungeonInterior += RemoveVanillaLightSources;
            PlayerEnterExit.OnTransitionDungeonInterior += AddImprovedLighting;
            PlayerEnterExit.OnTransitionExterior += OnTransitionExterior_ListCleanup;
            PlayerEnterExit.OnTransitionDungeonExterior += OnTransitionExterior_ListCleanup;

            EntityEffectBroker.OnNewMagicRound += DouseTorches_OnNewMagicRound;

            mod.IsReady = true;
        }

        void Update()
        {
            if (!dfUnity.IsReady || !playerEnterExit || GameManager.IsGamePaused)
                return;

            //if (!loadedDousedTorches)
            //{
            //    loadedDousedTorches = true;
            //    DouseTorches();
            //}
        }

        private static void RemoveVanillaLightSources(PlayerEnterExit.TransitionEventArgs args)
        {
            DungeonLightHandler[] dfLights = (DungeonLightHandler[])FindObjectsOfType(typeof(DungeonLightHandler)); //Get all dungeon lights in the scene
            for (int i = 0; i < dfLights.Length; i++)
            {
                if (dfLights[i].gameObject.name == "DaggerfallLight [Dungeon]")
                    Destroy(dfLights[i].gameObject);
            }
        }

        private static void AddImprovedLighting(PlayerEnterExit.TransitionEventArgs args)
        {
            DaggerfallBillboard[] lightBillboards = (DaggerfallBillboard[])FindObjectsOfType(typeof(DaggerfallBillboard)); //Get all "light emitting objects" in the dungeon
            foreach (DaggerfallBillboard billBoard in lightBillboards)
            {
                if (billBoard.Summary.Archive == 210)
                {
                    GameObject lightsNode = new GameObject("Lights");
                    lightsNode.transform.parent = billBoard.transform;
                    AddLight(DaggerfallUnity.Instance, billBoard.transform.gameObject, lightsNode.transform);
                }
            }
        }

        private static GameObject AddLight(DaggerfallUnity dfUnity, GameObject torch, Transform parent)
        {
            //float range = obj.Resources.LightResource.Radius * MeshReader.GlobalScale;
            GameObject go = GameObjectHelper.InstantiatePrefab(dfUnity.Option_DungeonLightPrefab.gameObject, string.Empty, parent, torch.transform.position);
            Light light = go.GetComponent<Light>();
            if (light != null)
            {
                light.range = 5;
            }
            return go;
        }

        private static void DouseTorches_OnNewMagicRound()
        {
            Debug.Log("Dousing Torches");
            DouseTorches();
        }

        private static void DouseTorches()
        {
            if (dousedTorches != null && dousedTorches.Count > 0)
            {
                //Code for dousing torches that did not work as I do not have any unique idetnifier for the torches.
                //List<GameObject> allUntaggedObjects = new List<GameObject>(GameObject.FindGameObjectsWithTag("Untagged"));
                //foreach (GameObject obj in allUntaggedObjects)
                //{
                //    foreach (Vector3 torch in dousedTorches)
                //    {
                //        if (obj.transform.position == torch)
                //        {
                //            Torch = obj;
                //            DouseTorch(Torch);
                //        }
                //    }
                //}
                RaycastHit hit;
                GameObject torch;
                foreach (Vector3 obj in dousedTorches)
                {
                    Vector3 torchUnder = obj + (Vector3.down);
                    Ray ray = new Ray(torchUnder, Vector3.up);
                    if (Physics.Raycast(ray, out hit, 10))
                    {
                        torch = hit.transform.gameObject;
                        DouseTorch(torch);
                    }
                }
                Debug.Log("Torches doused");
            }
            else
                Debug.Log("Not running DouseTorches()");
            EntityEffectBroker.OnNewMagicRound -= DouseTorches;
        }

        private static void TakeTorch(RaycastHit hit)
        {
            GameObject torch = hit.transform.gameObject;
            //Vector3 torchPos;
            //torch = hit.transform.gameObject;
            //torchPos = torch.transform.position;
            dousedTorches.Add(torch.transform.position);
            Debug.Log("Torch name = " + torch.name);
            Debug.Log("Torch GetType = " + torch.GetType().ToString());
            Debug.Log("Torch GetInstanceID = " + torch.GetInstanceID().ToString());
            Debug.Log("Torch tag = " + torch.tag);
            Debug.Log("Torch GetHashCode = " + torch.GetHashCode().ToString());
            DouseTorch(torch);
            DaggerfallUnityItem TorchItem = ItemBuilder.CreateItem(ItemGroups.UselessItems2, (int)UselessItems2.Torch);
            GameManager.Instance.PlayerEntity.Items.AddItem(TorchItem);
        }

        private static void DouseTorch(GameObject torch)
        {
            if (torch.name == "DaggerfallBillboard [TEXTURE.210, Index=16]")
                torch.SetActive(false);
            else
                Debug.Log("Not torch. Object name = " + torch.name);
        }

        private static void OnTransitionExterior_ListCleanup(PlayerEnterExit.TransitionEventArgs args)
        {
            dousedTorches.Clear();
        }
    }
}