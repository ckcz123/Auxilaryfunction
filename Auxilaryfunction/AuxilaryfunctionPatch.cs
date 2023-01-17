﻿using BepInEx;
using HarmonyLib;
using UnityEngine;
using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using System.Text.RegularExpressions;
using UnityEngine.UI;
using UnityEngine.Events;
using static Auxilaryfunction.Auxilaryfunction;
using System.Threading;

namespace Auxilaryfunction
{
    public static class AuxilaryfunctionPatch
    {
        private static Player Player => GameMain.mainPlayer;
        private static PlanetData LocalPlanet => GameMain.localPlanet;
        [HarmonyPatch(typeof(FactorySystem), "NewMinerComponent")]
        class NewMinerComponentPatch
        {
            public static void Postfix(ref int __result, FactorySystem __instance)
            {
                if (auto_supply_station.Value && __instance.factory.entityPool[__instance.minerPool[__result].entityId].protoId == 2316)
                {
                    __instance.minerPool[__result].speed = veincollectorspeed.Value * 1000;
                }
            }
        }
        [HarmonyPatch(typeof(UIDETopFunction), "SetDysonComboBox")]
        class UIDETopFunctionSetDysonComboBoxPatch
        {
            public static void Prefix()
            {
                if (autoClearEmptyDyson.Value && GameMain.data?.dysonSpheres!=null)
                {
                    for (int i = 0; i < GameMain.data.dysonSpheres.Length; i++)
                    {
                        var dysonsphere = GameMain.data.dysonSpheres[i];
                        if (dysonsphere != null && dysonsphere.totalNodeCount == 0)
                        {
                            if (dysonsphere.starData.index == (GameMain.localStar?.index ?? 0)) continue;
                            GameMain.data.dysonSpheres[i] = null;
                        }
                    }
                }
            }
        }
        [HarmonyPatch(typeof(FactorySystem), "NewEjectorComponent")]
        class NewEjectorComponentPatch
        {
            public static void Postfix(ref int __result, FactorySystem __instance)
            {
                if (EjectorDictionary.ContainsKey(__instance.planet.id))
                    EjectorDictionary[__instance.planet.id].Add(__result);
                else
                {
                    List<int> temp = new List<int>();
                    temp.Add(__result);
                    EjectorDictionary[__instance.planet.id] = temp;
                }

                __instance.ejectorPool[__result].SetOrbit(1);
            }
        }
        [HarmonyPatch(typeof(FactorySystem), "RemoveEjectorComponent")]
        class RemoveEjectorComponentPatch
        {
            public static void Prefix(int id, FactorySystem __instance)
            {
                if (EjectorDictionary[__instance.planet.id].Contains(id))
                    EjectorDictionary[__instance.planet.id].Remove(id);
            }
        }
        [HarmonyPatch(typeof(GameData), "GameTick")]
        class GameTick1Patch
        {
            public static bool Prefix(ref long time, GameData __instance)
            {
                if (!stopfactory)
                    return true;
                PerformanceMonitor.BeginSample(ECpuWorkEntry.Statistics);
                if (!DSPGame.IsMenuDemo)
                {
                    __instance.statistics.PrepareTick();
                    __instance.history.PrepareTick();
                }
                __instance.mainPlayer.packageUtility.Count();
                PerformanceMonitor.EndSample(ECpuWorkEntry.Statistics);
                if (__instance.localPlanet != null && __instance.localPlanet.factoryLoaded)
                {
                    PerformanceMonitor.BeginSample(ECpuWorkEntry.LocalPhysics);
                    __instance.localPlanet.factory.cargoTraffic.ClearStates();
                    __instance.localPlanet.physics.GameTick();
                    PerformanceMonitor.EndSample(ECpuWorkEntry.LocalPhysics);
                }
                PerformanceMonitor.BeginSample(ECpuWorkEntry.Scenario);
                if (__instance.guideMission != null)
                {
                    __instance.guideMission.GameTick();
                }
                PerformanceMonitor.EndSample(ECpuWorkEntry.Scenario);
                PerformanceMonitor.BeginSample(ECpuWorkEntry.Player);
                if (__instance.mainPlayer != null)
                    __instance.mainPlayer.GameTick(time);
                __instance.DetermineRelative();
                PerformanceMonitor.EndSample(ECpuWorkEntry.Player);
                PerformanceMonitor.BeginSample(ECpuWorkEntry.DysonSphere);
                if (!stopfactory)
                {
                    for (int i = 0; i < __instance.dysonSpheres.Length; i++)
                    {
                        if (__instance.dysonSpheres[i] != null)
                        {
                            __instance.dysonSpheres[i].BeforeGameTick(time);
                        }
                    }
                }
                PerformanceMonitor.EndSample(ECpuWorkEntry.DysonSphere);

                if (!stopfactory)
                {
                    PerformanceMonitor.BeginSample(ECpuWorkEntry.Factory);
                    PerformanceMonitor.BeginSample(ECpuWorkEntry.PowerSystem);
                    for (int j = 0; j < __instance.factoryCount; j++)
                    {
                        Assert.NotNull(__instance.factories[j]);
                        if (__instance.factories[j] != null)
                        {
                            __instance.factories[j].BeforeGameTick(time);
                        }
                    }
                    PerformanceMonitor.EndSample(ECpuWorkEntry.PowerSystem);
                    if (time == 1L)
                    {
                        Debug.Log("check point before multithread");
                    }
                    if (GameMain.multithreadSystem.multithreadSystemEnable)
                    {
                        PerformanceMonitor.BeginSample(ECpuWorkEntry.PowerSystem);
                        GameMain.multithreadSystem.PrepareBeforePowerFactoryData(GameMain.localPlanet, __instance.factories, __instance.factoryCount, time);
                        GameMain.multithreadSystem.Schedule();
                        GameMain.multithreadSystem.Complete();
                        PerformanceMonitor.EndSample(ECpuWorkEntry.PowerSystem);
                        PerformanceMonitor.BeginSample(ECpuWorkEntry.PowerSystem);
                        GameMain.multithreadSystem.PreparePowerSystemFactoryData(GameMain.localPlanet, __instance.factories, __instance.factoryCount, time, Player);
                        GameMain.multithreadSystem.Schedule();
                        GameMain.multithreadSystem.Complete();
                        PerformanceMonitor.EndSample(ECpuWorkEntry.PowerSystem);
                        for (int k = 0; k < __instance.factoryCount; k++)
                        {
                            if (__instance.factories[k].factorySystem != null)
                            {
                                __instance.factories[k].factorySystem.CheckBeforeGameTick();
                            }
                        }
                        PerformanceMonitor.BeginSample(ECpuWorkEntry.Facility);
                        GameMain.multithreadSystem.PrepareAssemblerFactoryData(GameMain.localPlanet, __instance.factories, __instance.factoryCount, time);
                        GameMain.multithreadSystem.Schedule();
                        GameMain.multithreadSystem.Complete();
                        PerformanceMonitor.BeginSample(ECpuWorkEntry.Lab);
                        for (int l = 0; l < __instance.factoryCount; l++)
                        {
                            bool isActive = GameMain.localPlanet == __instance.factories[l].planet;
                            if (__instance.factories[l].factorySystem != null)
                            {
                                __instance.factories[l].factorySystem.GameTickLabResearchMode(time, isActive);
                            }
                        }
                        GameMain.multithreadSystem.PrepareLabOutput2NextData(GameMain.localPlanet, __instance.factories, __instance.factoryCount, time);
                        GameMain.multithreadSystem.Schedule();
                        GameMain.multithreadSystem.Complete();
                        PerformanceMonitor.EndSample(ECpuWorkEntry.Lab);
                        PerformanceMonitor.EndSample(ECpuWorkEntry.Facility);
                        PerformanceMonitor.BeginSample(ECpuWorkEntry.Transport);
                        GameMain.multithreadSystem.PrepareTransportData(GameMain.localPlanet, __instance.factories, __instance.factoryCount, time);
                        GameMain.multithreadSystem.Schedule();
                        GameMain.multithreadSystem.Complete();
                        PerformanceMonitor.EndSample(ECpuWorkEntry.Transport);
                        PerformanceMonitor.BeginSample(ECpuWorkEntry.Storage);
                        for (int index = 0; index < __instance.factoryCount; index++)
                        {
                            PlanetTransport transport = __instance.factories[index].transport;
                            if (transport != null)
                                __instance.factories[index].transport.GameTick_InputFromBelt(time);
                        }
                        PerformanceMonitor.EndSample(ECpuWorkEntry.Storage);
                        PerformanceMonitor.BeginSample(ECpuWorkEntry.Inserter);
                        GameMain.multithreadSystem.PrepareInserterData(GameMain.localPlanet, __instance.factories, __instance.factoryCount, time);
                        GameMain.multithreadSystem.Schedule();
                        GameMain.multithreadSystem.Complete();
                        PerformanceMonitor.EndSample(ECpuWorkEntry.Inserter);
                        PerformanceMonitor.BeginSample(ECpuWorkEntry.Storage);
                        for (int n = 0; n < __instance.factoryCount; n++)
                        {
                            bool isActive2 = GameMain.localPlanet == __instance.factories[n].planet;
                            if (__instance.factories[n].factoryStorage != null)
                            {
                                __instance.factories[n].factoryStorage.GameTick(time, isActive2);
                            }
                        }
                        PerformanceMonitor.EndSample(ECpuWorkEntry.Storage);
                        PerformanceMonitor.BeginSample(ECpuWorkEntry.Belt);
                        GameMain.multithreadSystem.PrepareCargoPathsData(GameMain.localPlanet, __instance.factories, __instance.factoryCount, time);
                        GameMain.multithreadSystem.Schedule();
                        GameMain.multithreadSystem.Complete();
                        PerformanceMonitor.EndSample(ECpuWorkEntry.Belt);
                        PerformanceMonitor.BeginSample(ECpuWorkEntry.Splitter);
                        for (int num = 0; num < __instance.factoryCount; num++)
                        {
                            if (__instance.factories[num].cargoTraffic != null)
                            {
                                __instance.factories[num].cargoTraffic.SplitterGameTick(time);
                            }
                        }
                        PerformanceMonitor.EndSample(ECpuWorkEntry.Splitter);
                        PerformanceMonitor.BeginSample(ECpuWorkEntry.Belt);
                        for (int num2 = 0; num2 < __instance.factoryCount; num2++)
                        {
                            if (__instance.factories[num2].cargoTraffic != null)
                            {
                                __instance.factories[num2].cargoTraffic.MonitorGameTick();
                                __instance.factories[num2].cargoTraffic.SpraycoaterGameTick();
                                __instance.factories[num2].cargoTraffic.PilerGameTick();
                            }
                        }
                        PerformanceMonitor.EndSample(ECpuWorkEntry.Belt);
                        PerformanceMonitor.BeginSample(ECpuWorkEntry.Storage);
                        for (int index = 0; index < __instance.factoryCount; ++index)
                        {
                            PlanetTransport transport = __instance.factories[index].transport;
                            if (transport != null)
                                transport.GameTick_OutputToBelt(GameMain.history.stationPilerLevel, time);
                        }
                        PerformanceMonitor.EndSample(ECpuWorkEntry.Storage);
                        PerformanceMonitor.BeginSample(ECpuWorkEntry.LocalCargo);
                        GameMain.multithreadSystem.PreparePresentCargoPathsData(GameMain.localPlanet, __instance.factories, __instance.factoryCount, time);
                        GameMain.multithreadSystem.Schedule();
                        GameMain.multithreadSystem.Complete();
                        PerformanceMonitor.EndSample(ECpuWorkEntry.LocalCargo);
                        PerformanceMonitor.BeginSample(ECpuWorkEntry.Digital);
                        for (int num4 = 0; num4 < __instance.factoryCount; num4++)
                        {
                            bool isActive3 = GameMain.localPlanet == __instance.factories[num4].planet;
                            if (__instance.factories[num4].digitalSystem != null)
                            {
                                __instance.factories[num4].digitalSystem.GameTick(isActive3);
                            }
                        }
                        PerformanceMonitor.EndSample(ECpuWorkEntry.Digital);
                    }
                    else
                    {
                        for (int num5 = 0; num5 < __instance.factoryCount; num5++)
                        {
                            __instance.factories[num5].GameTick(time);
                        }
                    }
                    if (time == 1L)
                    {
                        Debug.Log("check point after multithread");
                    }
                    PerformanceMonitor.EndSample(ECpuWorkEntry.Factory);
                    PerformanceMonitor.BeginSample(ECpuWorkEntry.Trash);
                    __instance.trashSystem.GameTick(time);
                    PerformanceMonitor.EndSample(ECpuWorkEntry.Trash);
                }

                if (!stopfactory)
                {
                    PerformanceMonitor.BeginSample(ECpuWorkEntry.DysonSphere);
                    if (GameMain.multithreadSystem.multithreadSystemEnable)
                    {
                        for (int num6 = 0; num6 < __instance.dysonSpheres.Length; num6++)
                        {
                            if (__instance.dysonSpheres[num6] != null)
                            {
                                __instance.dysonSpheres[num6].GameTick(time);
                            }
                        }
                        PerformanceMonitor.BeginSample(ECpuWorkEntry.DysonRocket);
                        GameMain.multithreadSystem.PrepareRocketFactoryData(__instance.dysonSpheres, __instance.dysonSpheres.Length);
                        GameMain.multithreadSystem.Schedule();
                        GameMain.multithreadSystem.Complete();
                        PerformanceMonitor.EndSample(ECpuWorkEntry.DysonRocket);
                    }
                    else
                    {
                        for (int num7 = 0; num7 < __instance.dysonSpheres.Length; num7++)
                        {
                            if (__instance.dysonSpheres[num7] != null)
                            {
                                __instance.dysonSpheres[num7].GameTick(time);
                                PerformanceMonitor.BeginSample(ECpuWorkEntry.DysonRocket);
                                __instance.dysonSpheres[num7].RocketGameTick();
                                PerformanceMonitor.EndSample(ECpuWorkEntry.DysonRocket);
                            }
                        }
                    }
                    PerformanceMonitor.EndSample(ECpuWorkEntry.DysonSphere);
                }

                if (__instance.localPlanet != null && __instance.localPlanet.factoryLoaded)
                {
                    PerformanceMonitor.BeginSample(ECpuWorkEntry.LocalAudio);
                    __instance.localPlanet.audio.GameTick();
                    PerformanceMonitor.EndSample(ECpuWorkEntry.LocalAudio);
                }

                if (!stopfactory)
                {
                    PerformanceMonitor.BeginSample(ECpuWorkEntry.Statistics);
                    if (!DSPGame.IsMenuDemo)
                    {
                        __instance.statistics.GameTick(time);
                    }
                    PerformanceMonitor.EndSample(ECpuWorkEntry.Statistics);
                    PerformanceMonitor.BeginSample(ECpuWorkEntry.Digital);
                    if (!DSPGame.IsMenuDemo)
                    {
                        __instance.warningSystem.GameTick(time);
                    }
                    PerformanceMonitor.EndSample(ECpuWorkEntry.Digital);
                    PerformanceMonitor.BeginSample(ECpuWorkEntry.Scenario);
                    __instance.milestoneSystem.GameTick(time);
                    PerformanceMonitor.EndSample(ECpuWorkEntry.Scenario);
                    PerformanceMonitor.BeginSample(ECpuWorkEntry.Statistics);
                    __instance.history.AfterTick();
                    __instance.statistics.AfterTick();
                    PerformanceMonitor.EndSample(ECpuWorkEntry.Statistics);
                }
                __instance.preferences.Collect();
                return false;
            }
        }
        [HarmonyPatch(typeof(PlanetTransport), "NewDispenserComponent")]
        class NewDispenserComponentPatch
        {
            public static void Postfix(int __result, PlanetTransport __instance)
            {
                if (auto_supply_station.Value)
                {
                    __instance.dispenserPool[__result].idleCourierCount = Player.package.TakeItem(5003, auto_supply_Courier.Value, out _);
                }
            }
        }
        [HarmonyPatch(typeof(PlanetTransport), "NewStationComponent")]
        class NewStationComponentPatch
        {
            public static void Postfix(ref StationComponent __result, PlanetTransport __instance)
            {
                if (auto_supply_station.Value && !__result.isCollector && !__result.isVeinCollector)
                {
                    __result.idleDroneCount = Player.package.TakeItem(5001, __result.isStellar ? auto_supply_drone.Value : (auto_supply_drone.Value > 50 ? 50 : auto_supply_drone.Value), out _);
                    __result.tripRangeDrones = Math.Cos(stationdronedist.Value * Math.PI / 180);
                    __instance.planet.factory.powerSystem.consumerPool[__result.pcId].workEnergyPerTick = (long)stationmaxpowerpertick.Value * 16667;
                    if (stationmaxpowerpertick.Value > 60 && !__result.isStellar)
                    {
                        __instance.planet.factory.powerSystem.consumerPool[__result.pcId].workEnergyPerTick = (long)60 * 16667;
                    }
                    __result.deliveryDrones = (int)(DroneStartCarry.Value * 10) * 10;
                    if (__result.isStellar)
                    {
                        __result.warperCount = Player.package.TakeItem(1210, auto_supply_warp.Value, out _);
                        __result.warpEnableDist = stationwarpdist.Value * AU;
                        __result.deliveryShips = (int)(ShipStartCarry.Value * 10) * 10;
                        __result.idleShipCount = Player.package.TakeItem(5002, auto_supply_ship.Value, out _);
                        __result.tripRangeShips = stationshipdist.Value > 60 ? 24000000000 : stationshipdist.Value * 2400000;
                        if (GameMain.data.history.TechUnlocked(3404)) __result.warperCount = Player.package.TakeItem(1210, auto_supply_warp.Value, out _);
                    }
                }
                if (auto_supply_station.Value && __result.isVeinCollector)
                {
                    __instance.factory.factorySystem.minerPool[__result.minerId].speed = veincollectorspeed.Value * 1000;
                }
            }
        }

