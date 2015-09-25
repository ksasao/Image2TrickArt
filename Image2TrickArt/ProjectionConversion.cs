using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace Mpga.Imaging
{
    /// <summary>
    /// (0,0)-(0,q)-(p,q)-(p,0) で張られる長方形 A と
    /// 射影変換後の (X1,Y1)-(X2,Y2)-(X3,Y3)-(X4,Y4) の四角形 A' が既知のとき、
    /// A の任意座標を A' の対応する座標に変換するクラス
    /// </summary>
    public class ProjectionConversion
    {
        float _a1, _a2, _a3, _a4, _a5, _a6, _a7, _a8;

        public ProjectionConversion(float p, float q,
            float X1, float Y1, float X2, float Y2,
            float X3, float Y3, float X4, float Y4)
        {
            _a1 = -
                (-X2 * X4 * Y1 + X3 * X4 * Y1 + X1 * X3 * Y2 - X3 * X4 * Y2
                 -X1 * X2 * Y3 + X2 * X4 * Y3 + X1 * X2 * Y4 - X1 * X3 * Y4)
                / (p * (X3 * Y2 - X4 * Y2 - X2 * Y3 + X4 * Y3 + X2 * Y4 - X3 * Y4));


            _a2 = -
               (-X2 * X3 * Y1 + X2 * X4 * Y1 + X1 * X3 * Y2 - X1 * X4 * Y2
                +X1 * X4 * Y3 - X2 * X4 * Y3 - X1 * X3 * Y4 + X2 * X3 * Y4)
               / (q * (X3 * Y2 - X4 * Y2 - X2 * Y3 + X4 * Y3 + X2 * Y4 - X3 * Y4));
            _a3 = X1;

            _a4 = +
              (-X3 * Y1 * Y2 + X4 * Y1 * Y2 + X2 * Y1 * Y3 - X4 * Y1 * Y3
               -X1 * Y2 * Y4 + X3 * Y2 * Y4 + X1 * Y3 * Y4 - X2 * Y3 * Y4)
              / (p * (X3 * Y2 - X4 * Y2 - X2 * Y3 + X4 * Y3 + X2 * Y4 - X3 * Y4));

            _a5 = +
              (+X2 * Y1 * Y3 - X4 * Y1 * Y3 - X1 * Y2 * Y3 + X4 * Y2 * Y3
               -X2 * Y1 * Y4 + X3 * Y1 * Y4 + X1 * Y2 * Y4 - X3 * Y2 * Y4)
              / (q * (X3 * Y2 - X4 * Y2 - X2 * Y3 + X4 * Y3 + X2 * Y4 - X3 * Y4));
            _a6 = Y1;

            _a7 = +
              (X2 * Y1 - X3 * Y1 - X1 * Y2 + X4 * Y2 + X1 * Y3 - X4 * Y3 - X2 * Y4 + X3 * Y4)
              / (p * (X3 * Y2 - X4 * Y2 - X2 * Y3 + X4 * Y3 + X2 * Y4 - X3 * Y4));
            _a8 = +
              (X3 * Y1 - X4 * Y1 - X3 * Y2 + X4 * Y2 - X1 * Y3 + X2 * Y3 + X1 * Y4 - X2 * Y4)
              / (q * (X3 * Y2 - X4 * Y2 - X2 * Y3 + X4 * Y3 + X2 * Y4 - X3 * Y4));
        }

        /// <summary>
        /// 射影変換前の座標を射影変換後の座標に変換します。
        /// </summary>
        /// <param name="x">射影変換前のX座標</param>
        /// <param name="y">射影変換前のX座標</param>
        /// <returns>射影変換後の座標</returns>
        public PointF Convert(float x, float y)
        {
            float X = (_a1 * x + _a2 * y + _a3) / (_a7 * x + _a8 * y + 1);
            float Y = (_a4 * x + _a5 * y + _a6) / (_a7 * x + _a8 * y + 1);
            return new PointF(X, Y);
        }
    }
}
