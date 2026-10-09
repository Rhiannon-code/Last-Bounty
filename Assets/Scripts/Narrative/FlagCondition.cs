using System;
using UnityEngine;

namespace FPSParkour.Narrative
{
    [Serializable]
    public class FlagCondition
    {
        public string[] RequireAll;
        public string[] RequireNone;
        public string[] RequireAny;

        public bool IsMet(StoryFlags flags)
        {
            if (flags == null)
                return true;

            if (RequireAll != null)
            {
                foreach (string flag in RequireAll)
                {
                    if (!string.IsNullOrEmpty(flag) && !flags.Has(flag))
                        return false;
                }
            }

            if (RequireNone != null)
            {
                foreach (string flag in RequireNone)
                {
                    if (!string.IsNullOrEmpty(flag) && flags.Has(flag))
                        return false;
                }
            }

            if (RequireAny == null || RequireAny.Length == 0)
                return true;

            foreach (string flag in RequireAny)
            {
                if (!string.IsNullOrEmpty(flag) && flags.Has(flag))
                    return true;
            }

            return false;
        }
    }
}