        [HarmonyPatch(typeof(FactoryModel), "Update")]
        class FactoryModelUpdatePatch
        {
            public static bool Prefix()
            {
                return !norender_entity_bool.Value && !simulatorrender;
            }
        }
        [HarmonyPatch(typeof(FactoryModel), "LateUpdate")]
        class FactoryModelLateUpdatePatch
        {
            public static bool Prefix()
            {
                return !norender_entity_bool.Value && !simulatorrender;
            }
        }
        //[HarmonyPatch(typeof(FactoryModel), "DrawInstancedBatches")]
        //class DrawInstancedBatchesPatch
        //{
        //    public static bool Prefix()
        //    {
        //        return !norender_entity_bool.Value;
        //    }
        //}
        [HarmonyPatch(typeof(LogisticDroneRenderer), "Draw")]
        class DroneDrawPatch
        {
            public static bool Prefix()
            {
                return !norender_shipdrone_bool.Value && !simulatorrender;
            }
        }
        [HarmonyPatch(typeof(LogisticShipUIRenderer), "Draw")]
        class LogisticShipUIRendererDrawPatch
        {
            public static bool Prefix()
            {
                return !norender_shipdrone_bool.Value && !simulatorrender;
            }
        }
        [HarmonyPatch(typeof(LogisticShipRenderer), "Draw")]
        class ShipDrawPatch
        {
            public static bool Prefix()
            {
                return !norender_shipdrone_bool.Value && !simulatorrender;
            }
        }
        [HarmonyPatch(typeof(LabRenderer), "Render")]
        class LabRendererPatch
        {
            public static bool Prefix(LabRenderer __instance)
            {
                if (__instance.modelId == 70)
                    return !norender_lab_bool.Value && !simulatorrender;
                return true;
            }
        }

