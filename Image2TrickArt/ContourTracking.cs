using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace Mpga.Imaging
{
    public static class ContourTracking
    {

        /// <summary>
        /// 黒背景に描画された図形の8方向の輪郭追跡を行います。輪郭は最初に見つけたもの
        /// のみ返します。輪郭が見つからなかった場合は空の配列を返します。
        /// </summary>
        /// <remarks>ディジタル画像処理 改定新版 p.184 参照</remarks>
        /// <param name="bmp">輪郭追跡対象</param>
        /// <returns>輪郭を構成する点</returns>
        public static Point[] Apply(Bitmap bmp)
        {
            List<Point> points = new List<Point>();

            int w = bmp.Width;
            int h = bmp.Height;
            byte[] data = BitmapUtil.BitmapToByteArray(bmp);

            ClearEdge(w, h, data);

            int start = (1 + w) * 4;         // (1,1)の点を始点とする
            int end = (w - 1) * (h - 1) * 4; // (width-2,height-2)の点を終点とする
            int[] vectorCode = new int[] {
                -w * 4 - 4,
                -w * 4,
                -w * 4 + 4,
                4,
                w * 4 + 4,
                w * 4,
                w * 4 - 4,
                -4 };

            int e = 0; // 侵入方向
            int p = start;
            int lastStart = 0;

            while (p < end)
            {
                // ラスタスキャンで輪郭を発見
                if (data[p] > 0)
                {
                    lastStart = p;
                    // 輪郭追跡処理
                    while (p < end)
                    {
                        // 追跡済みフラグを立てる
                        data[p + 1] = 1;

                        // 輪郭点を追加
                        int x = (p / 4) % w;
                        int y = (p / 4) / w;
                        points.Add(new Point(x, y));

                        // 次に移動する方向
                        int v = GetNextVector(data, p, e, vectorCode);
                        if (v < 0)
                        {
                            p = end;
                            break;
                        }

                        // 次の点に移動する
                        p = p + vectorCode[v];

                        // 侵入方向を求める(画像の右方向への移動を0とした向き)
                        e = (v + 5) % vectorCode.Length;

                        // この点の次に移動する点
                        var vv = GetNextVector(data, p, e, vectorCode);
                        if (vv < 0)
                        {
                            p = end;
                            break;
                        }
                        int u = p + vectorCode[vv];

                        if (p == lastStart && data[u+1] > 0)
                        {
                            // 開始点かつ次の移動点が追跡済みなら完了
                            p = end;
                            break;
                        }

                    }
                }
                p = p + 4;
            }

            return points.ToArray();
        }

        // 輪郭追跡のために外周を0埋めする
        // 0以外の部分を輪郭とみなす
        private static void ClearEdge(int w, int h, byte[] data)
        {
            int under = w * (h - 1) * 4;
            int right = (w - 1) * 4;
            for (int i = 0; i < w * 4; i += 4)
            {
                data[i] = data[i + under] = 0;
                data[i+1] = data[i + under+1] = 0;
                data[i+2] = data[i + under+2] = 0;
            }
            for (int i = 0; i < w * h; i += w * 4)
            {
                data[i] = data[i + right] = 0;
                data[i+1] = data[i + right+1] = 0;
                data[i+2] = data[i + right+2] = 0;
            }

            // 2値化および追跡済みフラグをクリアする
            // 透過色が設定されている部分は黒とみなす
            for (int i = 0; i < w * h * 4; i += 4)
            {
                data[i] = (data[i] + data[i+1] + data[i+2] > 128 * 3 && data[i+3]  == 255) ? (byte)255 : (byte)0;
                data[i+1] = 0;
            }
        }

        private static int GetNextVector(byte[] data, int p, int e, int[] vectorCode)
        {
            int result = -1;
            for (int i = 0; i < vectorCode.Length; i++)
            {
                int v = (i + e) % vectorCode.Length;
                int t = p + vectorCode[v];
                if (data[t] > 0)
                {
                    result = v;
                    break;
                }
            }
            return result;
        }
    }

    
}
