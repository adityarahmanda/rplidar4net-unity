using RPLidar4Net.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Rahmanda.RpLidar4Net
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class RPLidarVisualizer : MaskableGraphic
    {
        [SerializeField] private RPLidar lidar;

        public float scale = 1f;
        public float rotation = 90f;
        public float lineThickness = 1f;
        public Color lineColor = Color.green;

        private Point[] m_points;

        protected override void Awake()
        {
            base.Awake();

            if (lidar != null)
                SetLidar(lidar);
        }

        public void SetLidar(RPLidar newLidar)
        {
            if (lidar != null)
                lidar.onScanFrame.RemoveListener(OnLidarScanFrame);

            lidar = newLidar;

            if (lidar != null)
                lidar.onScanFrame.AddListener(OnLidarScanFrame);
        }

        private void OnLidarScanFrame(Point[] points)
        {
            m_points = points;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            VertexDrawerHelper.DrawRect(vh, rectTransform.rect, color);

            if (m_points == null || m_points.Length == 0) return;

            Vector2 center = new Vector2(0, 0);
            foreach (var point in m_points)
            {
                Vector2 pointVector2 = Vector2Helper.ToVector2(center, rotation, point) * scale;
                VertexDrawerHelper.DrawLine(vh, center, pointVector2, lineThickness, lineColor);
            }
        }
    }
}