        [HarmonyPatch(typeof(DysonSphere), "DrawModel")]
        class DysonDrawModelPatch
        {
            public static bool Prefix()
            {
                return !norender_dysonshell_bool.Value && !simulatorrender;
            }
        }
        [HarmonyPatch(typeof(DysonSphere), "DrawPost")]
        class DysonDrawPostPatch
        {
            public static bool Prefix()
            {
                return !norender_dysonswarm_bool.Value && !simulatorrender;
            }
        }
        [HarmonyPatch(typeof(UIPowerGizmo), "DrawArea")]
        class UIPowerGizmoDrawAreaPatch
        {
            public static bool Prefix()
            {
                return !norender_powerdisk_bool.Value && !simulatorrender;
            }
        }
        [HarmonyPatch(typeof(UIPowerGizmo), "DrawCover")]
        class UIPowerGizmoDrawCoverPatch
        {
            public static bool Prefix()
            {
                return !norender_powerdisk_bool.Value && !simulatorrender;
            }
        }

        [HarmonyPatch(typeof(CargoContainer), "Draw")]
        class PathRenderingBatchDrawPatch
        {
            public static bool Prefix()
            {
                return !norender_beltitem.Value;
            }
        }
        [HarmonyPatch(typeof(BuildingParameters), "CopyFromFactoryObject")]
        class CopyFromFactoryObjectPatch
        {
            public static void Prefix(int objectId, PlanetFactory factory)
            {
                if (stationcopyItem_bool.Value)
                {
                    if (Player != null && Player.controller != null && Player.controller.actionBuild != null)
                    {
                        PlayerAction_Build build = Player.controller.actionBuild;
                        if (build.blueprintCopyTool != null || build.blueprintPasteTool != null)
                        {
                            if (build.blueprintPasteTool.active || build.blueprintCopyTool.active)
                                return;
                        }
                    }
                    EntityData[] entitypool = factory.entityPool;
                    if (objectId > entitypool.Length || objectId <= 0) return;
                    int stationId = entitypool[objectId].stationId;
                    if (stationId <= 0)
                        return;

                    StationComponent sc = factory.transport.stationPool[stationId];
                    for (int i = 0; i < 5; i++)
                    {
                        for (int j = 0; j < 6; j++)
                        {
                            stationcopyItem[i, j] = 0;
                        }
                    }
                    for (int i = 0; i < sc.storage.Length && i < 5; i++)
                    {
                        if (sc.storage[i].itemId <= 0) continue;
                        stationcopyItem[i, 0] = sc.storage[i].itemId;
                        stationcopyItem[i, 1] = sc.storage[i].max;
                        stationcopyItem[i, 2] = (int)sc.storage[i].localLogic;
                        stationcopyItem[i, 3] = (int)sc.storage[i].remoteLogic;
                        stationcopyItem[i, 4] = sc.storage[i].localOrder;
                        stationcopyItem[i, 5] = sc.storage[i].remoteOrder;
                    }
                }

            }
        }

