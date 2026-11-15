using System;
using BepInEx.Bootstrap;
using KingdomFoundation.Api;
using UnityEngine;
#if HAS_UNDO
using KingdomIncUndo;
#endif

namespace KingdomContainerColors
{
    internal static class UndoIntegration
    {
        const string UndoPluginId = "urakka.kingdominc.undo";

        static bool Available => Chainloader.PluginInfos.ContainsKey(UndoPluginId);

        internal static IDisposable Register()
        {
#if HAS_UNDO
            if (!Available)
                return null;
            return RegisterCore();
#else
            return null;
#endif
        }

        internal static void RunEdit(Action action)
        {
#if HAS_UNDO
            if (Available)
            {
                RunEditCore(action);
                return;
            }
#endif
            action();
        }

        internal static IDisposable OpenEditScope()
        {
#if HAS_UNDO
            if (Available)
                return OpenEditScopeCore();
#endif
            return null;
        }

#if HAS_UNDO
        static IDisposable RegisterCore()
        {
            return UndoApi.RegisterObjectExtension(
                Plugin.Id,
                obj =>
                {
                    if (!ColorData.Supports(obj) || !ColorData.TryGet(obj, out var color))
                        return null;
                    return Pack(color);
                },
                (obj, state) =>
                {
                    if (!ColorData.Supports(obj))
                        return;
                    if (state is int packed)
                        ColorData.SetRaw(obj, Unpack(packed));
                    else
                        ColorData.Clear(obj);
                    if (obj.View != null)
                        FoundationApi.Decorations.RefreshStyle(obj.View);
                });
        }

        static void RunEditCore(Action action) => UndoApi.RunEdit(action);
        static IDisposable OpenEditScopeCore() => UndoApi.OpenEditScope();

        static int Pack(Color32 color) => (color.r << 24) | (color.g << 16) | (color.b << 8) | color.a;
        static Color32 Unpack(int packed) => new Color32((byte)(packed >> 24), (byte)(packed >> 16), (byte)(packed >> 8), (byte)packed);
#endif
    }
}

