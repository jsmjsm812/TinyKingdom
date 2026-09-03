using System.Collections.Generic;
using UnityEngine;

namespace WitchHour.Field
{
    /// <summary>
    /// 침입자가 따라가는 경유점 목록. ㄹ자 — 우→하→좌→하→우로 꺾인다.
    /// 가로 선반(Shelf) 3개를 세로 낙하(Drop) 2개로 잇는다. 연속된 두 경유점이
    /// Y가 같으면 선반 구간(가로 이동), X가 같으면 낙하 구간(세로 이동)이라
    /// 별도 꺾임 처리 없이 각 선반의 시작/끝점만 순서대로 나열하면 된다.
    /// GridManager도 같은 ShelfY/ShelfHalfWidth 값을 참조해서 슬롯을 배치한다.
    /// </summary>
    public static class LanePath
    {
        private static readonly Vector2[] CachedWaypoints = BuildWaypoints();
        private static readonly float[] CachedCumulativeDistances = BuildCumulativeDistances();

        public static IReadOnlyList<Vector2> Waypoints => CachedWaypoints;
        public static IReadOnlyList<float> CumulativeDistances => CachedCumulativeDistances;
        public static Vector2 StartPoint => CachedWaypoints[0];

        private static Vector2[] BuildWaypoints()
        {
            var points = new List<Vector2>();
            float[] shelfY = FieldConstants.ShelfY;
            float halfWidth = FieldConstants.ShelfHalfWidth;

            for (int i = 0; i < shelfY.Length; i++)
            {
                bool leftToRight = i % 2 == 0;
                float startX = leftToRight ? -halfWidth : halfWidth;
                float endX = leftToRight ? halfWidth : -halfWidth;

                points.Add(new Vector2(startX, shelfY[i]));
                points.Add(new Vector2(endX, shelfY[i]));
            }

            return points.ToArray();
        }

        // 선반 구간은 Y가 그대로라 "Y가 작을수록 성벽에 가깝다"는 더 이상 성립하지 않는다.
        // 대신 경로를 따라 이동한 누적 거리로 "얼마나 진행했는지"를 판단한다(GuardianUnit 타겟 우선순위용).
        private static float[] BuildCumulativeDistances()
        {
            var distances = new float[CachedWaypoints.Length];
            for (int i = 1; i < CachedWaypoints.Length; i++)
                distances[i] = distances[i - 1] + Vector2.Distance(CachedWaypoints[i - 1], CachedWaypoints[i]);
            return distances;
        }
    }
}
