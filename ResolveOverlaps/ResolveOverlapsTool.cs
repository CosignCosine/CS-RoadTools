using ICities;
using ColossalFramework;
using ColossalFramework.Math;
using UnityEngine;
using System.Collections.Generic;
using ColossalFramework.UI;

namespace ResolveOverlaps
{
    public class ResolveOverlapsTool : ToolBase
    {
        public static ResolveOverlapsTool instance;
        public int mode = 0; // Mode; 0 = Intersection, 1 = Insertion
        public float m_minSegmentLength = 3f; // This is arbitrary and is subject to change.

        // Quality of life functions
        public NetManager Manager { get { return Singleton<NetManager>.instance; } }
        public NetNode GetNode(ushort id){ return Manager.m_nodes.m_buffer[id]; }
        public NetSegment GetSegment(ushort id){ return Manager.m_segments.m_buffer[id]; }

        public ushort m_seg1;
        public ushort m_seg2;
        public ushort m_hover;
        public Vector3 m_snapFakeNode;
        public bool m_errors;
        Color hcolor = new Color32(0, 181, 255, 255);
        Color scolor = new Color32(95, 166, 0, 244);
        int pulsating = 0;
        public ushort m_newSeg1;
        public ushort m_newSeg2;

        public bool ThrowError(string message)
        {
            ExceptionPanel panel = UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel");
            panel.SetMessage("Road Tools", message, false);
            return false;
        }

        public bool NodeInsertion(ushort segment, float time){ // time is in the range (0, 1) inclusive
            Bezier3 bezier = new Bezier3();
            NetSegment s1 = GetSegment(segment);
            bezier.a = NetManager.instance.m_nodes.m_buffer[s1.m_startNode].m_position;
            bezier.d = NetManager.instance.m_nodes.m_buffer[s1.m_endNode].m_position;

            bool smoothStart = (Singleton<NetManager>.instance.m_nodes.m_buffer[s1.m_startNode].m_flags & NetNode.Flags.Middle) != NetNode.Flags.None;
            bool smoothEnd = (Singleton<NetManager>.instance.m_nodes.m_buffer[s1.m_endNode].m_flags & NetNode.Flags.Middle) != NetNode.Flags.None;

            NetSegment.CalculateMiddlePoints(bezier.a, s1.m_startDirection, bezier.d, s1.m_endDirection, smoothStart, smoothEnd, out Vector3 b, out Vector3 c);

            bezier.b = b;
            bezier.c = c;

            Vector3 timePoint = bezier.Position(time);

            ushort seg1ls = s1.m_startNode;
            Vector3 seg1lsd = s1.m_startDirection;
            Vector3 seg1led = -bezier.Tangent(time).normalized;

            ushort seg1rs = s1.m_endNode;
            Vector3 seg1rsd = s1.m_endDirection;
            Vector3 seg1red = bezier.Tangent(time).normalized;

            Manager.CreateNode(out ushort timePointNode, ref SimulationManager.instance.m_randomizer, s1.Info, timePoint, SimulationManager.instance.m_currentBuildIndex++);
            Manager.CreateSegment(out ushort seg1, ref SimulationManager.instance.m_randomizer, s1.Info, timePointNode, seg1ls, seg1led, seg1lsd, SimulationManager.instance.m_currentBuildIndex++, SimulationManager.instance.m_currentBuildIndex-1, false);
            Manager.CreateSegment(out ushort seg2, ref SimulationManager.instance.m_randomizer, s1.Info, seg1rs, timePointNode, seg1rsd, seg1red, SimulationManager.instance.m_currentBuildIndex++, SimulationManager.instance.m_currentBuildIndex-1, false);

            Manager.UpdateNode(timePointNode);
            Manager.UpdateSegment(seg1);
            Manager.UpdateSegment(seg2);

            Manager.ReleaseSegment(segment, true);
            m_newSeg1 = seg1;
            m_newSeg2 = seg2;
            return true;
        }

