using Mpga.Imaging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Image2TrickArt
{
    public partial class MainForm : Form
    {
        Bitmap bmp = null;
        Bitmap proj = null;
        Bitmap nullBmp = new Bitmap(1, 1);
        public MainForm()
        {
            InitializeComponent();
        }

        private void InitializeImage(string filename)
        {
            ClearPictureBoxes();

            // 1ピクセル=4バイトのフォーマットに変換
            try
            {
                ConvertToARGBImage(filename);
            }
            catch (Exception)
            {
                ErrorDialog();
                return;
            }

            var data = ContourTracking.Apply(bmp);
            var poly = Vectorize.Apply(data);

            // DebugDraw(data, poly);

            this.pictureBox1.Image = bmp;

            if (poly.Length == 4)
            {
                CreateTrickArt(filename, poly, 2480, 3508);
            }
            else
            {
                ErrorDialog();
            }
            GC.Collect();
        }

        private void ClearPictureBoxes()
        {
            // 表示済みの画像を消去
            if (bmp != null)
            {
                this.pictureBox1.Image = nullBmp;
                bmp.Dispose();
            }
            if (proj != null)
            {
                this.pictureBox2.Image = nullBmp;
                proj.Dispose();
            }
        }

        private void ConvertToARGBImage(string filename)
        {
            Bitmap b = new Bitmap(filename);
            bmp = new Bitmap(b.Width, b.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Black);
                g.DrawImageUnscaledAndClipped(b, new Rectangle(0, 0, b.Width, b.Height));
            }
            b.Dispose();
        }

        private void CreateTrickArt(string filename, Point[] poly, int width, int height)
        {
            int w = width;
            int h = height;

            ProjectionConversion conv = null;

            conv = GetProjection(poly, w, h);

            proj = new Bitmap(w, h);
            byte[] projection = BitmapUtil.BitmapToByteArray(proj);
            byte[] original = BitmapUtil.BitmapToByteArray(bmp);
            int w2 = bmp.Width;
            Parallel.For(0, h, y =>
            {
                for (int x = 0; x < w; x++)
                {
                    int dest = (int)(x + (h - y - 1) * w) * 4;
                    PointF c = conv.Convert(x, y);

                    // バイリニア補間
                    float dx = (float) (c.X - Math.Truncate(c.X));
                    float dy = (float) (c.Y - Math.Truncate(c.Y));
                    int src0 = (int)(((int)c.X) + ((int)c.Y) * w2) * 4;
                    int src1 = src0 + 4;
                    int src2 = src0 + w2 * 4;
                    int src3 = src0 + 4 + w2 * 4;
                    if (0 <= src0 && src3 < original.Length)
                    {
                        float b0 = original[src0++] * (1.0f - dx) + original[src1++] * dx;
                        float g0 = original[src0++] * (1.0f - dx) + original[src1++] * dx;
                        float r0 = original[src0++] * (1.0f - dx) + original[src1++] * dx;
                        float a0 = original[src0  ] * (1.0f - dx) + original[src1  ] * dx;
                        float b1 = original[src2++] * (1.0f - dx) + original[src3++] * dx;
                        float g1 = original[src2++] * (1.0f - dx) + original[src3++] * dx;
                        float r1 = original[src2++] * (1.0f - dx) + original[src3++] * dx;
                        float a1 = original[src2  ] * (1.0f - dx) + original[src3  ] * dx;

                        projection[dest++] = (byte)(b0 * (1.0f - dy) + b1 * dy);
                        projection[dest++] = (byte)(g0 * (1.0f - dy) + g1 * dy);
                        projection[dest++] = (byte)(r0 * (1.0f - dy) + r1 * dy);
                        projection[dest  ] = (byte)(a0 * (1.0f - dy) + a1 * dy);
                    }
                }
            });
            BitmapUtil.ByteArrayToBitmap(projection, proj);


            using (Graphics g = Graphics.FromImage(proj))
            {
                Pen pen = new Pen(Brushes.White, 20);
                g.DrawRectangle(pen, new Rectangle(0, 0, proj.Width, proj.Height));
                pen.Dispose();
            }
            try
            {
                proj.Save(filename + ".png");
            }
            catch (Exception)
            {
                MessageBox.Show("ファイルが作成できませんでした。既にファイルを開いていないかどうかを確認してください。", "書き込み失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            this.pictureBox2.Image = proj;
        }

        private static ProjectionConversion GetProjection(Point[] poly, int w, int h)
        {
            ProjectionConversion conv;

            // ラスタスキャンで輪郭を追跡する場合、右上の角と左上の角のどちらを
            // 見つけるかは入力画像に依存するため、どちらが先に見つかっても
            // 正しく変換できるように点列を並び替えて初期化する

            // 4点の重心のX座標を求める
            int gx = 0;
            for (int i = 0; i < 4; i++)
            {
                gx += poly[i].X;
            }
            gx /= 4;

            // はじめに見つかった点が重心より右か左かによって処理を分岐
            if (poly[0].X > gx)
            {
                conv = new ProjectionConversion(w, h,
                    poly[2].X, poly[2].Y, poly[3].X, poly[3].Y,
                    poly[0].X, poly[0].Y, poly[1].X, poly[1].Y);
            }
            else
            {
                conv = new ProjectionConversion(w, h,
                    poly[3].X, poly[3].Y, poly[0].X, poly[0].Y,
                    poly[1].X, poly[1].Y, poly[2].X, poly[2].Y);
            }

            return conv;
        }

        private void ErrorDialog()
        {
            MessageBox.Show("解析に失敗しました。\r\n黒い背景の中に白い四角形がある画像ファイルのみ有効です。", "解析失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void DebugDraw(Point[] data, Point[] poly)
        {
            using (Graphics g = Graphics.FromImage(bmp))
            {
                if(data.Length > 1)
                {
                    g.DrawLines(Pens.Red, data);
                }
                for (int i=0; i<poly.Length; i++)
                {
                    g.DrawEllipse(Pens.Green, new Rectangle(poly[i].X-10, poly[i].Y-10, 20, 20));
                }
            }
            bmp.Save("debug.png");
        }

        private void panel2_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        private void panel2_DragDrop(object sender, DragEventArgs e)
        {
            var filename = (string[])e.Data.GetData(DataFormats.FileDrop);
            InitializeImage(filename[0]);
            this.label3.Visible = false;
        }
    }
}
