using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
            CreateThumb();
        }

        private Point init; //マウスクリック時の初期位置
        private bool isDrag; //ドラッグ判定
        private bool multi; //複数選択判定
        private Path frame = new Path(); //選択範囲記憶用パス
        private List<Thumb> thumbList = new List<Thumb>(); //再描画する際に既存のパスを消すための格納リスト
        private List<UIElement> canvasStock = new List<UIElement>(); //キャンバス内にある全要素を格納するためのリスト
        private List<UIElement> selectStock = new List<UIElement>(); //選択中のthumbを格納するリスト

        /// <summary>
        /// 起動時にthumbをキャンバスに配置
        /// </summary>
        private void CreateThumb()
        {
            var canvas = canvas1 as Canvas;
            for (int i=0; i<5; i++)
            {
                Thumb thumb = new Thumb()
                {
                    Width = 50,
                    Height = 50,
                    Background = new SolidColorBrush(Colors.Gray),
                    BorderBrush = new SolidColorBrush(Colors.Red),
                    BorderThickness = new Thickness(0),
                };
                //thumbドラッグ用のイベントを追加
                thumb.DragStarted += new DragStartedEventHandler(Thumb_DragStarted);
                thumb.DragDelta += new DragDeltaEventHandler(Thumb_DragDelta);
                thumb.DragCompleted += new DragCompletedEventHandler(Thumb_DragCompleted);

                //一定の間隔で配置
                Canvas.SetLeft(thumb, i*75);
                Canvas.SetTop(thumb, i*75);
                canvas1.Children.Add(thumb);
                //要素作成時にはリストへ追加しておく
                thumbList.Add(thumb);
            }
        }

        /// <summary>
        /// 渡されたエレメントを移動
        /// </summary>
        private void SetLocate(UIElement ele, Point p)
        {
            //Thumbは前の描画を消さなくてもよい
            Canvas.SetLeft(ele, p.X);
            Canvas.SetTop(ele, p.Y);
        }

        /// <summary>
        /// 渡されたエレメントを移動(複数選択)
        /// </summary>
        private void SetLocate_Multi(UIElement ele, Point p)
        {
            //キャンバス上の位置とマウスの移動量を合計して配置する
            var x = Canvas.GetLeft(ele) + p.X;
            var y = Canvas.GetTop(ele) + p.Y;
            Canvas.SetLeft(ele, x);
            Canvas.SetTop(ele, y);
        }

        /// <summary>
        /// Thumbのドラッグ移動開始
        /// </summary>
        private void Thumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            //色々な判定に使える　今回は未使用
        }

        /// <summary>
        /// Thumbのドラッグ移動中
        /// </summary>
        private void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var thumb = sender as Thumb;
            var canvas = thumb.Parent as Canvas;

            if (multi) //複数選択している場合はthumbの移動関数を変更
            {
                var x = e.HorizontalChange; //マウスの移動量X
                var y = e.VerticalChange; //マウスの移動量Y
                foreach (Thumb t in selectStock)
                {
                    //選択リスト内にある要素とマウスの移動量を渡す
                    SetLocate_Multi(t, new Point(x, y));
                }
            }
            else
            {
                var x = Canvas.GetLeft(thumb) + e.HorizontalChange;
                var y = Canvas.GetTop(thumb) + e.VerticalChange;

                //キャンバスの領域外に出ないように数値上限を設定、複数移動の場合はもうひと工夫必要
                x = Math.Max(x, 0);
                y = Math.Max(y, 0);
                x = Math.Min(x, canvas.ActualWidth - thumb.ActualWidth);
                y = Math.Min(y, canvas.ActualHeight - thumb.ActualHeight);

                SetLocate(thumb, new Point(x, y));
            }
        }

        /// <summary>
        /// Thumbのドラッグ完了
        /// </summary>
        private void Thumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            var thumb = sender as Thumb;
            if (thumb == null) return;
            ClearSelect();
        }

        /// <summary>
        /// 選択状態を解除
        /// </summary>
        private void ClearSelect()
        {
            foreach (Thumb t in selectStock)
            {
                t.Background = new SolidColorBrush(Colors.Gray);
                t.BorderThickness = new Thickness(0);
            }
            selectStock = new List<UIElement>(); //初期化
            frame.Data = null;
            multi = false;
        }

        /// <summary>
        /// 渡されたThumbの位置を返す
        /// </summary>
        private Point GetLocate(UIElement e)
        {
            double x = Canvas.GetLeft(e);
            double y = Canvas.GetTop(e);
            return new Point(x, y);
        }

        /// <summary>
        /// thumb選択の処理、複数選択
        /// </summary>
        private void SelectThumb()
        {
            if (frame.Data == null) return;
            Rect sr = frame.Data.Bounds; //枠のRect
            Rect tr;
            foreach (Thumb t in thumbList)
            {
                tr = new Rect(GetLocate(t), t.RenderSize); //ThumbのRect
                //ThumbのRectが枠のRectと重なっているか判定
                if (tr.IntersectsWith(sr))
                {
                    t.BorderThickness = new Thickness(1);
                    t.Background = new SolidColorBrush(Colors.Aqua);
                    selectStock.Add(t);
                    multi = true; //boolは使わずにselectStockの数を数えて判定する方法でも良い
                }                
            }
        }

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

            Rect r = new Rect(init, p); //範囲確認用
            RectangleGeometry rg = new RectangleGeometry(r);
            frame.Data = rg;
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

            if(multi) //boolは使わずにselectStockの数を数えて判定する方法でも良い
            {
                ClearSelect();
            }

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
                SelectThumb();
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