        public ushort InvertSegment(ushort segment, bool invertDirection = false)
        {
            NetSegment segLiteral = GetSegment(segment);
            Manager.CreateSegment(out ushort flippedSegment, ref SimulationManager.instance.m_randomizer, segLiteral.Info, segLiteral.m_endNode, segLiteral.m_startNode, segLiteral.m_endDirection, segLiteral.m_startDirection, SimulationManager.instance.m_currentBuildIndex+1, SimulationManager.instance.m_currentBuildIndex, false);
            Manager.ReleaseSegment(segment, true);
            Manager.UpdateSegment(flippedSegment);
            Manager.UpdateSegmentFlags(flippedSegment);

            SimulationManager.instance.m_currentBuildIndex++;
            return flippedSegment;
        }

        public bool Intersection(ushort segment1, ushort segment2){
            Bezier3 bezier = new Bezier3();
            Bezier3 bezier2  = new Bezier3();
            NetSegment s1 = GetSegment(segment1);
            NetSegment s2 = GetSegment(segment2);

            // Turn the segment data into a Bezier2 for easier calculations supported by the game
            bezier.a = NetManager.instance.m_nodes.m_buffer[s1.m_startNode].m_position;
            bezier.d = NetManager.instance.m_nodes.m_buffer[s1.m_endNode].m_position;

            bool smoothStart = (Singleton<NetManager>.instance.m_nodes.m_buffer[s1.m_startNode].m_flags & NetNode.Flags.Middle) != NetNode.Flags.None;
            bool smoothEnd = (Singleton<NetManager>.instance.m_nodes.m_buffer[s1.m_endNode].m_flags & NetNode.Flags.Middle) != NetNode.Flags.None;

            NetSegment.CalculateMiddlePoints(bezier.a, s1.m_startDirection, bezier.d, s1.m_endDirection, smoothStart, smoothEnd, out Vector3 b, out Vector3 c);

            bezier.b = b;
            bezier.c = c;

            Bezier2 xz = Bezier2.XZ(bezier);

            // Second segment:
            bezier2.a = NetManager.instance.m_nodes.m_buffer[s2.m_startNode].m_position;
            bezier2.d = NetManager.instance.m_nodes.m_buffer[s2.m_endNode].m_position;

            smoothStart = (Singleton<NetManager>.instance.m_nodes.m_buffer[s2.m_startNode].m_flags & NetNode.Flags.Middle) != NetNode.Flags.None;
            smoothEnd = (Singleton<NetManager>.instance.m_nodes.m_buffer[s2.m_endNode].m_flags & NetNode.Flags.Middle) != NetNode.Flags.None;

            NetSegment.CalculateMiddlePoints(bezier2.a, s2.m_startDirection, bezier2.d, s2.m_endDirection, smoothStart, smoothEnd, out Vector3 _b, out Vector3 _c);

            bezier2.b = _b;
            bezier2.c = _c;

            Bezier2 xz2 = Bezier2.XZ(bezier2);

            if (!xz.Intersect(xz2, out float t1, out float t2, 8))
            {
                
                return ThrowError("Could not find an intersection between these two roads. Remember, T-junctions do not count as intersections. Try extending the segment, creating the intersection, and deleting the excess.");
            }

            Vector3 intersectionPoint = bezier.Position(t1);

            ushort seg1ls = s1.m_startNode;
            Vector3 seg1lsd = s1.m_startDirection;
            Vector3 seg1led = -bezier.Tangent(t1).normalized;

            ushort seg1rs = s1.m_endNode;
            Vector3 seg1rsd = s1.m_endDirection;
            Vector3 seg1red = bezier.Tangent(t1).normalized;

            ushort seg2ls = s2.m_startNode;
            Vector3 seg2lsd = s2.m_startDirection;
            Vector3 seg2led = -bezier2.Tangent(t2).normalized;

            ushort seg2rs = s2.m_endNode;
            Vector3 seg2rsd = s2.m_endDirection;
            Vector3 seg2red = bezier2.Tangent(t2).normalized;


            // place segments and nodes
            Manager.CreateNode(out ushort intersectionNode, ref SimulationManager.instance.m_randomizer, s1.Info, intersectionPoint, SimulationManager.instance.m_currentBuildIndex);
            Manager.CreateSegment(out ushort seg1, ref SimulationManager.instance.m_randomizer, s1.Info, seg1ls, intersectionNode, seg1lsd, seg1led, SimulationManager.instance.m_currentBuildIndex, SimulationManager.instance.m_currentBuildIndex, false);
            Manager.CreateSegment(out ushort seg2, ref SimulationManager.instance.m_randomizer, s1.Info, seg1rs, intersectionNode, seg1rsd, seg1red, SimulationManager.instance.m_currentBuildIndex, SimulationManager.instance.m_currentBuildIndex, false);
            Manager.CreateSegment(out ushort seg3, ref SimulationManager.instance.m_randomizer, s2.Info, seg2ls, intersectionNode, seg2lsd, seg2led, SimulationManager.instance.m_currentBuildIndex, SimulationManager.instance.m_currentBuildIndex, false);
            Manager.CreateSegment(out ushort seg4, ref SimulationManager.instance.m_randomizer, s2.Info, seg2rs, intersectionNode, seg2rsd, seg2red, SimulationManager.instance.m_currentBuildIndex, SimulationManager.instance.m_currentBuildIndex, false);

            List<ushort> newSegments = new List<ushort>();
            newSegments.Add(seg1);
            newSegments.Add(seg2);
            newSegments.Add(seg3);
            newSegments.Add(seg4);

            if (seg1 == 0 || seg2 == 0 || seg3 == 0 || seg4 == 0) return Revert(newSegments, intersectionNode);

            Manager.ReleaseSegment(segment1, true);
            Manager.ReleaseSegment(segment2, true);

            FixTJunctions(newSegments);
            return true;
        }

