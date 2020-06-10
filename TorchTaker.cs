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
            if (GameManager.Instance.PlayerGPS.CurrentMapID == torchTakerSaveData.DungeonID)
            {
                RaycastHit hit;
                dousedTorches = torchTakerSaveData.DousedTorches;
                foreach (Vector3 torch in dousedTorches)
                {
                    Ray ray = new Ray(torch + (Vector3.down * 1f), Vector3.up);
                    if (Physics.Raycast(ray, out hit, 2))
                        Torch = hit.transform.gameObject;
                    DouseTorch(Torch);
                }
            }
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

            PlayerEnterExit.OnTransitionExterior += OnTransitionExterior_ListCleanup;
            PlayerEnterExit.OnTransitionDungeonExterior += OnTransitionExterior_ListCleanup;

            mod.IsReady = true;
        }

        private static void TakeTorch(RaycastHit hit)
        {
            GameObject torch;
            Vector3 torchPos;
            torch = hit.transform.gameObject;
            torchPos = torch.transform.position;
            dousedTorches.Add(torchPos);
            DouseTorch(torch);
            DaggerfallUnityItem TorchItem = ItemBuilder.CreateItem(ItemGroups.UselessItems2, (int)UselessItems2.Torch);
            GameManager.Instance.PlayerEntity.Items.AddItem(TorchItem);
        }

        private static void DouseTorch(GameObject torch)
        {
            torch.SetActive(false);
        }

        private static void OnTransitionExterior_ListCleanup(PlayerEnterExit.TransitionEventArgs args)
        {
            dousedTorches.Clear();
        }
    }
}