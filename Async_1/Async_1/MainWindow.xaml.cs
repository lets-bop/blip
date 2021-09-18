using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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

namespace Async_1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Stopwatch stopwatch = new Stopwatch();
        CancellationTokenSource mainCancellationTokenSource = new CancellationTokenSource();

        public MainWindow()
        {
            InitializeComponent();
        }

        // With TPL (no async/await)
        private void Search_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Preload();

                var service = new PlaceService();
                var placeType = PlaceType.Text;
                var asyncPlacesTask = Task.Run(() =>
                {
                    var places = service.GetAllPlacesAsync(placeType);
                    Dispatcher.Invoke(() => // we need the Dispatcher because we are accessing "Places" which can only be done by the UI Thread.
                    {
                        Places.ItemsSource = places.Result;
                    });
                });

                asyncPlacesTask.ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() => // asyncTask.ContinueWith launches another async task. Hence we need the Dispatcher because we are accessing "Status" which can only be done by the UI Thread
                    {
                        Post();
                    });
                });
            }
            catch (Exception ex)
            {
                Notepad.Text = ex.Message;
            }
            finally
            {
            }
        }

        // With TPL (no async/await)
        //private void Search_Click(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        Preload();

        //        var service = new PlaceService();
        //        var placeType = PlaceType.Text;

        //        CancellationTokenSource localCancellationTokenSource = new CancellationTokenSource();
        //        CancellationToken localCancellationToken = localCancellationTokenSource.Token;
        //        CancellationToken mainCancellationToken = mainCancellationTokenSource.Token;

        //        var cancelTask = Task.Run(() =>
        //        {
        //            while(true)
        //            {
        //                if (localCancellationToken.IsCancellationRequested || mainCancellationToken.IsCancellationRequested)
        //                {
        //                    return;
        //                }
        //            }
        //        });

        //        var asyncPlacesTask = Task.Run(() =>
        //        {
        //            var places = service.GetAllPlacesAsync(placeType);
        //            Dispatcher.Invoke(() => // we need the Dispatcher because we are accessing "Places" which can only be done by the UI Thread.
        //            {
        //                Places.ItemsSource = places.Result;
        //            });
        //        });

        //        localCancellationTokenSource.Cancel();
        //        var whenAnyTaskCompletesTask = Task.WhenAny(asyncPlacesTask, cancelTask);

        //        whenAnyTaskCompletesTask.ContinueWith(_ =>
        //        {
        //            Dispatcher.Invoke(() => // asyncTask.ContinueWith launches another async task. Hence we need the Dispatcher because we are accessing "Status" which can only be done by the UI Thread
        //            {
        //                if (localCancellationToken.IsCancellationRequested || mainCancellationToken.IsCancellationRequested)
        //                {
        //                    Notes.Text = "Cancelled!";
        //                }

        //                Post();
        //            });
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        Notes.Text = ex.Message;
        //    }
        //    finally
        //    {
        //    }
        //}


        // With async/await + TPL
        //private async void Search_Click(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        Preload();

        //        var service = new PlaceService();
        //        var placeType = PlaceType.Text;
        //        await Task.Run(() =>
        //        {
        //            var places = service.GetAllPlacesAsync(placeType);
        //            Dispatcher.Invoke(() => // we need the Dispatcher because we are accessing "Places" which can only be done by the UI Thread.
        //            {
        //                Places.ItemsSource = places.Result;
        //            });
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        Notes.Text = ex.Message;
        //    }
        //    finally
        //    {
        //        // The below works correctly only because of the await on Task.Run. Without the await, the load time will be approx 0 as Task.Run will return almost instantaneosly after queueing the work on the thread pool.
        //        Post();
        //    }
        //}

        // With async/await
        //private async void Search_Click(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        Preload();

        //        var service = new PlaceService();

        //        var places = service.GetAllPlacesAsync(PlaceType.Text);

        //        Places.ItemsSource = await places; // at this point, the control goes back to the UI thread, and the user can do other things on the UI like taking notes.

        //        // Any code here executes only once the task being awaited for has completed. This is called the continuation.
        //    }
        //    catch (Exception ex)
        //    {
        //        Notes.Text = ex.Message;
        //    }
        //    finally
        //    {
        //        Post();
        //    }
        //}

        // ConfigureAwait
        //private async void Search_Click(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        Preload();

        //        var data = await GetPlaces(PlaceType.Text);

        //        Places.ItemsSource = data.Result;

        //        Notes.Text = "Loaded!";     // This will work. But, it would've thrown an exception if you'd set await GetPlaces(PlaceType.Text).ConfigureAwait(false);

        //        Post();
        //    }
        //    catch (Exception ex)
        //    {
        //        Notes.Text = ex.Message;
        //    }
        //    finally
        //    {
        //    }
        //}

        // Deadlocking_1
        //private void Search_Click(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        Preload();

        //        // Synchronous wait. All of the below will result in a deadlock.
        //        GetPlaces(PlaceType.Text).Wait();
        //        // GetPlaces(PlaceType.Text).GetAwaiter().GetResult();
        //        // GetPlacesIntermediate().GetAwaiter().GetResult();

        //        Notes.Text = "Loaded!";     // We won't get here as there is a deadlock. This is because the state machine runs on the calling thread (UI Thread), which is blocked.

        //        Post();
        //    }
        //    catch (Exception ex)
        //    {
        //        Notes.Text = ex.Message;
        //    }
        //    finally
        //    {
        //    }
        //}

        // Deadlocking_2
        //private async void Search_Click(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        Preload();

        //        // Now we are launching the state machine for GetPlacesIntermediate to execute on a different thread than the UI thread. But we are blocking the UI thread waiting for GetPlacesIntermediate to return.
        //        // We will fix the deadlock here.
        //        // But since GetPlacesIntermediate or GetPlaces should access anything on the UI thread.
        //        Task.Run(GetPlacesIntermediate).Wait();

        //        // Notes.Text = "Loaded!";

        //        Post();
        //    }
        //    catch (Exception ex)
        //    {
        //        Notes.Text = ex.Message;
        //    }
        //    finally
        //    {
        //    }
        //}

        private async Task<Task<IList<Place>>> GetPlacesIntermediate()
        {
            return await GetPlaces("cafe").ConfigureAwait(false);
            // return await GetPlaces("cafe");
        }

        private async Task<Task<IList<Place>>> GetPlaces(string placeType)
        {
            var service = new PlaceService();

            var places = service.GetAllPlacesAsync(placeType);

            // await places.ConfigureAwait(false); // Indicate that you don't want to execute the continuation on the captured context.

            // Notes.Text = "Loaded!"; // This continuation executes on a separate thread instead of the captured context. Hence, this will throw exception if you try to access elements on the UI thread.

            return places;
        }

        private void Preload()
        {
            stopwatch.Restart();
            Progress.IsIndeterminate = true;
            Progress.Visibility = Visibility.Visible;
        }

        private void Post()
        {
            Status.Text = $"Total load Time: {stopwatch.ElapsedMilliseconds} ms";
            Progress.Visibility = Visibility.Hidden;
        }

        private void Places_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