        public bool Revert(List<ushort> s, ushort n) {
            for (int i = 0; i < s.Count; i++){
                Manager.ReleaseSegment(s[i], true);
            }
            Manager.ReleaseNode(n);
            return false;
        }

        public void FixTJunctions(List<ushort> m_segments) {
            for (int i = 0; i < m_segments.Count; i++){
                ushort segmentID = m_segments[i];
                NetSegment segment = GetSegment(segmentID);
                Debug.Log(segment.m_averageLength);
                if(segment.m_averageLength <= m_minSegmentLength){
                    //Manager.ReleaseSegment(segmentID, false);
                }
            }
        }

        // Essential Tool Functions
        protected override void OnToolUpdate()
        {
            base.OnToolUpdate();

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastInput input = new RaycastInput(ray, Camera.main.farClipPlane);
            input.m_ignoreSegmentFlags = NetSegment.Flags.None;
            input.m_ignoreNodeFlags = NetNode.Flags.All;
            input.m_ignoreParkFlags = DistrictPark.Flags.All;
            input.m_ignorePropFlags = PropInstance.Flags.All;
            input.m_ignoreTreeFlags = TreeInstance.Flags.All;
            input.m_ignoreCitizenFlags = CitizenInstance.Flags.All;
            input.m_ignoreVehicleFlags = Vehicle.Flags.Created;
            input.m_ignoreBuildingFlags = Building.Flags.All;
            input.m_ignoreDisasterFlags = DisasterData.Flags.All;
            input.m_ignoreTransportFlags = TransportLine.Flags.All;
            input.m_ignoreParkedVehicleFlags = VehicleParked.Flags.All;
            input.m_ignoreTerrain = true;
            RayCast(input, out RaycastOutput output);
            m_hover = output.m_netSegment;

            if(m_newSeg1 != 0 && m_newSeg2 != 0){
                NetSegment s1 = GetSegment(m_newSeg1);
                NetSegment s2 = GetSegment(m_newSeg2);

                NetNode a = GetNode(s1.m_startNode);
                NetNode b = GetNode(s2.m_endNode);
                NetNode c = GetNode(s2.m_startNode);

                if((a.m_problems | b.m_problems | c.m_problems) == Notification.Problem.RoadNotConnected){
                    InvertSegment(m_newSeg1);
                    InvertSegment(m_newSeg2);
                }

                m_newSeg1 = 0;
                m_newSeg2 = 0;
                enabled = false;
                UIView.Find("E3A").Unfocus();
                UIView.Find("E3B").Unfocus();
                mode = 0;
                ToolsModifierControl.SetTool<DefaultTool>();
            }

            if (m_hover != 0)
            {
                if(mode == 1){
                    //Snap to location on segment
                    Bezier3 bezierx = new Bezier3();
                    NetSegment seg = GetSegment(m_hover);
                    bezierx.a = NetManager.instance.m_nodes.m_buffer[seg.m_startNode].m_position;
                    bezierx.d = NetManager.instance.m_nodes.m_buffer[seg.m_endNode].m_position;

                    bool smoothStart = (Singleton<NetManager>.instance.m_nodes.m_buffer[seg.m_startNode].m_flags & NetNode.Flags.Middle) != NetNode.Flags.None;
                    bool smoothEnd = (Singleton<NetManager>.instance.m_nodes.m_buffer[seg.m_endNode].m_flags & NetNode.Flags.Middle) != NetNode.Flags.None;

                    NetSegment.CalculateMiddlePoints(bezierx.a, seg.m_startDirection, bezierx.d, seg.m_endDirection, smoothStart, smoothEnd, out Vector3 b, out Vector3 c);

                    bezierx.b = b;
                    bezierx.c = c;

                    float iterations = 40f; // Could be anywhere 0-100. Maybe configurable at a later point.
                    float time = 0f;
                    float shortestDistance = 1000f;
                    for (float i = 0f; i < iterations; i++)
                    {
                        float ttime = (i+1f)/iterations;
                        Vector3 testPoint = bezierx.Position(ttime);
                        float distance = Vector3.Distance(testPoint, output.m_hitPos);
                        if(distance < shortestDistance){
                            shortestDistance = distance;
                            time = ttime;
                            m_snapFakeNode = testPoint;
                        }
                    }

                    // If x < 0.1f or x > 0.9f, display a warning telling people the segment may start to glitch out? perhaps over a certain length? <0.1m?
                    m_errors = time * seg.m_averageLength < 3.0f || seg.m_averageLength - (time * seg.m_averageLength) < 3.0f;
                    pulsating++;

                    if(Input.GetMouseButtonUp(0) && Mathf.Abs(0.5f - time) < 0.5){
                        if (!NodeInsertion(m_hover, time))
                        {
                            Debug.LogError(":(");
                            return;
                        }
                    }


                }else{
                    if (Input.GetMouseButtonUp(0))
                    {
                        if (m_seg1 == 0)
                        {
                            m_seg1 = m_hover;
                        }
                        else
                        {
                            m_seg2 = m_hover;
                        }
                    }
                    else if (Input.GetMouseButtonUp(1))
                    {
                        if (m_seg2 != 0)
                        {
                            m_seg2 = 0;
                        }
                        else if (m_seg1 != 0)
                        {
                            m_seg1 = 0;
                        }
                    }
                }

            }

            if(Input.GetKeyUp(KeyCode.Return)){
                if(!Intersection(m_seg1, m_seg2)){
                    Debug.LogError(":(");
                }
                enabled = false;
                ToolsModifierControl.SetTool<DefaultTool>();
                m_seg1 = 0;
                m_seg2 = 0;
                UIView.Find("E3A").Unfocus();
                UIView.Find("E3B").Unfocus();
            }
        }

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            base.RenderOverlay(cameraInfo);

