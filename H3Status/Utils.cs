using System.IO;
using UnityEngine;
using FistVR;

namespace H3Status.Utils
{

    internal static class AmmoReader
    {
        public static void GetAmmo(string path)
        {
            Plugin.Logger.LogInfo("WRITING FILE");
            StreamWriter writer = new StreamWriter(path, false);

            try {
                ManagerSingleton<AM>.Instance.GenerateFireArmRoundDictionaries();
            }
            catch {
                Plugin.Logger.LogInfo("TypeDict already generated");
            }

            foreach(var roundType in ManagerSingleton<AM>.Instance.TypeDic)
            {
                foreach(var roundClass in roundType.Value)
                {
                    string output = roundType.Key + "," + roundClass.Key + "," + roundClass.Value.Mesh.name;
                    Plugin.Logger.LogInfo(output);
                    writer.WriteLine(output);
                }
            }
            writer.Flush();
            writer.Close();
            Plugin.Logger.LogInfo("DONE");
        }

        public static void GetShells(string path)
        {
            Plugin.Logger.LogInfo("WRITING FILE");
            StreamWriter writer = new StreamWriter(path, false);

            foreach (var roundType in ManagerSingleton<AM>.Instance.TypeList)
            {
                GameObject gameObject = AM.GetRoundSelfPrefab(roundType, AM.GetDefaultRoundClass(roundType)).GetGameObject();
                FVRFireArmRound round = gameObject.GetComponent<FVRFireArmRound>();
                if (round != null)
                {
                    round.Fire();

                    if (round != null && round.FiredRenderer != null)
                    {
                        string output = roundType + "," + round.FiredRenderer.gameObject.GetComponent<MeshFilter>().sharedMesh.name;
                        Plugin.Logger.LogInfo(output);
                        writer.WriteLine(output);
                    }
                }
            }

            writer.Flush();
            writer.Close();
            Plugin.Logger.LogInfo("DONE");
        }
    }

}
