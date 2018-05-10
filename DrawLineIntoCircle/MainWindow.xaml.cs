using System;
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
        //クリックしている時だけマウスに追従させるための判定
        private bool vectorEdit = false;

        /// <summary>
        /// canvas_vectorロード時の処理
        /// </summary>
        private void canvas_vector_Loaded(object sender, RoutedEventArgs e)
        {
            Canvas canvas = canvas_vector as Canvas;

            //borderの代わり
            Rectangle rect = new Rectangle
            {
                Width = 255,
                Height = 255,
                StrokeThickness = 1,
                Stroke = new SolidColorBrush(Colors.Gray),
            };
            canvas_vector.Children.Add(rect);
            Canvas.SetLeft(rect, 0);
            Canvas.SetTop(rect, 0);

            //直径＝255の円
            Ellipse ell = new Ellipse
            {
                Width = 255,
                Height = 255,
                StrokeThickness = 1,
                Stroke = new SolidColorBrush(Colors.DodgerBlue),
            };
            canvas_vector.Children.Add(ell);
            Canvas.SetLeft(ell, 0);
            Canvas.SetTop(ell, 0);
        }

        /// <summary>
        /// canvas_vector上で左クリックを押した時の処理
        /// </summary>
        private void canvas_vector_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            vectorEdit = true;
            canvas_vector.CaptureMouse();
        }

        /// <summary>
        /// canvas_vector上で左クリックを離した時の処理
        /// </summary>
        private void canvas_vector_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            vectorEdit = false;
            canvas_vector.ReleaseMouseCapture();
        }

        /// <summary>
        /// canvas_vector上でマウスを動かした時の処理
        /// </summary>
        private void canvas_vector_MouseMove(object sender, MouseEventArgs e)
        {
            //左クリックを押していない時はスルー
            if (!vectorEdit) return;
            Canvas canvas = canvas_vector as Canvas;

            //前回の描画をクリアして毎回全て描画する
            //キャンバスを複数重ねて背景だけ別に描画させておく方法もある
            canvas_vector.Children.Clear(); 
            Rectangle rect = new Rectangle
            {
                Width = 255,
                Height = 255,
                StrokeThickness = 1,
                Stroke = new SolidColorBrush(Colors.Gray),
            };
            canvas_vector.Children.Add(rect);
            Canvas.SetLeft(rect, 0);
            Canvas.SetTop(rect, 0);

            Ellipse ell = new Ellipse //外周円
            {
                Width = 255,
                Height = 255,
                StrokeThickness = 1,
                Stroke = new SolidColorBrush(Colors.DodgerBlue),
            };
            canvas_vector.Children.Add(ell);
            Canvas.SetLeft(ell, 0);
            Canvas.SetTop(ell, 0);

            Ellipse ell2 = new Ellipse //中心点
            {
                Width = 10,
                Height = 10,
                Fill = new SolidColorBrush(Colors.DodgerBlue),
            };
            canvas_vector.Children.Add(ell2);
            Canvas.SetLeft(ell2, 123);
            Canvas.SetTop(ell2, 123);

            for (int i = 1; i < 4; i++)
            {
                Line gridLine1 = new Line() //グリッド横線
                {
                    X1 = 0,
                    Y1 = 64 * i,
                    X2 = 255,
                    Y2 = 64 * i,
                    Stroke = new SolidColorBrush(Colors.LightGray),
                    StrokeThickness = 2,
                };
                canvas_vector.Children.Add(gridLine1);

                Line gridLine2 = new Line() //グリッド縦線
                {
                    X1 = 64 * i,
                    Y1 = 0,
                    X2 = 64 * i,
                    Y2 = 255,
                    Stroke = new SolidColorBrush(Colors.LightGray),
                    StrokeThickness = 2,
                };
                canvas_vector.Children.Add(gridLine2);
            }

            
            //マウスの位置と中心点を結ぶ直線
            Point p = e.GetPosition(canvas_vector);
            int x = (int)p.X; //マウスX軸
            int y = (int)p.Y; //マウスY軸
            x = Math.Max(x, 0); //キャンバスサイズを超えない用にする
            x = Math.Min(x, 255); //キャンバスサイズを超えない用にする
            y = Math.Max(y, 0);
            y = Math.Min(y, 255);
            int tanA = 0; //直線の角度（左を0として時計回りに増加）
            int piR = 0; //直線の長さ

            if (x != 128 && y != 128) //中心と垂直、水平に交わらない場合の処理
            {
                //座標Xと座標Yから直角三角形の対辺と隣辺の長さを求める
                double lx = Math.Abs(128 - x);
                double ly = Math.Abs(128 - y);
                //斜辺と角度を求める
                double r = Math.Sqrt(Math.Pow(lx, 2) + Math.Pow(ly, 2));
                double a = Math.Atan(ly / lx) * 180.0 / Math.PI;                
                //演算用のx座標とy座標
                double px = x;
                double py = y;

                //演算は常に右上のブロック（中点から見てXもYも正の値）で行うので、
                //4区分の調整が必要
                double angle = Math.PI * a / 180.0; //演算用
                if (x < 128)
                {                  
                    if (y < 128)
                    {
                        //左上
                        if (r > 124)
                        {                            
                            px = 128 - 128 * Math.Cos(angle);
                            py = 128 - 128 * Math.Sin(angle);
                            r = 128;
                        }
                    }
                    else
                    {
                        //左下
                        if (r > 124)
                        {
                            px = 128 - 128 * Math.Cos(angle);
                            py = 128 + 128 * Math.Sin(angle);
                            r = 128;
                        }
                        a = 360 - a;
                    }
                }
                else
                {
                    if (y < 128)
                    {
                        //右上
                        if (r > 124) //斜辺が一定の長さであれば円に接地するようにする、円からのみ出しも防止
                        {
                            //斜辺と角度が分かっているのでピタゴラスの定理で対辺と隣辺の長さ
                            //つまりは座標を導き出す
                            //x = R * Math.Cos(A);
                            //y = R * Math.Sin(A);
                            //128を足したり128から引いたりしているのは中点が0の座標ではないので、
                            //長さから座標に直すには4区分に応じた調整が必要
                            px = 128 + 128 * Math.Cos(angle);
                            py = 128 - 128 * Math.Sin(angle);
                            r = 128;
                        }
                        a = 180 - a;
                    }
                    else
                    {
                        //右下
                        if (r > 124)
                        {
                            px = 128 + 128 * Math.Cos(angle);
                            py = 128 + 128 * Math.Sin(angle);
                            r = 128;
                        }
                        a += 180;
                    }
                }

                x = (int)Math.Round(px);
                y = (int)Math.Round(py);

                x = Math.Max(x, 0);
                x = Math.Min(x, 255);
                y = Math.Max(y, 0);
                y = Math.Min(y, 255);

                tanA = (int)Math.Round(a);
                piR = (int)Math.Round(r);

            }
            else //中心と垂直、水平に交わる場合の処理　ピタゴラスの定理が使えないので別処理
            {
                if (x == 128)
                {
                    if (y < 5) y = 0; //外周円に近い場合は接地させる
                    if (y > 250) y = 255; //外周円に近い場合は接地させる
                    piR = Math.Abs(128 - y);
                    tanA = (y < 128) ? 90 : 270;
                }
                else
                {
                    if (x < 5) x = 0;
                    if (x > 250) x = 255;
                    piR = Math.Abs(128 - x);
                    piR = (piR > 126) ? 128 : piR; //128-255＝-127なので128に調整
                    tanA = (x < 128) ? 0 : 180;
                }
            }
            //演算させた結果をtextblockに直接書き込む
            vinfo_x.Text = "X:" + x;
            vinfo_y.Text = "Y:" + y;
            vinfo_a.Text = "A:" + tanA;
            vinfo_r.Text = "R:" + piR;

            //最後に直線を描画する
            Line vectorLine = new Line()
            {
                X1 = 128,
                Y1 = 128,
                X2 = x,
                Y2 = y,
                Stroke = new SolidColorBrush(Colors.Red),
                StrokeThickness = 2,
            };
            canvas_vector.Children.Add(vectorLine);
        }
    }
}
