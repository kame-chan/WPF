using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using Tesseract;

namespace VideoCapture4
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        public bool IsCaptureBool { get; set; } // ビデオ出力の動作確認
        public bool IsGridSizeChange { get; set; } // ウィンドウのサイズ変更確認
        public bool IsCaptureFnc { get; set; } // 文字認識の発火点
        public Bitmap SrcImg { get; set; } // Matを扱いやすいようにBitmapにして保存しておく
        public int crop_left { get; set; } // 切り取り用 未使用
        public int crop_top { get; set; }
        public int crop_right { get; set; }
        public int crop_bottom { get; set; }

        public MainWindow()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// デバイスから画像取得、別スレッドで回し続ける、中断処理は色々
        /// </summary>
        public virtual void Capture(object state)
        {
            var camera = new VideoCapture(0) // 0番目のデバイスを指定、一覧を取得する方法は色々
            {
                // キャプチャする画像のサイズフレームレートの指定
                FrameWidth = 1280,
                FrameHeight = 720,
                // Fps = 60
            };

            using (var img = new Mat())
            using (camera)
            {
                while (true)
                {
                    // 画像の表示を一時中断、スレッドは動き続ける
                    if (this.IsCaptureBool) 
                    {
                        this.Dispatcher.Invoke(() => {
                            this._Image.Source = null;
                        });
                        continue;
                    }

                    // サイズ変更
                    if (this.IsGridSizeChange)
                    {
                        this.Dispatcher.Invoke(() => {
                            this._Image.Width = panel.Width;
                            this.IsGridSizeChange = false;
                        });
                    }

                    // キャプチャ
                    if (IsCaptureFnc)
                    {
                        // UIを触るのでInvoke
                        this.Dispatcher.Invoke(() => {
                            crop_left = int.Parse(cropLeft.Text);
                            crop_top = int.Parse(cropTop.Text);
                            crop_right = int.Parse(cropRight.Text);
                            crop_bottom = int.Parse(cropBottom.Text);
                        });
                        SrcImg = img.ToBitmap();
                        AnalyzeString();
                        IsCaptureFnc = false;
                    }

                    camera.Read(img); // デバイスから読み取り

                    if (img.Empty())
                    {
                        continue;
                    }

                    this.Dispatcher.Invoke(() =>
                    {
                        //this._Image.Sourceを更新 for OpenCvSharp4
                        this._Image.Source = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(img);
                    });
                }
            }
        }


        /// <summary>
        /// Windowがロードされた時
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ThreadPool.QueueUserWorkItem(this.Capture);
        }

        /// <summary>
        /// Start / Stopボタンが押され時
        /// </summary>
        protected virtual void Button_Click(object sender, RoutedEventArgs e)
        {
            this.IsCaptureBool = !this.IsCaptureBool;
        }

        /// <summary>
        /// ウィンドウサイズ変化でビデオサイズも変更
        /// </summary>
        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.IsGridSizeChange = true;
        }

        /// <summary>
        /// GetStringsボタンが押され時
        /// </summary>
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.IsCaptureFnc = true;
        }

        /// <summary>
        /// 文字認識
        /// </summary>
        private void AnalyzeString()
        {
            // Bitmapを処理、適度に切り取った方がOCRを扱いやすい
            System.Drawing.Rectangle rect = new System.Drawing.Rectangle(20, 90, 450, 100);
            SrcImg = SrcImg.Clone(rect, SrcImg.PixelFormat);

            // 確認用の画面に画像を出力
            this.Dispatcher.Invoke(() => {
                this._Image_crop.Source = ConvertBitmapToIS(SrcImg);
            });

            // 以下OCR
            string langPath = @"C:\tessdata";
            string lngStr = "eng";

            //画像ファイルでテストするならパス指定
            //var img = new Bitmap(@"C:\test.jpg");
            var img = SrcImg;

            using (var tesseract = new Tesseract.TesseractEngine(langPath, lngStr))
            {
                // OCRの実行
                Pix pix = PixConverter.ToPix(img);
                Tesseract.Page page = tesseract.Process(pix);

                //表示
                Console.WriteLine(page.GetText());
                Console.ReadLine();
            }
        }

        /// <summary>
        /// Bitmap to ImageSource
        /// </summary>
        public BitmapImage ConvertBitmapToIS(Bitmap src)
        {
            MemoryStream ms = new MemoryStream();
            ((System.Drawing.Bitmap)src).Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            ms.Seek(0, SeekOrigin.Begin);
            image.StreamSource = ms;
            image.EndInit();
            return image;
        }
    }
}
