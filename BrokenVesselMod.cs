using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Modding;
using UnityEngine;
using TranCore;

namespace BrokenVessel
{
    public class BrokenVesselMod : Mod
    {
        static GameObject brokenVessel = null;
        public override List<(string, string)> GetPreloadNames()
        {
            return new List<(string, string)>()
            {
                ("GG_Broken_Vessel","Infected Knight")
            };
        }
        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            brokenVessel = UnityEngine.Object.Instantiate(
                preloadedObjects["GG_Broken_Vessel"]["Infected Knight"]
                );
            brokenVessel.transform.parent = null;
            UnityEngine.Object.DontDestroyOnLoad(brokenVessel);
            brokenVessel.AddComponent<Script>();

            ModHooks.HeroUpdateHook += ModHooks_HeroUpdateHook;
        }

        private void ModHooks_HeroUpdateHook()
        {
            if (Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (!brokenVessel.activeSelf) brokenVessel.SetActive(true);
                else brokenVessel.SetActive(false);
            }
        }
    }
}