        [HarmonyPatch(typeof(BuildTool_Path), "DeterminePreviews")]
        class BuildTool_PathPatch
        {
            public static void Postfix(BuildTool_Path __instance)
            {
                if (!KeepBeltHeight.Value) return;
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    PlanetAuxData planetAux = Player.controller.actionBuild.planetAux;
                    if (planetAux == null) return;
                    if(__instance.altitude == 0)
                    {
                        if (ObjectIsBeltOrSplitter(__instance, __instance.castObjectId))
                        {
                            __instance.altitude = Altitude(__instance.castObjectPos, planetAux, __instance);
                        }
                        else if (__instance.startObjectId != 0)
                        {
                            __instance.altitude = Altitude(__instance.pathPoints[0], planetAux, __instance);
                        }
                    }
                    else if(Input.GetKey(KeyCode.LeftControl) && ObjectIsBeltOrSplitter(__instance, __instance.castObjectId))
                    {
                        __instance.altitude = Altitude(__instance.castObjectPos, planetAux, __instance);
                        if(__instance.altitude == 0)
                        {
                            __instance.altitude =1;
                        }
                    }
                }
            }
            public static int Altitude(Vector3 pos, PlanetAuxData aux, BuildTool_Path buildtoolpath)
            {
                Vector3 b = aux.Snap(pos, true);
                return (int)Math.Round((double)(Vector3.Distance(pos, b) / 1.3333333f));
            }
            public static bool ObjectIsBeltOrSplitter(BuildTool_Path tool, int objId)
            {
                if (objId == 0) return false;
                ItemProto itemProto = LDB.items.Select(tool.factory.entityPool[Math.Abs(objId)].protoId);
                return itemProto != null && itemProto.prefabDesc != null && (itemProto.prefabDesc.isBelt || itemProto.prefabDesc.isSplitter);
            }
        }

