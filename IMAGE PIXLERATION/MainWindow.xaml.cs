using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace IMAGE_PIXLERATION
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private WriteableBitmap _processedImage;
        private WriteableBitmap _selectedImage;
        private Color[,] _imagePixels;
        private string[] files = null;
        private int x1, y1, x2, y2;
        public MainWindow()
        {
            InitializeComponent();
        }
        public Point GetImageCoordsAt(MouseButtonEventArgs e)
        {
            if (MainImage != null && MainImage.IsMouseOver)
            {
                var controlSpacePosition = e.GetPosition(MainImage);
                var imageControl = this.MainImage as Image;
                var mainViewModel = base.DataContext;
                if (imageControl != null && imageControl.Source != null)
                {
                    // Convert from control space to image space
                    var x = Math.Floor(controlSpacePosition.X * imageControl.Source.Width / imageControl.ActualWidth);
                    var y = Math.Floor(controlSpacePosition.Y * imageControl.Source.Height / imageControl.ActualHeight);

                    return new Point(x, y);
                }
            }
            return new Point(-1, -1);
        }

        private void MainImage_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            x1 =(int) GetImageCoordsAt(e).X;
            y1 =(int) GetImageCoordsAt(e).Y;
            Console.WriteLine(@"Start -> " + GetImageCoordsAt(e));
            StartingLocationLabel.Content = "SL : " + x1 + "x" + y1;
        }
        private void MainImage_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            x2 = (int)GetImageCoordsAt(e).X;
            y2 = (int)GetImageCoordsAt(e).Y;
            Console.WriteLine(@"END -> " + GetImageCoordsAt(e));
            EndingLocationLabel.Content = "EL : " + x2 + "x" + y2;

        }
        private void pixilation_Click(object sender, RoutedEventArgs e)
        {
            int res = 0;
            if (!int.TryParse(PixelSize.Text, out res))
            {
                MessageBox.Show("Invalid Size");
                return;
            }
            ImagePixilationThread();


        }



        private void ImagePixilationThread()
        {
            if (_selectedImage != null)
            {
                double width = _selectedImage.PixelWidth;
                double height = _selectedImage.PixelHeight;
                int pixelSize = Convert.ToInt32(PixelSize.Text);
                ImageSizeLabel.Content = "Size : " + width + "x" + height;
                if (pixelSize > width / 2)
                {
                    MessageBox.Show("To much high value");
                    return;
                }

                int h = (int)Math.Ceiling((height / pixelSize));
                int w = (int)Math.Ceiling((width / pixelSize));
                ImageSizeLabel.Content = "Size : " + height + "x" + width;
                StatusLabel.Content = "New Size : " + h + "x" + w;
                
                Color[,] selectedImageColors = new Color[(int)width, (int)height];
                Color[,] processedColor = new Color[w, h];


                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        Int32Rect r = new Int32Rect(x, y, 1, 1);
                        byte[] ColorData = { 25, 25, 25, 0 }; // B G R A;
                        _selectedImage.CopyPixels(r, ColorData, 4, 0);
                        selectedImageColors[x, y] = Color.FromRgb(ColorData[2], ColorData[1], ColorData[0]);
                    }
                }

                int totalcount = 0;
                int totalsize = (int)width * (int)height;
                if (x2 > 0 && y2 > 0)
                {
                    width = x2;
                    height = y2;
                }


                for (int x = x1; x < width; x += pixelSize)
                {
                    for (int y = y1; y < height; y += pixelSize)
                    {
                        // inner loop
                        int colourCount = 0;
                        int r = 0;
                        int g = 0;
                        int b = 0;
                        int count = 0;
                        for (int xx = x; xx < width && xx < x + pixelSize; xx++)
                        {
                            for (int yy = y; yy < height && yy < y + pixelSize; yy++)
                            {
                                colourCount++;
                                r += selectedImageColors[xx, yy].R;
                                g += selectedImageColors[xx, yy].G;
                                b += selectedImageColors[xx, yy].B;
                            }
                        }


                        r = r / colourCount;
                        g = g / colourCount;
                        b = b / colourCount;
                        totalcount++;
                        for (int xx = x; xx < width && xx < x + pixelSize; xx++)
                        {
                            for (int yy = y; yy < height && yy < y + pixelSize; yy++)
                            {
                                Int32Rect rect = new Int32Rect(xx, yy, 1, 1);
                                byte[] ColorData = { (byte)b, (byte)g, (byte)r, 255 }; // B G R A;
                                _selectedImage.WritePixels(rect, ColorData, 4, 0);
                            }

                        }

                    }
                }

                MainImage.Source = _selectedImage;
                x1 = 0;
                y1 = 0;
                x2 = (int)_selectedImage.Width;
                x1 = (int)_selectedImage.Height;
                StartingLocationLabel.Content = "SL : " + x1 + "x" + y1;
                EndingLocationLabel.Content = "EL : " + x2 + "x" + y2;

            }
            else
            {
                MessageBox.Show("Image Not Selected");
            }
        }
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }



        private void SaveImage_Click_1(object sender, RoutedEventArgs e)
        {
            SaveFileDialog fileDialog = new SaveFileDialog();
            if (fileDialog.ShowDialog() == true)
            {
                try
                {
                    BitmapSource bitmap = _selectedImage;
                    string filename = fileDialog.FileName;
                    if (filename != string.Empty)
                    {
                        using (FileStream stream5 = new FileStream(filename, FileMode.Create))
                        {
                            PngBitmapEncoder encoder5 = new PngBitmapEncoder();
                            encoder5.Frames.Add(BitmapFrame.Create(bitmap));
                            encoder5.Save(stream5);
                        }
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }
            }
        }

        private void LoadImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            if (fileDialog.ShowDialog() == true)
            {
                try
                {
                    Console.WriteLine(fileDialog.FileName);
                    BitmapImage image = new BitmapImage(new Uri(fileDialog.FileName));
                    _selectedImage = new WriteableBitmap(image);
                    MainImage.Source = _selectedImage;
                    ImageSizeLabel.Content = "Size : " + image.Height + "x" + image.Width;
                    x1 = 0;
                    y1 = 0;
                    x2 = (int)_selectedImage.Width;
                    y2 = (int)_selectedImage.Height;
                    StartingLocationLabel.Content = "SL : " + x1 + "x" + y1;
                    EndingLocationLabel.Content = "EL : " + x2 + "x" + y2;
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                    MessageBox.Show(exception.Message);
                }
            }

        }


    }
}
