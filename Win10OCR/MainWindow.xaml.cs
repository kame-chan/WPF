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
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using System.Windows.Controls;
using Microsoft.Win32;

// .NET Framework 4.6.1 (4.6以上)
// Nuget パッケージの管理はPackageReferenceにしておくこと
// - Microsoft.Windows.SDK.Contracts 10.0.22000.196
// - OpenCvSharp4 4.5.5.20211231
// - OpenCvSharp4.runtime.win 4.5.5.20211231
// - OpenCvSharp4.WpfExtensions 4.5.1.20201229

namespace Win10OCR
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        public bool IsCaptureBool { get; set; } // ビデオ出力の動作確認
        public bool IsGridSizeChange { get; set; } // ウィンドウのサイズ変更確認
        public bool IsCaptureFnc { get; set; } // 文字認識の発火点
        public System.Windows.Controls.Image copyimg = new System.Windows.Controls.Image(); // 画像解析用のコピー

        public MainWindow()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// デバイスから画像取得、別スレッドで回し続ける、中断処理は色々
        /// </summary>
        public virtual void Capture(object state)
        {
            var camera = new VideoCapture(1) // 0番目のデバイスを指定、一覧を取得する方法は色々
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
                        this.Dispatcher.Invoke(() => {
                            OcrStringAsync(img);
                        });

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

        private async void OcrStringAsync(Mat img)
        {
            copyimg.Source = System.Windows.Media.Imaging.BitmapFrame.Create(OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(img));
            SoftwareBitmap sbitmap = await ConvertSoftwareBitmap(copyimg);
            OcrResult result = await RunOcr(sbitmap);

            Console.WriteLine(result.Text);
            string output = "";
            foreach (var line in result.Lines)
            {
                // 1行分の文字列を格納するためのバッファ
                var sb = new System.Text.StringBuilder();
                // 出現場所は各文字ごとに記録されている
                RectangleF cloneRect = new RectangleF(
                    (float)line.Words[0].BoundingRect.Left,
                    (float)line.Words[0].BoundingRect.Top,
                    (float)(line.Words[line.Words.Count - 1].BoundingRect.Right - line.Words[0].BoundingRect.Left),
                    (float)(line.Words[0].BoundingRect.Bottom - line.Words[0].BoundingRect.Top)
                );

                foreach (var word in line.Words)
                {
                    // wordには1文字ずつ入っているので結合
                    sb.Append(word.Text);
                }

                output += string.Format("[{0}]{1}{2}",
                    sb.ToString().TrimEnd(),
                    cloneRect,
                    Environment.NewLine // 改行
                );
            }
            Console.WriteLine(output);
        }

        /// <summary>
        /// ImageSourceをBitmapFrameに変換
        /// 参考：https://marunaka-blog.com/wpf-ocr-windows10/2260/
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        private async Task<SoftwareBitmap> ConvertSoftwareBitmap(System.Windows.Controls.Image image)
        {
            SoftwareBitmap sbitmap = null;

            using (MemoryStream stream = new MemoryStream())
            {
                var encoder = new BmpBitmapEncoder();
                encoder.Frames.Add((System.Windows.Media.Imaging.BitmapFrame)image.Source);
                encoder.Save(stream);

                // メモリストリームを変換
                var irstream = WindowsRuntimeStreamExtensions.AsRandomAccessStream(stream);

                // 画像データをSoftwareBitmapに変換
                var decorder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(irstream);
                sbitmap = await decorder.GetSoftwareBitmapAsync();
            }

            return sbitmap;
        }

        /// <summary>
        /// OCRを実行
        /// </summary>
        /// <param name="sbitmap"></param>
        /// <returns></returns>
        private async Task<OcrResult> RunOcr(SoftwareBitmap sbitmap)
        {
            OcrEngine engine = OcrEngine.TryCreateFromLanguage(new Windows.Globalization.Language("ja-JP"));
            var result = await engine.RecognizeAsync(sbitmap);
            return result;
        }

    }
}