            if (enabled == true)
            {
                if (m_seg1 != 0)
                {
                    NetSegment hoveredSegment = GetSegment(m_seg1);
                    NetTool.RenderOverlay(cameraInfo, ref hoveredSegment, scolor, new Color(1f, 0f, 0f, 1f));
                }

                if (m_seg2 != 0)
                {
                    NetSegment hoveredSegment = GetSegment(m_seg2);
                    NetTool.RenderOverlay(cameraInfo, ref hoveredSegment, scolor, new Color(1f, 0f, 0f, 1f));
                }
                if (m_hover != 0)
                {
                    NetSegment hoveredSegment = GetSegment(m_hover);
                    NetTool.RenderOverlay(cameraInfo, ref hoveredSegment, m_errors ? Color.Lerp(hcolor, new Color(1f, 0f, 0f, 1f), (float) Mathf.Sin(pulsating/5)/2 + 0.5f) : hcolor, new Color(1f, 0f, 0f, 1f));
                    if(m_snapFakeNode != Vector3.zero){
                        RenderManager.instance.OverlayEffect.DrawCircle(cameraInfo, hcolor, m_snapFakeNode, 10f, m_snapFakeNode.y - 1f, m_snapFakeNode.y + 1f, true, true);
                    }
                }
                //if(true) { // @TODO find circle place???
                //    Singleton<RenderManager>.instance.OverlayEffect.DrawCircle(cameraInfo, new Color(), new Vector3(), 5f)
                //}
            }
        }
    }
}