        [HarmonyPatch(typeof(BuildingParameters), "PasteToFactoryObject")]
        class PasteToFactoryObjectPatch
        {
            public static void Prefix(int objectId, PlanetFactory factory)
            {
                if (stationcopyItem_bool.Value)
                {
                    EntityData[] entitypool = factory.entityPool;
                    if (objectId > entitypool.Length || objectId <= 0) return;
                    int stationId = entitypool[objectId].stationId;
                    if (stationId <= 0)
                        return;

                    StationComponent sc = factory.transport.stationPool[stationId];
                    if (sc.isVeinCollector || sc.isCollector) return;
                    for (int i = 0; i < sc.storage.Length && i < 5; i++)
                    {
                        if (stationcopyItem[i, 0] > 0)
                        {
                            if (sc.storage[i].count > 0 && sc.storage[i].itemId != stationcopyItem[i, 0])
                                Player.TryAddItemToPackage(sc.storage[i].itemId, sc.storage[i].count, 0, false);
                            factory.transport.SetStationStorage(stationId, i, stationcopyItem[i, 0], stationcopyItem[i, 1], (ELogisticStorage)stationcopyItem[i, 2]
                                , (ELogisticStorage)stationcopyItem[i, 3], Player);
                        }
                        else
                            factory.transport.SetStationStorage(stationId, i, 0, 0, ELogisticStorage.None, ELogisticStorage.None, Player);

                    }
                }
            }
        }

