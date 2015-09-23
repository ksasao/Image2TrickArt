﻿using Mpga.Imaging;
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
                ConvertToARGBImage(filename);
            }
            catch (Exception)
            {
                ErrorDialog();
                return;
            }

            var data = ContourTracking.Apply(bmp);
            var poly = Vectorize.Apply(data);

            //DebugDraw(data, poly);

            this.pictureBox1.Image = bmp;

            if (poly.Length == 4)
            {
                CreateTrickArt(filename, poly);
            }
            else
            {
                ErrorDialog();
            }
            GC.Collect();
        }

        private void ConvertToARGBImage(string filename)
        {
            Bitmap b = new Bitmap(filename);
            bmp = new Bitmap(b.Width, b.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.DrawImageUnscaledAndClipped(b, new Rectangle(0, 0, b.Width, b.Height));
            }
            b.Dispose();
        }

        private void CreateTrickArt(string filename, Point[] poly)
        {
            int w = 2480;
            int h = 3508;

            ProjectionConversion conv = null;

            conv = GetProjection(poly, w, h);

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

        private static void ErrorDialog()
        {
            MessageBox.Show("解析に失敗しました。\r\n黒い背景の中に白い四角形がある画像ファイルのみ有効です。", "解析失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void DebugDraw(Point[] data, Point[] poly)
        {
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.DrawLines(Pens.Red, data);
                for(int i=0; i<poly.Length; i++)
                {
                    g.DrawEllipse(Pens.Green, new Rectangle(poly[i].X-10, poly[i].Y-10, 20, 20));
                }
                g.DrawLines(Pens.Blue, poly);
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
