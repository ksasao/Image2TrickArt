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
        public MainForm()
        {
            InitializeComponent();
        }

        private void InitializeImage(string filename)
        {
            // 不要な画像を削除
            if (bmp != null)
            {
                bmp.Dispose();
            }

            // 1ピクセル=4バイトのフォーマットに変換
            try
            {
                Bitmap b = new Bitmap(filename);
                bmp = new Bitmap(b.Width, b.Height);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.DrawImageUnscaledAndClipped(b, new Rectangle(0, 0, b.Width, b.Height));
                }
                b.Dispose();
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
                int w = 2480;
                int h = 3508;

                ProjectionConversion conv = null;

                int gx = 0;
                for(int i=0; i<4; i++)
                {
                    gx += poly[i].X;
                }
                gx /= 4;

                if(poly[0].X > gx)
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
                if (proj != null)
                {
                    this.pictureBox2.Image = null;
                    proj.Dispose();
                }
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
                       int src = (int)(((int)c.X) + ((int)c.Y) * w2) * 4;
                       if (0 <= src && src < original.Length)
                       {
                           projection[dest++] = original[src++];
                           projection[dest++] = original[src++];
                           projection[dest++] = original[src++];
                           projection[dest] = original[src];
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
            else
            {
                ErrorDialog();
            }
            GC.Collect();
        }

        private static void ErrorDialog()
        {
            MessageBox.Show("解析に失敗しました。\r\n黒い背景の中に白い四角形がある画像ファイルのみ有効です。", "解析失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void DebugDraw(Point[] data, Point[] poly)
        {
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.DrawLines(Pens.Red, data);
                g.DrawLines(Pens.Blue, poly);
            }
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