        //星图方向指引启动自动导航
        [HarmonyPatch(typeof(BuildTool_Click), "CheckBuildConditions")]
        class BuildTool_ClickCheckBuildConditions
        {
            public static void Postfix(BuildTool_Click __instance)
            {

            }
        }
        //星图方向指引启动自动导航
        [HarmonyPatch(typeof(UIStarmap), "OnCursorFunction3Click")]
        class AutonavigationPatch
        {
            public static void Prefix(UIStarmap __instance)
            {
                if (autonavigation_bool.Value)
                {
                    if(__instance.focusPlanet!=null && __instance.focusPlanet.planet.id!=GameMain.localPlanet?.id)
                        PlayerOperation.fly = true;
                    else if (__instance.focusStar != null && __instance.focusStar.star.id != GameMain.localStar?    .id)
                        PlayerOperation.fly = true;
                    if (PlayerOperation.fly)
                    {
                        PlayerOperation.flyfocusPlanet = null;
                        PlayerOperation.flyfocusStar = null;
                    }
                }
            }
        }
        [HarmonyPatch(typeof(UIGeneralTips), "OnTechUnlocked")]
        class OnTechUnlockedPatch
        {
            public static bool Prefix()
            {
                return !close_alltip_bool.Value;
            }
        }

        [HarmonyPatch(typeof(UIRandomTip), "_OnOpen")]
        class UIBuildMenu_OnOpenPatch
        {
            public static void Postfix(ref UIRandomTip __instance)
            {
                if (close_alltip_bool.Value)
                {
                    __instance._Close();
                }
            }
        }
        [HarmonyPatch(typeof(UITutorialTip), "Determine")]
        class UITutorialWindow_OnOpenPatch
        {
            public static void Postfix(UITutorialTip __instance)
            {
                if (close_alltip_bool.Value)
                {
                    __instance._Close();
                }
            }
        }

        [HarmonyPatch(typeof(PowerSystem), "NewGeneratorComponent")]
        class NewGeneratorComponentPatch
        {
            public static void Postfix(ref int __result, PowerSystem __instance)
            {
                if (__instance.genPool[__result].fuelMask == 4)
                {
                    if (autosetSomevalue_bool.Value)
                    {
                        int inc;
                        short fuelcount = (short)Player.package.TakeItem(1803, auto_supply_starfuel.Value, out inc);
                        if (fuelcount > 0)
                        {
                            __instance.genPool[__result].SetNewFuel(1803, fuelcount, (short)inc);
                        }
                    }
                }
            }
        }
        [HarmonyPatch(typeof(MilestoneSystem), "NotifyUnlockMilestone")]
        class UIMilestoneTipNotifyUnlockMilestonePatch
        {
            public static bool Prefix()
            {
                return !close_alltip_bool.Value;
            }
        }
        [HarmonyPatch(typeof(PlayerControlGizmo), "AddOrderGizmo")]
        class PlayerControlGizmoPatch
        {
            public static bool Prefix()
            {
                return !closecollider;
            }
        }
        //操纵人物
        [HarmonyPatch(typeof(PlayerController), "GetInput")]
        public class PlayerOperation
        {
            public static float t = 20;
            public static bool fly;
            public static PlanetData flyfocusPlanet = null;
            public static StarData flyfocusStar = null;
            private static StarData LocalStar=> GameMain.localStar;
            private static Mecha mecha=>Player.mecha;

