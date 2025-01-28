using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using HarmonyLib;

namespace QOL.Patches
{
	[HarmonyPatch(typeof(Vicotory), "GetRandomWinText")]
	public class VicotoryPatch
	{
        private static string hikotokoText = null;

        private static int cleanUpCounter = 0;

        [HarmonyPostfix]
        private static void Postfix(ref string __result)
        {
            if (ChatCommands.CmdDict["hikotoko"].IsEnabled)
            {
                var c = ChatCommands.CmdDict["hikotoko"].Option;
                string url = BuildHitokotoUrl(c);

                CoroutineRunner.Run(FetchHitokoto(url));
                if (hikotokoText != null)
                {
                    __result = hikotokoText;
                }
            }

            cleanUpCounter++;
            if (cleanUpCounter >= 50)
            {
                CratesCleanUp();
                cleanUpCounter = 0;
            }
        }

        private static void CratesCleanUp()
        {
            var allObjs = Object.FindObjectsOfType<GameObject>();
            foreach (var obj in allObjs)
            {
                if (obj.name == "Crate(Clone)")
                {
                    Object.Destroy(obj);
                }
            }
        }

        private static string BuildHitokotoUrl(string input)
        {
            string baseUrl = "https://v1.hitokoto.cn/?encode=text";
            if (!string.IsNullOrEmpty(input))
            {
                foreach (char c in input)
                {
                    baseUrl += $"&c={c}";
                }
            }
            return baseUrl;
        }

        private static IEnumerator FetchHitokoto(string url)
        {
            using var request = UnityWebRequest.Get(url);

            yield return request.Send();

            if (request.isError)
            {
                hikotokoText = null;
                Debug.LogWarning("Error fetching hitokoto: " + request.error);
                yield break;
            }

            hikotokoText = request.downloadHandler.text;
        }
    }
}
