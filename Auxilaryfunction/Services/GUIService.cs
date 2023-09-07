﻿using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static Auxilaryfunction.Auxilaryfunction;
using static Auxilaryfunction.Constant;
using static Auxilaryfunction.Services.DysonBluePrintDataService;
using static Auxilaryfunction.Services.TechService;
using static UnityEngine.Object;

namespace Auxilaryfunction.Services
{
    public class GUIService
    {
        private int TechPanelBluePrintNum;
        public static float max_window_height = 710;
        private static bool DeleteDysonLayer;
        public static GameObject ui_AuxilaryPanelPanel;
        public static int recipewindowx;
        public static int recipewindowy;
        public static int[] locallogics = new int[5];
        public static int[] remotelogics = new int[5];
        public static List<int> fuelItems = new List<int>();
        public static Dictionary<int, bool> FuelFilter = new Dictionary<int, bool>();
        public static List<string> ConfigNames = new List<string>();
        public static Vector2 scrollPosition;
        public static Vector2 pdselectscrollPosition;
        public static ConfigEntry<int> scale;
        public static Texture2D mytexture;
        public static KeyboardShortcut tempShowWindow;
        public static bool blueprintopen;
        public static bool showwindow;
        public static bool ChangeQuickKey;
        public static bool autosetstationconfig;
        public static bool TextTech;
        public static bool DysonPanel;
        public static bool limitmaterial;
        public static bool leftscaling;
        public static bool rightscaling;
        public static bool topscaling;
        public static bool bottomscaling;
        public static bool selectautoaddtechid;
        public static bool moving;
        public static float window_x_move = 200;
        public static float window_y_move = 200;
        public static float temp_window_x = 10;
        public static float temp_window_y = 200;
        public static float window_x = 300;
        public static float window_y = 200;
        static float _windowwitdth;
        public static float Windowwidth
        {
            get => _windowwitdth;
            set
            {
                if (_windowwitdth != value)
                {
                    _windowwitdth = value;
                    window_width.Value = value;
                }
            }
        }
        static float _windowheight;
        public static float Windowheight
        {
            get => _windowheight;
            set
            {
                if (_windowheight != value)
                {
                    _windowheight = value;
                    window_height.Value = value;
                }
            }
        }
        public static List<float[]> boundaries = new List<float[]>();
        static GUIStyle styleblue = new GUIStyle();
        static GUIStyle styleyellow = new GUIStyle();
        static GUIStyle styleitemname = null;
        static GUIStyle buttonstyleyellow = null;
        static GUIStyle buttonstyleblue = null;
        static GUIStyle labelstyle = null;
        static GameObject AuxilaryPanel;
        static GUILayoutOption[] HorizontalSlideroptions;
        static GUILayoutOption[] buttonoptions;

        private static int baseSize;
        public static int BaseSize
        {
            get => baseSize;
            set
            {
                baseSize = value;
                scale.Value = value;
                firstopen = true;
            }
        }

        public static void Init()
        {
            AuxilaryPanel = AssetBundle.LoadFromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("Auxilaryfunction.auxilarypanel")).LoadAsset<GameObject>("AuxilaryPanel");
            autosetstationconfig = true;
            Windowwidth = window_width.Value;
            Windowheight = window_height.Value;
            mytexture = new Texture2D(10, 10);
            for (int i = 0; i < mytexture.width; i++)
                for (int j = 0; j < mytexture.height; j++)
                    mytexture.SetPixel(i, j, new Color(0, 0, 0, 1));
            mytexture.Apply();

            ConfigNames.Add("填充配送机数量");
            boundaries.Add(new float[] { 0, 10 });
            ConfigNames.Add("填充飞机数量");
            boundaries.Add(new float[] { 0, 100 });
            ConfigNames.Add("填充飞船数量");
            boundaries.Add(new float[] { 0, 10 });
            ConfigNames.Add("最大充电功率");
            boundaries.Add(new float[] { 30, 300 });
            ConfigNames.Add("运输机最远路程");
            boundaries.Add(new float[] { 20, 180 });
            ConfigNames.Add("运输船最远路程");
            boundaries.Add(new float[] { 1, 61 });
            ConfigNames.Add("曲速启用路程");
            boundaries.Add(new float[] { 0.5f, 60 });
            ConfigNames.Add("运输机起送量");
            boundaries.Add(new float[] { 0.01f, 1 });
            ConfigNames.Add("运输船起送量");
            boundaries.Add(new float[] { 0.1f, 1 });
            ConfigNames.Add("翘曲填充数量");
            boundaries.Add(new float[] { 0, 50 });
            ConfigNames.Add("大型采矿机采矿速率");
            boundaries.Add(new float[] { 10, 30 });

            if (!string.IsNullOrEmpty(FuelFilterConfig.Value))
            {
                string[] temp = FuelFilterConfig.Value.Split(',');
                foreach (var str in temp)
                {
                    if (str.Length > 3 && int.TryParse(str, out int itemID))
                    {
                        if (FuelFilter.ContainsKey(itemID))
                        {
                            FuelFilter[itemID] = true;
                        }
                    }
                }
            }

            styleblue.fontStyle = FontStyle.Bold;
            styleblue.fontSize = 20;
            styleblue.normal.textColor = new Color32(167, 255, 255, 255);
            styleyellow.fontStyle = FontStyle.Bold;
            styleyellow.fontSize = 20;
            styleyellow.normal.textColor = new Color32(240, 191, 103, 255);
            BeltMonitorWindowOpen();
        }

        public static void GUIUpdate()
        {
            if (QuickKey.Value.IsDown() && !ChangingQuickKey && ready)
            {
                showwindow = !showwindow;
                if (ui_AuxilaryPanelPanel == null)
                    ui_AuxilaryPanelPanel = UnityEngine.Object.Instantiate(AuxilaryPanel, UIRoot.instance.overlayCanvas.transform);
                ui_AuxilaryPanelPanel.SetActive(showwindow && !CloseUIpanel.Value);
            }
            if (showwindow && Input.GetKey(KeyCode.LeftControl))
            {
                int t = (int)(Input.GetAxis("Mouse Wheel") * 10);
                int temp = BaseSize + t;
                if (Input.GetKeyDown(KeyCode.UpArrow)) { temp++; }
                if (Input.GetKeyDown(KeyCode.DownArrow)) { temp--; }
                temp = Math.Max(5, Math.Min(temp, 35));
                BaseSize = temp;
            }
        }
        public static void OnGUIOpen()
        {
            if (firstopen)
            {
                firstopen = false;
                GUI.skin.label.fontSize = BaseSize;
                GUI.skin.button.fontSize = BaseSize;
                GUI.skin.toggle.fontSize = BaseSize;
                GUI.skin.textField.fontSize = BaseSize;
                GUI.skin.textArea.fontSize = BaseSize;
                labelstyle = new GUIStyle(GUI.skin.label);
                labelstyle.fontSize = BaseSize - 3;
                labelstyle.normal.textColor = GUI.skin.toggle.normal.textColor;
                HorizontalSlideroptions = new[] { GUILayout.ExpandWidth(false), GUILayout.Height(BaseSize), GUILayout.Width(BaseSize * 10) };
                buttonoptions = new[] { GUILayout.Height(BaseSize * 2), GUILayout.ExpandWidth(false) };
            }
            if (styleitemname == null)
            {
                styleitemname = new GUIStyle(GUI.skin.label);
                styleitemname.normal.textColor = Color.white;
                buttonstyleblue = new GUIStyle(GUI.skin.button);
                buttonstyleblue.normal.textColor = styleblue.normal.textColor;
                buttonstyleyellow = new GUIStyle(GUI.skin.button);
                buttonstyleyellow.normal.textColor = styleyellow.normal.textColor;
            }
            if (showwindow)
            {
                var rt = ui_AuxilaryPanelPanel.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(Windowwidth, Windowheight);
                rt.localPosition = new Vector2(-Screen.width / 2 + window_x, Screen.height / 2 - window_y - Windowheight);
                //ui_StarMapToolsBasePanel.transform. = new Vector3(window_width, window_height , 1);
                Rect window = new Rect(window_x, window_y, Windowwidth, Windowheight);
                GUI.DrawTexture(window, mytexture);
                if (leftscaling || rightscaling || topscaling || bottomscaling) { }
                else
                    MoveWindow_xl_first(ref window_x, ref window_y, ref window_x_move, ref window_y_move, ref moving, ref temp_window_x, ref temp_window_y, Windowwidth);
                Scaling_window(Windowwidth, Windowheight, ref window_x, ref window_y);
                window = GUI.Window(20210827, window, DoMyWindow1, "辅助面板".getTranslate() + "(" + VERSION + ")" + "ps:ctrl+↑↓");
                int window2width = Localization.language != Language.zhCN ? 15 * BaseSize : 15 * BaseSize / 2;
                Rect switchwindow = new Rect(window_x - window2width, window_y, window2width, 25 * BaseSize);
                if (leftscaling || rightscaling || topscaling || bottomscaling) { }
                else
                    MoveWindow_xl_first(ref window_x, ref window_y, ref window_x_move, ref window_y_move, ref moving, ref temp_window_x, ref temp_window_y, Windowwidth);
                Scaling_window(Windowwidth, Windowheight, ref window_x, ref window_y);
                switchwindow = GUI.Window(202108228, switchwindow, DoMyWindow2, "");
                GUI.DrawTexture(switchwindow, mytexture);
            }
            if (player?.navigation != null && player.navigation._indicatorAstroId != 0)
            {
                if (GUI.Button(new Rect(10, 250, 150, 60), PlayerOperation.fly ? "停止导航".getTranslate() : "继续导航".getTranslate()))
                {
                    PlayerOperation.fly = !PlayerOperation.fly;
                }
                if (GUI.Button(new Rect(10, 300, 150, 60), "取消方向指示".getTranslate()))
                {
                    player.navigation._indicatorAstroId = 0;
                }
            }
            if (automovetounbuilt.Value && player != null && LocalPlanet?.factory != null && LocalPlanet.factory.prebuildCount > 0 && player.movementState == EMovementState.Fly)
            {
                if (GUI.Button(new Rect(10, 360, 150, 60), closecollider ? "停止寻找未完成建筑".getTranslate() : "开始寻找未完成建筑".getTranslate()))
                {
                    StopAutoBuildThread();
                    player.gameObject.GetComponent<SphereCollider>().enabled = !closecollider;
                    player.gameObject.GetComponent<CapsuleCollider>().enabled = !closecollider;
                }
            }
            else if (closecollider)
            {
                StopAutoBuildThread();
                player.gameObject.GetComponent<SphereCollider>().enabled = true;
                player.gameObject.GetComponent<CapsuleCollider>().enabled = true;
            }
            if (closecollider && LocalPlanet.gasItems == null && GUI.Button(new Rect(10, 420, 150, 60), autobuildgetitem ? "停止自动补充材料".getTranslate() : "开始自动补充材料".getTranslate()))
            {
                autobuildgetitem = !autobuildgetitem;
            }
            if (changeups)
            {
                GUI.Label(new Rect(Screen.width / 2, 0, 200, 50), string.Format("{0:N2}", upsfix) + "x");
            }
            BluePrintRecipeSet();
        }

