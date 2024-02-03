﻿using System;
using Newtonsoft.Json;
using StockAnalyzer.Core.Domain;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using StockAnalyzer.Core;

namespace StockAnalyzer.Windows;

public partial class MainWindow : Window
{
    private static string API_URL = "https://ps-async.fekberg.com/api/stocks";
    private Stopwatch stopwatch = new Stopwatch();

    private CancellationTokenSource? _cancellationTokenSource;

    public MainWindow()
    {
        InitializeComponent();
    }


    private async void Search_Click(object sender, RoutedEventArgs e)
    {
        BeforeLoadingStockData();

        // using var client = new HttpClient();
        // var response = await client.GetAsync($"{API_URL}/{StockIdentifier.Text}");
        //
        // var content = await response.Content.ReadAsStringAsync();
        //
        // var data = JsonConvert.DeserializeObject<IEnumerable<StockPrice>>(content);
        //
        // Stocks.ItemsSource = data;

        if (_cancellationTokenSource is not null)
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;

            Search.Content = "Search";
            return;
        }

        try
        {
            _cancellationTokenSource = new CancellationTokenSource();
            Search.Content = "Cancel";

            // we are avoiding block of ui thread to load file once the file is loaded we will update the ui
            var items  = await GetStocksFromFile(_cancellationTokenSource.Token);

            Stocks.ItemsSource = items;
            AfterLoadingStockData(items.Count());
                    
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
            Search.Content = "Search";
            
            
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            Notes.Text = exception.Message;
        }
    }

    public async Task<IEnumerable<StockPrice>> GetStocksFromFile(CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            var lines = File.ReadAllLines("StockPrices_Small.csv");
            var data = new List<StockPrice>();
            foreach (var line in lines.Skip(1))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                var price = StockPrice.FromCSV(line);
                data.Add(price);
            }

            return data;
        }, cancellationToken);
    }

    private async Task<IEnumerable<StockPrice>> GetStocks()
    {
        var store = new DataStore();
        var data = await store.GetStockPrices(StockIdentifier.Text);
        return data;
    }


    private void BeforeLoadingStockData()
    {
        stopwatch.Restart();
        StockProgress.Visibility = Visibility.Visible;
        StockProgress.IsIndeterminate = true;
    }

    private void AfterLoadingStockData(int count)
    {
        StocksStatus.Text = $"Loaded stocks {count} for {StockIdentifier.Text} in {stopwatch.ElapsedMilliseconds}ms";
        StockProgress.Visibility = Visibility.Hidden;
    }

    private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo { FileName = e.Uri.AbsoluteUri, UseShellExecute = true });

        e.Handled = true;
    }

    private void Close_OnClick(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}