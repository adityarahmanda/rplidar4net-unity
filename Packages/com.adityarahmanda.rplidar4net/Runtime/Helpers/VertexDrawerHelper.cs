using UnityEngine;
using UnityEngine.UI;

namespace Rahmanda.RpLidar4Net
{
    public static class VertexDrawerHelper
    {
        public static void DrawRect(VertexHelper vh, Rect rect, Color color)
        {
            Vector2 bl = new Vector2(rect.xMin, rect.yMin);
            Vector2 br = new Vector2(rect.xMax, rect.yMin);
            Vector2 tr = new Vector2(rect.xMax, rect.yMax);
            Vector2 tl = new Vector2(rect.xMin, rect.yMax);

            var start = vh.currentVertCount;
            var v = UIVertex.simpleVert;
            v.color = color;
            v.position = tl;
            vh.AddVert(v);
            v.position = tr;
            vh.AddVert(v);
            v.position = br;
            vh.AddVert(v);
            v.position = bl;
            vh.AddVert(v);
            vh.AddTriangle(start + 0, start + 1, start + 2);
            vh.AddTriangle(start + 2, start + 3, start + 0);
        }
        
        public static void DrawLine(VertexHelper vh, Vector2 a, Vector2 b, float thickness, Color color)
        {
            var dir = (b - a).normalized;
            var normal = new Vector2(-dir.y, dir.x) * (thickness * 0.5f);
            var start = vh.currentVertCount;
            var v = UIVertex.simpleVert;
            v.color = color;
            v.position = a - normal;
            vh.AddVert(v);
            v.position = a + normal;
            vh.AddVert(v);
            v.position = b + normal;
            vh.AddVert(v);
            v.position = b - normal;
            vh.AddVert(v);
            vh.AddTriangle(start + 0, start + 1, start + 2);
            vh.AddTriangle(start + 2, start + 3, start + 0);
        }
    }
}