        public static void BluePrintRecipeSet()
        {
            if (blueprintopen)
            {
                int tempwidth = 0;
                int tempheight = 0;
                if (pointeRecipetype != ERecipeType.None)
                {
                    List<RecipeProto> showrecipe = new List<RecipeProto>();
                    foreach (RecipeProto rp in LDB.recipes.dataArray)
                    {
                        if (rp.Type != pointeRecipetype) continue;
                        showrecipe.Add(rp);
                    }
                    foreach (RecipeProto rp in showrecipe)
                    {
                        if (GUI.Button(new Rect(recipewindowx + tempwidth++ * 50, Screen.height - recipewindowy + tempheight * 50, 50, 50), rp.iconSprite.texture))
                        {
                            for (int j = 0; j < assemblerpools.Count; j++)
                            {
                                LocalPlanet.factory.factorySystem.assemblerPool[assemblerpools[j]].SetRecipe(rp.ID, LocalPlanet.factory.entitySignPool);
                            }
                        }
                        if (tempwidth % 10 == 0)
                        {
                            tempwidth = 0;
                            tempheight++;
                        }
                    }
                    if (showrecipe.Count > 0)
                    {
                        if (GUI.Button(new Rect(recipewindowx + tempwidth++ * 50, Screen.height - recipewindowy + tempheight++ * 50, 50, 50), "无".getTranslate()))
                        {
                            for (int j = 0; j < assemblerpools.Count; j++)
                            {
                                LocalPlanet.factory.factorySystem.assemblerPool[assemblerpools[j]].SetRecipe(0, LocalPlanet.factory.entitySignPool);
                            }
                        }
                        if (GUI.Button(new Rect(recipewindowx, Screen.height - recipewindowy + tempheight * 50, 200, 50), "额外产出".getTranslate()))
                        {
                            for (int j = 0; j < assemblerpools.Count; j++)
                            {
                                if (LocalPlanet.factory.factorySystem.assemblerPool[assemblerpools[j]].productive)
                                    LocalPlanet.factory.factorySystem.assemblerPool[assemblerpools[j]].forceAccMode = false;
                            }
                        }
                        if (GUI.Button(new Rect(recipewindowx + 200, Screen.height - recipewindowy + tempheight * 50, 200, 50), "生产加速".getTranslate()))
                        {
                            for (int j = 0; j < assemblerpools.Count; j++)
                            {
                                if (LocalPlanet.factory.factorySystem.assemblerPool[assemblerpools[j]].productive)
                                    LocalPlanet.factory.factorySystem.assemblerPool[assemblerpools[j]].forceAccMode = true;
                            }
                        }
                    }
                }
                else if (labpools.Count > 0)
                {
                    for (int i = 0; i <= 5; i++)
                        if (GUI.Button(new Rect(recipewindowx + tempwidth++ * 50, Screen.height - recipewindowy + tempheight * 50, 50, 50), LDB.items.Select(LabComponent.matrixIds[i]).iconSprite.texture))
                        {
                            for (int j = 0; j < labpools.Count; j++)
                            {
                                LocalPlanet.factory.factorySystem.labPool[labpools[j]].SetFunction(false, LDB.items.Select(LabComponent.matrixIds[i]).maincraft.ID, 0, LocalPlanet.factory.entitySignPool);
                            }
                        }
                    if (GUI.Button(new Rect(recipewindowx + tempwidth++ * 50, Screen.height - recipewindowy + tempheight++ * 50, 50, 50), "无".getTranslate()))
                    {
                        for (int j = 0; j < labpools.Count; j++)
                        {
                            LocalPlanet.factory.factorySystem.labPool[labpools[j]].SetFunction(false, 0, 0, LocalPlanet.factory.entitySignPool);
                        }
                    }
                    if (GUI.Button(new Rect(recipewindowx, Screen.height - recipewindowy + tempheight++ * 50, 200, 50), "科研模式".getTranslate()))
                    {
                        for (int j = 0; j < labpools.Count; j++)
                        {
                            LocalPlanet.factory.factorySystem.labPool[labpools[j]].SetFunction(true, 0, GameMain.history.currentTech, LocalPlanet.factory.entitySignPool);
                        }
                    }
                    if (GUI.Button(new Rect(recipewindowx, Screen.height - recipewindowy + tempheight * 50, 200, 50), "额外产出".getTranslate()))
                    {
                        for (int j = 0; j < labpools.Count; j++)
                        {
                            if (LocalPlanet.factory.factorySystem.labPool[labpools[j]].productive)
                                LocalPlanet.factory.factorySystem.labPool[labpools[j]].forceAccMode = false;
                        }
                    }
                    if (GUI.Button(new Rect(recipewindowx + 200, Screen.height - recipewindowy + tempheight * 50, 200, 50), "生产加速".getTranslate()))
                    {
                        for (int j = 0; j < labpools.Count; j++)
                        {
                            if (LocalPlanet.factory.factorySystem.labPool[labpools[j]].productive)
                                LocalPlanet.factory.factorySystem.labPool[labpools[j]].forceAccMode = true;
                        }
                    }
                }
                else if (ejectorpools.Count > 0 && GameMain.data.dysonSpheres[GameMain.localStar.index] != null)
                {
                    DysonSwarm ds = GameMain.data.dysonSpheres[GameMain.localStar.index].swarm;
                    for (int i = 0; i < 4; i++)
                    {
                        for (int j = 0; j < 5; j++)
                        {
                            int orbitid = i * 5 + j + 1;
                            if (ds.OrbitExist(orbitid) && GUI.Button(new Rect(recipewindowx + j * 50, Screen.height - recipewindowy + tempheight * 50, 50, 50), orbitid.ToString()))
                            {
                                for (int k = 0; k < ejectorpools.Count; k++)
                                {
                                    LocalPlanet.factory.factorySystem.ejectorPool[ejectorpools[k]].SetOrbit(orbitid);
                                }
                            }
                        }
                        tempheight++;
                    }
                }
                else if (stationpools.Count > 0)
                {
                    if (tempheight + tempwidth > 0) tempheight++;
                    tempwidth = 0;
                    for (int i = 0; i < 6; i++)
                    {
                        if (GUI.Button(new Rect(recipewindowx + tempwidth++ * 130, Screen.height - recipewindowy + tempheight * 50, 130, 50), StationNames[i]))
                        {

                            for (int j = 0; j < stationpools.Count; j++)
                            {
                                StationComponent sc = LocalPlanet.factory.transport.stationPool[stationpools[j]];
                                if (i == 5)
                                {
                                    if (sc.storage[4].count > 0 && sc.storage[4].itemId != 1210)
                                        player.TryAddItemToPackage(sc.storage[4].itemId, sc.storage[4].count, 0, false);
                                    LocalPlanet.factory.transport.SetStationStorage(stationpools[j], stationindex, 1210, (int)batchnum * 100, (ELogisticStorage)locallogic, (ELogisticStorage)remotelogic, player);
                                }
                                else sc.name = StationNames[i];
                            }
                            stationpools.Clear();
                            break;
                        }
                        if (i == 4)
                        {
                            tempheight++;
                            tempwidth = 0;
                        }
                    }
                    int tempx = recipewindowx + tempwidth * 130;
                    int tempy = Screen.height - recipewindowy + tempheight++ * 50;
                    batchnum = (int)GUI.HorizontalSlider(new Rect(tempx, tempy, 150, 30), batchnum, 0, 100);
                    GUI.Label(new Rect(tempx, tempy + 30, 100, 30), "上限".getTranslate() + ":" + batchnum * 100);
                    if (GUI.Button(new Rect(tempx + 150, tempy, 100, 30), "第".getTranslate() + (stationindex + 1) + "格".getTranslate()))
                    {
                        stationindex++;
                        stationindex %= 5;
                    }
                    if (GUI.Button(new Rect(tempx + 250, tempy, 100, 30), "本地".getTranslate() + GetStationlogic(locallogic)))
                    {
                        locallogic++;
                        locallogic %= 3;
                    }
                    if (GUI.Button(new Rect(tempx + 350, tempy, 100, 30), "星际".getTranslate() + GetStationlogic(remotelogic)))
                    {
                        remotelogic++;
                        remotelogic %= 3;
                    }
                    if (GUI.Button(new Rect(recipewindowx, Screen.height - recipewindowy + tempheight++ * 50, 130, 50), "粘贴物流站配方".getTranslate()))
                    {
                        PlanetFactory factory = LocalPlanet.factory;
                        for (int j = 0; j < stationpools.Count; j++)
                        {
                            StationComponent sc = factory.transport.stationPool[stationpools[j]];
                            for (int i = 0; i < sc.storage.Length && i < 5; i++)
                            {
                                if (stationcopyItem[i, 0] > 0)
                                {
                                    if (sc.storage[i].count > 0 && sc.storage[i].itemId != stationcopyItem[i, 0])
                                        player.TryAddItemToPackage(sc.storage[i].itemId, sc.storage[i].count, 0, false);
                                    factory.transport.SetStationStorage(stationpools[j], i, stationcopyItem[i, 0], stationcopyItem[i, 1], (ELogisticStorage)stationcopyItem[i, 2]
                                        , (ELogisticStorage)stationcopyItem[i, 3], player);
                                }
                                else
                                    factory.transport.SetStationStorage(stationpools[j], i, 0, 0, ELogisticStorage.None, ELogisticStorage.None, player);
                            }
                        }
                        stationpools.Clear();
                    }
                    int heightdis = BaseSize * 2;
                    GUILayout.BeginArea(new Rect(recipewindowx, Screen.height - recipewindowy + tempheight++ * 50, heightdis * 15, heightdis * 10));
                    {
                        GUILayout.BeginVertical();
                        GUILayout.BeginHorizontal();
                        for (int i = 0; i < 5; i++)
                        {
                            GUILayout.BeginVertical();
                            if (GUILayout.Button("本地".getTranslate() + GetStationlogic(locallogics[i])))
                            {
                                locallogics[i]++;
                                locallogics[i] %= 3;
                            }
                            if (GUILayout.Button("星际".getTranslate() + GetStationlogic(remotelogics[i])))
                            {
                                remotelogics[i]++;
                                remotelogics[i] %= 3;
                            }
                            GUILayout.EndVertical();
                        }
                        GUILayout.EndHorizontal();
                        if (GUILayout.Button("设置物流站逻辑"))
                        {
                            PlanetFactory factory = LocalPlanet.factory;
                            for (int j = 0; j < stationpools.Count; j++)
                            {
                                StationComponent sc = factory.transport.stationPool[stationpools[j]];
                                for (int i = 0; i < sc.storage.Length && i < 5; i++)
                                {
                                    if (sc.storage[i].itemId > 0)
                                    {
                                        sc.storage[i].localLogic = (ELogisticStorage)locallogics[i];
                                        sc.storage[i].remoteLogic = (ELogisticStorage)remotelogics[i];
                                    }
                                }
                            }
                            InitBluePrintData();
                        }
                        GUILayout.EndVertical();
                    }
                    GUILayout.EndArea();
                }
                else if (powergenGammapools.Count > 0)
                {
                    PlanetFactory factory = LocalPlanet.factory;
                    tempwidth = 0;
                    GUILayout.BeginArea(new Rect(recipewindowx, Screen.height - recipewindowy, BaseSize * 10, BaseSize * 10));
                    GUILayout.BeginVertical();
                    if (GUILayout.Button("直接发电".getTranslate()))
                    {
                        for (int j = 0; j < powergenGammapools.Count; j++)
                        {
                            var pgc = factory.powerSystem.genPool[powergenGammapools[j]];
                            int generatorId = powergenGammapools[j];
                            if (pgc.gamma)
                            {
                                PowerGeneratorComponent powerGeneratorComponent = factory.powerSystem.genPool[generatorId];

                                int productId = powerGeneratorComponent.productId;
                                int num = (int)powerGeneratorComponent.productCount;
                                if (productId != 0 && num > 0)
                                {
                                    int upCount = player.TryAddItemToPackage(productId, num, 0, true, 0);
                                    UIItemup.Up(productId, upCount);
                                }
                                factory.powerSystem.genPool[generatorId].productId = 0;
                                factory.powerSystem.genPool[generatorId].productCount = 0;
                            }
                        }
                    }
                    if (GUILayout.Button("光子生成".getTranslate()))
                    {
                        for (int j = 0; j < powergenGammapools.Count; j++)
                        {
                            var pgc = factory.powerSystem.genPool[powergenGammapools[j]];
                            int generatorId = powergenGammapools[j];
                            if (pgc.gamma)
                            {
                                PowerGeneratorComponent powerGeneratorComponent = factory.powerSystem.genPool[generatorId];

                                ItemProto itemProto = LDB.items.Select(factory.entityPool[powerGeneratorComponent.entityId].protoId);
                                if (itemProto == null)
                                {
                                    return;
                                }
                                GameHistoryData history = GameMain.history;
                                if (LDB.items.Select(itemProto.prefabDesc.powerProductId) == null || !history.ItemUnlocked(itemProto.prefabDesc.powerProductId))
                                {
                                    factory.powerSystem.genPool[generatorId].productId = 0;
                                    return;
                                }
                                factory.powerSystem.genPool[generatorId].productId = itemProto.prefabDesc.powerProductId;
                            }
                        }
                    }
                    GUILayout.EndVertical();
                    GUILayout.EndArea();
                }
            }
        }

