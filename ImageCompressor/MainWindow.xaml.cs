using ImageMagick;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using MessageBox = System.Windows.Forms.MessageBox;

namespace ImageCompressor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string input_path;
        string output_path;
        string[] files;
        private CancellationTokenSource cancellationTokenSource;
        private List<Task> compressionTasks;
        int image_compressed_count;
        bool task_enable;
        int compressionQuality;


        public MainWindow()
        {
            input_path = "";
            output_path = "";
            image_compressed_count = 0;

            task_enable = false;
            compressionQuality = 50;


            InitializeComponent();

            slider_percent_folder.ValueChanged += slider_percent_folder_ValueChanged;
            slider_percent_files.ValueChanged += slider_percent_files_ValueChanged;
        }

        //click events
        private void btn_input_folder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Windows.Forms.FolderBrowserDialog openFileDlg = new();
                var result = openFileDlg.ShowDialog();
                if (result.ToString() != string.Empty)
                {
                    input_path = openFileDlg.SelectedPath;
                    if (string.IsNullOrEmpty(input_path))
                    {
                        lbl_input_folder.Text = "no input folder selected.";
                        return;
                    }
                    lbl_input_folder.Text = input_path;
                }
            }
            catch
            {
                MessageBox.Show("System Error");
            }

        }

        private void btn_output_folder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Windows.Forms.FolderBrowserDialog openFileDlg = new();
                var result = openFileDlg.ShowDialog();

                if (result.ToString() != string.Empty)
                {
                    output_path = openFileDlg.SelectedPath;
                    if (string.IsNullOrEmpty(output_path))
                    {
                        lbl_output_folder.Text = "no output folder selected.";
                        return;
                    }
                    lbl_output_folder.Text = output_path;
                }
            }
            catch
            {
                MessageBox.Show("System Error");
            }
        }

        private async void btn_compress_folder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Validates
                if (string.IsNullOrEmpty(input_path))
                {
                    MessageBox.Show("Selsect input path!");
                    return;
                }

                if (string.IsNullOrEmpty(output_path))
                {
                    MessageBox.Show("Selsect output path!");
                    return;
                }

                if (task_enable)
                {
                    MessageBox.Show("Please wait...");
                    return;
                }


                //Inits
                compression_progressBar.Visibility = Visibility.Visible;
                btn_compress_folder.Content = "Task is runing...";
                btn_cancell_compress_folder.Visibility = Visibility.Visible;
                string[] imageFiles = Directory.GetFiles(input_path, "*.*", SearchOption.AllDirectories)
                .Where(s => s.EndsWith(".jpg") || s.EndsWith(".jpeg") || s.EndsWith(".png")).ToArray();
                lbl_compression_progressBar.Content = "0 files compressed!";
                compression_progressBar.Maximum = imageFiles.Count();
                task_enable = true;
                compressionQuality = (int)slider_percent_folder.Value;


                // Set up cancellation token.
                cancellationTokenSource = new CancellationTokenSource();
                CancellationToken cancellationToken = cancellationTokenSource.Token;

                // Set up progress reporting.
                var progress = new Progress<int>(value => UpdateProgressBar(value));
                var progressHandler = new Progress<int>(value => { });



                try
                {
                    await CompressImagesAsync(imageFiles, output_path, compressionQuality, cancellationToken, progress);
                    MessageBox.Show("Image compression complete. number of compressed images: " + image_compressed_count);
                }
                catch (OperationCanceledException)
                {
                    MessageBox.Show("Image compression canceled. number of compressed images:" + image_compressed_count);
                }
                finally
                {

                    btn_compress_folder.Content = "Start compress";
                    btn_cancell_compress_folder.Visibility = Visibility.Hidden;
                    compression_progressBar.Visibility = Visibility.Hidden;
                    lbl_compression_progressBar.Content = "";
                    compression_progressBar.Value = 0;
                    image_compressed_count = 0;
                    task_enable = false;
                }
            }
            catch
            {

            }
        }

        private void btn_cancell_compress_folder_Click(object sender, RoutedEventArgs e)
        {
            cancellationTokenSource?.Cancel();
        }

        //functions
        private async Task CompressImagesAsync(string[] imageFiles, string outputFolder, int compressionQuality, CancellationToken cancellationToken, IProgress<int> progress)
        {
            compressionTasks = new List<Task>();


            int skip = 0;
            int tread;
            if (imageFiles.Count() <= 100)
            {
                tread = 1;
            }
            else if (imageFiles.Count() > 100 && imageFiles.Count() <= 1000)
            {
                tread = 2;
            }
            else
            {
                //tread = Environment.ProcessorCount;
                tread = 4;
            }
            int take = (int)Math.Ceiling((decimal)(imageFiles.Count() / tread));



            for (int i = 0; i < tread; i++)
            {
                var image_files = imageFiles.Skip(skip + (i * take)).Take(take);


                compressionTasks.Add(Task.Run(() =>
                {
                    foreach (var image in image_files)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                        }

                        using (var magick_image = new MagickImage(image))
                        {
                            magick_image.Quality = compressionQuality;

                            string outputFilePath = Path.Combine(outputFolder, Path.GetFileName(image));

                            magick_image.Write(outputFilePath);

                            // Update the progress.
                            progress.Report(1);
                        }
                    }

                }, cancellationToken));
            }

            await Task.WhenAll(compressionTasks);
        }

        private void UpdateProgressBar(int percentComplete)
        {
            compression_progressBar.Value += 1;
            lbl_compression_progressBar.Content = compression_progressBar.Value + " files compressed!";
            image_compressed_count++;
        }

        private static IEnumerable<IEnumerable<T>> Partition<T>(IEnumerable<T> source, int chunkSize)
        {
            while (source.Any())
            {
                yield return source.Take(chunkSize);
                source = source.Skip(chunkSize);
            }
        }

        private void btn_input_file_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDlg = new();
                openFileDlg.Multiselect = true;
                openFileDlg.Filter = "Image Files|*.jpg;*.jpeg;*.png;";
                var result = openFileDlg.ShowDialog();
                if (result.ToString() != string.Empty)
                {
                    files = openFileDlg.FileNames;
                    if (files.Length <= 0)
                    {
                        lbl_input_file.Text = "no input file selected.";
                        return;
                    }
                    input_path = Path.GetDirectoryName(openFileDlg.FileNames[0]);
                    if(files.Length == 1)
                    {
                        lbl_input_file.Text = openFileDlg.FileNames[0];
                    }
                    else
                    {
                        lbl_input_file.Text = input_path + "\\" + files.Length + " files selected";
                    }
                }
            }
            catch
            {
                MessageBox.Show("System Error");
            }
        }

        private async void btn_compress_file_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //validates
                if (files.Length <= 0)
                {
                    MessageBox.Show("Selsect input files");
                    return;
                }

                if (task_enable)
                {
                    MessageBox.Show("Please wait...");
                    return;
                }


                //inits
                compression_progressBar.Visibility = Visibility.Visible;
                btn_compress_file.Content = "Task is runing...";
                btn_cancell_compress_file.Visibility = Visibility.Visible;
                lbl_compression_progressBar.Content = "0 files compressed!";
                compression_progressBar.Maximum = files.Length;
                task_enable = true;
                compressionQuality = (int)slider_percent_files.Value;


                // Set up cancellation token.
                cancellationTokenSource = new CancellationTokenSource();
                CancellationToken cancellationToken = cancellationTokenSource.Token;


                // Set up progress reporting.
                var progress = new Progress<int>(value => UpdateProgressBar(value));
                var progressHandler = new Progress<int>(value => { });


                //Directory exists check
                string directoryPath = input_path + "\\" + "compressed-images"; // Replace with the desired directory path
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }


                //run task
                try
                {
                    await CompressImagesAsync(files, directoryPath, compressionQuality, cancellationToken, progress);
                    MessageBox.Show("Image compression complete. number of compressed images: " + image_compressed_count);
                }
                catch (OperationCanceledException)
                {

                    MessageBox.Show("Image compression canceled. number of compressed images:" + image_compressed_count);
                }
                finally
                {
                    btn_compress_file.Content = "Start compress";
                    btn_cancell_compress_file.Visibility = Visibility.Hidden;
                    compression_progressBar.Visibility = Visibility.Hidden;
                    lbl_compression_progressBar.Content = "";
                    image_compressed_count = 0;
                    compression_progressBar.Value = 0;
                    task_enable = false;
                }

            }
            catch
            {

            }

        }

        private void btn_cancell_compress_file_Click(object sender, RoutedEventArgs e)
        {
            cancellationTokenSource?.Cancel();
        }

        private void compression_progressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }
        private void btn_ministerdv_Click(object sender, RoutedEventArgs e)
        {
            string url = "https://github.com/MinisterDv/Image-Compressor";

            try
            {
                Process.Start("explorer", url);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void slider_percent_folder_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int percentage = (int)slider_percent_folder.Value;
            lbl_percent_folder.Content = $"{percentage}%";
        }

        private void slider_percent_files_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int percentage = (int)slider_percent_files.Value;
            lbl_percent_files.Content = $"{percentage}%";
        }
    }
}
