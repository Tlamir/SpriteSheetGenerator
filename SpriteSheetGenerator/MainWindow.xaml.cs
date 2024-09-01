using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace SpriteSheetGenerator
{
    public partial class MainWindow : Window
    {
        private List<BitmapImage> _images = new List<BitmapImage>();
        private List<Border> _selectedBorders = new List<Border>();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnLoadImages_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                // Clear previous images
                _images.Clear();
                wrapPanelImages.Children.Clear();

                foreach (var filename in openFileDialog.FileNames)
                {
                    var bitmap = new BitmapImage(new Uri(filename));
                    _images.Add(bitmap);

                    var img = new Image
                    {
                        Source = bitmap,
                        Width = 100,
                        Height = 100,
                        Margin = new Thickness(5),
                        ToolTip = filename
                    };

                    var border = new Border
                    {
                        Child = img,
                        BorderThickness = new Thickness(2),
                        BorderBrush = Brushes.Transparent
                    };

                    border.MouseLeftButtonDown += Border_MouseLeftButtonDown;

                    wrapPanelImages.Children.Add(border);
                }
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private void BtnGenerateSpriteSheet_Click(object sender, RoutedEventArgs e)
        {
            if (_images.Count == 0)
            {
                ShowCustomMessageBox("Please load images first.");
                return;
            }

            if (!int.TryParse(txtColumns.Text, out int columns) || columns <= 0)
            {
                ShowCustomMessageBox("Please enter a valid number of columns.");
                return;
            }

            var spriteWidth = (int)_images[0].PixelWidth;
            var spriteHeight = (int)_images[0].PixelHeight;
            var rows = (int)Math.Ceiling((double)_images.Count / columns);

            var sheetWidth = spriteWidth * columns;
            var sheetHeight = spriteHeight * rows;

            var drawingVisual = new DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
            {
                for (int i = 0; i < _images.Count; i++)
                {
                    var column = i % columns;
                    var row = i / columns;
                    var rect = new Rect(column * spriteWidth, row * spriteHeight, spriteWidth, spriteHeight);
                    drawingContext.DrawImage(_images[i], rect);
                }
            }

            var rtb = new RenderTargetBitmap(sheetWidth, sheetHeight, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(drawingVisual);

            var selectedFormat = ((ComboBoxItem)cmbFormat.SelectedItem).Content.ToString();
            if (selectedFormat == "PNG")
            {
                SaveSpriteSheetAsPng(rtb);
            }
            else if (selectedFormat == "JSON")
            {
                SaveSpriteSheetAsJson(rtb, columns, spriteWidth, spriteHeight);
            }
        }

        private void BtnDeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            foreach (var border in _selectedBorders)
            {
                wrapPanelImages.Children.Remove(border);
                var img = (Image)border.Child;
                _images.Remove((BitmapImage)img.Source);
            }

            _selectedBorders.Clear();
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var clickedBorder = (Border)sender;
            if (_selectedBorders.Contains(clickedBorder))
            {
                _selectedBorders.Remove(clickedBorder);
                clickedBorder.BorderBrush = Brushes.Transparent;
            }
            else
            {
                _selectedBorders.Add(clickedBorder);
                clickedBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(128, 128, 128)); // Slightly darker gray
            }
        }

        private void SaveSpriteSheetAsPng(RenderTargetBitmap rtb)
        {
            var pngEncoder = new PngBitmapEncoder();
            pngEncoder.Frames.Add(BitmapFrame.Create(rtb));

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "PNG Image|*.png",
                Title = "Save Sprite Sheet"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                using (var fs = new FileStream(saveFileDialog.FileName, FileMode.Create))
                {
                    pngEncoder.Save(fs);
                }

                ShowCustomMessageBox("Sprite sheet saved successfully as PNG!");
            }
        }

        private void SaveSpriteSheetAsJson(RenderTargetBitmap rtb, int columns, int spriteWidth, int spriteHeight)
        {
            var sprites = new List<object>();
            for (int i = 0; i < _images.Count; i++)
            {
                var column = i % columns;
                var row = i / columns;
                sprites.Add(new
                {
                    x = column * spriteWidth,
                    y = row * spriteHeight,
                    width = spriteWidth,
                    height = spriteHeight
                });
            }

            var spriteSheetData = new
            {
                sheetWidth = rtb.PixelWidth,
                sheetHeight = rtb.PixelHeight,
                sprites = sprites
            };

            var json = JsonSerializer.Serialize(spriteSheetData, new JsonSerializerOptions { WriteIndented = true });

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "JSON File|*.json",
                Title = "Save Sprite Sheet"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                File.WriteAllText(saveFileDialog.FileName, json);
                ShowCustomMessageBox("Sprite sheet saved successfully as JSON!");
            }
        }

        private void ShowCustomMessageBox(string message)
        {
            var popup = new Window
            {
                Width = 300,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Background = new SolidColorBrush(Color.FromRgb(43, 43, 43)),
                Foreground = Brushes.White,
                Title = "Notification",
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.None,
                Owner = this
            };

            var stackPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var textBlock = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(10)
            };

            var button = new Button
            {
                Content = "OK",
                Width = 100,
                Height = 30,
                Margin = new Thickness(10),
                Background = new SolidColorBrush(Color.FromRgb(70, 70, 70)),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderBrush = new SolidColorBrush(Color.FromRgb(58, 58, 58))
            };

            button.Click += (s, e) => popup.Close();

            stackPanel.Children.Add(textBlock);
            stackPanel.Children.Add(button);
            popup.Content = stackPanel;

            popup.ShowDialog();
        }
    }
}