        public static void StationInfoWindowUpdate()
        {
            if (!ShowStationInfo.Value)
                return;
            if (UIGame.viewMode == EViewMode.Normal || UIGame.viewMode == EViewMode.Globe)
            {
                stationTip.SetActive(true);
                if (GameMain.data?.localPlanet?.factory == null)
                    return;
                var pd = GameMain.data.localPlanet;
                int index1 = 0;
                Vector3 localPosition = GameCamera.main.transform.localPosition;
                Vector3 forward = GameCamera.main.transform.forward;
                float realRadius = pd.realRadius;
                if (pd.factory.transport.stationCursor > 0)
                {
                    foreach (StationComponent stationComponent in pd.factory.transport.stationPool)
                    {
                        if (index1 == maxCount)
                        {
                            ++maxCount;
                            Instantiate(tipPrefab, stationTip.transform);
                            Array.Resize(ref tip, maxCount);
                            tip[maxCount - 1] = Instantiate(tipPrefab, stationTip.transform);
                        }
                        if (stationComponent != null && stationComponent.storage != null)
                        {
                            Vector3 position;
                            int num1;
                            if (stationComponent.isCollector)
                            {
                                position = pd.factory.entityPool[stationComponent.entityId].pos.normalized * (realRadius + 35f);
                                num1 = 2;
                            }
                            else if (stationComponent.isStellar)
                            {
                                position = pd.factory.entityPool[stationComponent.entityId].pos.normalized * (realRadius + 20f);
                                num1 = 5;
                            }
                            else if (stationComponent.isVeinCollector)
                            {
                                position = pd.factory.entityPool[stationComponent.entityId].pos.normalized * (realRadius + 5f);
                                num1 = 1;
                            }
                            else
                            {
                                position = pd.factory.entityPool[stationComponent.entityId].pos.normalized * (realRadius + 15f);
                                num1 = 4;
                            }
                            Vector3 rhs = position - localPosition;
                            float magnitude = rhs.magnitude;
                            float num2 = Vector3.Dot(forward, rhs);
                            if (magnitude < 1.0 || num2 < 1.0)
                            {
                                tip[index1].SetActive(false);
                            }
                            else
                            {
                                Vector2 rectPoint;
                                bool flag = UIRoot.ScreenPointIntoRect(GameCamera.main.WorldToScreenPoint(position), stationTip.GetComponent<RectTransform>(), out rectPoint);
                                if (Mathf.Abs(rectPoint.x) > 8000.0)
                                    flag = false;
                                if (Mathf.Abs(rectPoint.y) > 8000.0)
                                    flag = false;
                                if (Phys.RayCastSphere(localPosition, rhs / magnitude, magnitude, Vector3.zero, realRadius, out RCHCPU _))
                                    flag = false;
                                if (flag)
                                {
                                    rectPoint.x = Mathf.Round(rectPoint.x);
                                    rectPoint.y = Mathf.Round(rectPoint.y);
                                    tip[index1].GetComponent<RectTransform>().anchoredPosition = rectPoint;

                                    if (stationComponent.isCollector)
                                        tip[index1].GetComponent<RectTransform>().sizeDelta = new Vector2(100f, 70f);
                                    else if (stationComponent.isStellar)
                                        tip[index1].GetComponent<RectTransform>().sizeDelta = new Vector2(100f, 220f);
                                    else if (stationComponent.isVeinCollector)
                                        tip[index1].GetComponent<RectTransform>().sizeDelta = new Vector2(100f, 40f);
                                    else
                                        tip[index1].GetComponent<RectTransform>().sizeDelta = new Vector2(100f, 130f);
                                    for (int i = 0; i < num1; ++i)
                                    {
                                        if (stationComponent.storage[i].itemId > 0)
                                        {
                                            tip[index1].transform.Find("icon" + i).GetComponent<Image>().sprite = LDB.items.Select(stationComponent.storage[i].itemId)?.iconSprite;
                                            tip[index1].transform.Find("icon" + i).gameObject.SetActive(true);
                                            tip[index1].transform.Find("countText" + i).GetComponent<Text>().text = stationComponent.storage[i].count.ToString("#,##0");
                                            tip[index1].transform.Find("countText" + i).GetComponent<Text>().color = Color.white;
                                            tip[index1].transform.Find("countText" + i).gameObject.SetActive(true);
                                            tip[index1].SetActive(true);
                                        }
                                        else
                                        {
                                            tip[index1].transform.Find("icon" + i).gameObject.SetActive(false);
                                            tip[index1].transform.Find("countText" + i).GetComponent<Text>().text = "无";
                                            tip[index1].transform.Find("countText" + i).GetComponent<Text>().color = Color.white;
                                            tip[index1].transform.Find("countText" + i).gameObject.SetActive(true);
                                        }
                                    }
                                    if (!string.IsNullOrEmpty(stationComponent.name))
                                    {
                                        tip[index1].transform.Find("icon" + num1).gameObject.SetActive(false);
                                        tip[index1].transform.Find("countText" + num1).GetComponent<Text>().text = stationComponent.name;
                                        tip[index1].transform.Find("countText" + num1).GetComponent<Text>().color = Color.white;
                                        tip[index1].transform.Find("countText" + num1).gameObject.SetActive(true);
                                    }
                                    else
                                    {
                                        tip[index1].transform.Find("icon" + num1).gameObject.SetActive(false);
                                        tip[index1].transform.Find("countText" + num1).gameObject.SetActive(false);
                                    }
                                    for (int i = 0; i < 3; ++i)
                                    {
                                        if (stationComponent.isCollector || stationComponent.isVeinCollector)
                                        {
                                            tip[index1].transform.Find("icontext" + i).gameObject.SetActive(false);
                                            continue;
                                        }
                                        if (i >= 1 && !stationComponent.isStellar)
                                        {
                                            tip[index1].transform.Find("icontext" + i).gameObject.SetActive(false);
                                        }
                                        else
                                        {
                                            tip[index1].transform.Find("icontext" + i).gameObject.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(i * 30, -25 - 30 * (string.IsNullOrEmpty(stationComponent.name) ? num1 : num1 + 1), 0);
                                            tip[index1].transform.Find("icontext" + i).GetComponent<Image>().sprite = LDB.items.Select(i != 2 ? 5001 + i : 1210).iconSprite;
                                            tip[index1].transform.Find("icontext" + i).Find("countText").GetComponent<Text>().color = Color.white;
                                            tip[index1].transform.Find("icontext" + i).Find("countText").GetComponent<Text>().text = i == 0 ? (stationComponent.idleDroneCount + stationComponent.workDroneCount).ToString() : (i == 1 ? (stationComponent.idleShipCount + stationComponent.workShipCount).ToString() : stationComponent.warperCount.ToString());
                                            if (i != 2)
                                            {
                                                tip[index1].transform.Find("icontext" + i).Find("countText2").GetComponent<Text>().color = Color.white;
                                                tip[index1].transform.Find("icontext" + i).Find("countText2").GetComponent<Text>().text = i == 0 ? stationComponent.idleDroneCount.ToString() : stationComponent.idleShipCount.ToString();
                                            }
                                            tip[index1].transform.Find("icontext" + i).gameObject.SetActive(true);
                                        }
                                    }
                                    if (magnitude < 50.0)
                                        tip[index1].transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
                                    else if (magnitude < 250.0)
                                    {
                                        float num3 = (float)(1.75 - magnitude * 0.005);
                                        tip[index1].transform.localScale = new Vector3(1, 1, 1) * num3;
                                    }
                                    else
                                        tip[index1].transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                                    for (int i = string.IsNullOrEmpty(stationComponent.name) ? num1 : num1 + 1; i < 6; ++i)
                                    {
                                        tip[index1].transform.Find("icon" + i).gameObject.SetActive(false);
                                        tip[index1].transform.Find("countText" + i).gameObject.SetActive(false);
                                    }
                                    ++index1;
                                }
                            }
                        }
                    }
                }
                for (int index4 = index1; index4 < maxCount; ++index4)
                    tip[index4].SetActive(false);
            }
            else
                stationTip.SetActive(false);
        }

