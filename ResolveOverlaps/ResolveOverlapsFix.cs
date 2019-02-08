using UnityEngine;
using ICities;
using ColossalFramework.UI;

namespace ResolveOverlaps
{
    public class ResolveOverlapsFix : ThreadingExtensionBase
    {
        // When one presses "ESC" they expect tools to be cleared. This fix returns that functionality.
        public override void OnAfterSimulationTick()
        {
            if ((Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Return)) && ResolveOverlapsTool.instance != null && ToolsModifierControl.GetTool<ResolveOverlapsTool>())
            {
                ToolsModifierControl.GetTool<ResolveOverlapsTool>().enabled = false;
                UIView.Find("E3A")?.Unfocus();
                UIView.Find("E3B")?.Unfocus();
                ToolsModifierControl.SetTool<DefaultTool>();
            }

            // The strangest bugfix known to man
            if (ToolsModifierControl.toolController.CurrentTool == null)
            {
                ToolsModifierControl.SetTool<DefaultTool>();
            }
        }
    }
}
