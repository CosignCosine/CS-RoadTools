using ICities;
using ColossalFramework.Plugins;
using System;
using System.Linq;

namespace ResolveOverlaps
{
    public class ResolveOverlapsMod : IUserMod
    {
        public string Name => "Elektrix's Road Tools";
        public string Description => "Fixes overlapping roads, allows addition of new nodes at arbitrary positions, and allows flipping segments.";

        // Once again (x3) this code is taken directly from Network Skins and is adjusted for context. 
        // https://github.com/boformer/NetworkSkins/blob/master/NetworkSkins/NetworkSkinsMod.cs
        public static string AsmPath => PluginInfo.modPath;
        private static PluginManager.PluginInfo PluginInfo
        {
            get
            {
                var pluginManager = PluginManager.instance;
                var plugins = pluginManager.GetPluginsInfo();

                foreach (var item in plugins)
                {
                    try
                    {
                        var instances = item.GetInstances<IUserMod>();
                        if (!(instances.FirstOrDefault() is ResolveOverlapsMod))
                        {
                            continue;
                        }
                        return item;
                    }
                    catch
                    {

                    }
                }
                throw new Exception("Could not find assembly");

            }
        }

        public void OnSettingsUI(UIHelperBase helper)
        {
            // Load the configuration
            ElektrixModsConfiguration config = Configuration<ElektrixModsConfiguration>.Load();

            helper.AddButton("Reset Button Location to (500, 500)", () =>
            {
                config.PanelX = 500;
                config.PanelY = 500;
                Configuration<ElektrixModsConfiguration>.Save();
            });
        }
    }
}
