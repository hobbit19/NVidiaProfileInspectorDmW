﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using nspector.Native.NVAPI2;
using nvw = nspector.Native.NVAPI2.NvapiDrsWrapper;

namespace nspector.Common
{
    public class DrsSessionScope
    {

        private static volatile IntPtr GlobalSession; // Modified by DeadManWalking

        public static volatile bool HoldSession = true;

        private static object _Sync = new object();


        public static T DrsSession<T>(Func<IntPtr, T> action)
        {
            lock (_Sync)
            {
                if (HoldSession && (GlobalSession == IntPtr.Zero))
                {

#pragma warning disable CS0420
                    var csRes = nvw.DRS_CreateSession(ref GlobalSession);
#pragma warning restore CS0420

                    if (csRes != NvAPI_Status.NVAPI_OK)
                        throw new NvapiException("DRS_CreateSession", csRes);

                    var nvRes = nvw.DRS_LoadSettings(GlobalSession);
                    if (nvRes != NvAPI_Status.NVAPI_OK)
                        throw new NvapiException("DRS_LoadSettings", nvRes);
                }
            }

            if (HoldSession && GlobalSession != IntPtr.Zero)
            {
                return action(GlobalSession);
            }

            return NonGlobalDrsSession<T>(action);
        }

        public static void DestroyGlobalSession()
        {
            lock (_Sync)
            {
                if (GlobalSession != IntPtr.Zero)
                {
                    var csRes = nvw.DRS_DestroySession(GlobalSession);
                    GlobalSession = IntPtr.Zero;
                }
            }
        }

        private static T NonGlobalDrsSession<T>(Func<IntPtr, T> action)
        {
            IntPtr hSession = IntPtr.Zero;
            var csRes = nvw.DRS_CreateSession(ref hSession);
            if (csRes != NvAPI_Status.NVAPI_OK)
                throw new NvapiException("DRS_CreateSession", csRes);

            try
            {
                var nvRes = nvw.DRS_LoadSettings(hSession);
                if (nvRes != NvAPI_Status.NVAPI_OK)
                    throw new NvapiException("DRS_LoadSettings", nvRes);

                return action(hSession);
            }
            finally
            {
                var nvRes = nvw.DRS_DestroySession(hSession);
                if (nvRes != NvAPI_Status.NVAPI_OK)
                    throw new NvapiException("DRS_DestroySession", nvRes);
            }

        }

        
    }
}