            private static double max_acc => Player.controller.actionSail.max_acc;
            private static float maxSailSpeed => Player.controller.actionSail.maxSailSpeed;
            private static float maxWarpSpeed => Player.controller.actionSail.maxWarpSpeed;
            private static int indicatorAstroId => Player.navigation.indicatorAstroId;
            private static bool CanWarp => LocalPlanet == null && autowarpcommand.Value && !Player.warping && mecha.coreEnergy > mecha.warpStartPowerPerSpeed * maxWarpSpeed;
            
            public static void Postfix(PlayerController __instance)
            {
                #region 寻找建筑
                if (automovetounbuilt.Value && LocalPlanet != null && closecollider)
                {
                    if (autobuildThread == null)
                    {
                        autobuildThread = new Thread(delegate ()
                        {
                            while (closecollider)
                            {
                                try
                                {
                                    float mindistance = 3000;
                                    int lasthasitempd = -1;
                                    foreach (PrebuildData pd in GameMain.localPlanet.factory.prebuildPool)
                                    {
                                        if (pd.id == 0 || pd.itemRequired > 0) continue;
                                        if (lasthasitempd == -1 || mindistance > (pd.pos - Player.position).magnitude)
                                        {
                                            lasthasitempd = pd.id;
                                            mindistance = (pd.pos - Player.position).magnitude;
                                        }
                                    }
                                    if (lasthasitempd == -1)
                                    {
                                        bool getitem = true;
                                        foreach (PrebuildData pd in GameMain.localPlanet.factory.prebuildPool)
                                        {
                                            if (pd.id == 0) continue;
                                            if (__instance.player.package.GetItemCount(pd.protoId) > 0)
                                            {
                                                __instance.player.Order(new OrderNode() { target = pd.pos, type = EOrderType.Move }, false);
                                                getitem = false;
                                                break;
                                            }
                                        }
                                        if (getitem && autobuildgetitem)
                                        {
                                            int[] warningCounts = GameMain.data.warningSystem.warningCounts;
                                            WarningData[] warningpools = GameMain.data.warningSystem.warningPool;
                                            List<int> getItem = new List<int>();
                                            int stackSize = 0;
                                            int packageGridLen = Player.package.grids.Length;
                                            for (int j = packageGridLen - 1; j >= 0 && Player.package.grids[j].count == 0; j--, stackSize++) { }
                                            for (int i = 1; i < GameMain.data.warningSystem.warningCursor && stackSize > 0; i++)
                                            {
                                                if (getItem.Contains(warningpools[i].detailId)) continue;
                                                if (Player.package.GetItemCount(warningpools[i].detailId) > 0) break;
                                                getItem.Add(warningpools[i].detailId);
                                                FindItemAndMove(warningpools[i].detailId, warningCounts[warningpools[i].signalId]);
                                            }
                                        }
                                    }
                                    else if (GameMain.localPlanet.factory.prebuildPool[lasthasitempd].id != 0)
                                    {
                                        __instance.player.Order(new OrderNode() { target = GameMain.localPlanet.factory.prebuildPool[lasthasitempd].pos, type = EOrderType.Move }, false);
                                        if ((GameMain.localPlanet.factory.prebuildPool[lasthasitempd].pos - __instance.player.position).magnitude > 30)
                                        {
                                            __instance.player.currentOrder.targetReached = true;
                                        }
                                    }
                                    Thread.Sleep(2000);
                                }
                                catch
                                {
                                    if (autobuildThread.ThreadState == ThreadState.WaitSleepJoin)
                                        autobuildThread.Interrupt();
                                    else
                                        autobuildThread.Abort();
                                }
                            }
                        });
                        autobuildThread.Start();
                        autobuildThread.IsBackground = true;
                    }
                }
                #endregion

                #region 自动导航
                if (fly && autonavigation_bool.Value && indicatorAstroId != 0)
                {
                    if (indicatorAstroId == LocalStar?.id / 100 || indicatorAstroId == LocalPlanet?.id)
                        fly = false;
                    if (!Player.sailing)
                        FlyAwayPlanet();
                    else
                    {
                        //如果是100的整数倍，根据id生成规则，一定是星系
                        if (indicatorAstroId % 100 == 0)
                        {
                            flyfocusStar = flyfocusStar ?? GameMain.galaxy.StarById(indicatorAstroId / 100);
                            var uPosition = flyfocusStar.uPosition;
                            if ((Player.uPosition - uPosition).magnitude < 100_000)
                            {
                                fly = false;
                                if (Player.warping)
                                {
                                    Player.warpCommand = false;
                                }
                            }
                            else
                            {
                                FlyTo(uPosition);
                                if (CanWarp && mecha.UseWarper())
                                {
                                    Player.warpCommand = true;
                                }
                            }
                        }
                        else
                        {
                            flyfocusPlanet = flyfocusPlanet ?? GameMain.galaxy.PlanetById(indicatorAstroId);
                            var uPosition = flyfocusPlanet.uPosition;
                            var radius = flyfocusPlanet.radius;
                            var factoryLoaded = flyfocusPlanet.factoryLoaded;
                            double distance = (Player.uPosition - uPosition).magnitude;
                            if (distance < radius + 500 && factoryLoaded)
                            {
                                fly = false;
                            }
                            else if (distance < radius + 1000 && !factoryLoaded)
                            {
                                var directVector = (uPosition - Player.uPosition).normalized;
                                if (Vector3.Angle(Player.uVelocity.normalized, directVector) < 10)
                                {
                                    Player.uVelocity = directVector * (distance - radius);
                                }
                                else if(Player.uVelocity.magnitude < 1000)
                                {
                                    Player.uVelocity += directVector * (max_acc);
                                    __instance.actionSail.UseSailEnergy(max_acc);
                                }
                            }
                            else
                            {
                                FlyTo(uPosition);
                                if (Player.warping && distance < 10_000)
                                {
                                    Player.warpCommand = false;
                                }
                                if (CanWarp && distance > autowarpdistance.Value * 2_400_000 && distance > 10_000 && mecha.UseWarper())
                                {
                                    Player.warpCommand = true;
                                }
                            }
                        }
                    }
                }
                if(!fly && (flyfocusPlanet!=null || flyfocusStar!=null))
                {
                    flyfocusPlanet = null;
                    flyfocusStar = null;
                    t = 20;
                }
                #endregion
            }

