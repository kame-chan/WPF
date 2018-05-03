using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WpfApp1
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private Point init; //マウスクリック時の初期位置
        private bool isDrag; //ドラッグ判定
        private List<UIElement> canvasStock = new List<UIElement>(); //再描画する際に既存のパスを消す用の格納リスト

        /// <summary>
        /// パスを描画する
        /// </summary>
        /// <param name="p">マウスの現在地のポジション</param>
        private void DrawRectangle(Point p)
        {
            text1.Text = "X:"+ init.X.ToString() +"-"+ p.X.ToString() +" Y:"+ init.Y.ToString() + "-" + p.Y.ToString();
            var canvas = canvas1 as Canvas;

            //既存のパスを削除
            foreach (UIElement ui in canvasStock)
            {
                canvas1.Children.Remove(ui);
            }

            //描画用パス
            Rectangle rect = new Rectangle();
            double width;
            double height;
            rect.Stroke = new SolidColorBrush(Colors.Red);
            rect.StrokeThickness = 1;
            width = Math.Abs(init.X - p.X);
            height = Math.Abs(init.Y - p.Y);
            rect.Width = width;
            rect.Height = height;

            //マウスの位置により配置を変える
            if (init.X < p.X)
            {                
                Canvas.SetLeft(rect, init.X);
            }
            else
            {
                Canvas.SetLeft(rect, p.X);
            }

            if (init.Y < p.Y)
            {                
                Canvas.SetTop(rect, init.Y);
            }
            else
            {
                Canvas.SetTop(rect, p.Y);
            }

            //キャンバスの子と削除用に格納するのを忘れずに
            canvas1.Children.Add(rect);
            canvasStock.Add(rect);
        }

        /// <summary>
        /// canvas1上で左クリック時
        /// </summary>
        private void canvas1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Canvas c = sender as Canvas;
            init = e.GetPosition(c);
            c.CaptureMouse();
            isDrag = true;
        }

        /// <summary>
        /// canvas1上で移動時
        /// </summary>
        private void canvas1_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDrag)
            {
                Point imap = e.GetPosition(canvas1); //キャンバス上のマウスの現在地
                DrawRectangle(imap); //再描画
            }
        }

        /// <summary>
        /// canvas1上で左クリック離した時
        /// </summary>
        private void canvas1_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isDrag)
            {
                Canvas c = sender as Canvas;
                isDrag = false;
                c.ReleaseMouseCapture();
            }

            //既存のパスを削除
            foreach (UIElement ui in canvasStock)
            {
                canvas1.Children.Remove(ui);
            }
        }

    }
}
