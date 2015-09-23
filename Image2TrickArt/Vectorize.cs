using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace Mpga.Imaging
{

    public static class Vectorize
    {
        /// <summary>
        /// 点列のコーナー検出によるベクトル化
        /// </summary>
        /// <param name="points">ベクトル化対象の点列</param>
        /// <returns>ベクトル化結果</returns>
        public static Point[] Apply(Point[] points)
        {
            int size = points.Length;
            int radius = 1 + size / 10;
            double[] table = new double[points.Length];

            // ある点から少し離れた2点のなす角を求める
            for(int i=0; i< size; i++)
            {
                int p = (i + radius) % size;
                int m = (i - radius + size) % size;
                table[i] = GetAngle(points[p], points[i], points[m]);
            }

            List<Point> result = new List<Point>();

            // 極大値をコーナーとみなす
            for (int i = 0; i < size; i++)
            {
                int p = (i + 3) % size;
                int m = (i - 3 + size) % size;
                if( table[p] < table[i] && table[m] < table[i] && table[i] > -0.95)
                {
                    result.Add(points[i]);
                }
            }

            // 極大値が近い値を集めて重心を求める
            int c = result.Count;
            double minDist = 5;
            bool[] marked = new bool[c];

            List<Point> grouped = new List<Point>();
            for (int i=0; i< c - 1; i++)
            {
                int w = 1;
                int x = result[i].X;
                int y = result[i].Y;
                if (!marked[i])
                {
                    for (int j = i + 1; j < c; j++)
                    {
                        if (GetDistance(result[i], result[j]) < minDist)
                        {
                            marked[j] = true;
                            x += result[j].X;
                            y += result[j].Y;
                            w++;
                        }
                    }
                    grouped.Add(new Point(x / w, y / w));
                }
            }

            return grouped.ToArray();
        }

        private static double GetAngle(Point a, Point o, Point b)
        {
            int oaX = a.X - o.X;
            int obX = b.X - o.X;
            int oaY = a.Y - o.Y;
            int obY = b.Y - o.Y;

            // cosθを返す
            return (oaX * obX + oaY * obY) / Math.Sqrt(oaX * oaX + oaY * oaY) / Math.Sqrt(obX * obX + obY * obY);

        }

        private static double GetDistance(Point p1, Point p2)
        {
            int x = p1.X - p2.X;
            int y = p1.Y - p2.Y;
            return Math.Sqrt(x * x + y * y);
        }

    }
}