        private static void BeltMonitorWindowOpen()
        {
            # region BeltWindow
            beltWindow = Instantiate(GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Belt Window"), GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Blueprint Copy Mode").transform);
            beltWindow.GetComponent<RectTransform>().position = new Vector3(7.5f, 50, 20);
            beltWindow.name = "test";
            Vector3 item_sign_localposition = beltWindow.transform.Find("item-sign").GetComponent<RectTransform>().localPosition;
            beltWindow.transform.Find("item-sign").GetComponent<RectTransform>().localPosition = item_sign_localposition - new Vector3(item_sign_localposition.x, -30, 0);
            Vector3 number_input_localposition = beltWindow.transform.Find("number-input").GetComponent<RectTransform>().localPosition;
            beltWindow.transform.Find("number-input").GetComponent<RectTransform>().localPosition = number_input_localposition - new Vector3(number_input_localposition.x, -30, 0);
            Destroy(beltWindow.transform.Find("state").gameObject);
            Destroy(beltWindow.transform.Find("item-display").gameObject);
            Destroy(beltWindow.transform.Find("panel-bg").Find("title-text").gameObject);
            beltWindow.transform.Find("item-sign").GetComponent<Button>().onClick.AddListener(() =>
            {
                if (UISignalPicker.isOpened)
                    UISignalPicker.Close();
                else
                    UISignalPicker.Popup(new Vector2(-300, Screen.height / 3), new Action<int>(SetSignalId));
            });
            beltWindow.transform.Find("number-input").GetComponent<InputField>().onEndEdit.AddListener((string str) =>
            {
                float result = 0.0f;
                if (!float.TryParse(str, out result))
                    return;
                if (beltpools.Count > 0)
                {
                    foreach (int i in beltpools)
                    {
                        LocalPlanet.factory.cargoTraffic.SetBeltSignalIcon(i, pointsignalid);
                        LocalPlanet.factory.cargoTraffic.SetBeltSignalNumber(i, result);
                    }
                }
            });
            beltWindow.gameObject.SetActive(false);
            #endregion
            #region MonitorWindow
            SpeakerPanel = Instantiate<GameObject>(GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Monitor Window"), GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Blueprint Copy Mode").transform);
            SpeakerPanel.GetComponent<RectTransform>().position = new Vector3(7.5f, 50, 20);
            SpeakerPanel.name = "test1";
            SpeakerPanel.gameObject.SetActive(false);
            SpeakerPanel.transform.Find("speaker-panel").gameObject.SetActive(false);
            SpeakerPanel.transform.Find("speaker-panel").GetComponent<RectTransform>().position = new Vector3(8, 47, 20);
            Destroy(SpeakerPanel.transform.Find("flow-statistics").gameObject);
            Destroy(SpeakerPanel.transform.Find("alarm-settings").gameObject);
            Destroy(SpeakerPanel.transform.Find("monitor-settings").gameObject);
            Destroy(SpeakerPanel.transform.Find("sep-line").gameObject);
            Destroy(SpeakerPanel.transform.Find("sep-line").gameObject);
            GameObject speaker_panel = SpeakerPanel.transform.Find("speaker-panel").gameObject;
            GameObject pitch = speaker_panel.transform.Find("pitch").gameObject;
            GameObject volume = speaker_panel.transform.Find("volume").gameObject;
            speaker_panel.GetComponent<UISpeakerPanel>().toneCombo.onItemIndexChange.AddListener(new UnityAction(() =>
            {
                if (monitorpools != null && monitorpools.Count > 0)
                {
                    UIComboBox toneCombo = speaker_panel.GetComponent<UISpeakerPanel>().toneCombo;
                    foreach (int i in monitorpools)
                    {
                        int speakerId = LocalPlanet.factory.cargoTraffic.monitorPool[i].speakerId;
                        LocalPlanet.factory.digitalSystem.speakerPool[speakerId].SetTone(toneCombo.ItemsData[toneCombo.itemIndex]);
                        LocalPlanet.factory.digitalSystem.speakerPool[speakerId].Play(ESpeakerPlaybackOrigin.Current);
                    }
                }
            }));
            pitch.transform.Find("slider").GetComponent<Slider>().onValueChanged.AddListener((float f) =>
            {
                string str = PitchLetter[((int)f - 1) % 12] + (((int)f - 1) / 12).ToString();
                speaker_panel.GetComponent<UISpeakerPanel>().pitchText.text = str;

                if (monitorpools != null && monitorpools.Count > 0)
                {
                    UIComboBox toneCombo = speaker_panel.GetComponent<UISpeakerPanel>().toneCombo;
                    foreach (int i in monitorpools)
                    {
                        int speakerId = LocalPlanet.factory.cargoTraffic.monitorPool[i].speakerId;
                        LocalPlanet.factory.digitalSystem.speakerPool[speakerId].SetPitch((int)f);
                        LocalPlanet.factory.digitalSystem.speakerPool[speakerId].Play(ESpeakerPlaybackOrigin.Current);
                    }
                }
            });
            volume.transform.Find("slider").GetComponent<Slider>().onValueChanged.AddListener((float f) =>
            {
                speaker_panel.GetComponent<UISpeakerPanel>().volumeText.text = ((int)f).ToString();

                if (monitorpools != null && monitorpools.Count > 0)
                {
                    UIComboBox toneCombo = speaker_panel.GetComponent<UISpeakerPanel>().toneCombo;
                    foreach (int i in monitorpools)
                    {
                        int speakerId = LocalPlanet.factory.cargoTraffic.monitorPool[i].speakerId;
                        LocalPlanet.factory.digitalSystem.speakerPool[speakerId].SetVolume((int)speaker_panel.GetComponent<UISpeakerPanel>().volumeSlider.value);
                        LocalPlanet.factory.digitalSystem.speakerPool[speakerId].Play(ESpeakerPlaybackOrigin.Current);
                    }
                }
            });
            speaker_panel.GetComponent<UISpeakerPanel>().testPlayBtn.GetComponent<UIButton>().onClick += new Action<int>((int str) =>
            {
                if (monitorpools != null && monitorpools.Count > 0)
                {
                    UIComboBox toneCombo = speaker_panel.GetComponent<UISpeakerPanel>().toneCombo;
                    foreach (int i in monitorpools)
                    {
                        int speakerId = LocalPlanet.factory.cargoTraffic.monitorPool[i].speakerId;
                        LocalPlanet.factory.digitalSystem.speakerPool[speakerId].SetPitch((int)speaker_panel.GetComponent<UISpeakerPanel>().pitchSlider.value);
                        LocalPlanet.factory.digitalSystem.speakerPool[speakerId].SetVolume((int)speaker_panel.GetComponent<UISpeakerPanel>().volumeSlider.value);
                        LocalPlanet.factory.digitalSystem.speakerPool[speakerId].SetTone(toneCombo.ItemsData[toneCombo.itemIndex]);
                        LocalPlanet.factory.digitalSystem.speakerPool[speakerId].Play(ESpeakerPlaybackOrigin.Current);
                    }
                }
            });
            #endregion

            stationTip = Instantiate(GameObject.Find("UI Root/Overlay Canvas/In Game/Scene UIs/Vein Marks"), GameObject.Find("UI Root/Overlay Canvas/In Game/Scene UIs").transform);
            stationTip.name = "stationTip";
            Destroy(stationTip.GetComponent<UIVeinDetail>());
            tipPrefab = Instantiate(GameObject.Find("UI Root/Overlay Canvas/In Game/Scene UIs/Vein Marks/vein-tip-prefab"), stationTip.transform);
            tipPrefab.name = "tipPrefab";
            tipPrefab.GetComponent<Image>().sprite = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Key Tips/tip-prefab").GetComponent<Image>().sprite;
            tipPrefab.GetComponent<Image>().color = new Color(0, 0, 0, 0.8f);
            tipPrefab.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 160f);
            tipPrefab.GetComponent<Image>().enabled = true;
            tipPrefab.transform.localPosition = new Vector3(200f, 800f, 0);
            tipPrefab.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
            Destroy(tipPrefab.GetComponent<UIVeinDetailNode>());
            tipPrefab.SetActive(false);
            for (int index = 0; index < 6; ++index)
            {
                GameObject gameObject1 = Instantiate<GameObject>(tipPrefab.transform.Find("info-text").gameObject, new Vector3(0, 0, 0), Quaternion.identity, tipPrefab.transform);
                gameObject1.name = "countText" + index;
                gameObject1.GetComponent<Text>().fontSize = index == 5 ? 15 : 19;
                gameObject1.GetComponent<Text>().text = "99999";
                gameObject1.GetComponent<Text>().alignment = TextAnchor.MiddleRight;
                gameObject1.GetComponent<RectTransform>().sizeDelta = new Vector2(95, 30);
                gameObject1.GetComponent<RectTransform>().anchorMax = new Vector2(0, 1);
                gameObject1.GetComponent<RectTransform>().anchorMin = new Vector2(0, 1);
                gameObject1.GetComponent<RectTransform>().pivot = new Vector2(0, 1);
                gameObject1.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(0, (-5 - 30 * index), 0);
                Destroy(gameObject1.GetComponent<Shadow>());
                GameObject gameObject2 = Instantiate(tipPrefab.transform.Find("icon").gameObject, new Vector3(0, 0, 0), Quaternion.identity, tipPrefab.transform);
                gameObject2.name = "icon" + index;
                gameObject2.GetComponent<RectTransform>().anchorMax = new Vector2(0, 1);
                gameObject2.GetComponent<RectTransform>().anchorMin = new Vector2(0, 1);
                gameObject2.GetComponent<RectTransform>().pivot = new Vector2(0, 1);
                gameObject2.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(0, (-5 - 30 * index), 0);
                //UIIconCountInc uiiconCountInc = Instantiate<UIIconCountInc>(this.icons[0], this.icons[0].transform.parent);
                //uiiconCountInc.SetTransformIdentity();
                //uiiconCountInc.visible = false;
            }
            for (int i = 0; i < 3; i++)
            {
                GameObject icontext = Instantiate(GameObject.Find("UI Root/Overlay Canvas/In Game/Top Tips/Entity Briefs/brief-info-top/brief-info/content/icons/icon"), new Vector3(0, 0, 0), Quaternion.identity, tipPrefab.transform);
                icontext.name = "icontext" + i;
                icontext.GetComponent<RectTransform>().localScale = new Vector3(0.7f, 0.7f, 1);
                icontext.GetComponent<RectTransform>().anchorMax = new Vector2(0, 1);
                icontext.GetComponent<RectTransform>().anchorMin = new Vector2(0, 1);
                icontext.GetComponent<RectTransform>().pivot = new Vector2(0, 1);
                icontext.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(i * 30, -180, 0);
                GameObject gameObject1 = Instantiate(tipPrefab.transform.Find("info-text").gameObject, new Vector3(0, 0, 0), Quaternion.identity, icontext.transform);
                gameObject1.name = "countText";
                gameObject1.GetComponent<Text>().fontSize = 22;
                gameObject1.GetComponent<Text>().text = "100";
                gameObject1.GetComponent<Text>().alignment = TextAnchor.MiddleRight;
                gameObject1.GetComponent<RectTransform>().sizeDelta = new Vector2(95, 30);
                gameObject1.GetComponent<RectTransform>().anchorMax = new Vector2(0, 1);
                gameObject1.GetComponent<RectTransform>().anchorMin = new Vector2(0, 1);
                gameObject1.GetComponent<RectTransform>().pivot = new Vector2(0, 1);
                gameObject1.GetComponent<RectTransform>().localPosition = new Vector3(-50, -20, 0);
                Destroy(gameObject1.GetComponent<Shadow>());
                if (i != 2)
                {
                    GameObject gameObject2 = Instantiate(tipPrefab.transform.Find("info-text").gameObject, new Vector3(0, 0, 0), Quaternion.identity, icontext.transform);
                    gameObject2.name = "countText2";
                    gameObject2.GetComponent<Text>().fontSize = 22;
                    gameObject2.GetComponent<Text>().text = "100";
                    gameObject2.GetComponent<Text>().alignment = TextAnchor.MiddleRight;
                    gameObject2.GetComponent<RectTransform>().sizeDelta = new Vector2(95, 30);
                    gameObject2.GetComponent<RectTransform>().anchorMax = new Vector2(0, 1);
                    gameObject2.GetComponent<RectTransform>().anchorMin = new Vector2(0, 1);
                    gameObject2.GetComponent<RectTransform>().pivot = new Vector2(0, 1);
                    gameObject2.GetComponent<RectTransform>().localPosition = new Vector3(-50, 10, 0);
                    Destroy(gameObject2.GetComponent<Shadow>());
                }
                Destroy(icontext.transform.Find("count-text").gameObject);
                Destroy(icontext.transform.Find("bg").gameObject);
                Destroy(icontext.transform.Find("inc").gameObject);
                Destroy(icontext.GetComponent<UIIconCountInc>());
            }
            tipPrefab.transform.Find("info-text").gameObject.SetActive(false);
            tipPrefab.transform.Find("icon").gameObject.SetActive(false);
            for (int i = 0; i < maxCount; ++i)
                tip[i] = Instantiate<GameObject>(tipPrefab, stationTip.transform);
        }

        private static void SetSignalId(int signalId)
        {
            if (LDB.signals.IconSprite(signalId) == null) return;
            pointsignalid = signalId;
            beltWindow.transform.Find("item-sign").GetComponent<Image>().sprite = LDB.signals.IconSprite(signalId);
        }

        private static void DoMyWindow2(int winId)
        {
            int heightdis = BaseSize * 2;
            int widthlen2 = Localization.language != Language.zhCN ? 15 * BaseSize : 9 * BaseSize;
            GUILayout.BeginArea(new Rect(10, 20, widthlen2, 400));
            if (TextTech != GUI.Toggle(new Rect(0, 10, widthlen2, heightdis), TextTech, "文字科技树".getTranslate()))
            {
                TextTech = !TextTech;
                if (TextTech) DysonPanel = false;
            }
            if (limitmaterial != GUI.Toggle(new Rect(heightdis / 2, 10 + heightdis, widthlen2, heightdis), limitmaterial, "限制材料".getTranslate()))
            {
                limitmaterial = !limitmaterial;
                if (limitmaterial) TextTech = true;
            }
            if (DysonPanel != GUI.Toggle(new Rect(0, 10 + heightdis * 2, widthlen2, heightdis), DysonPanel, "戴森球面板".getTranslate()))
            {
                DysonPanel = !DysonPanel;
                if (DysonPanel) TextTech = false;
            }

            GUILayout.EndArea();

        }
        private static void DoMyWindow1(int winId)
        {
            int heightdis = BaseSize * 2;
            if (TextTech)
            {
                TextTechPanelGUI(heightdis);
            }
            else if (DysonPanel)
            {
                DysonPanelGUI(heightdis);
            }
            else
            {
                GUILayout.BeginArea(new Rect(10, 20, Windowwidth - 30, Windowheight));
                scrollPosition = GUILayout.BeginScrollView(scrollPosition);
                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
                GUILayout.Space(20);
                GUILayout.BeginVertical();
                {
                    GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
                    auto_supply_station.Value = GUILayout.Toggle(auto_supply_station.Value, "自动配置新运输站".getTranslate(), buttonoptions);
                    autosetstationconfig = GUILayout.Toggle(autosetstationconfig, "配置参数".getTranslate(), buttonoptions);
                    GUILayout.EndHorizontal();
                    if (autosetstationconfig && auto_supply_station.Value)
                    {
                        for (int i = 0; i < ConfigNames.Count; i++)
                        {
                            GUILayout.BeginHorizontal();
                            string showinfo = "";
                            switch (i)
                            {
                                case 0:
                                    auto_supply_Courier.Value = (int)GUILayout.HorizontalSlider(auto_supply_Courier.Value, boundaries[i][0], boundaries[i][1], HorizontalSlideroptions);
                                    showinfo = auto_supply_Courier.Value + " ";
                                    break;
                                case 1:
                                    auto_supply_drone.Value = (int)GUILayout.HorizontalSlider(auto_supply_drone.Value, boundaries[i][0], boundaries[i][1], HorizontalSlideroptions);
                                    showinfo = auto_supply_drone.Value + " ";
                                    break;
                                case 2:
                                    auto_supply_ship.Value = (int)GUILayout.HorizontalSlider(auto_supply_ship.Value, boundaries[i][0], boundaries[i][1], HorizontalSlideroptions);
                                    showinfo = auto_supply_ship.Value + " ";
                                    break;
                                case 3:
                                    stationmaxpowerpertick.Value = (int)GUILayout.HorizontalSlider(stationmaxpowerpertick.Value, boundaries[i][0], boundaries[i][1], HorizontalSlideroptions);
                                    showinfo = (int)stationmaxpowerpertick.Value + "MW ";
                                    break;
                                case 4:
                                    stationdronedist.Value = (int)GUILayout.HorizontalSlider(stationdronedist.Value, boundaries[i][0], boundaries[i][1], HorizontalSlideroptions);
                                    showinfo = stationdronedist.Value + "° ";
                                    break;
                                case 5:
                                    stationshipdist.Value = (int)GUILayout.HorizontalSlider(stationshipdist.Value, boundaries[i][0], boundaries[i][1], HorizontalSlideroptions);
                                    showinfo = (stationshipdist.Value == 61 ? "∞ " : stationshipdist.Value + "ly ");
                                    break;
                                case 6:
                                    stationwarpdist.Value = (int)GUILayout.HorizontalSlider((float)stationwarpdist.Value, boundaries[i][0], boundaries[i][1], HorizontalSlideroptions);
                                    if (stationwarpdist.Value == 0) stationwarpdist.Value = 0.5;
                                    showinfo = stationwarpdist.Value + "AU ";
                                    break;
                                case 7:
                                    DroneStartCarry.Value = GUILayout.HorizontalSlider(DroneStartCarry.Value, boundaries[i][0], boundaries[i][1], HorizontalSlideroptions);
                                    DroneStartCarry.Value = DroneStartCarry.Value == 0 ? 0.01f : DroneStartCarry.Value;
                                    showinfo = ((int)(DroneStartCarry.Value * 10) * 10 == 0 ? "1" : "" + (int)(DroneStartCarry.Value * 10) * 10) + "% ";
                                    break;
                                case 8:
                                    ShipStartCarry.Value = GUILayout.HorizontalSlider(ShipStartCarry.Value, boundaries[i][0], boundaries[i][1], HorizontalSlideroptions);
                                    showinfo = (int)(ShipStartCarry.Value * 10) * 10 + "% ";
                                    break;
                                case 9:
                                    auto_supply_warp.Value = (int)GUILayout.HorizontalSlider(auto_supply_warp.Value, boundaries[i][0], boundaries[i][1], HorizontalSlideroptions);
                                    showinfo = auto_supply_warp.Value + " ";
                                    break;
                                case 10:
                                    veincollectorspeed.Value = (int)GUILayout.HorizontalSlider(veincollectorspeed.Value, boundaries[i][0], boundaries[i][1], HorizontalSlideroptions);
                                    showinfo = veincollectorspeed.Value / 10.0f + " ";
                                    break;
                            }
                            GUILayout.Label(showinfo + ConfigNames[i].getTranslate(), labelstyle, buttonoptions);
                            GUILayout.EndHorizontal();
                        }
                    }
                    if (GUILayout.Button("铺满轨道采集器".getTranslate(), buttonoptions)) SetGasStation();
                    if (GUILayout.Button("批量配置当前星球物流站".getTranslate(), buttonoptions)) ChangeAllStationConfig();
                    if (GUILayout.Button("填充当前星球配送机飞机飞船、翘曲器".getTranslate(), buttonoptions)) AddDroneShipToStation();
                    if (GUILayout.Button("批量配置当前星球大型采矿机采矿速率".getTranslate(), buttonoptions)) ChangeAllVeinCollectorSpeedConfig();
                    norender_dysonshell_bool.Value = GUILayout.Toggle(norender_dysonshell_bool.Value, "不渲染戴森壳".getTranslate());
                    norender_dysonswarm_bool.Value = GUILayout.Toggle(norender_dysonswarm_bool.Value, "不渲染戴森云".getTranslate());
                    norender_lab_bool.Value = GUILayout.Toggle(norender_lab_bool.Value, "不渲染研究站".getTranslate());
                    norender_beltitem.Value = GUILayout.Toggle(norender_beltitem.Value, "不渲染传送带货物".getTranslate());
                    norender_shipdrone_bool.Value = GUILayout.Toggle(norender_shipdrone_bool.Value, "不渲染运输船和飞机".getTranslate());
                    norender_entity_bool.Value = GUILayout.Toggle(norender_entity_bool.Value, "不渲染实体".getTranslate());
                    if (simulatorrender != GUILayout.Toggle(simulatorrender, "不渲染全部".getTranslate()))
                    {
                        simulatorrender = !simulatorrender;
                        simulatorchanging = true;
                    }
                    norender_powerdisk_bool.Value = GUILayout.Toggle(norender_powerdisk_bool.Value, "不渲染电网覆盖".getTranslate());
                    closeplayerflyaudio.Value = GUILayout.Toggle(closeplayerflyaudio.Value, "关闭玩家走路飞行声音".getTranslate());
                }
                GUILayout.EndVertical();
                GUILayout.Space(20);

                GUILayout.BeginVertical();
                {
                    if (autoaddtech_bool.Value != GUILayout.Toggle(autoaddtech_bool.Value, "自动添加科技队列".getTranslate(), buttonoptions))
                    {
                        autoaddtech_bool.Value = !autoaddtech_bool.Value;
                        if (!autoaddtech_bool.Value) auto_add_techid.Value = 0;
                    }
                    if (autoaddtech_bool.Value)
                    {
                        GUILayout.Label("自动添加科技等级上限".getTranslate() + ":");
                        string t = GUILayout.TextField(auto_add_techmaxLevel.Value.ToString(), new[] { GUILayout.Height(heightdis), GUILayout.Width(heightdis * 3) });
                        bool reset = !int.TryParse(Regex.Replace(t, @"^[^0-9]", ""), out int maxlevel);
                        if (maxlevel != 0)
                        {
                            auto_add_techmaxLevel.Value = maxlevel;
                        }
                        var pointtech = LDB.techs.Select(auto_add_techid.Value);
                        var name = "未选择".getTranslate();
                        if (pointtech != null)
                        {
                            TechState techstate = GameMain.history.techStates[pointtech.ID];
                            if (techstate.curLevel != techstate.maxLevel)
                            {
                                name = pointtech.name + "level" + techstate.curLevel;
                            }
                            if (reset)
                            {
                                auto_add_techmaxLevel.Value = techstate.maxLevel;
                            }
                        }
                        if (GUILayout.Button(name, buttonoptions))
                        {
                            selectautoaddtechid = !selectautoaddtechid;
                        }
                        if (LDB.techs.dataArray != null && selectautoaddtechid)
                        {
                            for (int i = 0; i < LDB.techs.dataArray.Length; i++)
                            {
                                TechState techstate = GameMain.history.techStates[LDB.techs.dataArray[i].ID];
                                if (techstate.curLevel < techstate.maxLevel && techstate.maxLevel > 10)
                                {
                                    if (GUILayout.Button(LDB.techs.dataArray[i].name + " " + techstate.curLevel + " " + techstate.maxLevel, buttonoptions))
                                    {
                                        auto_add_techid.Value = LDB.techs.dataArray[i].ID;
                                    }
                                }
                            }
                        }
                    }
                    autoAddwarp.Value = GUILayout.Toggle(autoAddwarp.Value, "自动添加翘曲器".getTranslate(), buttonoptions);
                    autoAddFuel.Value = GUILayout.Toggle(autoAddFuel.Value, "自动添加燃料".getTranslate(), buttonoptions);
                    if (autoAddFuel.Value)
                    {
                        int rownum = fuelItems.Count / 6;
                        rownum = fuelItems.Count % 6 > 0 ? rownum + 1 : rownum;
                        int index = 0;
                        for (int i = 0; i < rownum; i++)
                        {
                            GUILayout.BeginHorizontal();
                            for (int j = 0; j < 6 && index < fuelItems.Count; j++, index++)
                            {
                                int itemID = fuelItems[index];
                                GUIStyle style = new GUIStyle();
                                if (FuelFilter[itemID])
                                    style.normal.background = Texture2D.whiteTexture;
                                if (GUILayout.Button(LDB.items.Select(itemID).iconSprite.texture, style, new[] { GUILayout.Height(heightdis), GUILayout.Width(heightdis) }))
                                {
                                    FuelFilter[itemID] = !FuelFilter[itemID];
                                    string result = "";
                                    foreach (var item in FuelFilter)
                                    {
                                        if (item.Value)
                                        {
                                            result += item.Key + ",";
                                        }
                                    }
                                    FuelFilterConfig.Value = result;
                                }
                            }
                            GUILayout.EndHorizontal();
                        }
                    }
                    auto_setejector_bool.Value = GUILayout.Toggle(auto_setejector_bool.Value, "自动配置太阳帆弹射器".getTranslate(), buttonoptions);
                    automovetounbuilt.Value = GUILayout.Toggle(automovetounbuilt.Value, "自动飞向未完成建筑".getTranslate(), buttonoptions);
                    close_alltip_bool.Value = GUILayout.Toggle(close_alltip_bool.Value, "一键闭嘴".getTranslate(), buttonoptions);
                    noscaleuitech_bool.Value = GUILayout.Toggle(noscaleuitech_bool.Value, "科技面板选中不缩放".getTranslate(), buttonoptions);
                    BluePrintSelectAll.Value = GUILayout.Toggle(BluePrintSelectAll.Value, "蓝图全选".getTranslate() + "(ctrl+A）", buttonoptions);
                    BluePrintDelete.Value = GUILayout.Toggle(BluePrintDelete.Value, "蓝图删除".getTranslate() + "(ctrl+X）", buttonoptions);
                    BluePrintRevoke.Value = GUILayout.Toggle(BluePrintRevoke.Value, "蓝图撤销".getTranslate() + "(ctrl+Z)", buttonoptions);
                    BluePrintSetRecipe.Value = GUILayout.Toggle(BluePrintSetRecipe.Value, "蓝图设置配方".getTranslate() + "(ctrl+F)", buttonoptions);
                    bool temp = GUILayout.Toggle(ShowStationInfo.Value, "物流站信息显示".getTranslate(), buttonoptions);
                    stationcopyItem_bool.Value = GUILayout.Toggle(stationcopyItem_bool.Value, "物流站物品设置复制粘贴".getTranslate(), buttonoptions);
                    if (temp != ShowStationInfo.Value)
                    {
                        ShowStationInfo.Value = temp;
                        if (!temp)
                            for (int index = 0; index < maxCount; ++index)
                                tip[index].SetActive(false);
                    }
                    if (autoabsorttrash_bool.Value != GUILayout.Toggle(autoabsorttrash_bool.Value, "30s间隔自动吸收垃圾".getTranslate(), buttonoptions))
                    {
                        autoabsorttrash_bool.Value = !autoabsorttrash_bool.Value;
                        if (autoabsorttrash_bool.Value)
                        {
                            autocleartrash_bool.Value = false;
                        }
                    }
                    if (autoabsorttrash_bool.Value)
                    {
                        onlygetbuildings.Value = GUILayout.Toggle(onlygetbuildings.Value, "只回收建筑".getTranslate(), buttonoptions);
                    }
                    if (autocleartrash_bool.Value != GUILayout.Toggle(autocleartrash_bool.Value, "30s间隔自动清除垃圾".getTranslate(), buttonoptions))
                    {
                        autocleartrash_bool.Value = !autocleartrash_bool.Value;
                        if (autocleartrash_bool.Value)
                        {
                            autoabsorttrash_bool.Value = false;
                            onlygetbuildings.Value = false;
                        }
                    }
                    ChangeQuickKey = GUILayout.Toggle(ChangeQuickKey, !ChangeQuickKey ? "改变窗口快捷键".getTranslate() : "点击确认".getTranslate(), buttonoptions);
                    if (ChangeQuickKey)
                    {
                        GUILayout.TextArea(tempShowWindow.ToString(), new[] { GUILayout.Height(heightdis), GUILayout.Width(6 * heightdis) });
                    }
                }
                GUILayout.EndVertical();
                GUILayout.Space(20);

                GUILayout.BeginVertical();
                {
                    changeups = GUILayout.Toggle(changeups, "启动时间流速修改".getTranslate(), buttonoptions);
                    GUILayout.Label("流速倍率".getTranslate() + ":" + string.Format("{0:N2}", upsfix), buttonoptions);
                    string tempupsfixstr = GUILayout.TextField(string.Format("{0:N2}", upsfix), new[] { GUILayout.Height(heightdis), GUILayout.Width(5 * heightdis) });

                    if (tempupsfixstr != string.Format("{0:N2}", upsfix) && float.TryParse(tempupsfixstr, out float tempupsfix))
                    {
                        if (tempupsfix < 0.01) tempupsfix = 0.01f;
                        if (tempupsfix > 10) tempupsfix = 10;
                        upsfix = tempupsfix;
                    }
                    upsfix = GUILayout.HorizontalSlider(upsfix, 0.01f, 10, HorizontalSlideroptions);
                    upsquickset.Value = GUILayout.Toggle(upsquickset.Value, "加速减速".getTranslate() + "(shift,'+''-')", buttonoptions);

                    autosetSomevalue_bool.Value = GUILayout.Toggle(autosetSomevalue_bool.Value, "自动配置建筑".getTranslate(), buttonoptions);
                    GUILayout.Label("人造恒星燃料数量".getTranslate() + "：" + auto_supply_starfuel.Value, buttonoptions);
                    auto_supply_starfuel.Value = (int)GUILayout.HorizontalSlider(auto_supply_starfuel.Value, 4, 100, HorizontalSlideroptions);
                    if (GUILayout.Button("填充当前星球人造恒星".getTranslate(), buttonoptions)) AddFuelToAllStar();

                    autosavetimechange.Value = GUILayout.Toggle(autosavetimechange.Value, "自动保存".getTranslate(), buttonoptions);
                    if (autosavetimechange.Value)
                    {
                        GUILayout.Label("自动保存时间".getTranslate() + "/min：", buttonoptions);
                        int tempint = autosavetime.Value / 60;
                        if (int.TryParse(Regex.Replace(GUILayout.TextField(tempint + "", new[] { GUILayout.Height(heightdis), GUILayout.Width(5 * heightdis) }), @"[^0-9]", ""), out tempint))
                        {
                            if (tempint < 1) tempint = 1;
                            if (tempint > 10000) tempint = 10000;
                            autosavetime.Value = tempint * 60;
                        }
                    }
                    if (CloseUIpanel.Value != GUILayout.Toggle(CloseUIpanel.Value, "关闭白色面板".getTranslate(), buttonoptions))
                    {
                        CloseUIpanel.Value = !CloseUIpanel.Value;
                        ui_AuxilaryPanelPanel.SetActive(!CloseUIpanel.Value);
                    }
                    KeepBeltHeight.Value = GUILayout.Toggle(KeepBeltHeight.Value, "保持传送带高度(shift)".getTranslate(), buttonoptions);
                    Quickstop_bool.Value = GUILayout.Toggle(Quickstop_bool.Value, "ctrl+空格快速开关".getTranslate(), buttonoptions);
                    stopfactory = GUILayout.Toggle(stopfactory, "     停止工厂".getTranslate(), buttonoptions);
                    autonavigation_bool.Value = GUILayout.Toggle(autonavigation_bool.Value, "自动导航".getTranslate(), buttonoptions);
                    if (autonavigation_bool.Value)
                    {
                        autowarpcommand.Value = GUILayout.Toggle(autowarpcommand.Value, "自动导航使用曲速".getTranslate(), buttonoptions);
                        GUILayout.Label("自动使用翘曲器距离".getTranslate() + ":", buttonoptions);
                        autowarpdistance.Value = GUILayout.HorizontalSlider(autowarpdistance.Value, 0, 30, HorizontalSlideroptions);
                        GUILayout.Label(string.Format("{0:N2}", autowarpdistance.Value) + "光年".getTranslate() + "\n", buttonoptions);
                    }
                }
                GUILayout.EndVertical();

                GUILayout.EndHorizontal();
                GUILayout.EndScrollView();
                GUILayout.EndArea();

            }
        }
        private static void TextTechPanelGUI(int heightdis)
        {
            int tempheight = 0;
            scrollPosition = GUI.BeginScrollView(new Rect(10, 20, Windowwidth - 20, Windowheight - 30), scrollPosition, new Rect(0, 0, Windowwidth - 20, max_window_height));

            int buttonwidth = heightdis * 5;
            int colnum = (int)Windowwidth / buttonwidth;
            var propertyicon = UIRoot.instance.uiGame.techTree.nodePrefab.buyoutButton.transform.Find("icon").GetComponent<Image>().mainTexture;
            GUI.Label(new Rect(0, 0, heightdis * 10, heightdis), "准备研究".getTranslate());
            int i = 0;
            tempheight += heightdis;
            for (; i < readyresearch.Count; i++)
            {
                TechProto tp = LDB.techs.Select(readyresearch[i]);
                if (i != 0 && i % colnum == 0) tempheight += heightdis * 4;
                if (GUI.Button(new Rect(i % colnum * buttonwidth, tempheight, buttonwidth, heightdis * 2), tp.ID < 2000 ? tp.name : (tp.name + tp.Level)))
                {
                    if (GameMain.history.TechInQueue(readyresearch[i]))
                    {
                        for (int j = 0; j < GameMain.history.techQueue.Length; j++)
                        {
                            if (GameMain.history.techQueue[j] != readyresearch[i]) continue;
                            GameMain.history.RemoveTechInQueue(j);
                            break;
                        }
                    }
                    readyresearch.RemoveAt(i);
                }
                int k = 0;
                foreach (ItemProto ip in tp.itemArray)
                {
                    GUI.Button(new Rect(i % colnum * buttonwidth + k++ * heightdis, heightdis * 2 + tempheight, heightdis, heightdis), ip.iconSprite.texture);
                }
                k = 0;
                foreach (RecipeProto rp in tp.unlockRecipeArray)
                {
                    GUI.Button(new Rect(i % colnum * buttonwidth + k++ * heightdis, heightdis * 3 + tempheight, heightdis, heightdis), rp.iconSprite.texture);
                }
                if (GUI.Button(new Rect(i % colnum * buttonwidth + 4 * heightdis, heightdis * 3 + tempheight, heightdis, heightdis), propertyicon))
                {
                    BuyoutTech(tp);
                }
            }
            tempheight += heightdis * 4;
            GUI.Label(new Rect(0, tempheight, heightdis * 10, heightdis), "科技".getTranslate());
            tempheight += heightdis;
            i = 0;
            foreach (TechProto tp in LDB.techs.dataArray)
            {
                if (tp.ID > 2000) break;
                if (readyresearch.Contains(tp.ID) || !GameMain.history.CanEnqueueTech(tp.ID) || tp.MaxLevel > 20 || GameMain.history.TechUnlocked(tp.ID)) continue;
                if (limitmaterial)
                {
                    bool condition = true;
                    foreach (int ip in tp.Items)
                    {
                        if (GameMain.history.ItemUnlocked(ip)) continue;
                        condition = false;
                        break;
                    }
                    if (!condition) continue;
                }
                if (i != 0 && i % colnum == 0) tempheight += heightdis * 4;
                if (GUI.Button(new Rect(i % colnum * buttonwidth, tempheight, buttonwidth, heightdis * 2), tp.name))
                {
                    readyresearch.Add(tp.ID);
                }
                int k = 0;
                foreach (ItemProto ip in tp.itemArray)
                {
                    GUI.Button(new Rect(i % colnum * buttonwidth + k++ * heightdis, heightdis * 2 + tempheight, heightdis, heightdis), ip.iconSprite.texture);
                }
                k = 0;
                foreach (RecipeProto rp in tp.unlockRecipeArray)
                {
                    GUI.Button(new Rect(i % colnum * buttonwidth + k++ * heightdis, heightdis * 3 + tempheight, heightdis, heightdis), rp.iconSprite.texture);
                }
                if (GUI.Button(new Rect(i % colnum * buttonwidth + 4 * heightdis, heightdis * 3 + tempheight, heightdis, heightdis), UIRoot.instance.uiGame.techTree.nodePrefab.buyoutButton.transform.Find("icon").GetComponent<Image>().mainTexture))
                {
                    BuyoutTech(tp);
                }
                i++;
            }
            tempheight += heightdis * 4;
            GUI.Label(new Rect(0, tempheight, heightdis * 10, heightdis), "升级".getTranslate());
            i = 0;
            tempheight += heightdis;
            foreach (TechProto tp in LDB.techs.dataArray)
            {
                if (tp.ID < 2000 || readyresearch.Contains(tp.ID) || !GameMain.history.CanEnqueueTech(tp.ID) || tp.MaxLevel > 20 || tp.MaxLevel > 100 || GameMain.history.TechUnlocked(tp.ID)) continue;
                if (limitmaterial)
                {
                    bool condition = true;
                    foreach (int ip in tp.Items)
                    {
                        if (GameMain.history.ItemUnlocked(ip)) continue;
                        condition = false;
                        break;
                    }
                    if (!condition) continue;
                }
                if (i != 0 && i % colnum == 0) tempheight += heightdis * 4;
                if (GUI.Button(new Rect(i % colnum * buttonwidth, tempheight, buttonwidth, heightdis * 2), tp.name + tp.Level))
                {
                    readyresearch.Add(tp.ID);
                }
                int k = 0;
                foreach (ItemProto ip in tp.itemArray)
                {
                    GUI.Button(new Rect(i % colnum * buttonwidth + k++ * heightdis, heightdis * 2 + tempheight, heightdis, heightdis), ip.iconSprite.texture);
                }
                if (GUI.Button(new Rect(i % colnum * buttonwidth + 4 * heightdis, heightdis * 3 + tempheight, heightdis, heightdis), UIRoot.instance.uiGame.techTree.nodePrefab.buyoutButton.transform.Find("icon").GetComponent<Image>().mainTexture))
                {
                    BuyoutTech(tp);
                }
                i++;
            }
            max_window_height = heightdis * 5 + tempheight;
            GUI.EndScrollView();
        }
        private static void DysonPanelGUI(int heightdis)
        {
            bool[] dysonlayers = new bool[11];
            var dyson = UIRoot.instance?.uiGame?.dysonEditor?.selection?.viewDysonSphere;
            if (dyson != null)
            {
                for (int i = 1; i <= 10; i++)
                {
                    if (dyson.layersIdBased[i] != null && dyson.layersIdBased[i].id != 0 && dyson.layersIdBased[i].nodeCount == 0)
                    {
                        dysonlayers[dyson.layersIdBased[i].id] = true;
                    }
                }
            }

            GUILayout.BeginArea(new Rect(10, 20, Windowwidth, Windowheight));
            {
                #region 左侧面板
                GUILayout.BeginArea(new Rect(10, 0, Windowwidth / 2, Windowheight));
                GUILayout.BeginVertical();
                GUILayout.Label("选择一个蓝图后，点击右侧的层级可以自动导入".getTranslate());
                if (GUILayout.Button("打开戴森球蓝图文件夹".getTranslate(), new[] { GUILayout.Height(heightdis), GUILayout.Width(heightdis * 10) }))
                {
                    string path = new StringBuilder(GameConfig.overrideDocumentFolder).Append(GameConfig.gameName).Append("/DysonBluePrint/").ToString();
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    Application.OpenURL(path);
                }
                if (GUILayout.Button("刷新文件".getTranslate(), new[] { GUILayout.Height(heightdis), GUILayout.Width(heightdis * 10) }))
                {
                    selectDysonBlueprintData.path = "";
                    LoadDysonBluePrintData();
                }
                GUILayout.BeginArea(new Rect(0, 3 * heightdis, Windowwidth / 2, Windowheight));
                scrollPosition = GUI.BeginScrollView(new Rect(0, 0, Windowwidth / 2 - 20, Windowheight - 4 * heightdis), scrollPosition, new Rect(0, 0, Windowwidth / 2 - 40, Math.Max((4 + DysonPanelBluePrintNum) * (heightdis + 4), Windowheight - 4 * heightdis)));

                GUILayout.BeginVertical();
                DysonPanelBluePrintNum = 0;
                if (tempDysonBlueprintData.Exists(o => o.type == EDysonBlueprintType.SingleLayer))
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("单层壳".getTranslate());
                    GUILayout.FlexibleSpace();
                    DysonPanelSingleLayer.Value = GUILayout.Toggle(DysonPanelSingleLayer.Value, "", new[] { GUILayout.Width(2 * heightdis) });
                    GUILayout.Space(heightdis);
                    GUILayout.EndHorizontal();
                    if (DysonPanelSingleLayer.Value)
                    {
                        var templist = tempDysonBlueprintData.Where(x => x.type == EDysonBlueprintType.SingleLayer).ToList();
                        for (int i = 0; i < templist.Count; i++)
                        {
                            bool temp = GUILayout.Toggle(templist[i].path == selectDysonBlueprintData.path, templist[i].name, new[] { GUILayout.Height(heightdis) });
                            if (temp != (templist[i].path == selectDysonBlueprintData.path))
                            {
                                selectDysonBlueprintData = templist[i];
                            }
                        }
                        DysonPanelBluePrintNum += templist.Count;
                    }
                }
                if (tempDysonBlueprintData.Exists(o => o.type == EDysonBlueprintType.Layers))
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("多层壳".getTranslate(), new[] { GUILayout.Height(heightdis) });
                    GUILayout.FlexibleSpace();
                    DysonPanelLayers.Value = GUILayout.Toggle(DysonPanelLayers.Value, "", new[] { GUILayout.Width(2 * heightdis) });
                    GUILayout.Space(heightdis);
                    GUILayout.EndHorizontal();
                    if (DysonPanelLayers.Value)
                    {
                        var templist = tempDysonBlueprintData.Where(x => x.type == EDysonBlueprintType.Layers).ToList();
                        for (int i = 0; i < templist.Count; i++)
                        {
                            bool temp = GUILayout.Toggle(templist[i].path == selectDysonBlueprintData.path, templist[i].name, new[] { GUILayout.Height(heightdis) });
                            if (temp != (templist[i].path == selectDysonBlueprintData.path))
                            {
                                selectDysonBlueprintData = templist[i];
                            }
                        }
                        DysonPanelBluePrintNum += templist.Count;
                    }
                }
                if (tempDysonBlueprintData.Exists(o => o.type == EDysonBlueprintType.SwarmOrbits))
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("戴森云".getTranslate());
                    GUILayout.FlexibleSpace();
                    DysonPanelSwarm.Value = GUILayout.Toggle(DysonPanelSwarm.Value, "", new[] { GUILayout.Width(2 * heightdis) });
                    GUILayout.Space(heightdis);
                    GUILayout.EndHorizontal();
                    if (DysonPanelSwarm.Value)
                    {
                        var templist = tempDysonBlueprintData.Where(x => x.type == EDysonBlueprintType.SwarmOrbits).ToList();
                        for (int i = 0; i < templist.Count; i++)
                        {
                            bool temp = GUILayout.Toggle(templist[i].path == selectDysonBlueprintData.path, templist[i].name, new[] { GUILayout.Height(heightdis) });
                            if (temp != (templist[i].path == selectDysonBlueprintData.path))
                            {
                                selectDysonBlueprintData = templist[i];
                            }
                        }
                        DysonPanelBluePrintNum += templist.Count;
                    }
                }
                if (tempDysonBlueprintData.Exists(o => o.type == EDysonBlueprintType.DysonSphere))
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("戴森球(包括壳、云)".getTranslate());
                    GUILayout.FlexibleSpace();
                    DysonPanelDysonSphere.Value = GUILayout.Toggle(DysonPanelDysonSphere.Value, "", new[] { GUILayout.Width(2 * heightdis) });
                    GUILayout.Space(heightdis);
                    GUILayout.EndHorizontal();
                    if (DysonPanelDysonSphere.Value)
                    {
                        var templist = tempDysonBlueprintData.Where(x => x.type == EDysonBlueprintType.DysonSphere).ToList();
                        for (int i = 0; i < templist.Count; i++)
                        {
                            bool temp = GUILayout.Toggle(templist[i].path == selectDysonBlueprintData.path, templist[i].name, new[] { GUILayout.Height(heightdis) });
                            if (temp != (templist[i].path == selectDysonBlueprintData.path))
                            {
                                selectDysonBlueprintData = templist[i];
                            }
                        }
                        DysonPanelBluePrintNum += templist.Count;
                    }
                }
                GUILayout.EndVertical();
                GUI.EndScrollView();
                GUILayout.EndArea();
                GUILayout.EndVertical();
                GUILayout.EndArea();
                #endregion
                #region 右侧面板
                GUILayout.BeginArea(new Rect(10 + Windowwidth / 2, 0, Windowwidth / 2, Windowheight));
                int lineIndex = 0;

                if (GUI.Button(new Rect(0, lineIndex++ * heightdis, heightdis * 10, heightdis), "复制选中文件代码".getTranslate()))
                {
                    string data = ReaddataFromFile(selectDysonBlueprintData.path);
                    GUIUtility.systemCopyBuffer = data;
                    ThreadPool.QueueUserWorkItem(o =>
                    {
                        Thread.Sleep(10000);
                        GUIUtility.systemCopyBuffer = "";
                    });
                }
                if (GUI.Button(new Rect(0, lineIndex++ * heightdis, heightdis * 10, heightdis), "清除剪贴板".getTranslate()))
                {
                    GUIUtility.systemCopyBuffer = "";
                }
                if (GUI.Button(new Rect(0, lineIndex++ * heightdis, heightdis * 10, heightdis), "应用蓝图".getTranslate()) && dyson != null)
                {
                    string data = ReaddataFromFile(selectDysonBlueprintData.path);
                    ApplyDysonBlueprintManage(data, dyson, selectDysonBlueprintData.type);
                }
                if (GUI.Button(new Rect(0, lineIndex++ * heightdis, heightdis * 10, heightdis), "自动生成最大半径戴森壳".getTranslate()) && dyson != null)
                {
                    for (int i = 1; i <= 10; i++)
                    {
                        float radius = dyson.maxOrbitRadius;
                        while (radius > dyson.minOrbitRadius)
                        {
                            if (dyson.QueryLayerRadius(ref radius, out float orbitAngularSpeed))
                            {
                                dyson.AddLayer(radius, Quaternion.identity, orbitAngularSpeed);
                                break;
                            }
                            radius -= 1;
                        }
                        if (dyson.layerCount == 10) break;
                    }
                }
                if (GUI.Button(new Rect(0, lineIndex++ * heightdis, heightdis * 10, heightdis), "删除全部空壳".getTranslate()) && dyson != null)
                {
                    for (int i = 1; i <= 10; i++)
                    {
                        if (dyson.layersIdBased[i] != null && dyson.layersIdBased[i].nodeCount == 0)
                        {
                            dyson.RemoveLayer(dyson.layersIdBased[i]);
                        }
                    }
                }
                if (autoClearEmptyDyson.Value != GUI.Toggle(new Rect(0, lineIndex++ * heightdis, heightdis * 8, heightdis), autoClearEmptyDyson.Value, "自动清除空戴森球".getTranslate()))
                {
                    UIMessageBox.Show(ErrorTitle.getTranslate(), "每次打开戴森球面板都会自动进行清理".getTranslate(), "确定".Translate(), 3, null);
                }


                GUI.Label(new Rect(0, lineIndex++ * heightdis, heightdis * 12, heightdis), "当前选中".getTranslate() + ":" +
                    UIRoot.instance?.uiGame?.dysonEditor?.selection?.viewDysonSphere?.starData.name ?? "");
                GUI.Label(new Rect(0, lineIndex++ * heightdis, heightdis * 5, heightdis), "可用戴森壳层级:".getTranslate());
                for (int i = 1; i <= 10; i++)
                {
                    if (GUI.Button(new Rect((i - 1) % 5 * heightdis, lineIndex * heightdis, heightdis, heightdis), dysonlayers[i] ? i.ToString() : ""))
                    {
                        if (dysonlayers[i])
                        {
                            string data = ReaddataFromFile(selectDysonBlueprintData.path);
                            ApplyDysonBlueprintManage(data, dyson, EDysonBlueprintType.SingleLayer, i);
                        }
                    }
                    if (i % 5 == 0)
                    {
                        lineIndex++;
                    }
                }
                DeleteDysonLayer = GUI.Toggle(new Rect(heightdis * 5, lineIndex * heightdis, heightdis * 8, heightdis), DeleteDysonLayer, "勾选即可点击删除".getTranslate());
                GUI.Label(new Rect(0, lineIndex++ * heightdis, heightdis * 5, heightdis), "不可用戴森壳层级:".getTranslate());
                for (int i = 1; i <= 10; i++)
                {
                    if (GUI.Button(new Rect((i - 1) % 5 * heightdis, lineIndex * heightdis, heightdis, heightdis), !dysonlayers[i] ? i.ToString() : ""))
                    {
                        if (DeleteDysonLayer)
                        {
                            RemoveLayerById(i);
                        }
                    }
                    if (i % 5 == 0)
                    {
                        lineIndex++;
                    }
                }
                GUILayout.EndArea();
                #endregion
            }
            GUILayout.EndArea();
        }
        private static void MoveWindow_xl_first(ref float x, ref float y, ref float x_move, ref float y_move, ref bool movewindow, ref float tempx, ref float tempy, float x_width)
        {
            Vector2 temp = Input.mousePosition;
            if (temp.x > x && temp.x < x + x_width && Screen.height - temp.y > y && Screen.height - temp.y < y + 20)
            {
                if (Input.GetMouseButton(0))
                {
                    if (!movewindow)
                    {
                        x_move = x;
                        y_move = y;
                        tempx = temp.x;
                        tempy = Screen.height - temp.y;
                    }
                    movewindow = true;
                    x = x_move + temp.x - tempx;
                    y = y_move + (Screen.height - temp.y) - tempy;
                }
                else
                {
                    movewindow = false;
                    tempx = x;
                    tempy = y;
                }
            }
            else if (movewindow)
            {
                movewindow = false;
                x = x_move + temp.x - tempx;
                y = y_move + (Screen.height - temp.y) - tempy;
            }
        }

        private static void Scaling_window(float x, float y, ref float x_move, ref float y_move)
        {
            Vector2 temp = Input.mousePosition;
            if (Input.GetMouseButton(0))
            {
                if ((temp.x + 10 > x_move && temp.x - 10 < x_move) && (Screen.height - temp.y >= y_move && Screen.height - temp.y <= y_move + y) || leftscaling)
                {
                    x -= temp.x - x_move;
                    x_move = temp.x;
                    leftscaling = true;
                    rightscaling = false;
                }
                if ((temp.x + 10 > x_move + x && temp.x - 10 < x_move + x) && (Screen.height - temp.y >= y_move && Screen.height - temp.y <= y_move + y) || rightscaling)
                {
                    x += temp.x - x_move - x;
                    rightscaling = true;
                    leftscaling = false;
                }
                if ((Screen.height - temp.y + 10 > y + y_move && Screen.height - temp.y - 10 < y + y_move) && (temp.x >= x_move && temp.x <= x_move + x) || bottomscaling)
                {
                    y += Screen.height - temp.y - (y_move + y);
                    bottomscaling = true;
                }
                if (rightscaling || leftscaling)
                {
                    if ((Screen.height - temp.y + 10 > y_move && Screen.height - temp.y - 10 < y_move) && (temp.x >= x_move && temp.x <= x_move + x) || topscaling)
                    {
                        y -= Screen.height - temp.y - y_move;
                        y_move = Screen.height - temp.y;
                        topscaling = true;
                    }
                }
            }
            if (Input.GetMouseButtonUp(0))
            {
                rightscaling = false;
                leftscaling = false;
                bottomscaling = false;
                topscaling = false;
            }
            Windowwidth = x;
            Windowheight = y;
        }
    }
}