            private static void FlyTo(VectorLF3 uPosition)
            {
                VectorLF3 direction = (uPosition - Player.uPosition).normalized;

                if (LocalPlanet != null)
                {
                    VectorLF3 diff = Player.uPosition - LocalPlanet.uPosition;
                    double altitude = diff.magnitude - LocalPlanet.radius;
                    float upFactor = Mathf.Clamp((float)((1000.0 - altitude) / 1000.0), 0.0f, 1.0f);
                    upFactor *= upFactor * upFactor;
                    direction = ((direction * (1.0f - upFactor)) + diff.normalized * upFactor).normalized;
                }

                if (Player.uVelocity.magnitude + max_acc >= maxSailSpeed)
                {
                    Player.uVelocity = direction * maxSailSpeed;
                }
                else
                {
                    Player.uVelocity += direction * max_acc;
                    Player.controller.actionSail.UseSailEnergy(max_acc);
                }
            }

            private static void FlyAwayPlanet()
            {
                PlayerController controller = Player.controller;
                controller.input0.z = 1;
                controller.input1.y += 1;
                if (controller.actionFly.currentAltitude > 49 && controller.horzSpeed < 12.5)
                {
                    controller.velocity = (Player.uPosition - LocalPlanet.uPosition).normalized * t++;
                }
            }
        }
        [HarmonyPatch(typeof(UITechTree), "UpdateScale")]
        class UITechTreeUpdateScalePatch
        {
            public static bool Prefix(UITechTree __instance)
            {
                if (noscaleuitech_bool.Value && (__instance.selected != null || __instance.centerViewNode != null)) return false;
                return true;
            }
        }
        [HarmonyPatch(typeof(FPSController), "Update")]
        class FPSControllerUpdatePatch
        {
            public static void Postfix()
            {
                if (!changeups)
                    return;
                Time.fixedDeltaTime = 1 / (upsfix * 60);
            }
        }

        [HarmonyPatch(typeof(UniverseSimulator), "GameTick")]
        class GameDataOnDrawPatch
        {
            public static void Prefix(UniverseSimulator __instance)
            {
                if (simulatorchanging)
                {
                    int num = 0;
                    __instance.backgroundStars.gameObject.SetActive(!simulatorrender);
                    while (__instance.planetSimulators != null && num < __instance.planetSimulators.Length)
                    {
                        if (__instance.planetSimulators[num] != null)
                        {
                            __instance.planetSimulators[num].gameObject.SetActive(!simulatorrender);
                        }
                        num++;
                    }
                    num = 0;
                    while (__instance.starSimulators != null && num < __instance.starSimulators.Length)
                    {
                        //if (__instance.starSimulators[num].starData.type == EStarType.NeutronStar)
                        //{
                        //    num++;
                        //    continue;
                        //}
                        if (__instance.starSimulators[num] != null)
                        {
                            if (__instance.starSimulators[num].starData.type == EStarType.NeutronStar && Configs.builtin.neutronStarPrefab.streamRenderer != null)
                            {
                                num++;
                                continue;
                            }
                            __instance.starSimulators[num].gameObject.SetActive(!simulatorrender);
                        }
                        num++;
                    }
                    simulatorchanging = false;
                }
            }
        }
        [HarmonyPatch(typeof(PlayerAudio), "Update")]
        class PlayerAudioUpdatePatch
        {
            public static bool Prefix()
            {
                return !closeplayerflyaudio.Value;
            }
        }
        [HarmonyPatch(typeof(PlayerFootsteps), "PlayFootstepSound")]
        class PlayerFootstepsPatch
        {
            public static bool Prefix()
            {
                return !closeplayerflyaudio.Value;
            }
        }
    }
}
